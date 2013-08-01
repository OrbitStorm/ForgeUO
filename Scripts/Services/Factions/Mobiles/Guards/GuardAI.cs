using System;
using System.Collections.Generic;
using Server.Factions.AI;
using Server.Items;
using Server.Mobiles;
using Server.Spells;
using Server.Spells.Fifth;
using Server.Spells.First;
using Server.Spells.Fourth;
using Server.Spells.Second;
using Server.Spells.Seventh;
using Server.Spells.Sixth;
using Server.Spells.Third;
using Server.Targeting;

namespace Server.Factions
{
    public enum GuardAI
    {
        Bless = 0x01, // heal, cure, +stats
        Curse = 0x02, // poison, -stats
        Melee = 0x04, // weapons
        Magic = 0x08, // damage spells
        Smart = 0x10  // smart weapons/damage spells
    }

    public class ComboEntry
    {
        private readonly Type m_Spell;
        private readonly TimeSpan m_Hold;
        private readonly int m_Chance;
        public ComboEntry(Type spell)
            : this(spell, 100, TimeSpan.Zero)
        {
        }

        public ComboEntry(Type spell, int chance)
            : this(spell, chance, TimeSpan.Zero)
        {
        }

        public ComboEntry(Type spell, int chance, TimeSpan hold)
        {
            this.m_Spell = spell;
            this.m_Chance = chance;
            this.m_Hold = hold;
        }

        public Type Spell
        {
            get
            {
                return this.m_Spell;
            }
        }
        public TimeSpan Hold
        {
            get
            {
                return this.m_Hold;
            }
        }
        public int Chance
        {
            get
            {
                return this.m_Chance;
            }
        }
    }

    public class SpellCombo
    {
        public static readonly SpellCombo Simple = new SpellCombo(50,
            new ComboEntry(typeof(ParalyzeSpell), 20),
            new ComboEntry(typeof(ExplosionSpell), 100, TimeSpan.FromSeconds(2.8)),
            new ComboEntry(typeof(PoisonSpell), 30),
            new ComboEntry(typeof(EnergyBoltSpell)));
        public static readonly SpellCombo Strong = new SpellCombo(90,
            new ComboEntry(typeof(ParalyzeSpell), 20),
            new ComboEntry(typeof(ExplosionSpell), 50, TimeSpan.FromSeconds(2.8)),
            new ComboEntry(typeof(PoisonSpell), 30),
            new ComboEntry(typeof(ExplosionSpell), 100, TimeSpan.FromSeconds(2.8)),
            new ComboEntry(typeof(EnergyBoltSpell)),
            new ComboEntry(typeof(PoisonSpell), 30),
            new ComboEntry(typeof(EnergyBoltSpell)));
        private readonly int m_Mana;
        private readonly ComboEntry[] m_Entries;
        public SpellCombo(int mana, params ComboEntry[] entries)
        {
            this.m_Mana = mana;
            this.m_Entries = entries;
        }

        public int Mana
        {
            get
            {
                return this.m_Mana;
            }
        }
        public ComboEntry[] Entries
        {
            get
            {
                return this.m_Entries;
            }
        }
        public static Spell Process(Mobile mob, Mobile targ, ref SpellCombo combo, ref int index, ref DateTime releaseTime)
        {
            while (++index < combo.m_Entries.Length)
            {
                ComboEntry entry = combo.m_Entries[index];

                if (entry.Spell == typeof(PoisonSpell) && targ.Poisoned)
                    continue;

                if (entry.Chance > Utility.Random(100))
                {
                    releaseTime = DateTime.Now + entry.Hold;
                    return (Spell)Activator.CreateInstance(entry.Spell, new object[] { mob, null });
                }
            }

            combo = null;
            index = -1;
            return null;
        }
    }

    public class FactionGuardAI : BaseAI
    {
        private const int ManaReserve = 30;
        private readonly BaseFactionGuard m_Guard;
        private BandageContext m_Bandage;
        private DateTime m_BandageStart;
        private SpellCombo m_Combo;
        private int m_ComboIndex = -1;
        private DateTime m_ReleaseTarget;
        public FactionGuardAI(BaseFactionGuard guard)
            : base(guard)
        {
            this.m_Guard = guard;
        }

        public bool IsDamaged
        {
            get
            {
                return (this.m_Guard.Hits < this.m_Guard.HitsMax);
            }
        }
        public bool IsPoisoned
        {
            get
            {
                return this.m_Guard.Poisoned;
            }
        }
        public TimeSpan TimeUntilBandage
        {
            get
            {
                if (this.m_Bandage != null && this.m_Bandage.Timer == null)
                    this.m_Bandage = null;

                if (this.m_Bandage == null)
                    return TimeSpan.MaxValue;

                TimeSpan ts = (this.m_BandageStart + this.m_Bandage.Timer.Delay) - DateTime.Now;

                if (ts < TimeSpan.FromSeconds(-1.0))
                {
                    this.m_Bandage = null;
                    return TimeSpan.MaxValue;
                }

                if (ts < TimeSpan.Zero)
                    ts = TimeSpan.Zero;

                return ts;
            }
        }
        public bool IsAllowed(GuardAI flag)
        {
            return ((this.m_Guard.GuardAI & flag) == flag);
        }

        public bool DequipWeapon()
        {
            Container pack = this.m_Guard.Backpack;

            if (pack == null)
                return false;

            Item weapon = this.m_Guard.Weapon as Item;

            if (weapon != null && weapon.Parent == this.m_Guard && !(weapon is Fists))
            {
                pack.DropItem(weapon);
                return true;
            }

            return false;
        }

        public bool EquipWeapon()
        {
            Container pack = this.m_Guard.Backpack;

            if (pack == null)
                return false;

            Item weapon = pack.FindItemByType(typeof(BaseWeapon));

            if (weapon == null)
                return false;

            return this.m_Guard.EquipItem(weapon);
        }

        public bool StartBandage()
        {
            this.m_Bandage = null;

            Container pack = this.m_Guard.Backpack;

            if (pack == null)
                return false;

            Item bandage = pack.FindItemByType(typeof(Bandage));

            if (bandage == null)
                return false;

            this.m_Bandage = BandageContext.BeginHeal(this.m_Guard, this.m_Guard);
            this.m_BandageStart = DateTime.Now;
            return (this.m_Bandage != null);
        }

        public bool UseItemByType(Type type)
        {
            Container pack = this.m_Guard.Backpack;

            if (pack == null)
                return false;

            Item item = pack.FindItemByType(type);

            if (item == null)
                return false;

            bool requip = this.DequipWeapon();

            item.OnDoubleClick(this.m_Guard);

            if (requip)
                this.EquipWeapon();

            return true;
        }

        public int GetStatMod(Mobile mob, StatType type)
        {
            StatMod mod = mob.GetStatMod(String.Format("[Magic] {0} Offset", type));

            if (mod == null)
                return 0;

            return mod.Offset;
        }

        public Spell RandomOffenseSpell()
        {
            int maxCircle = (int)((this.m_Guard.Skills.Magery.Value + 20.0) / (100.0 / 7.0));

            if (maxCircle < 1)
                maxCircle = 1;

            switch ( Utility.Random(maxCircle * 2) )
            {
                case 0:
                case 1:
                    return new MagicArrowSpell(this.m_Guard, null);
                case 2:
                case 3:
                    return new HarmSpell(this.m_Guard, null);
                case 4:
                case 5:
                    return new FireballSpell(this.m_Guard, null);
                case 6:
                case 7:
                    return new LightningSpell(this.m_Guard, null);
                case 8:
                    return new MindBlastSpell(this.m_Guard, null);
                case 9:
                    return new ParalyzeSpell(this.m_Guard, null);
                case 10:
                    return new EnergyBoltSpell(this.m_Guard, null);
                case 11:
                    return new ExplosionSpell(this.m_Guard, null);
                default:
                    return new FlameStrikeSpell(this.m_Guard, null);
            }
        }

        public Mobile FindDispelTarget(bool activeOnly)
        {
            if (this.m_Mobile.Deleted || this.m_Mobile.Int < 95 || this.CanDispel(this.m_Mobile) || this.m_Mobile.AutoDispel)
                return null;

            if (activeOnly)
            {
                List<AggressorInfo> aggressed = this.m_Mobile.Aggressed;
                List<AggressorInfo> aggressors = this.m_Mobile.Aggressors;

                Mobile active = null;
                double activePrio = 0.0;

                Mobile comb = this.m_Mobile.Combatant;

                if (comb != null && !comb.Deleted && comb.Alive && !comb.IsDeadBondedPet && this.m_Mobile.InRange(comb, 12) && this.CanDispel(comb))
                {
                    active = comb;
                    activePrio = this.m_Mobile.GetDistanceToSqrt(comb);

                    if (activePrio <= 2)
                        return active;
                }

                for (int i = 0; i < aggressed.Count; ++i)
                {
                    AggressorInfo info = aggressed[i];
                    Mobile m = info.Defender;

                    if (m != comb && m.Combatant == this.m_Mobile && this.m_Mobile.InRange(m, 12) && this.CanDispel(m))
                    {
                        double prio = this.m_Mobile.GetDistanceToSqrt(m);

                        if (active == null || prio < activePrio)
                        {
                            active = m;
                            activePrio = prio;

                            if (activePrio <= 2)
                                return active;
                        }
                    }
                }

                for (int i = 0; i < aggressors.Count; ++i)
                {
                    AggressorInfo info = aggressors[i];
                    Mobile m = info.Attacker;

                    if (m != comb && m.Combatant == this.m_Mobile && this.m_Mobile.InRange(m, 12) && this.CanDispel(m))
                    {
                        double prio = this.m_Mobile.GetDistanceToSqrt(m);

                        if (active == null || prio < activePrio)
                        {
                            active = m;
                            activePrio = prio;

                            if (activePrio <= 2)
                                return active;
                        }
                    }
                }

                return active;
            }
            else
            {
                Map map = this.m_Mobile.Map;

                if (map != null)
                {
                    Mobile active = null, inactive = null;
                    double actPrio = 0.0, inactPrio = 0.0;

                    Mobile comb = this.m_Mobile.Combatant;

                    if (comb != null && !comb.Deleted && comb.Alive && !comb.IsDeadBondedPet && this.CanDispel(comb))
                    {
                        active = inactive = comb;
                        actPrio = inactPrio = this.m_Mobile.GetDistanceToSqrt(comb);
                    }

                    foreach (Mobile m in this.m_Mobile.GetMobilesInRange(12))
                    {
                        if (m != this.m_Mobile && this.CanDispel(m))
                        {
                            double prio = this.m_Mobile.GetDistanceToSqrt(m);

                            if (!activeOnly && (inactive == null || prio < inactPrio))
                            {
                                inactive = m;
                                inactPrio = prio;
                            }

                            if ((this.m_Mobile.Combatant == m || m.Combatant == this.m_Mobile) && (active == null || prio < actPrio))
                            {
                                active = m;
                                actPrio = prio;
                            }
                        }
                    }

                    return active != null ? active : inactive;
                }
            }

            return null;
        }

        public bool CanDispel(Mobile m)
        {
            return (m is BaseCreature && ((BaseCreature)m).Summoned && this.m_Mobile.CanBeHarmful(m, false) && !((BaseCreature)m).IsAnimatedDead);
        }

        public void RunTo(Mobile m)
        {
            /*if ( m.Paralyzed || m.Frozen )
            {
            if ( m_Mobile.InRange( m, 1 ) )
            RunFrom( m );
            else if ( !m_Mobile.InRange( m, m_Mobile.RangeFight > 2 ? m_Mobile.RangeFight : 2 ) && !MoveTo( m, true, 1 ) )
            OnFailedMove();
            }
            else
            {*/
            if (!this.m_Mobile.InRange(m, this.m_Mobile.RangeFight))
            {
                if (!this.MoveTo(m, true, 1))
                    this.OnFailedMove();
            }
            else if (this.m_Mobile.InRange(m, this.m_Mobile.RangeFight - 1))
            {
                this.RunFrom(m);
            }
            /*}*/
        }

        public void RunFrom(Mobile m)
        {
            this.Run((Direction)((int)this.m_Mobile.GetDirectionTo(m) - 4) & Direction.Mask);
        }

        public void OnFailedMove()
        {
            /*if ( !m_Mobile.DisallowAllMoves && 20 > Utility.Random( 100 ) && IsAllowed( GuardAI.Magic ) )
            {
            if ( m_Mobile.Target != null )
            m_Mobile.Target.Cancel( m_Mobile, TargetCancelType.Canceled );
            new TeleportSpell( m_Mobile, null ).Cast();
            m_Mobile.DebugSay( "I am stuck, I'm going to try teleporting away" );
            }
            else*/ if (this.AcquireFocusMob(this.m_Mobile.RangePerception, this.m_Mobile.FightMode, false, false, true))
            {
                if (this.m_Mobile.Debug)
                    this.m_Mobile.DebugSay("My move is blocked, so I am going to attack {0}", this.m_Mobile.FocusMob.Name);

                this.m_Mobile.Combatant = this.m_Mobile.FocusMob;
                this.Action = ActionType.Combat;
            }
            else
            {
                this.m_Mobile.DebugSay("I am stuck");
            }
        }

        public void Run(Direction d)
        {
            if ((this.m_Mobile.Spell != null && this.m_Mobile.Spell.IsCasting) || this.m_Mobile.Paralyzed || this.m_Mobile.Frozen || this.m_Mobile.DisallowAllMoves)
                return;

            this.m_Mobile.Direction = d | Direction.Running;

            if (!this.DoMove(this.m_Mobile.Direction, true))
                this.OnFailedMove();
        }

        public override bool Think()
        {
            if (this.m_Mobile.Deleted)
                return false;

            Mobile combatant = this.m_Guard.Combatant;

            if (combatant == null || combatant.Deleted || !combatant.Alive || combatant.IsDeadBondedPet || !this.m_Mobile.CanSee(combatant) || !this.m_Mobile.CanBeHarmful(combatant, false) || combatant.Map != this.m_Mobile.Map)
            {
                // Our combatant is deleted, dead, hidden, or we cannot hurt them
                // Try to find another combatant
                if (this.AcquireFocusMob(this.m_Mobile.RangePerception, this.m_Mobile.FightMode, false, false, true))
                {
                    this.m_Mobile.Combatant = combatant = this.m_Mobile.FocusMob;
                    this.m_Mobile.FocusMob = null;
                }
                else
                {
                    this.m_Mobile.Combatant = combatant = null;
                }
            }

            if (combatant != null && (!this.m_Mobile.InLOS(combatant) || !this.m_Mobile.InRange(combatant, 12)))
            {
                if (this.AcquireFocusMob(this.m_Mobile.RangePerception, this.m_Mobile.FightMode, false, false, true))
                {
                    this.m_Mobile.Combatant = combatant = this.m_Mobile.FocusMob;
                    this.m_Mobile.FocusMob = null;
                }
                else if (!this.m_Mobile.InRange(combatant, 36))
                {
                    this.m_Mobile.Combatant = combatant = null;
                }
            }

            Mobile dispelTarget = this.FindDispelTarget(true);

            if (this.m_Guard.Target != null && this.m_ReleaseTarget == DateTime.MinValue)
                this.m_ReleaseTarget = DateTime.Now + TimeSpan.FromSeconds(10.0);

            if (this.m_Guard.Target != null && DateTime.Now > this.m_ReleaseTarget)
            {
                Target targ = this.m_Guard.Target;

                Mobile toHarm = (dispelTarget == null ? combatant : dispelTarget);

                if ((targ.Flags & TargetFlags.Harmful) != 0 && toHarm != null)
                {
                    if (this.m_Guard.Map == toHarm.Map && (targ.Range < 0 || this.m_Guard.InRange(toHarm, targ.Range)) && this.m_Guard.CanSee(toHarm) && this.m_Guard.InLOS(toHarm))
                        targ.Invoke(this.m_Guard, toHarm);
                    else if (targ is DispelSpell.InternalTarget)
                        targ.Cancel(this.m_Guard, TargetCancelType.Canceled);
                }
                else if ((targ.Flags & TargetFlags.Beneficial) != 0)
                {
                    targ.Invoke(this.m_Guard, this.m_Guard);
                }
                else
                {
                    targ.Cancel(this.m_Guard, TargetCancelType.Canceled);
                }

                this.m_ReleaseTarget = DateTime.MinValue;
            }

            if (dispelTarget != null)
            {
                if (this.Action != ActionType.Combat)
                    this.Action = ActionType.Combat;

                this.m_Guard.Warmode = true;

                this.RunFrom(dispelTarget);
            }
            else if (combatant != null)
            {
                if (this.Action != ActionType.Combat)
                    this.Action = ActionType.Combat;

                this.m_Guard.Warmode = true;

                this.RunTo(combatant);
            }
            else if (this.m_Guard.Orders.Movement != MovementType.Stand)
            {
                Mobile toFollow = null;

                if (this.m_Guard.Town != null && this.m_Guard.Orders.Movement == MovementType.Follow)
                {
                    toFollow = this.m_Guard.Orders.Follow;

                    if (toFollow == null)
                        toFollow = this.m_Guard.Town.Sheriff;
                }

                if (toFollow != null && toFollow.Map == this.m_Guard.Map && toFollow.InRange(this.m_Guard, this.m_Guard.RangePerception * 3) && Town.FromRegion(toFollow.Region) == this.m_Guard.Town)
                {
                    if (this.Action != ActionType.Combat)
                        this.Action = ActionType.Combat;

                    if (this.m_Mobile.CurrentSpeed != this.m_Mobile.ActiveSpeed)
                        this.m_Mobile.CurrentSpeed = this.m_Mobile.ActiveSpeed;

                    this.m_Guard.Warmode = true;

                    this.RunTo(toFollow);
                }
                else
                {
                    if (this.Action != ActionType.Wander)
                        this.Action = ActionType.Wander;

                    if (this.m_Mobile.CurrentSpeed != this.m_Mobile.PassiveSpeed)
                        this.m_Mobile.CurrentSpeed = this.m_Mobile.PassiveSpeed;

                    this.m_Guard.Warmode = false;

                    this.WalkRandomInHome(2, 2, 1);
                }
            }
            else
            {
                if (this.Action != ActionType.Wander)
                    this.Action = ActionType.Wander;

                this.m_Guard.Warmode = false;
            }

            if ((this.IsDamaged || this.IsPoisoned) && this.m_Guard.Skills.Healing.Base > 20.0)
            {
                TimeSpan ts = this.TimeUntilBandage;

                if (ts == TimeSpan.MaxValue)
                    this.StartBandage();
            }

            if (this.m_Mobile.Spell == null && DateTime.Now >= this.m_Mobile.NextSpellTime)
            {
                Spell spell = null;

                DateTime toRelease = DateTime.MinValue;

                if (this.IsPoisoned)
                {
                    Poison p = this.m_Guard.Poison;

                    TimeSpan ts = this.TimeUntilBandage;

                    if (p != Poison.Lesser || ts == TimeSpan.MaxValue || this.TimeUntilBandage < TimeSpan.FromSeconds(1.5) || (this.m_Guard.HitsMax - this.m_Guard.Hits) > Utility.Random(250))
                    {
                        if (this.IsAllowed(GuardAI.Bless))
                            spell = new CureSpell(this.m_Guard, null);
                        else
                            this.UseItemByType(typeof(BaseCurePotion));
                    }
                }
                else if (this.IsDamaged && (this.m_Guard.HitsMax - this.m_Guard.Hits) > Utility.Random(200))
                {
                    if (this.IsAllowed(GuardAI.Magic) && ((this.m_Guard.Hits * 100) / Math.Max(this.m_Guard.HitsMax, 1)) < 10 && this.m_Guard.Home != Point3D.Zero && !Utility.InRange(this.m_Guard.Location, this.m_Guard.Home, 15) && this.m_Guard.Mana >= 11)
                    {
                        spell = new RecallSpell(this.m_Guard, null, new RunebookEntry(this.m_Guard.Home, this.m_Guard.Map, "Guard's Home", null), null);
                    }
                    else if (this.IsAllowed(GuardAI.Bless))
                    {
                        if (this.m_Guard.Mana >= 11 && (this.m_Guard.Hits + 30) < this.m_Guard.HitsMax)
                            spell = new GreaterHealSpell(this.m_Guard, null);
                        else if ((this.m_Guard.Hits + 10) < this.m_Guard.HitsMax && (this.m_Guard.Mana < 11 || (this.m_Guard.NextCombatTime - DateTime.Now) > TimeSpan.FromSeconds(2.0)))
                            spell = new HealSpell(this.m_Guard, null);
                    }
                    else if (this.m_Guard.CanBeginAction(typeof(BaseHealPotion)))
                    {
                        this.UseItemByType(typeof(BaseHealPotion));
                    }
                }
                else if (dispelTarget != null && (this.IsAllowed(GuardAI.Magic) || this.IsAllowed(GuardAI.Bless) || this.IsAllowed(GuardAI.Curse)))
                {
                    if (!dispelTarget.Paralyzed && this.m_Guard.Mana > (ManaReserve + 20) && 40 > Utility.Random(100))
                        spell = new ParalyzeSpell(this.m_Guard, null);
                    else
                        spell = new DispelSpell(this.m_Guard, null);
                }

                if (combatant != null)
                {
                    if (this.m_Combo != null)
                    {
                        if (spell == null)
                        {
                            spell = SpellCombo.Process(this.m_Guard, combatant, ref this.m_Combo, ref this.m_ComboIndex, ref toRelease);
                        }
                        else
                        {
                            this.m_Combo = null;
                            this.m_ComboIndex = -1;
                        }
                    }
                    else if (20 > Utility.Random(100) && this.IsAllowed(GuardAI.Magic))
                    {
                        if (80 > Utility.Random(100))
                        {
                            this.m_Combo = (this.IsAllowed(GuardAI.Smart) ? SpellCombo.Simple : SpellCombo.Strong);
                            this.m_ComboIndex = -1;

                            if (this.m_Guard.Mana >= (ManaReserve + this.m_Combo.Mana))
                                spell = SpellCombo.Process(this.m_Guard, combatant, ref this.m_Combo, ref this.m_ComboIndex, ref toRelease);
                            else
                            {
                                this.m_Combo = null;

                                if (this.m_Guard.Mana >= (ManaReserve + 40))
                                    spell = this.RandomOffenseSpell();
                            }
                        }
                        else if (this.m_Guard.Mana >= (ManaReserve + 40))
                        {
                            spell = this.RandomOffenseSpell();
                        }
                    }

                    if (spell == null && 2 > Utility.Random(100) && this.m_Guard.Mana >= (ManaReserve + 10))
                    {
                        int strMod = this.GetStatMod(this.m_Guard, StatType.Str);
                        int dexMod = this.GetStatMod(this.m_Guard, StatType.Dex);
                        int intMod = this.GetStatMod(this.m_Guard, StatType.Int);

                        List<Type> types = new List<Type>();

                        if (strMod <= 0)
                            types.Add(typeof(StrengthSpell));

                        if (dexMod <= 0 && this.IsAllowed(GuardAI.Melee))
                            types.Add(typeof(AgilitySpell));

                        if (intMod <= 0 && this.IsAllowed(GuardAI.Magic))
                            types.Add(typeof(CunningSpell));

                        if (this.IsAllowed(GuardAI.Bless))
                        {
                            if (types.Count > 1)
                                spell = new BlessSpell(this.m_Guard, null);
                            else if (types.Count == 1)
                                spell = (Spell)Activator.CreateInstance(types[0], new object[] { this.m_Guard, null });
                        }
                        else if (types.Count > 0)
                        {
                            if (types[0] == typeof(StrengthSpell))
                                this.UseItemByType(typeof(BaseStrengthPotion));
                            else if (types[0] == typeof(AgilitySpell))
                                this.UseItemByType(typeof(BaseAgilityPotion));
                        }
                    }

                    if (spell == null && 2 > Utility.Random(100) && this.m_Guard.Mana >= (ManaReserve + 10) && this.IsAllowed(GuardAI.Curse))
                    {
                        if (!combatant.Poisoned && 40 > Utility.Random(100))
                        {
                            spell = new PoisonSpell(this.m_Guard, null);
                        }
                        else
                        {
                            int strMod = this.GetStatMod(combatant, StatType.Str);
                            int dexMod = this.GetStatMod(combatant, StatType.Dex);
                            int intMod = this.GetStatMod(combatant, StatType.Int);

                            List<Type> types = new List<Type>();

                            if (strMod >= 0)
                                types.Add(typeof(WeakenSpell));

                            if (dexMod >= 0 && this.IsAllowed(GuardAI.Melee))
                                types.Add(typeof(ClumsySpell));

                            if (intMod >= 0 && this.IsAllowed(GuardAI.Magic))
                                types.Add(typeof(FeeblemindSpell));

                            if (types.Count > 1)
                                spell = new CurseSpell(this.m_Guard, null);
                            else if (types.Count == 1)
                                spell = (Spell)Activator.CreateInstance(types[0], new object[] { this.m_Guard, null });
                        }
                    }
                }

                if (spell != null && (this.m_Guard.HitsMax - this.m_Guard.Hits + 10) > Utility.Random(100))
                {
                    Type type = null;

                    if (spell is GreaterHealSpell)
                        type = typeof(BaseHealPotion);
                    else if (spell is CureSpell)
                        type = typeof(BaseCurePotion);
                    else if (spell is StrengthSpell)
                        type = typeof(BaseStrengthPotion);
                    else if (spell is AgilitySpell)
                        type = typeof(BaseAgilityPotion);

                    if (type == typeof(BaseHealPotion) && !this.m_Guard.CanBeginAction(type))
                        type = null;

                    if (type != null && this.m_Guard.Target == null && this.UseItemByType(type))
                    {
                        if (spell is GreaterHealSpell)
                        {
                            if ((this.m_Guard.Hits + 30) > this.m_Guard.HitsMax && (this.m_Guard.Hits + 10) < this.m_Guard.HitsMax)
                                spell = new HealSpell(this.m_Guard, null);
                        }
                        else
                        {
                            spell = null;
                        }
                    }
                }
                else if (spell == null && this.m_Guard.Stam < (this.m_Guard.StamMax / 3) && this.IsAllowed(GuardAI.Melee))
                {
                    this.UseItemByType(typeof(BaseRefreshPotion));
                }

                if (spell == null || !spell.Cast())
                    this.EquipWeapon();
            }
            else if (this.m_Mobile.Spell is Spell && ((Spell)this.m_Mobile.Spell).State == SpellState.Sequencing)
            {
                this.EquipWeapon();
            }

            return true;
        }
    }
}