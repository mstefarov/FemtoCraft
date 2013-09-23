// Part of FemtoCraft | Copyright 2012-2013 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
using System;
using System.IO;
using System.Net;

namespace FemtoCraft {
    static class Config {
        public static string ServerName = "FemtoCraft Server";
        public static string MOTD = "Welcome to the server!";
        public static int Port = 25565;
        public static IPAddress IP = IPAddress.Any;
        public static int MaxPlayers = 20;
        // ReSharper disable RedundantDefaultFieldInitializer
        public static bool Public = false;
        public static bool VerifyNames = true;
        public static bool UseWhitelist = false;
        public static bool AdminSlot = true;
        public static bool RevealOps = false;
        public static bool AllowEmails = false;
        public static string HeartbeatUrl = "https://minecraft.net/heartbeat.jsp";

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

        public static int PhysicsTick = 50;
        public static bool Physics = true;
        public static bool PhysicsFloodProtection = false;
        public static bool PhysicsGrass = true;
        public static bool PhysicsLava = true;
        public static bool PhysicsPlants = true;
        public static bool PhysicsSand = true;
        public static bool PhysicsSnow = true;
        public static bool PhysicsTrees = false;
        public static bool PhysicsWater = true;

        public static bool AllowWaterBlocks = false;
        public static bool AllowLavaBlocks = false;
        public static bool AllowGrassBlocks = false;
        public static bool AllowSolidBlocks = false;
        public static bool OpAllowWaterBlocks = true;
        public static bool OpAllowLavaBlocks = true;
        public static bool OpAllowGrassBlocks = true;
        public static bool OpAllowSolidBlocks = true;

        public static bool ProtocolExtension = true;
        // ReSharper restore RedundantDefaultFieldInitializer

        public const string OpColor = "&3";

        const string ConfigFileName = "server.properties";


        public static void Load() {
            if( !File.Exists( ConfigFileName ) ) {
                Logger.LogWarning( "Config: \"server.properties\" missing, using defaults." );
                Save();
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
                        case "reveal-ops":
                            RevealOps = Boolean.Parse( value );
                            break;
                        case "allow-emails":
                            AllowEmails = Boolean.Parse( value );
                            break;
                        case "heartbeat-url":
                            HeartbeatUrl = value;
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

                        case "physics":
                            Physics = Boolean.Parse( value );
                            break;
                        case "physics-tick":
                            PhysicsTick = Byte.Parse( value );
                            break;
                        case "physics-flood-protection":
                            PhysicsFloodProtection = Boolean.Parse( value );
                            break;
                        case "physics-grass":
                            PhysicsGrass = Boolean.Parse( value );
                            break;
                        case "physics-lava":
                            PhysicsLava = Boolean.Parse( value );
                            break;
                        case "physics-plants":
                            PhysicsPlants = Boolean.Parse( value );
                            break;
                        case "physics-sand":
                            PhysicsSand = Boolean.Parse( value );
                            break;
                        case "physics-snow":
                            PhysicsSnow = Boolean.Parse( value );
                            break;
                        case "physics-trees":
                            PhysicsTrees = Boolean.Parse( value );
                            break;
                        case "physics-water":
                            PhysicsWater = Boolean.Parse( value );
                            break;

                        case "allow-water-blocks":
                            AllowWaterBlocks = Boolean.Parse( value );
                            break;
                        case "allow-lava-blocks":
                            AllowLavaBlocks = Boolean.Parse( value );
                            break;
                        case "allow-grass-blocks":
                            AllowGrassBlocks = Boolean.Parse( value );
                            break;
                        case "allow-solid-blocks":
                            AllowSolidBlocks = Boolean.Parse( value );
                            break;
                        case "op-allow-water-blocks":
                            OpAllowWaterBlocks = Boolean.Parse( value );
                            break;
                        case "op-allow-lava-blocks":
                            OpAllowLavaBlocks = Boolean.Parse( value );
                            break;
                        case "op-allow-grass-blocks":
                            OpAllowGrassBlocks = Boolean.Parse( value );
                            break;
                        case "op-allow-solid-blocks":
                            OpAllowSolidBlocks = Boolean.Parse( value );
                            break;

                        case "protocol-extension":
                            ProtocolExtension = Boolean.Parse( value );
                            break;

                        default:
                            Logger.LogWarning( "Config: Unknown key in {0} line {1}: {2}",
                                               ConfigFileName, lineNumber, line );
                            break;
                    }
                }
            }
            Save();
            Logger.Log( "Config: Loaded configuration from {0}", ConfigFileName );
        }


        public static void Save() {
            using( var writer = new StreamWriter( ConfigFileName ) ) {
                writer.WriteLine( "#{0} configuration file", Server.VersionString );
                writer.WriteLine( "#For instructions, see http://femto.fcraft.net/#configuration" );
                writer.WriteLine();
                writer.WriteLine( "server-name=" + ServerName );
                writer.WriteLine( "motd=" + MOTD );
                writer.WriteLine( "port=" + Port );
                writer.WriteLine( "ip=" + IP );
                writer.WriteLine( "max-players=" + MaxPlayers );
                writer.WriteLine( "public=" + Public );
                writer.WriteLine( "verify-names=" + VerifyNames );
                writer.WriteLine( "use-whitelist=" + UseWhitelist );
                writer.WriteLine( "admin-slot=" + AdminSlot );
                writer.WriteLine( "reveal-ops=" + RevealOps );
                writer.WriteLine( "allow-emails=" + AllowEmails );
                writer.WriteLine( "heartbeat-url=" + HeartbeatUrl );
                writer.WriteLine();
                writer.WriteLine( "max-connections=" + MaxConnections );
                writer.WriteLine( "limit-click-rate=" + LimitClickRate );
                writer.WriteLine( "limit-click-distance=" + LimitClickDistance );
                writer.WriteLine( "limit-chat-rate=" + LimitChatRate );
                writer.WriteLine( "allow-speed-hack=" + AllowSpeedHack );
                writer.WriteLine();
                writer.WriteLine( "op-max-connections=" + OpMaxConnections );
                writer.WriteLine( "op-limit-click-rate=" + OpLimitClickRate );
                writer.WriteLine( "op-limit-click-distance=" + OpLimitClickDistance );
                writer.WriteLine( "op-limit-chat-rate=" + OpLimitChatRate );
                writer.WriteLine( "op-allow-speed-hack=" + OpAllowSpeedHack );
                writer.WriteLine();
                writer.WriteLine( "physics=" + Physics );
                writer.WriteLine( "physics-tick=" + PhysicsTick );
                writer.WriteLine( "physics-flood-protection=" + PhysicsFloodProtection );
                writer.WriteLine( "physics-grass=" + PhysicsGrass );
                writer.WriteLine( "physics-lava=" + PhysicsLava );
                writer.WriteLine( "physics-plants=" + PhysicsPlants );
                writer.WriteLine( "physics-sand=" + PhysicsSand );
                writer.WriteLine( "physics-snow=" + PhysicsSnow );
                writer.WriteLine( "physics-trees=" + PhysicsTrees );
                writer.WriteLine( "physics-water=" + PhysicsWater );
                writer.WriteLine();
                writer.WriteLine( "allow-water-blocks=" + AllowWaterBlocks );
                writer.WriteLine( "allow-lava-blocks=" + AllowLavaBlocks );
                writer.WriteLine( "allow-grass-blocks=" + AllowGrassBlocks );
                writer.WriteLine( "allow-solid-blocks=" + AllowSolidBlocks );
                writer.WriteLine( "op-allow-water-blocks=" + OpAllowWaterBlocks );
                writer.WriteLine( "op-allow-lava-blocks=" + OpAllowLavaBlocks );
                writer.WriteLine( "op-allow-grass-blocks=" + OpAllowGrassBlocks );
                writer.WriteLine( "op-allow-solid-blocks=" + OpAllowSolidBlocks );
                writer.WriteLine();
                writer.WriteLine( "protocol-extension=" + ProtocolExtension );
            }
        }
    }
}