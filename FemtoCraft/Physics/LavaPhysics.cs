// Part of FemtoCraft | Copyright 2012-2013 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
using JetBrains.Annotations;

namespace FemtoCraft {
    sealed class LavaPhysics {
        public const int TickDelay = 5;
        public const Block LavaPlusWater = Block.Stone;
        readonly Map map;


        public LavaPhysics( Map map ) {
            this.map = map;
        }


        public void OnNeighborUpdated( int x, int y, int z, Block thisBlock, Block updatedNeighbor ) {
            if( Config.PhysicsFloodProtection && z >= map.WaterLevel )
                return;
            if( ( thisBlock == Block.Lava || thisBlock == Block.StillLava ) &&
                ( updatedNeighbor == Block.Water || updatedNeighbor == Block.StillWater ) ) {
                map.SetBlock( null, x, y, z, LavaPlusWater );

            } else if( thisBlock == Block.Lava ) {
                map.PhysicsQueueTick( x, y, z, updatedNeighbor );

            } else if( thisBlock == Block.StillLava ) {
                if( CanSpreadTo( map.GetBlock( x - 1, y, z ) ) || CanSpreadTo( map.GetBlock( x + 1, y, z ) ) ||
                    CanSpreadTo( map.GetBlock( x, y - 1, z ) ) || CanSpreadTo( map.GetBlock( x, y + 1, z ) ) ||
                    CanSpreadTo( map.GetBlock( x, y, z - 1 ) ) ) {
                    map.SetBlockNoUpdate( x, y, z, Block.Lava );
                    map.PhysicsQueueTick( x, y, z, Block.Lava );
                }
            }
        }


        [Pure]
        static bool CanSpreadTo( Block block ) {
            switch( block ) {
                case Block.Air:
                case Block.Snow:
                case Block.Fire:
                    return true;
                default:
                    return false;
            }
        }


        public void OnTick( int x, int y, int z ) {
            if( Config.PhysicsFloodProtection && z >= map.WaterLevel )
                return;
            bool updated = false;

            if( z > 1 && CanSpreadTo( map.GetBlock( x, y, z - 1 ) ) ) {
                updated = map.SetBlock( null, x, y, z - 1, Block.Lava );
            }

            if( !updated ) {
                updated |= Propagate( x - 1, y, z );
                updated |= Propagate( x + 1, y, z );
                updated |= Propagate( x, y - 1, z );
                updated |= Propagate( x, y + 1, z );
            }

            if( updated ) {
                map.PhysicsQueueTick( x, y, z, Block.Lava );
            } else {
                map.SetBlockNoUpdate( x, y, z, Block.StillLava );
            }
        }


        bool Propagate( int x, int y, int z ) {
            Block currentBlock = map.GetBlock( x, y, z );
            if( CanSpreadTo( currentBlock ) && map.SetBlock( null, x, y, z, Block.Lava ) ) {
                map.PhysicsQueueTick( x, y, z, Block.Lava );
                return true;
            } else {
                return false;
            }
        }
    }
}