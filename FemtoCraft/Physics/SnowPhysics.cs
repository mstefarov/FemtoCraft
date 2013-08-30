// Part of FemtoCraft | Copyright 2012-2013 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
// Based on ZombieDev code, contributed by Conrad "Redshift" Morgan
namespace FemtoCraft {
    sealed class SnowPhysics {
        readonly Map map;


        public SnowPhysics( Map map ) {
            this.map = map;
        }


        public void Trigger( int x, int y, int z ) {
            int dropZ = z;
            while( dropZ > 0 ) {
                if( !LetsSnowThrough( map.GetBlock( x, y, dropZ - 1 ) ) ) {
                    break;
                }
                dropZ--;
            }

            if( dropZ == z ) return;

            Block oldBlock = map.GetBlock( x, y, dropZ );
            map.SetBlockNoNeighborChange( null, x, y, z, Block.Air );
            if( oldBlock != Block.Snow ) {
                map.SetBlock( null, x, y, dropZ, Block.Snow );
            }
        }


        static bool LetsSnowThrough( Block block ) {
            switch( block ) {
                case Block.Air:
                case Block.Snow:
                    return true;
                default:
                    return false;
            }
        }
    }
}