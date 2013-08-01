/***************************************************************************
*                               LandTarget.cs
*                            -------------------
*   begin                : May 1, 2002
*   copyright            : (C) The RunUO Software Team
*   email                : info@runuo.com
*
*   $Id: LandTarget.cs 591 2010-12-06 06:45:45Z mark $
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
    public class LandTarget : IPoint3D
    {
        private readonly Point3D m_Location;
        private readonly int m_TileID;
        public LandTarget(Point3D location, Map map)
        {
            this.m_Location = location;

            if (map != null)
            {
                this.m_Location.Z = map.GetAverageZ(this.m_Location.X, this.m_Location.Y);
                this.m_TileID = map.Tiles.GetLandTile(this.m_Location.X, this.m_Location.Y).ID & TileData.MaxLandValue;
            }
        }

        [CommandProperty(AccessLevel.Counselor)]
        public string Name
        {
            get
            {
                return TileData.LandTable[this.m_TileID].Name;
            }
        }
        [CommandProperty(AccessLevel.Counselor)]
        public TileFlag Flags
        {
            get
            {
                return TileData.LandTable[this.m_TileID].Flags;
            }
        }
        [CommandProperty(AccessLevel.Counselor)]
        public int TileID
        {
            get
            {
                return this.m_TileID;
            }
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
    }
}