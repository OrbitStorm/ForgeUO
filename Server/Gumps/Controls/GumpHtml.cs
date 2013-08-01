/***************************************************************************
*                                GumpHtml.cs
*                            -------------------
*   begin                : May 1, 2002
*   copyright            : (C) The RunUO Software Team
*   email                : info@runuo.com
*
*   $Id: GumpHtml.cs 4 2006-06-15 04:28:39Z mark $
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
    public class GumpHtml : GumpEntry
    {
        private static readonly byte[] m_LayoutName = Gump.StringToBuffer("htmlgump");
        private int m_X, m_Y;
        private int m_Width, m_Height;
        private string m_Text;
        private bool m_Background, m_Scrollbar;
        public GumpHtml(int x, int y, int width, int height, string text, bool background, bool scrollbar)
        {
            this.m_X = x;
            this.m_Y = y;
            this.m_Width = width;
            this.m_Height = height;
            this.m_Text = text;
            this.m_Background = background;
            this.m_Scrollbar = scrollbar;
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
        public bool Background
        {
            get
            {
                return this.m_Background;
            }
            set
            {
                this.Delta(ref this.m_Background, value);
            }
        }
        public bool Scrollbar
        {
            get
            {
                return this.m_Scrollbar;
            }
            set
            {
                this.Delta(ref this.m_Scrollbar, value);
            }
        }
        public override string Compile()
        {
            return String.Format("{{ htmlgump {0} {1} {2} {3} {4} {5} {6} }}", this.m_X, this.m_Y, this.m_Width, this.m_Height, this.Parent.RootParent.Intern(this.m_Text), this.m_Background ? 1 : 0, this.m_Scrollbar ? 1 : 0);
        }

        public override void AppendTo(IGumpWriter disp)
        {
            disp.AppendLayout(m_LayoutName);
            disp.AppendLayout(this.m_X);
            disp.AppendLayout(this.m_Y);
            disp.AppendLayout(this.m_Width);
            disp.AppendLayout(this.m_Height);
            disp.AppendLayout(this.Parent.RootParent.Intern(this.m_Text));
            disp.AppendLayout(this.m_Background);
            disp.AppendLayout(this.m_Scrollbar);
        }
    }
}