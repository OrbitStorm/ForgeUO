using System;
using System.Collections.Generic;
using Server.ContextMenus;
using Server.Items;
using Server.Regions;
using BunnyHole = Server.Mobiles.VorpalBunny.BunnyHole;

namespace Server.Mobiles
{
    public class BaseTalismanSummon : BaseCreature
    {
        public BaseTalismanSummon()
            : base(AIType.AI_Melee, FightMode.None, 10, 1, 0.2, 0.4)
        {
        }

        public BaseTalismanSummon(Serial serial)
            : base(serial)
        {
        }

        public override bool Commandable
        {
            get
            {
                return false;
            }
        }
        public override bool InitialInnocent
        {
            get
            {
                return true;
            }
        }
        public virtual bool IsInvulnerable
        {
            get
            {
                return true;
            }
        }
        public override void AddCustomContextEntries(Mobile from, List<ContextMenuEntry> list)
        {
            if (from.Alive && this.ControlMaster == from)
                list.Add(new TalismanReleaseEntry(this));
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();
        }

        private class TalismanReleaseEntry : ContextMenuEntry
        {
            private readonly Mobile m_Mobile;
            public TalismanReleaseEntry(Mobile m)
                : base(6118, 3)
            {
                this.m_Mobile = m;
            }

            public override void OnClick()
            {
                Effects.SendLocationParticles(EffectItem.Create(this.m_Mobile.Location, this.m_Mobile.Map, EffectItem.DefaultDuration), 0x3728, 8, 20, 5042);
                Effects.PlaySound(this.m_Mobile, this.m_Mobile.Map, 0x201);

                this.m_Mobile.Delete();
            }
        }
    }

    public class SummonedAntLion : BaseTalismanSummon
    {
        [Constructable]
        public SummonedAntLion()
            : base()
        {
            this.Name = "an ant lion";
            this.Body = 787;
            this.BaseSoundID = 1006;
        }

        public SummonedAntLion(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();
        }
    }

    public class SummonedArcticOgreLord : BaseTalismanSummon
    {
        [Constructable]
        public SummonedArcticOgreLord()
            : base()
        {
            this.Name = "an arctic ogre lord";
            this.Body = 135;
            this.BaseSoundID = 427;
        }

        public SummonedArcticOgreLord(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();
        }
    }

    public class SummonedBakeKitsune : BaseTalismanSummon
    {
        [Constructable]
        public SummonedBakeKitsune()
            : base()
        {
            this.Name = "a bake kitsune";
            this.Body = 246;
            this.BaseSoundID = 0x4DD;
        }

        public SummonedBakeKitsune(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();
        }
    }

    public class SummonedBogling : BaseTalismanSummon
    {
        [Constructable]
        public SummonedBogling()
            : base()
        {
            this.Name = "a bogling";
            this.Body = 779;
            this.BaseSoundID = 422;
        }

        public SummonedBogling(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();
        }
    }

    public class SummonedBullFrog : BaseTalismanSummon
    {
        [Constructable]
        public SummonedBullFrog()
            : base()
        {
            this.Name = "a bull frog";
            this.Body = 81;
            this.Hue = Utility.RandomList(0x5AC, 0x5A3, 0x59A, 0x591, 0x588, 0x57F);
            this.BaseSoundID = 0x266;
        }

        public SummonedBullFrog(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();
        }
    }

    public class SummonedChicken : BaseTalismanSummon
    {
        [Constructable]
        public SummonedChicken()
            : base()
        {
            this.Name = "a chicken";
            this.Body = 0xD0;
            this.BaseSoundID = 0x6E;
        }

        public SummonedChicken(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();
        }
    }

    public class SummonedCow : BaseTalismanSummon
    {
        [Constructable]
        public SummonedCow()
            : base()
        {
            this.Name = "a cow";
            this.Body = Utility.RandomList(0xD8, 0xE7);
            this.BaseSoundID = 0x78;
        }

        public SummonedCow(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();
        }
    }

    public class SummonedDoppleganger : BaseTalismanSummon
    {
        [Constructable]
        public SummonedDoppleganger()
            : base()
        {
            this.Name = "a doppleganger";
            this.Body = 0x309;
            this.BaseSoundID = 0x451;
        }

        public SummonedDoppleganger(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();
        }
    }

    public class SummonedFrostSpider : BaseTalismanSummon
    {
        [Constructable]
        public SummonedFrostSpider()
            : base()
        {
            this.Name = "a frost spider";
            this.Body = 20;
            this.BaseSoundID = 0x388;
        }

        public SummonedFrostSpider(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();
        }
    }

    public class SummonedGreatHart : BaseTalismanSummon
    {
        [Constructable]
        public SummonedGreatHart()
            : base()
        {
            this.Name = "a great hart";
            this.Body = 0xEA;
            this.BaseSoundID = 0x82;
        }

        public SummonedGreatHart(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();
        }
    }

    public class SummonedLavaSerpent : BaseTalismanSummon
    {
        private DateTime m_NextWave;
        [Constructable]
        public SummonedLavaSerpent()
            : base()
        {
            this.Name = "a lava serpent";
            this.Body = 90;
            this.BaseSoundID = 219;
        }

        public SummonedLavaSerpent(Serial serial)
            : base(serial)
        {
        }

        public override void OnThink()
        {
            if (this.m_NextWave < DateTime.Now)
                this.AreaHeatDamage();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();
        }

        public void AreaHeatDamage()
        {
            Mobile mob = this.ControlMaster;

            if (mob != null)
            {
                if (mob.InRange(this.Location, 2))
                {
                    if (mob.IsStaff())
                    {
                        AOS.Damage(mob, Utility.Random(2, 3), 0, 100, 0, 0, 0);
                        mob.SendLocalizedMessage(1008112); // The intense heat is damaging you!
                    }
                }

                GuardedRegion r = this.Region as GuardedRegion;
				
                if (r != null && mob.Alive)
                {
                    foreach (Mobile m in this.GetMobilesInRange(2))
                    {
                        if (!mob.CanBeHarmful(m))
                            mob.CriminalAction(false);
                    }
                }
            }

            this.m_NextWave = DateTime.Now + TimeSpan.FromSeconds(3);
        }
    }

    public class SummonedOrcBrute : BaseTalismanSummon
    {
        [Constructable]
        public SummonedOrcBrute()
            : base()
        {
            this.Body = 189;
            this.Name = "an orc brute";
            this.BaseSoundID = 0x45A;
        }

        public SummonedOrcBrute(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();
        }
    }

    public class SummonedPanther : BaseTalismanSummon
    {
        [Constructable]
        public SummonedPanther()
            : base()
        {
            this.Name = "a panther";
            this.Body = 0xD6;
            this.Hue = 0x901;
            this.BaseSoundID = 0x462;
        }

        public SummonedPanther(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();
        }
    }

    public class SummonedSheep : BaseTalismanSummon
    {
        [Constructable]
        public SummonedSheep()
            : base()
        {
            this.Name = "a sheep";
            this.Body = 0xCF;
            this.BaseSoundID = 0xD6;
        }

        public SummonedSheep(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();
        }
    }

    public class SummonedSkeletalKnight : BaseTalismanSummon
    {
        [Constructable]
        public SummonedSkeletalKnight()
            : base()
        {
            this.Name = "a skeletal knight";
            this.Body = 147;
            this.BaseSoundID = 451;
        }

        public SummonedSkeletalKnight(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();
        }
    }

    public class SummonedVorpalBunny : BaseTalismanSummon
    {
        [Constructable]
        public SummonedVorpalBunny()
            : base()
        {
            this.Name = "a vorpal bunny";
            this.Body = 205;
            this.Hue = 0x480;
            this.BaseSoundID = 0xC9;

            Timer.DelayCall(TimeSpan.FromMinutes(30.0), new TimerCallback(BeginTunnel));
        }

        public SummonedVorpalBunny(Serial serial)
            : base(serial)
        {
        }

        public virtual void BeginTunnel()
        {
            if (this.Deleted)
                return;

            new BunnyHole().MoveToWorld(this.Location, this.Map);

            this.Frozen = true;
            this.Say("* The bunny begins to dig a tunnel back to its underground lair *");
            this.PlaySound(0x247);

            Timer.DelayCall(TimeSpan.FromSeconds(5.0), new TimerCallback(Delete));
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();
        }
    }

    public class SummonedWailingBanshee : BaseTalismanSummon
    {
        [Constructable]
        public SummonedWailingBanshee()
            : base()
        {
            this.Name = "a wailing banshee";
            this.Body = 310;
            this.BaseSoundID = 0x482;
        }

        public SummonedWailingBanshee(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();
        }
    }
}