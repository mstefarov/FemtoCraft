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

        public override short ReadInt16() {
            return IPAddress.NetworkToHostOrder( base.ReadInt16() );
        }

        public override string ReadString() {
            return Encoding.ASCII.GetString( ReadBytes( 64 ) ).Trim();
        }
    }
}