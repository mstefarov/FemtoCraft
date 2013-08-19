// Part of FemtoCraft | Copyright 2012-2013 Matvei Stefarov <me@matvei.org> | See LICENSE.txt

namespace FemtoCraft {
    sealed class BitList {
        readonly int[] array;

        public BitList( int size ) {
            array = new int[( size - 1 ) / 32 + 1];
        }


        public bool this[ int index ] {
            get {
                return ( array[index >> 5] & ( 1 << ( index & 31 ) ) ) != 0;
            }
            set {
                if( value ) {
                    array[index >> 5] |= 1 << ( index & 31 );
                } else {
                    array[index >> 5] &= ~( 1 << ( index & 31 ) );
                }
            }
        }
    }
}