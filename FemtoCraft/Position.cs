// Part of FemtoCraft | Copyright 2012 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
// Based on fCraft.Position - fCraft is Copyright 2009-2012 Matvei Stefarov <me@matvei.org> | See LICENSE.fCraft.txt
using System;

namespace FemtoCraft {
    public struct Position : IEquatable<Position> {
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


        #region Equality

        public static bool operator ==( Position a, Position b ) {
            return a.Equals( b );
        }

        public static bool operator !=( Position a, Position b ) {
            return !a.Equals( b );
        }

        public bool Equals( Position other ) {
            return ( X == other.X ) && ( Y == other.Y ) && ( Z == other.Z ) && ( R == other.R ) && ( L == other.L );
        }

        public override bool Equals( object obj ) {
            return obj is Position && Equals( (Position)obj );
        }

        public override int GetHashCode() {
            return ( X + Y * short.MaxValue ) ^ ( R + L * short.MaxValue ) + Z;
        }

        #endregion
    }
}
