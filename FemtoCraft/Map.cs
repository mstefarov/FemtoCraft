// Part of FemtoCraft | Copyright 2012 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
// Based on fCraft.MapConversion.MapMCSharp - fCraft is Copyright 2009-2012 Matvei Stefarov <me@matvei.org> | See LICENSE.fCraft.txt
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using JetBrains.Annotations;

namespace FemtoCraft {
    sealed unsafe class Map {
        public static Map CreateFlatgrass( int width, int length, int height ) {
            Map map = new Map( width, length, height );
            map.Blocks.MemSet( (byte)Block.Stone, 0, width * length * ( height / 2 - 5 ) );
            map.Blocks.MemSet( (byte)Block.Dirt, width * length * ( height / 2 - 5 ), width * length * 4 );
            map.Blocks.MemSet( (byte)Block.Grass, width * length * ( height / 2 - 1 ), width * length );
            map.PhysicsInit();
            return map;
        }


        public readonly int Width, Length, Height,
                            Volume,
                            WaterLevel;
        public readonly byte[] Blocks;
        public Position Spawn;

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
                                  Math.Min( Height * 32, short.MaxValue ) );
            sandPhysics= new SandPhysics( this );
            plantPhysics = new PlantPhysics( this );
            spongeData = new BitList( Volume );
            waterPhysics = new WaterPhysics( this );
            lavaPhysics = new LavaPhysics();
            shadows = new short[Width, Length];
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
            if( SetBlockNoNeighborChange( except, x, y, z, newBlock ) ) {
                PhysicsUpdateNeighbors( x, y, z, newBlock );
                return true;
            } else {
                return false;
            }
        }


        public bool SetBlockNoNeighborChange( Player except, int x, int y, int z, Block newBlock ) {
            if( !InBounds( x, y, z ) ) return false;
            Block oldBlock = GetBlock( x, y, z );
            if( oldBlock == newBlock ) return false;

            // water at map edges
            if( ( x == 0 || y == 0 || x == Width - 1 || y == Length - 1 ) &&
                ( z >= WaterLevel - 2 && z < WaterLevel ) && ( newBlock == Block.Air ) ) {
                newBlock = Block.Water;
            }

            Blocks[Index( x, y, z )] = (byte)newBlock;

            // do physics!
            PhysicsOnRemoved( x, y, z, oldBlock );
            PhysicsOnPlaced( x, y, z, newBlock );
            UpdateShadow( x, y, z );

            Server.Players.Send( except, Packet.MakeSetBlock( x, y, z, newBlock ) );
            return true;
        }


        public bool SetBlockNoUpdate( int x, int y, int z, Block newBlock ) {
            if( !InBounds( x, y, z ) ) return false;
            Block oldBlock = GetBlock( x, y, z );
            if( newBlock == oldBlock ) return false;

            Blocks[Index( x, y, z )] = (byte)newBlock;
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

        readonly BitList spongeData;
        readonly short[,] shadows;
        readonly SandPhysics sandPhysics;
        readonly PlantPhysics plantPhysics;
        readonly WaterPhysics waterPhysics;
        readonly LavaPhysics lavaPhysics;
        readonly Queue<PhysicsUpdate> tickQueue = new Queue<PhysicsUpdate>();
        readonly object physicsLock = new object();
        int tickNumber;


        void PhysicsOnRemoved( int x, int y, int z, Block oldBlock ) {
            if( oldBlock == Block.Sponge ) {
                OnSpongeRemoved( x, y, z );
            }
        }


        void PhysicsOnPlaced( int x, int y, int z, Block newBlock ) {
            if( newBlock == Block.Stair && GetBlock( x, y, z - 1 ) == Block.Stair ) {
                SetBlock( null, x, y, z, Block.Air );
                SetBlock( null, x, y, z - 1, Block.DoubleStair );

            } else if( Config.PhysicsWater && newBlock == Block.Water ) {
                QueuePhysicsUpdate( new PhysicsUpdate( x, y, z, Block.Water, WaterPhysics.TickDelay ) );

            } else if( Config.PhysicsLava && newBlock == Block.Lava ) {
                QueuePhysicsUpdate( new PhysicsUpdate( x, y, z, Block.Lava, LavaPhysics.TickDelay ) );

            } else if( Config.PhysicsSand && ( newBlock == Block.Sand || newBlock == Block.Gravel ) ) {
                sandPhysics.Trigger( x, y, z );

            } else if( newBlock == Block.Sponge ) {
                OnSpongeAdded( x, y, z );
            }
        }


        void PhysicsOnNeighborUpdate( int x, int y, int z, Block updatedBlock ) {
            if( !InBounds( x, y, z ) ) return;
            Block neighborBlock = GetBlock( x, y, z );

            if( Config.PhysicsSand && ( neighborBlock == Block.Sand || neighborBlock == Block.Gravel ) ) {
                sandPhysics.Trigger( x, y, z );
            }
        }


        void PhysicsOnTick( int x, int y, int z, Block newBlock ) {
            if( newBlock == Block.Water ) {
                waterPhysics.Trigger( x, y, z );
            }
        }
        

        void PhysicsUpdateNeighbors( int x, int y, int z, Block block ) {
            PhysicsOnNeighborUpdate( x - 1, y, z, block );
            PhysicsOnNeighborUpdate( x + 1, y, z, block );
            PhysicsOnNeighborUpdate( x, y, z - 1, block );
            PhysicsOnNeighborUpdate( x, y, z + 1, block );
            PhysicsOnNeighborUpdate( x, y - 1, z, block );
            PhysicsOnNeighborUpdate( x, y + 1, z, block );
        }


        public void PhysicsOnTick() {
            lock( physicsLock ) {
                tickNumber++;
                if( tickNumber % 5 == 0 ) {
                    int oldLength = tickQueue.Count;
                    for( int i = 0; i < oldLength; i++ ) {
                        PhysicsUpdate update = tickQueue.Dequeue();
                        if( update.Delay > 0 ) {
                            update.Delay--;
                            tickQueue.Enqueue( update );
                        } else {
                            if( update.OldBlock == GetBlock( update.X, update.Y, update.Z ) ) {
                                PhysicsOnTick( update.X, update.Y, update.Z, update.OldBlock );
                            }
                        }
                    }
                }
                plantPhysics.Tick( tickNumber );
            }
        }


        public void QueuePhysicsUpdate( PhysicsUpdate update ) {
            lock( physicsLock ) {
                tickQueue.Enqueue( update );
            }
        }


        public bool IsLit( int x, int y, int z ) {
            return shadows[x, y] <= z;
        }


        public void PhysicsInit() {
            // calculate shadows
            Logger.Log( "Map: Preparing physics..." );
            Stopwatch sw = Stopwatch.StartNew();
            for( int x = 0; x < Width; x++ ) {
                for( int y = 0; y < Length; y++ ) {
                    UpdateShadow( x, y, Height - 1 );
                }
            }

            // calculate sponge coverage
            fixed( byte* ptr = Blocks ) {
                for( int i = 0; i < Blocks.Length; i++ ) {
                    if( (Block)ptr[i] == Block.Sponge ) {
                        OnSpongeAdded( X( i ), Y( i ), Z( i ) );
                    }
                }
            }
            sw.Stop();
            Logger.Log( "Map: Physics prep done in {0}ms", sw.ElapsedMilliseconds );
        }


        const int SpongeRange = 2;

        void OnSpongeAdded( int x, int y, int z ) {
            int i = Index( x, y, z );
            for( int x1 = x - SpongeRange; x1 <= x + SpongeRange; x1++ ) {
                for( int y1 = y - SpongeRange; y1 <= y + SpongeRange; y1++ ) {
                    for( int z1 = z - SpongeRange; z1 <= z + SpongeRange; z1++ ) {
                        if( InBounds( x1, y1, z1 ) ) {
                            spongeData[Index( x1, y1, z1 )] = true;
                        }
                    }
                }
            }
        }


        void OnSpongeRemoved( int x, int y, int z ) {
            for( int x1 = x - SpongeRange; x1 <= x + SpongeRange; x1++ ) {
                for( int y1 = y - SpongeRange; y1 <= y + SpongeRange; y1++ ) {
                    for( int z1 = z - SpongeRange; z1 <= z + SpongeRange; z1++ ) {
                        OnSpongeRemovedInner( x1, y1, z1 );
                    }
                }
            }
        }


        void OnSpongeRemovedInner( int x1, int y1, int z1 ) {
            for( int x2 = x1 - SpongeRange; x2 <= x1 + SpongeRange; x2++ ) {
                for( int y2 = y1 - SpongeRange; y2 <= y1 + SpongeRange; y2++ ) {
                    for( int z2 = z1 - SpongeRange; z2 <= z1 + SpongeRange; z2++ ) {
                        if( GetBlock( x2, y2, z2 ) == Block.Sponge ) {
                            spongeData[Index( x1, y1, z1 )] = true;
                            return;
                        }
                    }
                }
            }
        }


        public bool IsSponged( int x, int y, int z ) {
            return spongeData[Index( x, y, z )];
        }


        void UpdateShadow( int x, int y, int topZ ) {
            if( topZ < shadows[x, y] ) return;
            for( int z = topZ; z >= 0; z-- ) {
                if( GetBlock( x, y, z ).CastsShadow() ) {
                    shadows[x, y] = (short)z;
                    return;
                }
            }
            shadows[x, y] = 0;
        }

        #endregion


        #region Loading and Saving

        [NotNull]
        public static Map Load( [NotNull] string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            using( FileStream mapStream = File.OpenRead( fileName ) ) {
                using( GZipStream gs = new GZipStream( mapStream, CompressionMode.Decompress ) ) {
                    BinaryReader bs = new BinaryReader( gs );

                    // Read in the magic number
                    if( bs.ReadUInt16() != 0x752 ) {
                        throw new Exception( "Could not load map (incorrect header)." );
                    }

                    // Read in the map dimesions
                    int width = bs.ReadInt16();
                    int length = bs.ReadInt16();
                    int height = bs.ReadInt16();

                    Map map = new Map( width, length, height );

                    // Read in the spawn location
                    map.Spawn = new Position {
                        X = (short)( bs.ReadInt16() * 32 + 16 ),
                        Z = (short)( bs.ReadInt16() * 32 + 16 ),
                        Y = (short)( bs.ReadInt16() * 32 + 16 ),
                        R = bs.ReadByte(),
                        L = bs.ReadByte(),
                    };

                    // Write the VistPermission and BuildPermission bytes
                    bs.ReadByte();
                    bs.ReadByte();

                    // Read map data
                    int bytesRead = 0;
                    int bytesLeft = map.Blocks.Length;
                    while( bytesLeft > 0 ) {
                        int readPass = bs.Read( map.Blocks, bytesRead, bytesLeft );
                        if( readPass == 0 ) throw new EndOfStreamException();
                        bytesRead += readPass;
                        bytesLeft -= readPass;
                    }
                    
                    fixed( byte* ptr = map.Blocks ) {
                        for( int j = 0; j < map.Blocks.Length; j++ ) {
                            if( ptr[j] > 49 ) {
                                ptr[j] = Mapping[ptr[j]];
                            }
                        }
                    }

                    map.PhysicsInit();
                    return map;
                }
            }
        }


        public void Save( [NotNull] string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            string tempFileName = Path.GetTempFileName();
            using( FileStream mapStream = File.Create( tempFileName ) ) {
                using( GZipStream gs = new GZipStream( mapStream, CompressionMode.Compress ) ) {
                    BinaryWriter bs = new BinaryWriter( gs );

                    // Write the magic number
                    bs.Write( (ushort)0x752 );

                    // Write the map dimensions
                    bs.Write( (short)Width );
                    bs.Write( (short)Length );
                    bs.Write( (short)Height );

                    // Write the spawn location
                    bs.Write( (short)(Spawn.X / 32) );
                    bs.Write( (short)(Spawn.Z / 32) );
                    bs.Write( (short)(Spawn.Y / 32) );

                    // Write the spawn orientation
                    bs.Write( Spawn.R );
                    bs.Write( Spawn.L );

                    // Write the VistPermission and BuildPermission bytes
                    bs.Write( (byte)0 );
                    bs.Write( (byte)0 );

                    // Write the map data
                    bs.Write( Blocks, 0, Volume );

                    bs.Close();
                }
            }
            Util.MoveOrReplaceFile( tempFileName, fileName );
        }


        static readonly byte[] Mapping = new byte[256];

        static Map() {
            Mapping[100] = (byte)Block.Glass;       // op_glass
            Mapping[101] = (byte)Block.Obsidian;    // opsidian
            Mapping[102] = (byte)Block.Brick;       // op_brick
            Mapping[103] = (byte)Block.Stone;       // op_stone
            Mapping[104] = (byte)Block.Cobblestone;       // op_cobblestone
            // 105 = op_air
            Mapping[106] = (byte)Block.Water;       // op_water

            // 107-109 unused
            Mapping[110] = (byte)Block.Wood;        // wood_float
            Mapping[111] = (byte)Block.Log;         // door
            Mapping[112] = (byte)Block.Lava;        // lava_fast
            Mapping[113] = (byte)Block.Obsidian;    // door2
            Mapping[114] = (byte)Block.Glass;       // door3
            Mapping[115] = (byte)Block.Stone;       // door4
            Mapping[116] = (byte)Block.Leaves;      // door5
            Mapping[117] = (byte)Block.Sand;        // door6
            Mapping[118] = (byte)Block.Wood;        // door7
            Mapping[119] = (byte)Block.Green;       // door8
            Mapping[120] = (byte)Block.TNT;         // door9
            Mapping[121] = (byte)Block.Stair;       // door10

            Mapping[122] = (byte)Block.Log;         // tdoor
            Mapping[123] = (byte)Block.Obsidian;    // tdoor2
            Mapping[124] = (byte)Block.Glass;       // tdoor3
            Mapping[125] = (byte)Block.Stone;       // tdoor4
            Mapping[126] = (byte)Block.Leaves;      // tdoor5
            Mapping[127] = (byte)Block.Sand;        // tdoor6
            Mapping[128] = (byte)Block.Wood;        // tdoor7
            Mapping[129] = (byte)Block.Green;       // tdoor8

            Mapping[130] = (byte)Block.White;       // MsgWhite
            Mapping[131] = (byte)Block.Black;       // MsgBlack
            Mapping[132] = (byte)Block.Air;         // MsgAir
            Mapping[133] = (byte)Block.Water;       // MsgWater
            Mapping[134] = (byte)Block.Lava;        // MsgLava

            Mapping[135] = (byte)Block.TNT;         // tdoor9
            Mapping[136] = (byte)Block.Stair;       // tdoor10
            Mapping[137] = (byte)Block.Air;         // tdoor11
            Mapping[138] = (byte)Block.Water;       // tdoor12
            Mapping[139] = (byte)Block.Lava;        // tdoor13

            Mapping[140] = (byte)Block.Water;       // WaterDown
            Mapping[141] = (byte)Block.Lava;        // LavaDown
            Mapping[143] = (byte)Block.Aqua;        // WaterFaucet
            Mapping[144] = (byte)Block.Orange;      // LavaFaucet

            // 143 unused
            Mapping[145] = (byte)Block.Water;       // finiteWater
            Mapping[146] = (byte)Block.Lava;        // finiteLava
            Mapping[147] = (byte)Block.Cyan;        // finiteFaucet

            Mapping[148] = (byte)Block.Log;         // odoor1
            Mapping[149] = (byte)Block.Obsidian;    // odoor2
            Mapping[150] = (byte)Block.Glass;       // odoor3
            Mapping[151] = (byte)Block.Stone;       // odoor4
            Mapping[152] = (byte)Block.Leaves;      // odoor5
            Mapping[153] = (byte)Block.Sand;        // odoor6
            Mapping[154] = (byte)Block.Wood;        // odoor7
            Mapping[155] = (byte)Block.Green;       // odoor8
            Mapping[156] = (byte)Block.TNT;         // odoor9
            Mapping[157] = (byte)Block.Stair;       // odoor10
            Mapping[158] = (byte)Block.Lava;        // odoor11
            Mapping[159] = (byte)Block.Water;       // odoor12

            Mapping[160] = (byte)Block.Air;         // air_portal
            Mapping[161] = (byte)Block.Water;       // water_portal
            Mapping[162] = (byte)Block.Lava;        // lava_portal

            // 163 unused
            Mapping[164] = (byte)Block.Air;         // air_door
            Mapping[165] = (byte)Block.Air;         // air_switch
            Mapping[166] = (byte)Block.Water;       // water_door
            Mapping[167] = (byte)Block.Lava;        // lava_door

            // 168-174 = odoor*_air
            Mapping[175] = (byte)Block.Cyan;        // blue_portal
            Mapping[176] = (byte)Block.Orange;      // orange_portal
            // 177-181 = odoor*_air

            Mapping[182] = (byte)Block.TNT;         // smalltnt
            Mapping[183] = (byte)Block.TNT;         // bigtnt
            Mapping[184] = (byte)Block.Lava;        // tntexplosion
            Mapping[185] = (byte)Block.Lava;        // fire

            // 186 unused
            Mapping[187] = (byte)Block.Glass;       // rocketstart
            Mapping[188] = (byte)Block.Gold;        // rockethead
            Mapping[189] = (byte)Block.Iron;       // firework

            Mapping[190] = (byte)Block.Lava;        // deathlava
            Mapping[191] = (byte)Block.Water;       // deathwater
            Mapping[192] = (byte)Block.Air;         // deathair
            Mapping[193] = (byte)Block.Water;       // activedeathwater
            Mapping[194] = (byte)Block.Lava;        // activedeathlava

            Mapping[195] = (byte)Block.Lava;        // magma
            Mapping[196] = (byte)Block.Water;       // geyser

            // 197-210 = air
            Mapping[211] = (byte)Block.Red;         // door8_air
            Mapping[212] = (byte)Block.Lava;        // door9_air
            // 213-229 = air

            Mapping[230] = (byte)Block.Aqua;        // train
            Mapping[231] = (byte)Block.TNT;         // creeper
            Mapping[232] = (byte)Block.MossyRocks;  // zombiebody
            Mapping[233] = (byte)Block.Lime;        // zombiehead

            // 234 unused
            Mapping[235] = (byte)Block.White;       // birdwhite
            Mapping[236] = (byte)Block.Black;       // birdblack
            Mapping[237] = (byte)Block.Lava;        // birdlava
            Mapping[238] = (byte)Block.Red;         // birdred
            Mapping[239] = (byte)Block.Water;       // birdwater
            Mapping[240] = (byte)Block.Blue;        // birdblue
            Mapping[242] = (byte)Block.Lava;        // birdkill

            Mapping[245] = (byte)Block.Gold;        // fishgold
            Mapping[246] = (byte)Block.Sponge;      // fishsponge
            Mapping[247] = (byte)Block.Gray;        // fishshark
            Mapping[248] = (byte)Block.Red;         // fishsalmon
            Mapping[249] = (byte)Block.Blue;        // fishbetta
        }

        #endregion
    }
}