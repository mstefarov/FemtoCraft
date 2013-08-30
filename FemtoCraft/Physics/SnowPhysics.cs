// Part of FemtoCraft | Copyright 2012-2013 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
namespace FemtoCraft {
    sealed class SnowPhysics {
        readonly Map map;
        public const int TickDelay = 2;


        public SnowPhysics( Map map ) {
            this.map = map;
        }


        public void Trigger( int x, int y, int z ) {
            // Find where the snow should drop down to (if at all)
            int dropZ = z;
            while( dropZ > 0 ) {
                if( !LetsSnowThrough( map.GetBlock( x, y, dropZ - 1 ) ) ) {
                    break;
                }
                dropZ--;
            }

            // If snow can drop down, start falling...
            bool snowHasDropped = false;
            if( dropZ != z ) {
                Block oldBlock = map.GetBlock( x, y, dropZ );
                map.SetBlockNoNeighborChange( null, x, y, z, Block.Air );
                if( oldBlock != Block.Snow ) {
                    snowHasDropped = true;
                }
                z = dropZ;
            }

            if( ThisShouldMelt( x, y, z ) ) {
                // If we have any hot neighbors, queue melting (2 ticks from now)
                map.PhysicsQueueTick( x, y, z, Block.Snow );

            }else if( snowHasDropped ) {
                // Land the dropped snow
                map.SetBlock( null, x, y, dropZ, Block.Snow );
            }
        }


        public void OnTick( int x, int y, int z ) {
            if( ThisShouldMelt( x, y, z ) ) {
                map.SetBlock( null, x, y, z, Block.Air );
            }
        }


        // check if any immediate neighbors are hot
        bool ThisShouldMelt( int x, int y, int z ) {
            return MeltsSnow( map.GetBlock( x, y + 1, z ) ) || MeltsSnow( map.GetBlock( x - 1, y, z ) ) ||
                   MeltsSnow( map.GetBlock( x + 1, y, z ) ) || MeltsSnow( map.GetBlock( x, y - 1, z ) ) ||
                   MeltsSnow( map.GetBlock( x, y, z - 1 ) );
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


        static bool LetsSnowThrough( Block block ) {
            return block == Block.Air || block == Block.Snow;
        }
    }
}