// Part of FemtoCraft | Copyright 2012 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
using System.Collections.Generic;
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
                case "op":
                    OpHandler( player, param );
                    break;

                case "deop":
                    DeopHandler( player, param );
                    break;

                case "kick":
                    KickHandler( player, param );
                    break;

                case "ban":
                    BanHandler( player, param );
                    break;

                case "unban":
                    UnbanHandler( player, param );
                    break;
            }
        }


        static void OpHandler( Player player, string targetName ) {
            if( !player.CheckIfOp() || !player.CheckPlayerName( targetName ) ) return;

            if( Server.Ops.Add( targetName ) ) {
                Player target = Server.FindPlayerExact( targetName );
                if( target != null ) {
                    targetName = target.Name;
                    target.IsOp = true;
                    target.Message( "You are now op!" );
                }
                Server.Players.Message( null, "Player {0} was promoted by {1}",
                                        targetName, player.Name );
            } else {
                player.Message( "Player {0} is already op", targetName );
            }
        }


        static void DeopHandler( Player player, string targetName ) {
            if( !player.CheckIfOp() || !player.CheckPlayerName( targetName ) ) return;

            if( Server.Ops.Remove( targetName ) ) {
                Player target = Server.FindPlayerExact( targetName );
                if( target != null ) {
                    targetName = target.Name;
                    target.IsOp = false;
                    target.Message( "You are no longer op." );
                }
                Server.Players.Message( null, "Player {0} was demoted by {1}",
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
                Player target = Server.FindPlayerExact( targetName );
                if( target != null ) {
                    targetName = target.Name;
                    target.Kick( "Banned by " + player.Name );
                }
                Server.Players.Message( null, "Player {0} was banned by {1}",
                                        targetName, player.Name );
            } else {
                player.Message( "Player {0} is already banned.", targetName );
            }
        }

        static void UnbanHandler( Player player, string targetName ) {
            if( !player.CheckIfOp() || !player.CheckPlayerName( targetName ) ) return;

            if( Server.Bans.Remove( targetName ) ) {
                Server.Players.Message( null, "Player {0} was unbanned by {1}",
                                        targetName, player.Name );
            } else {
                player.Message( "Player {0} is not banned.", targetName );
            }
        }
    }
}