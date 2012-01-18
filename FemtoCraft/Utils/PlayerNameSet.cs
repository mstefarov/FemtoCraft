// Part of FemtoCraft | Copyright 2012 Matvei Stefarov <me@matvei.org> | See LICENSE.txt

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;

namespace FemtoCraft {
    sealed class PlayerNameSet {
        readonly HashSet<string> names = new HashSet<string>();
        readonly object syncRoot = new object();
        readonly string fileName;


        public PlayerNameSet( [NotNull] string fileName ) {
            this.fileName = fileName;
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
            lock( syncRoot ) {
                return names.Contains( name.ToLower() );
            }
        }


        public bool Add( [NotNull] string name ) {
            lock( syncRoot ) {
                return names.Add( name.ToLower() );
            }
        }


        public bool Remove( [NotNull] string name ) {
            lock( syncRoot ) {
                return names.Remove( name.ToLower() );
            }
        }


        public void Save() {
            lock( syncRoot ) {
                File.WriteAllLines( fileName, names.ToArray() );
            }
        }


        public string[] GetCopy() {
            lock( syncRoot ) {
                return names.ToArray();
            }
        }
    }
}