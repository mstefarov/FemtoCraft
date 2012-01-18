// Part of FemtoCraft | Copyright 2012 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace FemtoCraft {
    sealed class IPAddressSet {
        readonly HashSet<IPAddress> addresses = new HashSet<IPAddress>();
        readonly object syncRoot = new object();
        readonly string fileName;


        public IPAddressSet( string fileName ) {
            this.fileName = fileName;
            if( !File.Exists( fileName ) ) {
                File.Create( fileName );
                return;
            }

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
            lock( syncRoot ) {
                return addresses.Any( address.Equals );
            }
        }


        public bool Add( IPAddress address ) {
            lock( syncRoot ) {
                IPAddress existingAddress = addresses.FirstOrDefault( address.Equals );
                if( existingAddress == null ) {
                    addresses.Add( address );
                    return true;
                } else {
                    return false;
                }
            }
        }


        public bool Remove( IPAddress address ) {
            lock( syncRoot ) {
                IPAddress existingAddress = addresses.FirstOrDefault( address.Equals );
                if( existingAddress == null ) {
                    return false;
                } else {
                    addresses.Remove( existingAddress );
                    return false;
                }
            }
        }


        public void Save() {
            lock( syncRoot ) {
                File.WriteAllLines( fileName, addresses.Select( a => a.ToString() ).ToArray() );
            }
        }
    }
}