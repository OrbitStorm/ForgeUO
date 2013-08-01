using System;
using Server.Network;

namespace Server.Spells.Chivalry
{
    public abstract class PaladinSpell : Spell
    {
        public PaladinSpell(Mobile caster, Item scroll, SpellInfo info)
            : base(caster, scroll, info)
        {
        }

        public abstract double RequiredSkill { get; }
        public abstract int RequiredMana { get; }
        public abstract int RequiredTithing { get; }
        public abstract int MantraNumber { get; }
        public override SkillName CastSkill
        {
            get
            {
                return SkillName.Chivalry;
            }
        }
        public override SkillName DamageSkill
        {
            get
            {
                return SkillName.Chivalry;
            }
        }
        public override bool ClearHandsOnCast
        {
            get
            {
                return false;
            }
        }
        //public override int CastDelayBase{ get{ return 1; } }
        public override int CastRecoveryBase
        {
            get
            {
                return 7;
            }
        }
        public static int ComputePowerValue(Mobile from, int div)
        {
            if (from == null)
                return 0;

            int v = (int)Math.Sqrt(from.Karma + 20000 + (from.Skills.Chivalry.Fixed * 10));

            return v / div;
        }

        public override bool CheckCast()
        {
            int mana = this.ScaleMana(this.RequiredMana);

            if (!base.CheckCast())
                return false;

            if (this.Caster.TithingPoints < this.RequiredTithing)
            {
                this.Caster.SendLocalizedMessage(1060173, this.RequiredTithing.ToString()); // You must have at least ~1_TITHE_REQUIREMENT~ Tithing Points to use this ability,
                return false;
            }
            else if (this.Caster.Mana < mana)
            {
                this.Caster.SendLocalizedMessage(1060174, mana.ToString()); // You must have at least ~1_MANA_REQUIREMENT~ Mana to use this ability.
                return false;
            }

            return true;
        }

        public override bool CheckFizzle()
        {
            int requiredTithing = this.RequiredTithing;

            if (AosAttributes.GetValue(this.Caster, AosAttribute.LowerRegCost) > Utility.Random(100))
                requiredTithing = 0;

            int mana = this.ScaleMana(this.RequiredMana);

            if (this.Caster.TithingPoints < requiredTithing)
            {
                this.Caster.SendLocalizedMessage(1060173, this.RequiredTithing.ToString()); // You must have at least ~1_TITHE_REQUIREMENT~ Tithing Points to use this ability,
                return false;
            }
            else if (this.Caster.Mana < mana)
            {
                this.Caster.SendLocalizedMessage(1060174, mana.ToString()); // You must have at least ~1_MANA_REQUIREMENT~ Mana to use this ability.
                return false;
            }

            this.Caster.TithingPoints -= requiredTithing;

            if (!base.CheckFizzle())
                return false;

            this.Caster.Mana -= mana;

            return true;
        }

        public override void SayMantra()
        {
            this.Caster.PublicOverheadMessage(MessageType.Regular, 0x3B2, this.MantraNumber, "", false);
        }

        public override void DoFizzle()
        {
            this.Caster.PlaySound(0x1D6);
            this.Caster.NextSpellTime = DateTime.Now;
        }

        public override void DoHurtFizzle()
        {
            this.Caster.PlaySound(0x1D6);
        }

        public override void OnDisturb(DisturbType type, bool message)
        {
            base.OnDisturb(type, message);

            if (message)
                this.Caster.PlaySound(0x1D6);
        }

        public override void OnBeginCast()
        {
            base.OnBeginCast();

            this.SendCastEffect();
        }

        public virtual void SendCastEffect()
        {
            this.Caster.FixedEffect(0x37C4, 10, (int)(this.GetCastDelay().TotalSeconds * 28), 4, 3);
        }

        public override void GetCastSkills(out double min, out double max)
        {
            min = this.RequiredSkill;
            max = this.RequiredSkill + 50.0;
        }

        public override int GetMana()
        {
            return 0;
        }

        public int ComputePowerValue(int div)
        {
            return ComputePowerValue(this.Caster, div);
        }
    }
}