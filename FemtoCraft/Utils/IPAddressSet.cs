// Part of FemtoCraft | Copyright 2012 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace FemtoCraft {
    sealed class IPAddressSet {
        readonly HashSet<IPAddress> addresses = new HashSet<IPAddress>();
        public readonly object SyncRoot = new object();
        public readonly string FileName;


        public IPAddressSet( string fileName ) {
            FileName = fileName;
            if( !File.Exists( fileName ) ) return;

            foreach( string name in File.ReadAllLines( fileName ) ) {
                IPAddress address;
                if( IPAddress.TryParse( name, out address ) ) {
                    addresses.Add( address );
                } else {
                    Logger.LogWarning( "IPAddressSet ({0}): Could not parse \"{1}\" as an IP address.",
                                       fileName, address );
                }
            }
        }


        public int Count {
            get { return addresses.Count; }
        }


        public bool Contains( IPAddress address ) {
            return addresses.Any( address.Equals );
        }


        public bool Add( IPAddress address ) {
            IPAddress existingAddress = addresses.FirstOrDefault( address.Equals );
            if( existingAddress == null ) {
                addresses.Add( address );
                return true;
            } else {
                return false;
            }
        }


        public bool Remove( IPAddress address ) {
            IPAddress existingAddress = addresses.FirstOrDefault( address.Equals );
            if( existingAddress == null ) {
                return false;
            } else {
                addresses.Remove( existingAddress );
                return false;
            }
        }


        public void Save() {
            File.WriteAllLines( FileName, addresses.Select( a => a.ToString() ).ToArray() );
        }
    }
}