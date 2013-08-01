/***************************************************************************
*                                IEntity.cs
*                            -------------------
*   begin                : May 1, 2002
*   copyright            : (C) The RunUO Software Team
*   email                : info@runuo.com
*
*   $Id: IEntity.cs 149 2007-01-19 22:10:11Z mark $
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
    public interface IEntity : IPoint3D, IComparable, IComparable<IEntity>
    {
        Serial Serial { get; }
        Point3D Location { get; }
        Map Map { get; }
        void Delete();

        void ProcessDelta();
    }

    public class Entity : IEntity, IComparable<Entity>
    {
        private readonly Serial m_Serial;
        private readonly Point3D m_Location;
        private readonly Map m_Map;
        public Entity(Serial serial, Point3D loc, Map map)
        {
            this.m_Serial = serial;
            this.m_Location = loc;
            this.m_Map = map;
        }

        public Serial Serial
        {
            get
            {
                return this.m_Serial;
            }
        }
        public Point3D Location
        {
            get
            {
                return this.m_Location;
            }
        }
        public int X
        {
            get
            {
                return this.m_Location.X;
            }
        }
        public int Y
        {
            get
            {
                return this.m_Location.Y;
            }
        }
        public int Z
        {
            get
            {
                return this.m_Location.Z;
            }
        }
        public Map Map
        {
            get
            {
                return this.m_Map;
            }
        }
        public int CompareTo(IEntity other)
        {
            if (other == null)
                return -1;

            return this.m_Serial.CompareTo(other.Serial);
        }

        public int CompareTo(Entity other)
        {
            return this.CompareTo((IEntity)other);
        }

        public int CompareTo(object other)
        {
            if (other == null || other is IEntity)
                return this.CompareTo((IEntity)other);

            throw new ArgumentException();
        }

        public void Delete()
        {
        }

        public void ProcessDelta()
        {
        }
    }
}