// Part of FemtoCraft | Copyright 2012 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
using System.IO;
using System.Net;
using System.Text;

namespace FemtoCraft {
    sealed class PacketWriter : BinaryWriter {
        public const byte ProtocolVersion = 7;


        public PacketWriter( Stream stream ) :
            base( stream ) {}


        public void Write( OpCode value ) {
            Write( (byte)value );
        }


        public override void Write( short value ) {
            base.Write( IPAddress.HostToNetworkOrder( value ) );
        }


        public override void Write( string value ) {
            Write( Encoding.ASCII.GetBytes( value.PadRight( 64 ).Substring( 0, 64 ) ) );
        }


        public void WriteHandshake( bool isOp ) {
            Write( OpCode.Handshake );
            Write( ProtocolVersion );
            Write( Config.ServerName );
            Write( Config.MOTD );
            Write( isOp ? (byte)100 : (byte)0 );
        }


        public static Packet MakeDisconnect( string reason ) {
            Packet packet = new Packet( OpCode.Kick );
            Encoding.ASCII.GetBytes( reason.PadRight( 64 ), 0, 64, packet.Bytes, 1 );
            return packet;
        }
    }
}