using System;
using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
    public class GypsyMaiden : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new List<SBInfo>();
        [Constructable]
        public GypsyMaiden()
            : base("the gypsy maiden")
        {
        }

        public GypsyMaiden(Serial serial)
            : base(serial)
        {
        }

        protected override List<SBInfo> SBInfos
        {
            get
            {
                return this.m_SBInfos;
            }
        }
        public override bool GetGender()
        {
            return true; // always female
        }

        public override void InitSBInfo()
        {
            this.m_SBInfos.Add(new SBProvisioner());
        }

        public override void InitOutfit()
        {
            base.InitOutfit();

            switch ( Utility.Random(4) )
            {
                case 0:
                    this.AddItem(new JesterHat(this.RandomBrightHue()));
                    break;
                case 1:
                    this.AddItem(new Bandana(this.RandomBrightHue()));
                    break;
                case 2:
                    this.AddItem(new SkullCap(this.RandomBrightHue()));
                    break;
            }

            if (Utility.RandomBool())
                this.AddItem(new HalfApron(this.RandomBrightHue()));

            Item item = this.FindItemOnLayer(Layer.Pants);

            if (item != null)
                item.Hue = this.RandomBrightHue();

            item = this.FindItemOnLayer(Layer.OuterLegs);

            if (item != null)
                item.Hue = this.RandomBrightHue();

            item = this.FindItemOnLayer(Layer.InnerLegs);

            if (item != null)
                item.Hue = this.RandomBrightHue();
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