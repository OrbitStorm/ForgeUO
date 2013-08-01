using System;
using Server.Items;

namespace Server.Mobiles
{
    public class ArcherAI : BaseAI
    {
        public ArcherAI(BaseCreature m)
            : base(m)
        {
        }

        public override bool DoActionWander()
        {
            this.m_Mobile.DebugSay("I have no combatant");

            if (this.AcquireFocusMob(this.m_Mobile.RangePerception, this.m_Mobile.FightMode, false, false, true))
            {
                if (this.m_Mobile.Debug)
                    this.m_Mobile.DebugSay("I have detected {0} and I will attack", this.m_Mobile.FocusMob.Name);

                this.m_Mobile.Combatant = this.m_Mobile.FocusMob;
                this.Action = ActionType.Combat;
            }
            else
            {
                return base.DoActionWander();
            }

            return true;
        }

        public override bool DoActionCombat()
        {
            if (this.m_Mobile.Combatant == null || this.m_Mobile.Combatant.Deleted || !this.m_Mobile.Combatant.Alive || this.m_Mobile.Combatant.IsDeadBondedPet)
            {
                this.m_Mobile.DebugSay("My combatant is deleted");
                this.Action = ActionType.Guard;
                return true;
            }

            if ((this.m_Mobile.LastMoveTime + TimeSpan.FromSeconds(1.0)) < DateTime.Now)
            {
                if (this.WalkMobileRange(this.m_Mobile.Combatant, 1, true, this.m_Mobile.RangeFight, this.m_Mobile.Weapon.MaxRange))
                {
                    // Be sure to face the combatant
                    this.m_Mobile.Direction = this.m_Mobile.GetDirectionTo(this.m_Mobile.Combatant.Location);
                }
                else
                {
                    if (this.m_Mobile.Combatant != null)
                    {
                        if (this.m_Mobile.Debug)
                            this.m_Mobile.DebugSay("I am still not in range of {0}", this.m_Mobile.Combatant.Name);

                        if ((int)this.m_Mobile.GetDistanceToSqrt(this.m_Mobile.Combatant) > this.m_Mobile.RangePerception + 1)
                        {
                            if (this.m_Mobile.Debug)
                                this.m_Mobile.DebugSay("I have lost {0}", this.m_Mobile.Combatant.Name);

                            this.m_Mobile.Combatant = null;
                            this.Action = ActionType.Guard;
                            return true;
                        }
                    }
                }
            }

            // When we have no ammo, we flee
            Container pack = this.m_Mobile.Backpack;

            if (pack == null || pack.FindItemByType(typeof(Arrow)) == null)
            {
                this.Action = ActionType.Flee;
                return true;
            }

            // At 20% we should check if we must leave
            if (this.m_Mobile.Hits < this.m_Mobile.HitsMax * 20 / 100 && this.m_Mobile.CanFlee)
            {
                bool bFlee = false;
                // if my current hits are more than my opponent, i don't care
                if (this.m_Mobile.Combatant != null && this.m_Mobile.Hits < this.m_Mobile.Combatant.Hits)
                {
                    int iDiff = this.m_Mobile.Combatant.Hits - this.m_Mobile.Hits;

                    if (Utility.Random(0, 100) > 10 + iDiff) // 10% to flee + the diff of hits
                    {
                        bFlee = true;
                    }
                }
                else if (this.m_Mobile.Combatant != null && this.m_Mobile.Hits >= this.m_Mobile.Combatant.Hits)
                {
                    if (Utility.Random(0, 100) > 10) // 10% to flee
                    {
                        bFlee = true;
                    }
                }
						
                if (bFlee)
                {
                    this.Action = ActionType.Flee; 
                }
            }

            return true;
        }

        public override bool DoActionGuard()
        {
            if (this.AcquireFocusMob(this.m_Mobile.RangePerception, this.m_Mobile.FightMode, false, false, true))
            {
                if (this.m_Mobile.Debug)
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
    }
}