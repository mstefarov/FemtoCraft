namespace FemtoCraft {
    class BitMap {
        readonly int[] array;

        public BitMap( int size ) {
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