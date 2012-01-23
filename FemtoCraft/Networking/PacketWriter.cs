// Part of FemtoCraft | Copyright 2012 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
using System;
using System.IO;
using System.Net;
using System.Text;
using JetBrains.Annotations;

namespace FemtoCraft {
    sealed class PacketWriter : BinaryWriter {
        public PacketWriter( [NotNull] Stream stream ) :
            base( stream ) {}


        public void Write( OpCode value ) {
            Write( (byte)value );
        }


        public override void Write( short value ) {
            base.Write( IPAddress.HostToNetworkOrder( value ) );
        }


        public override void Write( string value ) {
            if( value == null ) throw new ArgumentNullException( "value" );
            Write( Encoding.ASCII.GetBytes( value.PadRight( 64 ).Substring( 0, 64 ) ) );
        }
    }
}