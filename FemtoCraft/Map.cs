using System;

namespace FemtoCraft {
    class Map {
        public readonly int Width, Length, Height, Volume;
        public byte[] Blocks;
        public Position Spawn;

        public Map( int width, int length, int height ) {
            if( width < 16 || width > 2048 ||
                length < 16 || length > 2048 ||
                height < 16 || height > 2048 ) {
                throw new ArgumentException( "Invalid map dimension(s)." );
            }
            Width = width;
            Length = length;
            Height = height;
            Volume = width * length * height;
            Blocks = new byte[Volume];
        }


        public int Index( int x, int y, int z ) {
            return ( z * Length + y ) * Width + x;
        }


        public void SetBlock( int x, int y, int z, Block type ) {
            if( x < Width && y < Length && z < Height && x >= 0 && y >= 0 && z >= 0 ) {
                Blocks[Index( x, y, z )] = (byte)type;
            }
        }


        public Block GetBlock( int x, int y, int z ) {
            if( x < Width && y < Length && z < Height && x >= 0 && y >= 0 && z >= 0 ) {
                return (Block)Blocks[Index( x, y, z )];
            } else {
                return Block.Undefined;
            }
        }


        public bool InBounds( int x, int y, int z ) {
            return x < Width && y < Length && z < Height && x >= 0 && y >= 0 && z >= 0;
        }
    }
}
