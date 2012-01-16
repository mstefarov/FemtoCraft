// Part of FemtoCraft | Copyright 2012 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace FemtoCraft {
    static class Server {

        public const string VersionString = "FemtoCraft 0.10";

        public static readonly string Salt = Util.GenerateSalt();
        public static Uri Uri { get; set; }

        public static int PlayerCount { get; set; }

        const string MapFileName = "map.lvl";
        const int MapSaveInterval = 3000;
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
            int tick = 0;
            while( true ) {
                if( listener.Pending() ) {
                    try {
                        listener.BeginAcceptTcpClient( AcceptCallback, null );
                    } catch( Exception ex ) {
                        Logger.LogWarning( "Could not accept incoming connection: {0}", ex );
                    }
                }

                if( tick % MapSaveInterval == 0 ) {
                    Map.Save( MapFileName );
                    Logger.Log( "Map saved to {0}", MapFileName );
                }

                tick++;
                Thread.Sleep( 10 );
            }
        }
        static TcpListener listener;


        static void AcceptCallback( IAsyncResult e ) {
            TcpClient client = listener.EndAcceptTcpClient( e );
            new Player( client );
        }
    }
}