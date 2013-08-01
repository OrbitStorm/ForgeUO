using System;
using System.Collections.Generic;

namespace Server.Mobiles
{
    public class BuyItemStateComparer : IComparer<BuyItemState>
    {
        public int Compare(BuyItemState l, BuyItemState r)
        {
            if (l == null && r == null)
                return 0;
            if (l == null)
                return -1;
            if (r == null)
                return 1;

            return l.MySerial.CompareTo(r.MySerial);
        }
    }

    public class BuyItemResponse
    {
        private readonly Serial m_Serial;
        private readonly int m_Amount;
        public BuyItemResponse(Serial serial, int amount)
        {
            this.m_Serial = serial;
            this.m_Amount = amount;
        }

        public Serial Serial
        {
            get
            {
                return this.m_Serial;
            }
        }
        public int Amount
        {
            get
            {
                return this.m_Amount;
            }
        }
    }

    public class SellItemResponse
    {
        private readonly Item m_Item;
        private readonly int m_Amount;
        public SellItemResponse(Item i, int amount)
        {
            this.m_Item = i;
            this.m_Amount = amount;
        }

        public Item Item
        {
            get
            {
                return this.m_Item;
            }
        }
        public int Amount
        {
            get
            {
                return this.m_Amount;
            }
        }
    }

    public class SellItemState
    {
        private readonly Item m_Item;
        private readonly int m_Price;
        private readonly string m_Name;
        public SellItemState(Item item, int price, string name)
        {
            this.m_Item = item;
            this.m_Price = price;
            this.m_Name = name;
        }

        public Item Item
        {
            get
            {
                return this.m_Item;
            }
        }
        public int Price
        {
            get
            {
                return this.m_Price;
            }
        }
        public string Name
        {
            get
            {
                return this.m_Name;
            }
        }
    }

    public class BuyItemState
    {
        private readonly Serial m_ContSer;
        private readonly Serial m_MySer;
        private readonly int m_ItemID;
        private readonly int m_Amount;
        private readonly int m_Hue;
        private readonly int m_Price;
        private readonly string m_Desc;
        public BuyItemState(string name, Serial cont, Serial serial, int price, int amount, int itemID, int hue)
        {
            this.m_Desc = name;
            this.m_ContSer = cont;
            this.m_MySer = serial;
            this.m_Price = price;
            this.m_Amount = amount;
            this.m_ItemID = itemID;
            this.m_Hue = hue;
        }

        public int Price
        {
            get
            {
                return this.m_Price;
            }
        }
        public Serial MySerial
        {
            get
            {
                return this.m_MySer;
            }
        }
        public Serial ContainerSerial
        {
            get
            {
                return this.m_ContSer;
            }
        }
        public int ItemID
        {
            get
            {
                return this.m_ItemID;
            }
        }
        public int Amount
        {
            get
            {
                return this.m_Amount;
            }
        }
        public int Hue
        {
            get
            {
                return this.m_Hue;
            }
        }
        public string Description
        {
            get
            {
                return this.m_Desc;
            }
        }
    }
}