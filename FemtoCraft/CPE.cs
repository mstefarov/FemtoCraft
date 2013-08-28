using System;
using System.IO;
using System.Text;
using JetBrains.Annotations;

namespace FemtoCraft {
    sealed partial class Player {
        const string CustomBlocksExtName = "CustomBlocks";
        const int CustomBlocksExtVersion = 1;
        const byte CustomBlocksLevel = 1;

        // Note: if more levels are added, change UsesCustomBlocks from bool to int
        public bool UsesCustomBlocks { get; private set; }
        public string ClientName { get; private set; }

        bool NegotiateProtocolExtension() {
            // write our ExtInfo and ExtEntry packets
            writer.Write( Packet.MakeExtInfo( 1 ).Bytes );
            writer.Write( Packet.MakeExtEntry( CustomBlocksExtName, CustomBlocksExtVersion ).Bytes );

            // wait for client to reply with its ExtInfo
            OpCode clientReplyCode = reader.ReadOpCode();
            if( clientReplyCode != OpCode.ExtInfo ) {
                Logger.LogWarning( "Player {0} from {1}: Unexpected ExtInfo reply ({2})",
                                   Name, IP, clientReplyCode );
                return false;
            }
            ClientName = reader.ReadString();
            int expectedEntries = reader.ReadInt16();

            // wait for client to send its ExtEntries
            bool sendCustomBlockPacket = false;
            for( int i = 0; i < expectedEntries; i++ ) {
                OpCode clientCode = reader.ReadOpCode();
                if( clientCode != OpCode.ExtEntry ) {
                    Logger.LogWarning( "Player {0} from {1}: Unexpected ExtEntry reply ({2})",
                                       Name, IP, clientReplyCode );
                    return false;
                }
                string extName = reader.ReadString();
                int extVersion = reader.ReadInt32();
                if( extName == CustomBlocksExtName && extVersion == CustomBlocksExtVersion ) {
                    // Hooray, client supports custom blocks, like we do!
                    sendCustomBlockPacket = true;
                }
            }

            // if client also supports CustomBlockSupportLevel, figure out what level to use
            if( sendCustomBlockPacket ) {
                writer.Write( Packet.MakeCustomBlockSupportLevel( CustomBlocksLevel ).Bytes );
                OpCode customBlockSupportLevelReply = reader.ReadOpCode();
                if( customBlockSupportLevelReply != OpCode.CustomBlockSupportLevel ) {
                    Logger.LogWarning( "Player {0} from {1}: Unexpected CustomBlockSupportLevel reply ({2})",
                                       Name, IP, clientReplyCode );
                    return false;
                }
                byte clientLevel = reader.ReadByte();
                UsesCustomBlocks = (clientLevel >= CustomBlocksLevel);
            }
            return true;
        }


        void ProcessOutgoingSetBlock( ref Packet packet ) {
            if( packet.Bytes[7] > (byte)Map.MaxLegalBlockType && !UsesCustomBlocks ) {
                packet.Bytes[7] = (byte)Map.TranslateBlock( (Block)packet.Bytes[7] );
            }
        }
    }


    partial struct Packet {
        public static Packet MakeExtInfo( short extCount ) {
            Packet packet = new Packet( OpCode.ExtInfo );
            Encoding.ASCII.GetBytes( Server.VersionString.PadRight( 64 ), 0, 64, packet.Bytes, 1 );
            ToNetOrder( extCount, packet.Bytes, 65 );
            return packet;
        }

        public static Packet MakeExtEntry(string name, int version) {
            Packet packet = new Packet( OpCode.ExtEntry );
            Encoding.ASCII.GetBytes( Config.ServerName.PadRight( 64 ), 0, 64, packet.Bytes, 1 );
            ToNetOrder( version, packet.Bytes, 65 );
            return packet;
        }

        public static Packet MakeCustomBlockSupportLevel( byte level ) {
            Packet packet = new Packet( OpCode.CustomBlockSupportLevel );
            packet.Bytes[1] = level;
            return packet;
        }
    }


    sealed partial class Map {
        public const Block MaxCustomBlockType = Block.StoneBrick;
        readonly static Block[] BlockTranslation = new Block[256];


        static void DefineTranslations() {
            for( int i = 0; i <= (int)Block.Obsidian; i++ ) {
                BlockTranslation[i] = (Block)i;
            }
            BlockTranslation[(int)Block.CobbleSlab] = Block.Slab;
            BlockTranslation[(int)Block.SpiderWeb] = Block.Sapling;
            BlockTranslation[(int)Block.Sandstone] = Block.Sand;
            BlockTranslation[(int)Block.Snow] = Block.Air;
            BlockTranslation[(int)Block.Fire] = Block.StillLava;
            BlockTranslation[(int)Block.LightPink] = Block.Pink;
            BlockTranslation[(int)Block.DarkGreen] = Block.Green;
            BlockTranslation[(int)Block.Brown] = Block.Dirt;
            BlockTranslation[(int)Block.DarkBlue] = Block.Blue;
            BlockTranslation[(int)Block.Turquoise] = Block.Cyan;
            BlockTranslation[(int)Block.Ice] = Block.Glass;
            BlockTranslation[(int)Block.Tile] = Block.Iron;
            BlockTranslation[(int)Block.Magma] = Block.Obsidian;
            BlockTranslation[(int)Block.Pillar] = Block.White;
            BlockTranslation[(int)Block.Crate] = Block.Wood;
            BlockTranslation[(int)Block.StoneBrick] = Block.Stone;
        }


        public static Block TranslateBlock( Block block ) {
            return BlockTranslation[(int)block];
        }


        public unsafe byte[] TranslateMap() {
            byte[] translatedBlocks = (byte[])Blocks.Clone();
            fixed( byte* ptr = translatedBlocks ) {
                if( *ptr > (byte)MaxLegalBlockType ) {
                    *ptr = (byte)BlockTranslation[*ptr];
                }
            }
            return translatedBlocks;
        }
    }
}
