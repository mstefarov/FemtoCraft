// Part of FemtoCraft | Copyright 2012-2013 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
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
                if( !LetsSandThrough( map.GetBlock( x, y, dropZ - 1 ) ) ) {
                    break;
                }
                dropZ--;
            }

            if( dropZ == z ) return;
            Block oldBlock = map.GetBlock( x, y, dropZ );
            if( oldBlock != Block.Air ) {
                map.SetBlockNoUpdate( x, y, dropZ, Block.Air );
            }
            map.Swap( x, y, z,
                      x, y, dropZ );
        }


        static bool LetsSandThrough( Block block ) {
            switch( block ) {
                case Block.Air:
                case Block.Water:
                case Block.Lava:
                case Block.StillWater:
                case Block.StillLava:
                    return true;
                default:
                    return false;
            }
        }
    }
}