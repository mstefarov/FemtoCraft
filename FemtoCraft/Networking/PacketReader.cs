// Part of FemtoCraft | Copyright 2012 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
using System.IO;
using System.Net;
using System.Text;

namespace FemtoCraft {
    sealed class PacketReader : BinaryReader {
        public PacketReader( Stream stream ) :
            base( stream ) { }

        public OpCode ReadOpCode() {
            return (OpCode)ReadByte();
        }

        public short ReadInt16BE() {
            return IPAddress.NetworkToHostOrder( ReadInt16() );
        }

        public int ReadInt32BE() {
            return IPAddress.NetworkToHostOrder( ReadInt32() );
        }

        public string ReadMCString() {
            return Encoding.ASCII.GetString( ReadBytes( 64 ) ).Trim();
        }
    }
}
