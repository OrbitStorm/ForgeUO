/***************************************************************************
*                             GumpAlphaRegion.cs
*                            -------------------
*   begin                : May 1, 2002
*   copyright            : (C) The RunUO Software Team
*   email                : info@runuo.com
*
*   $Id: GumpAlphaRegion.cs 4 2006-06-15 04:28:39Z mark $
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
    public class GumpAlphaRegion : GumpEntry
    {
        private static readonly byte[] m_LayoutName = Gump.StringToBuffer("checkertrans");
        private int m_X, m_Y;
        private int m_Width, m_Height;
        public GumpAlphaRegion(int x, int y, int width, int height)
        {
            this.m_X = x;
            this.m_Y = y;
            this.m_Width = width;
            this.m_Height = height;
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
        public int Width
        {
            get
            {
                return this.m_Width;
            }
            set
            {
                this.Delta(ref this.m_Width, value);
            }
        }
        public int Height
        {
            get
            {
                return this.m_Height;
            }
            set
            {
                this.Delta(ref this.m_Height, value);
            }
        }
        public override string Compile()
        {
            return String.Format("{{ checkertrans {0} {1} {2} {3} }}", this.m_X, this.m_Y, this.m_Width, this.m_Height);
        }

        public override void AppendTo(IGumpWriter disp)
        {
            disp.AppendLayout(m_LayoutName);
            disp.AppendLayout(this.m_X);
            disp.AppendLayout(this.m_Y);
            disp.AppendLayout(this.m_Width);
            disp.AppendLayout(this.m_Height);
        }
    }
}