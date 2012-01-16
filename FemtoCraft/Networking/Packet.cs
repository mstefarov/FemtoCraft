// Part of FemtoCraft | Copyright 2012 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
namespace FemtoCraft {
    struct Packet {
        public Packet( OpCode opCode ) {
            Bytes = new byte[opCode.GetPacketSize()];
            Bytes[0] = (byte)opCode;
        }

        public readonly byte[] Bytes;

        public OpCode OpCode {
            get { return (OpCode)Bytes[0]; }
        }
    }
}
