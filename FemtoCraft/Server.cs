﻿// Part of FemtoCraft | Copyright 2012 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
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
        public const string VersionString = "FemtoCraft 0.26";

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


        const string WhitelistFileName = "whitelist.txt";
        public static PlayerNameSet Whitelist { get; private set; }


        static void Main() {
#if !DEBUG
            try {
#endif
            Console.Title = VersionString;
            Logger.Log( "Starting {0}", VersionString );

            // load config and fire up the heartbeat
            Config.Load();
            Console.Title = Config.ServerName + " - " + VersionString;
            Heartbeat.Start();

            // load player and IP lists
            Bans = new PlayerNameSet( BansFileName );
            Ops = new PlayerNameSet( OpsFileName );
            IPBans = new IPAddressSet( IPBanFileName );
            Logger.Log( "Server: Tracking {0} bans and {1} ops.",
                        Bans.Count, Ops.Count );
            if( Config.UseWhitelist ) {
                Whitelist = new PlayerNameSet( WhitelistFileName );
                Logger.Log( "Using a whitelist ({0} players).", Whitelist.Count );
            }

            // load or create map
            if( File.Exists( MapFileName ) ) {
                Map = Map.Load( MapFileName );
            } else {
                Map = Map.CreateFlatgrass( 256, 256, 64 );
                Map.Save( MapFileName );
            }

            // start listening for incoming connections
            for( byte i = 0; i <= sbyte.MaxValue; i++ ) {
                FreePlayerIDs.Push( i );
            }
            UpdatePlayerList();

            listener = new TcpListener( IPAddress.Any, Config.Port );
            listener.Start();

            // start the scheduler thread
            Thread schedulerThread = new Thread( MainLoop ) {
                IsBackground = true
            };
            schedulerThread.Start();

            // listen for console input
            while( true ) {
                string input = Console.ReadLine();
                if( input == null ) return;
                Commands.Parse( Player.Console, input );
            }

#if !DEBUG
            } catch( Exception ex ) {
                Logger.LogError( "Server crashed: {0}", ex );
            }
#endif
        }


        #region Scheduler

        static TcpListener listener;
        static readonly TimeSpan PhysicsInterval = TimeSpan.FromMilliseconds( 100 );
        static readonly TimeSpan MapSaveInterval = TimeSpan.FromSeconds( 60 );
        static readonly TimeSpan PingInterval = TimeSpan.FromSeconds( 5 );


        static void MainLoop() {
            DateTime now = DateTime.UtcNow;
            DateTime physicsTick = now;
            DateTime mapTick = now;
            DateTime pingTick = now;

            while( true ) {
                now = DateTime.UtcNow;

                if( listener.Pending() ) {
                    try {
                        listener.BeginAcceptTcpClient( AcceptCallback, null );
                    } catch( Exception ex ) {
                        Logger.LogWarning( "Could not accept incoming connection: {0}", ex );
                    }
                }

                if( now.Subtract( mapTick ) > MapSaveInterval ) {
                    ThreadPool.QueueUserWorkItem( MapSaveCallback );
                    mapTick = now;
                }
                if( now.Subtract( pingTick ) > PingInterval ) {
                    Players.Send( null, new Packet( OpCode.Ping ) );
                    pingTick = now;
                }

                while( now.Subtract( physicsTick ) > PhysicsInterval ) {
                    // todo: tick physics
                    physicsTick += PhysicsInterval;
                }

                Players.Send( null, new Packet( OpCode.Ping ) );

                Thread.Sleep( 10 );
            }
        }


        static void AcceptCallback( [NotNull] IAsyncResult e ) {
            TcpClient client = listener.EndAcceptTcpClient( e );
            new Player( client );
        }


        static void MapSaveCallback( object unused ) {
            Map.Save( MapFileName );
            Logger.Log( "Map saved to {0}", MapFileName );
        }

        #endregion


        #region Player List

        [NotNull]
        public static Player[] Players { get; private set; }

        static readonly Stack<byte> FreePlayerIDs = new Stack<byte>( 127 );
        static readonly List<Player> PlayerIndex = new List<Player>();

        static readonly object PlayerListLock = new object();


        public static bool RegisterPlayer( [NotNull] Player player ) {
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
                if( !player.IP.Equals( IPAddress.Loopback ) && Config.MaxConnections > 0 ) {
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

                // Assign index and spawn player entity
                player.ID = FreePlayerIDs.Pop();
                Players.Send( null, Packet.MakeAddEntity( player.ID, player.Name, Map.Spawn ) );
                player.HasRegistered = true;

                // Add player to index
                PlayerIndex.Add( player );
                UpdatePlayerList();
            }
            return true;
        }


        public static void UnregisterPlayer( [NotNull] Player player ) {
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
            return Players.FirstOrDefault( p => p.Name.Equals( fullName, StringComparison.OrdinalIgnoreCase ) );
        }


        [CanBeNull]
        public static Player FindPlayer( [NotNull] Player player, [NotNull] string partialName ) {
            List<Player> matches = new List<Player>();
            foreach( Player otherPlayer in Players ) {
                if( otherPlayer.Name.Equals( partialName, StringComparison.OrdinalIgnoreCase ) ) {
                    return player;
                }
                if( otherPlayer.Name.StartsWith( partialName, StringComparison.OrdinalIgnoreCase ) ) {
                    matches.Add( otherPlayer );
                }
            }
            if( matches.Count == 0 ) {
                player.Message( "No players found matching \"{0}\"", partialName );
            } else if( matches.Count == 1 ) {
                return matches[0];
            } else {
                player.Message( "More than one player matched \"{0}\": {1}",
                                partialName, matches.JoinToString( ", ", p => p.Name ) );
            }
            return null;
        }

        #endregion

        [StringFormatMethod( "message" )]
        public static void Message( [NotNull] this IEnumerable<Player> source,
                                    [NotNull] string message, [NotNull] params object[] formatArgs ) {
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
            foreach( Player player in source ) {
                if( player == except ) continue;
                player.Send( packet );
            }
        }
    }
}