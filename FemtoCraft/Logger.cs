// Part of FemtoCraft | Copyright 2012-2013 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
using System;
using System.IO;
using JetBrains.Annotations;

namespace FemtoCraft {
    static class Logger {
        const string LogFileName = "server.log";
        static readonly object LogLock = new object();


        [StringFormatMethod( "message" )]
        public static void LogError( [NotNull] string message, [NotNull] params object[] formatArgs ) {
            LogInternal( message, formatArgs, "ERROR ", true );
        }


        [StringFormatMethod( "message" )]
        public static void LogWarning( [NotNull] string message, [NotNull] params object[] formatArgs ) {
            LogInternal( message, formatArgs, "Warning ", true );
        }


        [StringFormatMethod( "message" )]
        public static void LogChat( [NotNull] string message, [NotNull] params object[] formatArgs ) {
            LogInternal( message, formatArgs, "Chat ", false );
        }


        [StringFormatMethod( "message" )]
        public static void LogCommand( [NotNull] string message, [NotNull] params object[] formatArgs ) {
            LogInternal( message, formatArgs, "Command ", false );
        }


        [StringFormatMethod( "message" )]
        public static void Log( [NotNull] string message, [NotNull] params object[] formatArgs ) {
            LogInternal( message, formatArgs, "", false );
        }


        static void LogInternal( [NotNull] string message, [NotNull] object[] formatArgs, [NotNull] string prefix,
                                 bool error ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            if( formatArgs == null ) throw new ArgumentNullException( "formatArgs" );
            if( prefix == null ) throw new ArgumentNullException( "prefix" );
            lock( LogLock ) {
                string consoleMessage = String.Format( "{0} {1}> {2}",
                                                       ConsoleTimestamp(),
                                                       prefix,
                                                       String.Format( message, formatArgs ) );
                if( error ) {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Error.WriteLine( consoleMessage );
                    Console.ResetColor();
                } else {
                    Console.WriteLine( consoleMessage );
                }
                string fileMessage = String.Format( "{0} {1}> {2}{3}",
                                                    FileTimestamp(),
                                                    prefix,
                                                    String.Format( message, formatArgs ),
                                                    Environment.NewLine );
                File.AppendAllText( LogFileName, fileMessage );
            }
        }


        [NotNull,Pure]
        static string FileTimestamp() {
            return DateTime.Now.ToString( "yyyy'-'MM'-'dd' 'HH':'mm':'ss" );
        }


        [NotNull,Pure]
        static string ConsoleTimestamp() {
            return DateTime.Now.ToString( "HH':'mm':'ss" );
        }
    }
}