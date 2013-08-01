/***************************************************************************
*                               Point3DList.cs
*                            -------------------
*   begin                : May 1, 2002
*   copyright            : (C) The RunUO Software Team
*   email                : info@runuo.com
*
*   $Id: Point3DList.cs 4 2006-06-15 04:28:39Z mark $
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
    public class Point3DList
    {
        private static readonly Point3D[] m_EmptyList = new Point3D[0];
        private Point3D[] m_List;
        private int m_Count;
        public Point3DList()
        {
            this.m_List = new Point3D[8];
            this.m_Count = 0;
        }

        public int Count
        {
            get
            {
                return this.m_Count;
            }
        }
        public Point3D Last
        {
            get
            {
                return this.m_List[this.m_Count - 1];
            }
        }
        public Point3D this[int index]
        {
            get
            {
                return this.m_List[index];
            }
        }
        public void Clear()
        {
            this.m_Count = 0;
        }

        public void Add(int x, int y, int z)
        {
            if ((this.m_Count + 1) > this.m_List.Length)
            {
                Point3D[] old = this.m_List;
                this.m_List = new Point3D[old.Length * 2];

                for (int i = 0; i < old.Length; ++i)
                    this.m_List[i] = old[i];
            }

            this.m_List[this.m_Count].m_X = x;
            this.m_List[this.m_Count].m_Y = y;
            this.m_List[this.m_Count].m_Z = z;
            ++this.m_Count;
        }

        public void Add(Point3D p)
        {
            if ((this.m_Count + 1) > this.m_List.Length)
            {
                Point3D[] old = this.m_List;
                this.m_List = new Point3D[old.Length * 2];

                for (int i = 0; i < old.Length; ++i)
                    this.m_List[i] = old[i];
            }

            this.m_List[this.m_Count].m_X = p.m_X;
            this.m_List[this.m_Count].m_Y = p.m_Y;
            this.m_List[this.m_Count].m_Z = p.m_Z;
            ++this.m_Count;
        }

        public Point3D[] ToArray()
        {
            if (this.m_Count == 0)
                return m_EmptyList;

            Point3D[] list = new Point3D[this.m_Count];

            for (int i = 0; i < this.m_Count; ++i)
                list[i] = this.m_List[i];

            this.m_Count = 0;

            return list;
        }
    }
}