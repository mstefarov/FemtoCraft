// Part of FemtoCraft | Copyright 2012 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FemtoCraft {
    sealed class PlayerNameSet {
        readonly HashSet<string> names = new HashSet<string>();

        public PlayerNameSet( string fileName ) {
            FileName = fileName;
            SyncRoot = new object();
            if( !File.Exists( fileName ) ) return;

            foreach( string name in File.ReadLines( fileName ) ) {
                if( Player.IsValidName( name ) ) {
                    names.Add( name.ToLower() );
                } else {
                    Logger.LogWarning( "PlayerNameSet ({0}): Invalid player name \"{1}\"",
                                       fileName, name );
                }
            }
        }


        public readonly object SyncRoot;


        public readonly string FileName;


        public int Count {
            get { return names.Count; }
        }


        public bool Contains( string name ) {
            return names.Contains( name.ToLower() );
        }


        public bool Add( string name ) {
            return names.Add( name.ToLower() );
        }


        public bool Remove( string name ) {
            return names.Remove( name.ToLower() );
        }


        public void Save() {
            File.WriteAllLines( FileName, names.ToArray() );
        }
    }
}
