/***************************************************************************
*                            GumpHtmlLocalized.cs
*                            -------------------
*   begin                : May 1, 2002
*   copyright            : (C) The RunUO Software Team
*   email                : info@runuo.com
*
*   $Id: GumpHtmlLocalized.cs 4 2006-06-15 04:28:39Z mark $
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
    public enum GumpHtmlLocalizedType
    {
        Plain,
        Color,
        Args
    }

    public class GumpHtmlLocalized : GumpEntry
    {
        private static readonly byte[] m_LayoutNamePlain = Gump.StringToBuffer("xmfhtmlgump");
        private static readonly byte[] m_LayoutNameColor = Gump.StringToBuffer("xmfhtmlgumpcolor");
        private static readonly byte[] m_LayoutNameArgs = Gump.StringToBuffer("xmfhtmltok");
        private int m_X, m_Y;
        private int m_Width, m_Height;
        private int m_Number;
        private string m_Args;
        private int m_Color;
        private bool m_Background, m_Scrollbar;
        private GumpHtmlLocalizedType m_Type;
        public GumpHtmlLocalized(int x, int y, int width, int height, int number, bool background, bool scrollbar)
        {
            this.m_X = x;
            this.m_Y = y;
            this.m_Width = width;
            this.m_Height = height;
            this.m_Number = number;
            this.m_Background = background;
            this.m_Scrollbar = scrollbar;

            this.m_Type = GumpHtmlLocalizedType.Plain;
        }

        public GumpHtmlLocalized(int x, int y, int width, int height, int number, int color, bool background, bool scrollbar)
        {
            this.m_X = x;
            this.m_Y = y;
            this.m_Width = width;
            this.m_Height = height;
            this.m_Number = number;
            this.m_Color = color;
            this.m_Background = background;
            this.m_Scrollbar = scrollbar;

            this.m_Type = GumpHtmlLocalizedType.Color;
        }

        public GumpHtmlLocalized(int x, int y, int width, int height, int number, string args, int color, bool background, bool scrollbar)
        {
            // Are multiple arguments unsupported? And what about non ASCII arguments?
            this.m_X = x;
            this.m_Y = y;
            this.m_Width = width;
            this.m_Height = height;
            this.m_Number = number;
            this.m_Args = args;
            this.m_Color = color;
            this.m_Background = background;
            this.m_Scrollbar = scrollbar;

            this.m_Type = GumpHtmlLocalizedType.Args;
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
        public int Number
        {
            get
            {
                return this.m_Number;
            }
            set
            {
                this.Delta(ref this.m_Number, value);
            }
        }
        public string Args
        {
            get
            {
                return this.m_Args;
            }
            set
            {
                this.Delta(ref this.m_Args, value);
            }
        }
        public int Color
        {
            get
            {
                return this.m_Color;
            }
            set
            {
                this.Delta(ref this.m_Color, value);
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
        public GumpHtmlLocalizedType Type
        {
            get
            {
                return this.m_Type;
            }
            set
            {
                if (this.m_Type != value)
                {
                    this.m_Type = value;

                    if (this.Parent != null)
                        this.Parent.Invalidate();
                }
            }
        }
        public override string Compile()
        {
            switch ( this.m_Type )
            {
                case GumpHtmlLocalizedType.Plain:
                    return String.Format("{{ xmfhtmlgump {0} {1} {2} {3} {4} {5} {6} }}", this.m_X, this.m_Y, this.m_Width, this.m_Height, this.m_Number, this.m_Background ? 1 : 0, this.m_Scrollbar ? 1 : 0);

                case GumpHtmlLocalizedType.Color:
                    return String.Format("{{ xmfhtmlgumpcolor {0} {1} {2} {3} {4} {5} {6} {7} }}", this.m_X, this.m_Y, this.m_Width, this.m_Height, this.m_Number, this.m_Background ? 1 : 0, this.m_Scrollbar ? 1 : 0, this.m_Color);

                default: // GumpHtmlLocalizedType.Args
                    return String.Format("{{ xmfhtmltok {0} {1} {2} {3} {4} {5} {6} {7} @{8}@ }}", this.m_X, this.m_Y, this.m_Width, this.m_Height, this.m_Background ? 1 : 0, this.m_Scrollbar ? 1 : 0, this.m_Color, this.m_Number, this.m_Args);
            }
        }

        public override void AppendTo(IGumpWriter disp)
        {
            switch ( this.m_Type )
            {
                case GumpHtmlLocalizedType.Plain:
                    {
                        disp.AppendLayout(m_LayoutNamePlain);

                        disp.AppendLayout(this.m_X);
                        disp.AppendLayout(this.m_Y);
                        disp.AppendLayout(this.m_Width);
                        disp.AppendLayout(this.m_Height);
                        disp.AppendLayout(this.m_Number);
                        disp.AppendLayout(this.m_Background);
                        disp.AppendLayout(this.m_Scrollbar);

                        break;
                    }

                case GumpHtmlLocalizedType.Color:
                    {
                        disp.AppendLayout(m_LayoutNameColor);

                        disp.AppendLayout(this.m_X);
                        disp.AppendLayout(this.m_Y);
                        disp.AppendLayout(this.m_Width);
                        disp.AppendLayout(this.m_Height);
                        disp.AppendLayout(this.m_Number);
                        disp.AppendLayout(this.m_Background);
                        disp.AppendLayout(this.m_Scrollbar);
                        disp.AppendLayout(this.m_Color);

                        break;
                    }

                case GumpHtmlLocalizedType.Args:
                    {
                        disp.AppendLayout(m_LayoutNameArgs);

                        disp.AppendLayout(this.m_X);
                        disp.AppendLayout(this.m_Y);
                        disp.AppendLayout(this.m_Width);
                        disp.AppendLayout(this.m_Height);
                        disp.AppendLayout(this.m_Background);
                        disp.AppendLayout(this.m_Scrollbar);
                        disp.AppendLayout(this.m_Color);
                        disp.AppendLayout(this.m_Number);
                        disp.AppendLayout(this.m_Args);

                        break;
                    }
            }
        }
    }
}