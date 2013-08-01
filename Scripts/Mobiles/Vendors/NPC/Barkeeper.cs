using System;
using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
    public class Barkeeper : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new List<SBInfo>();
        [Constructable]
        public Barkeeper()
            : base("the barkeeper")
        {
        }

        public Barkeeper(Serial serial)
            : base(serial)
        {
        }

        public override VendorShoeType ShoeType
        {
            get
            {
                return Utility.RandomBool() ? VendorShoeType.ThighBoots : VendorShoeType.Boots;
            }
        }
        protected override List<SBInfo> SBInfos
        {
            get
            {
                return this.m_SBInfos;
            }
        }
        public override void InitSBInfo()
        {
            this.m_SBInfos.Add(new SBBarkeeper()); 
        }

        public override void InitOutfit()
        {
            base.InitOutfit();

            this.AddItem(new HalfApron(this.RandomBrightHue()));
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