/***************************************************************************
*                                GumpItem.cs
*                            -------------------
*   begin                : May 1, 2002
*   copyright            : (C) The RunUO Software Team
*   email                : info@runuo.com
*
*   $Id: GumpItem.cs 4 2006-06-15 04:28:39Z mark $
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
using Server.Network;

namespace Server.Gumps
{
    public class GumpItem : GumpEntry
    {
        private static readonly byte[] m_LayoutName = Gump.StringToBuffer("tilepic");
        private static readonly byte[] m_LayoutNameHue = Gump.StringToBuffer("tilepichue");
        private int m_X, m_Y;
        private int m_ItemID;
        private int m_Hue;
        public GumpItem(int x, int y, int itemID)
            : this(x, y, itemID, 0)
        {
        }

        public GumpItem(int x, int y, int itemID, int hue)
        {
            this.m_X = x;
            this.m_Y = y;
            this.m_ItemID = itemID;
            this.m_Hue = hue;
        }

        public override int X
        {
            get
            {
                return this.m_X;
            }
            set
            {
                this.Delta(ref this.m_X, value);
            }
        }
        public override int Y
        {
            get
            {
                return this.m_Y;
            }
            set
            {
                this.Delta(ref this.m_Y, value);
            }
        }
        public int ItemID
        {
            get
            {
                return this.m_ItemID;
            }
            set
            {
                this.Delta(ref this.m_ItemID, value);
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
                this.Delta(ref this.m_Hue, value);
            }
        }
        public override string Compile()
        {
            if (this.m_Hue == 0)
                return String.Format("{{ tilepic {0} {1} {2} }}", this.m_X, this.m_Y, this.m_ItemID);
            else
                return String.Format("{{ tilepichue {0} {1} {2} {3} }}", this.m_X, this.m_Y, this.m_ItemID, this.m_Hue);
        }

        public override void AppendTo(IGumpWriter disp)
        {
            disp.AppendLayout(this.m_Hue == 0 ? m_LayoutName : m_LayoutNameHue);
            disp.AppendLayout(this.m_X);
            disp.AppendLayout(this.m_Y);
            disp.AppendLayout(this.m_ItemID);

            if (this.m_Hue != 0)
                disp.AppendLayout(this.m_Hue);
        }
    }
}