// Based on fCraft.Position
// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FemtoCraft {
    public struct Position {
        public readonly static Position Zero = new Position( 0, 0, 0 );

        public short X, Y, Z;
        public byte R, L;

        public Position( short x, short y, short z, byte r, byte l ) {
            X = x;
            Y = y;
            Z = z;
            R = r;
            L = l;
        }

        public Position( int x, int y, int z ) {
            X = (short)x;
            Y = (short)y;
            Z = (short)z;
            R = 0;
            L = 0;
        }

        public override string ToString() {
            return String.Format( "Position({0},{1},{2} @{3},{4})", X, Y, Z, R, L );
        }
    }
}
