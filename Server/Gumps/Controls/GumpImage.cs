/***************************************************************************
*                               GumpImage.cs
*                            -------------------
*   begin                : May 1, 2002
*   copyright            : (C) The RunUO Software Team
*   email                : info@runuo.com
*
*   $Id: GumpImage.cs 4 2006-06-15 04:28:39Z mark $
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
    public class GumpImage : GumpEntry
    {
        private static readonly byte[] m_LayoutName = Gump.StringToBuffer("gumppic");
        private static readonly byte[] m_HueEquals = Gump.StringToBuffer(" hue=");
        private int m_X, m_Y;
        private int m_GumpID;
        private int m_Hue;
        public GumpImage(int x, int y, int gumpID)
            : this(x, y, gumpID, 0)
        {
        }

        public GumpImage(int x, int y, int gumpID, int hue)
        {
            this.m_X = x;
            this.m_Y = y;
            this.m_GumpID = gumpID;
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
        public int GumpID
        {
            get
            {
                return this.m_GumpID;
            }
            set
            {
                this.Delta(ref this.m_GumpID, value);
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
                return String.Format("{{ gumppic {0} {1} {2} }}", this.m_X, this.m_Y, this.m_GumpID);
            else
                return String.Format("{{ gumppic {0} {1} {2} hue={3} }}", this.m_X, this.m_Y, this.m_GumpID, this.m_Hue);
        }

        public override void AppendTo(IGumpWriter disp)
        {
            disp.AppendLayout(m_LayoutName);
            disp.AppendLayout(this.m_X);
            disp.AppendLayout(this.m_Y);
            disp.AppendLayout(this.m_GumpID);

            if (this.m_Hue != 0)
            {
                disp.AppendLayout(m_HueEquals);
                disp.AppendLayoutNS(this.m_Hue);
            }
        }
    }
}