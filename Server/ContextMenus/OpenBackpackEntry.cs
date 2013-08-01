/***************************************************************************
*                            OpenBackpackEntry.cs
*                            -------------------
*   begin                : May 1, 2002
*   copyright            : (C) The RunUO Software Team
*   email                : info@runuo.com
*
*   $Id: OpenBackpackEntry.cs 4 2006-06-15 04:28:39Z mark $
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

namespace Server.ContextMenus
{
    public class OpenBackpackEntry : ContextMenuEntry
    {
        private readonly Mobile m_Mobile;
        public OpenBackpackEntry(Mobile m)
            : base(6145)
        {
            this.m_Mobile = m;
        }

        public override void OnClick()
        {
            this.m_Mobile.Use(this.m_Mobile.Backpack);
        }
    }
}