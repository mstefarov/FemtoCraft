// Part of FemtoCraft | Copyright 2012 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
using System;
using System.IO;
using System.Net;
using System.Text;
using JetBrains.Annotations;

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


        public override void Write( [NotNull] string value ) {
            Write( Encoding.ASCII.GetBytes( value.PadRight( 64 ).Substring( 0, 64 ) ) );
        }


        public void WriteHandshake( bool isOp ) {
            Write( OpCode.Handshake );
            Write( ProtocolVersion );
            Write( Config.ServerName );
            Write( Config.MOTD );
            Write( isOp ? (byte)100 : (byte)0 );
        }


        public void WriteAddEntity( byte id, string name, Position position ) {
            Write( OpCode.AddEntity );
            Write( id );
            Write( name );
            Write( position.X );
            Write( position.Z );
            Write( position.Y );
            Write( position.R );
            Write( position.L );
        }


        public void WriteTeleport( byte id, Position position ) {
            Write( OpCode.AddEntity );
            Write( id );
            Write( position.X );
            Write( position.Z );
            Write( position.Y );
            Write( position.R );
            Write( position.L );
        }


        public void WriteSetBlock( short x, short y, short z, Block block ) {
            Write( OpCode.SetBlockServer );
            Write( x );
            Write( z );
            Write( y );
            Write( (byte)block );
        }


        public static Packet MakeDisconnect( string reason ) {
            Packet packet = new Packet( OpCode.Kick );
            Encoding.ASCII.GetBytes( reason.PadRight( 64 ), 0, 64, packet.Bytes, 1 );
            return packet;
        }
    }
}