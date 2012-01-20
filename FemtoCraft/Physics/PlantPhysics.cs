// Part of FemtoCraft | Copyright 2012 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
using System;
using System.Linq;

namespace FemtoCraft {
    sealed class PlantPhysics {
        readonly Map map;
        readonly Random random;
        const int TraverseStep = 200;
        readonly int[] traversePattern = new int[TraverseStep];
        int tickNumber;


        public PlantPhysics( Map map ) {
            this.map = map;
            random = new Random();
            traversePattern = Enumerable.Range( 0, TraverseStep ).ToArray();
            RandomizeTraversal();
        }


        void RandomizeTraversal() {
            Array.Sort( traversePattern, ( i1, i2 ) => random.Next() );
        }


        public void Tick() {
            int startIndex = traversePattern[tickNumber % TraverseStep];
            for( int i = startIndex; i < map.Volume; i += TraverseStep ) {
                Block targetBlock = (Block)map.Blocks[i];
                switch( targetBlock ) {
                    case Block.Grass:
                        TriggerGrass( map.X( i ), map.Y( i ), map.Z( i ) );
                        break;

                    case Block.YellowFlower:
                    case Block.RedFlower:
                        TriggerFlower(map.X( i ), map.Y( i ), map.Z( i ) );
                        break;

                    case Block.BrownMushroom:
                    case Block.RedMushroom:
                        TriggerMushroom( map.X( i ), map.Y( i ), map.Z( i ) );
                        break;
                }

            }
            tickNumber++;
            if( tickNumber % TraverseStep == 0 ) {
                RandomizeTraversal();
            }
        }


        // die if block is lit, or if block underneath is not stone/gravel/cobblestone
        void TriggerMushroom( int x, int y, int z ) {
            Block blockUnder = map.GetBlock( x, y, z - 1 );
            if( blockUnder != Block.Stone && blockUnder != Block.Gravel && blockUnder != Block.Cobblestone
                    || map.IsLit( x, y, z ) ) {
                map.SetBlock( null, x, y, z, Block.Air );
            }
        }


        // die if block is not lit, or if block underneath is not grass/dirt
        void TriggerFlower( int x, int y, int z ) {
            Block blockUnder = map.GetBlock( x, y, z - 1 );
            if( blockUnder != Block.Grass && blockUnder != Block.Dirt
                    || !map.IsLit( x, y, z ) ) {
                map.SetBlock( null, x, y, z, Block.Air );
            }
        }


        void TriggerGrass( int x, int y, int z ) {
            // only trigger 25% of the time
            if( random.Next( 4 ) != 0 ) return;

            // die (turn to dirt) if not lit
            if( !map.IsLit( x, y, z ) ) {
                map.SetBlock( null, x, y, z, Block.Dirt );
                return;
            }

            // spread to 4 random nearby blocks
            for( int i = 0; i < 4; i++ ) {
                int x2 = random.Next( x - 1, x + 2 );
                int y2 = random.Next( y - 1, y + 2 );
                int z2 = random.Next( z - 2, z + 3 );
                if( map.InBounds( x2, y2, z2 ) &&
                        map.IsLit( x2, y2, z2 ) &&
                        map.GetBlock( x2, y2, z2 ) == Block.Dirt ) {
                    map.SetBlock( null, x2, y2, z2, Block.Grass );
                    return;
                }
            }
        }
    }
}