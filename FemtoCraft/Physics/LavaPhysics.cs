// Part of FemtoCraft | Copyright 2012 Matvei Stefarov <me@matvei.org> | See LICENSE.txt

namespace FemtoCraft {
    sealed class LavaPhysics {
        public const int TickDelay = 5;
        readonly Map map;


        public LavaPhysics( Map map ) {
            this.map = map;
        }


        public void OnNeighborUpdated( int x, int y, int z, Block thisBlock, Block updatedNeighbor ) {
            if( ( thisBlock == Block.Lava || thisBlock == Block.StillLava ) &&
                ( updatedNeighbor == Block.Water || updatedNeighbor == Block.StillWater ) ) {
                map.SetBlock( null, x, y, z, Block.Stone );

            } else if( thisBlock == Block.Lava && updatedNeighbor == Block.Air ) {
                map.QueuePhysicsUpdate( x, y, z, updatedNeighbor );

            } else if( thisBlock == Block.StillLava ) {
                if( map.GetBlock( x - 1, y, z ) == Block.Air ||
                    map.GetBlock( x + 1, y, z ) == Block.Air ||
                    map.GetBlock( x, y - 1, z ) == Block.Air ||
                    map.GetBlock( x, y + 1, z ) == Block.Air ||
                    map.GetBlock( x, y, z - 1 ) == Block.Air ) {
                    map.SetBlockNoUpdate( x, y, z, Block.Lava );
                    map.QueuePhysicsUpdate( x, y, z, Block.Lava );
                }
            }
        }


        public void OnTick( int x, int y, int z ) {
            bool updated = false;

                z--;
                if( z >= 0 && map.GetBlock( x, y, z ) == Block.Air ) {
                    updated = map.SetBlock( null, x, y, z, Block.StillLava );
                }
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
            if( currentBlock == Block.Air && map.SetBlock( null, x, y, z, Block.Lava ) ) {
                map.QueuePhysicsUpdate( x, y, z, Block.Lava );
                return true;
            } else {
                return false;
            }
        }
    }
}