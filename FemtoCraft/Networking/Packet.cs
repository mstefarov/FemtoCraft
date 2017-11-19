// Part of FemtoCraft | Copyright 2012-2013 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
using System;
using System.Text;
using JetBrains.Annotations;

namespace FemtoCraft {
    partial struct Packet {
        public const byte ProtocolVersion = 7;
        public readonly byte[] Bytes;


        public OpCode OpCode {
            get { return (OpCode)Bytes[0]; }
        }


        public Packet( OpCode opCode ) {
            Bytes = new byte[GetPacketSize( opCode )];
            Bytes[0] = (byte)opCode;
        }


        public Packet( [NotNull] byte[] bytes ) {
            Bytes = bytes;
        }


        #region Packet-Making

        [Pure]
        public static Packet MakeHandshake( bool isOp ) {
            //Logger.Log( "Send: Handshake({0},{1},{2})", Config.ServerName, Config.MOTD, isOp ? (byte)100 : (byte)0 );
            Packet packet = new Packet( OpCode.Handshake );
            packet.Bytes[1] = ProtocolVersion;
            Encoding.ASCII.GetBytes( Config.ServerName.PadRight( 64 ), 0, 64, packet.Bytes, 2 );
            Encoding.ASCII.GetBytes( Config.MOTD.PadRight( 64 ), 0, 64, packet.Bytes, 66 );
            packet.Bytes[130] = isOp ? (byte)100 : (byte)0;
            return packet;
        }


        [Pure]
        public static Packet MakeSetBlock( int x, int y, int z, Block type ) {
            Packet packet = new Packet( OpCode.SetBlockServer );
            ToNetOrder( (short)x, packet.Bytes, 1 );
            ToNetOrder( (short)z, packet.Bytes, 3 );
            ToNetOrder( (short)y, packet.Bytes, 5 );
            packet.Bytes[7] = (byte)type;
            return packet;
        }


        [Pure]
        public static Packet MakeAddEntity( byte id, [NotNull] string name, Position pos ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            Packet packet = new Packet( OpCode.AddEntity );
            packet.Bytes[1] = id;
            Encoding.ASCII.GetBytes( name.PadRight( 64 ), 0, 64, packet.Bytes, 2 );
            ToNetOrder( pos.X, packet.Bytes, 66 );
            ToNetOrder( pos.Z, packet.Bytes, 68 );
            ToNetOrder( pos.Y, packet.Bytes, 70 );
            packet.Bytes[72] = pos.R;
            packet.Bytes[73] = pos.L;
            return packet;
        }


        [Pure]
        public static Packet MakeTeleport( byte id, Position pos ) {
            Packet packet = new Packet( OpCode.Teleport );
            packet.Bytes[1] = id;
            ToNetOrder( pos.X, packet.Bytes, 2 );
            ToNetOrder( pos.Z, packet.Bytes, 4 );
            ToNetOrder( pos.Y, packet.Bytes, 6 );
            packet.Bytes[8] = pos.R;
            packet.Bytes[9] = pos.L;
            return packet;
        }


        [Pure]
        public static Packet MakeSelfTeleport( Position pos ) {
            return MakeTeleport( 255, pos.GetFixed() );
        }


        [Pure]
        public static Packet MakeMoveRotate( int id, Position pos ) {
            Packet packet = new Packet( OpCode.MoveRotate );
            packet.Bytes[1] = (byte)id;
            packet.Bytes[2] = (byte)( pos.X & 0xFF );
            packet.Bytes[3] = (byte)( pos.Z & 0xFF );
            packet.Bytes[4] = (byte)( pos.Y & 0xFF );
            packet.Bytes[5] = pos.R;
            packet.Bytes[6] = pos.L;
            return packet;
        }


        [Pure]
        public static Packet MakeMove( int id, Position pos ) {
            Packet packet = new Packet( OpCode.Move );
            packet.Bytes[1] = (byte)id;
            packet.Bytes[2] = (byte)pos.X;
            packet.Bytes[3] = (byte)pos.Z;
            packet.Bytes[4] = (byte)pos.Y;
            return packet;
        }


        [Pure]
        public static Packet MakeRotate( int id, Position pos ) {
            Packet packet = new Packet( OpCode.Rotate );
            packet.Bytes[1] = (byte)id;
            packet.Bytes[2] = pos.R;
            packet.Bytes[3] = pos.L;
            return packet;
        }


        [Pure]
        public static Packet MakeRemoveEntity( int id ) {
            Packet packet = new Packet( OpCode.RemoveEntity );
            packet.Bytes[1] = (byte)id;
            return packet;
        }


        public static Packet MakeKick( [NotNull] string reason ) {
            if( reason == null ) throw new ArgumentNullException( "reason" );
            Packet packet = new Packet( OpCode.Kick );
            Encoding.ASCII.GetBytes( reason.PadRight( 64 ), 0, 64, packet.Bytes, 1 );
            return packet;
        }


        [Pure]
        public static Packet MakeSetPermission( bool isOp ) {
            Packet packet = new Packet( OpCode.SetPermission );
            if( isOp ) packet.Bytes[1] = 100;
            return packet;
        }

        #endregion


        static void ToNetOrder( short number, [NotNull] byte[] arr, int offset ) {
            if( arr == null ) throw new ArgumentNullException( "arr" );
            arr[offset] = (byte)( ( number & 0xff00 ) >> 8 );
            arr[offset + 1] = (byte)( number & 0x00ff );
        }


        static void ToNetOrder( int number, [NotNull] byte[] arr, int offset ) {
            if( arr == null ) throw new ArgumentNullException( "arr" );
            arr[offset] = (byte)((number & 0xff000000) >> 24);
            arr[offset + 1] = (byte)((number & 0x00ff0000) >> 16);
            arr[offset + 2] = (byte)((number & 0x0000ff00) >> 8);
            arr[offset + 3] = (byte)(number & 0x000000ff);
        }


        static int GetPacketSize( OpCode opCode ) {
            return PacketSizes[(int)opCode];
        }


        static readonly int[] PacketSizes = {
            131,    // Handshake
            1,      // Ping
            1,      // MapBegin
            1028,   // MapChunk
            7,      // MapEnd
            9,      // SetBlockClient
            8,      // SetBlockServer
            74,     // AddEntity
            10,     // Teleport
            7,      // MoveRotate
            5,      // Move
            4,      // Rotate
            2,      // RemoveEntity
            66,     // Message
            65,     // Kick
            2,      // SetPermission

            67,     // ExtInfo
            69,     // ExtEntry
            0,
            2,      // CustomBlockSupportLevel (#19)
            0,0,0,0,0,0,0,0,
            4,      // SetBlockPermission (#28)
            0,0,0,0,0,0,0,0,0,0,0,0,0,0,
            4       // TwoWayPing (#43)
        };
    }
}
