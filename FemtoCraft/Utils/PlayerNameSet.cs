// Part of FemtoCraft | Copyright 2012 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
using System;
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
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            this.fileName = fileName;
            if( !File.Exists( fileName ) ) {
                File.Create( fileName );
                return;
            }

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
            if( name == null ) throw new ArgumentNullException( "name" );
            lock( syncRoot ) {
                return names.Contains( name.ToLower() );
            }
        }


        public bool Add( [NotNull] string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            lock( syncRoot ) {
                if( names.Add( name.ToLower() ) ) {
                    Save();
                    return true;
                } else {
                    return false;
                }
            }
        }


        public bool Remove( [NotNull] string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            lock( syncRoot ) {
                if( names.Remove( name.ToLower() ) ) {
                    Save();
                    return true;
                } else {
                    return false;
                }
            }
        }


        void Save() {
            lock( syncRoot ) {
                string tempFileName = Path.GetTempFileName();
                File.WriteAllLines( tempFileName, names.ToArray() );
                Util.MoveOrReplaceFile( tempFileName, fileName );
            }
        }


        [NotNull]
        public string[] GetCopy() {
            lock( syncRoot ) {
                return names.ToArray();
            }
        }
    }
}