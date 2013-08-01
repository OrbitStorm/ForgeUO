using System;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Spells;
using Server.Spells.Necromancy;
using Server.Spells.Ninjitsu;

namespace Server
{
    public class PoisonImpl : Poison
    {
        [CallPriority(10)]
        public static void Configure()
        {
            if (Core.AOS)
            {
                Register(new PoisonImpl("Lesser", 0, 4, 16, 7.5, 3.0, 2.25, 10, 4));
                Register(new PoisonImpl("Regular",	1, 8, 18, 10.0, 3.0, 3.25, 10, 3));
                Register(new PoisonImpl("Greater",	2, 12, 20, 15.0, 3.0, 4.25, 10, 2));
                Register(new PoisonImpl("Deadly", 3, 16, 30, 30.0, 3.0, 5.25, 15, 2));
                Register(new PoisonImpl("Lethal", 4, 20, 50, 35.0, 3.0, 5.25, 20, 2));
            }
            else
            {
                Register(new PoisonImpl("Lesser", 0, 4, 26, 2.500, 3.5, 3.0, 10, 2));
                Register(new PoisonImpl("Regular",	1, 5, 26, 3.125, 3.5, 3.0, 10, 2));
                Register(new PoisonImpl("Greater",	2, 6, 26, 6.250, 3.5, 3.0, 10, 2));
                Register(new PoisonImpl("Deadly", 3, 7, 26, 12.500, 3.5, 4.0, 10, 2));
                Register(new PoisonImpl("Lethal", 4, 9, 26, 25.000, 3.5, 5.0, 10, 2));
            }
			
            #region Mondain's Legacy
            if (Core.ML)
            {
                Register(new PoisonImpl("LesserDarkglow", 10, 4, 16, 7.5, 3.0, 2.25, 10, 4));
                Register(new PoisonImpl("RegularDarkglow",	11, 8, 18, 10.0, 3.0, 3.25, 10, 3));
                Register(new PoisonImpl("GreaterDarkglow",	12, 12, 20, 15.0, 3.0, 4.25, 10, 2));
                Register(new PoisonImpl("DeadlyDarkglow", 13, 16, 30, 30.0, 3.0, 5.25, 15, 2));
				
                Register(new PoisonImpl("LesserParasitic",	14, 4, 16, 7.5, 3.0, 2.25, 10, 4));
                Register(new PoisonImpl("RegularParasitic",	15, 8, 18, 10.0, 3.0, 3.25, 10, 3));
                Register(new PoisonImpl("GreaterParasitic",	16, 12, 20, 15.0, 3.0, 4.25, 10, 2));
                Register(new PoisonImpl("DeadlyParasitic",	17, 16, 30, 30.0, 3.0, 5.25, 15, 2));
                Register(new PoisonImpl("LethalParasitic",	18, 20, 50, 35.0, 3.0, 5.25, 20, 2));
            }
            #endregion
        }

        public static Poison IncreaseLevel(Poison oldPoison)
        {
            Poison newPoison = (oldPoison == null ? null : GetPoison(oldPoison.Level + 1));

            return (newPoison == null ? oldPoison : newPoison);
        }

        // Info
        private readonly string m_Name;
        private readonly int m_Level;

        // Damage
        private readonly int m_Minimum;

        private readonly int m_Maximum;

        private readonly double m_Scalar;

        // Timers
        private readonly TimeSpan m_Delay;
        private readonly TimeSpan m_Interval;
        private readonly int m_Count;

        private readonly int m_MessageInterval;

        public PoisonImpl(string name, int level, int min, int max, double percent, double delay, double interval, int count, int messageInterval)
        {
            this.m_Name = name;
            this.m_Level = level;
            this.m_Minimum = min;
            this.m_Maximum = max;
            this.m_Scalar = percent * 0.01;
            this.m_Delay = TimeSpan.FromSeconds(delay);
            this.m_Interval = TimeSpan.FromSeconds(interval);
            this.m_Count = count;
            this.m_MessageInterval = messageInterval;
        }

        public override string Name
        {
            get
            {
                return this.m_Name;
            }
        }
        public override int Level
        {
            get
            {
                return this.m_Level;
            }
        }

        #region Mondain's Legacy
        public override int RealLevel
        {
            get
            {
                if (this.m_Level >= 14)
                    return this.m_Level - 14;
                else if (this.m_Level >= 10)
                    return this.m_Level - 10;

                return this.m_Level;
            }
        }

        public override int LabelNumber
        {
            get
            {
                if (this.m_Level >= 14)
                    return 1072852; // parasitic poison charges: ~1_val~
                else if (this.m_Level >= 10)
                    return 1072853; // darkglow poison charges: ~1_val~

                return 1062412 + this.m_Level; // ~poison~ poison charges: ~1_val~
            }
        }
        #endregion

        public class PoisonTimer : Timer
        {
            private readonly PoisonImpl m_Poison;
            private readonly Mobile m_Mobile;
            private Mobile m_From;
            private int m_LastDamage;
            private int m_Index;

            public Mobile From
            {
                get
                {
                    return this.m_From;
                }
                set
                {
                    this.m_From = value;
                }
            }

            public PoisonTimer(Mobile m, PoisonImpl p)
                : base(p.m_Delay, p.m_Interval)
            {
                this.m_From = m;
                this.m_Mobile = m;
                this.m_Poison = p;
            }

            protected override void OnTick()
            {
                #region Mondain's Legacy
                if ((Core.AOS && this.m_Poison.RealLevel < 4 && TransformationSpellHelper.UnderTransformation(this.m_Mobile, typeof(VampiricEmbraceSpell))) ||
                    (this.m_Poison.RealLevel < 3 && OrangePetals.UnderEffect(this.m_Mobile)) ||
                    AnimalForm.UnderTransformation(this.m_Mobile, typeof(Unicorn)))
                {
                    if (this.m_Mobile.CurePoison(this.m_Mobile))
                    {
                        this.m_Mobile.LocalOverheadMessage(MessageType.Emote, 0x3F, true,
                            "* You feel yourself resisting the effects of the poison *");

                        this.m_Mobile.NonlocalOverheadMessage(MessageType.Emote, 0x3F, true,
                            String.Format("* {0} seems resistant to the poison *", this.m_Mobile.Name));

                        this.Stop();
                        return;
                    }
                }
                #endregion

                if (this.m_Index++ == this.m_Poison.m_Count)
                {
                    this.m_Mobile.SendLocalizedMessage(502136); // The poison seems to have worn off.
                    this.m_Mobile.Poison = null;

                    this.Stop();
                    return;
                }

                int damage;

                if (!Core.AOS && this.m_LastDamage != 0 && Utility.RandomBool())
                {
                    damage = this.m_LastDamage;
                }
                else
                {
                    damage = 1 + (int)(this.m_Mobile.Hits * this.m_Poison.m_Scalar);

                    if (damage < this.m_Poison.m_Minimum)
                        damage = this.m_Poison.m_Minimum;
                    else if (damage > this.m_Poison.m_Maximum)
                        damage = this.m_Poison.m_Maximum;

                    this.m_LastDamage = damage;
                }

                if (this.m_From != null)
                    this.m_From.DoHarmful(this.m_Mobile, true);

                IHonorTarget honorTarget = this.m_Mobile as IHonorTarget;
                if (honorTarget != null && honorTarget.ReceivedHonorContext != null)
                    honorTarget.ReceivedHonorContext.OnTargetPoisoned();
					
                #region Mondain's Legacy
                if (Core.ML)
                {
                    if (this.m_From != null && this.m_Mobile != this.m_From && !this.m_From.InRange(this.m_Mobile.Location, 1) && this.m_Poison.m_Level >= 10 && this.m_Poison.m_Level <= 13) // darkglow
                    {
                        this.m_From.SendLocalizedMessage(1072850); // Darkglow poison increases your damage!
                        damage = (int)Math.Floor(damage * 1.1);
                    }
					
                    if (this.m_From != null && this.m_Mobile != this.m_From && this.m_From.InRange(this.m_Mobile.Location, 1) && this.m_Poison.m_Level >= 14 && this.m_Poison.m_Level <= 18) // parasitic
                    {
                        int toHeal = Math.Min(this.m_From.HitsMax - this.m_From.Hits, damage);
												
                        if (toHeal > 0)
                        {
                            this.m_From.SendLocalizedMessage(1060203, toHeal.ToString()); // You have had ~1_HEALED_AMOUNT~ hit points of damage healed.
                            this.m_From.Heal(toHeal, this.m_Mobile, false);
                        }
                    }
                }
                #endregion

                AOS.Damage(this.m_Mobile, this.m_From, damage, 0, 0, 0, 100, 0);

                if (0.60 <= Utility.RandomDouble()) // OSI: randomly revealed between first and third damage tick, guessing 60% chance
                    this.m_Mobile.RevealingAction();

                if ((this.m_Index % this.m_Poison.m_MessageInterval) == 0)
                    this.m_Mobile.OnPoisoned(this.m_From, this.m_Poison, this.m_Poison);
            }
        }

        public override Timer ConstructTimer(Mobile m)
        {
            return new PoisonTimer(m, this);
        }
    }
}