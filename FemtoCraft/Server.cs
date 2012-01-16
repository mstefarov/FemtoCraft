// Part of FemtoCraft | Copyright 2012 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
using System;
using System.Security.Cryptography;
using System.Text;

namespace FemtoCraft {
    static class Server {
        public const string VersionString = "FemtoCraft 0.04";
        public static readonly string Salt = GenerateSalt();

        public static Uri Uri { get; set; }
        public static int PlayerCount { get; set; }

        public static Map Map { get; set; }


        static void Main() {
            Console.Title = VersionString;
            Logger.Log( "Starting {0}", VersionString );
            Config.Load();
            Console.Title = Config.ServerName + " - " + VersionString;
            Heartbeat.Start();
            Console.ReadLine();
        }


        static string GenerateSalt() {
            RandomNumberGenerator prng = RandomNumberGenerator.Create();
            StringBuilder sb = new StringBuilder();
            byte[] oneChar = new byte[1];
            while( sb.Length < 32 ) {
                prng.GetBytes( oneChar );
                if( oneChar[0] >= 48 && oneChar[0] <= 57 ||
                    oneChar[0] >= 65 && oneChar[0] <= 90 ||
                    oneChar[0] >= 97 && oneChar[0] <= 122 ) {
                    sb.Append( (char)oneChar[0] );
                }
            }
            return sb.ToString();
        }
    }
}