// Part of FemtoCraft | Copyright 2012-2013 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
// Based on fCraft.Position - fCraft is Copyright 2009-2012 Matvei Stefarov <me@matvei.org> | See LICENSE.fCraft.txt
using System;

namespace FemtoCraft {
    public struct Position {
        public short X, Y, Z;
        public byte R, L;

        public Position( int x, int y, int z ) {
            X = (short)x;
            Y = (short)y;
            Z = (short)z;
            R = 0;
            L = 0;
        }


        // Adjust for bugs in position-reporting in Minecraft client by offsetting Z by -22 units.
        public Position GetFixed() {
            return new Position {
                X = X,
                Y = Y,
                Z = (short)( Z - 22 ),
                R = R,
                L = L
            };
        }


        public bool FitsIntoMoveRotatePacket {
            get {
                return X >= SByte.MinValue && X <= SByte.MaxValue &&
                       Y >= SByte.MinValue && Y <= SByte.MaxValue &&
                       Z >= SByte.MinValue && Z <= SByte.MaxValue;
            }
        }
    }
}