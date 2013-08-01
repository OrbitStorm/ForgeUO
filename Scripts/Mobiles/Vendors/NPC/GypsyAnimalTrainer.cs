using System;

namespace Server.Mobiles
{
    public class GypsyAnimalTrainer : AnimalTrainer
    {
        [Constructable]
        public GypsyAnimalTrainer()
        {
            if (Utility.RandomBool())
                this.Title = "the gypsy animal trainer";
            else
                this.Title = "the gypsy animal herder";
        }

        public GypsyAnimalTrainer(Serial serial)
            : base(serial)
        {
        }

        public override VendorShoeType ShoeType
        {
            get
            {
                return this.Female ? VendorShoeType.ThighBoots : VendorShoeType.Boots;
            }
        }
        public override int GetShoeHue()
        {
            return 0;
        }

        public override void InitOutfit()
        {
            base.InitOutfit();

            Item item = this.FindItemOnLayer(Layer.Pants);

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