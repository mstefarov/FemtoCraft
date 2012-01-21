// Part of FemtoCraft | Copyright 2012 Matvei Stefarov <me@matvei.org> | See LICENSE.txt

namespace FemtoCraft {
    sealed class WaterPhysics {
        public const int TickDelay = 0;
        readonly Map map;


        public WaterPhysics( Map map ) {
            this.map = map;
        }


        public void OnNeighborUpdated( int x, int y, int z, Block thisBlock, Block updatedNeighbor ) {
            if( updatedNeighbor == Block.Lava || updatedNeighbor == Block.StillLava ) {
                map.SetBlock( null, x, y, z, Block.Stone );

            } else if( thisBlock == Block.Water && updatedNeighbor == Block.Air ) {
                map.QueuePhysicsUpdate( x, y, z, updatedNeighbor );

            } else if( thisBlock == Block.StillWater ) {
                if( map.GetBlock( x - 1, y, z ) == Block.Air ||
                    map.GetBlock( x + 1, y, z ) == Block.Air ||
                    map.GetBlock( x, y - 1, z ) == Block.Air ||
                    map.GetBlock( x, y + 1, z ) == Block.Air ||
                    map.GetBlock( x, y, z - 1 ) == Block.Air ) {
                    map.SetBlockNoUpdate( x, y, z, Block.Water );
                    map.QueuePhysicsUpdate( x, y, z, Block.Water );
                }
            }
        }


        public void OnTick( int x, int y, int z ) {
            bool updated = false;
            do {
                z--;
                if( z < 0 || map.GetBlock( x, y, z ) != Block.Air || map.IsSponged( x, y, z ) ) {
                    break;
                }
                updated = map.SetBlock( null, x, y, z, Block.Water );
            } while( updated );
            z++;

            if( !updated ) {
                updated |= Propagate( x - 1, y, z );
                updated |= Propagate( x + 1, y, z );
                updated |= Propagate( x, y - 1, z );
                updated |= Propagate( x, y + 1, z );
            }

            if( updated ) {
                map.QueuePhysicsUpdate( x, y, z, Block.Water );
            } else {
                map.SetBlockNoUpdate( x, y, z, Block.StillWater );
            }
        }


        bool Propagate( int x, int y, int z ) {
            Block currentBlock = map.GetBlock( x, y, z );
            if( currentBlock == Block.Air &&
                    !map.IsSponged( x, y, z ) &&
                    map.SetBlock( null, x, y, z, Block.Water ) ) {
                map.QueuePhysicsUpdate( x, y, z, Block.Water );
                return true;
            } else {
                return false;
            }
        }
    }
}