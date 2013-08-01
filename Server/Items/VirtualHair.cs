using System;
using Server.Network;

namespace Server
{
    public abstract class BaseHairInfo
    {
        private int m_ItemID;
        private int m_Hue;
        protected BaseHairInfo(int itemid)
            : this(itemid, 0)
        {
        }

        protected BaseHairInfo(int itemid, int hue)
        {
            this.m_ItemID = itemid;
            this.m_Hue = hue;
        }

        protected BaseHairInfo(GenericReader reader)
        {
            int version = reader.ReadInt();

            switch( version )
            {
                case 0:
                    {
                        this.m_ItemID = reader.ReadInt();
                        this.m_Hue = reader.ReadInt();
                        break;
                    }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ItemID
        {
            get
            {
                return this.m_ItemID;
            }
            set
            {
                this.m_ItemID = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public int Hue
        {
            get
            {
                return this.m_Hue;
            }
            set
            {
                this.m_Hue = value;
            }
        }
        public virtual void Serialize(GenericWriter writer)
        {
            writer.Write((int)0); //version
            writer.Write((int)this.m_ItemID);
            writer.Write((int)this.m_Hue);
        }
    }

    public class HairInfo : BaseHairInfo
    {
        public HairInfo(int itemid)
            : base(itemid, 0)
        {
        }

        public HairInfo(int itemid, int hue)
            : base(itemid, hue)
        {
        }

        public HairInfo(GenericReader reader)
            : base(reader)
        {
        }

        public static int FakeSerial(Mobile parent)
        {
            return (0x7FFFFFFF - 0x400 - (parent.Serial * 4));
        }
    }

    public class FacialHairInfo : BaseHairInfo
    {
        public FacialHairInfo(int itemid)
            : base(itemid, 0)
        {
        }

        public FacialHairInfo(int itemid, int hue)
            : base(itemid, hue)
        {
        }

        public FacialHairInfo(GenericReader reader)
            : base(reader)
        {
        }

        public static int FakeSerial(Mobile parent)
        {
            return (0x7FFFFFFF - 0x400 - 1 - (parent.Serial * 4));
        }
    }

    public sealed class HairEquipUpdate : Packet
    {
        public HairEquipUpdate(Mobile parent)
            : base(0x2E, 15)
        {
            int hue = parent.HairHue;

            if (parent.SolidHueOverride >= 0)
                hue = parent.SolidHueOverride;

            int hairSerial = HairInfo.FakeSerial(parent);

            this.m_Stream.Write((int)hairSerial);
            this.m_Stream.Write((short)parent.HairItemID);
            this.m_Stream.Write((byte)0);
            this.m_Stream.Write((byte)Layer.Hair);
            this.m_Stream.Write((int)parent.Serial);
            this.m_Stream.Write((short)hue);
        }
    }

    public sealed class FacialHairEquipUpdate : Packet
    {
        public FacialHairEquipUpdate(Mobile parent)
            : base(0x2E, 15)
        {
            int hue = parent.FacialHairHue;

            if (parent.SolidHueOverride >= 0)
                hue = parent.SolidHueOverride;

            int hairSerial = FacialHairInfo.FakeSerial(parent);

            this.m_Stream.Write((int)hairSerial);
            this.m_Stream.Write((short)parent.FacialHairItemID);
            this.m_Stream.Write((byte)0);
            this.m_Stream.Write((byte)Layer.FacialHair);
            this.m_Stream.Write((int)parent.Serial);
            this.m_Stream.Write((short)hue);
        }
    }

    public sealed class RemoveHair : Packet
    {
        public RemoveHair(Mobile parent)
            : base(0x1D, 5)
        {
            this.m_Stream.Write((int)HairInfo.FakeSerial(parent));
        }
    }

    public sealed class RemoveFacialHair : Packet
    {
        public RemoveFacialHair(Mobile parent)
            : base(0x1D, 5)
        {
            this.m_Stream.Write((int)FacialHairInfo.FakeSerial(parent));
        }
    }
}