// Part of FemtoCraft | Copyright 2012 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
using System;
using System.Collections.Generic;
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

                default:
                    player.Message( "Unknown command \"{0}\"", command );
                    break;
            }
        }


        static void OpsHandler( [NotNull] Player player ) {
            if( Server.Ops.Count > 0 ) {
                player.Message( "Ops: {0}", Server.Ops.GetCopy().JoinToString( ", " ) );
            } else {
                player.Message( "There are no ops." );
            }
        }


        static void OpHandler( [NotNull] Player player, [CanBeNull] string targetName ) {
            if( !player.CheckIfOp() || !player.CheckPlayerName( targetName ) ) return;
            if( Server.Ops.Add( targetName ) ) {
                Server.Ops.Save();
                Player target = Server.FindPlayerExact( targetName );
                if( target != null ) {
                    targetName = target.Name;
                    target.IsOp = true;
                    target.Send( Packet.MakeSetPermission( target.IsOp ) );
                    target.Message( "You are now op!" );
                }
                Server.Players.Message( "Player {0} was promoted by {1}",
                                        targetName, player.Name );
            } else {
                player.Message( "Player {0} is already op", targetName );
            }
        }


        static void DeopHandler( [NotNull] Player player, [CanBeNull] string targetName ) {
            if( !player.CheckIfOp() || !player.CheckPlayerName( targetName ) ) return;
            if( Server.Ops.Remove( targetName ) ) {
                Server.Ops.Save();
                Player target = Server.FindPlayerExact( targetName );
                if( target != null ) {
                    targetName = target.Name;
                    target.IsOp = false;
                    target.PlaceSolid = false;
                    target.PlaceWater = false;
                    target.PlaceLava = false;
                    target.PlaceGrass = false;
                    target.Send( Packet.MakeSetPermission( target.IsOp ) );
                    target.Message( "You are no longer op." );
                }
                Server.Players.Message( "Player {0} was demoted by {1}",
                                        targetName, player.Name );
            } else {
                player.Message( "Player {0} is not an op.", targetName );
            }
        }


        static void KickHandler( [NotNull] Player player, [CanBeNull] string targetName ) {
            if( !player.CheckIfOp() || !player.CheckPlayerName( targetName ) ) return;
            Player target = Server.FindPlayer( player, targetName );
            if( target == null ) return;
            target.Kick( "Kicked by " + player.Name );
        }


        static void BanHandler( [NotNull] Player player, [CanBeNull] string targetName ) {
            if( !player.CheckIfOp() || !player.CheckPlayerName( targetName ) ) return;
            if( Server.Bans.Add( targetName ) ) {
                Server.Bans.Save();
                Player target = Server.FindPlayerExact( targetName );
                if( target != null ) {
                    targetName = target.Name;
                    target.Kick( "Banned by " + player.Name );
                }
                Logger.Log( "Player {0} banned {1}", player.Name, targetName );
                Server.Players.Message( "Player {0} was banned by {1}",
                                        targetName, player.Name );
            } else {
                player.Message( "Player {0} is already banned.", targetName );
            }
        }


        static void UnbanHandler( [NotNull] Player player, [CanBeNull] string targetName ) {
            if( !player.CheckIfOp() || !player.CheckPlayerName( targetName ) ) return;
            if( Server.Bans.Remove( targetName ) ) {
                Server.Bans.Save();
                Logger.Log( "Player {0} unbanned {1}", player.Name, targetName );
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
                player.CheckPlayerName( targetName );
                ip = target.IP;
                Server.Bans.Add( target.Name );
                Server.Bans.Save();
            } else if( !IPAddress.TryParse( targetName, out ip ) ) {
                player.Message( "BanIP: Player name or IP address required." );
                return;
            }

            if( Server.IPBans.Add( ip ) ) {
                Server.IPBans.Save();
                Logger.Log( "Player {0} banned {1}", player.Name, ip );
                var everyoneOnIP = Server.Players.Where( p => p.IP.Equals( ip ) );
                foreach( Player playerOnIP in everyoneOnIP ) {
                    Server.Bans.Add( playerOnIP.Name );
                    playerOnIP.Kick( "IP-Banned by " + player.Name );
                    Server.Players.Message( "Player {0} was IP-banned by {1}",
                                            playerOnIP.Name, player.Name );
                }
                Server.Bans.Save();
            } else {
                player.Message( "Given IP ({0}) is already banned.", ip );
            }
        }


        static void SolidHandler( [NotNull] Player player ) {
            if( !player.CheckIfOp() ) return;
            player.Message( player.PlaceSolid ? "Solid: OFF" : "Solid: ON" );
            player.PlaceSolid = !player.PlaceSolid;
        }


        static void WaterHandler( [NotNull] Player player ) {
            if( !player.CheckIfOp() ) return;
            player.Message( player.PlaceWater ? "Water: OFF" : "Water: ON" );
            player.PlaceWater = !player.PlaceWater;
        }


        static void LavaHandler( [NotNull] Player player ) {
            if( !player.CheckIfOp() ) return;
            player.Message( player.PlaceLava ? "Lava: OFF" : "Lava: ON" );
            player.PlaceLava = !player.PlaceLava;
        }


        static void GrassHandler( [NotNull] Player player ) {
            if( !player.CheckIfOp() ) return;
            player.Message( player.PlaceGrass ? "Grass: OFF" : "Grass: ON" );
            player.PlaceGrass = !player.PlaceGrass;
        }


        static void SayHandler( [NotNull] Player player, [CanBeNull] string message ) {
            if( player.CheckIfOp() ) {
                if( message == null ) message = "";
                Server.Players.Message( message );
            }
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
            player.Send( Packet.MakeAddEntity( 255, player.Name, player.Map.Spawn.GetFixed() ) );
            Server.Players.Message( "Player {0} set a new spawn point.", player.Name );
        }


        static void WhitelistHandler( [NotNull] Player player ) {
            if( Config.UseWhitelist ) {
                player.Message( "Whitelist: {0}", Server.Whitelist.GetCopy().JoinToString( ", " ) );
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
                Server.Whitelist.Save();
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
                Server.Whitelist.Save();
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
                }else {
                    player.Message( "Load: Unsupported map format." );
                    return;
                }
                Server.Players.Message( "Player {0} changed map to {1}", player.Name, fileName );
                Server.ChangeMap( map );
            } catch( Exception ex ) {
                player.Message( "Could not load map: {0}: {1}", ex.GetType().Name, ex.Message );
            }
        }


        static void SaveHandler( [NotNull] Player player, [CanBeNull] string fileName ) {
            if( !player.CheckIfOp() ) return;
            if( fileName == null || !fileName.EndsWith(".lvl") ) {
                player.Message( "Load: Filename that ends with .lvl is required." );
                return;
            }
            try {
                player.Map.Save( fileName );
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
                        player.Map.EnablePhysics();
                        Logger.Log( "Player {0} enabled physics.",
                                    player.Name );
                        player.Message( "Selected physics modules: ON" );
                    }
                    break;

                case "off":
                    // toggle everything off
                    if( Config.Physics ) {
                        player.Message( "Physics are already enabled." );
                    } else {
                        Config.Physics = false;
                        player.Map.DisablePhysics();
                        Logger.Log( "Player {0} enabled physics.",
                                    player.Name );
                        player.Message( "All physics: OFF" );
                    }
                    break;

                case "grass":
                    Config.PhysicsGrass = !Config.PhysicsGrass;
                    Logger.Log( "Player {0} turned {1} grass physics.",
                                player.Name, Config.PhysicsGrass ? "on" : "off" );
                    player.Message( "Grass physics  : {0}",
                                    Config.PhysicsGrass ? "ON" : "OFF" );
                    break;

                case "plant":
                case "plants":
                    Config.PhysicsPlants = !Config.PhysicsPlants;
                    Logger.Log( "Player {0} turned {1} plant physics.",
                                player.Name, Config.PhysicsPlants ? "on" : "off" );
                    player.Message( "Plant physics  : {0}",
                                    Config.PhysicsPlants ? "ON" : "OFF" );
                    break;

                case "sand":
                    Config.PhysicsSand = !Config.PhysicsSand;
                    Logger.Log( "Player {0} turned {1} sand/gravel physics.",
                                player.Name, Config.PhysicsSand ? "on" : "off" );
                    player.Message( "Sand physics  : {0}",
                                    Config.PhysicsSand ? "ON" : "OFF" );
                    break;

                case "tree":
                case "trees":
                    Config.PhysicsTrees = !Config.PhysicsTrees;
                    Logger.Log( "Player {0} turned {1} tree physics.",
                                player.Name, Config.PhysicsTrees ? "on" : "off" );
                    player.Message( "Tree physics  : {0}",
                                    Config.PhysicsTrees ? "ON" : "OFF" );
                    break;

                case "water":
                    Config.PhysicsWater = !Config.PhysicsWater;
                    Logger.Log( "Player {0} turned {1} water physics.",
                                player.Name, Config.PhysicsWater ? "on" : "off" );
                    player.Message( "Water physics  : {0}",
                                    Config.PhysicsWater ? "ON" : "OFF" );
                    break;

                case "lava":
                    Config.PhysicsLava = !Config.PhysicsLava;
                    Logger.Log( "Player {0} turned {1} lava physics.",
                                player.Name, Config.PhysicsLava ? "on" : "off" );
                    player.Message( "Lava physics  : {0}",
                                    Config.PhysicsLava ? "ON" : "OFF" );
                    break;
            }
        }


        static void PaintHandler( [NotNull] Player player ) {
            if( player == Player.Console ) {
                player.Message( "Can't paint from console!" );
                return;
            }
            player.IsPainting = !player.IsPainting;
            player.Message( "Paint: {0}", player.IsPainting ? "ON" : "OFF" );
        }
    }
}