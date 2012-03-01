using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace FemtoCraft {
    class NotchyMapGenerator {
        public static Map Generate( int mapWidth, int mapLength, int mapHeight ) {
            return new NotchyMapGenerator( mapWidth, mapLength, mapHeight ).Generate();
        }


        NotchyMapGenerator( int mapWidth, int mapLength, int mapHeight ) {
            this.mapWidth = mapWidth;
            this.mapLength = mapLength;
            this.mapHeight = mapHeight;
            random = new Random();
            waterLevel = mapHeight / 2;
            heightmap = new int[mapWidth * mapLength];
            map = new Map( mapWidth, mapLength, mapHeight );
            blocks = map.Blocks;
        }


        readonly int mapWidth;
        readonly int mapLength;
        readonly int mapHeight;
        readonly Random random;
        readonly byte[] blocks;
        readonly int waterLevel;
        readonly int[] heightmap;
        readonly Map map;


        Map Generate() {
            Raise();
            Erode();
            Soil();
            Water();
            Melt();
            Grow();
            PlantFlowers();
            PlantShrooms();
            PlantTrees();
            return map;
        }


        void Raise() {
            FilteredNoise raiseNoise1 = new FilteredNoise( new PerlinNoise( random, 8 ), new PerlinNoise( random, 8 ) );
            FilteredNoise raiseNoise2 = new FilteredNoise( new PerlinNoise( random, 8 ), new PerlinNoise( random, 8 ) );
            PerlinNoise raiseNoise3 = new PerlinNoise( random, 6 );

            // raising
            const float f3 = 1.3F;
            for( int x = 0; x < mapWidth; x++ ) {
                for( int y = 0; y < mapLength; y++ ) {
                    double d2 = raiseNoise1.GetNoise( x * f3, y * f3 ) / 6.0 - 4;
                    double d3 = raiseNoise2.GetNoise( x * f3, y * f3 ) / 5.0 + 10.0 - 4;
                    double d4 = raiseNoise3.GetNoise( x, y ) / 8.0;
                    if( d4 > 0 )
                        d3 = d2;
                    double d5 = Math.Max( d2, d3 ) / 2.0;
                    if( d5 < 0 )
                        d5 *= 0.8;
                    heightmap[( x + y * mapWidth )] = (int)d5;
                }
            }
        }


        void Erode() {
            FilteredNoise erodeNoise1 = new FilteredNoise( new PerlinNoise( random, 8 ), new PerlinNoise( random, 8 ) );
            FilteredNoise erodeNoise2 = new FilteredNoise( new PerlinNoise( random, 8 ), new PerlinNoise( random, 8 ) );
            for( int x = 0; x < mapWidth; x++ ) {
                for( int y = 0; y < mapLength; y++ ) {
                    double d1 = erodeNoise1.GetNoise( x * 2, y * 2 ) / 8.0;
                    int i7 = erodeNoise2.GetNoise( x * 2, y * 2 ) > 0 ? 1 : 0;
                    if( d1 <= 2 )
                        continue;
                    int i19 = ( ( heightmap[( x + y * mapWidth )] - i7 ) / 2 * 2 ) + i7;
                    heightmap[( x + y * mapWidth )] = i19;
                }
            }
        }


        void Soil() {
            PerlinNoise soilNoise1 = new PerlinNoise( random, 8 );
            for( int x = 0; x < mapWidth; x++ ) {
                for( int y = 0; y < mapLength; y++ ) {
                    int i7 = (int)( soilNoise1.GetNoise( x, y ) / 24.0 ) - 4;
                    int i19 = heightmap[( x + y * mapWidth )] + waterLevel;
                    int i21 = i19 + i7;
                    heightmap[( x + y * mapWidth )] = Math.Max( i19, i21 );
                    if( heightmap[( x + y * mapWidth )] > mapHeight - 2 )
                        heightmap[( x + y * mapWidth )] = ( mapHeight - 2 );
                    if( heightmap[( x + y * mapWidth )] < 1 )
                        heightmap[( x + y * mapWidth )] = 1;
                    for( int z = 0; z < mapHeight; z++ ) {
                        int index = ( z * mapLength + y ) * mapWidth + x;
                        Block block = Block.Air;
                        if( z <= i19 )
                            block = Block.Dirt;
                        if( z <= i21 )
                            block = Block.Stone;
                        if( z == 0 )
                            block = Block.Lava;
                        blocks[index] = (byte)block;
                    }
                }
            }
        }


        void Water() {
            for( int x = 0; x < mapWidth; x++ ) {
                FloodFill( x, 0, mapHeight / 2 - 1, Block.StillWater );
                FloodFill( x, mapLength - 1, mapHeight / 2 - 1,Block.StillWater );
            }
            for( int y = 0; y < mapLength; y++ ) {
                FloodFill( 0, y, mapHeight / 2 - 1, Block.StillWater );
                FloodFill( mapWidth - 1, y, mapHeight / 2 - 1, Block.StillWater );
            }
            int i4 = mapWidth * mapLength / 8000;
            for( int i12 = 0; i12 < i4; i12++ ) {
                int i16 = random.Next( mapWidth );
                int i8 = waterLevel - 1 - random.Next( 2 );
                int i20 = random.Next( mapLength );
                if( blocks[( ( i8 * mapLength + i20 ) * mapWidth + i16 )] != 0 )
                    continue;
                FloodFill( i16, i20, i8, Block.StillWater );
            }
        }


        void Melt() {
            int m = mapWidth * mapLength * mapHeight / 20000;
            for( int i1 = 0; i1 < m; i1++ ) {
                int i2 = random.Next( mapWidth );
                int i4 = (int)( random.NextDouble() * random.NextDouble() * ( waterLevel - 3 ) );
                int i12 = random.Next( mapLength );
                if( blocks[( ( i4 * mapLength + i12 ) * mapWidth + i2 )] != 0 )
                    continue;
                FloodFill( i2, i12, i4, Block.StillLava );
            }
        }


        void Grow() {
            PerlinNoise growNoise1 = new PerlinNoise( random, 8 );
            PerlinNoise growNoise2 = new PerlinNoise( random, 8 );
            for( int x = 0; x < mapWidth; x++ ) {
                for( int y = 0; y < mapLength; y++ ) {
                    bool placeSand = growNoise1.GetNoise( x, y ) > 8.0;
                    bool placeGravel = growNoise2.GetNoise( x, y ) > 12.0;
                    int i24 = heightmap[( x + y * mapWidth )];
                    int index = ( i24 * mapLength + y ) * mapWidth + x;
                    Block tileAbove = (Block)blocks[( ( ( i24 + 1 ) * mapLength + y ) * mapWidth + x )];
                    if( ( ( tileAbove == Block.Water ) || ( tileAbove == Block.StillWater ) ) &&
                        ( i24 <= mapHeight / 2 - 1 ) && placeGravel )
                        blocks[index] = (byte)Block.Gravel;
                    if( tileAbove != Block.Air )
                        continue;
                    if( ( i24 <= mapHeight / 2 - 1 ) && placeSand ) {
                        blocks[index] = (byte)Block.Sand;
                    } else {
                        blocks[index] = (byte)Block.Grass;
                    }
                }
            }
        }


        const int FlowerClusterDensity = 3000,
                  FlowerSpread = 6,
                  FlowerChainsPerCluster = 10,
                  FlowersPerChain = 5;

        void PlantFlowers() {
            int maxFlowers = mapWidth * mapLength / FlowerClusterDensity;
            for( int cluster = 0; cluster < maxFlowers; cluster++ ) {
                int flowerType = random.Next( 2 );
                int clusterX = random.Next( mapWidth );
                int clusterY = random.Next( mapLength );
                for( int flower = 0; flower < FlowerChainsPerCluster; flower++ ) {
                    int x = clusterX;
                    int y = clusterY;
                    for( int hop = 0; hop < FlowersPerChain; hop++ ) {
                        x += random.Next( FlowerSpread ) - random.Next( FlowerSpread );
                        y += random.Next( FlowerSpread ) - random.Next( FlowerSpread );
                        if( ( x < 0 ) || ( y < 0 ) || ( x >= mapWidth ) || ( y >= mapLength ) )
                            continue;
                        int flowerAltitude = heightmap[( x + y * mapWidth )] + 1;
                        int tileAbove = blocks[( ( flowerAltitude * mapLength + y ) * mapWidth + x )];
                        if( tileAbove != (byte)Block.Air )
                            continue;
                        int i29 = ( flowerAltitude * mapLength + y ) * mapWidth + x;
                        Block blockUnder = (Block)blocks[( ( ( flowerAltitude - 1 ) * mapLength + y ) * mapWidth + x )];
                        if( blockUnder != Block.Grass )
                            continue;
                        if( flowerType == 0 ) {
                            blocks[i29] = (byte)Block.YellowFlower;
                        } else {
                            blocks[i29] = (byte)Block.RedFlower;
                        }
                    }
                }
            }
        }


        void PlantShrooms() {
            int maxShrooms = mapWidth * mapLength * mapHeight / 2000;
            for( int cluster = 0; cluster < maxShrooms; cluster++ ) {
                int shroomType = random.Next( 2 );
                int clusterX = random.Next( mapWidth );
                int clusterY = random.Next( mapLength );
                int clusterZ = random.Next( mapHeight );
                for( int shroom = 0; shroom < 20; shroom++ ) {
                    int x = clusterX;
                    int y = clusterY;
                    int z = clusterZ;
                    for( int hop = 0; hop < 5; hop++ ) {
                        x += random.Next( 6 ) - random.Next( 6 );
                        z += random.Next( 2 ) - random.Next( 2 );
                        y += random.Next( 6 ) - random.Next( 6 );
                        if( ( x < 0 ) || ( y < 0 ) || ( z < 1 ) || ( x >= mapWidth ) || ( y >= mapLength ) ||
                            ( z >= heightmap[( x + y * mapWidth )] - 1 ) )
                            continue;
                        int i30 = ( blocks[( ( z * mapLength + y ) * mapWidth + x )] ) == (byte)Block.Air ? 1 : 0;
                        if( i30 == 0 )
                            continue;
                        int index = ( z * mapLength + y ) * mapWidth + x;
                        Block blockUnder = (Block)blocks[( ( ( z - 1 ) * mapLength + y ) * mapWidth + x )];
                        if( blockUnder != Block.Stone )
                            continue;
                        if( shroomType == 0 ) {
                            blocks[index] = (byte)Block.BrownMushroom;
                        } else {
                            blocks[index] = (byte)Block.RedMushroom;
                        }
                    }
                }
            }
        }


        const int TreeClusterDensity = 4000,
                  TreeChainsPerCluster = 20,
                  TreeHopsPerChain = 20,
                  TreeSpread = 6,
                  TreePlantRatio = 4;

        void PlantTrees() {
            int maxTrees = mapWidth * mapLength / TreeClusterDensity;
            for( int cluster = 0; cluster < maxTrees; cluster++ ) {
                int clusterX = random.Next( mapWidth );
                int clusterY = random.Next( mapLength );
                for( int tree = 0; tree < TreeChainsPerCluster; tree++ ) {
                    int x = clusterX;
                    int y = clusterY;
                    for( int hop = 0; hop < TreeHopsPerChain; hop++ ) {
                        x += random.Next( TreeSpread ) - random.Next( TreeSpread );
                        y += random.Next( TreeSpread ) - random.Next( TreeSpread );
                        if( ( x < 0 ) || ( y < 0 ) || ( x >= mapWidth ) || ( y >= mapLength ) )
                            continue;
                        int i26 = heightmap[( x + y * mapWidth )] + 1;
                        if( random.Next( TreePlantRatio ) != 0 )
                            continue;
                        map.GrowTree( random, x, y, i26 );
                    }
                }
            }
        }


        void FloodFill( int x, int y, int z, Block newBlock ) {
            if( blocks[Index( x, y, z )] != (byte)Block.Air ) return;
            Vector3I coord = new Vector3I( x, y, z );
            Stack<Vector3I> stack = new Stack<Vector3I>();
            stack.Push( coord );
            while( stack.Count > 0 ) {
                coord = stack.Pop();
                blocks[Index( coord )] = (byte)newBlock;
                if( coord.X + 1 < mapWidth && blocks[Index( coord.X + 1, coord.Y, coord.Z )] == (byte)Block.Air ) {
                    stack.Push( new Vector3I( coord.X + 1, coord.Y, coord.Z ) );
                }
                if( coord.X - 1 >= 0 && blocks[Index( coord.X - 1, coord.Y, coord.Z )] == (byte)Block.Air ) {
                    stack.Push( new Vector3I( coord.X - 1, coord.Y, coord.Z ) );
                }
                if( coord.Y + 1 < mapLength && blocks[Index( coord.X, coord.Y + 1, coord.Z )] == (byte)Block.Air ) {
                    stack.Push( new Vector3I( coord.X, coord.Y + 1, coord.Z ) );
                }
                if( coord.Y - 1 >= 0 && blocks[Index( coord.X, coord.Y - 1, coord.Z )] == (byte)Block.Air ) {
                    stack.Push( new Vector3I( coord.X, coord.Y - 1, coord.Z ) );
                }
                if( coord.Z - 1 >= 0 && blocks[Index( coord.X, coord.Y, coord.Z - 1 )] == (byte)Block.Air ) {
                    stack.Push( new Vector3I( coord.X, coord.Y, coord.Z - 1 ) );
                }
            }
        }


        struct Vector3I {
            public Vector3I( int x, int y, int z ) {
                X = x;
                Y = y;
                Z = z;
            }
            public readonly int X, Y, Z;
        }


        int Index( int x, int y, int z ) {
            return ( z * mapLength + y ) * mapWidth + x;
        }


        int Index( Vector3I coord ) {
            return ( coord.Z * mapLength + coord.Y ) * mapWidth + coord.X;
        }
    }
}