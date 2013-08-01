using System;
using Server.Items;

//
// This is a first simple AI
//
//
namespace Server.Mobiles
{
    public class ThiefAI : BaseAI
    {
        private Item m_toDisarm;
        public ThiefAI(BaseCreature m)
            : base(m)
        {
        }

        public override bool DoActionWander()
        {
            this.m_Mobile.DebugSay("I have no combatant");

            if (this.AcquireFocusMob(this.m_Mobile.RangePerception, this.m_Mobile.FightMode, false, false, true))
            {
                this.m_Mobile.DebugSay("I have detected {0}, attacking", this.m_Mobile.FocusMob.Name);

                this.m_Mobile.Combatant = this.m_Mobile.FocusMob;
                this.Action = ActionType.Combat;
            }
            else
            {
                base.DoActionWander();
            }

            return true;
        }

        public override bool DoActionCombat()
        {
            Mobile combatant = this.m_Mobile.Combatant;

            if (combatant == null || combatant.Deleted || combatant.Map != this.m_Mobile.Map)
            {
                this.m_Mobile.DebugSay("My combatant is gone, so my guard is up");

                this.Action = ActionType.Guard;

                return true;
            }

            if (this.WalkMobileRange(combatant, 1, true, this.m_Mobile.RangeFight, this.m_Mobile.RangeFight))
            {
                this.m_Mobile.Direction = this.m_Mobile.GetDirectionTo(combatant);
                if (this.m_toDisarm == null)
                    this.m_toDisarm = combatant.FindItemOnLayer(Layer.OneHanded);

                if (this.m_toDisarm == null)
                    this.m_toDisarm = combatant.FindItemOnLayer(Layer.TwoHanded);

                if (this.m_toDisarm != null && this.m_toDisarm.IsChildOf(this.m_Mobile.Backpack))
                {
                    this.m_toDisarm = combatant.FindItemOnLayer(Layer.OneHanded);
                    if (this.m_toDisarm == null)
                        this.m_toDisarm = combatant.FindItemOnLayer(Layer.TwoHanded);
                }
                if (!Core.AOS && !this.m_Mobile.DisarmReady && this.m_Mobile.Skills[SkillName.Wrestling].Value >= 80.0 && this.m_Mobile.Skills[SkillName.ArmsLore].Value >= 80.0 && this.m_toDisarm != null)
                    EventSink.InvokeDisarmRequest(new DisarmRequestEventArgs(this.m_Mobile));

                if (this.m_toDisarm != null && this.m_toDisarm.IsChildOf(combatant.Backpack) && this.m_Mobile.NextSkillTime <= DateTime.Now && (this.m_toDisarm.LootType != LootType.Blessed && this.m_toDisarm.LootType != LootType.Newbied))
                {
                    this.m_Mobile.DebugSay("Trying to steal from combatant.");
                    this.m_Mobile.UseSkill(SkillName.Stealing);
                    if (this.m_Mobile.Target != null)
                        this.m_Mobile.Target.Invoke(this.m_Mobile, this.m_toDisarm);
                }
                else if (this.m_toDisarm == null && this.m_Mobile.NextSkillTime <= DateTime.Now)
                {
                    Container cpack = combatant.Backpack;

                    if (cpack != null)
                    {
                        Item steala = cpack.FindItemByType(typeof (Bandage));
                        if (steala != null) 
                        {
                            this.m_Mobile.DebugSay("Trying to steal from combatant.");
                            this.m_Mobile.UseSkill(SkillName.Stealing);
                            if (this.m_Mobile.Target != null)
                                this.m_Mobile.Target.Invoke(this.m_Mobile, steala);
                        }
                        Item stealb = cpack.FindItemByType(typeof (Nightshade));
                        if (stealb != null) 
                        {
                            this.m_Mobile.DebugSay("Trying to steal from combatant.");
                            this.m_Mobile.UseSkill(SkillName.Stealing);
                            if (this.m_Mobile.Target != null)
                                this.m_Mobile.Target.Invoke(this.m_Mobile, stealb);
                        }
                        Item stealc = cpack.FindItemByType(typeof (BlackPearl));
                        if (stealc != null) 
                        {
                            this.m_Mobile.DebugSay("Trying to steal from combatant.");
                            this.m_Mobile.UseSkill(SkillName.Stealing);
                            if (this.m_Mobile.Target != null)
                                this.m_Mobile.Target.Invoke(this.m_Mobile, stealc);
                        }

                        Item steald = cpack.FindItemByType(typeof (MandrakeRoot));
                        if (steald != null) 
                        {
                            this.m_Mobile.DebugSay("Trying to steal from combatant.");
                            this.m_Mobile.UseSkill(SkillName.Stealing);
                            if (this.m_Mobile.Target != null)
                                this.m_Mobile.Target.Invoke(this.m_Mobile, steald);
                        }
                        else if (steala == null && stealb == null && stealc == null && steald == null)
                        {
                            this.m_Mobile.DebugSay("I am going to flee from {0}", combatant.Name);

                            this.Action = ActionType.Flee;
                        }
                    }
                }
            }
            else
            {
                this.m_Mobile.DebugSay("I should be closer to {0}", combatant.Name);
            }

            if (this.m_Mobile.Hits < this.m_Mobile.HitsMax * 20 / 100 && this.m_Mobile.CanFlee)
            {
                // We are low on health, should we flee?
                bool flee = false;

                if (this.m_Mobile.Hits < combatant.Hits)
                {
                    // We are more hurt than them
                    int diff = combatant.Hits - this.m_Mobile.Hits;

                    flee = (Utility.Random(0, 100) > (10 + diff)); // (10 + diff)% chance to flee
                }
                else
                {
                    flee = Utility.Random(0, 100) > 10; // 10% chance to flee
                }

                if (flee)
                {
                    this.m_Mobile.DebugSay("I am going to flee from {0}", combatant.Name);

                    this.Action = ActionType.Flee;
                }
            }

            return true;
        }

        public override bool DoActionGuard()
        {
            if (this.AcquireFocusMob(this.m_Mobile.RangePerception, this.m_Mobile.FightMode, false, false, true))
            {
                this.m_Mobile.DebugSay("I have detected {0}, attacking", this.m_Mobile.FocusMob.Name);

                this.m_Mobile.Combatant = this.m_Mobile.FocusMob;
                this.Action = ActionType.Combat;
            }
            else
            {
                base.DoActionGuard();
            }

            return true;
        }

        public override bool DoActionFlee()
        {
            if (this.m_Mobile.Hits > this.m_Mobile.HitsMax / 2)
            {
                this.m_Mobile.DebugSay("I am stronger now, so I will continue fighting");
                this.Action = ActionType.Combat;
            }
            else
            {
                this.m_Mobile.FocusMob = this.m_Mobile.Combatant;
                base.DoActionFlee();
            }

            return true;
        }
    }
}