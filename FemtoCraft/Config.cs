// Part of FemtoCraft | Copyright 2012 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
using System;
using System.IO;
using System.Net;

namespace FemtoCraft {
    static class Config {
        public static string ServerName = "FemtoCraft Server";
        public static string MOTD = "Welcome to the server.";
        public static int Port = 25565;
        public static IPAddress IP = IPAddress.Any;
        public static int MaxPlayers = 20;
        public static bool Public = false;
        public static bool VerifyNames = true;
        public static bool UseWhitelist = false;
        public static bool AdminSlot = true;

        public static int MaxConnections = 3;
        public static bool LimitClickRate = true;
        public static bool LimitClickDistance = true;
        public static bool LimitChatRate = true;
        public static bool AllowSpeedHack = false;

        public static int OpMaxConnections = 3;
        public static bool OpLimitClickRate = true;
        public static bool OpLimitClickDistance = true;
        public static bool OpLimitChatRate = true;
        public static bool OpAllowSpeedHack = true;

        const string ConfigFileName = "server.properties";


        public static void Load() {
            if( !File.Exists( ConfigFileName ) ) {
                Logger.LogWarning( "Config.Load: server.properties missing, using defaults." );
                WriteDefaults();
                return;
            }

            using( var reader = File.OpenText( ConfigFileName ) ) {
                int lineNumber = 0;
                while( true ) {
                    string line = reader.ReadLine();
                    lineNumber++;
                    if( line == null ) break;
                    if( line.Length == 0 || line[0] == '#' ) continue;
                    int equalsIndex = line.IndexOf( '=' );
                    if( equalsIndex == -1 ) {
                        Logger.LogWarning( "Config: Skipping {0} line {1}: {2}",
                                           ConfigFileName, lineNumber, line );
                        continue;
                    }
                    string key = line.Substring( 0, equalsIndex ).Trim();
                    string value = line.Substring( equalsIndex + 1 ).Trim();
                    switch( key ) {
                        case "server-name":
                            ServerName = value;
                            break;
                        case "motd":
                            MOTD = value;
                            break;
                        case "port":
                            Port = UInt16.Parse( value );
                            break;
                        case "ip":
                            IP = IPAddress.Parse( value );
                            break;
                        case "max-players":
                            MaxPlayers = Byte.Parse( value );
                            break;
                        case "public":
                            Public = Boolean.Parse( value );
                            break;
                        case "verify-names":
                            VerifyNames = Boolean.Parse( value );
                            break;
                        case "use-whitelist":
                            UseWhitelist = Boolean.Parse( value );
                            break;
                        case "admin-slot":
                            AdminSlot = Boolean.Parse( value );
                            break;

                        case "max-connections":
                            MaxConnections = Int32.Parse( value );
                            break;
                        case "limit-click-rate":
                            LimitClickRate = Boolean.Parse( value );
                            break;
                        case "limit-click-distance":
                            LimitClickDistance = Boolean.Parse( value );
                            break;
                        case "limit-chat-rate":
                            LimitChatRate = Boolean.Parse( value );
                            break;
                        case "allow-speed-hack":
                            AllowSpeedHack = Boolean.Parse( value );
                            break;

                        case "op-max-connections":
                            OpMaxConnections = Int32.Parse( value );
                            break;
                        case "op-limit-click-rate":
                            OpLimitClickRate = Boolean.Parse( value );
                            break;
                        case "op-limit-click-distance":
                            OpLimitClickDistance = Boolean.Parse( value );
                            break;
                        case "op-limit-chat-rate":
                            OpLimitChatRate = Boolean.Parse( value );
                            break;
                        case "op-allow-speed-hack":
                            OpAllowSpeedHack = Boolean.Parse( value );
                            break;

                        default:
                            Logger.LogWarning( "Config: Unknown key in {0} line {1}: {2}",
                                               ConfigFileName, lineNumber, line );
                            break;
                    }
                }
            }
            Logger.Log( "Config: Loaded configuration from {0}", ConfigFileName );
        }


        static void WriteDefaults() {
            using( var writer = new StreamWriter( ConfigFileName ) ) {
                writer.WriteLine( "#{0} configuration file", Server.VersionString );
                writer.WriteLine( "server-name={0}", ServerName );
                writer.WriteLine( "motd={0}", MOTD );
                writer.WriteLine( "port={0}", Port );
                writer.WriteLine( "ip={0}", IP );
                writer.WriteLine( "max-players={0}", MaxPlayers );
                writer.WriteLine( "public={0}", Public );
                writer.WriteLine( "verify-names={0}", VerifyNames );
                writer.WriteLine( "use-whitelist={0}", UseWhitelist );
                writer.WriteLine( "admin-slot={0}", AdminSlot );

                writer.WriteLine( "max-connections={0}", MaxConnections );
                writer.WriteLine( "limit-click-rate={0}", LimitClickRate );
                writer.WriteLine( "limit-click-distance={0}", LimitClickDistance );
                writer.WriteLine( "limit-chat-rate={0}", LimitChatRate );
                writer.WriteLine( "allow-speed-hack={0}", AllowSpeedHack );

                writer.WriteLine( "op-max-connections={0}", OpMaxConnections );
                writer.WriteLine( "op-limit-click-rate={0}", OpLimitClickRate );
                writer.WriteLine( "op-limit-click-distance={0}", OpLimitClickDistance );
                writer.WriteLine( "op-limit-chat-rate={0}", OpLimitChatRate );
                writer.WriteLine( "op-allow-speed-hack={0}", OpAllowSpeedHack );
            }
        }
    }
}