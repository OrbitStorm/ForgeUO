using System;
using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
    public class Vagabond : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new List<SBInfo>();
        [Constructable]
        public Vagabond()
            : base("the vagabond")
        {
            this.SetSkill(SkillName.ItemID, 60.0, 83.0);
        }

        public Vagabond(Serial serial)
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
        public override void InitSBInfo()
        {
            this.m_SBInfos.Add(new SBTinker());
            this.m_SBInfos.Add(new SBVagabond());
        }

        public override void InitOutfit()
        {
            this.AddItem(new FancyShirt(this.RandomBrightHue()));
            this.AddItem(new Shoes(this.GetShoeHue()));
            this.AddItem(new LongPants(this.GetRandomHue()));

            if (Utility.RandomBool())
                this.AddItem(new Cloak(this.RandomBrightHue()));

            switch ( Utility.Random(2) )
            {
                case 0:
                    this.AddItem(new SkullCap(Utility.RandomNeutralHue()));
                    break;
                case 1:
                    this.AddItem(new Bandana(Utility.RandomNeutralHue()));
                    break;
            }

            Utility.AssignRandomHair(this);
            Utility.AssignRandomFacialHair(this, this.HairHue);

            this.PackGold(100, 200);
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