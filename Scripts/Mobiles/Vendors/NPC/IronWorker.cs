using System;
using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
    public class IronWorker : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new List<SBInfo>();
        [Constructable]
        public IronWorker()
            : base("the iron worker")
        {
            this.SetSkill(SkillName.ArmsLore, 36.0, 68.0);
            this.SetSkill(SkillName.Blacksmith, 65.0, 88.0);
            this.SetSkill(SkillName.Fencing, 60.0, 83.0);
            this.SetSkill(SkillName.Macing, 61.0, 93.0);
            this.SetSkill(SkillName.Swords, 60.0, 83.0);
            this.SetSkill(SkillName.Tactics, 60.0, 83.0);
            this.SetSkill(SkillName.Parry, 61.0, 93.0);
        }

        public IronWorker(Serial serial)
            : base(serial)
        {
        }

        public override VendorShoeType ShoeType
        {
            get
            {
                return VendorShoeType.None;
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
            this.m_SBInfos.Add(new SBAxeWeapon());
            this.m_SBInfos.Add(new SBKnifeWeapon());
            this.m_SBInfos.Add(new SBMaceWeapon());
            this.m_SBInfos.Add(new SBSmithTools());
            this.m_SBInfos.Add(new SBPoleArmWeapon());
            this.m_SBInfos.Add(new SBSpearForkWeapon());
            this.m_SBInfos.Add(new SBSwordWeapon());

            this.m_SBInfos.Add(new SBMetalShields());

            this.m_SBInfos.Add(new SBHelmetArmor());
            this.m_SBInfos.Add(new SBPlateArmor());
            this.m_SBInfos.Add(new SBChainmailArmor());
            this.m_SBInfos.Add(new SBRingmailArmor());
            this.m_SBInfos.Add(new SBStuddedArmor());
            this.m_SBInfos.Add(new SBLeatherArmor());
        }

        public override void InitOutfit()
        {
            base.InitOutfit();

            Item item = (Utility.RandomBool() ? null : new Server.Items.RingmailChest());

            if (item != null && !this.EquipItem(item))
            {
                item.Delete();
                item = null;
            }

            switch ( Utility.Random(3) )
            {
                case 0:
                case 1:
                    this.AddItem(new JesterHat(this.RandomBrightHue()));
                    break;
                case 2:
                    this.AddItem(new Bandana(this.RandomBrightHue()));
                    break;
            }

            if (item == null)
                this.AddItem(new FullApron(this.RandomBrightHue()));

            this.AddItem(new Bascinet());
            this.AddItem(new SmithHammer());

            item = this.FindItemOnLayer(Layer.Pants);

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