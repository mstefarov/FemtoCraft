// Part of FemtoCraft | Copyright 2012 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Linq;

namespace FemtoCraft {
    static class Server {
        public const string VersionString = "FemtoCraft 0.13";

        public static readonly string Salt = Util.GenerateSalt();
        public static Uri Uri { get; set; }

        public static int PlayerCount { get; set; }

        const string MapFileName = "map.lvl";
        public static Map Map { get; set; }

        const string BansFileName = "banned.txt";
        public static PlayerNameSet Bans { get; private set; }

        const string OpsFileName = "admins.txt";
        public static PlayerNameSet Ops { get; private set; }

        const string IPBanFileName = "banned-ip.txt";
        public static IPAddressSet IPBans { get; private set; }


        static void Main() {
#if !DEBUG
            try {
#endif
                Console.Title = VersionString;
                Logger.Log( "Starting {0}", VersionString );

                Config.Load();

                Console.Title = Config.ServerName + " - " + VersionString;

                Bans = new PlayerNameSet( BansFileName );
                Ops = new PlayerNameSet( OpsFileName );
                IPBans = new IPAddressSet( IPBanFileName );

                Logger.Log( "Server: Tracking {0} bans and {1} ops.",
                            Bans.Count, Ops.Count );

                Heartbeat.Start();
                if( File.Exists( MapFileName ) ) {
                    Map = Map.Load( MapFileName );
                } else {
                    Map = Map.CreateFlatgrass( 256, 256, 64 );
                }

                Map.Save( MapFileName );

                listener = new TcpListener( IPAddress.Any, Config.Port );
                listener.Start();

                Thread mainThread = new Thread( MainLoop ) {
                                                               IsBackground = true
                                                           };
                mainThread.Start();

                while( true ) {
                    string input = Console.ReadLine();
                    if( input == null ) return;
                    // todo: parse console command
                }

#if !DEBUG
            } catch( Exception ex ) {
                Logger.LogError( "Server crashed: {0}", ex );
            }
#endif
        }


        static void MainLoop() {
            DateTime physicsTick = DateTime.UtcNow;
            DateTime mapTick = DateTime.UtcNow;
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

                while( DateTime.UtcNow.Subtract( physicsTick ) > PhysicsInterval ) {
                    // todo: tick physics
                    physicsTick += PhysicsInterval;
                }

                Thread.Sleep( 10 );
            }
        }


        static void MapSaveCallback( object unused ) {
            Map.Save( MapFileName );
            Logger.Log( "Map saved to {0}", MapFileName );
        }


        static TcpListener listener;
        static readonly TimeSpan PhysicsInterval = TimeSpan.FromMilliseconds( 100 );
        static readonly TimeSpan MapSaveInterval = TimeSpan.FromSeconds( 60 );


        static void AcceptCallback( IAsyncResult e ) {
            TcpClient client = listener.EndAcceptTcpClient( e );
            new Player( client );
        }


        // list of all connected sessions
        static readonly List<Player> Sessions = new List<Player>();

        // Registers a new session, and checks the number of connections from this IP.
        // Returns true if the session was registered succesfully.
        // Returns false if the max number of connections was reached.
        internal static bool RegisterSession( Player session ) {
            if( session == null ) throw new ArgumentNullException( "session" );
            lock( PlayerListLock ) {
                if( !session.IP.Equals( IPAddress.Loopback ) && Config.MaxConnections > 0 ) {
                    int sessionCount = 0;
                    for( int i = 0; i < Sessions.Count; i++ ) {
                        Player p = Sessions[i];
                        if( p.IP.Equals( session.IP ) ) {
                            sessionCount++;
                            if( sessionCount >= Config.MaxConnections ) {
                                return false;
                            }
                        }
                    }
                }
                Sessions.Add( session );
            }
            return true;
        }


        // list of registered players
        static readonly SortedDictionary<string, Player> PlayerIndex = new SortedDictionary<string, Player>();

        /// <summary> List of currently registered players. </summary>
        public static Player[] Players { get; private set; }

        static readonly object PlayerListLock = new object();

        // Registers a player and checks if the server is full.
        // Also kicks any existing connections for this player account.
        // Returns true if player was registered succesfully.
        // Returns false if the server was full.
        internal static bool RegisterPlayer( Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );

            // Kick other sessions with same player name
            lock( PlayerListLock ) {
                List<Player> sessionsToKick = new List<Player>();
                foreach( Player s in Sessions ) {
                    if( s == player ) continue;
                    if( s.Name.Equals( player.Name, StringComparison.OrdinalIgnoreCase ) ) {
                        sessionsToKick.Add( s );
                        Logger.Log( "Kicked a duplicate connection from {0} for player {1}.",
                                    s.IP, s.Name );
                    }
                }

                // Wait for other sessions to exit/unregister (if any)
                foreach( Player sessionToKick in sessionsToKick ) {
                    sessionToKick.KickSynchronously( "Connected from elsewhere!" );
                }

                // Add player to the list
                if( PlayerIndex.Count >= Config.MaxPlayers ) {
                    if( Config.AdminSlot && player.IsOp ) {
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
                PlayerIndex.Add( player.Name, player );
                player.IsRegistered = true;
            }

            // todo: accept player
            return true;
        }


        // Removes player from the list or registered players, and announces them leaving
        public static void UnregisterPlayer( Player player ) {
            lock( PlayerListLock ) {
                if( !player.IsRegistered ) return;

                Logger.Log( "Player {0} left the server.", player.Name );
                if( player.IsRegistered ) {
                    // todo: announce leaving
                }

                // todo: release player
                PlayerIndex.Remove( player.Name );
                UpdatePlayerList();
                Sessions.Remove( player );
            }
        }


        static void UpdatePlayerList() {
            lock( PlayerListLock ) {
                Players = PlayerIndex.Values.Where( p => p.IsOnline )
                                            .OrderBy( player => player.Name )
                                            .ToArray();
            }
        }
    }
}