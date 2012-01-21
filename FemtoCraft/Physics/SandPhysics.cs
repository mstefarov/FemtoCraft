// Part of FemtoCraft | Copyright 2012 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
// Based on ZombieDev code, contributed by Conrad "Redshift" Morgan
namespace FemtoCraft {
    sealed class SandPhysics {
        readonly Map map;


        public SandPhysics( Map map ) {
            this.map = map;
        }


        public void Trigger( int x, int y, int z ) {
            int dropZ = z;
            while( dropZ > 0 ) {
                if( !map.GetBlock( x, y, dropZ - 1 ).LetsSandThrough() ) {
                    break;
                }
                dropZ--;
            }

            if( dropZ != z ) {
                Block oldBlock = map.GetBlock( x, y, dropZ );
                if( oldBlock != Block.Air ) {
                    map.SetBlockNoUpdate( x, y, dropZ, Block.Air );
                }
                map.Swap( x, y, z,
                          x, y, dropZ );
            }
        }
    }
}