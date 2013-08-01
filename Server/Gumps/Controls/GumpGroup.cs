/***************************************************************************
*                                GumpGroup.cs
*                            -------------------
*   begin                : May 1, 2002
*   copyright            : (C) The RunUO Software Team
*   email                : info@runuo.com
*
*   $Id: GumpGroup.cs 4 2006-06-15 04:28:39Z mark $
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
    public class GumpGroup : GumpEntry
    {
        private static readonly byte[] m_LayoutName = Gump.StringToBuffer("group");
        private int m_Group;
        public GumpGroup(int group)
        {
            this.m_Group = group;
        }

        public int Group
        {
            get
            {
                return this.m_Group;
            }
            set
            {
                this.Delta(ref this.m_Group, value);
            }
        }
        public override string Compile()
        {
            return String.Format("{{ group {0} }}", this.m_Group);
        }

        public override void AppendTo(IGumpWriter disp)
        {
            disp.AppendLayout(m_LayoutName);
            disp.AppendLayout(this.m_Group);
        }
    }
}