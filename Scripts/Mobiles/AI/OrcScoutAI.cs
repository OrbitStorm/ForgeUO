using System;
using Server.Items;
using Server.Spells;
using Server.Targeting;

namespace Server.Mobiles
{
    public class OrcScoutAI : BaseAI
    {
        private static readonly double teleportChance = 0.04;
        private static readonly int[] m_Offsets = new int[]
        {
            0, 0,
            -1, -1,
            0, -1,
            1, -1,
            -1, 0,
            1, 0,
            -1, -1,
            0, 1,
            1, 1,
        };
        public OrcScoutAI(BaseCreature m)
            : base(m)
        {
        }

        public override bool DoActionWander()
        {
            this.m_Mobile.DebugSay("I have no combatant");

            this.PerformHide();

            if (this.AcquireFocusMob(this.m_Mobile.RangePerception, this.m_Mobile.FightMode, false, false, true))
            {
                if (this.m_Mobile.Debug)
                {
                    this.m_Mobile.DebugSay("I have detected {0}, attacking", this.m_Mobile.FocusMob.Name);
                }

                this.m_Mobile.Combatant = this.m_Mobile.FocusMob;
                this.Action = ActionType.Combat;
            }
            else
            {
                if (this.m_Mobile.Combatant != null)
                {
                    this.Action = ActionType.Combat;
                    return true;
                }

                base.DoActionWander();
            }

            return true;
        }

        public override bool DoActionCombat()
        {
            Mobile combatant = this.m_Mobile.Combatant;

            if (combatant == null || combatant.Deleted || combatant.Map != this.m_Mobile.Map || !combatant.Alive || combatant.IsDeadBondedPet)
            {
                this.m_Mobile.DebugSay("My combatant is gone, so my guard is up");

                this.Action = ActionType.Guard;

                return true;
            }

            if (Utility.RandomDouble() < teleportChance)
            {
                this.TryToTeleport();
            }

            if (!this.m_Mobile.InRange(combatant, this.m_Mobile.RangePerception))
            {
                // They are somewhat far away, can we find something else?
                if (this.AcquireFocusMob(this.m_Mobile.RangePerception, this.m_Mobile.FightMode, false, false, true))
                {
                    this.m_Mobile.Combatant = this.m_Mobile.FocusMob;
                    this.m_Mobile.FocusMob = null;
                }
                else if (!this.m_Mobile.InRange(combatant, this.m_Mobile.RangePerception * 3))
                {
                    this.m_Mobile.Combatant = null;
                }

                combatant = this.m_Mobile.Combatant;

                if (combatant == null)
                {
                    this.m_Mobile.DebugSay("My combatant has fled, so I am on guard");
                    this.Action = ActionType.Guard;

                    return true;
                }
            }

            /*if ( !m_Mobile.InLOS( combatant ) )
            {
            if ( AcquireFocusMob( m_Mobile.RangePerception, m_Mobile.FightMode, false, false, true ) )
            {
            m_Mobile.Combatant = combatant = m_Mobile.FocusMob;
            m_Mobile.FocusMob = null;
            }
            }*/

            if (this.MoveTo(combatant, true, this.m_Mobile.RangeFight))
            {
                this.m_Mobile.Direction = this.m_Mobile.GetDirectionTo(combatant);
            }
            else if (this.AcquireFocusMob(this.m_Mobile.RangePerception, this.m_Mobile.FightMode, false, false, true))
            {
                if (this.m_Mobile.Debug)
                {
                    this.m_Mobile.DebugSay("My move is blocked, so I am going to attack {0}", this.m_Mobile.FocusMob.Name);
                }

                this.m_Mobile.Combatant = this.m_Mobile.FocusMob;
                this.Action = ActionType.Combat;

                return true;
            }
            else if (this.m_Mobile.GetDistanceToSqrt(combatant) > this.m_Mobile.RangePerception + 1)
            {
                if (this.m_Mobile.Debug)
                {
                    this.m_Mobile.DebugSay("I cannot find {0}, so my guard is up", combatant.Name);
                }

                this.Action = ActionType.Guard;

                return true;
            }
            else
            {
                if (this.m_Mobile.Debug)
                {
                    this.m_Mobile.DebugSay("I should be closer to {0}", combatant.Name);
                }
            }

            if (!this.m_Mobile.Controlled && !this.m_Mobile.Summoned)
            {
                if (this.m_Mobile.Hits < this.m_Mobile.HitsMax * 20 / 100)
                {
                    // We are low on health, should we flee?
                    bool flee = false;

                    if (this.m_Mobile.Hits < combatant.Hits)
                    {
                        // We are more hurt than them
                        int diff = combatant.Hits - this.m_Mobile.Hits;

                        flee = (Utility.Random(0, 100) < (10 + diff)); // (10 + diff)% chance to flee
                    }
                    else
                    {
                        flee = Utility.Random(0, 100) < 10; // 10% chance to flee
                    }

                    if (flee)
                    {
                        if (this.m_Mobile.Debug)
                        {
                            this.m_Mobile.DebugSay("I am going to flee from {0}", combatant.Name);
                        }

                        this.Action = ActionType.Flee;

                        if (Utility.RandomDouble() < teleportChance + 0.1)
                        {
                            this.TryToTeleport();
                        }
                    }
                }
            }

            return true;
        }

        public override bool DoActionGuard()
        {
            if (this.AcquireFocusMob(this.m_Mobile.RangePerception, this.m_Mobile.FightMode, false, false, true))
            {
                if (this.m_Mobile.Debug)
                {
                    this.m_Mobile.DebugSay("I have detected {0}, attacking", this.m_Mobile.FocusMob.Name);
                }

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

                this.PerformHide();

                if (this.WalkMobileRange(this.m_Mobile.FocusMob, 1, false, this.m_Mobile.RangePerception * 2, this.m_Mobile.RangePerception * 3))
                {
                    this.m_Mobile.DebugSay("I Have fled");
                    this.Action = ActionType.Guard;
                    return true;
                }
                else
                {
                    this.m_Mobile.DebugSay("I am fleeing!");
                }
            }

            return true;
        }

        private Mobile FindNearestAggressor()
        {
            Mobile nearest = null;

            double dist = 9999.0;

            foreach (Mobile m in this.m_Mobile.GetMobilesInRange(this.m_Mobile.RangePerception))
            {
                if (m.Player && !m.Hidden && m.IsPlayer() && m.Combatant == this.m_Mobile)
                {
                    if (dist > m.GetDistanceToSqrt(this.m_Mobile))
                    {
                        nearest = m;
                    }
                }
            }

            return nearest;
        }

        private void TryToTeleport()
        {
            Mobile m = this.FindNearestAggressor();

            if (m == null || m.Map == null || this.m_Mobile.Map == null)
            {
                return;
            }

            if (this.m_Mobile.GetDistanceToSqrt(m) > this.m_Mobile.RangePerception + 1)
            {
                return;
            }

            int px = this.m_Mobile.X;
            int py = this.m_Mobile.Y;

            int dx = this.m_Mobile.X - m.X;
            int dy = this.m_Mobile.Y - m.Y;

            // get vector's length

            double l = Math.Sqrt((double)(dx * dx + dy * dy));

            if (l == 0)
            {
                int rand = Utility.Random(8) + 1;
                rand *= 2;
                dx = m_Offsets[rand];
                dy = m_Offsets[rand + 1];
                l = Math.Sqrt((double)(dx * dx + dy * dy));
            }

            // normalize vector
            double dpx = ((double)dx) / l;
            double dpy = ((double)dy) / l;
            // move 
            px += (int)(dpx * (4 + Utility.Random(3)));
            py += (int)(dpy * (4 + Utility.Random(3)));

            for (int i = 0; i < m_Offsets.Length; i += 2)
            {
                int x = m_Offsets[i], y = m_Offsets[i + 1];

                Point3D p = new Point3D(px + x, py + y, 0);

                LandTarget lt = new LandTarget(p, this.m_Mobile.Map);

                if (this.m_Mobile.InLOS(lt) && this.m_Mobile.Map.CanSpawnMobile(px + x, py + y, lt.Z) && !SpellHelper.CheckMulti(p, this.m_Mobile.Map))
                {
                    this.m_Mobile.FixedParticles(0x376A, 9, 32, 0x13AF, EffectLayer.Waist);
                    this.m_Mobile.PlaySound(0x1FE);

                    this.m_Mobile.Location = new Point3D(lt.X, lt.Y, lt.Z);
                    this.m_Mobile.ProcessDelta();

                    return;
                }
            }

            return;
        }

        private void HideSelf()
        {
            if (DateTime.Now >= this.m_Mobile.NextSkillTime)
            {
                Effects.SendLocationParticles(EffectItem.Create(this.m_Mobile.Location, this.m_Mobile.Map, EffectItem.DefaultDuration), 0x3728, 10, 10, 2023);

                this.m_Mobile.PlaySound(0x22F);
                this.m_Mobile.Hidden = true;

                this.m_Mobile.UseSkill(SkillName.Stealth);
            }
        }

        private void PerformHide()
        {
            if (!this.m_Mobile.Alive || this.m_Mobile.Deleted)
            {
                return;
            }

            if (!this.m_Mobile.Hidden)
            {
                double chance = 0.05;

                if (this.m_Mobile.Hits < 20)
                {
                    chance = 0.1;
                }

                if (this.m_Mobile.Poisoned)
                {
                    chance = 0.01;
                }

                if (Utility.RandomDouble() < chance)
                {
                    this.HideSelf();
                }
            }
        }
    }
}