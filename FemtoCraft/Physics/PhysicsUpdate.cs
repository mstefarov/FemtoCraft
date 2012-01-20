using System.Runtime.InteropServices;

namespace FemtoCraft {
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    struct PhysicsUpdate {
        public PhysicsUpdate( int x, int y, int z, Block oldBlock, byte delay ) {
            X = (short)x;
            Y = (short)y;
            Z = (short)z;
            OldBlock = oldBlock;
            Delay = delay;
        }

        public short X, Y, Z;
        public Block OldBlock;
        public byte Delay;
    }
}