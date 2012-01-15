// Part of FemtoCraft | Copyright 2012 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
using System;
using System.IO;

namespace FemtoCraft {
    static class Logger {
        const string LogFileName = "server.log";
        static readonly object LogLock = new object();


        public static void LogError( string message, params object[] formatArgs ) {
            LogInternal( message, formatArgs, "ERROR " );
        }


        public static void LogWarning( string message, params object[] formatArgs ) {
            LogInternal( message, formatArgs, "Warning " );
        }


        public static void Log( string message, params object[] formatArgs ) {
            LogInternal( message, formatArgs, "" );
        }


        static void LogInternal( string message, object[] formatArgs,  string prefix ) {
            lock( LogLock ) {
                string formattedMsg = String.Format( "{0} {1}> {2}",
                                                     Timestamp(),
                                                     prefix,
                                                     String.Format( message, formatArgs ) );
                if( prefix.Length>0 ) {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Error.WriteLine( formattedMsg );
                    Console.ResetColor();
                } else {
                    Console.WriteLine( formattedMsg );
                }
                File.AppendAllText( LogFileName, formattedMsg + Environment.NewLine );
            }
        }


        static string Timestamp() {
            return DateTime.Now.ToString( "yyyy'-'MM'-'dd' 'HH':'mm':'ss" );
        }
    }
}