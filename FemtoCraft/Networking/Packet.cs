// Part of FemtoCraft | Copyright 2012 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
using System.Text;
using JetBrains.Annotations;

namespace FemtoCraft {
    struct Packet {
        public readonly byte[] Bytes;


        public Packet( OpCode opCode ) {
            Bytes = new byte[GetPacketSize( opCode )];
            Bytes[0] = (byte)opCode;
        }


        public Packet( byte[] bytes ) {
            Bytes = bytes;
        }


        public OpCode OpCode {
            get { return (OpCode)Bytes[0]; }
        }


        #region Packet-Making

        public static Packet MakeSetBlock( int x, int y, int z, Block type ) {
            Packet packet = new Packet( OpCode.SetBlockServer );
            ToNetOrder( x, packet.Bytes, 1 );
            ToNetOrder( z, packet.Bytes, 3 );
            ToNetOrder( y, packet.Bytes, 5 );
            packet.Bytes[7] = (byte)type;
            return packet;
        }


        public static Packet MakeSelfTeleport( Position pos ) {
            return MakeTeleport( 255, pos.GetFixed() );
        }


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


        public static Packet MakeSetPermission( bool isOp ) {
            Packet packet = new Packet( OpCode.SetPermission );
            if( isOp ) packet.Bytes[1] = 100;
            return packet;
        }


        public static Packet MakeAddEntity( byte id, [NotNull] string name, Position pos ) {
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


        public static Packet MakeMove( int id, Position pos ) {
            Packet packet = new Packet( OpCode.Move );
            packet.Bytes[1] = (byte)id;
            packet.Bytes[2] = (byte)pos.X;
            packet.Bytes[3] = (byte)pos.Z;
            packet.Bytes[4] = (byte)pos.Y;
            return packet;
        }


        public static Packet MakeRotate( int id, Position pos ) {
            Packet packet = new Packet( OpCode.Rotate );
            packet.Bytes[1] = (byte)id;
            packet.Bytes[2] = pos.R;
            packet.Bytes[3] = pos.L;
            return packet;
        }


        public static Packet MakeRemoveEntity( int id ) {
            Packet packet = new Packet( OpCode.RemoveEntity );
            packet.Bytes[1] = (byte)id;
            return packet;
        }

        #endregion


        static void ToNetOrder( int number, byte[] arr, int offset ) {
            arr[offset] = (byte)( ( number & 0xff00 ) >> 8 );
            arr[offset + 1] = (byte)( number & 0x00ff );
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
            2       // SetPermission
        };
    }
}
