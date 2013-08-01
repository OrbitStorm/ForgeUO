/***************************************************************************
*                               HuePicker.cs
*                            -------------------
*   begin                : May 1, 2002
*   copyright            : (C) The RunUO Software Team
*   email                : info@runuo.com
*
*   $Id: HuePicker.cs 4 2006-06-15 04:28:39Z mark $
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

namespace Server.HuePickers
{
    public class HuePicker
    {
        private static int m_NextSerial = 1;
        private readonly int m_Serial;
        private readonly int m_ItemID;
        public HuePicker(int itemID)
        {
            do
            {
                this.m_Serial = m_NextSerial++;
            }
            while (this.m_Serial == 0);

            this.m_ItemID = itemID;
        }

        public int Serial
        {
            get
            {
                return this.m_Serial;
            }
        }
        public int ItemID
        {
            get
            {
                return this.m_ItemID;
            }
        }
        public virtual void OnResponse(int hue)
        {
        }

        public void SendTo(NetState state)
        {
            state.Send(new DisplayHuePicker(this));
            state.AddHuePicker(this);
        }
    }
}