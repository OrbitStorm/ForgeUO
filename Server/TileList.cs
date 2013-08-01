using System;

namespace Server
{
    public class TileList
    {
        private static readonly StaticTile[] m_EmptyTiles = new StaticTile[0];
        private StaticTile[] m_Tiles;
        private int m_Count;
        public TileList()
        {
            this.m_Tiles = new StaticTile[8];
            this.m_Count = 0;
        }

        public int Count
        {
            get
            {
                return this.m_Count;
            }
        }
        public void AddRange(StaticTile[] tiles)
        {
            if ((this.m_Count + tiles.Length) > this.m_Tiles.Length)
            {
                StaticTile[] old = this.m_Tiles;
                this.m_Tiles = new StaticTile[(this.m_Count + tiles.Length) * 2];

                for (int i = 0; i < old.Length; ++i)
                    this.m_Tiles[i] = old[i];
            }

            for (int i = 0; i < tiles.Length; ++i)
                this.m_Tiles[this.m_Count++] = tiles[i];
        }

        public void Add(ushort id, sbyte z)
        {
            if ((this.m_Count + 1) > this.m_Tiles.Length)
            {
                StaticTile[] old = this.m_Tiles;
                this.m_Tiles = new StaticTile[old.Length * 2];

                for (int i = 0; i < old.Length; ++i)
                    this.m_Tiles[i] = old[i];
            }

            this.m_Tiles[this.m_Count].m_ID = id;
            this.m_Tiles[this.m_Count].m_Z = z;
            ++this.m_Count;
        }

        public StaticTile[] ToArray()
        {
            if (this.m_Count == 0)
                return m_EmptyTiles;

            StaticTile[] tiles = new StaticTile[this.m_Count];

            for (int i = 0; i < this.m_Count; ++i)
                tiles[i] = this.m_Tiles[i];

            this.m_Count = 0;

            return tiles;
        }
    }
}