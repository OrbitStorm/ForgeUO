/***************************************************************************
*                              ItemListMenu.cs
*                            -------------------
*   begin                : May 1, 2002
*   copyright            : (C) The RunUO Software Team
*   email                : info@runuo.com
*
*   $Id: ItemListMenu.cs 4 2006-06-15 04:28:39Z mark $
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

namespace Server.Menus.ItemLists
{
    public class ItemListEntry
    {
        private readonly string m_Name;
        private readonly int m_ItemID;
        private readonly int m_Hue;
        public ItemListEntry(string name, int itemID)
            : this(name, itemID, 0)
        {
        }

        public ItemListEntry(string name, int itemID, int hue)
        {
            this.m_Name = name;
            this.m_ItemID = itemID;
            this.m_Hue = hue;
        }

        public string Name
        {
            get
            {
                return this.m_Name;
            }
        }
        public int ItemID
        {
            get
            {
                return this.m_ItemID;
            }
        }
        public int Hue
        {
            get
            {
                return this.m_Hue;
            }
        }
    }

    public class ItemListMenu : IMenu
    {
        private static int m_NextSerial;
        private readonly string m_Question;
        private readonly int m_Serial;
        private ItemListEntry[] m_Entries;
        public ItemListMenu(string question, ItemListEntry[] entries)
        {
            this.m_Question = question;
            this.m_Entries = entries;

            do
            {
                this.m_Serial = m_NextSerial++;
                this.m_Serial &= 0x7FFFFFFF;
            }
            while (this.m_Serial == 0);

            this.m_Serial = (int)((uint)this.m_Serial | 0x80000000);
        }

        public string Question
        {
            get
            {
                return this.m_Question;
            }
        }
        public ItemListEntry[] Entries
        {
            get
            {
                return this.m_Entries;
            }
            set
            {
                this.m_Entries = value;
            }
        }
        int IMenu.Serial
        {
            get
            {
                return this.m_Serial;
            }
        }
        int IMenu.EntryLength
        {
            get
            {
                return this.m_Entries.Length;
            }
        }
        public virtual void OnCancel(NetState state)
        {
        }

        public virtual void OnResponse(NetState state, int index)
        {
        }

        public void SendTo(NetState state)
        {
            state.AddMenu(this);
            state.Send(new DisplayItemListMenu(this));
        }
    }
}