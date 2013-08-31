// Part of FemtoCraft | Copyright 2012-2013 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
using System;
using JetBrains.Annotations;

namespace FemtoCraft {
    unsafe sealed class WaterPhysics {
        public const int TickDelay = 0;
        const int SpongeRange = 2;
        readonly Map map;
        readonly BitList spongeData;


        public WaterPhysics( [NotNull] Map map ) {
            if( map == null ) throw new ArgumentNullException( "map" );
            this.map = map;
            spongeData = new BitList( map.Volume );
            fixed( byte* ptr = map.Blocks ) {
                for( int i = 0; i < map.Volume; i++ ) {
                    if( (Block)ptr[i] == Block.Sponge ) {
                        SpongePlacedUpdateCoverage( map.X( i ), map.Y( i ), map.Z( i ) );
                    }
                }
            }
        }


        public void OnNeighborUpdated( int x, int y, int z, Block thisBlock, Block updatedNeighbor ) {
            if( Config.PhysicsFloodProtection && z >= map.WaterLevel ) return;
            if( ( thisBlock == Block.Water || thisBlock == Block.StillWater ) &&
                ( updatedNeighbor == Block.Lava || updatedNeighbor == Block.StillLava ) ) {
                map.SetBlock( null, x, y, z, LavaPhysics.LavaPlusWater );

            } else if( thisBlock == Block.Water ) {
                map.PhysicsQueueTick( x, y, z, updatedNeighbor );

            } else if( thisBlock == Block.StillWater ) {
                if( CanSpreadTo(map.GetBlock( x - 1, y, z )) ||
                    CanSpreadTo(map.GetBlock( x + 1, y, z )) ||
                    CanSpreadTo(map.GetBlock( x, y - 1, z )) ||
                    CanSpreadTo(map.GetBlock( x, y + 1, z )) ||
                    CanSpreadTo(map.GetBlock( x, y, z - 1 )) ) {
                    map.SetBlockNoUpdate( x, y, z, Block.Water );
                    map.PhysicsQueueTick( x, y, z, Block.Water );
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
            if( Config.PhysicsFloodProtection && z >= map.WaterLevel ) return;
            bool updated = false;
            do {
                z--;
                if( z < 0 || !CanSpreadTo(map.GetBlock( x, y, z )) || IsSponged( x, y, z ) ) {
                    break;
                }
                updated = map.SetBlock( null, x, y, z, Block.Water );
            } while( updated );
            z++;

            updated |= Propagate( x - 1, y, z );
            updated |= Propagate( x + 1, y, z );
            updated |= Propagate( x, y - 1, z );
            updated |= Propagate( x, y + 1, z );

            if( updated ) {
                map.PhysicsQueueTick( x, y, z, Block.Water );
            } else {
                map.SetBlockNoUpdate( x, y, z, Block.StillWater );
            }
        }


        bool Propagate( int x, int y, int z ) {
            Block currentBlock = map.GetBlock( x, y, z );
            if( CanSpreadTo(currentBlock) && !IsSponged( x, y, z ) && map.SetBlock( null, x, y, z, Block.Water ) ) {
                map.PhysicsQueueTick( x, y, z, Block.Water );
                return true;
            } else {
                return false;
            }
        }


        bool IsSponged( int x, int y, int z ) {
            return spongeData[map.Index( x, y, z )];
        }


        public void OnSpongePlaced( int x, int y, int z  ) {
            SpongePlacedUpdateCoverage( x, y, z );
            for( int x1 = x - SpongeRange; x1 <= x + SpongeRange; x1++ ) {
                for( int y1 = y - SpongeRange; y1 <= y + SpongeRange; y1++ ) {
                    for( int z1 = z - SpongeRange; z1 <= z + SpongeRange; z1++ ) {
                        Block block = map.GetBlock( x1, y1, z1 );
                        if( block == Block.Water || block == Block.StillWater ) {
                            map.SetBlockNoNeighborChange( null, x1, y1, z1, Block.Air );
                        }
                    }
                }
            }
        }


        void SpongePlacedUpdateCoverage( int x, int y, int z ) {
            for( int x1 = x - SpongeRange; x1 <= x + SpongeRange; x1++ ) {
                for( int y1 = y - SpongeRange; y1 <= y + SpongeRange; y1++ ) {
                    for( int z1 = z - SpongeRange; z1 <= z + SpongeRange; z1++ ) {
                        if( map.InBounds( x1, y1, z1 ) ) {
                            spongeData[map.Index( x1, y1, z1 )] = true;
                        }
                    }
                }
            }
        }


        public void OnSpongeRemoved( int x, int y, int z ) {
            for( int x1 = x - SpongeRange; x1 <= x + SpongeRange; x1++ ) {
                for( int y1 = y - SpongeRange; y1 <= y + SpongeRange; y1++ ) {
                    for( int z1 = z - SpongeRange; z1 <= z + SpongeRange; z1++ ) {
                        if( map.InBounds( x1, y1, z1 ) ) {
                            SpongeRemovedUpdateCoverage( x1, y1, z1 );
                        }
                    }
                }
            }
            for( int x1 = x - SpongeRange; x1 <= x + SpongeRange; x1++ ) {
                for( int y1 = y - SpongeRange; y1 <= y + SpongeRange; y1++ ) {
                    for( int z1 = z - SpongeRange; z1 <= z + SpongeRange; z1++ ) {
                        if( map.InBounds( x1, y1, z1 ) ) {
                            map.PhysicsUpdateNeighbors( x1, y1, z1, map.GetBlock( x1, y1, z1 ) );
                        }
                    }
                }
            }
        }


        void SpongeRemovedUpdateCoverage( int x1, int y1, int z1 ) {
            for( int x2 = x1 - SpongeRange; x2 <= x1 + SpongeRange; x2++ ) {
                for( int y2 = y1 - SpongeRange; y2 <= y1 + SpongeRange; y2++ ) {
                    for( int z2 = z1 - SpongeRange; z2 <= z1 + SpongeRange; z2++ ) {
                        if( map.GetBlock( x2, y2, z2 ) == Block.Sponge ) {
                            spongeData[map.Index( x1, y1, z1 )] = true;
                            return;
                        }
                    }
                }
            }
            spongeData[map.Index( x1, y1, z1 )] = false;
        }
    }
}