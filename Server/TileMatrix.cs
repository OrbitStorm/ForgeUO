/***************************************************************************
*                               TileMatrix.cs
*                            -------------------
*   begin                : May 1, 2002
*   copyright            : (C) The RunUO Software Team
*   email                : info@runuo.com
*
*   $Id: TileMatrix.cs 895 2012-07-31 07:07:44Z eos $
*
***************************************************************************/

/***************************************************************************
*
*   This program is free software; you can redistribute it and/or modify
*   it under the terms of the GNU General Public License as published by
*   the Free Software Foundation; either version 2 of the License, or
*   (at your option) any later version.
*
***************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;

namespace Server
{
    public class TileMatrix
    {
        private readonly StaticTile[][][][][] m_StaticTiles;
        private readonly LandTile[][][] m_LandTiles;

        private readonly LandTile[] m_InvalidLandBlock;
        private readonly StaticTile[][][] m_EmptyStaticBlock;

        private FileStream m_Map;
        private readonly UOPIndex m_MapIndex;

        private FileStream m_Index;
        private BinaryReader m_IndexReader;

        private FileStream m_Statics;

        private readonly int m_FileIndex;
        private readonly int m_BlockWidth;

        private readonly int m_BlockHeight;

        private readonly int m_Width;

        private readonly int m_Height;

        private readonly Map m_Owner;

        private readonly TileMatrixPatch m_Patch;
        private readonly int[][] m_StaticPatches;
        private readonly int[][] m_LandPatches;

        public Map Owner
        {
            get
            {
                return this.m_Owner;
            }
        }

        public TileMatrixPatch Patch
        {
            get
            {
                return this.m_Patch;
            }
        }

        public int BlockWidth
        {
            get
            {
                return this.m_BlockWidth;
            }
        }

        public int BlockHeight
        {
            get
            {
                return this.m_BlockHeight;
            }
        }

        public int Width
        {
            get
            {
                return this.m_Width;
            }
        }

        public int Height
        {
            get
            {
                return this.m_Height;
            }
        }

        public FileStream MapStream
        {
            get
            {
                return this.m_Map;
            }
            set
            {
                this.m_Map = value;
            }
        }

        public bool MapUOPPacked
        {
            get
            {
                return (this.m_MapIndex != null);
            }
        }

        public FileStream IndexStream
        {
            get
            {
                return this.m_Index;
            }
            set
            {
                this.m_Index = value;
            }
        }

        public FileStream DataStream
        {
            get
            {
                return this.m_Statics;
            }
            set
            {
                this.m_Statics = value;
            }
        }

        public BinaryReader IndexReader
        {
            get
            {
                return this.m_IndexReader;
            }
            set
            {
                this.m_IndexReader = value;
            }
        }

        public bool Exists
        {
            get
            {
                return (this.m_Map != null && this.m_Index != null && this.m_Statics != null);
            }
        }

        private static readonly List<TileMatrix> m_Instances = new List<TileMatrix>();
        private readonly List<TileMatrix> m_FileShare = new List<TileMatrix>();

        public TileMatrix(Map owner, int fileIndex, int mapID, int width, int height)
        {
            for (int i = 0; i < m_Instances.Count; ++i)
            {
                TileMatrix tm = m_Instances[i];

                if (tm.m_FileIndex == fileIndex)
                {
                    tm.m_FileShare.Add(this);
                    this.m_FileShare.Add(tm);
                }
            }

            m_Instances.Add(this);
            this.m_FileIndex = fileIndex;
            this.m_Width = width;
            this.m_Height = height;
            this.m_BlockWidth = width >> 3;
            this.m_BlockHeight = height >> 3;

            this.m_Owner = owner;

            if (fileIndex != 0x7F)
            {
                string mapPath = Core.FindDataFile("map{0}.mul", fileIndex);

                if (File.Exists(mapPath))
                {
                    this.m_Map = new FileStream(mapPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                }
                else
                {
                    mapPath = Core.FindDataFile("map{0}LegacyMUL.uop", fileIndex);

                    if (File.Exists(mapPath))
                    {
                        this.m_Map = new FileStream(mapPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        this.m_MapIndex = new UOPIndex(this.m_Map);
                    }
                }

                string indexPath = Core.FindDataFile("staidx{0}.mul", fileIndex);

                if (File.Exists(indexPath))
                {
                    this.m_Index = new FileStream(indexPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    this.m_IndexReader = new BinaryReader(this.m_Index);
                }

                string staticsPath = Core.FindDataFile("statics{0}.mul", fileIndex);

                if (File.Exists(staticsPath))
                    this.m_Statics = new FileStream(staticsPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }

            this.m_EmptyStaticBlock = new StaticTile[8][][];

            for (int i = 0; i < 8; ++i)
            {
                this.m_EmptyStaticBlock[i] = new StaticTile[8][];

                for (int j = 0; j < 8; ++j)
                    this.m_EmptyStaticBlock[i][j] = new StaticTile[0];
            }

            this.m_InvalidLandBlock = new LandTile[196];

            this.m_LandTiles = new LandTile[this.m_BlockWidth][][];
            this.m_StaticTiles = new StaticTile[this.m_BlockWidth][][][][];
            this.m_StaticPatches = new int[this.m_BlockWidth][];
            this.m_LandPatches = new int[this.m_BlockWidth][];

            this.m_Patch = new TileMatrixPatch(this, mapID);
        }

        public StaticTile[][][] EmptyStaticBlock
        {
            get
            {
                return this.m_EmptyStaticBlock;
            }
        }

        public void SetStaticBlock(int x, int y, StaticTile[][][] value)
        {
            if (x < 0 || y < 0 || x >= this.m_BlockWidth || y >= this.m_BlockHeight)
                return;

            if (this.m_StaticTiles[x] == null)
                this.m_StaticTiles[x] = new StaticTile[this.m_BlockHeight][][][];

            this.m_StaticTiles[x][y] = value;

            if (this.m_StaticPatches[x] == null)
                this.m_StaticPatches[x] = new int[(this.m_BlockHeight + 31) >> 5];

            this.m_StaticPatches[x][y >> 5] |= 1 << (y & 0x1F);
        }

        public StaticTile[][][] GetStaticBlock(int x, int y)
        {
            if (x < 0 || y < 0 || x >= this.m_BlockWidth || y >= this.m_BlockHeight || this.m_Statics == null || this.m_Index == null)
                return this.m_EmptyStaticBlock;

            if (this.m_StaticTiles[x] == null)
                this.m_StaticTiles[x] = new StaticTile[this.m_BlockHeight][][][];

            StaticTile[][][] tiles = this.m_StaticTiles[x][y];

            if (tiles == null)
            {
                for (int i = 0; tiles == null && i < this.m_FileShare.Count; ++i)
                {
                    TileMatrix shared = this.m_FileShare[i];

                    if (x >= 0 && x < shared.m_BlockWidth && y >= 0 && y < shared.m_BlockHeight)
                    {
                        StaticTile[][][][] theirTiles = shared.m_StaticTiles[x];

                        if (theirTiles != null)
                            tiles = theirTiles[y];

                        if (tiles != null)
                        {
                            int[] theirBits = shared.m_StaticPatches[x];

                            if (theirBits != null && (theirBits[y >> 5] & (1 << (y & 0x1F))) != 0)
                                tiles = null;
                        }
                    }
                }

                if (tiles == null)
                    tiles = this.ReadStaticBlock(x, y);

                this.m_StaticTiles[x][y] = tiles;
            }

            return tiles;
        }

        public StaticTile[] GetStaticTiles(int x, int y)
        {
            StaticTile[][][] tiles = this.GetStaticBlock(x >> 3, y >> 3);

            return tiles[x & 0x7][y & 0x7];
        }

        private static readonly TileList m_TilesList = new TileList();

        public StaticTile[] GetStaticTiles(int x, int y, bool multis)
        {
            StaticTile[][][] tiles = this.GetStaticBlock(x >> 3, y >> 3);

            if (multis)
            {
                IPooledEnumerable eable = this.m_Owner.GetMultiTilesAt(x, y);

                if (eable == Map.NullEnumerable.Instance)
                    return tiles[x & 0x7][y & 0x7];

                bool any = false;

                foreach (StaticTile[] multiTiles in eable)
                {
                    if (!any)
                        any = true;

                    m_TilesList.AddRange(multiTiles);
                }

                eable.Free();

                if (!any)
                    return tiles[x & 0x7][y & 0x7];

                m_TilesList.AddRange(tiles[x & 0x7][y & 0x7]);

                return m_TilesList.ToArray();
            }
            else
            {
                return tiles[x & 0x7][y & 0x7];
            }
        }

        public void SetLandBlock(int x, int y, LandTile[] value)
        {
            if (x < 0 || y < 0 || x >= this.m_BlockWidth || y >= this.m_BlockHeight)
                return;

            if (this.m_LandTiles[x] == null)
                this.m_LandTiles[x] = new LandTile[this.m_BlockHeight][];

            this.m_LandTiles[x][y] = value;

            if (this.m_LandPatches[x] == null)
                this.m_LandPatches[x] = new int[(this.m_BlockHeight + 31) >> 5];

            this.m_LandPatches[x][y >> 5] |= 1 << (y & 0x1F);
        }

        public LandTile[] GetLandBlock(int x, int y)
        {
            if (x < 0 || y < 0 || x >= this.m_BlockWidth || y >= this.m_BlockHeight || this.m_Map == null)
                return this.m_InvalidLandBlock;

            if (this.m_LandTiles[x] == null)
                this.m_LandTiles[x] = new LandTile[this.m_BlockHeight][];

            LandTile[] tiles = this.m_LandTiles[x][y];

            if (tiles == null)
            {
                for (int i = 0; tiles == null && i < this.m_FileShare.Count; ++i)
                {
                    TileMatrix shared = this.m_FileShare[i];

                    if (x >= 0 && x < shared.m_BlockWidth && y >= 0 && y < shared.m_BlockHeight)
                    {
                        LandTile[][] theirTiles = shared.m_LandTiles[x];

                        if (theirTiles != null)
                            tiles = theirTiles[y];

                        if (tiles != null)
                        {
                            int[] theirBits = shared.m_LandPatches[x];

                            if (theirBits != null && (theirBits[y >> 5] & (1 << (y & 0x1F))) != 0)
                                tiles = null;
                        }
                    }
                }

                if (tiles == null)
                    tiles = this.ReadLandBlock(x, y);

                this.m_LandTiles[x][y] = tiles;
            }

            return tiles;
        }

        public LandTile GetLandTile(int x, int y)
        {
            LandTile[] tiles = this.GetLandBlock(x >> 3, y >> 3);

            return tiles[((y & 0x7) << 3) + (x & 0x7)];
        }

        private static TileList[][] m_Lists;

        private static StaticTile[] m_TileBuffer = new StaticTile[128];

        private unsafe StaticTile[][][] ReadStaticBlock(int x, int y)
        {
            try
            {
                this.m_IndexReader.BaseStream.Seek(((x * this.m_BlockHeight) + y) * 12, SeekOrigin.Begin);

                int lookup = this.m_IndexReader.ReadInt32();
                int length = this.m_IndexReader.ReadInt32();

                if (lookup < 0 || length <= 0)
                {
                    return this.m_EmptyStaticBlock;
                }
                else
                {
                    int count = length / 7;

                    this.m_Statics.Seek(lookup, SeekOrigin.Begin);

                    if (m_TileBuffer.Length < count)
                        m_TileBuffer = new StaticTile[count];

                    StaticTile[] staTiles = m_TileBuffer;//new StaticTile[tileCount];

                    fixed (StaticTile *pTiles = staTiles)
                    {
                        #if !MONO
                        NativeReader.Read(this.m_Statics.SafeFileHandle.DangerousGetHandle(), pTiles, length);
                        #else
						NativeReader.Read( m_Statics.Handle, pTiles, length );
                        #endif
                        if (m_Lists == null)
                        {
                            m_Lists = new TileList[8][];

                            for (int i = 0; i < 8; ++i)
                            {
                                m_Lists[i] = new TileList[8];

                                for (int j = 0; j < 8; ++j)
                                    m_Lists[i][j] = new TileList();
                            }
                        }

                        TileList[][] lists = m_Lists;

                        StaticTile *pCur = pTiles, pEnd = pTiles + count;

                        while (pCur < pEnd)
                        {
                            lists[pCur->m_X & 0x7][pCur->m_Y & 0x7].Add(pCur->m_ID, pCur->m_Z);
                            pCur = pCur + 1;
                        }

                        StaticTile[][][] tiles = new StaticTile[8][][];

                        for (int i = 0; i < 8; ++i)
                        {
                            tiles[i] = new StaticTile[8][];

                            for (int j = 0; j < 8; ++j)
                                tiles[i][j] = lists[i][j].ToArray();
                        }

                        return tiles;
                    }
                }
            }
            catch (EndOfStreamException)
            {
                if (DateTime.Now >= this.m_NextStaticWarning)
                {
                    Console.WriteLine("Warning: Static EOS for {0} ({1}, {2})", this.m_Owner, x, y);
                    this.m_NextStaticWarning = DateTime.Now + TimeSpan.FromMinutes(1.0);
                }

                return this.m_EmptyStaticBlock;
            }
        }

        private DateTime m_NextStaticWarning;
        private DateTime m_NextLandWarning;

        public void Force()
        {
            if (ScriptCompiler.Assemblies == null || ScriptCompiler.Assemblies.Length == 0)
                throw new Exception();
        }

        private unsafe LandTile[] ReadLandBlock(int x, int y)
        {
            try
            {
                int offset = ((x * this.m_BlockHeight) + y) * 196 + 4;

                if (this.m_MapIndex != null)
                    offset = this.m_MapIndex.Lookup(offset);

                this.m_Map.Seek(offset, SeekOrigin.Begin);

                LandTile[] tiles = new LandTile[64];

                fixed (LandTile *pTiles = tiles)
                {
                    #if !MONO
                    NativeReader.Read(this.m_Map.SafeFileHandle.DangerousGetHandle(), pTiles, 192);
                    #else
					NativeReader.Read( m_Map.Handle, pTiles, 192 );
                    #endif
                }

                return tiles;
            }
            catch
            {
                if (DateTime.Now >= this.m_NextLandWarning)
                {
                    Console.WriteLine("Warning: Land EOS for {0} ({1}, {2})", this.m_Owner, x, y);
                    this.m_NextLandWarning = DateTime.Now + TimeSpan.FromMinutes(1.0);
                }

                return this.m_InvalidLandBlock;
            }
        }

        public void Dispose()
        {
            if (this.m_MapIndex != null)
                this.m_MapIndex.Close();
            else if (this.m_Map != null)
                this.m_Map.Close();

            if (this.m_Statics != null)
                this.m_Statics.Close();

            if (this.m_IndexReader != null)
                this.m_IndexReader.Close();
        }
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
    public struct LandTile
    {
        internal short m_ID;
        internal sbyte m_Z;

        public int ID
        {
            get
            {
                return this.m_ID;
            }
        }

        public int Z
        {
            get
            {
                return this.m_Z;
            }
            set
            {
                this.m_Z = (sbyte)value;
            }
        }

        public int Height
        {
            get
            {
                return 0;
            }
        }

        public bool Ignored
        {
            get
            {
                return (this.m_ID == 2 || this.m_ID == 0x1DB || (this.m_ID >= 0x1AE && this.m_ID <= 0x1B5));
            }
        }

        public LandTile(short id, sbyte z)
        {
            this.m_ID = id;
            this.m_Z = z;
        }

        public void Set(short id, sbyte z)
        {
            this.m_ID = id;
            this.m_Z = z;
        }
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
    public struct StaticTile
    {
        internal ushort m_ID;
        internal byte m_X;
        internal byte m_Y;
        internal sbyte m_Z;
        internal short m_Hue;

        public int ID
        {
            get
            {
                return this.m_ID;
            }
        }

        public int X
        {
            get
            {
                return this.m_X;
            }
            set
            {
                this.m_X = (byte)value;
            }
        }

        public int Y
        {
            get
            {
                return this.m_Y;
            }
            set
            {
                this.m_Y = (byte)value;
            }
        }

        public int Z
        {
            get
            {
                return this.m_Z;
            }
            set
            {
                this.m_Z = (sbyte)value;
            }
        }

        public int Hue
        {
            get
            {
                return this.m_Hue;
            }
            set
            {
                this.m_Hue = (short)value;
            }
        }

        public int Height
        {
            get
            {
                return TileData.ItemTable[this.m_ID & TileData.MaxItemValue].Height;
            }
        }

        public StaticTile(ushort id, sbyte z)
        {
            this.m_ID = id;
            this.m_Z = z;

            this.m_X = 0;
            this.m_Y = 0;
            this.m_Hue = 0;
        }

        public StaticTile(ushort id, byte x, byte y, sbyte z, short hue)
        {
            this.m_ID = id;
            this.m_X = x;
            this.m_Y = y;
            this.m_Z = z;
            this.m_Hue = hue;
        }

        public void Set(ushort id, sbyte z)
        {
            this.m_ID = id;
            this.m_Z = z;
        }

        public void Set(ushort id, byte x, byte y, sbyte z, short hue)
        {
            this.m_ID = id;
            this.m_X = x;
            this.m_Y = y;
            this.m_Z = z;
            this.m_Hue = hue;
        }
    }

    public class UOPIndex
    {
        private class UOPEntry : IComparable<UOPEntry>
        {
            public int m_Offset;
            public readonly int m_Length;
            public int m_Order;

            public UOPEntry(int offset, int length)
            {
                this.m_Offset = offset;
                this.m_Length = length;
                this.m_Order = 0;
            }

            public int CompareTo(UOPEntry other)
            {
                return this.m_Order.CompareTo(other.m_Order);
            }
        }

        private class OffsetComparer : IComparer<UOPEntry>
        {
            public static readonly IComparer<UOPEntry> Instance = new OffsetComparer();

            public OffsetComparer()
            {
            }

            public int Compare(UOPEntry x, UOPEntry y)
            {
                return x.m_Offset.CompareTo(y.m_Offset);
            }
        }

        private readonly BinaryReader m_Reader;
        private readonly int m_Length;
        private readonly int m_Version;
        private readonly UOPEntry[] m_Entries;

        public int Version
        {
            get
            {
                return this.m_Version;
            }
        }

        public UOPIndex(FileStream stream)
        {
            this.m_Reader = new BinaryReader(stream);
            this.m_Length = (int)stream.Length;

            if (this.m_Reader.ReadInt32() != 0x50594D)
                throw new ArgumentException("Invalid UOP file.");

            this.m_Version = this.m_Reader.ReadInt32();
            this.m_Reader.ReadInt32();
            int nextTable = this.m_Reader.ReadInt32();

            List<UOPEntry> entries = new List<UOPEntry>();

            do
            {
                stream.Seek(nextTable, SeekOrigin.Begin);
                int count = this.m_Reader.ReadInt32();
                nextTable = this.m_Reader.ReadInt32();
                this.m_Reader.ReadInt32();

                for (int i = 0; i < count; ++i)
                {
                    int offset = this.m_Reader.ReadInt32();

                    if (offset == 0)
                    {
                        stream.Seek(30, SeekOrigin.Current);
                        continue;
                    }

                    this.m_Reader.ReadInt64();
                    int length = this.m_Reader.ReadInt32();

                    entries.Add(new UOPEntry(offset, length));

                    stream.Seek(18, SeekOrigin.Current);
                }
            }
            while (nextTable != 0 && nextTable < this.m_Length);

            entries.Sort(OffsetComparer.Instance);

            for (int i = 0; i < entries.Count; ++i)
            {
                stream.Seek(entries[i].m_Offset + 2, SeekOrigin.Begin);

                int dataOffset = this.m_Reader.ReadInt16();
                entries[i].m_Offset += 4 + dataOffset;

                stream.Seek(dataOffset, SeekOrigin.Current);
                entries[i].m_Order = this.m_Reader.ReadInt32();
            }

            entries.Sort();
            this.m_Entries = entries.ToArray();
        }

        public int Lookup(int offset)
        {
            int total = 0;

            for (int i = 0; i < this.m_Entries.Length; ++i)
            {
                int newTotal = total + this.m_Entries[i].m_Length;

                if (offset < newTotal)
                    return this.m_Entries[i].m_Offset + (offset - total);

                total = newTotal;
            }

            return this.m_Length;
        }

        public void Close()
        {
            this.m_Reader.Close();
        }
    }
}