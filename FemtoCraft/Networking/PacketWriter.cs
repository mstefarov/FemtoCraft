// Part of FemtoCraft | Copyright 2012 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
using System;
using System.IO;
using System.Net;
using System.Text;

namespace FemtoCraft {
    sealed class PacketWriter : BinaryWriter {
        public const byte ProtocolVersion = 7;

        public PacketWriter( Stream stream ) :
            base( stream ) { }

        public void Write( OpCode value ) {
            Write( (byte)value );
        }

        public void Write( Packet packet ) {
            Write( packet.Bytes );
        }

        public void WriteBE( short value ) {
            Write( IPAddress.HostToNetworkOrder( value ) );
        }

        public void WriteBE( int value ) {
            Write( IPAddress.HostToNetworkOrder( value ) );
        }

        public void WriteMCString( string value ) {
            if( value == null ) throw new ArgumentNullException( "value" );
            Write( Encoding.ASCII.GetBytes( value.PadRight( 64 ).Substring( 0, 64 ) ) );
        }


        public void WriteAddEntity( byte id, string name, Position position ) {
            Write( OpCode.AddEntity );
            Write( id );
            WriteMCString( name );
            WriteBE( position.X );
            WriteBE( position.Z );
            WriteBE( position.Y );
            Write( position.R );
            Write( position.L );
        }


        public void WriteSetBlock( short x, short y, short z, Block block ) {
            Write( OpCode.SetBlockServer );
            WriteBE( x );
            WriteBE( z );
            WriteBE( y );
            Write( (byte)block );
        }


        public void WriteTeleport( byte id, Position position ) {
            Write( OpCode.AddEntity );
            Write( id );
            WriteBE( position.X );
            WriteBE( position.Z );
            WriteBE( position.Y );
            Write( position.R );
            Write( position.L );
        }


        public static Packet MakeDisconnect( string reason ) {
            Packet packet = new Packet( OpCode.Kick );
            Encoding.ASCII.GetBytes( reason.PadRight( 64 ), 0, 64, packet.Bytes, 1 );
            return packet;
        }
    }
}
