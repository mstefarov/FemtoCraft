// Part of FemtoCraft | Copyright 2012-2013 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
// Based on fCraft.Player - fCraft is Copyright 2009-2012 Matvei Stefarov <me@matvei.org> | See LICENSE.fCraft.txt
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using JetBrains.Annotations;

namespace FemtoCraft {
    sealed partial class Player {
        public static readonly Player Console = new Player( "(console)" );

        [NotNull]
        public string Name { get; private set; }
        public byte Id { get; set; }

        public IPAddress IP { get; private set; }
        public Position Position { get; private set; }
        public Map Map { get; set; }
        Map mapToJoin;

        bool isOp;
        public bool IsOp {
            get {
                return isOp;
            }
            set {
                if( value == isOp )
                    return;
                isOp = value;
                if( SupportsBlockPermissions ) {
                    SendBlockPermissions();
                } else {
                    Send( Packet.MakeSetPermission( CanUseSolid ) );
                }
            }
        }

        public bool HasRegistered { get; set; }
        public bool HasBeenAnnounced { get; private set; }
        public bool IsPainting { get; set; }
        public DateTime LastActiveTime { get; private set; }

        const int Timeout = 10000,
                  SleepDelay = 5;
        readonly TcpClient client;
        NetworkStream stream;
        PacketReader reader;
        PacketWriter writer;

        static readonly TimeSpan ThrottleInterval = new TimeSpan( 0, 0, 1 );
        DateTime throttleCheckTimer;
        int throttlePacketCount;
        const int ThrottleThreshold = 2500;

        volatile bool canReceive = true,
                      canSend = true,
                      canQueue = true;


        Player( [NotNull] string name ) {
            Name = name;
            IsOp = true;
        }


        public Player( [NotNull] TcpClient newClient ) {
            if( newClient == null ) throw new ArgumentNullException( "newClient" );
            try {
                client = newClient;
                Thread thread = new Thread( IoThread ) {
                    IsBackground = true
                };
                thread.Start();

            } catch( Exception ex ) {
                Logger.LogError( "Player: Error setting up session: {0}", ex );
                Disconnect();
            }
        }


        void IoThread() {
            try {
                client.SendTimeout = Timeout;
                client.ReceiveTimeout = Timeout;
                IP = ( (IPEndPoint)( client.Client.RemoteEndPoint ) ).Address;
                stream = client.GetStream();
                reader = new PacketReader( stream );
                writer = new PacketWriter( stream );
                throttleCheckTimer = DateTime.UtcNow + ThrottleInterval;

                if( !LoginSequence() ) return;

                while( canSend ) {
                    // Write normal packets to output
                    while( sendQueue.Count > 0 ) {
                        Packet packet;
                        lock( sendQueueLock ) {
                            packet = sendQueue.Dequeue();
                        }
                        if( packet.OpCode == OpCode.SetBlockServer ) {
                            ProcessOutgoingSetBlock( ref packet );
                        }
                        writer.Write( packet.Bytes );
                        if( packet.OpCode == OpCode.Kick ) {
                            writer.Flush();
                            return;
                        }
                    }

                    // Write SetBlock packets to output
                    while( blockSendQueue.Count > 0 && throttlePacketCount < ThrottleThreshold && canSend ) {
                        Packet packet;
                        lock( blockSendQueueLock ) {
                            packet = blockSendQueue.Dequeue();
                        }
                        writer.Write( packet.Bytes );
                        throttlePacketCount++;
                    }
                    if( DateTime.UtcNow > throttleCheckTimer ) {
                        throttlePacketCount = 0;
                        throttleCheckTimer += ThrottleInterval;
                    }

                    // Check if a map change is pending. Resend map if it is.
                    if( mapToJoin != Map ) {
                        Map = mapToJoin;
                        for( int i = 1; i < sbyte.MaxValue; i++ ) {
                            writer.Write( Packet.MakeRemoveEntity( i ).Bytes );
                        }
                        SendMap();
                        Server.SpawnPlayers( this );
                    }

                    // Read input from player
                    while( canReceive && stream.DataAvailable ) {
                        OpCode opcode = reader.ReadOpCode();
                        switch( opcode ) {
                            case OpCode.Message:
                                if( !ProcessMessagePacket() ) return;
                                break;

                            case OpCode.Teleport:
                                ProcessMovementPacket();
                                break;

                            case OpCode.SetBlockClient:
                                if( !ProcessSetBlockPacket() ) return;
                                break;

                            case OpCode.Ping:
                                break;

                            default:
                                Logger.Log( "Player {0} was kicked after sending an invalid opcode ({1}).",
                                            Name, opcode );
                                KickNow( "Unknown packet opcode " + opcode );
                                return;
                        }
                    }

                    Thread.Sleep( SleepDelay );
                }


            } catch( IOException ) {} catch( SocketException ) {
#if !DEBUG
            } catch( Exception ex ) {
                Logger.LogError( "Player: Session crashed: {0}", ex );
#endif
            } finally {
                canQueue = false;
                canSend = false;
                Disconnect();
            }
        }


        void Disconnect() {
            if( useSyncKick ) {
                kickWaiter.Set();
            } else {
                Server.UnregisterPlayer( this );
            }
            if( stream != null ) stream.Close();
            if( client != null ) client.Close();
        }


        bool LoginSequence() {
            // start reading the first packet
            OpCode opCode = reader.ReadOpCode();
            //Logger.Log( "Expected: {0} / Received: {1}", OpCode.Handshake, opCode );
            if( opCode != OpCode.Handshake ) {
                Logger.LogWarning( "Player from {0}: Unexpected handshake packet opcode ({1})",
                                   IP, opCode );
                return false;
            }

            // check protocol version
            int protocolVersion = reader.ReadByte();
            if( protocolVersion != Packet.ProtocolVersion ) {
                Logger.LogWarning( "Player from {0}: Wrong protocol version ({1})",
                                   IP, protocolVersion );
                return false;
            }

            // check if name is valid
            string name = reader.ReadString();
            if( !IsValidName( name ) ) {
                KickNow( "Unacceptable player name." );
                Logger.LogWarning( "Player from {0}: Unacceptable player name ({1})",
                                   IP, name );
                return false;
            }

            // check if name is verified
            string mppass = reader.ReadString();
            byte magicNum = reader.ReadByte();
            if( Config.VerifyNames ) {
                while( mppass.Length < 32 ) {
                    mppass = "0" + mppass;
                }
                MD5 hasher = MD5.Create();
                StringBuilder sb = new StringBuilder( 32 );
                foreach( byte b in hasher.ComputeHash( Encoding.ASCII.GetBytes( Heartbeat.Salt + name ) ) ) {
                    sb.AppendFormat( "{0:x2}", b );
                }
                bool verified = sb.ToString().Equals( mppass, StringComparison.OrdinalIgnoreCase );
                if( !verified ) {
                    KickNow( "Could not verify player name." );
                    Logger.LogWarning( "Player {0} from {1}: Could not verify name.",
                                       name, IP );
                    return false;
                }
            }
            Name = name;

            // check if player is banned
            if( Server.Bans.Contains( Name ) ) {
                KickNow( "You are banned!" );
                Logger.LogWarning( "Banned player {0} tried to log in from {1}",
                                   Name, IP );
                return false;
            }

            // check if player's IP is banned
            if( Server.IPBans.Contains( IP ) ) {
                KickNow( "Your IP address is banned!" );
                Logger.LogWarning( "Player {0} tried to log in from a banned IP ({1})",
                                   Name, IP );
                return false;
            }

            // check whitelist
            if( Config.UseWhitelist && !Server.Whitelist.Contains( Name ) ) {
                KickNow( "You are not on the whitelist!" );
                Logger.LogWarning( "Player {0} tried to log in from ({1}), but was not on the whitelist.",
                                   Name, IP );
                return false;
            }

            // negotiate protocol extensions, if applicable
            if( Config.ProtocolExtension && magicNum == 0x42 ) {
                if( !NegotiateProtocolExtension() ) return false;
            }

            // check if player is op
            IsOp = Server.Ops.Contains( Name );

            // register player and send map
            if( !Server.RegisterPlayer( this ) ) return false;

            // write handshake, send map
            writer.Write( Packet.MakeHandshake( CanUseSolid ).Bytes );
            SendMap();

            // announce player, and print MOTD
            Server.Players.Message( this, false,
                                    "Player {0} connected.", Name );
            HasBeenAnnounced = true;
            //Logger.Log( "Send: Message({0})", Config.MOTD );
            Message( Config.MOTD );
            Commands.PlayersHandler( this );
            return true;
        }


        void SendMap() {
            // write MapBegin
            //Logger.Log( "Send: MapBegin()" );
            writer.Write( OpCode.MapBegin );

            // grab a compressed copy of the map
            byte[] blockData;
            Map map = Server.Map;
            using( MemoryStream mapStream = new MemoryStream() ) {
                using( GZipStream compressor = new GZipStream( mapStream, CompressionMode.Compress ) ) {
                    int convertedBlockCount = IPAddress.HostToNetworkOrder( map.Volume );
                    compressor.Write( BitConverter.GetBytes( convertedBlockCount ), 0, 4 );
                    byte[] rawData = ( UsesCustomBlocks ? map.Blocks : map.GetFallbackMap() );
                    compressor.Write( rawData, 0, rawData.Length );
                }
                blockData = mapStream.ToArray();
            }

            // Transfer the map copy
            byte[] buffer = new byte[1024];
            int mapBytesSent = 0;
            while( mapBytesSent < blockData.Length ) {
                int chunkSize = blockData.Length - mapBytesSent;
                if( chunkSize > 1024 ) {
                    chunkSize = 1024;
                } else {
                    // CRC fix for ManicDigger
                    for( int i = 0; i < buffer.Length; i++ ) {
                        buffer[i] = 0;
                    }
                }
                Buffer.BlockCopy( blockData, mapBytesSent, buffer, 0, chunkSize );
                byte progress = (byte)( 100 * mapBytesSent / blockData.Length );

                // write in chunks of 1024 bytes or less
                //Logger.Log( "Send: MapChunk({0},{1})", chunkSize, progress );
                writer.Write( OpCode.MapChunk );
                writer.Write( (short)chunkSize );
                writer.Write( buffer, 0, 1024 );
                writer.Write( progress );
                mapBytesSent += chunkSize;
            }

            // write MapEnd
            writer.Write( OpCode.MapEnd );
            writer.Write( (short)map.Width );
            writer.Write( (short)map.Height );
            writer.Write( (short)map.Length );

            // write spawn point
            writer.Write( Packet.MakeAddEntity( 255, Name, map.Spawn ).Bytes );
            writer.Write( Packet.MakeTeleport( 255, map.Spawn ).Bytes );

            lastValidPosition = map.Spawn;
        }


        public void ChangeMap( Map newMap ) {
            mapToJoin = newMap;
        }


        #region Send / Kick

        readonly object sendQueueLock = new object(),
                        blockSendQueueLock = new object();
        readonly Queue<Packet> sendQueue = new Queue<Packet>();
        readonly Queue<Packet> blockSendQueue = new Queue<Packet>();

        bool useSyncKick;
        readonly AutoResetEvent kickWaiter = new AutoResetEvent( false );


        public void Send( Packet packet ) {
            if( packet.OpCode == OpCode.SetBlockServer ) {
                lock( blockSendQueueLock ) {
                    if( canQueue ) {
                        blockSendQueue.Enqueue( packet );
                    }
                }
            } else {
                lock( sendQueueLock ) {
                    if( canQueue ) {
                        sendQueue.Enqueue( packet );
                    }
                }
            }
        }


        public void Kick( [NotNull] string message ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            Packet packet = Packet.MakeKick( message );
            lock( sendQueueLock ) {
                canReceive = false;
                canQueue = false;
                sendQueue.Enqueue( packet );
            }
        }


        void KickNow( [NotNull] string message ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            canReceive = false;
            canQueue = false;
            canSend = false;
            writer.Write( OpCode.Kick );
            writer.Write( message );
        }


        public void KickSynchronously( [NotNull] string message ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            useSyncKick = true;
            Kick( message );
            kickWaiter.WaitOne();
            Server.UnregisterPlayer( this );
        }

        #endregion


        #region Movement

        // anti-speedhack vars
        int speedHackDetectionCounter,
            positionSyncCounter;

        const int AntiSpeedMaxJumpDelta = 25, // 16 for normal client, 25 for WoM
                  AntiSpeedMaxDistanceSquared = 1024, // 32 * 32
                  AntiSpeedMaxPacketCount = 200,
                  AntiSpeedMaxPacketInterval = 5,
                  PositionSyncInterval = 20;

        // anti-speedhack vars: packet spam
        readonly Queue<DateTime> antiSpeedPacketLog = new Queue<DateTime>();
        DateTime antiSpeedLastNotification = DateTime.UtcNow;
        Position lastValidPosition;


        void ProcessMovementPacket() {
            reader.ReadByte();
            Position newPos = new Position {
                X = reader.ReadInt16(),
                Z = reader.ReadInt16(),
                Y = reader.ReadInt16(),
                R = reader.ReadByte(),
                L = reader.ReadByte()
            };
            Position oldPos = Position;

            // calculate difference between old and new positions
            Position delta = new Position {
                X = (short)( newPos.X - oldPos.X ),
                Y = (short)( newPos.Y - oldPos.Y ),
                Z = (short)( newPos.Z - oldPos.Z ),
                R = (byte)Math.Abs( newPos.R - oldPos.R ),
                L = (byte)Math.Abs( newPos.L - oldPos.L )
            };

            bool posChanged = ( delta.X != 0 ) || ( delta.Y != 0 ) || ( delta.Z != 0 );
            bool rotChanged = ( delta.R != 0 ) || ( delta.L != 0 );

            // skip everything if player hasn't moved
            if( !posChanged && !rotChanged ) return;

            // only reset the timer if player rotated
            // if player is just pushed around, rotation does not change (and timer should not reset)
            if( rotChanged ) ResetIdleTimer();

            if( !IsOp && !Config.AllowSpeedHack || IsOp && !Config.OpAllowSpeedHack ) {
                int distSquared = delta.X * delta.X + delta.Y * delta.Y + delta.Z * delta.Z;
                // speedhack detection
                if( DetectMovementPacketSpam() ) return;
                if( ( distSquared - delta.Z * delta.Z > AntiSpeedMaxDistanceSquared ||
                    delta.Z > AntiSpeedMaxJumpDelta ) && speedHackDetectionCounter >= 0 ) {

                    if( speedHackDetectionCounter == 0 ) {
                        lastValidPosition = Position;
                    } else if( speedHackDetectionCounter > 1 ) {
                        DenyMovement();
                        speedHackDetectionCounter = 0;
                        return;
                    }
                    speedHackDetectionCounter++;

                } else {
                    speedHackDetectionCounter = 0;
                }
            }

            BroadcastMovementChange( newPos, delta );
        }


        void BroadcastMovementChange( Position newPos, Position delta ) {
            Position = newPos;

            bool posChanged = ( delta.X != 0 ) || ( delta.Y != 0 ) || ( delta.Z != 0 );
            bool rotChanged = ( delta.R != 0 ) || ( delta.L != 0 );

            Packet packet;
            // create the movement packet
            if( delta.FitsIntoMoveRotatePacket && positionSyncCounter < PositionSyncInterval ) {
                if( posChanged && rotChanged ) {
                    // incremental position + rotation update
                    packet = Packet.MakeMoveRotate( Id, new Position {
                        X = delta.X,
                        Y = delta.Y,
                        Z = delta.Z,
                        R = newPos.R,
                        L = newPos.L
                    } );

                } else if( posChanged ) {
                    // incremental position update
                    packet = Packet.MakeMove( Id, delta );

                } else if( rotChanged ) {
                    // absolute rotation update
                    packet = Packet.MakeRotate( Id, newPos );
                } else {
                    return;
                }

            } else {
                // full (absolute position + rotation) update
                packet = Packet.MakeTeleport( Id, newPos );
            }

            positionSyncCounter++;
            if( positionSyncCounter >= PositionSyncInterval ) {
                positionSyncCounter = 0;
            }

            Server.Players.Send( this, packet );
        }


        bool DetectMovementPacketSpam() {
            if( antiSpeedPacketLog.Count >= AntiSpeedMaxPacketCount ) {
                DateTime oldestTime = antiSpeedPacketLog.Dequeue();
                double spamTimer = DateTime.UtcNow.Subtract( oldestTime ).TotalSeconds;
                if( spamTimer < AntiSpeedMaxPacketInterval ) {
                    DenyMovement();
                    return true;
                }
            }
            antiSpeedPacketLog.Enqueue( DateTime.UtcNow );
            return false;
        }


        void DenyMovement() {
            writer.Write( Packet.MakeSelfTeleport( lastValidPosition ).Bytes );
            if( DateTime.UtcNow.Subtract( antiSpeedLastNotification ).Seconds > 1 ) {
                Message( "You are not allowed to speedhack." );
                antiSpeedLastNotification = DateTime.UtcNow;
            }
        }

        #endregion


        #region Block Placement

        public bool PlaceWater,
                    PlaceLava,
                    PlaceSolid,
                    PlaceGrass;

        readonly Queue<DateTime> spamBlockLog = new Queue<DateTime>();

        const int AntiGriefBlocks = 47,
                  AntiGriefSeconds = 6,
                  MaxBlockPlacementRange = 7 * 32;


        bool ProcessSetBlockPacket() {
            ResetIdleTimer();
            short x = reader.ReadInt16();
            short z = reader.ReadInt16();
            short y = reader.ReadInt16();
            bool isDeleting = ( reader.ReadByte() == 0 );
            byte rawType = reader.ReadByte();

            // check if block type is valid
            if( !UsesCustomBlocks && rawType > (byte)Map.MaxLegalBlockType ||
                UsesCustomBlocks && rawType > (byte)Map.MaxCustomBlockType ) {
                KickNow( "Hacking detected." );
                Logger.LogWarning( "Player {0} tried to place an invalid block type.", Name );
                return false;
            }
            if( IsPainting ) isDeleting = false;
            Block block = (Block)rawType;
            if( isDeleting ) block = Block.Air;

            // check if coordinates are within map boundaries (don't kick)
            if( !Map.InBounds( x, y, z ) ) return true;

            // check if player is close enough to place
            if( !IsOp && Config.LimitClickDistance || IsOp && Config.OpLimitClickDistance ) {
                if( Math.Abs( x * 32 - Position.X ) > MaxBlockPlacementRange ||
                    Math.Abs( y * 32 - Position.Y ) > MaxBlockPlacementRange ||
                    Math.Abs( z * 32 - Position.Z ) > MaxBlockPlacementRange ) {
                    KickNow( "Hacking detected." );
                    Logger.LogWarning( "Player {0} tried to place a block too far away.", Name );
                    return false;
                }
            }

            // check click rate
            if( !IsOp && Config.LimitClickRate || IsOp && Config.OpLimitClickRate ) {
                if( DetectBlockSpam() ) {
                    KickNow( "Hacking detected." );
                    Logger.LogWarning( "Player {0} tried to place blocks too quickly.", Name );
                    return false;
                }
            }

            // apply blocktype mapping
            if( block == Block.Blue && PlaceWater ) {
                block = Block.Water;
            } else if( block == Block.Red && PlaceLava ) {
                block = Block.Lava;
            } else if( block == Block.Stone && PlaceSolid ) {
                block = Block.Admincrete;
            } else if( block == Block.Dirt && PlaceGrass ) {
                block = Block.Grass;
            }

            // check if blocktype is permitted
            if( ( block == Block.Water || block == Block.StillWater ) && !CanUseWater ||
                ( block == Block.Lava || block == Block.StillLava ) && !CanUseLava ||
                ( block == Block.Grass ) && !CanUseGrass ||
                ( block == Block.Admincrete || block == Block.Admincrete ) && !CanUseSolid ) {
                KickNow( "Hacking detected." );
                Logger.LogWarning( "Player {0} tried to place a restricted block type.", Name );
                return false;
            }

            // check if deleting admincrete
            Block oldBlock = Map.GetBlock( x, y, z );
            if( ( oldBlock == Block.Admincrete ) && !CanUseSolid ) {
                KickNow( "Hacking detected." );
                Logger.LogWarning( "Player {0} tried to delete a restricted block type.", Name );
                return false;
            }

            // update map
            Map.SetBlock( this, x, y, z, block );

            // check if sending back an update is necessary
            Block placedBlock = Map.GetBlock( x, y, z );
            if( IsPainting || ( !isDeleting && placedBlock != (Block)rawType ) ) {
                writer.Write( Packet.MakeSetBlock( x, y, z, placedBlock ).Bytes );
            }
            return true;
        }


        bool DetectBlockSpam() {
            if( spamBlockLog.Count >= AntiGriefBlocks ) {
                DateTime oldestTime = spamBlockLog.Dequeue();
                double spamTimer = DateTime.UtcNow.Subtract( oldestTime ).TotalSeconds;
                if( spamTimer < AntiGriefSeconds ) {
                    return true;
                }
            }
            spamBlockLog.Enqueue( DateTime.UtcNow );
            return false;
        }

        #endregion


        #region Messaging

        [CanBeNull] string partialMessage;

        const int AntispamMessageCount = 3,
                  AntispamInterval = 4;

        readonly Queue<DateTime> spamChatLog = new Queue<DateTime>( AntispamMessageCount );


        bool ProcessMessagePacket() {
            ResetIdleTimer();
            reader.ReadByte();
            string message = reader.ReadString();

            // special handler for WoM id packets
            // (which are erroneously padded with zeroes instead of spaces).
            if( message.StartsWith( "/womid " ) ) return true;

            if( ContainsInvalidChars( message ) ) {
                KickNow( "Hacking detected." );
                Logger.LogWarning( "Player {0} attempted to write illegal characters in chat.",
                                   Name );
                return false;
            }

            if( !IsOp && Config.LimitChatRate || IsOp && Config.OpLimitChatRate ) {
                if( DetectChatSpam() ) return false;
            }

            ProcessMessage( message );
            return true;
        }


        public void ProcessMessage( [NotNull] string rawMessage ) {
            if( rawMessage == null ) throw new ArgumentNullException( "rawMessage" );
            if( rawMessage.Length == 0 ) return;

            // cancel partial message
            if( rawMessage.StartsWith( "/nvm", StringComparison.OrdinalIgnoreCase ) ||
                rawMessage.StartsWith( "/cancel", StringComparison.OrdinalIgnoreCase ) ) {
                if( partialMessage != null ) {
                    Message( "Partial message cancelled." );
                    partialMessage = null;
                } else {
                    Message( "No partial message to cancel." );
                }
                return;
            }

            // handle partial messages
            if( partialMessage != null ) {
                rawMessage = partialMessage + rawMessage;
                partialMessage = null;
            }
            if( rawMessage.EndsWith( " /" ) ) {
                partialMessage = rawMessage.Substring( 0, rawMessage.Length - 1 );
                Message( "Partial: &F{0}", partialMessage );
                return;
            }
            if( rawMessage.EndsWith( " //" ) ) {
                rawMessage = rawMessage.Substring( 0, rawMessage.Length - 1 );
            }

            // handle commands
            if( rawMessage[0] == '/' ) {
                if( rawMessage.Length < 2 ) {
                    Message( "Cannot parse message." );
                    return;
                } else if( rawMessage[1] == '/' ) {
                    rawMessage = rawMessage.Substring( 1 );
                } else {
                    Commands.Parse( this, rawMessage );
                    return;
                }
            }

            // broadcast chat
            Logger.LogChat( "{0}: {1}", Name, rawMessage );
            if( Config.RevealOps && IsOp ) {
                Server.Players.Message( null, false,
                                        "{0}{1}&F: {2}",
                                        Config.OpColor, Name, rawMessage );
            } else {
                Server.Players.Message( null, false,
                                        "&F{0}: {1}",
                                        Name, rawMessage );
            }
        }


        [StringFormatMethod( "message" )]
        public void Message( [NotNull] string message, [NotNull] params object[] formatArgs ) {
            if( formatArgs.Length > 0 ) {
                message = String.Format( message, formatArgs );
            }
            if( this == Console ) {
                System.Console.WriteLine( message );
            } else {
                foreach( Packet p in new LineWrapper( "&E" + message ) ) {
                    Send( p );
                }
            }
        }


        [StringFormatMethod( "message" )]
        public void MessageNow( [NotNull] string message, [NotNull] params object[] formatArgs ) {
            if( formatArgs.Length > 0 ) {
                message = String.Format( message, formatArgs );
            }
            if( this == Console ) {
                System.Console.WriteLine( message );
            } else {
                foreach( Packet p in new LineWrapper( "&E" + message ) ) {
                    writer.Write( p.Bytes );
                }
            }
        }


        public bool CheckIfOp() {
            if( !IsOp ) Message( "You must be op to do this." );
            return IsOp;
        }


        public bool CheckIfConsole() {
            if( this == Console ) Message( "You cannot use this command from console." );
            return (this == Console);
        }


        [ContractAnnotation( "givenName:null => false" )]
        public bool CheckPlayerName( [CanBeNull] string givenName ) {
            if( givenName == null ) {
                Message( "This command requires a player name." );
                return false;
            } else if( !IsValidName( givenName ) ) {
                Message( "\"{0}\" is not a valid player name.", givenName );
                return false;
            } else {
                return true;
            }
        }


        public bool CheckIfAllowed( bool guestConfigKey, bool opConfigKey ) {
            if( CheckIfConsole() ) return false;
            if( !guestConfigKey ) {
                if( !opConfigKey ) {
                    Message( "This command is disabled on this server." );
                } else if( !CheckIfOp() ) {
                    return false;
                }
            }
            return true;
        }


        bool DetectChatSpam() {
            if( this == Console ) return false;
            if( spamChatLog.Count >= AntispamMessageCount ) {
                DateTime oldestTime = spamChatLog.Dequeue();
                if( DateTime.UtcNow.Subtract( oldestTime ).TotalSeconds < AntispamInterval ) {
                    KickNow( "Kicked for chat spam!" );
                    return true;
                }
            }
            spamChatLog.Enqueue( DateTime.UtcNow );
            return false;
        }

        #endregion


        #region Permissions

        bool CanUseWater {
            get { return ( Config.AllowWaterBlocks || Config.OpAllowWaterBlocks && IsOp ); }
        }

        bool CanUseLava {
            get { return ( Config.AllowLavaBlocks || Config.OpAllowLavaBlocks && IsOp ); }
        }

        bool CanUseGrass {
            get { return ( Config.AllowGrassBlocks || Config.OpAllowGrassBlocks && IsOp ); }
        }

        bool CanUseSolid {
            get { return ( Config.AllowSolidBlocks || Config.OpAllowSolidBlocks && IsOp ); }
        }

        #endregion


        void ResetIdleTimer() {
            LastActiveTime = DateTime.UtcNow;
        }


        public static bool IsValidName( [NotNull] string name ) {
            if( name.Length < 2 || name.Length > 16 ) return false;
            return name.All( ch => ( ch >= '0' || ch == '.' ) &&
                                   ( ch <= '9' || ch >= 'A' ) &&
                                   ( ch <= 'Z' || ch >= '_' ) &&
                                   ( ch <= '_' || ch >= 'a' ) &&
                                   ch <= 'z' );
        }


        // checks if message contains any characters that cannot be typed in from Minecraft client
        static bool ContainsInvalidChars( [NotNull] string message ) {
            return message.Any( t => t < ' ' || t == '&' || t > '~' );
        }
    }
}