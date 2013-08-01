/***************************************************************************
*                               MultiData.cs
*                            -------------------
*   begin                : May 1, 2002
*   copyright            : (C) The RunUO Software Team
*   email                : info@runuo.com
*
*   $Id: MultiData.cs 644 2010-12-23 09:18:45Z asayre $
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
using System.IO;

namespace Server
{
    public struct MultiTileEntry
    {
        public ushort m_ItemID;
        public short m_OffsetX, m_OffsetY, m_OffsetZ;
        public int m_Flags;
        public MultiTileEntry(ushort itemID, short xOffset, short yOffset, short zOffset, int flags)
        {
            this.m_ItemID = itemID;
            this.m_OffsetX = xOffset;
            this.m_OffsetY = yOffset;
            this.m_OffsetZ = zOffset;
            this.m_Flags = flags;
        }
    }

    public static class MultiData
    {
        private static MultiComponentList[] m_Components;
        private static FileStream m_Index, m_Stream;
        private static BinaryReader m_IndexReader, m_StreamReader;
        static MultiData()
        {
            string idxPath = Core.FindDataFile("multi.idx");
            string mulPath = Core.FindDataFile("multi.mul");

            if (File.Exists(idxPath) && File.Exists(mulPath))
            {
                m_Index = new FileStream(idxPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                m_IndexReader = new BinaryReader(m_Index);

                m_Stream = new FileStream(mulPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                m_StreamReader = new BinaryReader(m_Stream);

                m_Components = new MultiComponentList[(int)(m_Index.Length / 12)];

                string vdPath = Core.FindDataFile("verdata.mul");

                if (File.Exists(vdPath))
                {
                    using (FileStream fs = new FileStream(vdPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        BinaryReader bin = new BinaryReader(fs);

                        int count = bin.ReadInt32();

                        for (int i = 0; i < count; ++i)
                        {
                            int file = bin.ReadInt32();
                            int index = bin.ReadInt32();
                            int lookup = bin.ReadInt32();
                            int length = bin.ReadInt32();
                            int extra = bin.ReadInt32();

                            if (file == 14 && index >= 0 && index < m_Components.Length && lookup >= 0 && length > 0)
                            {
                                bin.BaseStream.Seek(lookup, SeekOrigin.Begin);

                                m_Components[index] = new MultiComponentList(bin, length / 12);

                                bin.BaseStream.Seek(24 + (i * 20), SeekOrigin.Begin);
                            }
                        }

                        bin.Close();
                    }
                }
            }
            else
            {
                Console.WriteLine("Warning: Multi data files not found");

                m_Components = new MultiComponentList[0];
            }
        }

        public static MultiComponentList GetComponents(int multiID)
        {
            MultiComponentList mcl;

            if (multiID >= 0 && multiID < m_Components.Length)
            {
                mcl = m_Components[multiID];

                if (mcl == null)
                    m_Components[multiID] = mcl = Load(multiID);
            }
            else
            {
                mcl = MultiComponentList.Empty;
            }

            return mcl;
        }

        public static MultiComponentList Load(int multiID)
        {
            try
            {
                m_IndexReader.BaseStream.Seek(multiID * 12, SeekOrigin.Begin);

                int lookup = m_IndexReader.ReadInt32();
                int length = m_IndexReader.ReadInt32();

                if (lookup < 0 || length <= 0)
                    return MultiComponentList.Empty;

                m_StreamReader.BaseStream.Seek(lookup, SeekOrigin.Begin);

                return new MultiComponentList(m_StreamReader, length / (MultiComponentList.PostHSFormat ? 16 : 12));
            }
            catch
            {
                return MultiComponentList.Empty;
            }
        }
    }

    public sealed class MultiComponentList
    {
        public static readonly MultiComponentList Empty = new MultiComponentList();
        private static bool _PostHSFormat = false;
        private readonly Point2D m_Center;
        private Point2D m_Min, m_Max;
        private int m_Width, m_Height;
        private StaticTile[][][] m_Tiles;
        private MultiTileEntry[] m_List;
        public MultiComponentList(MultiComponentList toCopy)
        {
            this.m_Min = toCopy.m_Min;
            this.m_Max = toCopy.m_Max;

            this.m_Center = toCopy.m_Center;

            this.m_Width = toCopy.m_Width;
            this.m_Height = toCopy.m_Height;

            this.m_Tiles = new StaticTile[this.m_Width][][];

            for (int x = 0; x < this.m_Width; ++x)
            {
                this.m_Tiles[x] = new StaticTile[this.m_Height][];

                for (int y = 0; y < this.m_Height; ++y)
                {
                    this.m_Tiles[x][y] = new StaticTile[toCopy.m_Tiles[x][y].Length];

                    for (int i = 0; i < this.m_Tiles[x][y].Length; ++i)
                        this.m_Tiles[x][y][i] = toCopy.m_Tiles[x][y][i];
                }
            }

            this.m_List = new MultiTileEntry[toCopy.m_List.Length];

            for (int i = 0; i < this.m_List.Length; ++i)
                this.m_List[i] = toCopy.m_List[i];
        }

        public MultiComponentList(GenericReader reader)
        {
            int version = reader.ReadInt();

            this.m_Min = reader.ReadPoint2D();
            this.m_Max = reader.ReadPoint2D();
            this.m_Center = reader.ReadPoint2D();
            this.m_Width = reader.ReadInt();
            this.m_Height = reader.ReadInt();

            int length = reader.ReadInt();

            MultiTileEntry[] allTiles = this.m_List = new MultiTileEntry[length];

            if (version == 0)
            {
                for (int i = 0; i < length; ++i)
                {
                    int id = reader.ReadShort();
                    if (id >= 0x4000)
                        id -= 0x4000;

                    allTiles[i].m_ItemID = (ushort)id;
                    allTiles[i].m_OffsetX = reader.ReadShort();
                    allTiles[i].m_OffsetY = reader.ReadShort();
                    allTiles[i].m_OffsetZ = reader.ReadShort();
                    allTiles[i].m_Flags = reader.ReadInt();
                }
            }
            else
            {
                for (int i = 0; i < length; ++i)
                {
                    allTiles[i].m_ItemID = reader.ReadUShort();
                    allTiles[i].m_OffsetX = reader.ReadShort();
                    allTiles[i].m_OffsetY = reader.ReadShort();
                    allTiles[i].m_OffsetZ = reader.ReadShort();
                    allTiles[i].m_Flags = reader.ReadInt();
                }
            }

            TileList[][] tiles = new TileList[this.m_Width][];
            this.m_Tiles = new StaticTile[this.m_Width][][];

            for (int x = 0; x < this.m_Width; ++x)
            {
                tiles[x] = new TileList[this.m_Height];
                this.m_Tiles[x] = new StaticTile[this.m_Height][];

                for (int y = 0; y < this.m_Height; ++y)
                    tiles[x][y] = new TileList();
            }

            for (int i = 0; i < allTiles.Length; ++i)
            {
                if (i == 0 || allTiles[i].m_Flags != 0)
                {
                    int xOffset = allTiles[i].m_OffsetX + this.m_Center.m_X;
                    int yOffset = allTiles[i].m_OffsetY + this.m_Center.m_Y;

                    tiles[xOffset][yOffset].Add((ushort)allTiles[i].m_ItemID, (sbyte)allTiles[i].m_OffsetZ);
                }
            }

            for (int x = 0; x < this.m_Width; ++x)
                for (int y = 0; y < this.m_Height; ++y)
                    this.m_Tiles[x][y] = tiles[x][y].ToArray();
        }

        public MultiComponentList(BinaryReader reader, int count)
        {
            MultiTileEntry[] allTiles = this.m_List = new MultiTileEntry[count];

            for (int i = 0; i < count; ++i)
            {
                allTiles[i].m_ItemID = reader.ReadUInt16();
                allTiles[i].m_OffsetX = reader.ReadInt16();
                allTiles[i].m_OffsetY = reader.ReadInt16();
                allTiles[i].m_OffsetZ = reader.ReadInt16();
                allTiles[i].m_Flags = reader.ReadInt32();

                if (_PostHSFormat)
                    reader.ReadInt32(); // ??

                MultiTileEntry e = allTiles[i];

                if (i == 0 || e.m_Flags != 0)
                {
                    if (e.m_OffsetX < this.m_Min.m_X)
                        this.m_Min.m_X = e.m_OffsetX;

                    if (e.m_OffsetY < this.m_Min.m_Y)
                        this.m_Min.m_Y = e.m_OffsetY;

                    if (e.m_OffsetX > this.m_Max.m_X)
                        this.m_Max.m_X = e.m_OffsetX;

                    if (e.m_OffsetY > this.m_Max.m_Y)
                        this.m_Max.m_Y = e.m_OffsetY;
                }
            }

            this.m_Center = new Point2D(-this.m_Min.m_X, -this.m_Min.m_Y);
            this.m_Width = (this.m_Max.m_X - this.m_Min.m_X) + 1;
            this.m_Height = (this.m_Max.m_Y - this.m_Min.m_Y) + 1;

            TileList[][] tiles = new TileList[this.m_Width][];
            this.m_Tiles = new StaticTile[this.m_Width][][];

            for (int x = 0; x < this.m_Width; ++x)
            {
                tiles[x] = new TileList[this.m_Height];
                this.m_Tiles[x] = new StaticTile[this.m_Height][];

                for (int y = 0; y < this.m_Height; ++y)
                    tiles[x][y] = new TileList();
            }

            for (int i = 0; i < allTiles.Length; ++i)
            {
                if (i == 0 || allTiles[i].m_Flags != 0)
                {
                    int xOffset = allTiles[i].m_OffsetX + this.m_Center.m_X;
                    int yOffset = allTiles[i].m_OffsetY + this.m_Center.m_Y;

                    tiles[xOffset][yOffset].Add((ushort)allTiles[i].m_ItemID, (sbyte)allTiles[i].m_OffsetZ);
                }
            }

            for (int x = 0; x < this.m_Width; ++x)
                for (int y = 0; y < this.m_Height; ++y)
                    this.m_Tiles[x][y] = tiles[x][y].ToArray();
        }

        private MultiComponentList()
        {
            this.m_Tiles = new StaticTile[0][][];
            this.m_List = new MultiTileEntry[0];
        }

        public static bool PostHSFormat
        {
            get
            {
                return _PostHSFormat;
            }
            set
            {
                _PostHSFormat = value;
            }
        }
        public Point2D Min
        {
            get
            {
                return this.m_Min;
            }
        }
        public Point2D Max
        {
            get
            {
                return this.m_Max;
            }
        }
        public Point2D Center
        {
            get
            {
                return this.m_Center;
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
        public StaticTile[][][] Tiles
        {
            get
            {
                return this.m_Tiles;
            }
        }
        public MultiTileEntry[] List
        {
            get
            {
                return this.m_List;
            }
        }
        public void Add(int itemID, int x, int y, int z)
        {
            int vx = x + this.m_Center.m_X;
            int vy = y + this.m_Center.m_Y;

            if (vx >= 0 && vx < this.m_Width && vy >= 0 && vy < this.m_Height)
            {
                StaticTile[] oldTiles = this.m_Tiles[vx][vy];

                for (int i = oldTiles.Length - 1; i >= 0; --i)
                {
                    ItemData data = TileData.ItemTable[itemID & TileData.MaxItemValue];

                    if (oldTiles[i].Z == z && (oldTiles[i].Height > 0 == data.Height > 0))
                    {
                        bool newIsRoof = (data.Flags & TileFlag.Roof) != 0;
                        bool oldIsRoof = (TileData.ItemTable[oldTiles[i].ID & TileData.MaxItemValue].Flags & TileFlag.Roof) != 0;

                        if (newIsRoof == oldIsRoof)
                            this.Remove(oldTiles[i].ID, x, y, z);
                    }
                }

                oldTiles = this.m_Tiles[vx][vy];

                StaticTile[] newTiles = new StaticTile[oldTiles.Length + 1];

                for (int i = 0; i < oldTiles.Length; ++i)
                    newTiles[i] = oldTiles[i];

                newTiles[oldTiles.Length] = new StaticTile((ushort)itemID, (sbyte)z);

                this.m_Tiles[vx][vy] = newTiles;

                MultiTileEntry[] oldList = this.m_List;
                MultiTileEntry[] newList = new MultiTileEntry[oldList.Length + 1];

                for (int i = 0; i < oldList.Length; ++i)
                    newList[i] = oldList[i];

                newList[oldList.Length] = new MultiTileEntry((ushort)itemID, (short)x, (short)y, (short)z, 1);

                this.m_List = newList;

                if (x < this.m_Min.m_X)
                    this.m_Min.m_X = x;

                if (y < this.m_Min.m_Y)
                    this.m_Min.m_Y = y;

                if (x > this.m_Max.m_X)
                    this.m_Max.m_X = x;

                if (y > this.m_Max.m_Y)
                    this.m_Max.m_Y = y;
            }
        }

        public void RemoveXYZH(int x, int y, int z, int minHeight)
        {
            int vx = x + this.m_Center.m_X;
            int vy = y + this.m_Center.m_Y;

            if (vx >= 0 && vx < this.m_Width && vy >= 0 && vy < this.m_Height)
            {
                StaticTile[] oldTiles = this.m_Tiles[vx][vy];

                for (int i = 0; i < oldTiles.Length; ++i)
                {
                    StaticTile tile = oldTiles[i];

                    if (tile.Z == z && tile.Height >= minHeight)
                    {
                        StaticTile[] newTiles = new StaticTile[oldTiles.Length - 1];

                        for (int j = 0; j < i; ++j)
                            newTiles[j] = oldTiles[j];

                        for (int j = i + 1; j < oldTiles.Length; ++j)
                            newTiles[j - 1] = oldTiles[j];

                        this.m_Tiles[vx][vy] = newTiles;

                        break;
                    }
                }

                MultiTileEntry[] oldList = this.m_List;

                for (int i = 0; i < oldList.Length; ++i)
                {
                    MultiTileEntry tile = oldList[i];

                    if (tile.m_OffsetX == (short)x && tile.m_OffsetY == (short)y && tile.m_OffsetZ == (short)z && TileData.ItemTable[tile.m_ItemID & TileData.MaxItemValue].Height >= minHeight)
                    {
                        MultiTileEntry[] newList = new MultiTileEntry[oldList.Length - 1];

                        for (int j = 0; j < i; ++j)
                            newList[j] = oldList[j];

                        for (int j = i + 1; j < oldList.Length; ++j)
                            newList[j - 1] = oldList[j];

                        this.m_List = newList;

                        break;
                    }
                }
            }
        }

        public void Remove(int itemID, int x, int y, int z)
        {
            int vx = x + this.m_Center.m_X;
            int vy = y + this.m_Center.m_Y;

            if (vx >= 0 && vx < this.m_Width && vy >= 0 && vy < this.m_Height)
            {
                StaticTile[] oldTiles = this.m_Tiles[vx][vy];

                for (int i = 0; i < oldTiles.Length; ++i)
                {
                    StaticTile tile = oldTiles[i];

                    if (tile.ID == itemID && tile.Z == z)
                    {
                        StaticTile[] newTiles = new StaticTile[oldTiles.Length - 1];

                        for (int j = 0; j < i; ++j)
                            newTiles[j] = oldTiles[j];

                        for (int j = i + 1; j < oldTiles.Length; ++j)
                            newTiles[j - 1] = oldTiles[j];

                        this.m_Tiles[vx][vy] = newTiles;

                        break;
                    }
                }

                MultiTileEntry[] oldList = this.m_List;

                for (int i = 0; i < oldList.Length; ++i)
                {
                    MultiTileEntry tile = oldList[i];

                    if (tile.m_ItemID == itemID && tile.m_OffsetX == (short)x && tile.m_OffsetY == (short)y && tile.m_OffsetZ == (short)z)
                    {
                        MultiTileEntry[] newList = new MultiTileEntry[oldList.Length - 1];

                        for (int j = 0; j < i; ++j)
                            newList[j] = oldList[j];

                        for (int j = i + 1; j < oldList.Length; ++j)
                            newList[j - 1] = oldList[j];

                        this.m_List = newList;

                        break;
                    }
                }
            }
        }

        public void Resize(int newWidth, int newHeight)
        {
            int oldWidth = this.m_Width, oldHeight = this.m_Height;
            StaticTile[][][] oldTiles = this.m_Tiles;

            int totalLength = 0;

            StaticTile[][][] newTiles = new StaticTile[newWidth][][];

            for (int x = 0; x < newWidth; ++x)
            {
                newTiles[x] = new StaticTile[newHeight][];

                for (int y = 0; y < newHeight; ++y)
                {
                    if (x < oldWidth && y < oldHeight)
                        newTiles[x][y] = oldTiles[x][y];
                    else
                        newTiles[x][y] = new StaticTile[0];

                    totalLength += newTiles[x][y].Length;
                }
            }

            this.m_Tiles = newTiles;
            this.m_List = new MultiTileEntry[totalLength];
            this.m_Width = newWidth;
            this.m_Height = newHeight;

            this.m_Min = Point2D.Zero;
            this.m_Max = Point2D.Zero;

            int index = 0;

            for (int x = 0; x < newWidth; ++x)
            {
                for (int y = 0; y < newHeight; ++y)
                {
                    StaticTile[] tiles = newTiles[x][y];

                    for (int i = 0; i < tiles.Length; ++i)
                    {
                        StaticTile tile = tiles[i];

                        int vx = x - this.m_Center.X;
                        int vy = y - this.m_Center.Y;

                        if (vx < this.m_Min.m_X)
                            this.m_Min.m_X = vx;

                        if (vy < this.m_Min.m_Y)
                            this.m_Min.m_Y = vy;

                        if (vx > this.m_Max.m_X)
                            this.m_Max.m_X = vx;

                        if (vy > this.m_Max.m_Y)
                            this.m_Max.m_Y = vy;

                        this.m_List[index++] = new MultiTileEntry((ushort)tile.ID, (short)vx, (short)vy, (short)tile.Z, 1);
                    }
                }
            }
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write((int)1); // version;

            writer.Write(this.m_Min);
            writer.Write(this.m_Max);
            writer.Write(this.m_Center);

            writer.Write((int)this.m_Width);
            writer.Write((int)this.m_Height);

            writer.Write((int)this.m_List.Length);

            for (int i = 0; i < this.m_List.Length; ++i)
            {
                MultiTileEntry ent = this.m_List[i];

                writer.Write((ushort)ent.m_ItemID);
                writer.Write((short)ent.m_OffsetX);
                writer.Write((short)ent.m_OffsetY);
                writer.Write((short)ent.m_OffsetZ);
                writer.Write((int)ent.m_Flags);
            }
        }
    }
}