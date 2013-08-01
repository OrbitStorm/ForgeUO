/***************************************************************************
*                               GumpLabel.cs
*                            -------------------
*   begin                : May 1, 2002
*   copyright            : (C) The RunUO Software Team
*   email                : info@runuo.com
*
*   $Id: GumpLabel.cs 4 2006-06-15 04:28:39Z mark $
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
    public class GumpLabel : GumpEntry
    {
        private static readonly byte[] m_LayoutName = Gump.StringToBuffer("text");
        private int m_X, m_Y;
        private int m_Hue;
        private string m_Text;
        public GumpLabel(int x, int y, int hue, string text)
        {
            this.m_X = x;
            this.m_Y = y;
            this.m_Hue = hue;
            this.m_Text = text;
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
        public string Text
        {
            get
            {
                return this.m_Text;
            }
            set
            {
                this.Delta(ref this.m_Text, value);
            }
        }
        public override string Compile()
        {
            return String.Format("{{ text {0} {1} {2} {3} }}", this.m_X, this.m_Y, this.m_Hue, this.Parent.RootParent.Intern(this.m_Text));
        }

        public override void AppendTo(IGumpWriter disp)
        {
            disp.AppendLayout(m_LayoutName);
            disp.AppendLayout(this.m_X);
            disp.AppendLayout(this.m_Y);
            disp.AppendLayout(this.m_Hue);
            disp.AppendLayout(this.Parent.RootParent.Intern(this.m_Text));
        }
    }
}