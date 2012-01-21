// Part of FemtoCraft | Copyright 2012 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Linq;
using JetBrains.Annotations;

namespace FemtoCraft {
    static class Server {
        public const string VersionString = "FemtoCraft 0.53";

        public static readonly string Salt = Util.GenerateSalt();
        public static Uri Uri { get; set; }

        const string MapFileName = "map.lvl";
        public static Map Map { get; private set; }

        const string BansFileName = "banned.txt";
        public static PlayerNameSet Bans { get; private set; }

        const string OpsFileName = "admins.txt";
        public static PlayerNameSet Ops { get; private set; }

        const string IPBanFileName = "banned-ip.txt";
        public static IPAddressSet IPBans { get; private set; }

        const string WhitelistFileName = "whitelist.txt";
        public static PlayerNameSet Whitelist { get; private set; }


        static void Main() {
#if !DEBUG
            try {
#endif
            Console.Title = VersionString;
            Logger.Log( "Starting {0}", VersionString );

            // load config
            Config.Load();
            Console.Title = Config.ServerName + " - " + VersionString;

            // prepare to accept players and fire up the heartbeat
            for( byte i = 1; i <= sbyte.MaxValue; i++ ) {
                FreePlayerIDs.Push( i );
            }
            UpdatePlayerList();
            Heartbeat.Start();

            // load player and IP lists
            Bans = new PlayerNameSet( BansFileName );
            Ops = new PlayerNameSet( OpsFileName );
            IPBans = new IPAddressSet( IPBanFileName );
            Logger.Log( "Server: Tracking {0} bans and {1} ops.",
                        Bans.Count, Ops.Count );
            if( Config.UseWhitelist ) {
                Whitelist = new PlayerNameSet( WhitelistFileName );
                Logger.Log( "Using a whitelist ({0} players): {1}",
                            Whitelist.Count, Whitelist.GetCopy().JoinToString( ", " ) );
            }

            // load or create map
            if( File.Exists( MapFileName ) ) {
                Map = Map.Load( MapFileName );
                Logger.Log( "Loaded map from {0}", MapFileName );
            } else {
                Map = Map.CreateFlatgrass( 256, 256, 64 );
                Map.Save( MapFileName );
            }

            // start listening for incoming connections
            listener = new TcpListener( Config.IP, Config.Port );
            listener.Start();

            // start the scheduler thread
            Thread schedulerThread = new Thread( SchedulerLoop ) {
                IsBackground = true
            };
            schedulerThread.Start();

            // listen for console input
            while( true ) {
                string input = Console.ReadLine();
                if( input == null ) return;
                Player.Console.ProcessMessage( input );
            }

#if !DEBUG
            } catch( Exception ex ) {
                Logger.LogError( "Server crashed: {0}", ex );
            }
#endif
        }


        #region Scheduler

        static TcpListener listener;
        static readonly TimeSpan MapSaveInterval = TimeSpan.FromSeconds( 60 );
        static readonly TimeSpan PingInterval = TimeSpan.FromSeconds( 5 );
        static TimeSpan physicsInterval;


        static void SchedulerLoop() {
            DateTime physicsTick = DateTime.UtcNow;
            DateTime mapTick = DateTime.UtcNow;
            DateTime pingTick = DateTime.UtcNow;
            Logger.Log( "{0} is ready to go!", VersionString );
            physicsInterval = TimeSpan.FromMilliseconds( Config.PhysicsTick );

            while( true ) {
                if( listener.Pending() ) {
                    try {
                        listener.BeginAcceptTcpClient( AcceptCallback, null );
                    } catch( Exception ex ) {
                        Logger.LogWarning( "Could not accept incoming connection: {0}", ex );
                    }
                }

                if( DateTime.UtcNow.Subtract( mapTick ) > MapSaveInterval ) {
                    ThreadPool.QueueUserWorkItem( MapSaveCallback );
                    mapTick = DateTime.UtcNow;
                }

                if( DateTime.UtcNow.Subtract( pingTick ) > PingInterval ) {
                    Players.Send( null, new Packet( OpCode.Ping ) );
                    pingTick = DateTime.UtcNow;
                }

                while( DateTime.UtcNow.Subtract( physicsTick ) > physicsInterval ) {
                    Map.PhysicsOnTick();
                    physicsTick += physicsInterval;
                }

                Thread.Sleep( 5 );
            }
        }


        static void AcceptCallback( [NotNull] IAsyncResult e ) {
            if( e == null ) throw new ArgumentNullException( "e" );
            TcpClient client = listener.EndAcceptTcpClient( e );
            new Player( client );
        }


        static void MapSaveCallback( object unused ) {
            try {
                Map.Save( MapFileName );
                Logger.Log( "Map saved to {0}", MapFileName );
            } catch( Exception ex ) {
                Logger.LogError( "Failed to save map: {0}", ex );
            }
        }

        #endregion


        #region Player List

        [NotNull]
        public static Player[] Players { get; private set; }
        static readonly Stack<byte> FreePlayerIDs = new Stack<byte>( 127 );
        static readonly List<Player> PlayerIndex = new List<Player>();
        static readonly object PlayerListLock = new object();


        public static bool RegisterPlayer( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            lock( PlayerListLock ) {
                // Kick other sessions with same player name
                Player ghost = PlayerIndex.FirstOrDefault( p => p.Name.Equals( player.Name,
                                                                               StringComparison.OrdinalIgnoreCase ) );
                if( ghost != null ) {
                    // Wait for other session to exit/unregister
                    Logger.Log( "Kicked a duplicate connection from {0} for player {1}.",
                                ghost.IP, ghost.Name );
                    ghost.KickSynchronously( "Connected from elsewhere!" );
                }

                // check the number of connections from this IP.
                if( !player.IP.Equals( IPAddress.Loopback ) &&
                    ( !player.IsOp && Config.MaxConnections > 0 || player.IsOp && Config.OpMaxConnections > 0 ) ) {
                    int connections = PlayerIndex.Count( p => p.IP.Equals( player.IP ) );
                    if( connections >= Config.MaxConnections ) {
                        player.Kick( "Too many connections from your IP address!" );
                        return false;
                    }
                }

                // check if server is full
                if( PlayerIndex.Count >= Config.MaxPlayers ) {
                    if( Config.AdminSlot && player.IsOp ) {
                        // if player has a reserved slot, kick someone to make room
                        Player playerToKick = Players.OrderBy( p => p.LastActiveTime )
                                                     .FirstOrDefault( p => p.IsOp );
                        if( playerToKick != null ) {
                            playerToKick.KickSynchronously( "Making room for an op." );
                        } else {
                            player.Kick( "Server is full of ops!" );
                            return false;
                        }
                    } else {
                        player.Kick( "Server is full!" );
                        return false;
                    }
                }

                // Assign index and spawn player
                player.ID = FreePlayerIDs.Pop();
                Players.Send( null, Packet.MakeAddEntity( player.ID, player.Name, Map.Spawn ) );
                player.HasRegistered = true;

                // Spawn existing players
                foreach( Player other in PlayerIndex ) {
                    player.Send( Packet.MakeAddEntity( other.ID, other.Name, other.Position ) );
                }

                // Add player to index
                PlayerIndex.Add( player );
                UpdatePlayerList();
                player.Map = Map;
            }
            return true;
        }


        public static void UnregisterPlayer( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( !player.HasRegistered ) return;
            lock( PlayerListLock ) {
                // Despawn player entity
                Players.Send( player, Packet.MakeRemoveEntity( player.ID ) );
                FreePlayerIDs.Push( player.ID );

                // Remove player from index
                PlayerIndex.Remove( player );
                UpdatePlayerList();

                // Announce departure
                Logger.Log( "Player {0} left the server.", player.Name );
                Players.Message( null, false,
                                 "Player {0} left the server.", player.Name );
            }
        }


        static void UpdatePlayerList() {
            Players = PlayerIndex.OrderBy( player => player.Name, StringComparer.OrdinalIgnoreCase )
                                 .ToArray();
        }


        [CanBeNull]
        public static Player FindPlayerExact( [NotNull] string fullName ) {
            if( fullName == null ) throw new ArgumentNullException( "fullName" );
            return Players.FirstOrDefault( p => p.Name.Equals( fullName, StringComparison.OrdinalIgnoreCase ) );
        }


        [CanBeNull]
        public static Player FindPlayer( [NotNull] Player player, [NotNull] string partialName ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( partialName == null ) throw new ArgumentNullException( "partialName" );
            List<Player> matches = new List<Player>();
            foreach( Player otherPlayer in Players ) {
                if( otherPlayer.Name.Equals( partialName, StringComparison.OrdinalIgnoreCase ) ) {
                    return player;
                }
                if( otherPlayer.Name.StartsWith( partialName, StringComparison.OrdinalIgnoreCase ) ) {
                    matches.Add( otherPlayer );
                }
            }
            switch( matches.Count ) {
                case 0:
                    player.Message( "No players found matching \"{0}\"", partialName );
                    return null;
                case 1:
                    return matches[0];
                default:
                    player.Message( "More than one player matched \"{0}\": {1}",
                                    partialName, matches.JoinToString( ", ", p => p.Name ) );
                    return null;
            }
        }

        #endregion


        public static void ChangeMap( [NotNull] Map newMap ) {
            if( newMap == null ) throw new ArgumentNullException( "newMap" );
            ThreadPool.QueueUserWorkItem( ChangeMapCallback, newMap );
        }


        static void ChangeMapCallback( [NotNull] object mapObj ) {
            if( mapObj == null ) throw new ArgumentNullException( "mapObj" );
            Map newMap = (Map)mapObj;
            lock( PlayerListLock ) {
                Player[] playerListCache = PlayerIndex.ToArray();
                foreach( Player player in playerListCache ) {
                    player.KickSynchronously( "Changing map, please rejoin." );
                }
                Map = newMap;
            }
        }


        [StringFormatMethod( "message" )]
        public static void Message( [NotNull] this IEnumerable<Player> source,
                                    [NotNull] string message, [NotNull] params object[] formatArgs ) {
            if( source == null ) throw new ArgumentNullException( "source" );
            if( message == null ) throw new ArgumentNullException( "message" );
            if( formatArgs == null ) throw new ArgumentNullException( "formatArgs" );
            if( formatArgs.Length > 0 ) {
                message = String.Format( message, formatArgs );
            }
            Packet[] packets = new LineWrapper( "&E" + message ).ToArray();
            foreach( Player player in source ) {
                for( int i = 0; i < packets.Length; i++ ) {
                    player.Send( packets[i] );
                }
            }
            Logger.Log( message );
        }


        [StringFormatMethod( "message" )]
        public static void Message( [NotNull] this IEnumerable<Player> source,
                                    [CanBeNull] Player except, bool sentToConsole,
                                    [NotNull] string message, [NotNull] params object[] formatArgs ) {
            if( source == null ) throw new ArgumentNullException( "source" );
            if( message == null ) throw new ArgumentNullException( "message" );
            if( formatArgs == null ) throw new ArgumentNullException( "formatArgs" );
            if( formatArgs.Length > 0 ) {
                message = String.Format( message, formatArgs );
            }
            Packet[] packets = new LineWrapper( "&E" + message ).ToArray();
            foreach( Player player in source ) {
                if( player == except ) continue;
                for( int i = 0; i < packets.Length; i++ ) {
                    player.Send( packets[i] );
                }
            }
            if( except != Player.Console && sentToConsole ) {
                Logger.Log( message );
            }
        }


        public static void Send( [NotNull] this IEnumerable<Player> source, [CanBeNull] Player except,
                                 Packet packet ) {
            if( source == null ) throw new ArgumentNullException( "source" );
            foreach( Player player in source ) {
                if( player == except ) continue;
                player.Send( packet );
            }
        }
    }
}