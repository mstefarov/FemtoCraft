// Part of FemtoCraft | Copyright 2012-2013 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using JetBrains.Annotations;

namespace FemtoCraft {
    static class Commands {
        public static void Parse( [NotNull] Player player, [NotNull] string message ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( message == null ) throw new ArgumentNullException( "message" );
            string command, param;
            int spaceIndex = message.IndexOf( ' ' );
            if( spaceIndex == -1 ) {
                command = message.Substring( 1 ).ToLower();
                param = null;
            } else {
                command = message.Substring( 1, spaceIndex - 1 ).ToLower();
                param = message.Substring( spaceIndex + 1 ).Trim();
            }
            Logger.LogCommand( "{0}: {1}", player.Name, message );

            switch( command ) {
                case "ops":
                    OpsHandler( player );
                    break;
                case "op":
                    OpHandler( player, param );
                    break;
                case "deop":
                    DeopHandler( player, param );
                    break;

                case "kick":
                case "k":
                    KickHandler( player, param );
                    break;

                case "ban":
                    BanHandler( player, param );
                    break;
                case "unban":
                    UnbanHandler( player, param );
                    break;
                case "banip":
                    BanIPHandler( player, param );
                    break;
                case "unbanip":
                    UnbanIPHandler( player, param );
                    break;

                case "solid":
                case "s":
                    SolidHandler( player );
                    break;
                case "water":
                case "w":
                    WaterHandler( player );
                    break;
                case "lava":
                case "l":
                    LavaHandler( player );
                    break;
                case "grass":
                case "g":
                    GrassHandler( player );
                    break;

                case "say":
                case "broadcast":
                    SayHandler( player, param );
                    break;

                case "tp":
                case "teleport":
                    TeleportHandler( player, param );
                    break;
                case "bring":
                    BringHandler( player, param );
                    break;

                case "setspawn":
                    SetSpawnHandler( player );
                    break;

                case "whitelist":
                    WhitelistHandler( player );
                    break;
                case "whitelistadd":
                    WhitelistAddHandler( player, param );
                    break;
                case "whitelistremove":
                    WhitelistRemoveHandler( player, param );
                    break;

                case "load":
                    LoadHandler( player, param );
                    break;
                case "save":
                    SaveHandler( player, param );
                    break;

                case "physics":
                    PhysicsHandler( player, param );
                    break;

                case "p":
                case "paint":
                    PaintHandler( player );
                    break;

                case "players":
                    PlayersHandler( player );
                    break;

                case "gen":
                    GenHandler( false, player, param );
                    break;
                case "genflat":
                    GenHandler( true, player, param );
                    break;

                default:
                    player.Message( "Unknown command \"{0}\"", command );
                    break;
            }
        }


        static void OpsHandler( [NotNull] Player player ) {
            if( !Config.RevealOps && !player.CheckIfOp() ) return;
            if( Server.Ops.Count > 0 ) {
                string[] opNames = Server.Ops.GetCopy();
                Array.Sort( opNames, StringComparer.OrdinalIgnoreCase );
                player.Message( "Ops: {0}", opNames.JoinToString( ", " ) );
            } else {
                player.Message( "There are no ops." );
            }
        }


        static void OpHandler( [NotNull] Player player, [CanBeNull] string targetName ) {
            if( !player.CheckIfOp() || !player.CheckPlayerName( targetName ) ) return;
            if( Server.Ops.Add( targetName ) ) {
                Player target = Server.FindPlayerExact( targetName );
                if( target != null ) {
                    target.IsOp = true;
                    target.Send( Packet.MakeSetPermission( target.CanUseSolid ) );
                    target.Message( "You are now op!" );
                    Server.Players.Message( "Player {0} was opped by {1}",
                                            target.Name, player.Name );
                } else {
                    Server.Players.Message( "Player {0} (offline) was opped by {1}",
                                            targetName, player.Name );
                }
            } else {
                player.Message( "Player {0} is already op", targetName );
            }
        }


        static void DeopHandler( [NotNull] Player player, [CanBeNull] string targetName ) {
            if( !player.CheckIfOp() || !player.CheckPlayerName( targetName ) ) return;
            if( Server.Ops.Remove( targetName ) ) {
                Player target = Server.FindPlayerExact( targetName );
                if( target != null ) {
                    targetName = target.Name;
                    target.IsOp = false;
                    target.PlaceSolid = false;
                    target.PlaceWater = false;
                    target.PlaceLava = false;
                    target.PlaceGrass = false;
                    target.Send( Packet.MakeSetPermission( target.CanUseSolid ) );
                    target.Message( "You are no longer op." );
                    Server.Players.Message( "Player {0} was deopped by {1}",
                                            targetName, player.Name );
                } else {
                    Server.Players.Message( "Player {0} (offline) was deopped by {1}",
                                            targetName, player.Name );
                }
            } else {
                player.Message( "Player {0} is not an op.", targetName );
            }
        }


        static void KickHandler( [NotNull] Player player, [CanBeNull] string targetName ) {
            if( !player.CheckIfOp() || !player.CheckPlayerName( targetName ) ) return;
            Player target = Server.FindPlayer( player, targetName );
            if( target == null ) return;
            target.Kick( "Kicked by " + player.Name );
            Server.Players.Message( "Player {0} was kicked by {1}",
                                    target.Name, player.Name );
        }


        static void BanHandler( [NotNull] Player player, [CanBeNull] string targetName ) {
            if( !player.CheckIfOp() || !player.CheckPlayerName( targetName ) ) return;
            if( Server.Bans.Add( targetName ) ) {
                Player target = Server.FindPlayerExact( targetName );
                if( target != null ) {
                    target.Kick( "Banned by " + player.Name );
                    Server.Players.Message( "Player {0} was banned by {1}",
                                            target.Name, player.Name );
                } else {
                    Server.Players.Message( "Player {0} (offline) was banned by {1}",
                                            targetName, player.Name );
                }
            } else {
                player.Message( "Player {0} is already banned.", targetName );
            }
        }


        static void UnbanHandler( [NotNull] Player player, [CanBeNull] string targetName ) {
            if( !player.CheckIfOp() || !player.CheckPlayerName( targetName ) ) return;
            if( Server.Bans.Remove( targetName ) ) {
                Server.Players.Message( "Player {0} was unbanned by {1}",
                                        targetName, player.Name );
            } else {
                player.Message( "Unban: Player {0} is not banned.", targetName );
            }
        }


        static void BanIPHandler( [NotNull] Player player, [CanBeNull] string targetName ) {
            if( !player.CheckIfOp() ) return;
            if( targetName == null ) {
                player.Message( "BanIP: Player name or IP address required." );
                return;
            }

            IPAddress ip;
            Player target = Server.FindPlayer( player, targetName );
            if( target != null ) {
                ip = target.IP;
                Server.Bans.Add( target.Name );
            } else if( !IPAddress.TryParse( targetName, out ip ) ) {
                player.Message( "BanIP: Player name or IP address required." );
                return;
            }

            if( Server.IPBans.Add( ip ) ) {
                Logger.Log( "IP address {0} was banned by {1}", ip, player.Name );
                var everyoneOnIP = Server.Players.Where( p => p.IP.Equals( ip ) );
                foreach( Player playerOnIP in everyoneOnIP ) {
                    playerOnIP.Kick( "IP-Banned by " + player.Name );
                    Server.Players.Message( "Player {0} was IP-banned by {1}",
                                            playerOnIP.Name, player.Name );
                }
            } else {
                player.Message( "Given IP ({0}) is already banned.", ip );
            }
        }


        static void UnbanIPHandler( [NotNull] Player player, [CanBeNull] string targetName ) {
            if( !player.CheckIfOp() ) return;
            IPAddress ip;
            if( targetName == null || !IPAddress.TryParse( targetName, out ip ) ) {
                player.Message( "UnbanIP: IP address required." );
                return;
            }
            if( Server.IPBans.Remove( ip ) ) {
                Logger.Log( "IP address {0} was unbanned by {1}", ip, player.Name );
                player.Message( "UnbanIP: Unbanned {0}", ip );

            } else {
                player.Message( "Given IP ({0}) is not banned.", ip );
            }
        }


        static void SolidHandler( [NotNull] Player player ) {
            if( !player.CheckIfAllowed( Config.AllowSolidBlocks, Config.OpAllowSolidBlocks ) ) return;
            player.Message( player.PlaceSolid ? "Solid: OFF" : "Solid: ON" );
            player.PlaceSolid = !player.PlaceSolid;
        }


        static void WaterHandler( [NotNull] Player player ) {
            if( !player.CheckIfAllowed( Config.AllowWaterBlocks, Config.OpAllowWaterBlocks ) ) return;
            player.Message( player.PlaceWater ? "Water: OFF" : "Water: ON" );
            player.PlaceWater = !player.PlaceWater;
        }


        static void LavaHandler( [NotNull] Player player ) {
            if( !player.CheckIfAllowed( Config.AllowLavaBlocks, Config.OpAllowLavaBlocks ) ) return;
            player.Message( player.PlaceLava ? "Lava: OFF" : "Lava: ON" );
            player.PlaceLava = !player.PlaceLava;
        }


        static void GrassHandler( [NotNull] Player player ) {
            if( !player.CheckIfAllowed( Config.AllowGrassBlocks, Config.OpAllowGrassBlocks ) ) return;
            player.Message( player.PlaceGrass ? "Grass: OFF" : "Grass: ON" );
            player.PlaceGrass = !player.PlaceGrass;
        }


        static void SayHandler( [NotNull] Player player, [CanBeNull] string message ) {
            if( !player.CheckIfOp() ) return;
            if( message == null ) message = "";
            Server.Players.Message( null, false, "&C" + message );
        }


        static void TeleportHandler( [NotNull] Player player, [CanBeNull] string targetName ) {
            if( !player.CheckIfOp() || !player.CheckPlayerName( targetName ) ) return;
            if( player == Player.Console ) {
                player.Message( "Can't teleport from console!" );
                return;
            }
            Player target = Server.FindPlayer( player, targetName );
            if( target == null ) return;
            player.Send( Packet.MakeSelfTeleport( target.Position ) );
        }


        static void BringHandler( [NotNull] Player player, [CanBeNull] string targetName ) {
            if( !player.CheckIfOp() || !player.CheckPlayerName( targetName ) ) return;
            if( player == Player.Console ) {
                player.Message( "Can't bring from console!" );
                return;
            }
            Player target = Server.FindPlayer( player, targetName );
            if( target == null ) return;
            target.Send( Packet.MakeSelfTeleport( player.Position ) );
        }


        static void SetSpawnHandler( [NotNull] Player player ) {
            if( !player.CheckIfOp() ) return;
            if( player == Player.Console ) {
                player.Message( "Can't set spawn from console!" );
                return;
            }
            player.Map.Spawn = player.Position;
            player.Map.ChangedSinceSave = true;
            player.Send( Packet.MakeAddEntity( 255, player.Name, player.Map.Spawn.GetFixed() ) );
            player.Send( Packet.MakeSelfTeleport( player.Map.Spawn ) );
            Server.Players.Message( "Player {0} set a new spawn point.", player.Name );
        }


        static void WhitelistHandler( [NotNull] Player player ) {
            if( Config.UseWhitelist ) {
                string[] whitelistNames = Server.Whitelist.GetCopy();
                Array.Sort( whitelistNames, StringComparer.OrdinalIgnoreCase );
                player.Message( "Whitelist: {0}", whitelistNames.JoinToString( ", " ) );
            } else {
                player.Message( "Whitelist is disabled." );
            }
        }


        static void WhitelistAddHandler( [NotNull] Player player, [CanBeNull] string targetName ) {
            if( !player.CheckIfOp() || !player.CheckPlayerName( targetName ) ) return;
            if( !Config.UseWhitelist ) {
                player.Message( "Whitelist is disabled." );
                return;
            }
            if( Server.Whitelist.Add( targetName ) ) {
                Server.Players.Message( "Player {0} was whitelisted by {1}",
                                        targetName, player.Name );
            } else {
                player.Message( "Player {0} is already whitelisted.", targetName );
            }
        }


        static void WhitelistRemoveHandler( [NotNull] Player player, [CanBeNull] string targetName ) {
            if( !player.CheckIfOp() || !player.CheckPlayerName( targetName ) ) return;
            if( !Config.UseWhitelist ) {
                player.Message( "Whitelist is disabled." );
                return;
            }
            if( Server.Whitelist.Add( targetName ) ) {
                Player target = Server.FindPlayerExact( targetName );
                if( target != null ) {
                    targetName = target.Name;
                    target.Kick( "Removed from whitelist by " + player.Name );
                }
                Server.Players.Message( "Player {0} was removed from whitelist by {1}",
                                        targetName, player.Name );
            } else {
                player.Message( "Player {0} is not whitelisted.", targetName );
            }
        }


        static void LoadHandler( [NotNull] Player player, [CanBeNull] string fileName ) {
            if( !player.CheckIfOp() ) return;
            if( fileName == null ) {
                player.Message( "Load: Filename required." );
                return;
            }
            try {
                player.MessageNow( "Loading map, please wait..." );
                Map map;
                if( fileName.EndsWith( ".dat", StringComparison.OrdinalIgnoreCase ) ) {
                    map = DatMapConverter.Load( fileName );
                } else if( fileName.EndsWith( ".lvl", StringComparison.OrdinalIgnoreCase ) ) {
                    map = LvlMapConverter.Load( fileName );
                } else {
                    player.Message( "Load: Unsupported map format." );
                    return;
                }
                Server.Players.Message( "Player {0} changed map to {1}",
                                        player.Name, Path.GetFileName( fileName ) );
                Server.ChangeMap( map );
            } catch( Exception ex ) {
                player.Message( "Could not load map: {0}: {1}", ex.GetType().Name, ex.Message );
            }
        }


        static void SaveHandler( [NotNull] Player player, [CanBeNull] string fileName ) {
            if( !player.CheckIfOp() ) return;
            if( fileName == null || !fileName.EndsWith( ".lvl" ) ) {
                player.Message( "Load: Filename must end with .lvl" );
                return;
            }
            try {
                player.Map.Save( fileName );
                player.Message( "Map saved to {0}", Path.GetFileName( fileName ) );
            } catch( Exception ex ) {
                player.Message( "Could not save map: {0}: {1}", ex.GetType().Name, ex.Message );
                Logger.LogError( "Failed to save map: {0}", ex );
            }
        }


        static void PhysicsHandler( [NotNull] Player player, [CanBeNull] string param ) {
            if( !player.CheckIfOp() ) return;
            if( param == null ) param = "";
            switch( param.ToLower() ) {
                case "":
                    // print info
                    player.Message( "Physics are: {0}", Config.Physics ? "ON" : "OFF" );
                    List<string> modules = new List<string>();
                    if( Config.PhysicsGrass ) modules.Add( "grass" );
                    if( Config.PhysicsLava ) modules.Add( "lava" );
                    if( Config.PhysicsPlants ) modules.Add( "plants" );
                    if( Config.PhysicsSand ) modules.Add( "sand" );
                    if( Config.PhysicsTrees ) modules.Add( "trees" );
                    if( Config.PhysicsWater ) modules.Add( "water" );
                    if( modules.Count == 0 ) {
                        player.Message( "None of the modules are enabled." );
                    } else {
                        player.Message( "Following modules are enabled: {0}",
                                        modules.JoinToString( ", " ) );
                    }
                    break;

                case "on":
                    // toggle selected things on
                    if( Config.Physics ) {
                        player.Message( "Physics are already enabled." );
                    } else {
                        player.MessageNow( "Enabling physics, please wait..." );
                        Config.Physics = true;
                        Config.Save();
                        player.Map.EnablePhysics();
                        Logger.Log( "Player {0} enabled physics.",
                                    player.Name );
                        player.Message( "Selected physics modules: ON" );
                    }
                    break;

                case "off":
                    // toggle everything off
                    if( !Config.Physics ) {
                        player.Message( "Physics are already disabled." );
                    } else {
                        Config.Physics = false;
                        Config.Save();
                        player.Map.DisablePhysics();
                        Logger.Log( "Player {0} enabled physics.",
                                    player.Name );
                        player.Message( "All physics: OFF" );
                    }
                    break;

                case "grass":
                    Config.PhysicsGrass = !Config.PhysicsGrass;
                    Config.Save();
                    Logger.Log( "Player {0} turned {1} grass physics.",
                                player.Name, Config.PhysicsGrass ? "on" : "off" );
                    player.Message( "Grass physics: {0}",
                                    Config.PhysicsGrass ? "ON" : "OFF" );
                    break;

                case "lava":
                    Config.PhysicsLava = !Config.PhysicsLava;
                    Config.Save();
                    Logger.Log( "Player {0} turned {1} lava physics.",
                                player.Name, Config.PhysicsLava ? "on" : "off" );
                    player.Message( "Lava physics: {0}",
                                    Config.PhysicsLava ? "ON" : "OFF" );
                    break;

                case "plant":
                case "plants":
                    Config.PhysicsPlants = !Config.PhysicsPlants;
                    Config.Save();
                    Logger.Log( "Player {0} turned {1} plant physics.",
                                player.Name, Config.PhysicsPlants ? "on" : "off" );
                    player.Message( "Plant physics: {0}",
                                    Config.PhysicsPlants ? "ON" : "OFF" );
                    break;

                case "sand":
                    Config.PhysicsSand = !Config.PhysicsSand;
                    Config.Save();
                    Logger.Log( "Player {0} turned {1} sand/gravel physics.",
                                player.Name, Config.PhysicsSand ? "on" : "off" );
                    player.Message( "Sand physics: {0}",
                                    Config.PhysicsSand ? "ON" : "OFF" );
                    break;

                case "tree":
                case "trees":
                    Config.PhysicsTrees = !Config.PhysicsTrees;
                    Config.Save();
                    Logger.Log( "Player {0} turned {1} tree physics.",
                                player.Name, Config.PhysicsTrees ? "on" : "off" );
                    player.Message( "Tree physics: {0}",
                                    Config.PhysicsTrees ? "ON" : "OFF" );
                    break;

                case "water":
                    Config.PhysicsWater = !Config.PhysicsWater;
                    Config.Save();
                    Logger.Log( "Player {0} turned {1} water physics.",
                                player.Name, Config.PhysicsWater ? "on" : "off" );
                    player.Message( "Water physics: {0}",
                                    Config.PhysicsWater ? "ON" : "OFF" );
                    break;

                default:
                    player.Message( "Unknown /physics option \"{0}\"", param );
                    break;
            }
        }


        static void PaintHandler( [NotNull] Player player ) {
            if( player.CheckIfConsole() ) return;
            player.IsPainting = !player.IsPainting;
            player.Message( "Paint: {0}", player.IsPainting ? "ON" : "OFF" );
        }


        public static void PlayersHandler( [NotNull] Player player ) {
            Player[] players = Server.Players;
            Array.Sort( players, ( p1, p2 ) => StringComparer.OrdinalIgnoreCase.Compare( p1.Name, p2.Name ) );
            if( players.Length == 0 ) {
                player.Message( "There are no players online." );
            } else {
                string playerList;
                if( player.IsOp || Config.RevealOps ) {
                    playerList = players.JoinToString( ", ", p => ( p.IsOp ? Config.OpColor : "&F" ) + p.Name );
                } else {
                    playerList = players.JoinToString( ", ", p => p.Name );
                }
                if( players.Length % 10 == 1 ) {
                    player.Message( "There is {0} player online: {1}",
                                    players.Length, playerList );
                } else {
                    player.Message( "There are {0} players online: {1}",
                                    players.Length, playerList );
                }
            }
        }


        static void GenHandler( bool flat, [NotNull] Player player, [CanBeNull] string param ) {
            if( !player.CheckIfOp() ) return;

            string cmdName = ( flat ? "GenFlat" : "Gen" );
            if( String.IsNullOrEmpty( param ) ) {
                PrintGenUsage(cmdName, player );
                return;
            }

            string[] args = param.Split( ' ' );

            ushort width, length, height;
            if( args.Length != 4 ||
                !UInt16.TryParse( args[0], out width ) ||
                !UInt16.TryParse( args[1], out length ) ||
                !UInt16.TryParse( args[2], out height ) ) {
                PrintGenUsage( cmdName, player );
                return;
            }

            if( !IsPowerOfTwo( width ) || !IsPowerOfTwo( length ) || !IsPowerOfTwo( height ) ||
                width < 16 || length < 16 || height < 16 ||
                width > 1024 || length > 1024 || height > 1024 ) {
                    player.Message( "{0}: Map dimensions should be powers-of-2 between 16 and 1024", cmdName );
                    return;
            }

            string fileName = args[3];
            if( !fileName.EndsWith( ".lvl" ) ) {
                player.Message( "Load: Filename must end with .lvl" );
                return;
            }

            player.MessageNow( "Generating a {0}x{1}x{2} map...", width, length, height );
            Map map;
            if( flat ) {
                map = Map.CreateFlatgrass( width, length, height );
            } else {
                map = NotchyMapGenerator.Generate( width, length, height );
            }
            try {
                map.Save( fileName );
                player.Message( "Map saved to {0}", Path.GetFileName( fileName ) );
            } catch( Exception ex ) {
                player.Message( "Could not save map: {0}: {1}", ex.GetType().Name, ex.Message );
                Logger.LogError( "Failed to save map: {0}", ex );
            }
        }

        static bool IsPowerOfTwo( int x ) {
            return ( x != 0 ) && ( ( x & ( x - 1 ) ) == 0 );
        }

        static void PrintGenUsage( string cmdName, Player player ) {
            player.Message( "Usage: /{0} Width Length Height filename.lvl", cmdName );
        }
    }
}