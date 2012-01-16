using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FemtoCraft {
    unsafe static class Util {
        public static void MemSet( this byte[] array, byte value, int startIndex, int length ) {
            if( array == null ) throw new ArgumentNullException( "array" );
            if( length < 0 || length > array.Length ) {
                throw new ArgumentOutOfRangeException( "length" );
            }
            if( startIndex < 0 || startIndex + length > array.Length ) {
                throw new ArgumentOutOfRangeException( "startIndex" );
            }

            byte[] rawValue = new[] { value, value, value, value, value, value, value, value };
            Int64 fillValue = BitConverter.ToInt64( rawValue, 0 );

            fixed( byte* ptr = &array[startIndex] ) {
                Int64* dest = (Int64*)ptr;
                while( length >= 8 ) {
                    *dest = fillValue;
                    dest++;
                    length -= 8;
                }
                byte* bDest = (byte*)dest;
                for( byte i = 0; i < length; i++ ) {
                    *bDest = value;
                    bDest++;
                }
            }
        }
    }
}
