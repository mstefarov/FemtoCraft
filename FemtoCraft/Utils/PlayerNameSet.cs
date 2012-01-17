// Part of FemtoCraft | Copyright 2012 Matvei Stefarov <me@matvei.org> | See LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;

namespace FemtoCraft {
    sealed class PlayerNameSet {
        readonly HashSet<string> names = new HashSet<string>();
        public readonly object SyncRoot = new object();
        public readonly string FileName;


        public PlayerNameSet( [NotNull] string fileName ) {
            FileName = fileName;
            if( !File.Exists( fileName ) ) return;

            foreach( string name in File.ReadAllLines( fileName ) ) {
                if( Player.IsValidName( name ) ) {
                    names.Add( name.ToLower() );
                } else {
                    Logger.LogWarning( "PlayerNameSet ({0}): Invalid player name \"{1}\"",
                                       fileName, name );
                }
            }
        }


        public int Count {
            get { return names.Count; }
        }


        public bool Contains( [NotNull] string name ) {
            return names.Contains( name.ToLower() );
        }


        public bool Add( [NotNull] string name ) {
            return names.Add( name.ToLower() );
        }


        public bool Remove( [NotNull] string name ) {
            return names.Remove( name.ToLower() );
        }


        public void Save() {
            File.WriteAllLines( FileName, names.ToArray() );
        }
    }
}