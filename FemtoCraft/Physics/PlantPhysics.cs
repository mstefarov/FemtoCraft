// Part of FemtoCraft | Copyright 2012 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
using System;
using System.Linq;

namespace FemtoCraft {
    sealed class PlantPhysics {
        readonly Map map;
        readonly Random random;
        const int TraverseStep = 200;
        readonly int[] traversePattern = new int[TraverseStep];
        readonly short[,] shadows;


        public PlantPhysics( Map map ) {
            this.map = map;
            random = new Random();
            traversePattern = Enumerable.Range( 0, TraverseStep ).ToArray();
            RandomizeTraversal();
            shadows = new short[map.Width,map.Length];
            for( int x = 0; x < map.Width; x++ ) {
                for( int y = 0; y < map.Length; y++ ) {
                    UpdateShadow( x, y, map.Height - 1 );
                }
            }
        }


        bool IsLit( int x, int y, int z ) {
            return shadows[x, y] <= z;
        }


        void UpdateShadow( int x, int y, int topZ ) {
            if( topZ < shadows[x, y] ) return;
            for( int z = topZ; z >= 0; z-- ) {
                if( map.GetBlock( x, y, z ).CastsShadow() ) {
                    shadows[x, y] = (short)z;
                    return;
                }
            }
            shadows[x, y] = 0;
        }


        void RandomizeTraversal() {
            Array.Sort( traversePattern, ( i1, i2 ) => random.Next() );
        }


        public void OnBlockPlaced( int x, int y, int z ) {
            UpdateShadow( x, y, z );
        }


        public void Tick( int tickNumber ) {
            int startIndex = traversePattern[tickNumber % TraverseStep];
            for( int i = startIndex; i < map.Volume; i += TraverseStep ) {
                Block targetBlock = (Block)map.Blocks[i];
                switch( targetBlock ) {
                    case Block.Grass:
                        TriggerGrass( map.X( i ), map.Y( i ), map.Z( i ) );
                        break;

                    case Block.YellowFlower:
                    case Block.RedFlower:
                        TriggerFlower( map.X( i ), map.Y( i ), map.Z( i ) );
                        break;

                    case Block.BrownMushroom:
                    case Block.RedMushroom:
                        TriggerMushroom( map.X( i ), map.Y( i ), map.Z( i ) );
                        break;

                    case Block.Sapling:
                        TriggerSapling( map.X( i ), map.Y( i ), map.Z( i ) );
                        break;
                }
            }
            if( tickNumber % TraverseStep == 0 ) {
                RandomizeTraversal();
            }
        }


        // die if block is lit, or if block underneath is not stone/gravel/cobblestone
        void TriggerMushroom( int x, int y, int z ) {
            if( !Config.PhysicsPlants ) return;
            Block blockUnder = map.GetBlock( x, y, z - 1 );
            if( blockUnder != Block.Stone && blockUnder != Block.Gravel && blockUnder != Block.Cobblestone ||
                IsLit( x, y, z ) ) {
                map.SetBlock( null, x, y, z, Block.Air );
            }
        }


        // die if block is not lit, or if block underneath is not grass/dirt
        void TriggerFlower( int x, int y, int z ) {
            if( !Config.PhysicsPlants ) return;
            Block blockUnder = map.GetBlock( x, y, z - 1 );
            if( blockUnder != Block.Grass && blockUnder != Block.Dirt || !IsLit( x, y, z ) ) {
                map.SetBlock( null, x, y, z, Block.Air );
            }
        }


        void TriggerGrass( int x, int y, int z ) {
            if( !Config.PhysicsGrass ) return;

            // only trigger 25% of the time
            if( random.Next( 4 ) != 0 ) return;

            // die (turn to dirt) if not lit
            if( !IsLit( x, y, z ) ) {
                map.SetBlock( null, x, y, z, Block.Dirt );
                return;
            }

            // spread to 4 random nearby blocks
            for( int i = 0; i < 4; i++ ) {
                int x2 = random.Next( x - 1, x + 2 );
                int y2 = random.Next( y - 1, y + 2 );
                int z2 = random.Next( z - 2, z + 3 );
                if( map.InBounds( x2, y2, z2 ) &&
                    IsLit( x2, y2, z2 ) &&
                    map.GetBlock( x2, y2, z2 ) == Block.Dirt ) {
                    map.SetBlock( null, x2, y2, z2, Block.Grass );
                    return;
                }
            }
        }


        void TriggerSapling( int x, int y, int z ) {
            if( !Config.PhysicsPlants ) return;
            Block blockUnder = map.GetBlock( x, y, z - 1 );
            if( blockUnder != Block.Grass && blockUnder != Block.Dirt || !IsLit( x, y, z ) ) {
                map.SetBlock( null, x, y, z, Block.Air );
                return;
            }
            if( Config.PhysicsTrees && random.Next( 5 ) == 0 ) {
                map.SetBlockNoUpdate( x, y, z, Block.Air );
                if( !GrowTree( x, y, z ) ) {
                    map.SetBlockNoUpdate( x, y, z, Block.Sapling );
                }
            }
        }


        bool GrowTree( int sx, int sy, int sz ) {
            int treeHeight = random.Next( 4, 7 );
            if( sz >= map.Height - treeHeight - 1 ) return false;

            for( int z = sz; z <= sz + treeHeight + 1; z++ ) {
                byte extent = 1;
                if( z == sz ) extent = 0;
                if( z >= sz + treeHeight - 1 ) extent = 2;
                for( int x = sx - extent; x <= sx + extent; x++ ) {
                    for( int y = sy - extent; y <= sy + extent; y++ ) {
                        if( map.GetBlock( x, y, z ) != Block.Air ) {
                            return false;
                        }
                    }
                }
            }

            if( map.GetBlock( sx, sy, sz - 1 ) != Block.Grass ) return false;

            map.SetBlock( null, sx, sy, sz - 1, Block.Dirt );

            for( int z = ( sz - 3 ) + treeHeight; z <= sz + treeHeight; z++ ) {
                int blocksLeft = z - ( sz + treeHeight );
                int foliageExtent = 1 - blocksLeft / 2;
                for( int x = sx - foliageExtent; x <= sx + foliageExtent; x++ ) {
                    for( int y = sy - foliageExtent; y <= sy + foliageExtent; y++ ) {
                        if( ( Math.Abs( x - sx ) != foliageExtent || Math.Abs( y - sz ) != foliageExtent ||
                                random.Next( 2 ) != 0 && blocksLeft != 0 ) &&
                                map.GetBlock( x, y, z ) == Block.Air )
                            map.SetBlock( null, x, y, z, Block.Leaves );
                    }
                }
            }

            for( int z = 0; z < treeHeight; z++ ) {
                if( map.GetBlock( sx, sy, sz + z ) == Block.Air ) {
                    map.SetBlock( null, sx, sy, sz + z, Block.Log );
                }
            }
            return true;
        }
    }
}