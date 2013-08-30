// Part of FemtoCraft | Copyright 2012-2013 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
// Based on ZombieDev code, contributed by Conrad "Redshift" Morgan
namespace FemtoCraft {
    sealed class SnowPhysics {
        readonly Map map;
        public const int TickDelay = 2;


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

            bool needSet = false;
            // drop down
            if( dropZ != z ) {
                Block oldBlock = map.GetBlock( x, y, dropZ );
                map.SetBlockNoNeighborChange( null, x, y, z, Block.Air );
                if( oldBlock != Block.Snow ) {
                    needSet = true;
                }
                z = dropZ;
            }

            if( MeltsSnow( map.GetBlock( x - 1, y, z ) ) ||
                MeltsSnow( map.GetBlock( x + 1, y, z ) ) ||
                MeltsSnow( map.GetBlock( x, y - 1, z ) ) ||
                MeltsSnow( map.GetBlock( x, y + 1, z ) ) ||
                MeltsSnow( map.GetBlock( x, y, z - 1 ) ) ) {

                map.PhysicsQueueTick( x, y, z, Block.Snow );

            }else if( needSet ) {
                map.SetBlock( null, x, y, dropZ, Block.Snow );
            }
        }


        public void OnTick(int x, int y, int z) {
            if( MeltsSnow( map.GetBlock( x - 1, y, z ) ) ||
                MeltsSnow( map.GetBlock( x + 1, y, z ) ) ||
                MeltsSnow( map.GetBlock( x, y - 1, z ) ) ||
                MeltsSnow( map.GetBlock( x, y + 1, z ) ) ||
                MeltsSnow( map.GetBlock( x, y, z - 1 ) ) ) {
                map.SetBlock( null, x, y, z, Block.Air );
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

        static bool MeltsSnow( Block block ) {
            switch( block ) {
                case Block.Fire:
                case Block.Lava:
                case Block.StillLava:
                case Block.Magma:
                    return true;
                default:
                    return false;
            }
        }
    }
}