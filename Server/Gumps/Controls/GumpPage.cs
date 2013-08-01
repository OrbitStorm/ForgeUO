/***************************************************************************
*                                GumpPage.cs
*                            -------------------
*   begin                : May 1, 2002
*   copyright            : (C) The RunUO Software Team
*   email                : info@runuo.com
*
*   $Id: GumpPage.cs 4 2006-06-15 04:28:39Z mark $
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
    public class GumpPage : GumpEntry
    {
        private static readonly byte[] m_LayoutName = Gump.StringToBuffer("page");
        private int m_Page;
        public GumpPage(int page)
        {
            this.m_Page = page;
        }

        public int Page
        {
            get
            {
                return this.m_Page;
            }
            set
            {
                this.Delta(ref this.m_Page, value);
            }
        }
        public override string Compile()
        {
            return String.Format("{{ page {0} }}", this.m_Page);
        }

        public override void AppendTo(IGumpWriter disp)
        {
            disp.AppendLayout(m_LayoutName);
            disp.AppendLayout(this.m_Page);
        }
    }
}