// Part of FemtoCraft | Copyright 2012 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
// Contributed by Conrad "Redshift" Morgan
namespace FemtoCraft {
    sealed class SandPhysics {
        readonly Map map;


        public SandPhysics( Map map ) {
            this.map = map;
        }


        public void Trigger( int x, int y, int z ) {
            int dropHeight = Drop( x, y, z );
            if( dropHeight != z ) {
                Block oldBlock = map.GetBlock( x, y, dropHeight );
                if( oldBlock != Block.Air && oldBlock.LetsSandThrough() ) {
                    map.SetBlockNoUpdate( x, y, dropHeight, Block.Air );
                }
                map.Swap( x, y, z,
                          x, y, dropHeight );
            }
        }


        int Drop( int x, int y, int z ) {
            for( ; z > 0; z-- ) {
                if( !map.GetBlock( x, y, z - 1 ).LetsSandThrough() ) {
                    return z;
                }
            }
            return 0;
        }
    }
}