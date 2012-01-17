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

        // list of registered players
        static readonly List<Player> PlayerIndex = new List<Player>();

        /// <summary> List of currently registered players. </summary>
        public static Player[] Players { get; private set; }

        static readonly object PlayerListLock = new object();

        // Registers a player and checks if the server is full.
        // Also kicks any existing connections for this player account.
        // Returns true if player was registered succesfully.
        // Returns false if the server was full.
        internal static bool RegisterPlayer( Player player ) {
            lock( PlayerListLock ) {

                // Kick other sessions with same player name
                Player ghost = PlayerIndex.FirstOrDefault( p => p.Name.Equals( player.Name, StringComparison.OrdinalIgnoreCase ) );
                if( ghost != null ) {
                    // Wait for other session to exit/unregister
                    Logger.Log( "Kicked a duplicate connection from {0} for player {1}.",
                                ghost.IP, ghost.Name );
                    ghost.KickSynchronously( "Connected from elsewhere!" );
                }

                // check the number of connections from this IP.
                if( !player.IP.Equals( IPAddress.Loopback ) && Config.MaxConnections > 0 ) {
                    int sessionCount = 0;
                    foreach( Player p in PlayerIndex ) {
                        if( p.IP.Equals( player.IP ) ) {
                            sessionCount++;
                            if( sessionCount >= Config.MaxConnections ) {
                                return false;
                            }
                        }
                    }
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
                PlayerIndex.Add( player );
                player.IsRegistered = true;
            }

            // todo: accept player
            return true;
        }


        // Removes player from the list or registered players, and announces them leaving
        public static void UnregisterPlayer( Player player ) {
            if( !player.IsRegistered ) return;
            lock( PlayerListLock ) {
                Logger.Log( "Player {0} left the server.", player.Name );
                // todo: announce leaving
                // todo: release player
                PlayerIndex.Remove( player );
                UpdatePlayerList();
            }
        }


        static void UpdatePlayerList() {
            lock( PlayerListLock ) {
                Players = PlayerIndex.Where( p => p.IsOnline )
                                     .OrderBy( player => player.Name )
                                     .ToArray();
            }
        }
    }
}