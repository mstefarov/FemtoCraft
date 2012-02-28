// Part of FemtoCraft | Copyright 2012 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
// Based on fCraft.MapConversion.MapMCSharp - fCraft is Copyright 2009-2012 Matvei Stefarov <me@matvei.org> | See LICENSE.fCraft.txt
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FemtoCraft {
    sealed class Map {
        public readonly int Width, Length, Height,
                            Volume,
                            WaterLevel;
        public readonly byte[] Blocks;
        public Position Spawn;
        public bool ChangedSinceSave;


        public Map( int width, int length, int height ) {
            if( width < 16 || width > 2048 ||
                length < 16 || length > 2048 ||
                height < 16 || height > 2048 ) {
                throw new ArgumentException( "Invalid map dimension(s)." );
            }
            Width = width;
            Length = length;
            Height = height;
            Volume = width * length * height;
            WaterLevel = Height / 2;
            Blocks = new byte[Volume];
            Spawn = new Position( Width * 16, Length * 16,
                                  Math.Min( Height * 32, Int16.MaxValue ) );
        }


        [DebuggerStepThrough]
        public int Index( int x, int y, int z ) {
            return ( z * Length + y ) * Width + x;
        }


        [DebuggerStepThrough]
        public int X( int i) {
            return i % Width;
        }


        [DebuggerStepThrough]
        public int Y( int i ) {
            return ( i / Width ) % Length;
        }


        [DebuggerStepThrough]
        public int Z( int i ) {
            return i / ( Length * Width );
        }


        public bool SetBlock( Player except, int x, int y, int z, Block newBlock ) {
            lock( physicsLock ) {
                if( SetBlockNoNeighborChange( except, x, y, z, newBlock ) ) {
                    PhysicsUpdateNeighbors( x, y, z, newBlock );
                    return true;
                } else {
                    return false;
                }
            }
        }


        public bool SetBlockNoNeighborChange( Player except, int x, int y, int z, Block newBlock ) {
            Block oldBlock = GetBlock( x, y, z );
            if( oldBlock == newBlock || oldBlock == Block.Undefined ) return false;

            // water at map edges
            if( ( x == 0 || y == 0 || x == Width - 1 || y == Length - 1 ) &&
                ( z >= WaterLevel - 2 && z < WaterLevel ) && ( newBlock == Block.Air ) ) {
                newBlock = Block.Water;

            // stair stacking
            } else if( newBlock == Block.Stair && GetBlock( x, y, z - 1 ) == Block.Stair ) {
                SetBlock( null, x, y, z, Block.Air );
                SetBlock( null, x, y, z - 1, Block.DoubleStair );
                return true;
            }

            Blocks[Index( x, y, z )] = (byte)newBlock;
            ChangedSinceSave = true;

            PhysicsOnRemoved( x, y, z, oldBlock );
            PhysicsOnPlaced( x, y, z, newBlock );

            Server.Players.Send( except, Packet.MakeSetBlock( x, y, z, GetBlock( x, y, z ) ) );
            return true;
        }


        public bool SetBlockNoUpdate( int x, int y, int z, Block newBlock ) {
            Block oldBlock = GetBlock( x, y, z );
            if( oldBlock == newBlock || oldBlock == Block.Undefined ) return false;

            Blocks[Index( x, y, z )] = (byte)newBlock;
            ChangedSinceSave = true;
            return true;
        }


        public Block GetBlock( int x, int y, int z ) {
            if( x < Width && y < Length && z < Height && x >= 0 && y >= 0 && z >= 0 ) {
                return (Block)Blocks[Index( x, y, z )];
            } else {
                return Block.Undefined;
            }
        }


        public bool InBounds( int x, int y, int z ) {
            return x < Width && y < Length && z < Height && x >= 0 && y >= 0 && z >= 0;
        }


        public void Swap( int x1, int y1, int z1, int x2, int y2, int z2 ) {
            Block block1 = GetBlock( x1, y1, z1 );
            Block block2 = GetBlock( x2, y2, z2 );
            SetBlockNoNeighborChange( null, x1, y1, z1, block2 );
            SetBlockNoNeighborChange( null, x2, y2, z2, block1 );
            PhysicsUpdateNeighbors( x1, y1, z1, block2 );
            PhysicsUpdateNeighbors( x2, y2, z2, block1 );
        }


        #region Physics

        bool physicsEnabled;
        SandPhysics sandPhysics;
        PlantPhysics plantPhysics;
        WaterPhysics waterPhysics;
        LavaPhysics lavaPhysics;
        readonly Queue<PhysicsUpdate> tickQueue = new Queue<PhysicsUpdate>();
        readonly object physicsLock = new object();
        static readonly byte[] TickDelays = new byte[256];
        int tickNumber;


        void PhysicsOnRemoved( int x, int y, int z, Block oldBlock ) {
            if( !physicsEnabled ) return;
            if( Config.PhysicsWater && oldBlock == Block.Sponge ) {
                waterPhysics.OnSpongeRemoved( x, y, z );
            }
        }


        void PhysicsOnPlaced( int x, int y, int z, Block newBlock ) {
            if( !physicsEnabled ) return;

            if( Config.PhysicsWater && newBlock == Block.Water ) {
                PhysicsQueueTick( x, y, z, Block.Water );

            } else if( Config.PhysicsLava && newBlock == Block.Lava ) {
                PhysicsQueueTick( x, y, z, Block.Lava );

            } else if( Config.PhysicsSand && ( newBlock == Block.Sand || newBlock == Block.Gravel ) ) {
                sandPhysics.Trigger( x, y, z );

            } else if( newBlock == Block.Sponge ) {
                waterPhysics.OnSpongePlaced( x, y, z );
            }
            plantPhysics.OnBlockPlaced( x, y, z );
        }


        void PhysicsOnNeighborUpdate( int x, int y, int z, Block updatedNeighbor ) {
            if( !physicsEnabled || !InBounds( x, y, z ) ) return;
            Block thisBlock = GetBlock( x, y, z );

            if( Config.PhysicsSand && ( thisBlock == Block.Sand || thisBlock == Block.Gravel ) ) {
                sandPhysics.Trigger( x, y, z );
            }
            if( Config.PhysicsWater ) {
                waterPhysics.OnNeighborUpdated( x, y, z, thisBlock, updatedNeighbor );
            }
            if( Config.PhysicsLava ) {
                lavaPhysics.OnNeighborUpdated( x, y, z, thisBlock, updatedNeighbor );
            }
        }


        void PhysicsOnTick( int x, int y, int z, Block newBlock ) {
            if( !physicsEnabled ) return;
            if( newBlock == Block.Water ) {
                waterPhysics.OnTick( x, y, z );
            } else if( newBlock == Block.Lava ) {
                lavaPhysics.OnTick( x, y, z );
            }
        }


        public void Tick() {
            lock( physicsLock ) {
                if( !physicsEnabled ) return;
                tickNumber++;
                if( tickNumber % 5 == 0 ) {
                    int oldLength = tickQueue.Count;
                    for( int i = 0; i < oldLength; i++ ) {
                        PhysicsUpdate update = tickQueue.Dequeue();
                        if( update.Delay > 0 ) {
                            update.Delay--;
                            tickQueue.Enqueue( update );
                        } else if( update.OldBlock == GetBlock( update.X, update.Y, update.Z ) ) {
                            PhysicsOnTick( update.X, update.Y, update.Z, update.OldBlock );
                        }
                    }
                }
                plantPhysics.Tick( tickNumber );
            }
        }


        public void PhysicsUpdateNeighbors( int x, int y, int z, Block block ) {
            PhysicsOnNeighborUpdate( x - 1, y, z, block );
            PhysicsOnNeighborUpdate( x + 1, y, z, block );
            PhysicsOnNeighborUpdate( x, y, z - 1, block );
            PhysicsOnNeighborUpdate( x, y, z + 1, block );
            PhysicsOnNeighborUpdate( x, y - 1, z, block );
            PhysicsOnNeighborUpdate( x, y + 1, z, block );
        }


        public void PhysicsQueueTick( int x, int y, int z, Block oldBlock ) {
            if( !InBounds( x, y, z ) ) return;
            PhysicsUpdate update = new PhysicsUpdate( x, y, z, oldBlock, TickDelays[(int)oldBlock] );
            lock( physicsLock ) {
                tickQueue.Enqueue( update );
            }
        }


        public void DisablePhysics() {
            lock( physicsLock ) {
                sandPhysics = null;
                plantPhysics = null;
                waterPhysics = null;
                lavaPhysics = null;
                tickQueue.Clear();
                physicsEnabled = false;
            }
            Logger.Log( "Map: Physics disabled." );
        }


        public void EnablePhysics() {
            Stopwatch sw = Stopwatch.StartNew();
            lock( physicsLock ) {
                sandPhysics = new SandPhysics( this );
                plantPhysics = new PlantPhysics( this );
                waterPhysics = new WaterPhysics( this );
                lavaPhysics = new LavaPhysics( this );
                physicsEnabled = true;
            }
            sw.Stop();
            Logger.Log( "Map: Physics enabled in {0}ms", sw.ElapsedMilliseconds );
        }

        #endregion


        static Map() {
            TickDelays[(int)Block.Water] = WaterPhysics.TickDelay;
            TickDelays[(int)Block.Lava] = LavaPhysics.TickDelay;
        }


        public static Map CreateFlatgrass( int width, int length, int height ) {
            Map map = new Map( width, length, height );
            map.Blocks.MemSet( (byte)Block.Stone, 0, width * length * ( height / 2 - 5 ) );
            map.Blocks.MemSet( (byte)Block.Dirt, width * length * ( height / 2 - 5 ), width * length * 4 );
            map.Blocks.MemSet( (byte)Block.Grass, width * length * ( height / 2 - 1 ), width * length );
            if( Config.Physics ) {
                map.EnablePhysics();
            }
            return map;
        }
    }
}