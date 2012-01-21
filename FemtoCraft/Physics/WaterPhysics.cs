// Part of FemtoCraft | Copyright 2012 Matvei Stefarov <me@matvei.org> | See LICENSE.txt

namespace FemtoCraft {
    sealed class WaterPhysics {
        public const int TickDelay = 0;
        readonly Map map;


        public WaterPhysics( Map map ) {
            this.map = map;
        }


        public void Trigger( int x, int y, int z ) {
            bool updated = false;
            for( ; z > 0; z-- ) {
                if( map.GetBlock( x, y, z - 1 ) != Block.Air || map.IsSponged( x, y, z - 1 ) ) {
                    break;
                }
                if( map.SetBlock( null, x, y, z - 1, Block.Water ) ) {
                    updated = true;
                } else {
                    break;
                }
            }
            if( !updated ) {
                updated |= Propagate( x - 1, y, z );
                updated |= Propagate( x + 1, y, z );
                updated |= Propagate( x, y - 1, z );
                updated |= Propagate( x, y + 1, z );
            }

            if( updated ) {
                map.QueuePhysicsUpdate( new PhysicsUpdate( x, y, z, Block.Water, TickDelay ) );
            } else {
                map.SetBlockNoUpdate( x, y, z, Block.StillWater );
            }
        }


        bool Propagate( int x, int y, int z ) {
            Block currentBlock = map.GetBlock( x, y, z );
            if( currentBlock == Block.Air &&
                    !map.IsSponged( x, y, z ) &&
                    map.SetBlock( null, x, y, z, Block.Water ) ) {
                map.QueuePhysicsUpdate( new PhysicsUpdate( x, y, z, Block.Water, TickDelay ) );
                return true;
            } else {
                return false;
            }
        }
    }
}