/***************************************************************************
*                          SecureTradeContainer.cs
*                            -------------------
*   begin                : May 1, 2002
*   copyright            : (C) The RunUO Software Team
*   email                : info@runuo.com
*
*   $Id: SecureTradeContainer.cs 793 2011-12-18 05:09:31Z mark $
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
    public class SecureTradeContainer : Container
    {
        private readonly SecureTrade m_Trade;
        public SecureTradeContainer(SecureTrade trade)
            : base(0x1E5E)
        {
            this.m_Trade = trade;

            this.Movable = false;
        }

        public SecureTradeContainer(Serial serial)
            : base(serial)
        {
        }

        public SecureTrade Trade
        {
            get
            {
                return this.m_Trade;
            }
        }
        public override bool CheckHold(Mobile m, Item item, bool message, bool checkItems, int plusItems, int plusWeight)
        {
            Mobile to;

            if (this.Trade.From.Container != this)
                to = this.Trade.From.Mobile;
            else
                to = this.Trade.To.Mobile;

            return m.CheckTrade(to, item, this, message, checkItems, plusItems, plusWeight);
        }

        public override bool CheckLift(Mobile from, Item item, ref LRReason reject)
        {
            reject = LRReason.CannotLift;
            return false;
        }

        public override bool IsAccessibleTo(Mobile check)
        {
            if (!this.IsChildOf(check) || this.m_Trade == null || !this.m_Trade.Valid)
                return false;

            return base.IsAccessibleTo(check);
        }

        public override void OnItemAdded(Item item)
        {
            this.ClearChecks();
        }

        public override void OnItemRemoved(Item item)
        {
            this.ClearChecks();
        }

        public override void OnSubItemAdded(Item item)
        {
            this.ClearChecks();
        }

        public override void OnSubItemRemoved(Item item)
        {
            this.ClearChecks();
        }

        public void ClearChecks()
        {
            if (this.m_Trade != null)
            {
                this.m_Trade.From.Accepted = false;
                this.m_Trade.To.Accepted = false;
                this.m_Trade.Update();
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}