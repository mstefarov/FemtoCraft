// Part of FemtoCraft | Copyright 2012 Matvei Stefarov <me@matvei.org> | See LICENSE.txt

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace FemtoCraft {
    sealed class Player {
        readonly TcpClient client;
        readonly NetworkStream stream;
        readonly PacketReader reader;
        readonly PacketWriter writer;
        readonly Thread thread;

        public IPAddress IP { get; private set; }
        public string Name { get; private set; }
        public bool IsOp { get; private set; }
        public Position Position { get; private set; }

        public bool Connected { get; private set; }

        const int Timeout = 10000;


        public Player( TcpClient newClient ) {
            try {
                client = newClient;
                client.SendTimeout = Timeout;
                client.ReceiveTimeout = Timeout;
                IP = ( (IPEndPoint)( client.Client.RemoteEndPoint ) ).Address;
                stream = client.GetStream();
                reader = new PacketReader( stream );
                writer = new PacketWriter( stream );

                thread = new Thread( IoThread ) {
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
                if( !LoginSequence() ) return;

                while( true ) {

                }


            } catch( IOException ) {} catch( SocketException ) {
#if !DEBUG
            } catch( Exception ex ) {
                Logger.LogError( "Player: Session crashed: {0}", ex );
#endif
            } finally {
                Disconnect();
            }
        }


        void Disconnect() {
            if( Connected ) {
                Logger.Log( "Player {0} disconnected.", Name, IP );
            }
            if( reader != null ) {
                reader.Close();
            }
            if( writer != null ) {
                writer.Close();
            }
            if( client != null ) {
                client.Close();
            }
        }


        bool LoginSequence() {
            // read the first packet
            OpCode opCode = reader.ReadOpCode();
            if( opCode != OpCode.Handshake ) {
                Logger.LogWarning( "Player from {0}: Enexpected handshake packet opcode ({1})",
                                   IP, opCode );
                return false;
            }

            // check protocol version
            int protocolVersion = reader.ReadByte();
            if( protocolVersion != PacketWriter.ProtocolVersion ) {
                Logger.LogWarning( "Player from {0}: Enexpected protocol version ({1})",
                                   IP, protocolVersion );
                return false;
            }

            // check if name is valid
            string name = reader.ReadMCString();
            if( !IsValidName( name ) ) {
                Logger.LogWarning( "Player from {0}: Unacceptible player name ({1})",
                                   IP, name );
                return false;
            }

            // check if name is verified
            string mppass = reader.ReadMCString();
            while( mppass.Length < 32 ) {
                mppass = "0" + mppass;
            }
            MD5 hasher = MD5.Create();
            StringBuilder sb = new StringBuilder( 32 );
            foreach( byte b in hasher.ComputeHash( Encoding.ASCII.GetBytes( Server.Salt + name ) ) ) {
                sb.AppendFormat( "{0:x2}", b );
            }
            bool verified = sb.ToString().Equals( mppass, StringComparison.OrdinalIgnoreCase );
            if( !verified ) {
                Logger.LogWarning( "Player {0} from {1}: Could not verify name.",
                                   name, IP );
                return false;
            }
            Name = name;

            // check if player is banned
            if( Server.Bans.Contains( Name ) ) {
                Kick( "You are banned!" );
                Logger.Log( "Banned player {0} tried to log in from {1}", Name, IP );
                return false;
            }

            // check if player's IP is banned
            if( Server.IPBans.Contains( IP ) ) {
                Kick( "Your IP address is banned!" );
                Logger.Log( "Player {0} tried to log in from a banned IP ({1})", Name, IP );
                return false;
            }

            // skip the unused byte
            reader.ReadByte();

            // check if player is op
            IsOp = Server.Ops.Contains( Name );

            SendMap();

            Connected = true;
            Logger.Log( "Player {0} connected from {1}", Name, IP );
            return true;
        }


        void SendMap() {
            // write handshake
            writer.Write( OpCode.Handshake );
            writer.Write( PacketWriter.ProtocolVersion );
            writer.WriteMCString( Config.ServerName );
            writer.WriteMCString( Config.MOTD );
            writer.Write( IsOp ? (byte)100 : (byte)0 );

            // write MapBegin
            writer.Write( OpCode.MapBegin );

            // grab a compressed copy of the map
            byte[] blockData;
            Map map = Server.Map;
            using( MemoryStream mapStream = new MemoryStream() ) {
                using( GZipStream compressor = new GZipStream( mapStream, CompressionMode.Compress ) ) {
                    int convertedBlockCount = IPAddress.HostToNetworkOrder( map.Volume );
                    compressor.Write( BitConverter.GetBytes( convertedBlockCount ), 0, 4 );
                    compressor.Write( map.Blocks, 0, map.Blocks.Length );
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
                writer.Write( OpCode.MapChunk );
                writer.WriteBE( (short)chunkSize );
                writer.Write( buffer, 0, 1024 );
                writer.Write( progress );
                mapBytesSent += chunkSize;
            }

            // write MapEnd
            writer.Write( OpCode.MapEnd );
            writer.WriteBE( (short)map.Width );
            writer.WriteBE( (short)map.Height );
            writer.WriteBE( (short)map.Length );

            // write spawn point
            writer.Write( OpCode.AddEntity );
            writer.Write( (byte)255 );
            writer.WriteMCString( Name );
            writer.WriteBE( map.Spawn.X );
            writer.WriteBE( map.Spawn.Z );
            writer.WriteBE( map.Spawn.Y );
            writer.Write( map.Spawn.R );
            writer.Write( map.Spawn.L );
            
            // write self-teleport
            writer.Write( OpCode.Teleport );
            writer.Write( (byte)255 );
            writer.WriteBE( map.Spawn.X );
            writer.WriteBE( map.Spawn.Z );
            writer.WriteBE( map.Spawn.Y );
            writer.Write( map.Spawn.R );
            writer.Write( map.Spawn.L );
        }


        public static void Kick( string message ) {
        }


        public static bool IsValidName( string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            if( name.Length < 2 || name.Length > 16 ) return false;
            return name.All( ch => ( ch >= '0' || ch == '.' ) &&
                                   ( ch <= '9' || ch >= 'A' ) &&
                                   ( ch <= 'Z' || ch >= '_' ) &&
                                   ( ch <= '_' || ch >= 'a' ) &&
                                   ch <= 'z' );
        }
    }
}