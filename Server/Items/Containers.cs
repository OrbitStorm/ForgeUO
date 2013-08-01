/***************************************************************************
*                               Containers.cs
*                            -------------------
*   begin                : May 1, 2002
*   copyright            : (C) The RunUO Software Team
*   email                : info@runuo.com
*
*   $Id: Containers.cs 4 2006-06-15 04:28:39Z mark $
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

namespace Server.Items
{
    public class BankBox : Container
    {
        private static bool m_SendRemovePacket;
        private Mobile m_Owner;
        private bool m_Open;
        public BankBox(Serial serial)
            : base(serial)
        {
        }

        public BankBox(Mobile owner)
            : base(0xE7C)
        {
            this.Layer = Layer.Bank;
            this.Movable = false;
            this.m_Owner = owner;
        }

        public static bool SendDeleteOnClose
        {
            get
            {
                return m_SendRemovePacket;
            }
            set
            {
                m_SendRemovePacket = value;
            }
        }
        public override int DefaultMaxWeight
        {
            get
            {
                return 0;
            }
        }
        public override bool IsVirtualItem
        {
            get
            {
                return true;
            }
        }
        public Mobile Owner
        {
            get
            {
                return this.m_Owner;
            }
        }
        public bool Opened
        {
            get
            {
                return this.m_Open;
            }
        }
        public void Open()
        {
            this.m_Open = true;

            if (this.m_Owner != null)
            {
                this.m_Owner.PrivateOverheadMessage(MessageType.Regular, 0x3B2, true, String.Format("Bank container has {0} items, {1} stones", this.TotalItems, this.TotalWeight), this.m_Owner.NetState);
                this.m_Owner.Send(new EquipUpdate(this));
                this.DisplayTo(this.m_Owner);
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((Mobile)this.m_Owner);
            writer.Write((bool)this.m_Open);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch ( version )
            {
                case 0:
                    {
                        this.m_Owner = reader.ReadMobile();
                        this.m_Open = reader.ReadBool();

                        if (this.m_Owner == null)
                            this.Delete();

                        break;
                    }
            }

            if (this.ItemID == 0xE41)
                this.ItemID = 0xE7C;
        }

        public void Close()
        {
            this.m_Open = false;

            if (this.m_Owner != null && m_SendRemovePacket)
                this.m_Owner.Send(this.RemovePacket);
        }

        public override void OnSingleClick(Mobile from)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
        }

        public override DeathMoveResult OnParentDeath(Mobile parent)
        {
            return DeathMoveResult.RemainEquiped;
        }

        public override bool IsAccessibleTo(Mobile check)
        {
            if ((check == this.m_Owner && this.m_Open) || check.AccessLevel >= AccessLevel.GameMaster)
                return base.IsAccessibleTo(check);
            else
                return false;
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            if ((from == this.m_Owner && this.m_Open) || from.AccessLevel >= AccessLevel.GameMaster)
                return base.OnDragDrop(from, dropped);
            else
                return false;
        }

        public override bool OnDragDropInto(Mobile from, Item item, Point3D p)
        {
            if ((from == this.m_Owner && this.m_Open) || from.AccessLevel >= AccessLevel.GameMaster)
                return base.OnDragDropInto(from, item, p);
            else
                return false;
        }
    }
}