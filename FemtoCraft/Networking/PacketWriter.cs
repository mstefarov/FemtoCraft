// Part of FemtoCraft | Copyright 2012 Matvei Stefarov <me@matvei.org> | See LICENSE.txt

using System;
using System.IO;
using System.Net;
using System.Text;
using JetBrains.Annotations;

namespace FemtoCraft {
    sealed class PacketWriter : BinaryWriter {
        public const byte ProtocolVersion = 7;


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


        public void WriteHandshake( bool isOp ) {
            Write( OpCode.Handshake );
            Write( ProtocolVersion );
            Write( Config.ServerName );
            Write( Config.MOTD );
            Write( isOp ? (byte)100 : (byte)0 );
        }


        public static Packet MakeDisconnect( [NotNull] string reason ) {
            if( reason == null ) throw new ArgumentNullException( "reason" );
            Packet packet = new Packet( OpCode.Kick );
            Encoding.ASCII.GetBytes( reason.PadRight( 64 ), 0, 64, packet.Bytes, 1 );
            return packet;
        }
    }
}