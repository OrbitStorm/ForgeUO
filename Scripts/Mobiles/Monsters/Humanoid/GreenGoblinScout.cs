using System;
using Server.Targeting;

namespace Server.Mobiles
{
    [CorpseName("an goblin corpse")]
    public class GreenGoblinScout : BaseCreature
    {
        //public override InhumanSpeech SpeechType { get { return InhumanSpeech.Orc; } }
        [Constructable]
        public GreenGoblinScout()
            : base(AIType.AI_OrcScout, FightMode.Closest, 10, 7, 0.2, 0.4)
        {
            this.Name = "an green goblin scout";
            this.Body = 723;
            this.BaseSoundID = 0x45A;

            this.SetStr(250, 261);
            this.SetDex(65, 70);
            this.SetInt(105, 108);

            this.SetHits(200, 204);
            this.SetMana(100, 108);

            this.SetDamage(5, 7);

            this.SetDamageType(ResistanceType.Physical, 100);

            this.SetResistance(ResistanceType.Physical, 35, 45);
            this.SetResistance(ResistanceType.Fire, 30, 33);
            this.SetResistance(ResistanceType.Cold, 25, 28);
            this.SetResistance(ResistanceType.Poison, 10, 13);
            this.SetResistance(ResistanceType.Energy, 10, 11);

            this.SetSkill(SkillName.MagicResist, 105.1, 110.2);
            this.SetSkill(SkillName.Tactics, 85.1, 89.1);

            this.SetSkill(SkillName.Wrestling, 90.1, 92.9);
            this.SetSkill(SkillName.Anatomy, 70.1, 80.3);

            this.Fame = 1500;
            this.Karma = -1500;
        }

        public GreenGoblinScout(Serial serial)
            : base(serial)
        {
        }

        public override OppositionGroup OppositionGroup
        {
            get
            {
                return OppositionGroup.SavagesAndOrcs;
            }
        }
        public override bool CanRummageCorpses
        {
            get
            {
                return true;
            }
        }
        public override int Meat
        {
            get
            {
                return 1;
            }
        }
        public override void GenerateLoot()
        {
            this.AddLoot(LootPack.Meager);
        }

        public override void OnThink()
        {
            if (Utility.RandomDouble() < 0.2)
                this.TryToDetectHidden();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }

        private Mobile FindTarget()
        {
            foreach (Mobile m in this.GetMobilesInRange(10))
            {
                if (m.Player && m.Hidden && m.IsPlayer())
                {
                    return m;
                }
            }

            return null;
        }

        private void TryToDetectHidden()
        {
            Mobile m = this.FindTarget();

            if (m != null)
            {
                if (DateTime.Now >= this.NextSkillTime && this.UseSkill(SkillName.DetectHidden))
                {
                    Target targ = this.Target;

                    if (targ != null)
                        targ.Invoke(this, this);

                    Effects.PlaySound(this.Location, this.Map, 0x340);
                }
            }
        }
    }
}