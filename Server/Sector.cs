/***************************************************************************
*                                 Sector.cs
*                            -------------------
*   begin                : May 1, 2002
*   copyright            : (C) The RunUO Software Team
*   email                : info@runuo.com
*
*   $Id: Sector.cs 24 2006-06-16 22:31:18Z krrios $
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
using Server.Items;
using Server.Network;

namespace Server
{
    public class RegionRect : IComparable
    {
        private readonly Region m_Region;
        private readonly Rectangle3D m_Rect;
        public RegionRect(Region region, Rectangle3D rect)
        {
            this.m_Region = region;
            this.m_Rect = rect;
        }

        public Region Region
        {
            get
            {
                return this.m_Region;
            }
        }
        public Rectangle3D Rect
        {
            get
            {
                return this.m_Rect;
            }
        }
        public bool Contains(Point3D loc)
        {
            return this.m_Rect.Contains(loc);
        }

        int IComparable.CompareTo(object obj)
        {
            if (obj == null)
                return 1;

            RegionRect regRect = obj as RegionRect;

            if (regRect == null)
                throw new ArgumentException("obj is not a RegionRect", "obj");

            return ((IComparable)this.m_Region).CompareTo(regRect.m_Region);
        }
    }

    public class Sector
    {
        // TODO: Can we avoid this?
        private static readonly List<Mobile> m_DefaultMobileList = new List<Mobile>();
        private static readonly List<Item> m_DefaultItemList = new List<Item>();
        private static readonly List<NetState> m_DefaultClientList = new List<NetState>();
        private static readonly List<BaseMulti> m_DefaultMultiList = new List<BaseMulti>();
        private static readonly List<RegionRect> m_DefaultRectList = new List<RegionRect>();
        private readonly int m_X;
        private readonly int m_Y;
        private readonly Map m_Owner;
        private List<Mobile> m_Mobiles;
        private List<Mobile> m_Players;
        private List<Item> m_Items;
        private List<NetState> m_Clients;
        private List<BaseMulti> m_Multis;
        private List<RegionRect> m_RegionRects;
        private bool m_Active;
        public Sector(int x, int y, Map owner)
        {
            this.m_X = x;
            this.m_Y = y;
            this.m_Owner = owner;
            this.m_Active = false;
        }

        public List<RegionRect> RegionRects
        {
            get
            {
                if (this.m_RegionRects == null)
                    return m_DefaultRectList;

                return this.m_RegionRects;
            }
        }
        public List<BaseMulti> Multis
        {
            get
            {
                if (this.m_Multis == null)
                    return m_DefaultMultiList;

                return this.m_Multis;
            }
        }
        public List<Mobile> Mobiles
        {
            get
            {
                if (this.m_Mobiles == null)
                    return m_DefaultMobileList;

                return this.m_Mobiles;
            }
        }
        public List<Item> Items
        {
            get
            {
                if (this.m_Items == null)
                    return m_DefaultItemList;

                return this.m_Items;
            }
        }
        public List<NetState> Clients
        {
            get
            {
                if (this.m_Clients == null)
                    return m_DefaultClientList;

                return this.m_Clients;
            }
        }
        public List<Mobile> Players
        {
            get
            {
                if (this.m_Players == null)
                    return m_DefaultMobileList;

                return this.m_Players;
            }
        }
        public bool Active
        {
            get
            {
                return (this.m_Active && this.m_Owner != Map.Internal);
            }
        }
        public Map Owner
        {
            get
            {
                return this.m_Owner;
            }
        }
        public int X
        {
            get
            {
                return this.m_X;
            }
        }
        public int Y
        {
            get
            {
                return this.m_Y;
            }
        }
        public void OnClientChange(NetState oldState, NetState newState)
        {
            this.Replace(ref this.m_Clients, oldState, newState);
        }

        public void OnEnter(Item item)
        {
            this.Add(ref this.m_Items, item);
        }

        public void OnLeave(Item item)
        {
            this.Remove(ref this.m_Items, item);
        }

        public void OnEnter(Mobile mob)
        {
            this.Add(ref this.m_Mobiles, mob);

            if (mob.NetState != null)
            {
                this.Add(ref this.m_Clients, mob.NetState);
            }

            if (mob.Player)
            {
                if (this.m_Players == null)
                {
                    this.m_Owner.ActivateSectors(this.m_X, this.m_Y);
                }

                this.Add(ref this.m_Players, mob);
            }
        }

        public void OnLeave(Mobile mob)
        {
            this.Remove(ref this.m_Mobiles, mob);

            if (mob.NetState != null)
            {
                this.Remove(ref this.m_Clients, mob.NetState);
            }

            if (mob.Player && this.m_Players != null)
            {
                this.Remove(ref this.m_Players, mob);

                if (this.m_Players == null)
                {
                    this.m_Owner.DeactivateSectors(this.m_X, this.m_Y);
                }
            }
        }

        public void OnEnter(Region region, Rectangle3D rect)
        {
            this.Add(ref this.m_RegionRects, new RegionRect(region, rect));

            this.m_RegionRects.Sort();

            this.UpdateMobileRegions();
        }

        public void OnLeave(Region region)
        {
            if (this.m_RegionRects != null)
            {
                for (int i = this.m_RegionRects.Count - 1; i >= 0; i--)
                {
                    RegionRect regRect = this.m_RegionRects[i];

                    if (regRect.Region == region)
                    {
                        this.m_RegionRects.RemoveAt(i);
                    }
                }

                if (this.m_RegionRects.Count == 0)
                {
                    this.m_RegionRects = null;
                }
            }

            this.UpdateMobileRegions();
        }

        public void OnMultiEnter(BaseMulti multi)
        {
            this.Add(ref this.m_Multis, multi);
        }

        public void OnMultiLeave(BaseMulti multi)
        {
            this.Remove(ref this.m_Multis, multi);
        }

        public void Activate()
        {
            if (!this.Active && this.m_Owner != Map.Internal)
            {
                if (this.m_Items != null)
                {
                    foreach (Item item in this.m_Items)
                    {
                        item.OnSectorActivate();
                    }
                }

                if (this.m_Mobiles != null)
                {
                    foreach (Mobile mob in this.m_Mobiles)
                    {
                        mob.OnSectorActivate();
                    }
                }

                this.m_Active = true;
            }
        }

        public void Deactivate()
        {
            if (this.Active)
            {
                if (this.m_Items != null)
                {
                    foreach (Item item in this.m_Items)
                    {
                        item.OnSectorDeactivate();
                    }
                }

                if (this.m_Mobiles != null)
                {
                    foreach (Mobile mob in this.m_Mobiles)
                    {
                        mob.OnSectorDeactivate();
                    }
                }

                this.m_Active = false;
            }
        }

        private void Add<T>(ref List<T> list, T value)
        {
            if (list == null)
            {
                list = new List<T>();
            }

            list.Add(value);
        }

        private void Remove<T>(ref List<T> list, T value)
        {
            if (list != null)
            {
                list.Remove(value);

                if (list.Count == 0)
                {
                    list = null;
                }
            }
        }

        private void Replace<T>(ref List<T> list, T oldValue, T newValue)
        {
            if (oldValue != null && newValue != null)
            {
                int index = (list != null ? list.IndexOf(oldValue) : -1);

                if (index >= 0)
                {
                    list[index] = newValue;
                }
                else
                {
                    this.Add(ref list, newValue);
                }
            }
            else if (oldValue != null)
            {
                this.Remove(ref list, oldValue);
            }
            else if (newValue != null)
            {
                this.Add(ref list, newValue);
            }
        }

        private void UpdateMobileRegions()
        {
            if (this.m_Mobiles != null)
            {
                List<Mobile> sandbox = new List<Mobile>(this.m_Mobiles);

                foreach (Mobile mob in sandbox)
                {
                    mob.UpdateRegion();
                }
            }
        }
    }
}