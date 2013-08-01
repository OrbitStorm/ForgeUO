/***************************************************************************
*                               KeywordList.cs
*                            -------------------
*   begin                : May 1, 2002
*   copyright            : (C) The RunUO Software Team
*   email                : info@runuo.com
*
*   $Id: KeywordList.cs 4 2006-06-15 04:28:39Z mark $
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

namespace Server
{
    public class KeywordList
    {
        private static readonly int[] m_EmptyInts = new int[0];
        private int[] m_Keywords;
        private int m_Count;
        public KeywordList()
        {
            this.m_Keywords = new int[8];
            this.m_Count = 0;
        }

        public int Count
        {
            get
            {
                return this.m_Count;
            }
        }
        public bool Contains(int keyword)
        {
            bool contains = false;

            for (int i = 0; !contains && i < this.m_Count; ++i)
                contains = (keyword == this.m_Keywords[i]);

            return contains;
        }

        public void Add(int keyword)
        {
            if ((this.m_Count + 1) > this.m_Keywords.Length)
            {
                int[] old = this.m_Keywords;
                this.m_Keywords = new int[old.Length * 2];

                for (int i = 0; i < old.Length; ++i)
                    this.m_Keywords[i] = old[i];
            }

            this.m_Keywords[this.m_Count++] = keyword;
        }

        public int[] ToArray()
        {
            if (this.m_Count == 0)
                return m_EmptyInts;

            int[] keywords = new int[this.m_Count];

            for (int i = 0; i < this.m_Count; ++i)
                keywords[i] = this.m_Keywords[i];

            this.m_Count = 0;

            return keywords;
        }
    }
}