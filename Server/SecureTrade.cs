/***************************************************************************
*                               SecureTrade.cs
*                            -------------------
*   begin                : May 1, 2002
*   copyright            : (C) The RunUO Software Team
*   email                : info@runuo.com
*
*   $Id: SecureTrade.cs 521 2010-06-17 07:11:43Z mark $
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
using System.Collections.Generic;
using Server.Items;
using Server.Network;

namespace Server
{
    public class SecureTrade
    {
        private readonly SecureTradeInfo m_From;
        private readonly SecureTradeInfo m_To;
        private bool m_Valid;
        public SecureTrade(Mobile from, Mobile to)
        {
            this.m_Valid = true;

            this.m_From = new SecureTradeInfo(this, from, new SecureTradeContainer(this));
            this.m_To = new SecureTradeInfo(this, to, new SecureTradeContainer(this));

            bool from6017 = (from.NetState == null ? false : from.NetState.ContainerGridLines);
            bool to6017 = (to.NetState == null ? false : to.NetState.ContainerGridLines);

            from.Send(new MobileStatus(from, to));
            from.Send(new UpdateSecureTrade(this.m_From.Container, false, false));
            if (from6017)
                from.Send(new SecureTradeEquip6017(this.m_To.Container, to));
            else
                from.Send(new SecureTradeEquip(this.m_To.Container, to));
            from.Send(new UpdateSecureTrade(this.m_From.Container, false, false));
            if (from6017)
                from.Send(new SecureTradeEquip6017(this.m_From.Container, from));
            else
                from.Send(new SecureTradeEquip(this.m_From.Container, from));
            from.Send(new DisplaySecureTrade(to, this.m_From.Container, this.m_To.Container, to.Name));
            from.Send(new UpdateSecureTrade(this.m_From.Container, false, false));

            to.Send(new MobileStatus(to, from));
            to.Send(new UpdateSecureTrade(this.m_To.Container, false, false));
            if (to6017)
                to.Send(new SecureTradeEquip6017(this.m_From.Container, from));
            else
                to.Send(new SecureTradeEquip(this.m_From.Container, from));
            to.Send(new UpdateSecureTrade(this.m_To.Container, false, false));
            if (to6017)
                to.Send(new SecureTradeEquip6017(this.m_To.Container, to));
            else
                to.Send(new SecureTradeEquip(this.m_To.Container, to));
            to.Send(new DisplaySecureTrade(from, this.m_To.Container, this.m_From.Container, from.Name));
            to.Send(new UpdateSecureTrade(this.m_To.Container, false, false));
        }

        public SecureTradeInfo From
        {
            get
            {
                return this.m_From;
            }
        }
        public SecureTradeInfo To
        {
            get
            {
                return this.m_To;
            }
        }
        public bool Valid
        {
            get
            {
                return this.m_Valid;
            }
        }
        public void Cancel()
        {
            if (!this.m_Valid)
                return;

            List<Item> list = this.m_From.Container.Items;

            for (int i = list.Count - 1; i >= 0; --i)
            {
                if (i < list.Count)
                {
                    Item item = list[i];

                    item.OnSecureTrade(this.m_From.Mobile, this.m_To.Mobile, this.m_From.Mobile, false);

                    if (!item.Deleted)
                        this.m_From.Mobile.AddToBackpack(item);
                }
            }

            list = this.m_To.Container.Items;

            for (int i = list.Count - 1; i >= 0; --i)
            {
                if (i < list.Count)
                {
                    Item item = list[i];

                    item.OnSecureTrade(this.m_To.Mobile, this.m_From.Mobile, this.m_To.Mobile, false);

                    if (!item.Deleted)
                        this.m_To.Mobile.AddToBackpack(item);
                }
            }

            this.Close();
        }

        public void Close()
        {
            if (!this.m_Valid)
                return;

            this.m_From.Mobile.Send(new CloseSecureTrade(this.m_From.Container));
            this.m_To.Mobile.Send(new CloseSecureTrade(this.m_To.Container));

            this.m_Valid = false;

            NetState ns = this.m_From.Mobile.NetState;

            if (ns != null)
                ns.RemoveTrade(this);

            ns = this.m_To.Mobile.NetState;

            if (ns != null)
                ns.RemoveTrade(this);

            Timer.DelayCall(TimeSpan.Zero, delegate { this.m_From.Container.Delete(); });
            Timer.DelayCall(TimeSpan.Zero, delegate { this.m_To.Container.Delete(); });
        }

        public void Update()
        {
            if (!this.m_Valid)
                return;

            if (this.m_From.Accepted && this.m_To.Accepted)
            {
                List<Item> list = this.m_From.Container.Items;

                bool allowed = true;

                for (int i = list.Count - 1; allowed && i >= 0; --i)
                {
                    if (i < list.Count)
                    {
                        Item item = list[i];

                        if (!item.AllowSecureTrade(this.m_From.Mobile, this.m_To.Mobile, this.m_To.Mobile, true))
                            allowed = false;
                    }
                }

                list = this.m_To.Container.Items;

                for (int i = list.Count - 1; allowed && i >= 0; --i)
                {
                    if (i < list.Count)
                    {
                        Item item = list[i];

                        if (!item.AllowSecureTrade(this.m_To.Mobile, this.m_From.Mobile, this.m_From.Mobile, true))
                            allowed = false;
                    }
                }

                if (!allowed)
                {
                    this.m_From.Accepted = false;
                    this.m_To.Accepted = false;

                    this.m_From.Mobile.Send(new UpdateSecureTrade(this.m_From.Container, this.m_From.Accepted, this.m_To.Accepted));
                    this.m_To.Mobile.Send(new UpdateSecureTrade(this.m_To.Container, this.m_To.Accepted, this.m_From.Accepted));

                    return;
                }

                list = this.m_From.Container.Items;

                for (int i = list.Count - 1; i >= 0; --i)
                {
                    if (i < list.Count)
                    {
                        Item item = list[i];

                        item.OnSecureTrade(this.m_From.Mobile, this.m_To.Mobile, this.m_To.Mobile, true);

                        if (!item.Deleted)
                            this.m_To.Mobile.AddToBackpack(item);
                    }
                }

                list = this.m_To.Container.Items;

                for (int i = list.Count - 1; i >= 0; --i)
                {
                    if (i < list.Count)
                    {
                        Item item = list[i];

                        item.OnSecureTrade(this.m_To.Mobile, this.m_From.Mobile, this.m_From.Mobile, true);

                        if (!item.Deleted)
                            this.m_From.Mobile.AddToBackpack(item);
                    }
                }

                this.Close();
            }
            else
            {
                this.m_From.Mobile.Send(new UpdateSecureTrade(this.m_From.Container, this.m_From.Accepted, this.m_To.Accepted));
                this.m_To.Mobile.Send(new UpdateSecureTrade(this.m_To.Container, this.m_To.Accepted, this.m_From.Accepted));
            }
        }
    }

    public class SecureTradeInfo
    {
        private readonly SecureTrade m_Owner;
        private readonly Mobile m_Mobile;
        private readonly SecureTradeContainer m_Container;
        private bool m_Accepted;
        public SecureTradeInfo(SecureTrade owner, Mobile m, SecureTradeContainer c)
        {
            this.m_Owner = owner;
            this.m_Mobile = m;
            this.m_Container = c;

            this.m_Mobile.AddItem(this.m_Container);
        }

        public SecureTrade Owner
        {
            get
            {
                return this.m_Owner;
            }
        }
        public Mobile Mobile
        {
            get
            {
                return this.m_Mobile;
            }
        }
        public SecureTradeContainer Container
        {
            get
            {
                return this.m_Container;
            }
        }
        public bool Accepted
        {
            get
            {
                return this.m_Accepted;
            }
            set
            {
                this.m_Accepted = value;
            }
        }
    }
}