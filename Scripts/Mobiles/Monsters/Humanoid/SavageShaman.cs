using System;
using System.Collections;
using Server.Items;
using Server.Spells;

namespace Server.Mobiles
{
    [CorpseName("a savage corpse")]
    public class SavageShaman : BaseCreature
    {
        [Constructable]
        public SavageShaman()
            : base(AIType.AI_Mage, FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            this.Name = NameList.RandomName("savage shaman");

            if (Utility.RandomBool())
                this.Body = 184;
            else
                this.Body = 183;

            this.SetStr(126, 145);
            this.SetDex(91, 110);
            this.SetInt(161, 185);

            this.SetDamage(4, 10);

            this.SetDamageType(ResistanceType.Physical, 100);

            this.SetResistance(ResistanceType.Physical, 30, 40);
            this.SetResistance(ResistanceType.Fire, 20, 30);
            this.SetResistance(ResistanceType.Cold, 20, 30);
            this.SetResistance(ResistanceType.Poison, 20, 30);
            this.SetResistance(ResistanceType.Energy, 40, 50);

            this.SetSkill(SkillName.EvalInt, 77.5, 100.0);
            this.SetSkill(SkillName.Fencing, 62.5, 85.0);
            this.SetSkill(SkillName.Macing, 62.5, 85.0);
            this.SetSkill(SkillName.Magery, 72.5, 95.0);
            this.SetSkill(SkillName.Meditation, 77.5, 100.0);
            this.SetSkill(SkillName.MagicResist, 77.5, 100.0);
            this.SetSkill(SkillName.Swords, 62.5, 85.0);
            this.SetSkill(SkillName.Tactics, 62.5, 85.0);
            this.SetSkill(SkillName.Wrestling, 62.5, 85.0);

            this.Fame = 1000;
            this.Karma = -1000;
			
            this.PackReg(10, 15);
            this.PackItem(new Bandage(Utility.RandomMinMax(1, 15)));

            if (0.1 > Utility.RandomDouble())
                this.PackItem(new TribalBerry());

            this.AddItem(new BoneArms());
            this.AddItem(new BoneLegs());
            this.AddItem(new DeerMask());
        }

        public SavageShaman(Serial serial)
            : base(serial)
        {
        }

        public override int Meat
        {
            get
            {
                return 1;
            }
        }
        public override bool AlwaysMurderer
        {
            get
            {
                return true;
            }
        }
        public override bool ShowFameTitle
        {
            get
            {
                return false;
            }
        }
        public override OppositionGroup OppositionGroup
        {
            get
            {
                return OppositionGroup.SavagesAndOrcs;
            }
        }
        public override void GenerateLoot()
        {
            this.AddLoot(LootPack.Average);
        }

        public override bool IsEnemy(Mobile m)
        {
            if (m.BodyMod == 183 || m.BodyMod == 184)
                return false;

            return base.IsEnemy(m);
        }

        public override void AggressiveAction(Mobile aggressor, bool criminal)
        {
            base.AggressiveAction(aggressor, criminal);

            if (aggressor.BodyMod == 183 || aggressor.BodyMod == 184)
            {
                AOS.Damage(aggressor, 50, 0, 100, 0, 0, 0);
                aggressor.BodyMod = 0;
                aggressor.HueMod = -1;
                aggressor.FixedParticles(0x36BD, 20, 10, 5044, EffectLayer.Head);
                aggressor.PlaySound(0x307);
                aggressor.SendLocalizedMessage(1040008); // Your skin is scorched as the tribal paint burns away!

                if (aggressor is PlayerMobile)
                    ((PlayerMobile)aggressor).SavagePaintExpiration = TimeSpan.Zero;
            }
        }

        public override void AlterMeleeDamageTo(Mobile to, ref int damage)
        {
            if (to is Dragon || to is WhiteWyrm || to is SwampDragon || to is Drake || to is Nightmare || to is Hiryu || to is LesserHiryu || to is Daemon)
                damage *= 3;
        }

        public override void OnGotMeleeAttack(Mobile attacker)
        {
            base.OnGotMeleeAttack(attacker);

            if (0.1 > Utility.RandomDouble())
                this.BeginSavageDance();
        }

        public void BeginSavageDance()
        {
            if (this.Map == null)
                return;

            ArrayList list = new ArrayList();

            foreach (Mobile m in this.GetMobilesInRange(8))
            {
                if (m != this && m is SavageShaman)
                    list.Add(m);
            }

            this.Animate(111, 5, 1, true, false, 0); // Do a little dance...

            if (this.AIObject != null)
                this.AIObject.NextMove = DateTime.Now + TimeSpan.FromSeconds(1.0);

            if (list.Count >= 3)
            {
                for (int i = 0; i < list.Count; ++i)
                {
                    SavageShaman dancer = (SavageShaman)list[i];

                    dancer.Animate(111, 5, 1, true, false, 0); // Get down tonight...

                    if (dancer.AIObject != null)
                        dancer.AIObject.NextMove = DateTime.Now + TimeSpan.FromSeconds(1.0);
                }

                Timer.DelayCall(TimeSpan.FromSeconds(1.0), new TimerCallback(EndSavageDance));
            }
        }

        public void EndSavageDance()
        {
            if (this.Deleted)
                return;

            ArrayList list = new ArrayList();

            foreach (Mobile m in this.GetMobilesInRange(8))
                list.Add(m);

            if (list.Count > 0)
            {
                switch ( Utility.Random(3) )
                {
                    case 0: /* greater heal */
                        {
                            foreach (Mobile m in list)
                            {
                                bool isFriendly = (m is Savage || m is SavageRider || m is SavageShaman || m is SavageRidgeback);

                                if (!isFriendly)
                                    continue;

                                if (m.Poisoned || MortalStrike.IsWounded(m) || !this.CanBeBeneficial(m))
                                    continue;

                                this.DoBeneficial(m);

                                // Algorithm: (40% of magery) + (1-10)

                                int toHeal = (int)(this.Skills[SkillName.Magery].Value * 0.4);
                                toHeal += Utility.Random(1, 10);

                                m.Heal(toHeal, this);

                                m.FixedParticles(0x376A, 9, 32, 5030, EffectLayer.Waist);
                                m.PlaySound(0x202);
                            }

                            break;
                        }
                    case 1: /* lightning */
                        {
                            foreach (Mobile m in list)
                            {
                                bool isFriendly = (m is Savage || m is SavageRider || m is SavageShaman || m is SavageRidgeback);

                                if (isFriendly)
                                    continue;

                                if (!this.CanBeHarmful(m))
                                    continue;

                                this.DoHarmful(m);

                                double damage;

                                if (Core.AOS)
                                {
                                    int baseDamage = 6 + (int)(this.Skills[SkillName.EvalInt].Value / 5.0);

                                    damage = Utility.RandomMinMax(baseDamage, baseDamage + 3);
                                }
                                else
                                {
                                    damage = Utility.Random(12, 9);
                                }

                                m.BoltEffect(0);

                                SpellHelper.Damage(TimeSpan.FromSeconds(0.25), m, this, damage, 0, 0, 0, 0, 100);
                            }

                            break;
                        }
                    case 2: /* poison */
                        {
                            foreach (Mobile m in list)
                            {
                                bool isFriendly = (m is Savage || m is SavageRider || m is SavageShaman || m is SavageRidgeback);

                                if (isFriendly)
                                    continue;

                                if (!this.CanBeHarmful(m))
                                    continue;

                                this.DoHarmful(m);

                                if (m.Spell != null)
                                    m.Spell.OnCasterHurt();

                                m.Paralyzed = false;

                                double total = this.Skills[SkillName.Magery].Value + this.Skills[SkillName.Poisoning].Value;

                                double dist = this.GetDistanceToSqrt(m);

                                if (dist >= 3.0)
                                    total -= (dist - 3.0) * 10.0;

                                int level;

                                if (total >= 200.0 && Utility.Random(1, 100) <= 10)
                                    level = 3;
                                else if (total > 170.0)
                                    level = 2;
                                else if (total > 130.0)
                                    level = 1;
                                else
                                    level = 0;

                                m.ApplyPoison(this, Poison.GetPoison(level));

                                m.FixedParticles(0x374A, 10, 15, 5021, EffectLayer.Waist);
                                m.PlaySound(0x474);
                            }

                            break;
                        }
                }
            }
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
    }
}