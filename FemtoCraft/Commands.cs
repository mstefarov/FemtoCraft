// Part of FemtoCraft | Copyright 2012 Matvei Stefarov <me@matvei.org> | See LICENSE.txt

using System;
using System.Linq;
using JetBrains.Annotations;

namespace FemtoCraft {
    static class Commands {
        public static void Parse( [NotNull] Player player, [NotNull] string message ) {
            string command, param;
            int spaceIndex = message.IndexOf( ' ' );
            if( spaceIndex == -1 ) {
                command = message.Substring( 1 ).ToLower();
                param = null;
            } else {
                command = message.Substring( 1, spaceIndex - 1 ).ToLower();
                param = message.Substring( spaceIndex + 1 ).Trim();
            }

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
                    SolidHandler( player );
                    break;
                case "water":
                    WaterHandler( player );
                    break;
                case "lava":
                    LavaHandler( player );
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

                default:
                    player.Message( "Unknown command \"{0}\"", command );
                    break;
            }
        }


        static void OpsHandler( Player player ) {
            if( Server.Ops.Count > 0 ) {
                player.Message( "Ops: {0}", Server.Ops.GetCopy().JoinToString( ", " ) );
            } else {
                player.Message( "There are no ops." );
            }
        }


        static void OpHandler( Player player, string targetName ) {
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


        static void DeopHandler( Player player, string targetName ) {
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
                    target.Send( Packet.MakeSetPermission( target.IsOp ) );
                    target.Message( "You are no longer op." );
                }
                Server.Players.Message( "Player {0} was demoted by {1}",
                                        targetName, player.Name );
            } else {
                player.Message( "Player {0} is not an op.", targetName );
            }
        }


        static void KickHandler( Player player, string targetName ) {
            if( !player.CheckIfOp() || !player.CheckPlayerName( targetName ) ) return;
            Player target = Server.FindPlayer( player, targetName );
            if( target == null ) return;
            target.Kick( "Kicked by " + player.Name );
        }


        static void BanHandler( Player player, string targetName ) {
            if( !player.CheckIfOp() || !player.CheckPlayerName( targetName ) ) return;
            if( Server.Bans.Add( targetName ) ) {
                Server.Bans.Save();
                Player target = Server.FindPlayerExact( targetName );
                if( target != null ) {
                    targetName = target.Name;
                    target.Kick( "Banned by " + player.Name );
                }
                Server.Players.Message( "Player {0} was banned by {1}",
                                        targetName, player.Name );
            } else {
                player.Message( "Player {0} is already banned.", targetName );
            }
        }


        static void UnbanHandler( Player player, string targetName ) {
            if( !player.CheckIfOp() || !player.CheckPlayerName( targetName ) ) return;
            if( Server.Bans.Remove( targetName ) ) {
                Server.Bans.Save();
                Server.Players.Message( "Player {0} was unbanned by {1}",
                                        targetName, player.Name );
            } else {
                player.Message( "Player {0} is not banned.", targetName );
            }
        }


        static void BanIPHandler( Player player, string targetName ) {
            if( !player.CheckIfOp() || !player.CheckPlayerName( targetName ) ) return;
            Player target = Server.FindPlayer( player, targetName );
            if( target == null ) return;

            target.Kick( "Banned by " + player.Name );
            Server.Bans.Add( target.Name );
            Server.IPBans.Add( target.IP );
            Server.IPBans.Save();
            Server.Players.Message( "Player {0} was IP-banned by {1}",
                                    target.Name, player.Name );
            var everyoneOnIP = Server.Players.Where( p => p.IP.Equals( target.IP ) );
            foreach( Player playerOnIP in everyoneOnIP ) {
                playerOnIP.Kick( "IP-Banned by " + player.Name );
            }
        }


        static void SolidHandler( Player player ) {
            if( !player.CheckIfOp() ) return;
            player.Message( player.PlaceSolid ? "Solid: Off" : "Solid: On" );
            player.PlaceSolid = !player.PlaceSolid;
        }


        static void WaterHandler( Player player ) {
            if( !player.CheckIfOp() ) return;
            player.Message( player.PlaceWater ? "Water: Off" : "Water: On" );
            player.PlaceWater = !player.PlaceWater;
        }


        static void LavaHandler( Player player ) {
            if( !player.CheckIfOp() ) return;
            player.Message( player.PlaceLava ? "Lava: Off" : "Lava: On" );
            player.PlaceLava = !player.PlaceLava;
        }


        static void SayHandler( Player player, string message ) {
            if( player.CheckIfOp() ) {
                Server.Players.Message( message );
            }
        }


        static void TeleportHandler( Player player, string targetName ) {
            if( !player.CheckIfOp() || !player.CheckPlayerName( targetName ) ) return;
            if( player == Player.Console ) {
                player.Message( "Can't teleport from console!" );
                return;
            }
            Player target = Server.FindPlayer( player, targetName );
            if( target == null ) return;
            player.Send( Packet.MakeSelfTeleport( target.Position ) );
        }


        static void BringHandler( Player player, string targetName ) {
            if( !player.CheckIfOp() || !player.CheckPlayerName( targetName ) ) return;
            if( player == Player.Console ) {
                player.Message( "Can't bring from console!" );
                return;
            }
            Player target = Server.FindPlayer( player, targetName );
            if( target == null ) return;
            target.Send( Packet.MakeSelfTeleport( player.Position ) );
        }


        static void SetSpawnHandler( Player player ) {
            if( !player.CheckIfOp() ) return;
            if( player == Player.Console ) {
                player.Message( "Can't set spawn from console!" );
                return;
            }
            Server.Map.Spawn = player.Position;
            player.Send( Packet.MakeAddEntity( 255, player.Name, Server.Map.Spawn ) );
            Server.Players.Message( "Player {0} set a new spawn point.", player.Name );
        }


        static void WhitelistHandler( Player player ) {
            if( Config.UseWhitelist ) {
                player.Message( "Whitelist: {0}", Server.Whitelist.GetCopy().JoinToString( ", " ) );
            } else {
                player.Message( "Whitelist is disabled." );
            }
        }


        static void WhitelistAddHandler( Player player, string targetName ) {
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


        static void WhitelistRemoveHandler( Player player, string targetName ) {
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
    }
}