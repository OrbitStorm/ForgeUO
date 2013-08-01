using System;
using Server.Items;

namespace Server.Mobiles
{
    public class GypsyBanker : Banker
    {
        [Constructable]
        public GypsyBanker()
        {
            this.Title = "the gypsy banker";
        }

        public GypsyBanker(Serial serial)
            : base(serial)
        {
        }

        public override bool IsActiveVendor
        {
            get
            {
                return false;
            }
        }
        public override NpcGuild NpcGuild
        {
            get
            {
                return NpcGuild.None;
            }
        }
        public override bool ClickTitle
        {
            get
            {
                return false;
            }
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

            Item item = this.FindItemOnLayer(Layer.Pants);

            if (item != null)
                item.Hue = this.RandomBrightHue();

            item = this.FindItemOnLayer(Layer.Shoes);

            if (item != null)
                item.Hue = this.RandomBrightHue();

            item = this.FindItemOnLayer(Layer.OuterLegs);

            if (item != null)
                item.Hue = this.RandomBrightHue();

            item = this.FindItemOnLayer(Layer.InnerLegs);

            if (item != null)
                item.Hue = this.RandomBrightHue();

            item = this.FindItemOnLayer(Layer.OuterTorso);

            if (item != null)
                item.Hue = this.RandomBrightHue();

            item = this.FindItemOnLayer(Layer.InnerTorso);

            if (item != null)
                item.Hue = this.RandomBrightHue();

            item = this.FindItemOnLayer(Layer.Shirt);

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