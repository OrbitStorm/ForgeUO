/***************************************************************************
*                              StaticTarget.cs
*                            -------------------
*   begin                : May 1, 2002
*   copyright            : (C) The RunUO Software Team
*   email                : info@runuo.com
*
*   $Id: StaticTarget.cs 591 2010-12-06 06:45:45Z mark $
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

namespace Server.Targeting
{
    public class StaticTarget : IPoint3D
    {
        private readonly Point3D m_Location;
        private readonly int m_ItemID;
        public StaticTarget(Point3D location, int itemID)
        {
            this.m_Location = location;
            this.m_ItemID = itemID & TileData.MaxItemValue;
            this.m_Location.Z += TileData.ItemTable[this.m_ItemID].CalcHeight;
        }

        [CommandProperty(AccessLevel.Counselor)]
        public Point3D Location
        {
            get
            {
                return this.m_Location;
            }
        }
        [CommandProperty(AccessLevel.Counselor)]
        public string Name
        {
            get
            {
                return TileData.ItemTable[this.m_ItemID].Name;
            }
        }
        [CommandProperty(AccessLevel.Counselor)]
        public TileFlag Flags
        {
            get
            {
                return TileData.ItemTable[this.m_ItemID].Flags;
            }
        }
        [CommandProperty(AccessLevel.Counselor)]
        public int X
        {
            get
            {
                return this.m_Location.X;
            }
        }
        [CommandProperty(AccessLevel.Counselor)]
        public int Y
        {
            get
            {
                return this.m_Location.Y;
            }
        }
        [CommandProperty(AccessLevel.Counselor)]
        public int Z
        {
            get
            {
                return this.m_Location.Z;
            }
        }
        [CommandProperty(AccessLevel.Counselor)]
        public int ItemID
        {
            get
            {
                return this.m_ItemID;
            }
        }
    }
}