using System;
using Server.Items;
using Server.Mobiles;

namespace Server.Spells.Spellweaving
{
    public abstract class ArcanistSpell : Spell
    {
        private int m_CastTimeFocusLevel;
        public ArcanistSpell(Mobile caster, Item scroll, SpellInfo info)
            : base(caster, scroll, info)
        {
        }

        public abstract double RequiredSkill { get; }
        public abstract int RequiredMana { get; }
        public override SkillName CastSkill
        {
            get
            {
                return SkillName.Spellweaving;
            }
        }
        public override SkillName DamageSkill
        {
            get
            {
                return SkillName.Spellweaving;
            }
        }
        public override bool ClearHandsOnCast
        {
            get
            {
                return false;
            }
        }
        public virtual int FocusLevel
        {
            get
            {
                return this.m_CastTimeFocusLevel;
            }
        }
        public static int GetFocusLevel(Mobile from)
        {
            ArcaneFocus focus = FindArcaneFocus(from);

            if (focus == null || focus.Deleted)
                return 0;

            return focus.StrengthBonus;
        }

        public static ArcaneFocus FindArcaneFocus(Mobile from)
        {
            if (from == null || from.Backpack == null)
                return null;

            if (from.Holding is ArcaneFocus)
                return (ArcaneFocus)from.Holding;

            return from.Backpack.FindItemByType<ArcaneFocus>();
        }

        public static bool CheckExpansion(Mobile from)
        {
            if (!(from is PlayerMobile))
                return true;

            if (from.NetState == null)
                return false;

            return from.NetState.SupportsExpansion(Expansion.ML);
        }

        public override bool CheckCast()
        {
            if (!base.CheckCast())
                return false;

            if (!CheckExpansion(this.Caster))
            {
                this.Caster.SendLocalizedMessage(1072176); // You must upgrade to the Mondain's Legacy Expansion Pack before using that ability
                return false;
            }

            if (!MondainsLegacy.Spellweaving)
            {
                this.Caster.SendLocalizedMessage(1042753, "Spellweaving"); // ~1_SOMETHING~ has been temporarily disabled.
                return false;
            }

            if (this.Caster is PlayerMobile && !((PlayerMobile)this.Caster).Spellweaving)
            {
                this.Caster.SendLocalizedMessage(1073220); // You must have completed the epic arcanist quest to use this ability.
                return false;
            }

            int mana = this.ScaleMana(this.RequiredMana);

            if (this.Caster.Mana < mana)
            {
                this.Caster.SendLocalizedMessage(1060174, mana.ToString()); // You must have at least ~1_MANA_REQUIREMENT~ Mana to use this ability.
                return false;
            }
            else if (this.Caster.Skills[this.CastSkill].Value < this.RequiredSkill)
            {
                this.Caster.SendLocalizedMessage(1063013, String.Format("{0}\t{1}", this.RequiredSkill.ToString("F1"), "#1044114")); // You need at least ~1_SKILL_REQUIREMENT~ ~2_SKILL_NAME~ skill to use that ability.
                return false;
            }

            return true;
        }

        public override void GetCastSkills(out double min, out double max)
        {
            min = this.RequiredSkill - 12.5;	//per 5 on friday, 2/16/07
            max = this.RequiredSkill + 37.5;
        }

        public override int GetMana()
        {
            return this.RequiredMana;
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
            this.m_CastTimeFocusLevel = GetFocusLevel(this.Caster);
        }

        public virtual void SendCastEffect()
        {
            this.Caster.FixedEffect(0x37C4, 10, (int)(this.GetCastDelay().TotalSeconds * 28), 4, 3);
        }

        public virtual bool CheckResisted(Mobile m)
        {
            double percent = (50 + 2 * (this.GetResistSkill(m) - this.GetDamageSkill(this.Caster))) / 100;	//TODO: According to the guide this is it.. but.. is it correct per OSI?

            if (percent <= 0)
                return false;

            if (percent >= 1.0)
                return true;

            return (percent >= Utility.RandomDouble());
        }
    }
}