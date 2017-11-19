// Part of FemtoCraft | Copyright 2012-2013 Matvei Stefarov <me@matvei.org> | See LICENSE.txt

using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;

namespace FemtoCraft {
    sealed partial class Player {
        const string CustomBlocksExtName = "CustomBlocks";
        const int CustomBlocksExtVersion = 1;
        const string BlockPermissionsExtName = "BlockPermissions";
        const int BlockPermissionsExtVersion = 1;
        const string LongerMessagesExtName = "LongerMessages";
        const int LongerMessagesExtVersion = 1;
        const byte CustomBlocksLevel = 1;

        // Note: if more levels are added, change UsesCustomBlocks from bool to int
        bool UsesCustomBlocks { get; set; }
        bool SupportsBlockPermissions { get; set; }
        bool SupportsLongerMessages { get; set; }
        [NotNull]
        string ClientName { get; set; }

        bool NegotiateProtocolExtension() {
            // write our ExtInfo and ExtEntry packets
            writer.Write( Packet.MakeExtInfo( 3 ).Bytes );
            writer.Write( Packet.MakeExtEntry( CustomBlocksExtName, CustomBlocksExtVersion ).Bytes );
            writer.Write( Packet.MakeExtEntry( BlockPermissionsExtName, BlockPermissionsExtVersion ).Bytes );
            writer.Write( Packet.MakeExtEntry( LongerMessagesExtName, LongerMessagesExtVersion ).Bytes );

            // Expect ExtInfo reply from the client
            OpCode extInfoReply = reader.ReadOpCode();
            //Logger.Log( "Expected: {0} / Received: {1}", OpCode.ExtInfo, extInfoReply );
            if( extInfoReply != OpCode.ExtInfo ) {
                Logger.LogWarning( "Player {0} from {1}: Unexpected ExtInfo reply ({2})", Name, IP, extInfoReply );
                return false;
            }
            ClientName = reader.ReadString();
            int expectedEntries = reader.ReadInt16();

            // wait for client to send its ExtEntries
            bool sendCustomBlockPacket = false;
            List<string> clientExts = new List<string>();
            for( int i = 0; i < expectedEntries; i++ ) {
                // Expect ExtEntry replies (0 or more)
                OpCode extEntryReply = reader.ReadOpCode();
                //Logger.Log( "Expected: {0} / Received: {1}", OpCode.ExtEntry, extEntryReply );
                if( extEntryReply != OpCode.ExtEntry ) {
                    Logger.LogWarning( "Player {0} from {1}: Unexpected ExtEntry reply ({2})", Name, IP, extInfoReply );
                    return false;
                }
                string extName = reader.ReadString();
                int extVersion = reader.ReadInt32();
                if( extName == CustomBlocksExtName && extVersion == CustomBlocksExtVersion ) {
                    // Hooray, client supports custom blocks! We still need to check support level.
                    sendCustomBlockPacket = true;
                    clientExts.Add( extName + " " + extVersion );
                } else if( extName == BlockPermissionsExtName && extVersion == BlockPermissionsExtVersion ) {
                    SupportsBlockPermissions = true;
                    clientExts.Add( extName + " " + extVersion );
                } else if( extName == LongerMessagesExtName && extVersion == LongerMessagesExtVersion ) {
                    SupportsLongerMessages = true;
                    clientExts.Add( extName + " " + extVersion );
                } // else ignore any extensions we don't recognize
            }

            // log client's capabilities
            if( clientExts.Count > 0 ) {
                Logger.Log( "Player {0} is using \"{1}\", supporting: {2}",
                            Name,
                            ClientName,
                            clientExts.JoinToString( ", " ) );
            }

            if( sendCustomBlockPacket ) {
                // if client also supports CustomBlockSupportLevel, figure out what level to use

                // Send CustomBlockSupportLevel
                writer.Write( Packet.MakeCustomBlockSupportLevel( CustomBlocksLevel ).Bytes );

                // Expect CustomBlockSupportLevel reply
                OpCode customBlockSupportLevelReply = reader.ReadOpCode();
                //Logger.Log( "Expected: {0} / Received: {1}", OpCode.CustomBlockSupportLevel, customBlockSupportLevelReply );
                if( customBlockSupportLevelReply != OpCode.CustomBlockSupportLevel ) {
                    Logger.LogWarning( "Player {0} from {1}: Unexpected CustomBlockSupportLevel reply ({2})",
                                       Name,
                                       IP,
                                       customBlockSupportLevelReply );
                    return false;
                }
                byte clientLevel = reader.ReadByte();
                UsesCustomBlocks = ( clientLevel >= CustomBlocksLevel );
            }
            return true;
        }


        // For non-extended players, use appropriate substitution
        void ProcessOutgoingSetBlock( ref Packet packet ) {
            if( packet.Bytes[7] > (byte)Map.MaxLegalBlockType && !UsesCustomBlocks ) {
                packet.Bytes[7] = (byte)Map.GetFallbackBlock( (Block)packet.Bytes[7] );
            }
        }


        void SendBlockPermissions() {
            Send( Packet.MakeSetBlockPermission( Block.Water, CanUseWater, true ) );
            Send( Packet.MakeSetBlockPermission( Block.StillWater, CanUseWater, true ) );
            Send( Packet.MakeSetBlockPermission( Block.Lava, CanUseLava, true ) );
            Send( Packet.MakeSetBlockPermission( Block.StillLava, CanUseLava, true ) );
            Send( Packet.MakeSetBlockPermission( Block.Admincrete, CanUseSolid, CanUseSolid ) );
            Send( Packet.MakeSetBlockPermission( Block.Grass, CanUseGrass, true ) );
        }
    }


    partial struct Packet {
        [Pure]
        public static Packet MakeExtInfo( short extCount ) {
            // Logger.Log( "Send: ExtInfo({0},{1})", Server.VersionString, extCount );
            Packet packet = new Packet( OpCode.ExtInfo );
            Encoding.ASCII.GetBytes( Server.VersionString.PadRight( 64 ), 0, 64, packet.Bytes, 1 );
            ToNetOrder( extCount, packet.Bytes, 65 );
            return packet;
        }

        [Pure]
        public static Packet MakeExtEntry( [NotNull] string name, int version ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            // Logger.Log( "Send: ExtEntry({0},{1})", name, version );
            Packet packet = new Packet( OpCode.ExtEntry );
            Encoding.ASCII.GetBytes( name.PadRight( 64 ), 0, 64, packet.Bytes, 1 );
            ToNetOrder( version, packet.Bytes, 65 );
            return packet;
        }

        [Pure]
        public static Packet MakeCustomBlockSupportLevel( byte level ) {
            // Logger.Log( "Send: CustomBlockSupportLevel({0})", level );
            Packet packet = new Packet( OpCode.CustomBlockSupportLevel );
            packet.Bytes[1] = level;
            return packet;
        }

        [Pure]
        public static Packet MakeSetBlockPermission( Block block, bool canPlace, bool canDelete ) {
            Packet packet = new Packet( OpCode.SetBlockPermission );
            packet.Bytes[1] = (byte)block;
            packet.Bytes[2] = (byte)(canPlace ? 1 : 0);
            packet.Bytes[3] = (byte)(canDelete ? 1 : 0);
            return packet;
        }
    }


    sealed partial class Map {
        public const Block MaxCustomBlockType = Block.StoneBrick;
        readonly static Block[] FallbackBlocks = new Block[256];


        static void DefineFallbackBlocks() {
            for( int i = 0; i <= (int)Block.Obsidian; i++ ) {
                FallbackBlocks[i] = (Block)i;
            }
            FallbackBlocks[(int)Block.CobbleSlab] = Block.Slab;
            FallbackBlocks[(int)Block.Rope] = Block.BrownMushroom;
            FallbackBlocks[(int)Block.Sandstone] = Block.Sand;
            FallbackBlocks[(int)Block.Snow] = Block.Air;
            FallbackBlocks[(int)Block.Fire] = Block.StillLava;
            FallbackBlocks[(int)Block.LightPink] = Block.Pink;
            FallbackBlocks[(int)Block.DarkGreen] = Block.Green;
            FallbackBlocks[(int)Block.Brown] = Block.Dirt;
            FallbackBlocks[(int)Block.DarkBlue] = Block.Blue;
            FallbackBlocks[(int)Block.Turquoise] = Block.Cyan;
            FallbackBlocks[(int)Block.Ice] = Block.Glass;
            FallbackBlocks[(int)Block.Tile] = Block.Iron;
            FallbackBlocks[(int)Block.Magma] = Block.Obsidian;
            FallbackBlocks[(int)Block.Pillar] = Block.White;
            FallbackBlocks[(int)Block.Crate] = Block.Wood;
            FallbackBlocks[(int)Block.StoneBrick] = Block.Stone;
        }


        public static Block GetFallbackBlock( Block block ) {
            return FallbackBlocks[(int)block];
        }


        [NotNull]
        public unsafe byte[] GetFallbackMap() {
            byte[] translatedBlocks = (byte[])Blocks.Clone();
            int volume = translatedBlocks.Length;
            fixed( byte* ptr = translatedBlocks ) {
                for( int i = 0; i < volume; i++ ) {
                    byte block = ptr[i];
                    if( block > (byte)MaxLegalBlockType ) {
                        ptr[i] = (byte)FallbackBlocks[block];
                    }
                }
            }
            return translatedBlocks;
        }
    }
}
