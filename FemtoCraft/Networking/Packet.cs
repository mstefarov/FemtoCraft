// Part of FemtoCraft | Copyright 2012 Matvei Stefarov <me@matvei.org> | See LICENSE.txt

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

        static int GetPacketSize( OpCode opCode ) {
            return PacketSizes[(int)opCode];
        }
    }
}
