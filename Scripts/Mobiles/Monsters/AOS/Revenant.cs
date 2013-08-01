using System;
using Server.Items;
using Server.Targeting;

namespace Server.Mobiles
{
    public class Revenant : BaseCreature
    {
        private readonly Mobile m_Target;
        private readonly DateTime m_ExpireTime;
        public Revenant(Mobile caster, Mobile target, TimeSpan duration)
            : base(AIType.AI_Melee, FightMode.Closest, 10, 1, 0.18, 0.36)
        {
            this.Name = "a revenant";
            this.Body = 400;
            this.Hue = 1;
            // TODO: Sound values?

            double scalar = caster.Skills[SkillName.SpiritSpeak].Value * 0.01;

            this.m_Target = target;
            this.m_ExpireTime = DateTime.Now + duration;

            this.SetStr(200);
            this.SetDex(150);
            this.SetInt(150);

            this.SetDamage(16, 17);

            // Bestiary says 50 phys 50 cold, animal lore says differently
            this.SetDamageType(ResistanceType.Physical, 100);

            this.SetSkill(SkillName.MagicResist, 100.0 * scalar); // magic resist is absolute value of spiritspeak
            this.SetSkill(SkillName.Tactics, 100.0); // always 100
            this.SetSkill(SkillName.Swords, 100.0 * scalar); // not displayed in animal lore but tests clearly show this is influenced
            this.SetSkill(SkillName.DetectHidden, 75.0 * scalar);

            scalar /= 1.2;

            this.SetResistance(ResistanceType.Physical, 40 + (int)(20 * scalar), 50 + (int)(20 * scalar));
            this.SetResistance(ResistanceType.Cold, 40 + (int)(20 * scalar), 50 + (int)(20 * scalar));
            this.SetResistance(ResistanceType.Fire, (int)(20 * scalar));
            this.SetResistance(ResistanceType.Poison, 100);
            this.SetResistance(ResistanceType.Energy, 40 + (int)(20 * scalar), 50 + (int)(20 * scalar));

            this.Fame = 0;
            this.Karma = 0;

            this.ControlSlots = 3;

            this.VirtualArmor = 32;

            Item shroud = new DeathShroud();

            shroud.Hue = 0x455;

            shroud.Movable = false;

            this.AddItem(shroud);

            Halberd weapon = new Halberd();

            weapon.Hue = 1;
            weapon.Movable = false;

            this.AddItem(weapon);
        }

        public Revenant(Serial serial)
            : base(serial)
        {
        }

        public override Mobile ConstantFocus
        {
            get
            {
                return this.m_Target;
            }
        }
        public override bool NoHouseRestrictions
        {
            get
            {
                return true;
            }
        }
        public override double DispelDifficulty
        {
            get
            {
                return 80.0;
            }
        }
        public override double DispelFocus
        {
            get
            {
                return 20.0;
            }
        }
        public override bool AlwaysMurderer
        {
            get
            {
                return true;
            }
        }
        public override bool BleedImmune
        {
            get
            {
                return true;
            }
        }
        public override bool BardImmune
        {
            get
            {
                return true;
            }
        }
        public override Poison PoisonImmune
        {
            get
            {
                return Poison.Lethal;
            }
        }
        public override void DisplayPaperdollTo(Mobile to)
        {
            // Do nothing
        }

        public override void OnThink()
        {
            if (!this.m_Target.Alive || DateTime.Now > this.m_ExpireTime)
            {
                this.Kill();
                return;
            }
            else if (this.Map != this.m_Target.Map || !this.InRange(this.m_Target, 15))
            {
                Map fromMap = this.Map;
                Point3D from = this.Location;

                Map toMap = this.m_Target.Map;
                Point3D to = this.m_Target.Location;

                if (toMap != null)
                {
                    for (int i = 0; i < 5; ++i)
                    {
                        Point3D loc = new Point3D(to.X - 4 + Utility.Random(9), to.Y - 4 + Utility.Random(9), to.Z);

                        if (toMap.CanSpawnMobile(loc))
                        {
                            to = loc;
                            break;
                        }
                        else
                        {
                            loc.Z = toMap.GetAverageZ(loc.X, loc.Y);

                            if (toMap.CanSpawnMobile(loc))
                            {
                                to = loc;
                                break;
                            }
                        }
                    }
                }

                this.Map = toMap;
                this.Location = to;

                this.ProcessDelta();

                Effects.SendLocationParticles(EffectItem.Create(from, fromMap, EffectItem.DefaultDuration), 0x3728, 1, 13, 37, 7, 5023, 0);
                this.FixedParticles(0x3728, 1, 13, 5023, 37, 7, EffectLayer.Waist);

                this.PlaySound(0x37D);
            }

            if (this.m_Target.Hidden && this.InRange(this.m_Target, 3) && DateTime.Now >= this.NextSkillTime && this.UseSkill(SkillName.DetectHidden))
            {
                Target targ = this.Target;

                if (targ != null)
                    targ.Invoke(this, this);
            }

            this.Combatant = this.m_Target;
            this.FocusMob = this.m_Target;

            if (this.AIObject != null)
                this.AIObject.Action = ActionType.Combat;

            base.OnThink();
        }

        public override bool OnBeforeDeath()
        {
            Effects.PlaySound(this.Location, this.Map, 0x10B);
            Effects.SendLocationParticles(EffectItem.Create(this.Location, this.Map, TimeSpan.FromSeconds(10.0)), 0x37CC, 1, 50, 2101, 7, 9909, 0);

            this.Delete();
            return false;
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

            this.Delete();
        }
    }
}