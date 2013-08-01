using System;
using System.Collections.Generic;
using Server.ContextMenus;
using Server.Engines.PartySystem;
using Server.Engines.Quests;
using Server.Engines.XmlSpawner2;
using Server.Factions;
using Server.Items;
using Server.Misc;
using Server.Multis;
using Server.Network;
using Server.Regions;
using Server.SkillHandlers;
using Server.Spells;
using Server.Spells.Bushido;
using Server.Spells.Necromancy;
using Server.Spells.Spellweaving;
using Server.Targeting;

namespace Server.Mobiles
{
    #region Enums
    /// <summary>
    /// Summary description for MobileAI.
    /// </summary>
    ///
    public enum FightMode
    {
        None,			// Never focus on others
        Aggressor,		// Only attack aggressors
        Strongest,		// Attack the strongest
        Weakest,		// Attack the weakest
        Closest, 		// Attack the closest
        Evil			// Only attack aggressor -or- negative karma
    }

    public enum OrderType
    {
        None,			//When no order, let's roam
        Come,			//"(All/Name) come"  Summons all or one pet to your location.
        Drop,			//"(Name) drop"  Drops its loot to the ground (if it carries any).
        Follow,			//"(Name) follow"  Follows targeted being.
        //"(All/Name) follow me"  Makes all or one pet follow you.
        Friend,			//"(Name) friend"  Allows targeted player to confirm resurrection.
        Unfriend,		// Remove a friend
        Guard,			//"(Name) guard"  Makes the specified pet guard you. Pets can only guard their owner.
        //"(All/Name) guard me"  Makes all or one pet guard you.
        Attack,			//"(All/Name) kill",
        //"(All/Name) attack"  All or the specified pet(s) currently under your control attack the target.
        Patrol,			//"(Name) patrol"  Roves between two or more guarded targets.
        Release,		//"(Name) release"  Releases pet back into the wild (removes "tame" status).
        Stay,			//"(All/Name) stay" All or the specified pet(s) will stop and stay in current spot.
        Stop,			//"(All/Name) stop Cancels any current orders to attack, guard or follow.
        Transfer		//"(Name) transfer" Transfers complete ownership to targeted player.
    }

    [Flags]
    public enum FoodType
    {
        None = 0x0000,
        Meat = 0x0001,
        FruitsAndVegies = 0x0002,
        GrainsAndHay = 0x0004,
        Fish = 0x0008,
        Eggs = 0x0010,
        Gold = 0x0020,
        Metal = 0x0040
    }

    [Flags]
    public enum PackInstinct
    {
        None = 0x0000,
        Canine = 0x0001,
        Ostard = 0x0002,
        Feline = 0x0004,
        Arachnid = 0x0008,
        Daemon = 0x0010,
        Bear = 0x0020,
        Equine = 0x0040,
        Bull = 0x0080
    }

    public enum ScaleType
    {
        Red,
        Yellow,
        Black,
        Green,
        White,
        Blue,
        MedusaLight,
        MedusaDark,
        All
    }

    public enum MeatType
    {
        Ribs,
        Bird,
        LambLeg
    }

    public enum HideType
    {
        Regular,
        Spined,
        Horned,
        Barbed,
        Fur
    }

    #endregion

    public class DamageStore : IComparable
    {
        public Mobile m_Mobile;
        public int m_Damage;
        public bool m_HasRight;

        public DamageStore(Mobile m, int damage)
        {
            this.m_Mobile = m;
            this.m_Damage = damage;
        }

        public int CompareTo(object obj)
        {
            DamageStore ds = (DamageStore)obj;

            return ds.m_Damage - this.m_Damage;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class FriendlyNameAttribute : Attribute
    {
        //future use: Talisman 'Protection/Bonus vs. Specific Creature
        private readonly TextDefinition m_FriendlyName;

        public TextDefinition FriendlyName
        {
            get
            {
                return this.m_FriendlyName;
            }
        }

        public FriendlyNameAttribute(TextDefinition friendlyName)
        {
            this.m_FriendlyName = friendlyName;
        }

        public static TextDefinition GetFriendlyNameFor(Type t)
        {
            if (t.IsDefined(typeof(FriendlyNameAttribute), false))
            {
                object[] objs = t.GetCustomAttributes(typeof(FriendlyNameAttribute), false);

                if (objs != null && objs.Length > 0)
                {
                    FriendlyNameAttribute friendly = objs[0] as FriendlyNameAttribute;

                    return friendly.FriendlyName;
                }
            }

            return t.Name;
        }
    }

    public partial class BaseCreature : Mobile, IHonorTarget
    {
        public const int MaxLoyalty = 100;
        public int FollowRange = -1;

        #region Var declarations
        private BaseAI	m_AI;					// THE AI

        private AIType	m_CurrentAI;			// The current AI
        private AIType	m_DefaultAI;			// The default AI

        private Mobile	m_FocusMob;				// Use focus mob instead of combatant, maybe we don't whan to fight
        private FightMode m_FightMode;			// The style the mob uses

        private int m_iRangePerception;		// The view area
        private int m_iRangeFight;			// The fight distance

        private bool	m_bDebugAI;				// Show debug AI messages

        private int m_iTeam;				// Monster Team

        private double	m_dActiveSpeed;			// Timer speed when active
        private double	m_dPassiveSpeed;		// Timer speed when not active
        private double	m_dCurrentSpeed;		// The current speed, lets say it could be changed by something;

        private Point3D m_pHome;				// The home position of the creature, used by some AI
        private int m_iRangeHome = 10;		// The home range of the creature

        List<Type> m_arSpellAttack;		// List of attack spell/power
        List<Type> m_arSpellDefense;		// List of defensive spell/power

        private bool m_bControlled;		// Is controlled
        private Mobile m_ControlMaster;	// My master
        private Mobile m_ControlTarget;	// My target mobile
        private Point3D m_ControlDest;		// My target destination (patrol)
        private OrderType	m_ControlOrder;		// My order

        private int m_Loyalty;

        private double m_dMinTameSkill;
        private bool m_bTamable;

        private bool m_bSummoned = false;
        private DateTime	m_SummonEnd;
        private int m_iControlSlots = 1;

        private bool m_bBardProvoked = false;
        private bool m_bBardPacified = false;
        private Mobile m_bBardMaster = null;
        private Mobile m_bBardTarget = null;
        private DateTime	m_timeBardEnd;
        private WayPoint	m_CurrentWayPoint = null;
        private IPoint2D	m_TargetLocation = null;

        private Mobile m_SummonMaster;

        private int m_HitsMax = -1;
        private	int m_StamMax = -1;
        private int m_ManaMax = -1;
        private int m_DamageMin = -1;
        private int m_DamageMax = -1;

        private int m_PhysicalResistance, m_PhysicalDamage = 100;
        private int m_FireResistance, m_FireDamage;
        private int m_ColdResistance, m_ColdDamage;
        private int m_PoisonResistance, m_PoisonDamage;
        private int m_EnergyResistance, m_EnergyDamage;
        private int m_ChaosDamage;
        private int m_DirectDamage;

        private List<Mobile> m_Owners;
        private List<Mobile> m_Friends;

        private bool m_IsStabled;
        private Mobile m_StabledBy;

        private bool m_HasGeneratedLoot; // have we generated our loot yet?

        private bool m_Paragon;

        private bool m_IsPrisoner;

        private string m_CorpseNameOverride;
        #endregion

        public virtual InhumanSpeech SpeechType
        {
            get
            {
                return null;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string CorpseNameOverride
        {
            get
            {
                return this.m_CorpseNameOverride;
            }
            set
            {
                this.m_CorpseNameOverride = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool IsStabled
        {
            get
            {
                return this.m_IsStabled;
            }
            set
            {
                this.m_IsStabled = value;
                if (this.m_IsStabled)
                    this.StopDeleteTimer();
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public Mobile StabledBy
        {
            get 
            { 
                return this.m_StabledBy; 
            }
            set
            {
                this.m_StabledBy = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsPrisoner
        {
            get
            {
                return this.m_IsPrisoner;
            }
            set
            {
                this.m_IsPrisoner = value;
            }
        }

        protected DateTime SummonEnd
        {
            get
            {
                return this.m_SummonEnd;
            }
            set
            {
                this.m_SummonEnd = value;
            }
        }

        public virtual Faction FactionAllegiance
        {
            get
            {
                return null;
            }
        }
        public virtual int FactionSilverWorth
        {
            get
            {
                return 30;
            }
        }

        #region Bonding
        public const bool BondingEnabled = true;

        public virtual bool IsBondable
        {
            get
            {
                return (BondingEnabled && !this.Summoned && !this.Allured);
            }
        }

        public virtual TimeSpan BondingDelay
        {
            get
            {
                return TimeSpan.FromDays(7.0);
            }
        }

        public virtual TimeSpan BondingAbandonDelay
        {
            get
            {
                return TimeSpan.FromDays(1.0);
            }
        }

        public override bool CanRegenHits
        {
            get
            {
                return !this.m_IsDeadPet && base.CanRegenHits;
            }
        }

        public override bool CanRegenStam
        {
            get
            {
                return !this.m_IsDeadPet && base.CanRegenStam;
            }
        }

        public override bool CanRegenMana
        {
            get
            {
                return !this.m_IsDeadPet && base.CanRegenMana;
            }
        }

        public override bool IsDeadBondedPet
        {
            get
            {
                return this.m_IsDeadPet;
            }
        }

        private bool m_IsBonded;
        private bool m_IsDeadPet;
        private DateTime m_BondingBegin;
        private DateTime m_OwnerAbandonTime;

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile LastOwner
        {
            get
            {
                if (this.m_Owners == null || this.m_Owners.Count == 0)
                    return null;

                return this.m_Owners[this.m_Owners.Count - 1];
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsBonded
        {
            get
            {
                return this.m_IsBonded;
            }
            set
            {
                this.m_IsBonded = value;
                this.InvalidateProperties();
            }
        }

        public bool IsDeadPet
        {
            get
            {
                return this.m_IsDeadPet;
            }
            set
            {
                this.m_IsDeadPet = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime BondingBegin
        {
            get
            {
                return this.m_BondingBegin;
            }
            set
            {
                this.m_BondingBegin = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime OwnerAbandonTime
        {
            get
            {
                return this.m_OwnerAbandonTime;
            }
            set
            {
                this.m_OwnerAbandonTime = value;
            }
        }
        #endregion

        #region Delete Previously Tamed Timer
        private DeleteTimer m_DeleteTimer;

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan DeleteTimeLeft
        {
            get
            {
                if (this.m_DeleteTimer != null && this.m_DeleteTimer.Running)
                    return this.m_DeleteTimer.Next - DateTime.Now;

                return TimeSpan.Zero;
            }
        }

        private class DeleteTimer : Timer
        {
            private Mobile m;

            public DeleteTimer(Mobile creature, TimeSpan delay)
                : base(delay)
            {
                this.m = creature;
                this.Priority = TimerPriority.OneMinute;
            }

            protected override void OnTick()
            {
                this.m.Delete();
            }
        }

        public void BeginDeleteTimer()
        {
            if (!(this is BaseEscortable) && !this.Summoned && !this.Deleted && !this.IsStabled)
            {
                this.StopDeleteTimer();
                this.m_DeleteTimer = new DeleteTimer(this, TimeSpan.FromDays(3.0));
                this.m_DeleteTimer.Start();
            }
        }

        public void StopDeleteTimer()
        {
            if (this.m_DeleteTimer != null)
            {
                this.m_DeleteTimer.Stop();
                this.m_DeleteTimer = null;
            }
        }

        #endregion

        public virtual double WeaponAbilityChance
        {
            get
            {
                return 0.4;
            }
        }

        public virtual WeaponAbility GetWeaponAbility()
        {
            return null;
        }

        #region Elemental Resistance/Damage

        public override int BasePhysicalResistance
        {
            get
            {
                return this.m_PhysicalResistance;
            }
        }
        public override int BaseFireResistance
        {
            get
            {
                return this.m_FireResistance;
            }
        }
        public override int BaseColdResistance
        {
            get
            {
                return this.m_ColdResistance;
            }
        }
        public override int BasePoisonResistance
        {
            get
            {
                return this.m_PoisonResistance;
            }
        }
        public override int BaseEnergyResistance
        {
            get
            {
                return this.m_EnergyResistance;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int PhysicalResistanceSeed
        {
            get
            {
                return this.m_PhysicalResistance;
            }
            set
            {
                this.m_PhysicalResistance = value;
                this.UpdateResistances();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int FireResistSeed
        {
            get
            {
                return this.m_FireResistance;
            }
            set
            {
                this.m_FireResistance = value;
                this.UpdateResistances();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ColdResistSeed
        {
            get
            {
                return this.m_ColdResistance;
            }
            set
            {
                this.m_ColdResistance = value;
                this.UpdateResistances();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int PoisonResistSeed
        {
            get
            {
                return this.m_PoisonResistance;
            }
            set
            {
                this.m_PoisonResistance = value;
                this.UpdateResistances();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int EnergyResistSeed
        {
            get
            {
                return this.m_EnergyResistance;
            }
            set
            {
                this.m_EnergyResistance = value;
                this.UpdateResistances();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int PhysicalDamage
        {
            get
            {
                return this.m_PhysicalDamage;
            }
            set
            {
                this.m_PhysicalDamage = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int FireDamage
        {
            get
            {
                return this.m_FireDamage;
            }
            set
            {
                this.m_FireDamage = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ColdDamage
        {
            get
            {
                return this.m_ColdDamage;
            }
            set
            {
                this.m_ColdDamage = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int PoisonDamage
        {
            get
            {
                return this.m_PoisonDamage;
            }
            set
            {
                this.m_PoisonDamage = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int EnergyDamage
        {
            get
            {
                return this.m_EnergyDamage;
            }
            set
            {
                this.m_EnergyDamage = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ChaosDamage
        {
            get
            {
                return this.m_ChaosDamage;
            }
            set
            {
                this.m_ChaosDamage = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int DirectDamage
        {
            get
            {
                return this.m_DirectDamage;
            }
            set
            {
                this.m_DirectDamage = value;
            }
        }

        #endregion

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsParagon
        {
            get
            {
                return this.m_Paragon;
            }
            set
            {
                if (this.m_Paragon == value)
                    return;
                else if (value)
                    XmlParagon.Convert(this);
                else
                    XmlParagon.UnConvert(this);

                this.m_Paragon = value;

                this.InvalidateProperties();
            }
        }

        public virtual FoodType FavoriteFood
        {
            get
            {
                return FoodType.Meat;
            }
        }
        public virtual PackInstinct PackInstinct
        {
            get
            {
                return PackInstinct.None;
            }
        }

        public List<Mobile> Owners
        {
            get
            {
                return this.m_Owners;
            }
        }

        public virtual bool AllowMaleTamer
        {
            get
            {
                return true;
            }
        }
        public virtual bool AllowFemaleTamer
        {
            get
            {
                return true;
            }
        }
        public virtual bool SubdueBeforeTame
        {
            get
            {
                return false;
            }
        }
        public virtual bool StatLossAfterTame
        {
            get
            {
                return this.SubdueBeforeTame;
            }
        }
        public virtual bool ReduceSpeedWithDamage
        {
            get
            {
                return true;
            }
        }
        public virtual bool IsSubdued
        {
            get
            {
                return this.SubdueBeforeTame && (this.Hits < (this.HitsMax / 10));
            }
        }

        public virtual bool Commandable
        {
            get
            {
                return true;
            }
        }

        public virtual Poison HitPoison
        {
            get
            {
                return null;
            }
        }
        public virtual double HitPoisonChance
        {
            get
            {
                return 0.5;
            }
        }
        public virtual Poison PoisonImmune
        {
            get
            {
                return null;
            }
        }

        public virtual bool BardImmune
        {
            get
            {
                return false;
            }
        }
        public virtual bool Unprovokable
        {
            get
            {
                return this.BardImmune || this.m_IsDeadPet;
            }
        }
        public virtual bool Uncalmable
        {
            get
            {
                return this.BardImmune || this.m_IsDeadPet;
            }
        }
        public virtual bool AreaPeaceImmune
        {
            get
            {
                return this.BardImmune || this.m_IsDeadPet;
            }
        }

        public virtual bool BleedImmune
        {
            get
            {
                return false;
            }
        }
        public virtual double BonusPetDamageScalar
        {
            get
            {
                return 1.0;
            }
        }

        public virtual bool DeathAdderCharmable
        {
            get
            {
                return false;
            }
        }

        //TODO: Find the pub 31 tweaks to the DispelDifficulty and apply them of course.
        public virtual double DispelDifficulty
        {
            get
            {
                return 0.0;
            }
        }// at this skill level we dispel 50% chance
        public virtual double DispelFocus
        {
            get
            {
                return 20.0;
            }
        }// at difficulty - focus we have 0%, at difficulty + focus we have 100%
        public virtual bool DisplayWeight
        {
            get
            {
                return this.Backpack is StrongBackpack;
            }
        }

        #region Breath ability, like dragon fire breath
        private DateTime m_NextBreathTime;

        // Must be overriden in subclass to enable
        public virtual bool HasBreath
        {
            get
            {
                return false;
            }
        }

        // Base damage given is: CurrentHitPoints * BreathDamageScalar
        public virtual double BreathDamageScalar
        {
            get
            {
                return (Core.AOS ? 0.16 : 0.05);
            }
        }

        // Min/max seconds until next breath
        public virtual double BreathMinDelay
        {
            get
            {
                return 10.0;
            }
        }
        public virtual double BreathMaxDelay
        {
            get
            {
                return 15.0;
            }
        }

        // Creature stops moving for 1.0 seconds while breathing
        public virtual double BreathStallTime
        {
            get
            {
                return 1.0;
            }
        }

        // Effect is sent 1.3 seconds after BreathAngerSound and BreathAngerAnimation is played
        public virtual double BreathEffectDelay
        {
            get
            {
                return 1.3;
            }
        }

        // Damage is given 1.0 seconds after effect is sent
        public virtual double BreathDamageDelay
        {
            get
            {
                return 1.0;
            }
        }

        public virtual int BreathRange
        {
            get
            {
                return this.RangePerception;
            }
        }

        // Damage types
        public virtual int BreathChaosDamage
        {
            get
            {
                return 0;
            }
        }
        public virtual int BreathPhysicalDamage
        {
            get
            {
                return 0;
            }
        }
        public virtual int BreathFireDamage
        {
            get
            {
                return 100;
            }
        }
        public virtual int BreathColdDamage
        {
            get
            {
                return 0;
            }
        }
        public virtual int BreathPoisonDamage
        {
            get
            {
                return 0;
            }
        }
        public virtual int BreathEnergyDamage
        {
            get
            {
                return 0;
            }
        }

        // Is immune to breath damages
        public virtual bool BreathImmune
        {
            get
            {
                return false;
            }
        }

        // Effect details and sound
        public virtual int BreathEffectItemID
        {
            get
            {
                return 0x36D4;
            }
        }
        public virtual int BreathEffectSpeed
        {
            get
            {
                return 5;
            }
        }
        public virtual int BreathEffectDuration
        {
            get
            {
                return 0;
            }
        }
        public virtual bool BreathEffectExplodes
        {
            get
            {
                return false;
            }
        }
        public virtual bool BreathEffectFixedDir
        {
            get
            {
                return false;
            }
        }
        public virtual int BreathEffectHue
        {
            get
            {
                return 0;
            }
        }
        public virtual int BreathEffectRenderMode
        {
            get
            {
                return 0;
            }
        }

        public virtual int BreathEffectSound
        {
            get
            {
                return 0x227;
            }
        }

        // Anger sound/animations
        public virtual int BreathAngerSound
        {
            get
            {
                return this.GetAngerSound();
            }
        }
        public virtual int BreathAngerAnimation
        {
            get
            {
                return 12;
            }
        }

        public virtual void BreathStart(Mobile target)
        {
            this.BreathStallMovement();
            this.BreathPlayAngerSound();
            this.BreathPlayAngerAnimation();

            this.Direction = this.GetDirectionTo(target);

            Timer.DelayCall(TimeSpan.FromSeconds(this.BreathEffectDelay), new TimerStateCallback(BreathEffect_Callback), target);
        }

        public virtual void BreathStallMovement()
        {
            if (this.m_AI != null)
                this.m_AI.NextMove = DateTime.Now + TimeSpan.FromSeconds(this.BreathStallTime);
        }

        public virtual void BreathPlayAngerSound()
        {
            this.PlaySound(this.BreathAngerSound);
        }

        public virtual void BreathPlayAngerAnimation()
        {
            this.Animate(this.BreathAngerAnimation, 5, 1, true, false, 0);
        }

        public virtual void BreathEffect_Callback(object state)
        {
            Mobile target = (Mobile)state;

            if (!target.Alive || !this.CanBeHarmful(target))
                return;

            this.BreathPlayEffectSound();
            this.BreathPlayEffect(target);

            Timer.DelayCall(TimeSpan.FromSeconds(this.BreathDamageDelay), new TimerStateCallback(BreathDamage_Callback), target);
        }

        public virtual void BreathPlayEffectSound()
        {
            this.PlaySound(this.BreathEffectSound);
        }

        public virtual void BreathPlayEffect(Mobile target)
        {
            Effects.SendMovingEffect(this, target, this.BreathEffectItemID,
                this.BreathEffectSpeed, this.BreathEffectDuration, this.BreathEffectFixedDir,
                this.BreathEffectExplodes, this.BreathEffectHue, this.BreathEffectRenderMode);
        }

        public virtual void BreathDamage_Callback(object state)
        {
            Mobile target = (Mobile)state;

            if (target is BaseCreature && ((BaseCreature)target).BreathImmune)
                return;

            if (this.CanBeHarmful(target))
            {
                this.DoHarmful(target);
                this.BreathDealDamage(target);
            }
        }

        public virtual void BreathDealDamage(Mobile target)
        {
            if (!Evasion.CheckSpellEvasion(target))
            {
                int physDamage = this.BreathPhysicalDamage;
                int fireDamage = this.BreathFireDamage;
                int coldDamage = this.BreathColdDamage;
                int poisDamage = this.BreathPoisonDamage;
                int nrgyDamage = this.BreathEnergyDamage;

                if (this.BreathChaosDamage > 0)
                {
                    switch( Utility.Random(5))
                    {
                        case 0:
                            physDamage += this.BreathChaosDamage;
                            break;
                        case 1:
                            fireDamage += this.BreathChaosDamage;
                            break;
                        case 2:
                            coldDamage += this.BreathChaosDamage;
                            break;
                        case 3:
                            poisDamage += this.BreathChaosDamage;
                            break;
                        case 4:
                            nrgyDamage += this.BreathChaosDamage;
                            break;
                    }
                }

                if (physDamage == 0 && fireDamage == 0 && coldDamage == 0 && poisDamage == 0 && nrgyDamage == 0)
                {
                    target.Damage(this.BreathComputeDamage(), this);// Unresistable damage even in AOS
                }
                else
                {
                    AOS.Damage(target, this, this.BreathComputeDamage(), physDamage, fireDamage, coldDamage, poisDamage, nrgyDamage);
                }
            }
        }

        public virtual int BreathComputeDamage()
        {
            int damage = (int)(this.Hits * this.BreathDamageScalar);

            if (this.IsParagon)
                damage = (int)(damage / XmlParagon.GetHitsBuff(this));

            if (damage > 200)
                damage = 200;

            return damage;
        }

        #endregion

        public virtual bool CanFly
        {
            get
            {
                return false;
            }
        }

        #region Spill Acid

        public void SpillAcid(int Amount)
        {
            this.SpillAcid(null, Amount);
        }

        public void SpillAcid(Mobile target, int Amount)
        {
            if ((target != null && target.Map == null) || this.Map == null)
                return;

            for (int i = 0; i < Amount; ++i)
            {
                Point3D loc = this.Location;
                Map map = this.Map;
                Item acid = this.NewHarmfulItem();

                if (target != null && target.Map != null && Amount == 1)
                {
                    loc = target.Location;
                    map = target.Map;
                }
                else
                {
                    bool validLocation = false;
                    for (int j = 0; !validLocation && j < 10; ++j)
                    {
                        loc = new Point3D(
                            loc.X + (Utility.Random(0, 3) - 2),
                            loc.Y + (Utility.Random(0, 3) - 2),
                            loc.Z);
                        loc.Z = map.GetAverageZ(loc.X, loc.Y);
                        validLocation = map.CanFit(loc, 16, false, false) ;
                    }
                }
                acid.MoveToWorld(loc, map);
            }
        }

        /*
        Solen Style, override me for other mobiles/items:
        kappa+acidslime, grizzles+whatever, etc.
        */

        public virtual Item NewHarmfulItem()
        {
            return new PoolOfAcid(TimeSpan.FromSeconds(10), 30, 30);
        }

        #endregion

        #region Flee!!!
        public virtual bool CanFlee
        {
            get
            {
                return !this.m_Paragon;
            }
        }

        private DateTime m_EndFlee;

        public DateTime EndFleeTime
        {
            get
            {
                return this.m_EndFlee;
            }
            set
            {
                this.m_EndFlee = value;
            }
        }

        public virtual void StopFlee()
        {
            this.m_EndFlee = DateTime.MinValue;
        }

        public virtual bool CheckFlee()
        {
            if (this.m_EndFlee == DateTime.MinValue)
                return false;

            if (DateTime.Now >= this.m_EndFlee)
            {
                this.StopFlee();
                return false;
            }

            return true;
        }

        public virtual void BeginFlee(TimeSpan maxDuration)
        {
            this.m_EndFlee = DateTime.Now + maxDuration;
        }

        #endregion

        public BaseAI AIObject
        {
            get
            {
                return this.m_AI;
            }
        }

        public const int MaxOwners = 5;

        public virtual OppositionGroup OppositionGroup
        {
            get
            {
                return null;
            }
        }

        #region Friends
        public List<Mobile> Friends
        {
            get
            {
                return this.m_Friends;
            }
        }

        public virtual bool AllowNewPetFriend
        {
            get
            {
                return (this.m_Friends == null || this.m_Friends.Count < 5);
            }
        }

        public virtual bool IsPetFriend(Mobile m)
        {
            return (this.m_Friends != null && this.m_Friends.Contains(m));
        }

        public virtual void AddPetFriend(Mobile m)
        {
            if (this.m_Friends == null)
                this.m_Friends = new List<Mobile>();

            this.m_Friends.Add(m);
        }

        public virtual void RemovePetFriend(Mobile m)
        {
            if (this.m_Friends != null)
                this.m_Friends.Remove(m);
        }

        public virtual bool IsFriend(Mobile m)
        {
            OppositionGroup g = this.OppositionGroup;

            if (g != null && g.IsEnemy(this, m))
                return false;

            if (!(m is BaseCreature))
                return false;

            BaseCreature c = (BaseCreature)m;

            return (this.m_iTeam == c.m_iTeam && ((this.m_bSummoned || this.m_bControlled) == (c.m_bSummoned || c.m_bControlled))/* && c.Combatant != this */);
        }

        #endregion

        #region Allegiance
        public virtual Ethics.Ethic EthicAllegiance
        {
            get
            {
                return null;
            }
        }

        public enum Allegiance
        {
            None,
            Ally,
            Enemy
        }

        public virtual Allegiance GetFactionAllegiance(Mobile mob)
        {
            if (mob == null || mob.Map != Faction.Facet || this.FactionAllegiance == null)
                return Allegiance.None;

            Faction fac = Faction.Find(mob, true);

            if (fac == null)
                return Allegiance.None;

            return (fac == this.FactionAllegiance ? Allegiance.Ally : Allegiance.Enemy);
        }

        public virtual Allegiance GetEthicAllegiance(Mobile mob)
        {
            if (mob == null || mob.Map != Faction.Facet || this.EthicAllegiance == null)
                return Allegiance.None;

            Ethics.Ethic ethic = Ethics.Ethic.Find(mob, true);

            if (ethic == null)
                return Allegiance.None;

            return (ethic == this.EthicAllegiance ? Allegiance.Ally : Allegiance.Enemy);
        }

        #endregion

        public virtual bool IsEnemy(Mobile m)
        {
            XmlIsEnemy a = (XmlIsEnemy)XmlAttach.FindAttachment(this, typeof(XmlIsEnemy));
            if (a != null)
            {
                return a.IsEnemy(m);
            }

            OppositionGroup g = this.OppositionGroup;

            if (g != null && g.IsEnemy(this, m))
                return true;

            if (m is BaseGuard)
                return false;

            if (this.GetFactionAllegiance(m) == Allegiance.Ally)
                return false;

            Ethics.Ethic ourEthic = this.EthicAllegiance;
            Ethics.Player pl = Ethics.Player.Find(m, true);

            if (pl != null && pl.IsShielded && (ourEthic == null || ourEthic == pl.Ethic))
                return false;

            if (!(m is BaseCreature) || m is Server.Engines.Quests.Haven.MilitiaFighter)
                return true;

            if (TransformationSpellHelper.UnderTransformation(m, typeof(EtherealVoyageSpell)))
                return false;

            if (m is PlayerMobile && ((PlayerMobile)m).HonorActive)
                return false;

            BaseCreature c = (BaseCreature)m;

            if ((this.FightMode == FightMode.Evil && m.Karma < 0) || (c.FightMode == FightMode.Evil && this.Karma < 0))
                return true;

            return (this.m_iTeam != c.m_iTeam || ((this.m_bSummoned || this.m_bControlled) != (c.m_bSummoned || c.m_bControlled))/* || c.Combatant == this*/);
        }

        public override string ApplyNameSuffix(string suffix)
        {
            XmlData customtitle = (XmlData)XmlAttach.FindAttachment(this, typeof(XmlData), "ParagonTitle");

            if (customtitle != null)
            {
                suffix = customtitle.Data;
            }
            else if (this.IsParagon)
            {
                if (suffix.Length == 0)
                    suffix = XmlParagon.GetParagonLabel(this);
                else
                    suffix = String.Concat(suffix, " " + XmlParagon.GetParagonLabel(this));

                XmlAttach.AttachTo(this, new XmlData("ParagonTitle", suffix));
            }

            return base.ApplyNameSuffix(suffix);
        }

        public virtual bool CheckControlChance(Mobile m)
        {
            if (this.GetControlChance(m) > Utility.RandomDouble())
            {
                this.Loyalty += 1;
                return true;
            }

            this.PlaySound(this.GetAngerSound());

            if (this.Body.IsAnimal)
                this.Animate(10, 5, 1, true, false, 0);
            else if (this.Body.IsMonster)
                this.Animate(18, 5, 1, true, false, 0);

            this.Loyalty -= 3;
            return false;
        }

        public virtual bool CanBeControlledBy(Mobile m)
        {
            return (this.GetControlChance(m) > 0.0);
        }

        public double GetControlChance(Mobile m)
        {
            return this.GetControlChance(m, false);
        }

        public virtual double GetControlChance(Mobile m, bool useBaseSkill)
        {
            if (this.m_dMinTameSkill <= 29.1 || this.m_bSummoned || m.AccessLevel >= AccessLevel.GameMaster)
                return 1.0;

            double dMinTameSkill = this.m_dMinTameSkill;

            if (dMinTameSkill > -24.9 && Server.SkillHandlers.AnimalTaming.CheckMastery(m, this))
                dMinTameSkill = -24.9;

            int taming = (int)((useBaseSkill ? m.Skills[SkillName.AnimalTaming].Base : m.Skills[SkillName.AnimalTaming].Value) * 10);
            int lore = (int)((useBaseSkill ? m.Skills[SkillName.AnimalLore].Base : m.Skills[SkillName.AnimalLore].Value) * 10);
            int bonus = 0, chance = 700;

            if (Core.ML)
            {
                int SkillBonus = taming - (int)(dMinTameSkill * 10);
                int LoreBonus = lore - (int)(dMinTameSkill * 10);

                int SkillMod = 6, LoreMod = 6;

                if (SkillBonus < 0)
                    SkillMod = 28;
                if (LoreBonus < 0)
                    LoreMod = 14;

                SkillBonus *= SkillMod;
                LoreBonus *= LoreMod;

                bonus = (SkillBonus + LoreBonus) / 2;
            }
            else
            {
                int difficulty = (int)(dMinTameSkill * 10);
                int weighted = ((taming * 4) + lore) / 5;
                bonus = weighted - difficulty;

                if (bonus <= 0)
                    bonus *= 14;
                else
                    bonus *= 6;
            }

            chance += bonus;

            if (chance >= 0 && chance < 200)
                chance = 200;
            else if (chance > 990)
                chance = 990;

            chance -= (MaxLoyalty - this.m_Loyalty) * 10;

            chance += (int)XmlMobFactions.GetScaledFaction(m, this, -250, 250, 0.001);

            return ((double)chance / 1000);
        }

        private static Type[] m_AnimateDeadTypes = new Type[]
        {
            typeof(MoundOfMaggots), typeof(HellSteed), typeof(SkeletalMount),
            typeof(WailingBanshee), typeof(Wraith), typeof(SkeletalDragon),
            typeof(LichLord), typeof(FleshGolem), typeof(Lich),
            typeof(SkeletalKnight), typeof(BoneKnight), typeof(Mummy),
            typeof(SkeletalMage), typeof(BoneMagi), typeof(PatchworkSkeleton)
        };

        public virtual bool IsAnimatedDead
        {
            get
            {
                if (!this.Summoned)
                    return false;

                Type type = this.GetType();

                bool contains = false;

                for (int i = 0; !contains && i < m_AnimateDeadTypes.Length; ++i)
                    contains = (type == m_AnimateDeadTypes[i]);

                return contains;
            }
        }

        public virtual bool IsNecroFamiliar
        {
            get
            {
                if (!this.Summoned)
                    return false;

                if (this.m_ControlMaster != null && SummonFamiliarSpell.Table.Contains(this.m_ControlMaster))
                    return SummonFamiliarSpell.Table[this.m_ControlMaster] == this;

                return false;
            }
        }

        public override void Damage(int amount, Mobile from)
        {
            int oldHits = this.Hits;

            if (Core.AOS && !this.Summoned && this.Controlled && 0.2 > Utility.RandomDouble())
                amount = (int)(amount * this.BonusPetDamageScalar);

            if (Spells.Necromancy.EvilOmenSpell.TryEndEffect(this))
                amount = (int)(amount * 1.25);

            Mobile oath = Spells.Necromancy.BloodOathSpell.GetBloodOath(from);

            if (oath == this)
            {
                amount = (int)(amount * 1.1);
                from.Damage(amount, from);
            }

            base.Damage(amount, from);

            if (this.SubdueBeforeTame && !this.Controlled)
            {
                if ((oldHits > (this.HitsMax / 10)) && (this.Hits <= (this.HitsMax / 10)))
                    this.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "* The creature has been beaten into subjugation! *");
            }
        }

        public virtual bool DeleteCorpseOnDeath
        {
            get
            {
                return !Core.AOS && this.m_bSummoned;
            }
        }

        public override void SetLocation(Point3D newLocation, bool isTeleport)
        {
            base.SetLocation(newLocation, isTeleport);

            if (isTeleport && this.m_AI != null)
                this.m_AI.OnTeleported();
        }

        public override void OnBeforeSpawn(Point3D location, Map m)
        {
            if (XmlParagon.CheckConvert(this, location, m))
                this.IsParagon = true;

            base.OnBeforeSpawn(location, m);
        }

        public override ApplyPoisonResult ApplyPoison(Mobile from, Poison poison)
        {
            if (!this.Alive || this.IsDeadPet)
                return ApplyPoisonResult.Immune;

            if (Spells.Necromancy.EvilOmenSpell.TryEndEffect(this))
                poison = PoisonImpl.IncreaseLevel(poison);

            ApplyPoisonResult result = base.ApplyPoison(from, poison);

            if (from != null && result == ApplyPoisonResult.Poisoned && this.PoisonTimer is PoisonImpl.PoisonTimer)
                (this.PoisonTimer as PoisonImpl.PoisonTimer).From = from;

            return result;
        }

        public override bool CheckPoisonImmunity(Mobile from, Poison poison)
        {
            if (base.CheckPoisonImmunity(from, poison))
                return true;

            Poison p = this.PoisonImmune;

            XmlPoison xp = (XmlPoison)XmlAttach.FindAttachment(this, typeof(XmlPoison));
            
            if (xp != null)
            {
                p = xp.PoisonImmune;
            }

            return (p != null && p.RealLevel >= poison.RealLevel);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Loyalty
        {
            get
            {
                return this.m_Loyalty;
            }
            set
            {
                this.m_Loyalty = Math.Min(Math.Max(value, 0), MaxLoyalty);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public WayPoint CurrentWayPoint
        {
            get
            {
                return this.m_CurrentWayPoint;
            }
            set
            {
                this.m_CurrentWayPoint = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public IPoint2D TargetLocation
        {
            get
            {
                return this.m_TargetLocation;
            }
            set
            {
                this.m_TargetLocation = value;
            }
        }

        public virtual Mobile ConstantFocus
        {
            get
            {
                return null;
            }
        }

        public virtual bool DisallowAllMoves
        {
            get
            {
                XmlData x = (XmlData)XmlAttach.FindAttachment(this, typeof(XmlData), "NoSpecials");
                
                if (x != null && x.Data == "True")
                {
                    return true;
                }

                return false;
            }
        }

        public virtual bool InitialInnocent
        {
            get
            {
                XmlData x = (XmlData)XmlAttach.FindAttachment(this, typeof(XmlData), "Notoriety");
                
                if (x != null && x.Data == "blue")
                {
                    return true;
                }

                return false;
            }
        }

        public virtual bool AlwaysMurderer
        {
            get
            {
                XmlData x = (XmlData)XmlAttach.FindAttachment(this, typeof(XmlData), "Notoriety");
                
                if (x != null && x.Data == "red")
                {
                    return true;
                }

                return false;
            }
        }

        public virtual bool AlwaysAttackable
        {
            get
            {
                XmlData x = (XmlData)XmlAttach.FindAttachment(this, typeof(XmlData), "Notoriety");
                
                if (x != null && x.Data == "gray")
                {
                    return true;
                }

                return false;
            }
        }

        public virtual bool HoldSmartSpawning
        {
            get
            {
                if (this.IsParagon)
                    return true;
                else
                    return false;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int DamageMin
        {
            get
            {
                return this.m_DamageMin;
            }
            set
            {
                this.m_DamageMin = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int DamageMax
        {
            get
            {
                return this.m_DamageMax;
            }
            set
            {
                this.m_DamageMax = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public override int HitsMax
        {
            get
            {
                if (this.m_HitsMax > 0)
                {
                    int value = this.m_HitsMax + this.GetStatOffset(StatType.Str);

                    if (value < 1)
                        value = 1;
                    else if (value > 1000000)
                        value = 1000000;

                    return value;
                }

                return this.Str;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int HitsMaxSeed
        {
            get
            {
                return this.m_HitsMax;
            }
            set
            {
                this.m_HitsMax = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public override int StamMax
        {
            get
            {
                if (this.m_StamMax > 0)
                {
                    int value = this.m_StamMax + this.GetStatOffset(StatType.Dex);

                    if (value < 1)
                        value = 1;
                    else if (value > 1000000)
                        value = 1000000;

                    return value;
                }

                return this.Dex;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int StamMaxSeed
        {
            get
            {
                return this.m_StamMax;
            }
            set
            {
                this.m_StamMax = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public override int ManaMax
        {
            get
            {
                if (this.m_ManaMax > 0)
                {
                    int value = this.m_ManaMax + this.GetStatOffset(StatType.Int);

                    if (value < 1)
                        value = 1;
                    else if (value > 1000000)
                        value = 1000000;

                    return value;
                }

                return this.Int;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ManaMaxSeed
        {
            get
            {
                return this.m_ManaMax;
            }
            set
            {
                this.m_ManaMax = value;
            }
        }

        public virtual bool CanOpenDoors
        {
            get
            {
                return !this.Body.IsAnimal && !this.Body.IsSea;
            }
        }

        public virtual bool CanMoveOverObstacles
        {
            get
            {
                return Core.AOS || this.Body.IsMonster;
            }
        }

        public virtual bool CanDestroyObstacles
        {
            get
            {
                // to enable breaking of furniture, 'return CanMoveOverObstacles;'
                return false;
            }
        }

        public void Unpacify()
        {
            this.BardEndTime = DateTime.Now;
            this.BardPacified = false;
        }

        private HonorContext m_ReceivedHonorContext;

        public HonorContext ReceivedHonorContext
        {
            get
            {
                return this.m_ReceivedHonorContext;
            }
            set
            {
                this.m_ReceivedHonorContext = value;
            }
        }

        /*
        Seems this actually was removed on OSI somewhere between the original bug report and now.
        We will call it ML, until we can get better information. I suspect it was on the OSI TC when
        originally it taken out of RunUO, and not implmented on OSIs production shards until more 
        recently.  Either way, this is, or was, accurate OSI behavior, and just entirely 
        removing it was incorrect.  OSI followers were distracted by being attacked well into
        AoS, at very least.
        */
        public virtual bool CanBeDistracted
        {
            get
            {
                return !Core.ML;
            }
        }

        public virtual void CheckDistracted(Mobile from)
        {
            if (Utility.RandomDouble() < .10)
            {
                this.ControlTarget = from;
                this.ControlOrder = OrderType.Attack;
                this.Combatant = from;
                this.Warmode = true;
            }
        }

        public override void OnDamage(int amount, Mobile from, bool willKill)
        {
            if (this.BardPacified && (this.HitsMax - this.Hits) * 0.001 > Utility.RandomDouble())
                this.Unpacify();

            int disruptThreshold;
            //NPCs can use bandages too!
            if (!Core.AOS)
                disruptThreshold = 0;
            else if (from != null && from.Player)
                disruptThreshold = 18;
            else
                disruptThreshold = 25;

            if (amount > disruptThreshold)
            {
                BandageContext c = BandageContext.GetContext(this);

                if (c != null)
                    c.Slip();
            }

            if (Confidence.IsRegenerating(this))
                Confidence.StopRegenerating(this);

            WeightOverloading.FatigueOnDamage(this, amount);

            InhumanSpeech speechType = this.SpeechType;

            if (speechType != null && !willKill)
                speechType.OnDamage(this, amount);

            if (this.m_ReceivedHonorContext != null)
                this.m_ReceivedHonorContext.OnTargetDamaged(from, amount);

            if (!willKill)
            {
                if (this.CanBeDistracted && this.ControlOrder == OrderType.Follow)
                {
                    this.CheckDistracted(from);
                }
            }
            else if (from is PlayerMobile)
            {
                Timer.DelayCall(TimeSpan.FromSeconds(10), new TimerCallback(((PlayerMobile)from).RecoverAmmo));
            }

            base.OnDamage(amount, from, willKill);
        }

        public virtual void OnDamagedBySpell(Mobile from)
        {
            if (this.CanBeDistracted && this.ControlOrder == OrderType.Follow)
            {
                this.CheckDistracted(from);
            }
        }

        public virtual void OnHarmfulSpell(Mobile from)
        {
        }

        #region Alter[...]Damage From/To

        public virtual void AlterDamageScalarFrom(Mobile caster, ref double scalar)
        {
        }

        public virtual void AlterDamageScalarTo(Mobile target, ref double scalar)
        {
        }

        public virtual void AlterSpellDamageFrom(Mobile from, ref int damage)
        {
        }

        public virtual void AlterSpellDamageTo(Mobile to, ref int damage)
        {
        }

        public virtual void AlterMeleeDamageFrom(Mobile from, ref int damage)
        {
            #region Mondain's Legacy
            if (from != null && from.Talisman is BaseTalisman)
            {
                BaseTalisman talisman = (BaseTalisman)from.Talisman;

                if (talisman.Killer != null && talisman.Killer.Type != null)
                {
                    Type type = talisman.Killer.Type;

                    if (type.IsAssignableFrom(this.GetType()))
                        damage = (int)(damage * (1 + (double)talisman.Killer.Amount / 100));
                }
            }
            #endregion
        }

        public virtual void AlterMeleeDamageTo(Mobile to, ref int damage)
        {
        }

        #endregion

        public virtual void CheckReflect(Mobile caster, ref bool reflect)
        {
        }

        public virtual void OnCarve(Mobile from, Corpse corpse, Item with)
        {
            int feathers = this.Feathers;
            int wool = this.Wool;
            int meat = this.Meat;
            int hides = this.Hides;
            int scales = this.Scales;
            int dragonblood = this.DragonBlood;

            if ((feathers == 0 && wool == 0 && meat == 0 && hides == 0 && scales == 0) || this.Summoned || this.IsBonded || corpse.Animated)
            {
                if (corpse.Animated)
                    corpse.SendLocalizedMessageTo(from, 500464); // Use this on corpses to carve away meat and hide
                else
                    from.SendLocalizedMessage(500485); // You see nothing useful to carve from the corpse.
            }
            else
            {
                if (Core.ML && from.Race == Race.Human)
                    hides = (int)Math.Ceiling(hides * 1.1); // 10% bonus only applies to hides, ore & logs

                if (corpse.Map == Map.Felucca)
                {
                    feathers *= 2;
                    wool *= 2;
                    hides *= 2;

                    if (Core.ML)
                    {
                        meat *= 2;
                        scales *= 2;
                    }
                }

                new Blood(0x122D).MoveToWorld(corpse.Location, corpse.Map);

                if (feathers != 0)
                {
                    corpse.AddCarvedItem(new Feather(feathers), from);
                    from.SendLocalizedMessage(500479); // You pluck the bird. The feathers are now on the corpse.
                }

                if (wool != 0)
                {
                    corpse.AddCarvedItem(new TaintedWool(wool), from);
                    from.SendLocalizedMessage(500483); // You shear it, and the wool is now on the corpse.
                }

                if (meat != 0)
                {
                    if (this.MeatType == MeatType.Ribs)
                        corpse.AddCarvedItem(new RawRibs(meat), from);
                    else if (this.MeatType == MeatType.Bird)
                        corpse.AddCarvedItem(new RawBird(meat), from);
                    else if (this.MeatType == MeatType.LambLeg)
                        corpse.AddCarvedItem(new RawLambLeg(meat), from);

                    from.SendLocalizedMessage(500467); // You carve some meat, which remains on the corpse.
                }

                if (hides != 0)
                {
                    Item holding = from.Weapon as Item;

                    if (Core.AOS && (holding is SkinningKnife /* TODO: || holding is ButcherWarCleaver || with is ButcherWarCleaver */))
                    {
                        Item leather = null;

                        switch ( this.HideType )
                        {
                            case HideType.Regular:
                                leather = new Leather(hides);
                                break;
                            case HideType.Spined:
                                leather = new SpinedLeather(hides);
                                break;
                            case HideType.Horned:
                                leather = new HornedLeather(hides);
                                break;
                            case HideType.Barbed:
                                leather = new BarbedLeather(hides);
                                break;
                        }

                        if (leather != null)
                        {
                            if (!from.PlaceInBackpack(leather))
                            {
                                corpse.DropItem(leather);
                                from.SendLocalizedMessage(500471); // You skin it, and the hides are now in the corpse.
                            }
                            else
                            {
                                from.SendLocalizedMessage(1073555); // You skin it and place the cut-up hides in your backpack.
                            }
                        }
                    }
                    else
                    {
                        if (this.HideType == HideType.Regular)
                            corpse.DropItem(new Hides(hides));
                        else if (this.HideType == HideType.Spined)
                            corpse.DropItem(new SpinedHides(hides));
                        else if (this.HideType == HideType.Horned)
                            corpse.DropItem(new HornedHides(hides));
                        else if (this.HideType == HideType.Barbed)
                            corpse.DropItem(new BarbedHides(hides));

                        from.SendLocalizedMessage(500471); // You skin it, and the hides are now in the corpse.
                    }
                }

                if (scales != 0)
                {
                    ScaleType sc = this.ScaleType;

                    switch ( sc )
                    {
                        case ScaleType.Red:
                            corpse.AddCarvedItem(new RedScales(scales), from);
                            break;
                        case ScaleType.Yellow:
                            corpse.AddCarvedItem(new YellowScales(scales), from);
                            break;
                        case ScaleType.Black:
                            corpse.AddCarvedItem(new BlackScales(scales), from);
                            break;
                        case ScaleType.Green:
                            corpse.AddCarvedItem(new GreenScales(scales), from);
                            break;
                        case ScaleType.White:
                            corpse.AddCarvedItem(new WhiteScales(scales), from);
                            break;
                        case ScaleType.Blue:
                            corpse.AddCarvedItem(new BlueScales(scales), from);
                            break;
                        case ScaleType.All:
                            {
                                corpse.AddCarvedItem(new RedScales(scales), from);
                                corpse.AddCarvedItem(new YellowScales(scales), from);
                                corpse.AddCarvedItem(new BlackScales(scales), from);
                                corpse.AddCarvedItem(new GreenScales(scales), from);
                                corpse.AddCarvedItem(new WhiteScales(scales), from);
                                corpse.AddCarvedItem(new BlueScales(scales), from);
                                break;
                            }
                    }

                    from.SendMessage("You cut away some scales, but they remain on the corpse.");
                }

                if (dragonblood != 0)
                {
                    corpse.AddCarvedItem(new DragonBlood(dragonblood), from);
                    from.SendLocalizedMessage(500467); // You carve some meat, which remains on the corpse.
                }

                corpse.Carved = true;

                if (corpse.IsCriminalAction(from))
                    from.CriminalAction(true);
            }
        }

        public const int DefaultRangePerception = 16;
        public const int OldRangePerception = 10;

        public BaseCreature(AIType ai,
            FightMode mode,
            int iRangePerception,
            int iRangeFight,
            double dActiveSpeed,
            double dPassiveSpeed)
        {
            if (iRangePerception == OldRangePerception)
                iRangePerception = DefaultRangePerception;

            this.m_Loyalty = MaxLoyalty; // Wonderfully Happy

            this.m_CurrentAI = ai;
            this.m_DefaultAI = ai;

            this.m_iRangePerception = iRangePerception;
            this.m_iRangeFight = iRangeFight;

            this.m_FightMode = mode;

            this.m_iTeam = 0;

            SpeedInfo.GetSpeeds(this, ref dActiveSpeed, ref dPassiveSpeed);

            this.m_dActiveSpeed = dActiveSpeed;
            this.m_dPassiveSpeed = dPassiveSpeed;
            this.m_dCurrentSpeed = dPassiveSpeed;

            this.m_bDebugAI = false;

            this.m_arSpellAttack = new List<Type>();
            this.m_arSpellDefense = new List<Type>();

            this.m_bControlled = false;
            this.m_ControlMaster = null;
            this.m_ControlTarget = null;
            this.m_ControlOrder = OrderType.None;

            this.m_bTamable = false;

            this.m_Owners = new List<Mobile>();

            this.m_NextReacquireTime = DateTime.Now + this.ReacquireDelay;

            this.ChangeAIType(this.AI);

            InhumanSpeech speechType = this.SpeechType;

            if (speechType != null)
                speechType.OnConstruct(this);

            this.GenerateLoot(true);
        }

        public BaseCreature(Serial serial)
            : base(serial)
        {
            this.m_arSpellAttack = new List<Type>();
            this.m_arSpellDefense = new List<Type>();

            this.m_bDebugAI = false;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(19); // version

            writer.Write((int)this.m_CurrentAI);
            writer.Write((int)this.m_DefaultAI);

            writer.Write((int)this.m_iRangePerception);
            writer.Write((int)this.m_iRangeFight);

            writer.Write((int)this.m_iTeam);

            writer.Write((double)this.m_dActiveSpeed);
            writer.Write((double)this.m_dPassiveSpeed);
            writer.Write((double)this.m_dCurrentSpeed);

            writer.Write((int)this.m_pHome.X);
            writer.Write((int)this.m_pHome.Y);
            writer.Write((int)this.m_pHome.Z);

            // Version 1
            writer.Write((int)this.m_iRangeHome);

            int i = 0;

            writer.Write((int)this.m_arSpellAttack.Count);
            for (i = 0; i < this.m_arSpellAttack.Count; i++)
            {
                writer.Write(this.m_arSpellAttack[i].ToString());
            }

            writer.Write((int)this.m_arSpellDefense.Count);
            for (i = 0; i < this.m_arSpellDefense.Count; i++)
            {
                writer.Write(this.m_arSpellDefense[i].ToString());
            }

            // Version 2
            writer.Write((int)this.m_FightMode);

            writer.Write((bool)this.m_bControlled);
            writer.Write((Mobile)this.m_ControlMaster);
            writer.Write((Mobile)this.m_ControlTarget);
            writer.Write((Point3D)this.m_ControlDest);
            writer.Write((int)this.m_ControlOrder);
            writer.Write((double)this.m_dMinTameSkill);
            // Removed in version 9
            //writer.Write( (double) m_dMaxTameSkill );
            writer.Write((bool)this.m_bTamable);
            writer.Write((bool)this.m_bSummoned);

            if (this.m_bSummoned)
                writer.WriteDeltaTime(this.m_SummonEnd);

            writer.Write((int)this.m_iControlSlots);

            // Version 3
            writer.Write((int)this.m_Loyalty);

            // Version 4
            writer.Write(this.m_CurrentWayPoint);

            // Verison 5
            writer.Write(this.m_SummonMaster);

            // Version 6
            writer.Write((int)this.m_HitsMax);
            writer.Write((int)this.m_StamMax);
            writer.Write((int)this.m_ManaMax);
            writer.Write((int)this.m_DamageMin);
            writer.Write((int)this.m_DamageMax);

            // Version 7
            writer.Write((int)this.m_PhysicalResistance);
            writer.Write((int)this.m_PhysicalDamage);

            writer.Write((int)this.m_FireResistance);
            writer.Write((int)this.m_FireDamage);

            writer.Write((int)this.m_ColdResistance);
            writer.Write((int)this.m_ColdDamage);

            writer.Write((int)this.m_PoisonResistance);
            writer.Write((int)this.m_PoisonDamage);

            writer.Write((int)this.m_EnergyResistance);
            writer.Write((int)this.m_EnergyDamage);

            // Version 8
            writer.Write(this.m_Owners, true);

            // Version 10
            writer.Write((bool)this.m_IsDeadPet);
            writer.Write((bool)this.m_IsBonded);
            writer.Write((DateTime)this.m_BondingBegin);
            writer.Write((DateTime)this.m_OwnerAbandonTime);

            // Version 11
            writer.Write((bool)this.m_HasGeneratedLoot);

            // Version 12
            writer.Write((bool)this.m_Paragon);

            // Version 13
            writer.Write((bool)(this.m_Friends != null && this.m_Friends.Count > 0));

            if (this.m_Friends != null && this.m_Friends.Count > 0)
                writer.Write(this.m_Friends, true);

            // Version 14
            writer.Write((bool)this.m_RemoveIfUntamed);
            writer.Write((int)this.m_RemoveStep);

            // Version 17
            if (this.IsStabled || (this.Controlled && this.ControlMaster != null))
                writer.Write(TimeSpan.Zero);
            else
                writer.Write(this.DeleteTimeLeft);

            // Version 18
            writer.Write(this.m_CorpseNameOverride);

            // Mondain's Legacy version 19
            writer.Write((bool)this.m_Allured);
        }

        private static double[] m_StandardActiveSpeeds = new double[]
        {
            0.175, 0.1, 0.15, 0.2, 0.25, 0.3, 0.4, 0.5, 0.6, 0.8
        };

        private static double[] m_StandardPassiveSpeeds = new double[]
        {
            0.350, 0.2, 0.4, 0.5, 0.6, 0.8, 1.0, 1.2, 1.6, 2.0
        };

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            this.m_CurrentAI = (AIType)reader.ReadInt();
            this.m_DefaultAI = (AIType)reader.ReadInt();

            this.m_iRangePerception = reader.ReadInt();
            this.m_iRangeFight = reader.ReadInt();

            this.m_iTeam = reader.ReadInt();

            this.m_dActiveSpeed = reader.ReadDouble();
            this.m_dPassiveSpeed = reader.ReadDouble();
            this.m_dCurrentSpeed = reader.ReadDouble();

            if (this.m_iRangePerception == OldRangePerception)
                this.m_iRangePerception = DefaultRangePerception;

            this.m_pHome.X = reader.ReadInt();
            this.m_pHome.Y = reader.ReadInt();
            this.m_pHome.Z = reader.ReadInt();

            if (version >= 1)
            {
                this.m_iRangeHome = reader.ReadInt();

                int i, iCount;

                iCount = reader.ReadInt();
                for (i = 0; i < iCount; i++)
                {
                    string str = reader.ReadString();
                    Type type = Type.GetType(str);

                    if (type != null)
                    {
                        this.m_arSpellAttack.Add(type);
                    }
                }

                iCount = reader.ReadInt();
                for (i = 0; i < iCount; i++)
                {
                    string str = reader.ReadString();
                    Type type = Type.GetType(str);

                    if (type != null)
                    {
                        this.m_arSpellDefense.Add(type);
                    }
                }
            }
            else
            {
                this.m_iRangeHome = 0;
            }

            if (version >= 2)
            {
                this.m_FightMode = (FightMode)reader.ReadInt();

                this.m_bControlled = reader.ReadBool();
                this.m_ControlMaster = reader.ReadMobile();
                this.m_ControlTarget = reader.ReadMobile();
                this.m_ControlDest = reader.ReadPoint3D();
                this.m_ControlOrder = (OrderType)reader.ReadInt();

                this.m_dMinTameSkill = reader.ReadDouble();

                if (version < 9)
                    reader.ReadDouble();

                this.m_bTamable = reader.ReadBool();
                this.m_bSummoned = reader.ReadBool();

                if (this.m_bSummoned)
                {
                    this.m_SummonEnd = reader.ReadDeltaTime();
                    new UnsummonTimer(this.m_ControlMaster, this, this.m_SummonEnd - DateTime.Now).Start();
                }

                this.m_iControlSlots = reader.ReadInt();
            }
            else
            {
                this.m_FightMode = FightMode.Closest;

                this.m_bControlled = false;
                this.m_ControlMaster = null;
                this.m_ControlTarget = null;
                this.m_ControlOrder = OrderType.None;
            }

            if (version >= 3)
                this.m_Loyalty = reader.ReadInt();
            else
                this.m_Loyalty = MaxLoyalty; // Wonderfully Happy

            if (version >= 4)
                this.m_CurrentWayPoint = reader.ReadItem() as WayPoint;

            if (version >= 5)
                this.m_SummonMaster = reader.ReadMobile();

            if (version >= 6)
            {
                this.m_HitsMax = reader.ReadInt();
                this.m_StamMax = reader.ReadInt();
                this.m_ManaMax = reader.ReadInt();
                this.m_DamageMin = reader.ReadInt();
                this.m_DamageMax = reader.ReadInt();
            }

            if (version >= 7)
            {
                this.m_PhysicalResistance = reader.ReadInt();
                this.m_PhysicalDamage = reader.ReadInt();

                this.m_FireResistance = reader.ReadInt();
                this.m_FireDamage = reader.ReadInt();

                this.m_ColdResistance = reader.ReadInt();
                this.m_ColdDamage = reader.ReadInt();

                this.m_PoisonResistance = reader.ReadInt();
                this.m_PoisonDamage = reader.ReadInt();

                this.m_EnergyResistance = reader.ReadInt();
                this.m_EnergyDamage = reader.ReadInt();
            }

            if (version >= 8)
                this.m_Owners = reader.ReadStrongMobileList();
            else
                this.m_Owners = new List<Mobile>();

            if (version >= 10)
            {
                this.m_IsDeadPet = reader.ReadBool();
                this.m_IsBonded = reader.ReadBool();
                this.m_BondingBegin = reader.ReadDateTime();
                this.m_OwnerAbandonTime = reader.ReadDateTime();
            }

            if (version >= 11)
                this.m_HasGeneratedLoot = reader.ReadBool();
            else
                this.m_HasGeneratedLoot = true;

            if (version >= 12)
                this.m_Paragon = reader.ReadBool();
            else
                this.m_Paragon = false;

            if (version >= 13 && reader.ReadBool())
                this.m_Friends = reader.ReadStrongMobileList();
            else if (version < 13 && this.m_ControlOrder >= OrderType.Unfriend)
                ++this.m_ControlOrder;

            if (version < 16 && this.Loyalty != MaxLoyalty)
                this.Loyalty *= 10;

            double activeSpeed = this.m_dActiveSpeed;
            double passiveSpeed = this.m_dPassiveSpeed;

            SpeedInfo.GetSpeeds(this, ref activeSpeed, ref passiveSpeed);

            bool isStandardActive = false;
            for (int i = 0; !isStandardActive && i < m_StandardActiveSpeeds.Length; ++i)
                isStandardActive = (this.m_dActiveSpeed == m_StandardActiveSpeeds[i]);

            bool isStandardPassive = false;
            for (int i = 0; !isStandardPassive && i < m_StandardPassiveSpeeds.Length; ++i)
                isStandardPassive = (this.m_dPassiveSpeed == m_StandardPassiveSpeeds[i]);

            if (isStandardActive && this.m_dCurrentSpeed == this.m_dActiveSpeed)
                this.m_dCurrentSpeed = activeSpeed;
            else if (isStandardPassive && this.m_dCurrentSpeed == this.m_dPassiveSpeed)
                this.m_dCurrentSpeed = passiveSpeed;

            if (isStandardActive && !this.m_Paragon)
                this.m_dActiveSpeed = activeSpeed;

            if (isStandardPassive && !this.m_Paragon)
                this.m_dPassiveSpeed = passiveSpeed;

            if (version >= 14)
            {
                this.m_RemoveIfUntamed = reader.ReadBool();
                this.m_RemoveStep = reader.ReadInt();
            }

            TimeSpan deleteTime = TimeSpan.Zero;

            if (version >= 17)
                deleteTime = reader.ReadTimeSpan();

            if (deleteTime > TimeSpan.Zero || this.LastOwner != null && !this.Controlled && !this.IsStabled)
            {
                if (deleteTime <= TimeSpan.Zero || deleteTime > TimeSpan.FromDays(3.0))
                    deleteTime = TimeSpan.FromDays(3.0);

                this.m_DeleteTimer = new DeleteTimer(this, deleteTime);
                this.m_DeleteTimer.Start();
            }

            if (version >= 18)
                this.m_CorpseNameOverride = reader.ReadString();

            if (version >= 19)
                this.m_Allured = reader.ReadBool();

            if (version <= 14 && this.m_Paragon && this.Hue == 0x31)
            {
                this.Hue = Paragon.Hue; //Paragon hue fixed, should now be 0x501.
            }

            this.CheckStatTimers();

            this.ChangeAIType(this.m_CurrentAI);

            this.AddFollowers();

            if (this.IsAnimatedDead)
                Spells.Necromancy.AnimateDeadSpell.Register(this.m_SummonMaster, this);
        }

        public virtual bool IsHumanInTown()
        {
            return (this.Body.IsHuman && this.Region.IsPartOf(typeof(Regions.GuardedRegion)));
        }

        public virtual bool CheckGold(Mobile from, Item dropped)
        {
            if (dropped is Gold)
                return this.OnGoldGiven(from, (Gold)dropped);

            return false;
        }

        public virtual bool OnGoldGiven(Mobile from, Gold dropped)
        {
            if (this.CheckTeachingMatch(from))
            {
                if (this.Teach(this.m_Teaching, from, dropped.Amount, true))
                {
                    dropped.Delete();
                    return true;
                }
            }
            else if (this.IsHumanInTown())
            {
                this.Direction = this.GetDirectionTo(from);

                int oldSpeechHue = this.SpeechHue;

                this.SpeechHue = 0x23F;
                this.SayTo(from, "Thou art giving me gold?");

                if (dropped.Amount >= 400)
                    this.SayTo(from, "'Tis a noble gift.");
                else
                    this.SayTo(from, "Money is always welcome.");

                this.SpeechHue = 0x3B2;
                this.SayTo(from, 501548); // I thank thee.

                this.SpeechHue = oldSpeechHue;

                dropped.Delete();
                return true;
            }

            return false;
        }

        public override bool ShouldCheckStatTimers
        {
            get
            {
                return false;
            }
        }

        #region Food
        private static Type[] m_Eggs = new Type[]
        {
            typeof(FriedEggs), typeof(Eggs)
        };

        private static Type[] m_Fish = new Type[]
        {
            typeof(FishSteak), typeof(RawFishSteak)
        };

        private static Type[] m_GrainsAndHay = new Type[]
        {
            typeof(BreadLoaf), typeof(FrenchBread), typeof(SheafOfHay)
        };

        private static Type[] m_Meat = new Type[]
        {
            /* Cooked */
            typeof(Bacon), typeof(CookedBird), typeof(Sausage),
            typeof(Ham), typeof(Ribs), typeof(LambLeg),
            typeof(ChickenLeg),

            /* Uncooked */
            typeof(RawBird), typeof(RawRibs), typeof(RawLambLeg),
            typeof(RawChickenLeg),

            /* Body Parts */
            typeof(Head), typeof(LeftArm), typeof(LeftLeg),
            typeof(Torso), typeof(RightArm), typeof(RightLeg)
        };

        private static Type[] m_FruitsAndVegies = new Type[]
        {
            typeof(HoneydewMelon), typeof(YellowGourd), typeof(GreenGourd),
            typeof(Banana), typeof(Bananas), typeof(Lemon), typeof(Lime),
            typeof(Dates), typeof(Grapes), typeof(Peach), typeof(Pear),
            typeof(Apple), typeof(Watermelon), typeof(Squash),
            typeof(Cantaloupe), typeof(Carrot), typeof(Cabbage),
            typeof(Onion), typeof(Lettuce), typeof(Pumpkin)
        };

        private static Type[] m_Gold = new Type[]
        {
            // white wyrms eat gold..
            typeof(Gold)
        };

        private static Type[] m_Metal = new Type[]
        {
            // Some Stygian Abyss Monsters eat Metal..
            typeof(IronIngot), typeof(DullCopperIngot), typeof(ShadowIronIngot),
            typeof(CopperIngot), typeof(BronzeIngot), typeof(GoldIngot),
            typeof(AgapiteIngot), typeof(VeriteIngot), typeof(ValoriteIngot)
        };

        public virtual bool CheckFoodPreference(Item f)
        {
            if (this.CheckFoodPreference(f, FoodType.Eggs, m_Eggs))
                return true;

            if (this.CheckFoodPreference(f, FoodType.Fish, m_Fish))
                return true;

            if (this.CheckFoodPreference(f, FoodType.GrainsAndHay, m_GrainsAndHay))
                return true;

            if (this.CheckFoodPreference(f, FoodType.Meat, m_Meat))
                return true;

            if (this.CheckFoodPreference(f, FoodType.FruitsAndVegies, m_FruitsAndVegies))
                return true;

            if (this.CheckFoodPreference(f, FoodType.Metal, m_Metal))
                return true;

            return false;
        }

        public virtual bool CheckFoodPreference(Item fed, FoodType type, Type[] types)
        {
            if ((this.FavoriteFood & type) == 0)
                return false;

            Type fedType = fed.GetType();
            bool contains = false;

            for (int i = 0; !contains && i < types.Length; ++i)
                contains = (fedType == types[i]);

            return contains;
        }

        public virtual bool CheckFeed(Mobile from, Item dropped)
        {
            if (!this.IsDeadPet && this.Controlled && (this.ControlMaster == from || this.IsPetFriend(from)) && (dropped is Food || dropped is Gold || dropped is CookableFood || dropped is Head || dropped is LeftArm || dropped is LeftLeg || dropped is Torso || dropped is RightArm || dropped is RightLeg ||
                                                                                                                 dropped is IronIngot || dropped is DullCopperIngot || dropped is ShadowIronIngot || dropped is CopperIngot || dropped is BronzeIngot || dropped is GoldIngot || dropped is AgapiteIngot || dropped is VeriteIngot || dropped is ValoriteIngot))
            {
                Item f = dropped;

                if (this.CheckFoodPreference(f))
                {
                    int amount = f.Amount;

                    if (amount > 0)
                    {
                        bool happier = false;

                        int stamGain;

                        if (f is Gold)
                            stamGain = amount - 50;
                        else
                            stamGain = (amount * 15) - 50;

                        if (stamGain > 0)
                            this.Stam += stamGain;

                        if (Core.SE)
                        {
                            if (this.m_Loyalty < MaxLoyalty)
                            {
                                this.m_Loyalty = MaxLoyalty;
                                happier = true;
                            }
                        }
                        else
                        {
                            for (int i = 0; i < amount; ++i)
                            {
                                if (this.m_Loyalty < MaxLoyalty && 0.5 >= Utility.RandomDouble())
                                {
                                    this.m_Loyalty += 10;
                                    happier = true;
                                }
                            }
                        }

                        if (happier)
                            this.SayTo(from, 502060); // Your pet looks happier.

                        if (this.Body.IsAnimal)
                            this.Animate(3, 5, 1, true, false, 0);
                        else if (this.Body.IsMonster)
                            this.Animate(17, 5, 1, true, false, 0);

                        if (this.IsBondable && !this.IsBonded)
                        {
                            Mobile master = this.m_ControlMaster;

                            if (master != null && master == from)	//So friends can't start the bonding process
                            {
                                if (this.m_dMinTameSkill <= 29.1 || master.Skills[SkillName.AnimalTaming].Base >= this.m_dMinTameSkill || this.OverrideBondingReqs() || (Core.ML && master.Skills[SkillName.AnimalTaming].Value >= this.m_dMinTameSkill))
                                {
                                    if (this.BondingBegin == DateTime.MinValue)
                                    {
                                        this.BondingBegin = DateTime.Now;
                                    }
                                    else if ((this.BondingBegin + this.BondingDelay) <= DateTime.Now)
                                    {
                                        this.IsBonded = true;
                                        this.BondingBegin = DateTime.MinValue;
                                        from.SendLocalizedMessage(1049666); // Your pet has bonded with you!
                                    }
                                }
                                else if (Core.ML)
                                {
                                    from.SendLocalizedMessage(1075268); // Your pet cannot form a bond with you until your animal taming ability has risen.
                                }
                            }
                        }

                        dropped.Delete();
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion

        public virtual bool OverrideBondingReqs()
        {
            return false;
        }

        public virtual bool CanAngerOnTame
        {
            get
            {
                return false;
            }
        }

        #region OnAction[...]

        public virtual void OnActionWander()
        {
        }

        public virtual void OnActionCombat()
        {
        }

        public virtual void OnActionGuard()
        {
        }

        public virtual void OnActionFlee()
        {
        }

        public virtual void OnActionInteract()
        {
        }

        public virtual void OnActionBackoff()
        {
        }

        #endregion

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            if (this.CheckFeed(from, dropped))
                return true;
            else if (this.CheckGold(from, dropped))
                return true;

            return base.OnDragDrop(from, dropped);
        }

        protected virtual BaseAI ForcedAI
        {
            get
            {
                return null;
            }
        }

        public void ChangeAIType(AIType NewAI)
        {
            if (this.m_AI != null)
                this.m_AI.m_Timer.Stop();

            if (this.ForcedAI != null)
            {
                this.m_AI = this.ForcedAI;
                return;
            }

            this.m_AI = null;

            switch ( NewAI )
            {
                case AIType.AI_Melee:
                    this.m_AI = new MeleeAI(this);
                    break;
                case AIType.AI_Animal:
                    this.m_AI = new AnimalAI(this);
                    break;
                case AIType.AI_Berserk:
                    this.m_AI = new BerserkAI(this);
                    break;
                case AIType.AI_Archer:
                    this.m_AI = new ArcherAI(this);
                    break;
                case AIType.AI_Healer:
                    this.m_AI = new HealerAI(this);
                    break;
                case AIType.AI_Vendor:
                    this.m_AI = new VendorAI(this);
                    break;
                case AIType.AI_Mage:
                    this.m_AI = new MageAI(this);
                    break;
                case AIType.AI_Predator:
                    //m_AI = new PredatorAI(this);
                    this.m_AI = new MeleeAI(this);
                    break;
                case AIType.AI_Thief:
                    this.m_AI = new ThiefAI(this);
                    break;
                case AIType.AI_NecroMage:
                    this.m_AI = new NecroMageAI(this);
                    break;
                case AIType.AI_OrcScout:
                    this.m_AI = new OrcScoutAI(this);
                    break;
                case AIType.AI_Spellbinder:
                    this.m_AI = new SpellbinderAI(this);
                    break;
            }
        }

        public void ChangeAIToDefault()
        {
            this.ChangeAIType(this.m_DefaultAI);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public AIType AI
        {
            get
            {
                return this.m_CurrentAI;
            }
            set
            {
                this.m_CurrentAI = value;

                if (this.m_CurrentAI == AIType.AI_Use_Default)
                {
                    this.m_CurrentAI = this.m_DefaultAI;
                }

                this.ChangeAIType(this.m_CurrentAI);
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool Debug
        {
            get
            {
                return this.m_bDebugAI;
            }
            set
            {
                this.m_bDebugAI = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Team
        {
            get
            {
                return this.m_iTeam;
            }
            set
            {
                this.m_iTeam = value;

                this.OnTeamChange();
            }
        }

        public virtual void OnTeamChange()
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile FocusMob
        {
            get
            {
                return this.m_FocusMob;
            }
            set
            {
                this.m_FocusMob = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public FightMode FightMode
        {
            get
            {
                return this.m_FightMode;
            }
            set
            {
                this.m_FightMode = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int RangePerception
        {
            get
            {
                return this.m_iRangePerception;
            }
            set
            {
                this.m_iRangePerception = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int RangeFight
        {
            get
            {
                return this.m_iRangeFight;
            }
            set
            {
                this.m_iRangeFight = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int RangeHome
        {
            get
            {
                return this.m_iRangeHome;
            }
            set
            {
                this.m_iRangeHome = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double ActiveSpeed
        {
            get
            {
                return this.m_dActiveSpeed;
            }
            set
            {
                this.m_dActiveSpeed = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double PassiveSpeed
        {
            get
            {
                return this.m_dPassiveSpeed;
            }
            set
            {
                this.m_dPassiveSpeed = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double CurrentSpeed
        {
            get
            {
                if (this.m_TargetLocation != null)
                    return 0.3;

                return this.m_dCurrentSpeed;
            }
            set
            {
                if (this.m_dCurrentSpeed != value)
                {
                    this.m_dCurrentSpeed = value;

                    if (this.m_AI != null)
                        this.m_AI.OnCurrentSpeedChanged();
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D Home
        {
            get
            {
                return this.m_pHome;
            }
            set
            {
                this.m_pHome = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Controlled
        {
            get
            {
                return this.m_bControlled;
            }
            set
            {
                if (this.m_bControlled == value)
                    return;

                this.m_bControlled = value;
                this.Delta(MobileDelta.Noto);

                this.InvalidateProperties();
            }
        }

        public override void RevealingAction()
        {
            Spells.Sixth.InvisibilitySpell.RemoveTimer(this);

            base.RevealingAction();
        }

        public void RemoveFollowers()
        {
            if (this.m_ControlMaster != null)
            {
                this.m_ControlMaster.Followers -= this.ControlSlots;
                if (this.m_ControlMaster is PlayerMobile)
                {
                    ((PlayerMobile)this.m_ControlMaster).AllFollowers.Remove(this);
                    if (((PlayerMobile)this.m_ControlMaster).AutoStabled.Contains(this))
                        ((PlayerMobile)this.m_ControlMaster).AutoStabled.Remove(this);
                }
            }
            else if (this.m_SummonMaster != null)
            {
                this.m_SummonMaster.Followers -= this.ControlSlots;
                if (this.m_SummonMaster is PlayerMobile)
                {
                    ((PlayerMobile)this.m_SummonMaster).AllFollowers.Remove(this);
                }
            }

            if (this.m_ControlMaster != null && this.m_ControlMaster.Followers < 0)
                this.m_ControlMaster.Followers = 0;

            if (this.m_SummonMaster != null && this.m_SummonMaster.Followers < 0)
                this.m_SummonMaster.Followers = 0;
        }

        public void AddFollowers()
        {
            if (this.m_ControlMaster != null)
            {
                this.m_ControlMaster.Followers += this.ControlSlots;
                if (this.m_ControlMaster is PlayerMobile)
                {
                    ((PlayerMobile)this.m_ControlMaster).AllFollowers.Add(this);
                }
            }
            else if (this.m_SummonMaster != null)
            {
                this.m_SummonMaster.Followers += this.ControlSlots;
                if (this.m_SummonMaster is PlayerMobile)
                {
                    ((PlayerMobile)this.m_SummonMaster).AllFollowers.Add(this);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile ControlMaster
        {
            get
            {
                return this.m_ControlMaster;
            }
            set
            {
                if (this.m_ControlMaster == value || this == value)
                    return;

                this.RemoveFollowers();
                this.m_ControlMaster = value;
                this.AddFollowers();
                if (this.m_ControlMaster != null)
                    this.StopDeleteTimer();

                this.Delta(MobileDelta.Noto);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile SummonMaster
        {
            get
            {
                return this.m_SummonMaster;
            }
            set
            {
                if (this.m_SummonMaster == value || this == value)
                    return;

                this.RemoveFollowers();
                this.m_SummonMaster = value;
                this.AddFollowers();

                this.Delta(MobileDelta.Noto);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile ControlTarget
        {
            get
            {
                return this.m_ControlTarget;
            }
            set
            {
                this.m_ControlTarget = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D ControlDest
        {
            get
            {
                return this.m_ControlDest;
            }
            set
            {
                this.m_ControlDest = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public OrderType ControlOrder
        {
            get
            {
                return this.m_ControlOrder;
            }
            set
            {
                this.m_ControlOrder = value;

                if (this.m_Allured)
                {
                    if (this.m_ControlOrder == OrderType.Release)
                        this.Say(502003); // Sorry, but no.
                    else if (this.m_ControlOrder != OrderType.None)
                        this.Say(1079120); // Very well.
                }

                if (this.m_AI != null)
                    this.m_AI.OnCurrentOrderChanged();

                this.InvalidateProperties();

                if (this.m_ControlMaster != null)
                    this.m_ControlMaster.InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool BardProvoked
        {
            get
            {
                return this.m_bBardProvoked;
            }
            set
            {
                this.m_bBardProvoked = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool BardPacified
        {
            get
            {
                return this.m_bBardPacified;
            }
            set
            {
                this.m_bBardPacified = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile BardMaster
        {
            get
            {
                return this.m_bBardMaster;
            }
            set
            {
                this.m_bBardMaster = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile BardTarget
        {
            get
            {
                return this.m_bBardTarget;
            }
            set
            {
                this.m_bBardTarget = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime BardEndTime
        {
            get
            {
                return this.m_timeBardEnd;
            }
            set
            {
                this.m_timeBardEnd = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double MinTameSkill
        {
            get
            {
                return this.m_dMinTameSkill;
            }
            set
            {
                this.m_dMinTameSkill = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Tamable
        {
            get
            {
                return this.m_bTamable && !this.m_Paragon;
            }
            set
            {
                this.m_bTamable = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool Summoned
        {
            get
            {
                return this.m_bSummoned;
            }
            set
            {
                if (this.m_bSummoned == value)
                    return;

                this.m_NextReacquireTime = DateTime.Now;

                this.m_bSummoned = value;
                this.Delta(MobileDelta.Noto);

                this.InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public int ControlSlots
        {
            get
            {
                return this.m_iControlSlots;
            }
            set
            {
                this.m_iControlSlots = value;
            }
        }

        public virtual bool NoHouseRestrictions
        {
            get
            {
                return false;
            }
        }

        public virtual bool IsHouseSummonable
        {
            get
            {
                return false;
            }
        }

        #region Corpse Resources
        public virtual int Feathers
        {
            get
            {
                return 0;
            }
        }

        public virtual int Wool
        {
            get
            {
                return 0;
            }
        }

        public virtual int Fur
        {
            get
            {
                return 0;
            }
        }

        public virtual MeatType MeatType
        {
            get
            {
                return MeatType.Ribs;
            }
        }
        public virtual int Meat
        {
            get
            {
                return 0;
            }
        }

        public virtual int Hides
        {
            get
            {
                return 0;
            }
        }
        public virtual HideType HideType
        {
            get
            {
                return HideType.Regular;
            }
        }

        public virtual int Scales
        {
            get
            {
                return 0;
            }
        }

        public virtual ScaleType ScaleType
        {
            get
            {
                return ScaleType.Red;
            }
        }

        public virtual int DragonBlood
        {
            get
            {
                return 0;
            }
        }
        #endregion

        public virtual bool AutoDispel
        {
            get
            {
                return false;
            }
        }
        public virtual double AutoDispelChance
        {
            get
            {
                return ((Core.SE) ? .10 : 1.0);
            }
        }

        public virtual bool IsScaryToPets
        {
            get
            {
                return false;
            }
        }
        public virtual bool IsScaredOfScaryThings
        {
            get
            {
                return true;
            }
        }

        public virtual bool CanRummageCorpses
        {
            get
            {
                return false;
            }
        }

        public virtual void OnGotMeleeAttack(Mobile attacker)
        {
            if (this.AutoDispel && attacker is BaseCreature && ((BaseCreature)attacker).IsDispellable && this.AutoDispelChance > Utility.RandomDouble())
                this.Dispel(attacker);
        }

        public virtual void Dispel(Mobile m)
        {
            Effects.SendLocationParticles(EffectItem.Create(m.Location, m.Map, EffectItem.DefaultDuration), 0x3728, 8, 20, 5042);
            Effects.PlaySound(m, m.Map, 0x201);

            m.Delete();
        }

        public virtual bool DeleteOnRelease
        {
            get
            {
                return this.m_bSummoned;
            }
        }

        public virtual void OnGaveMeleeAttack(Mobile defender)
        {
            Poison p = this.HitPoison;

            XmlPoison xp = (XmlPoison)XmlAttach.FindAttachment(this, typeof(XmlPoison));
            
            if (xp != null)
            {
                p = xp.HitPoison;
            }

            if (this.m_Paragon)
                p = PoisonImpl.IncreaseLevel(p);

            if (p != null && this.HitPoisonChance >= Utility.RandomDouble())
            {
                defender.ApplyPoison(this, p);

                if (this.Controlled)
                    this.CheckSkill(SkillName.Poisoning, 0, this.Skills[SkillName.Poisoning].Cap);
            }

            if (this.AutoDispel && defender is BaseCreature && ((BaseCreature)defender).IsDispellable && this.AutoDispelChance > Utility.RandomDouble())
                this.Dispel(defender);
        }

        public override void OnAfterDelete()
        {
            if (this.m_AI != null)
            {
                if (this.m_AI.m_Timer != null)
                    this.m_AI.m_Timer.Stop();

                this.m_AI = null;
            }

            if (this.m_DeleteTimer != null)
            {
                this.m_DeleteTimer.Stop();
                this.m_DeleteTimer = null;
            }

            this.FocusMob = null;

            if (this.IsAnimatedDead)
                Spells.Necromancy.AnimateDeadSpell.Unregister(this.m_SummonMaster, this);

            base.OnAfterDelete();
        }

        public void DebugSay(string text)
        {
            if (this.m_bDebugAI)
                this.PublicOverheadMessage(MessageType.Regular, 41, false, text);
        }

        public void DebugSay(string format, params object[] args)
        {
            if (this.m_bDebugAI)
                this.PublicOverheadMessage(MessageType.Regular, 41, false, String.Format(format, args));
        }

        /*
        * This function can be overriden.. so a "Strongest" mobile, can have a different definition depending
        * on who check for value
        * -Could add a FightMode.Prefered
        *
        */

        public virtual double GetFightModeRanking(Mobile m, FightMode acqType, bool bPlayerOnly)
        {
            if ((bPlayerOnly && m.Player) || !bPlayerOnly)
            {
                switch( acqType )
                {
                    case FightMode.Strongest :
                        return (m.Skills[SkillName.Tactics].Value + m.Str); //returns strongest mobile

                    case FightMode.Weakest :
                        return -m.Hits; // returns weakest mobile

                    default :
                        return -this.GetDistanceToSqrt(m); // returns closest mobile
                }
            }
            else
            {
                return double.MinValue;
            }
        }

        // Turn, - for left, + for right
        // Basic for now, needs work
        public virtual void Turn(int iTurnSteps)
        {
            int v = (int)this.Direction;

            this.Direction = (Direction)((((v & 0x7) + iTurnSteps) & 0x7) | (v & 0x80));
        }

        public virtual void TurnInternal(int iTurnSteps)
        {
            int v = (int)this.Direction;

            this.SetDirection((Direction)((((v & 0x7) + iTurnSteps) & 0x7) | (v & 0x80)));
        }

        public bool IsHurt()
        {
            return (this.Hits != this.HitsMax);
        }

        public double GetHomeDistance()
        {
            return this.GetDistanceToSqrt(this.m_pHome);
        }

        public virtual int GetTeamSize(int iRange)
        {
            int iCount = 0;

            foreach (Mobile m in this.GetMobilesInRange(iRange))
            {
                if (m is BaseCreature)
                {
                    if (((BaseCreature)m).Team == this.Team)
                    {
                        if (!m.Deleted)
                        {
                            if (m != this)
                            {
                                if (this.CanSee(m))
                                {
                                    iCount++;
                                }
                            }
                        }
                    }
                }
            }

            return iCount;
        }

        private class TameEntry : ContextMenuEntry
        {
            private BaseCreature m_Mobile;

            public TameEntry(Mobile from, BaseCreature creature)
                : base(6130, 6)
            {
                this.m_Mobile = creature;

                this.Enabled = this.Enabled && (from.Female ? creature.AllowFemaleTamer : creature.AllowMaleTamer);
            }

            public override void OnClick()
            {
                if (!this.Owner.From.CheckAlive())
                    return;

                this.Owner.From.TargetLocked = true;
                SkillHandlers.AnimalTaming.DisableMessage = true;

                if (this.Owner.From.UseSkill(SkillName.AnimalTaming))
                    this.Owner.From.Target.Invoke(this.Owner.From, this.m_Mobile);

                SkillHandlers.AnimalTaming.DisableMessage = false;
                this.Owner.From.TargetLocked = false;
            }
        }

        #region Teaching
        public virtual bool CanTeach
        {
            get
            {
                return false;
            }
        }

        public virtual bool CheckTeach(SkillName skill, Mobile from)
        {
            if (!this.CanTeach)
                return false;

            if (skill == SkillName.Stealth && from.Skills[SkillName.Hiding].Base < Stealth.HidingRequirement)
                return false;

            if (skill == SkillName.RemoveTrap && (from.Skills[SkillName.Lockpicking].Base < 50.0 || from.Skills[SkillName.DetectHidden].Base < 50.0))
                return false;

            if (!Core.AOS && (skill == SkillName.Focus || skill == SkillName.Chivalry || skill == SkillName.Necromancy))
                return false;

            return true;
        }

        public enum TeachResult
        {
            Success,
            Failure,
            KnowsMoreThanMe,
            KnowsWhatIKnow,
            SkillNotRaisable,
            NotEnoughFreePoints
        }

        public virtual TeachResult CheckTeachSkills(SkillName skill, Mobile m, int maxPointsToLearn, ref int pointsToLearn, bool doTeach)
        {
            if (!this.CheckTeach(skill, m) || !m.CheckAlive())
                return TeachResult.Failure;

            Skill ourSkill = this.Skills[skill];
            Skill theirSkill = m.Skills[skill];

            if (ourSkill == null || theirSkill == null)
                return TeachResult.Failure;

            int baseToSet = ourSkill.BaseFixedPoint / 3;

            if (baseToSet > 420)
                baseToSet = 420;
            else if (baseToSet < 200)
                return TeachResult.Failure;

            if (baseToSet > theirSkill.CapFixedPoint)
                baseToSet = theirSkill.CapFixedPoint;

            pointsToLearn = baseToSet - theirSkill.BaseFixedPoint;

            if (maxPointsToLearn > 0 && pointsToLearn > maxPointsToLearn)
            {
                pointsToLearn = maxPointsToLearn;
                baseToSet = theirSkill.BaseFixedPoint + pointsToLearn;
            }

            if (pointsToLearn < 0)
                return TeachResult.KnowsMoreThanMe;

            if (pointsToLearn == 0)
                return TeachResult.KnowsWhatIKnow;

            if (theirSkill.Lock != SkillLock.Up)
                return TeachResult.SkillNotRaisable;

            int freePoints = m.Skills.Cap - m.Skills.Total;
            int freeablePoints = 0;

            if (freePoints < 0)
                freePoints = 0;

            for (int i = 0; (freePoints + freeablePoints) < pointsToLearn && i < m.Skills.Length; ++i)
            {
                Skill sk = m.Skills[i];

                if (sk == theirSkill || sk.Lock != SkillLock.Down)
                    continue;

                freeablePoints += sk.BaseFixedPoint;
            }

            if ((freePoints + freeablePoints) == 0)
                return TeachResult.NotEnoughFreePoints;

            if ((freePoints + freeablePoints) < pointsToLearn)
            {
                pointsToLearn = freePoints + freeablePoints;
                baseToSet = theirSkill.BaseFixedPoint + pointsToLearn;
            }

            if (doTeach)
            {
                int need = pointsToLearn - freePoints;

                for (int i = 0; need > 0 && i < m.Skills.Length; ++i)
                {
                    Skill sk = m.Skills[i];

                    if (sk == theirSkill || sk.Lock != SkillLock.Down)
                        continue;

                    if (sk.BaseFixedPoint < need)
                    {
                        need -= sk.BaseFixedPoint;
                        sk.BaseFixedPoint = 0;
                    }
                    else
                    {
                        sk.BaseFixedPoint -= need;
                        need = 0;
                    }
                }

                /* Sanity check */
                if (baseToSet > theirSkill.CapFixedPoint || (m.Skills.Total - theirSkill.BaseFixedPoint + baseToSet) > m.Skills.Cap)
                    return TeachResult.NotEnoughFreePoints;

                theirSkill.BaseFixedPoint = baseToSet;
            }

            return TeachResult.Success;
        }

        public virtual bool CheckTeachingMatch(Mobile m)
        {
            if (this.m_Teaching == (SkillName)(-1))
                return false;

            if (m is PlayerMobile)
                return (((PlayerMobile)m).Learning == this.m_Teaching);

            return true;
        }

        private SkillName m_Teaching = (SkillName)(-1);

        public virtual bool Teach(SkillName skill, Mobile m, int maxPointsToLearn, bool doTeach)
        {
            int pointsToLearn = 0;
            TeachResult res = this.CheckTeachSkills(skill, m, maxPointsToLearn, ref pointsToLearn, doTeach);

            switch ( res )
            {
                case TeachResult.KnowsMoreThanMe:
                    {
                        this.Say(501508); // I cannot teach thee, for thou knowest more than I!
                        break;
                    }
                case TeachResult.KnowsWhatIKnow:
                    {
                        this.Say(501509); // I cannot teach thee, for thou knowest all I can teach!
                        break;
                    }
                case TeachResult.NotEnoughFreePoints:
                case TeachResult.SkillNotRaisable:
                    {
                        // Make sure this skill is marked to raise. If you are near the skill cap (700 points) you may need to lose some points in another skill first.
                        m.SendLocalizedMessage(501510, "", 0x22);
                        break;
                    }
                case TeachResult.Success:
                    {
                        if (doTeach)
                        {
                            this.Say(501539); // Let me show thee something of how this is done.
                            m.SendLocalizedMessage(501540); // Your skill level increases.

                            this.m_Teaching = (SkillName)(-1);

                            if (m is PlayerMobile)
                                ((PlayerMobile)m).Learning = (SkillName)(-1);
                        }
                        else
                        {
                            // I will teach thee all I know, if paid the amount in full.  The price is:
                            this.Say(1019077, AffixType.Append, String.Format(" {0}", pointsToLearn), "");
                            this.Say(1043108); // For less I shall teach thee less.

                            this.m_Teaching = skill;

                            if (m is PlayerMobile)
                                ((PlayerMobile)m).Learning = skill;
                        }

                        return true;
                    }
            }

            return false;
        }

        #endregion

        public override void AggressiveAction(Mobile aggressor, bool criminal)
        {
            base.AggressiveAction(aggressor, criminal);

            if (this.ControlMaster != null)
                if (NotorietyHandlers.CheckAggressor(this.ControlMaster.Aggressors, aggressor))
                    aggressor.Aggressors.Add(AggressorInfo.Create(this, aggressor, true));

            OrderType ct = this.m_ControlOrder;

            if (this.m_AI != null)
            {
                if (!Core.ML || (ct != OrderType.Follow && ct != OrderType.Stop && ct != OrderType.Stay))
                {
                    this.m_AI.OnAggressiveAction(aggressor);
                }
                else
                {
                    this.DebugSay("I'm being attacked but my master told me not to fight.");
                    this.Warmode = false;
                    return;
                }
            }

            this.StopFlee();

            this.ForceReacquire();

            if (!this.IsEnemy(aggressor))
            {
                Ethics.Player pl = Ethics.Player.Find(aggressor, true);

                if (pl != null && pl.IsShielded)
                    pl.FinishShield();
            }

            if (aggressor.ChangingCombatant && (this.m_bControlled || this.m_bSummoned) && (ct == OrderType.Come || (!Core.ML && ct == OrderType.Stay) || ct == OrderType.Stop || ct == OrderType.None || ct == OrderType.Follow))
            {
                this.ControlTarget = aggressor;
                this.ControlOrder = OrderType.Attack;
            }
            else if (this.Combatant == null && !this.m_bBardPacified)
            {
                this.Warmode = true;
                this.Combatant = aggressor;
            }
        }

        public override bool OnMoveOver(Mobile m)
        {
            if (m is BaseCreature && !((BaseCreature)m).Controlled)
                return (!this.Alive || !m.Alive || this.IsDeadBondedPet || m.IsDeadBondedPet) || (this.Hidden && this.IsStaff());

            return base.OnMoveOver(m);
        }

        public virtual void AddCustomContextEntries(Mobile from, List<ContextMenuEntry> list)
        {
        }

        public virtual bool CanDrop
        {
            get
            {
                return this.IsBonded;
            }
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            if (this.m_AI != null && this.Commandable)
                this.m_AI.GetContextMenuEntries(from, list);

            if (this.m_bTamable && !this.m_bControlled && from.Alive)
                list.Add(new TameEntry(from, this));

            this.AddCustomContextEntries(from, list);

            if (this.CanTeach && from.Alive)
            {
                Skills ourSkills = this.Skills;
                Skills theirSkills = from.Skills;

                for (int i = 0; i < ourSkills.Length && i < theirSkills.Length; ++i)
                {
                    Skill skill = ourSkills[i];
                    Skill theirSkill = theirSkills[i];

                    if (skill != null && theirSkill != null && skill.Base >= 60.0 && this.CheckTeach(skill.SkillName, from))
                    {
                        int toTeach = skill.BaseFixedPoint / 3;

                        if (toTeach > 42.0)
                            toTeach = 42;

                        list.Add(new TeachEntry((SkillName)i, this, from, (toTeach > theirSkill.BaseFixedPoint)));
                    }
                }
            }
        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            InhumanSpeech speechType = this.SpeechType;

            if (speechType != null && (speechType.Flags & IHSFlags.OnSpeech) != 0 && from.InRange(this, 3))
                return true;

            return (this.m_AI != null && this.m_AI.HandlesOnSpeech(from) && from.InRange(this, this.m_iRangePerception));
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            InhumanSpeech speechType = this.SpeechType;

            if (speechType != null && speechType.OnSpeech(this, e.Mobile, e.Speech))
                e.Handled = true;
            else if (!e.Handled && this.m_AI != null && e.Mobile.InRange(this, this.m_iRangePerception))
                this.m_AI.OnSpeech(e);
        }

        public override bool IsHarmfulCriminal(Mobile target)
        {
            if ((this.Controlled && target == this.m_ControlMaster) || (this.Summoned && target == this.m_SummonMaster))
                return false;

            if (target is BaseCreature && ((BaseCreature)target).InitialInnocent && !((BaseCreature)target).Controlled)
                return false;

            if (target is PlayerMobile && ((PlayerMobile)target).PermaFlags.Count > 0)
                return false;

            return base.IsHarmfulCriminal(target);
        }

        public override void CriminalAction(bool message)
        {
            base.CriminalAction(message);

            if (this.Controlled || this.Summoned)
            {
                if (this.m_ControlMaster != null && this.m_ControlMaster.Player)
                    this.m_ControlMaster.CriminalAction(false);
                else if (this.m_SummonMaster != null && this.m_SummonMaster.Player)
                    this.m_SummonMaster.CriminalAction(false);
            }
        }

        public override void DoHarmful(Mobile target, bool indirect)
        {
            base.DoHarmful(target, indirect);

            if (target == this || target == this.m_ControlMaster || target == this.m_SummonMaster || (!this.Controlled && !this.Summoned))
                return;

            List<AggressorInfo> list = this.Aggressors;

            for (int i = 0; i < list.Count; ++i)
            {
                AggressorInfo ai = list[i];

                if (ai.Attacker == target)
                    return;
            }

            list = this.Aggressed;

            for (int i = 0; i < list.Count; ++i)
            {
                AggressorInfo ai = list[i];

                if (ai.Defender == target)
                {
                    if (this.m_ControlMaster != null && this.m_ControlMaster.Player && this.m_ControlMaster.CanBeHarmful(target, false))
                        this.m_ControlMaster.DoHarmful(target, true);
                    else if (this.m_SummonMaster != null && this.m_SummonMaster.Player && this.m_SummonMaster.CanBeHarmful(target, false))
                        this.m_SummonMaster.DoHarmful(target, true);

                    return;
                }
            }
        }

        private static Mobile m_NoDupeGuards;

        public void ReleaseGuardDupeLock()
        {
            m_NoDupeGuards = null;
        }

        public void ReleaseGuardLock()
        {
            this.EndAction(typeof(GuardedRegion));
        }

        private DateTime m_IdleReleaseTime;

        public virtual bool CheckIdle()
        {
            if (this.Combatant != null)
                return false; // in combat.. not idling

            if (this.m_IdleReleaseTime > DateTime.MinValue)
            {
                // idling...
                if (DateTime.Now >= this.m_IdleReleaseTime)
                {
                    this.m_IdleReleaseTime = DateTime.MinValue;
                    return false; // idle is over
                }

                return true; // still idling
            }

            if (95 > Utility.Random(100))
                return false; // not idling, but don't want to enter idle state

            this.m_IdleReleaseTime = DateTime.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(15, 25));

            if (this.Body.IsHuman)
            {
                switch ( Utility.Random(2) )
                {
                    case 0:
                        this.Animate(5, 5, 1, true, true, 1);
                        break;
                    case 1:
                        this.Animate(6, 5, 1, true, false, 1);
                        break;
                }
            }
            else if (this.Body.IsAnimal)
            {
                switch ( Utility.Random(3) )
                {
                    case 0:
                        this.Animate(3, 3, 1, true, false, 1);
                        break;
                    case 1:
                        this.Animate(9, 5, 1, true, false, 1);
                        break;
                    case 2:
                        this.Animate(10, 5, 1, true, false, 1);
                        break;
                }
            }
            else if (this.Body.IsMonster)
            {
                switch ( Utility.Random(2) )
                {
                    case 0:
                        this.Animate(17, 5, 1, true, false, 1);
                        break;
                    case 1:
                        this.Animate(18, 5, 1, true, false, 1);
                        break;
                }
            }

            this.PlaySound(this.GetIdleSound());
            return true; // entered idle state
        }

        private void CheckAIActive()
        {
            Map map = this.Map;

            if (this.PlayerRangeSensitive && this.m_AI != null && map != null && map.GetSector(this.Location).Active)
                this.m_AI.Activate();
        }

        public override void OnCombatantChange()
        {
            base.OnCombatantChange();

            this.Warmode = (this.Combatant != null && !this.Combatant.Deleted && this.Combatant.Alive);

            if (this.CanFly && this.Warmode)
            {
                this.Flying = false;
            }
        }

        protected override void OnMapChange(Map oldMap)
        {
            this.CheckAIActive();

            base.OnMapChange(oldMap);
        }

        protected override void OnLocationChange(Point3D oldLocation)
        {
            this.CheckAIActive();

            base.OnLocationChange(oldLocation);
        }

        public virtual void ForceReacquire()
        {
            this.m_NextReacquireTime = DateTime.MinValue;
        }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            if (this.AcquireOnApproach && FightMode != FightMode.Aggressor)
            {
                if (this.InRange(m.Location, this.AcquireOnApproachRange) && !this.InRange(oldLocation, this.AcquireOnApproachRange))
                {
                    if (this.CanBeHarmful(m) && this.IsEnemy(m))
                    {
                        this.Combatant = this.FocusMob = m;

                        if (this.AIObject != null)
                        {
                            this.AIObject.MoveTo(m, true, 1);
                        }

                        this.DoHarmful(m);
                    }
                }
            }
            else if (this.ReacquireOnMovement)
            {
                this.ForceReacquire();
            }

            InhumanSpeech speechType = this.SpeechType;

            if (speechType != null)
                speechType.OnMovement(this, m, oldLocation);

            /* Begin notice sound */
            if ((!m.Hidden || m.IsPlayer()) && m.Player && this.m_FightMode != FightMode.Aggressor && this.m_FightMode != FightMode.None && this.Combatant == null && !this.Controlled && !this.Summoned)
            {
                // If this creature defends itself but doesn't actively attack (animal) or
                // doesn't fight at all (vendor) then no notice sounds are played..
                // So, players are only notified of aggressive monsters
                // Monsters that are currently fighting are ignored
                // Controlled or summoned creatures are ignored
                if (this.InRange(m.Location, 18) && !this.InRange(oldLocation, 18))
                {
                    if (this.Body.IsMonster)
                        this.Animate(11, 5, 1, true, false, 1);

                    this.PlaySound(this.GetAngerSound());
                }
            }
            /* End notice sound */

            if (m_NoDupeGuards == m)
                return;

            if (!this.Body.IsHuman || this.Kills >= 5 || this.AlwaysMurderer || this.AlwaysAttackable || m.Kills < 5 || !m.InRange(this.Location, 12) || !m.Alive)
                return;

            GuardedRegion guardedRegion = (GuardedRegion)this.Region.GetRegion(typeof(GuardedRegion));

            if (guardedRegion != null)
            {
                if (!guardedRegion.IsDisabled() && guardedRegion.IsGuardCandidate(m) && this.BeginAction(typeof(GuardedRegion)))
                {
                    this.Say(1013037 + Utility.Random(16));
                    guardedRegion.CallGuards(this.Location);

                    Timer.DelayCall(TimeSpan.FromSeconds(5.0), new TimerCallback(ReleaseGuardLock));

                    m_NoDupeGuards = m;
                    Timer.DelayCall(TimeSpan.Zero, new TimerCallback(ReleaseGuardDupeLock));
                }
            }
        }

        public void AddSpellAttack(Type type)
        {
            this.m_arSpellAttack.Add(type);
        }

        public void AddSpellDefense(Type type)
        {
            this.m_arSpellDefense.Add(type);
        }

        public Spell GetAttackSpellRandom()
        {
            if (this.m_arSpellAttack.Count > 0)
            {
                Type type = this.m_arSpellAttack[Utility.Random(this.m_arSpellAttack.Count)];

                object[] args = { this, null };
                return Activator.CreateInstance(type, args) as Spell;
            }
            else
            {
                return null;
            }
        }

        public Spell GetDefenseSpellRandom()
        {
            if (this.m_arSpellDefense.Count > 0)
            {
                Type type = this.m_arSpellDefense[Utility.Random(this.m_arSpellDefense.Count)];

                object[] args = { this, null };
                return Activator.CreateInstance(type, args) as Spell;
            }
            else
            {
                return null;
            }
        }

        public Spell GetSpellSpecific(Type type)
        {
            int i;

            for (i = 0; i < this.m_arSpellAttack.Count; i++)
            {
                if (this.m_arSpellAttack[i] == type)
                {
                    object[] args = { this, null };
                    return Activator.CreateInstance(type, args) as Spell;
                }
            }

            for (i = 0; i < this.m_arSpellDefense.Count; i++)
            {
                if (this.m_arSpellDefense[i] == type)
                {
                    object[] args = { this, null };
                    return Activator.CreateInstance(type, args) as Spell;
                }
            }

            return null;
        }

        #region Set[...]

        public void SetDamage(int val)
        {
            this.m_DamageMin = val;
            this.m_DamageMax = val;
        }

        public void SetDamage(int min, int max)
        {
            this.m_DamageMin = min;
            this.m_DamageMax = max;
        }

        public void SetHits(int val)
        {
            if (val < 1000 && !Core.AOS)
                val = (val * 100) / 60;

            this.m_HitsMax = val;
            this.Hits = this.HitsMax;
        }

        public void SetHits(int min, int max)
        {
            if (min < 1000 && !Core.AOS)
            {
                min = (min * 100) / 60;
                max = (max * 100) / 60;
            }

            this.m_HitsMax = Utility.RandomMinMax(min, max);
            this.Hits = this.HitsMax;
        }

        public void SetStam(int val)
        {
            this.m_StamMax = val;
            this.Stam = this.StamMax;
        }

        public void SetStam(int min, int max)
        {
            this.m_StamMax = Utility.RandomMinMax(min, max);
            this.Stam = this.StamMax;
        }

        public void SetMana(int val)
        {
            this.m_ManaMax = val;
            this.Mana = this.ManaMax;
        }

        public void SetMana(int min, int max)
        {
            this.m_ManaMax = Utility.RandomMinMax(min, max);
            this.Mana = this.ManaMax;
        }

        public void SetStr(int val)
        {
            this.RawStr = val;
            this.Hits = this.HitsMax;
        }

        public void SetStr(int min, int max)
        {
            this.RawStr = Utility.RandomMinMax(min, max);
            this.Hits = this.HitsMax;
        }

        public void SetDex(int val)
        {
            this.RawDex = val;
            this.Stam = this.StamMax;
        }

        public void SetDex(int min, int max)
        {
            this.RawDex = Utility.RandomMinMax(min, max);
            this.Stam = this.StamMax;
        }

        public void SetInt(int val)
        {
            this.RawInt = val;
            this.Mana = this.ManaMax;
        }

        public void SetInt(int min, int max)
        {
            this.RawInt = Utility.RandomMinMax(min, max);
            this.Mana = this.ManaMax;
        }

        public void SetDamageType(ResistanceType type, int min, int max)
        {
            this.SetDamageType(type, Utility.RandomMinMax(min, max));
        }

        public void SetDamageType(ResistanceType type, int val)
        {
            switch ( type )
            {
                case ResistanceType.Physical:
                    this.m_PhysicalDamage = val;
                    break;
                case ResistanceType.Fire:
                    this.m_FireDamage = val;
                    break;
                case ResistanceType.Cold:
                    this.m_ColdDamage = val;
                    break;
                case ResistanceType.Poison:
                    this.m_PoisonDamage = val;
                    break;
                case ResistanceType.Energy:
                    this.m_EnergyDamage = val;
                    break;
            }
        }

        public void SetResistance(ResistanceType type, int min, int max)
        {
            this.SetResistance(type, Utility.RandomMinMax(min, max));
        }

        public void SetResistance(ResistanceType type, int val)
        {
            switch ( type )
            {
                case ResistanceType.Physical:
                    this.m_PhysicalResistance = val;
                    break;
                case ResistanceType.Fire:
                    this.m_FireResistance = val;
                    break;
                case ResistanceType.Cold:
                    this.m_ColdResistance = val;
                    break;
                case ResistanceType.Poison:
                    this.m_PoisonResistance = val;
                    break;
                case ResistanceType.Energy:
                    this.m_EnergyResistance = val;
                    break;
            }

            this.UpdateResistances();
        }

        public void SetSkill(SkillName name, double val)
        {
            this.Skills[name].BaseFixedPoint = (int)(val * 10);

            if (this.Skills[name].Base > this.Skills[name].Cap)
            {
                if (Core.SE)
                    this.SkillsCap += (this.Skills[name].BaseFixedPoint - this.Skills[name].CapFixedPoint);

                this.Skills[name].Cap = this.Skills[name].Base;
            }
        }

        public void SetSkill(SkillName name, double min, double max)
        {
            int minFixed = (int)(min * 10);
            int maxFixed = (int)(max * 10);

            this.Skills[name].BaseFixedPoint = Utility.RandomMinMax(minFixed, maxFixed);

            if (this.Skills[name].Base > this.Skills[name].Cap)
            {
                if (Core.SE)
                    this.SkillsCap += (this.Skills[name].BaseFixedPoint - this.Skills[name].CapFixedPoint);

                this.Skills[name].Cap = this.Skills[name].Base;
            }
        }

        public void SetFameLevel(int level)
        {
            switch ( level )
            {
                case 1:
                    this.Fame = Utility.RandomMinMax(0, 1249);
                    break;
                case 2:
                    this.Fame = Utility.RandomMinMax(1250, 2499);
                    break;
                case 3:
                    this.Fame = Utility.RandomMinMax(2500, 4999);
                    break;
                case 4:
                    this.Fame = Utility.RandomMinMax(5000, 9999);
                    break;
                case 5:
                    this.Fame = Utility.RandomMinMax(10000, 10000);
                    break;
            }
        }

        public void SetKarmaLevel(int level)
        {
            switch ( level )
            {
                case 0:
                    this.Karma = -Utility.RandomMinMax(0, 624);
                    break;
                case 1:
                    this.Karma = -Utility.RandomMinMax(625, 1249);
                    break;
                case 2:
                    this.Karma = -Utility.RandomMinMax(1250, 2499);
                    break;
                case 3:
                    this.Karma = -Utility.RandomMinMax(2500, 4999);
                    break;
                case 4:
                    this.Karma = -Utility.RandomMinMax(5000, 9999);
                    break;
                case 5:
                    this.Karma = -Utility.RandomMinMax(10000, 10000);
                    break;
            }
        }

        #endregion

        public static void Cap(ref int val, int min, int max)
        {
            if (val < min)
                val = min;
            else if (val > max)
                val = max;
        }

        #region Pack & Loot

        #region Mondain's Legacy
        public void PackArcaneScroll(int min, int max)
        {
            this.PackArcaneScroll(Utility.RandomMinMax(min, max));
        }

        public void PackArcaneScroll(int amount)
        {
            for (int i = 0; i < amount; ++i)
                this.PackArcaneScroll();
        }

        public void PackArcaneScroll()
        {
            if (!Core.ML)
                return;

            this.PackItem(Loot.Construct(Loot.ArcanistScrollTypes));
        }

        #endregion

        public void PackPotion()
        {
            this.PackItem(Loot.RandomPotion());
        }

        public void PackArcanceScroll(double chance)
        {
            if (!Core.ML || chance <= Utility.RandomDouble())
                return;

            this.PackItem(Loot.Construct(Loot.ArcanistScrollTypes));
        }

        public void PackNecroScroll(int index)
        {
            if (!Core.AOS || 0.05 <= Utility.RandomDouble())
                return;

            this.PackItem(Loot.Construct(Loot.NecromancyScrollTypes, index));
        }

        public void PackScroll(int minCircle, int maxCircle)
        {
            this.PackScroll(Utility.RandomMinMax(minCircle, maxCircle));
        }

        public void PackScroll(int circle)
        {
            int min = (circle - 1) * 8;

            this.PackItem(Loot.RandomScroll(min, min + 7, SpellbookType.Regular));
        }

        public void PackMagicItems(int minLevel, int maxLevel)
        {
            this.PackMagicItems(minLevel, maxLevel, 0.30, 0.15);
        }

        public void PackMagicItems(int minLevel, int maxLevel, double armorChance, double weaponChance)
        {
            if (!this.PackArmor(minLevel, maxLevel, armorChance))
                this.PackWeapon(minLevel, maxLevel, weaponChance);
        }

        public virtual void DropBackpack()
        {
            if (this.Backpack != null)
            {
                if (this.Backpack.Items.Count > 0)
                {
                    Backpack b = new CreatureBackpack(this.Name);

                    List<Item> list = new List<Item>(this.Backpack.Items);
                    foreach (Item item in list)
                    {
                        b.DropItem(item);
                    }

                    BaseHouse house = BaseHouse.FindHouseAt(this);
                    if (house != null)
                        b.MoveToWorld(house.BanLocation, house.Map);
                    else
                        b.MoveToWorld(this.Location, this.Map);
                }
            }
        }

        protected bool m_Spawning;
        protected int m_KillersLuck;

        public virtual void GenerateLoot(bool spawning)
        {
            this.m_Spawning = spawning;

            if (!spawning)
                this.m_KillersLuck = LootPack.GetLuckChanceForKiller(this);

            this.GenerateLoot();

            if (this.m_Paragon)
            {
                if (this.Fame < 1250)
                    this.AddLoot(LootPack.Meager);
                else if (this.Fame < 2500)
                    this.AddLoot(LootPack.Average);
                else if (this.Fame < 5000)
                    this.AddLoot(LootPack.Rich);
                else if (this.Fame < 10000)
                    this.AddLoot(LootPack.FilthyRich);
                else
                    this.AddLoot(LootPack.UltraRich);
            }

            this.m_Spawning = false;
            this.m_KillersLuck = 0;
        }

        public virtual void GenerateLoot()
        {
        }

        public virtual void AddLoot(LootPack pack, int amount)
        {
            for (int i = 0; i < amount; ++i)
                this.AddLoot(pack);
        }

        public virtual void AddLoot(LootPack pack)
        {
            if (this.Summoned)
                return;

            Container backpack = this.Backpack;

            if (backpack == null)
            {
                backpack = new Backpack();

                backpack.Movable = false;

                this.AddItem(backpack);
            }

            pack.Generate(this, backpack, this.m_Spawning, this.m_KillersLuck);
        }

        public bool PackArmor(int minLevel, int maxLevel)
        {
            return this.PackArmor(minLevel, maxLevel, 1.0);
        }

        public bool PackArmor(int minLevel, int maxLevel, double chance)
        {
            if (chance <= Utility.RandomDouble())
                return false;

            Cap(ref minLevel, 0, 5);
            Cap(ref maxLevel, 0, 5);

            if (Core.AOS)
            {
                Item item = Loot.RandomArmorOrShieldOrJewelry();

                if (item == null)
                    return false;

                int attributeCount, min, max;
                GetRandomAOSStats(minLevel, maxLevel, out attributeCount, out min, out max);

                if (item is BaseArmor)
                    BaseRunicTool.ApplyAttributesTo((BaseArmor)item, attributeCount, min, max);
                else if (item is BaseJewel)
                    BaseRunicTool.ApplyAttributesTo((BaseJewel)item, attributeCount, min, max);

                this.PackItem(item);
            }
            else
            {
                BaseArmor armor = Loot.RandomArmorOrShield();

                if (armor == null)
                    return false;

                armor.ProtectionLevel = (ArmorProtectionLevel)RandomMinMaxScaled(minLevel, maxLevel);
                armor.Durability = (ArmorDurabilityLevel)RandomMinMaxScaled(minLevel, maxLevel);

                this.PackItem(armor);
            }

            return true;
        }

        public static void GetRandomAOSStats(int minLevel, int maxLevel, out int attributeCount, out int min, out int max)
        {
            int v = RandomMinMaxScaled(minLevel, maxLevel);

            if (v >= 5)
            {
                attributeCount = Utility.RandomMinMax(2, 6);
                min = 20;
                max = 70;
            }
            else if (v == 4)
            {
                attributeCount = Utility.RandomMinMax(2, 4);
                min = 20;
                max = 50;
            }
            else if (v == 3)
            {
                attributeCount = Utility.RandomMinMax(2, 3);
                min = 20;
                max = 40;
            }
            else if (v == 2)
            {
                attributeCount = Utility.RandomMinMax(1, 2);
                min = 10;
                max = 30;
            }
            else
            {
                attributeCount = 1;
                min = 10;
                max = 20;
            }
        }

        public static int RandomMinMaxScaled(int min, int max)
        {
            if (min == max)
                return min;

            if (min > max)
            {
                int hold = min;
                min = max;
                max = hold;
            }

            /* Example:
            *    min: 1
            *    max: 5
            *  count: 5
            *
            * total = (5*5) + (4*4) + (3*3) + (2*2) + (1*1) = 25 + 16 + 9 + 4 + 1 = 55
            *
            * chance for min+0 : 25/55 : 45.45%
            * chance for min+1 : 16/55 : 29.09%
            * chance for min+2 :  9/55 : 16.36%
            * chance for min+3 :  4/55 :  7.27%
            * chance for min+4 :  1/55 :  1.81%
            */

            int count = max - min + 1;
            int total = 0, toAdd = count;

            for (int i = 0; i < count; ++i, --toAdd)
                total += toAdd * toAdd;

            int rand = Utility.Random(total);
            toAdd = count;

            int val = min;

            for (int i = 0; i < count; ++i, --toAdd, ++val)
            {
                rand -= toAdd * toAdd;

                if (rand < 0)
                    break;
            }

            return val;
        }

        public bool PackSlayer()
        {
            return this.PackSlayer(0.05);
        }

        public bool PackSlayer(double chance)
        {
            if (chance <= Utility.RandomDouble())
                return false;

            if (Utility.RandomBool())
            {
                BaseInstrument instrument = Loot.RandomInstrument();

                if (instrument != null)
                {
                    instrument.Slayer = SlayerGroup.GetLootSlayerType(this.GetType());
                    this.PackItem(instrument);
                }
            }
            else if (!Core.AOS)
            {
                BaseWeapon weapon = Loot.RandomWeapon();

                if (weapon != null)
                {
                    weapon.Slayer = SlayerGroup.GetLootSlayerType(this.GetType());
                    this.PackItem(weapon);
                }
            }

            return true;
        }

        public bool PackWeapon(int minLevel, int maxLevel)
        {
            return this.PackWeapon(minLevel, maxLevel, 1.0);
        }

        public bool PackWeapon(int minLevel, int maxLevel, double chance)
        {
            if (chance <= Utility.RandomDouble())
                return false;

            Cap(ref minLevel, 0, 5);
            Cap(ref maxLevel, 0, 5);

            if (Core.AOS)
            {
                Item item = Loot.RandomWeaponOrJewelry();

                if (item == null)
                    return false;

                int attributeCount, min, max;
                GetRandomAOSStats(minLevel, maxLevel, out attributeCount, out min, out max);

                if (item is BaseWeapon)
                    BaseRunicTool.ApplyAttributesTo((BaseWeapon)item, attributeCount, min, max);
                else if (item is BaseJewel)
                    BaseRunicTool.ApplyAttributesTo((BaseJewel)item, attributeCount, min, max);

                this.PackItem(item);
            }
            else
            {
                BaseWeapon weapon = Loot.RandomWeapon();

                if (weapon == null)
                    return false;

                if (0.05 > Utility.RandomDouble())
                    weapon.Slayer = SlayerName.Silver;

                weapon.DamageLevel = (WeaponDamageLevel)RandomMinMaxScaled(minLevel, maxLevel);
                weapon.AccuracyLevel = (WeaponAccuracyLevel)RandomMinMaxScaled(minLevel, maxLevel);
                weapon.DurabilityLevel = (WeaponDurabilityLevel)RandomMinMaxScaled(minLevel, maxLevel);

                this.PackItem(weapon);
            }

            return true;
        }

        public void PackGold(int amount)
        {
            if (amount > 0)
                this.PackItem(new Gold(amount));
        }

        public void PackGold(int min, int max)
        {
            this.PackGold(Utility.RandomMinMax(min, max));
        }

        public void PackStatue(int min, int max)
        {
            this.PackStatue(Utility.RandomMinMax(min, max));
        }

        public void PackStatue(int amount)
        {
            for (int i = 0; i < amount; ++i)
                this.PackStatue();
        }

        public void PackStatue()
        {
            this.PackItem(Loot.RandomStatue());
        }

        public void PackGem()
        {
            this.PackGem(1);
        }

        public void PackGem(int min, int max)
        {
            this.PackGem(Utility.RandomMinMax(min, max));
        }

        public void PackGem(int amount)
        {
            if (amount <= 0)
                return;

            Item gem = Loot.RandomGem();

            gem.Amount = amount;

            this.PackItem(gem);
        }

        public void PackNecroReg(int min, int max)
        {
            this.PackNecroReg(Utility.RandomMinMax(min, max));
        }

        public void PackNecroReg(int amount)
        {
            for (int i = 0; i < amount; ++i)
                this.PackNecroReg();
        }

        public void PackNecroReg()
        {
            if (!Core.AOS)
                return;

            this.PackItem(Loot.RandomNecromancyReagent());
        }

        public void PackReg(int min, int max)
        {
            this.PackReg(Utility.RandomMinMax(min, max));
        }

        public void PackReg(int amount)
        {
            if (amount <= 0)
                return;

            Item reg = Loot.RandomReagent();

            reg.Amount = amount;

            this.PackItem(reg);
        }

        public void PackItem(Item item)
        {
            if (this.Summoned || item == null)
            {
                if (item != null)
                    item.Delete();

                return;
            }

            Container pack = this.Backpack;

            if (pack == null)
            {
                pack = new Backpack();

                pack.Movable = false;

                this.AddItem(pack);
            }

            if (!item.Stackable || !pack.TryDropItem(this, item, false)) // try stack
                pack.DropItem(item); // failed, drop it anyway
        }

        #endregion

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel >= AccessLevel.GameMaster && !this.Body.IsHuman)
            {
                Container pack = this.Backpack;

                if (pack != null)
                    pack.DisplayTo(from);
            }

            if (this.DeathAdderCharmable && from.CanBeHarmful(this, false))
            {
                DeathAdder da = Spells.Necromancy.SummonFamiliarSpell.Table[from] as DeathAdder;

                if (da != null && !da.Deleted)
                {
                    from.SendAsciiMessage("You charm the snake.  Select a target to attack.");
                    from.Target = new DeathAdderCharmTarget(this);
                }
            }

            base.OnDoubleClick(from);
        }

        private class DeathAdderCharmTarget : Target
        {
            private BaseCreature m_Charmed;

            public DeathAdderCharmTarget(BaseCreature charmed)
                : base(-1, false, TargetFlags.Harmful)
            {
                this.m_Charmed = charmed;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (!this.m_Charmed.DeathAdderCharmable || this.m_Charmed.Combatant != null || !from.CanBeHarmful(this.m_Charmed, false))
                    return;

                DeathAdder da = Spells.Necromancy.SummonFamiliarSpell.Table[from] as DeathAdder;
                if (da == null || da.Deleted)
                    return;

                Mobile targ = targeted as Mobile;
                if (targ == null || !from.CanBeHarmful(targ, false))
                    return;

                from.RevealingAction();
                from.DoHarmful(targ, true);

                this.m_Charmed.Combatant = targ;

                if (this.m_Charmed.AIObject != null)
                    this.m_Charmed.AIObject.Action = ActionType.Combat;
            }
        }

        public override void AddNameProperties(ObjectPropertyList list)
        {
            base.AddNameProperties(list);

            if (Core.ML)
            {
                if (this.DisplayWeight)
                    list.Add(this.TotalWeight == 1 ? 1072788 : 1072789, this.TotalWeight.ToString()); // Weight: ~1_WEIGHT~ stones

                if (this.m_ControlOrder == OrderType.Guard)
                    list.Add(1080078); // guarding
            }

            if (this.Summoned && !this.IsAnimatedDead && !this.IsNecroFamiliar && !(this is Clone))
                list.Add(1049646); // (summoned)
            else if (this.Controlled && this.Commandable)
            {
                if (this.IsBonded)	//Intentional difference (showing ONLY bonded when bonded instead of bonded & tame)
                    list.Add(1049608); // (bonded)
                else
                    list.Add(502006); // (tame)
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            if (this.Controlled && this.Commandable)
            {
                int number;

                if (this.Summoned)
                    number = 1049646; // (summoned)
                else if (this.IsBonded)
                    number = 1049608; // (bonded)
                else
                    number = 502006; // (tame)

                this.PrivateOverheadMessage(MessageType.Regular, 0x3B2, number, from.NetState);
            }

            base.OnSingleClick(from);
        }

        public virtual double TreasureMapChance
        {
            get
            {
                return TreasureMap.LootChance;
            }
        }
        public virtual int TreasureMapLevel
        {
            get
            {
                return -1;
            }
        }

        public virtual bool IgnoreYoungProtection
        {
            get
            {
                return false;
            }
        }

        public override bool OnBeforeDeath()
        {
            int treasureLevel = this.TreasureMapLevel;

            if (treasureLevel == 1 && this.Map == Map.Trammel && TreasureMap.IsInHavenIsland(this))
            {
                Mobile killer = this.LastKiller;

                if (killer is BaseCreature)
                    killer = ((BaseCreature)killer).GetMaster();

                if (killer is PlayerMobile && ((PlayerMobile)killer).Young)
                    treasureLevel = 0;
            }

            if (!this.Summoned && !this.NoKillAwards && !this.IsBonded)
            {
                if (treasureLevel >= 0)
                {
                    if (this.m_Paragon && XmlParagon.GetChestChance(this) > Utility.RandomDouble())
                        XmlParagon.AddChest(this, treasureLevel);
                    else if ((this.Map == Map.Felucca || this.Map == Map.Trammel) && TreasureMap.LootChance >= Utility.RandomDouble())
                        this.PackItem(new TreasureMap(treasureLevel, this.Map));
                }

                if (this.m_Paragon && Paragon.ChocolateIngredientChance > Utility.RandomDouble())
                {
                    switch ( Utility.Random(4) )
                    {
                        case 0:
                            this.PackItem(new CocoaButter());
                            break;
                        case 1:
                            this.PackItem(new CocoaLiquor());
                            break;
                        case 2:
                            this.PackItem(new SackOfSugar());
                            break;
                        case 3:
                            this.PackItem(new Vanilla());
                            break;
                    }
                }
            }

            if (!this.Summoned && !this.NoKillAwards && !this.m_HasGeneratedLoot)
            {
                this.m_HasGeneratedLoot = true;
                this.GenerateLoot(false);
            }

            if (!this.NoKillAwards && this.Region.IsPartOf("Doom"))
            {
                int bones = Engines.Quests.Doom.TheSummoningQuest.GetDaemonBonesFor(this);

                if (bones > 0)
                    this.PackItem(new DaemonBone(bones));
            }

            if (this.IsAnimatedDead)
                Effects.SendLocationEffect(this.Location, this.Map, 0x3728, 13, 1, 0x461, 4);

            InhumanSpeech speechType = this.SpeechType;

            if (speechType != null)
                speechType.OnDeath(this);

            if (this.m_ReceivedHonorContext != null)
                this.m_ReceivedHonorContext.OnTargetKilled();

            return base.OnBeforeDeath();
        }

        private bool m_NoKillAwards;

        public bool NoKillAwards
        {
            get
            {
                return this.m_NoKillAwards;
            }
            set
            {
                this.m_NoKillAwards = value;
            }
        }

        public int ComputeBonusDamage(List<DamageEntry> list, Mobile m)
        {
            int bonus = 0;

            for (int i = list.Count - 1; i >= 0; --i)
            {
                DamageEntry de = list[i];

                if (de.Damager == m || !(de.Damager is BaseCreature))
                    continue;

                BaseCreature bc = (BaseCreature)de.Damager;
                Mobile master = null;

                master = bc.GetMaster();

                if (master == m)
                    bonus += de.DamageGiven;
            }

            return bonus;
        }

        public Mobile GetMaster()
        {
            if (this.Controlled && this.ControlMaster != null)
                return this.ControlMaster;
            else if (this.Summoned && this.SummonMaster != null)
                return this.SummonMaster;

            return null;
        }

        private class FKEntry
        {
            public Mobile m_Mobile;
            public int m_Damage;

            public FKEntry(Mobile m, int damage)
            {
                this.m_Mobile = m;
                this.m_Damage = damage;
            }
        }

        public static List<DamageStore> GetLootingRights(List<DamageEntry> damageEntries, int hitsMax)
        {
            List<DamageStore> rights = new List<DamageStore>();

            for (int i = damageEntries.Count - 1; i >= 0; --i)
            {
                if (i >= damageEntries.Count)
                    continue;

                DamageEntry de = damageEntries[i];

                if (de.HasExpired)
                {
                    damageEntries.RemoveAt(i);
                    continue;
                }

                int damage = de.DamageGiven;

                List<DamageEntry> respList = de.Responsible;

                if (respList != null)
                {
                    for (int j = 0; j < respList.Count; ++j)
                    {
                        DamageEntry subEntry = respList[j];
                        Mobile master = subEntry.Damager;

                        if (master == null || master.Deleted || !master.Player)
                            continue;

                        bool needNewSubEntry = true;

                        for (int k = 0; needNewSubEntry && k < rights.Count; ++k)
                        {
                            DamageStore ds = rights[k];

                            if (ds.m_Mobile == master)
                            {
                                ds.m_Damage += subEntry.DamageGiven;
                                needNewSubEntry = false;
                            }
                        }

                        if (needNewSubEntry)
                            rights.Add(new DamageStore(master, subEntry.DamageGiven));

                        damage -= subEntry.DamageGiven;
                    }
                }

                Mobile m = de.Damager;

                if (m == null || m.Deleted || !m.Player)
                    continue;

                if (damage <= 0)
                    continue;

                bool needNewEntry = true;

                for (int j = 0; needNewEntry && j < rights.Count; ++j)
                {
                    DamageStore ds = rights[j];

                    if (ds.m_Mobile == m)
                    {
                        ds.m_Damage += damage;
                        needNewEntry = false;
                    }
                }

                if (needNewEntry)
                    rights.Add(new DamageStore(m, damage));
            }

            if (rights.Count > 0)
            {
                rights[0].m_Damage = (int)(rights[0].m_Damage * 1.25);	//This would be the first valid person attacking it.  Gets a 25% bonus.  Per 1/19/07 Five on Friday

                if (rights.Count > 1)
                    rights.Sort(); //Sort by damage

                int topDamage = rights[0].m_Damage;
                int minDamage;

                if (hitsMax >= 3000)
                    minDamage = topDamage / 16;
                else if (hitsMax >= 1000)
                    minDamage = topDamage / 8;
                else if (hitsMax >= 200)
                    minDamage = topDamage / 4;
                else
                    minDamage = topDamage / 2;

                for (int i = 0; i < rights.Count; ++i)
                {
                    DamageStore ds = rights[i];

                    ds.m_HasRight = (ds.m_Damage >= minDamage);
                }
            }

            return rights;
        }

        private bool m_Allured;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Allured
        {
            get
            {
                return this.m_Allured;
            }
            set
            {
                this.m_Allured = value;
            }
        }

        public virtual void OnRelease(Mobile from)
        {
            if (this.m_Allured)
                Timer.DelayCall(TimeSpan.FromSeconds(2), new TimerCallback(Delete));
        }

        public override void OnItemLifted(Mobile from, Item item)
        {
            base.OnItemLifted(from, item);

            this.InvalidateProperties();
        }

        public virtual bool GivesMLMinorArtifact
        {
            get
            {
                return false;
            }
        }

        private static Type[] m_Artifacts = new Type[]
        {
            typeof(AegisOfGrace), typeof(BladeDance),
            typeof(Bonesmasher), typeof(FeyLeggings),
            typeof(FleshRipper), typeof(HelmOfSwiftness),
            typeof(PadsOfTheCuSidhe), typeof(RaedsGlory),
            typeof(RighteousAnger), typeof(RobeOfTheEclipse),
            typeof(RobeOfTheEquinox), typeof(SoulSeeker),
            typeof(TalonBite), typeof(BloodwoodSpirit),
            typeof(TotemOfVoid), typeof(QuiverOfRage),
            typeof(QuiverOfElements), typeof(BrightsightLenses),
            typeof(Boomstick), typeof(WildfireBow),
            typeof(Windsong)
        };

        public static void GiveMinorArtifact(Mobile m)
        {
            Item item = Activator.CreateInstance(m_Artifacts[Utility.Random(m_Artifacts.Length)]) as Item;

            if (item == null)
                return;

            if (m.AddToBackpack(item))
            {
                m.SendLocalizedMessage(1062317); // For your valor in combating the fallen beast, a special artifact has been bestowed on you.
                m.SendLocalizedMessage(1072223); // An item has been placed in your backpack.
            }
            else if (m.BankBox != null && m.BankBox.TryDropItem(m, item, false))
            {
                m.SendLocalizedMessage(1062317); // For your valor in combating the fallen beast, a special artifact has been bestowed on you.
                m.SendLocalizedMessage(1072224); // An item has been placed in your bank box.
            }
            else
            {
                item.MoveToWorld(m.Location, m.Map);
                m.SendLocalizedMessage(1072523); // You find an artifact, but your backpack and bank are too full to hold it.
            }
        }
		
        public virtual bool GivesSAArtifact
        {
            get
            {
                return false;
            }
        }

        private static Type[] m_SAArtifacts = new Type[]
        {
            typeof(AxesOfFury), typeof(BreastplateOfTheBerserker),
            typeof(EternalGuardianStaff), typeof(LegacyOfDespair),
            typeof(GiantSteps), typeof(StaffOfShatteredDreams),
            typeof(PetrifiedSnake), typeof(StoneDragonsTooth),
            typeof(TokenOfHolyFavor), typeof(SwordOfShatteredHopes),
            typeof(Venom), typeof(StormCaller)
        };

        public static void GiveSAArtifact(Mobile m)
        {
            Item item = Activator.CreateInstance(m_SAArtifacts[Utility.Random(m_SAArtifacts.Length)]) as Item;

            if (item == null)
                return;

            if (m.AddToBackpack(item))
            {
                m.SendLocalizedMessage(1062317); // For your valor in combating the fallen beast, a special artifact has been bestowed on you.
                m.SendLocalizedMessage(1072223); // An item has been placed in your backpack.
            }
            else if (m.BankBox != null && m.BankBox.TryDropItem(m, item, false))
            {
                m.SendLocalizedMessage(1062317); // For your valor in combating the fallen beast, a special artifact has been bestowed on you.
                m.SendLocalizedMessage(1072224); // An item has been placed in your bank box.
            }
            else
            {
                item.MoveToWorld(m.Location, m.Map);
                m.SendLocalizedMessage(1072523); // You find an artifact, but your backpack and bank are too full to hold it.
            }
        }

        public virtual void OnKilledBy(Mobile mob)
        {
            if (this.m_Paragon && XmlParagon.CheckArtifactChance(mob, this))
                XmlParagon.GiveArtifactTo(mob, this);

            #region Mondain's Legacy
            if (this.GivesMLMinorArtifact)
            {
                if (MondainsLegacy.CheckArtifactChance(mob, this))
                    MondainsLegacy.GiveArtifactTo(mob);
            }
            #endregion

            if (this.GivesSAArtifact && Paragon.CheckArtifactChance(mob, this))
                GiveSAArtifact(mob);

            EventSink.InvokeOnKilledBy(new OnKilledByEventArgs(this, mob));
        }

        public override void OnDeath(Container c)
        {
            MeerMage.StopEffect(this, false);

            if (this.IsBonded)
            {
                int sound = this.GetDeathSound();

                if (sound >= 0)
                    Effects.PlaySound(this, this.Map, sound);

                this.Warmode = false;

                this.Poison = null;
                this.Combatant = null;

                this.Hits = 0;
                this.Stam = 0;
                this.Mana = 0;

                this.IsDeadPet = true;
                this.ControlTarget = this.ControlMaster;
                this.ControlOrder = OrderType.Follow;

                ProcessDeltaQueue();
                this.SendIncomingPacket();
                this.SendIncomingPacket();

                List<AggressorInfo> aggressors = this.Aggressors;

                for (int i = 0; i < aggressors.Count; ++i)
                {
                    AggressorInfo info = aggressors[i];

                    if (info.Attacker.Combatant == this)
                        info.Attacker.Combatant = null;
                }

                List<AggressorInfo> aggressed = this.Aggressed;

                for (int i = 0; i < aggressed.Count; ++i)
                {
                    AggressorInfo info = aggressed[i];

                    if (info.Defender.Combatant == this)
                        info.Defender.Combatant = null;
                }

                Mobile owner = this.ControlMaster;

                if (owner == null || owner.Deleted || owner.Map != this.Map || !owner.InRange(this, 12) || !this.CanSee(owner) || !this.InLOS(owner))
                {
                    if (this.OwnerAbandonTime == DateTime.MinValue)
                        this.OwnerAbandonTime = DateTime.Now;
                }
                else
                {
                    this.OwnerAbandonTime = DateTime.MinValue;
                }

                GiftOfLifeSpell.HandleDeath(this);

                this.CheckStatTimers();
            }
            else
            {
                if (!this.Summoned && !this.m_NoKillAwards)
                {
                    int totalFame = this.Fame / 100;
                    int totalKarma = -this.Karma / 100;
                    if (this.Map == Map.Felucca)
                    {
                        totalFame += ((totalFame / 10) * 3);
                        totalKarma += ((totalKarma / 10) * 3);
                    }

                    List<DamageStore> list = GetLootingRights(this.DamageEntries, this.HitsMax);
                    List<Mobile> titles = new List<Mobile>();
                    List<int> fame = new List<int>();
                    List<int> karma = new List<int>();

                    bool givenQuestKill = false;
                    bool givenFactionKill = false;
                    bool givenToTKill = false;

                    for (int i = 0; i < list.Count; ++i)
                    {
                        DamageStore ds = list[i];

                        if (!ds.m_HasRight)
                            continue;

                        Party party = Engines.PartySystem.Party.Get(ds.m_Mobile);

                        if (party != null)
                        {
                            int divedFame = totalFame / party.Members.Count;
                            int divedKarma = totalKarma / party.Members.Count;

                            for (int j = 0; j < party.Members.Count; ++j)
                            {
                                PartyMemberInfo info = party.Members[j] as PartyMemberInfo;

                                if (info != null && info.Mobile != null)
                                {
                                    int index = titles.IndexOf(info.Mobile);

                                    if (index == -1)
                                    {
                                        titles.Add(info.Mobile);
                                        fame.Add(divedFame);
                                        karma.Add(divedKarma);
                                    }
                                    else
                                    {
                                        fame[index] += divedFame;
                                        karma[index] += divedKarma;
                                    }
                                }
                            }
                        }
                        else
                        {
                            titles.Add(ds.m_Mobile);
                            fame.Add(totalFame);
                            karma.Add(totalKarma);
                        }

                        this.OnKilledBy(ds.m_Mobile);

                        XmlQuest.RegisterKill(this, ds.m_Mobile);

                        if (!givenFactionKill)
                        {
                            givenFactionKill = true;
                            Faction.HandleDeath(this, ds.m_Mobile);
                        }

                        Region region = ds.m_Mobile.Region;

                        if (!givenToTKill && (this.Map == Map.Tokuno || region.IsPartOf("Yomotsu Mines") || region.IsPartOf("Fan Dancer's Dojo")))
                        {
                            givenToTKill = true;
                            TreasuresOfTokuno.HandleKill(this, ds.m_Mobile);
                        }

                        if (givenQuestKill)
                            continue;

                        PlayerMobile pm = ds.m_Mobile as PlayerMobile;

                        if (pm != null)
                        {
                            QuestSystem qs = pm.Quest;

                            if (qs != null)
                            {
                                qs.OnKill(this, c);
                                givenQuestKill = true;
                            }

                            QuestHelper.CheckCreature(pm, this);
                        }
                    }
                    for (int i = 0; i < titles.Count; ++i)
                    {
                        Titles.AwardFame(titles[i], fame[i], true);
                        Titles.AwardKarma(titles[i], karma[i], true);
                    }
                }

                base.OnDeath(c);

                if (this.DeleteCorpseOnDeath)
                    c.Delete();
            }
        }

        /* To save on cpu usage, RunUO creatures only reacquire creatures under the following circumstances:
        *  - 10 seconds have elapsed since the last time it tried
        *  - The creature was attacked
        *  - Some creatures, like dragons, will reacquire when they see someone move
        *
        * This functionality appears to be implemented on OSI as well
        */

        private DateTime m_NextReacquireTime;

        public DateTime NextReacquireTime
        {
            get
            {
                return this.m_NextReacquireTime;
            }
            set
            {
                this.m_NextReacquireTime = value;
            }
        }

        public virtual TimeSpan ReacquireDelay
        {
            get
            {
                return TimeSpan.FromSeconds(10.0);
            }
        }

        public virtual bool ReacquireOnMovement
        {
            get
            {
                return false;
            }
        }

        public virtual bool AcquireOnApproach
        {
            get
            {
                return this.m_Paragon;
            }
        }

        public virtual int AcquireOnApproachRange
        {
            get
            {
                return 10;
            }
        }

        public override void OnDelete()
        {
            Mobile m = this.m_ControlMaster;

            this.SetControlMaster(null);
            this.SummonMaster = null;

            if (this.m_ReceivedHonorContext != null)
                this.m_ReceivedHonorContext.Cancel();

            base.OnDelete();

            if (m != null)
                m.InvalidateProperties();
        }

        public override bool CanBeHarmful(Mobile target, bool message, bool ignoreOurBlessedness)
        {
            if (target is BaseFactionGuard)
                return false;

            if ((target is BaseVendor && ((BaseVendor)target).IsInvulnerable) || target is PlayerVendor || target is TownCrier)
            {
                if (message)
                {
                    if (target.Title == null)
                        this.SendMessage("{0} the vendor cannot be harmed.", target.Name);
                    else
                        this.SendMessage("{0} {1} cannot be harmed.", target.Name, target.Title);
                }

                return false;
            }

            return base.CanBeHarmful(target, message, ignoreOurBlessedness);
        }

        public override bool CanBeRenamedBy(Mobile from)
        {
            bool ret = base.CanBeRenamedBy(from);

            if (this.Controlled && from == this.ControlMaster && !from.Region.IsPartOf(typeof(Jail)))
                ret = true;

            return ret;
        }

        public bool SetControlMaster(Mobile m)
        {
            if (m == null)
            {
                this.ControlMaster = null;
                this.Controlled = false;
                this.ControlTarget = null;
                this.ControlOrder = OrderType.None;
                this.Guild = null;

                this.Delta(MobileDelta.Noto);
            }
            else
            {
                ISpawner se = this.Spawner;
                if (se != null && se.UnlinkOnTaming)
                {
                    this.Spawner.Remove(this);
                    this.Spawner = null;
                }

                if (m.Followers + this.ControlSlots > m.FollowersMax)
                {
                    m.SendLocalizedMessage(1049607); // You have too many followers to control that creature.
                    return false;
                }

                this.CurrentWayPoint = null;//so tamed animals don't try to go back

                this.ControlMaster = m;
                this.Controlled = true;
                this.ControlTarget = null;
                this.ControlOrder = OrderType.Come;
                this.Guild = null;

                if (this.m_DeleteTimer != null)
                {
                    this.m_DeleteTimer.Stop();
                    this.m_DeleteTimer = null;
                }

                this.Delta(MobileDelta.Noto);
            }

            this.InvalidateProperties();

            return true;
        }

        public override void OnRegionChange(Region Old, Region New)
        {
            base.OnRegionChange(Old, New);

            if (this.Controlled)
            {
                SpawnEntry se = this.Spawner as SpawnEntry;

                if (se != null && !se.UnlinkOnTaming && (New == null || !New.AcceptsSpawnsFrom(se.Region)))
                {
                    this.Spawner.Remove(this);
                    this.Spawner = null;
                }
            }
        }

        private static bool m_Summoning;

        public static bool Summoning
        {
            get
            {
                return m_Summoning;
            }
            set
            {
                m_Summoning = value;
            }
        }

        public static bool Summon(BaseCreature creature, Mobile caster, Point3D p, int sound, TimeSpan duration)
        {
            return Summon(creature, true, caster, p, sound, duration);
        }

        public static bool Summon(BaseCreature creature, bool controlled, Mobile caster, Point3D p, int sound, TimeSpan duration)
        {
            if (caster.Followers + creature.ControlSlots > caster.FollowersMax)
            {
                caster.SendLocalizedMessage(1049645); // You have too many followers to summon that creature.
                creature.Delete();
                return false;
            }

            m_Summoning = true;

            if (controlled)
                creature.SetControlMaster(caster);

            creature.RangeHome = 10;
            creature.Summoned = true;

            creature.SummonMaster = caster;

            Container pack = creature.Backpack;

            if (pack != null)
            {
                for (int i = pack.Items.Count - 1; i >= 0; --i)
                {
                    if (i >= pack.Items.Count)
                        continue;

                    pack.Items[i].Delete();
                }
            }

            creature.SetHits((int)Math.Floor(creature.HitsMax * (1 + ArcaneEmpowermentSpell.GetSpellBonus(caster, false) / 100.0)));

            new UnsummonTimer(caster, creature, duration).Start();
            creature.m_SummonEnd = DateTime.Now + duration;

            creature.MoveToWorld(p, caster.Map);

            Effects.PlaySound(p, creature.Map, sound);

            m_Summoning = false;

            return true;
        }

        private static Type[] m_MinorArtifactsMl = new Type[]
        {
            typeof(AegisOfGrace), typeof(BladeDance), typeof(Bonesmasher),
            typeof(Boomstick), typeof(FeyLeggings), typeof(FleshRipper),
            typeof(HelmOfSwiftness), typeof(PadsOfTheCuSidhe), typeof(QuiverOfRage),
            typeof(QuiverOfElements), typeof(RaedsGlory), typeof(RighteousAnger),
            typeof(RobeOfTheEclipse), typeof(RobeOfTheEquinox), typeof(SoulSeeker),
            typeof(TalonBite), typeof(WildfireBow), typeof(Windsong),
            // TODO: Brightsight lenses, Bloodwood spirit, Totem of the void
        };

        public static Type[] MinorArtifactsMl
        {
            get
            {
                return m_MinorArtifactsMl;
            }
        }

        private static bool EnableRummaging = true;

        private const double ChanceToRummage = 0.5; // 50%

        private const double MinutesToNextRummageMin = 1.0;
        private const double MinutesToNextRummageMax = 4.0;

        private const double MinutesToNextChanceMin = 0.25;
        private const double MinutesToNextChanceMax = 0.75;

        private DateTime m_NextRummageTime;

        public virtual bool CanBreath
        {
            get
            {
                return this.HasBreath && !this.Summoned;
            }
        }
        public virtual bool IsDispellable
        {
            get
            {
                return this.Summoned && !this.IsAnimatedDead;
            }
        }

        #region Animate Dead
        public virtual bool CanAnimateDead
        {
            get
            {
                return false;
            }
        }
        public virtual double AnimateChance
        {
            get
            {
                return 0.05;
            }
        }
        public virtual int AnimateScalar
        {
            get
            {
                return 50;
            }
        }
        public virtual TimeSpan AnimateDelay
        {
            get
            {
                return TimeSpan.FromSeconds(10);
            }
        }
        public virtual BaseCreature Animates
        {
            get
            {
                return null;
            }
        }

        private DateTime m_NextAnimateDead = DateTime.Now;

        public virtual void AnimateDead()
        {
            Corpse best = null;

            foreach (Item item in this.Map.GetItemsInRange(this.Location, 12))
            {
                Corpse c = null;

                if (item is Corpse)
                    c = (Corpse)item;
                else
                    continue;

                if (c.ItemID != 0x2006 || c.Channeled || c.Owner.GetType() == typeof(PlayerMobile) || c.Owner.GetType() == null || (c.Owner != null && c.Owner.Fame < 100) || ((c.Owner != null) && (c.Owner is BaseCreature) && (((BaseCreature)c.Owner).Summoned || ((BaseCreature)c.Owner).IsBonded)))
                    continue;

                best = c;
                break;
            }

            if (best != null)
            {
                BaseCreature animated = this.Animates;

                if (animated != null)
                {
                    animated.Tamable = false;
                    animated.MoveToWorld(best.Location, this.Map);
                    Scale(animated, this.AnimateScalar);
                    Effects.PlaySound(best.Location, this.Map, 0x1FB);
                    Effects.SendLocationParticles(EffectItem.Create(best.Location, this.Map, EffectItem.DefaultDuration), 0x3789, 1, 40, 0x3F, 3, 9907, 0);
                }

                best.ProcessDelta();
                best.SendRemovePacket();
                best.ItemID = Utility.Random(0xECA, 9); // bone graphic
                best.Hue = 0;
                best.ProcessDelta();
            }

            this.m_NextAnimateDead = DateTime.Now + this.AnimateDelay;
        }

        public static void Scale(BaseCreature bc, int scalar)
        {
            int toScale;

            toScale = bc.RawStr;
            bc.RawStr = AOS.Scale(toScale, scalar);

            toScale = bc.HitsMaxSeed;

            if (toScale > 0)
                bc.HitsMaxSeed = AOS.Scale(toScale, scalar);

            bc.Hits = bc.Hits; // refresh hits
        }

        #endregion

        #region Area Poison
        public virtual bool CanAreaPoison
        {
            get
            {
                return false;
            }
        }
        public virtual Poison HitAreaPoison
        {
            get
            {
                return Poison.Deadly;
            }
        }
        public virtual int AreaPoisonRange
        {
            get
            {
                return 10;
            }
        }
        public virtual double AreaPosionChance
        {
            get
            {
                return 0.4;
            }
        }
        public virtual TimeSpan AreaPoisonDelay
        {
            get
            {
                return TimeSpan.FromSeconds(8);
            }
        }

        private DateTime m_NextAreaPoison = DateTime.Now;

        public virtual void AreaPoison()
        {
            List<Mobile> targets = new List<Mobile>();

            if (this.Map != null)
                foreach (Mobile m in this.GetMobilesInRange(this.AreaDamageRange))
                    if (this != m && SpellHelper.ValidIndirectTarget(this, m) && this.CanBeHarmful(m, false) && (!Core.AOS || this.InLOS(m)))
                    {
                        if (m is BaseCreature && ((BaseCreature)m).Controlled)
                            targets.Add(m);
                        else if (m.Player)
                            targets.Add(m);
                    }

            for (int i = 0; i < targets.Count; ++i)
            {
                Mobile m = targets[i];

                m.ApplyPoison(this, this.HitAreaPoison);

                Effects.SendLocationParticles(EffectItem.Create(m.Location, m.Map, EffectItem.DefaultDuration), 0x36B0, 1, 14, 63, 7, 9915, 0);
                Effects.PlaySound(m.Location, m.Map, 0x229);
            }

            this.m_NextAreaPoison = DateTime.Now + this.AreaPoisonDelay;
        }

        #endregion

        #region Area damage
        public virtual bool CanAreaDamage
        {
            get
            {
                return false;
            }
        }
        public virtual int AreaDamageRange
        {
            get
            {
                return 10;
            }
        }
        public virtual double AreaDamageScalar
        {
            get
            {
                return 1.0;
            }
        }
        public virtual double AreaDamageChance
        {
            get
            {
                return 0.4;
            }
        }
        public virtual TimeSpan AreaDamageDelay
        {
            get
            {
                return TimeSpan.FromSeconds(8);
            }
        }

        public virtual int AreaPhysicalDamage
        {
            get
            {
                return 0;
            }
        }
        public virtual int AreaFireDamage
        {
            get
            {
                return 100;
            }
        }
        public virtual int AreaColdDamage
        {
            get
            {
                return 0;
            }
        }
        public virtual int AreaPoisonDamage
        {
            get
            {
                return 0;
            }
        }
        public virtual int AreaEnergyDamage
        {
            get
            {
                return 0;
            }
        }

        private DateTime m_NextAreaDamage = DateTime.Now;

        public virtual void AreaDamage()
        {
            List<Mobile> targets = new List<Mobile>();

            if (this.Map != null)
                foreach (Mobile m in this.GetMobilesInRange(this.AreaDamageRange))
                    if (this != m && SpellHelper.ValidIndirectTarget(this, m) && this.CanBeHarmful(m, false) && (!Core.AOS || this.InLOS(m)))
                    {
                        if (m is BaseCreature && ((BaseCreature)m).Controlled)
                            targets.Add(m);
                        else if (m.Player)
                            targets.Add(m);
                    }

            for (int i = 0; i < targets.Count; ++i)
            {
                Mobile m = targets[i];

                int damage;

                if (Core.AOS)
                {
                    damage = m.Hits / 2;

                    if (!m.Player)
                        damage = Math.Max(Math.Min(damage, 100), 15);

                    damage += Utility.RandomMinMax(0, 15);
                }
                else
                {
                    damage = (m.Hits * 6) / 10;

                    if (!m.Player && damage < 10)
                        damage = 10;
                    else if (damage > 75)
                        damage = 75;
                }

                damage = (int)(damage * this.AreaDamageScalar);

                this.DoHarmful(m);
                this.AreaDamageEffect(m);
                SpellHelper.Damage(TimeSpan.Zero, m, this, damage, this.AreaPhysicalDamage, this.AreaFireDamage, this.AreaColdDamage, this.AreaPoisonDamage, this.AreaEnergyDamage);
            }

            this.m_NextAreaDamage = DateTime.Now + this.AreaDamageDelay;
        }

        public virtual void AreaDamageEffect(Mobile m)
        {
            m.FixedParticles(0x3709, 10, 30, 5052, EffectLayer.LeftFoot); // flamestrike
            m.PlaySound(0x208);
        }

        #endregion

        #region Healing
        public virtual bool CanHeal
        {
            get
            {
                return false;
            }
        }
        public virtual bool CanHealOwner
        {
            get
            {
                return false;
            }
        }
        public virtual double HealScalar
        {
            get
            {
                return 1.0;
            }
        }

        public virtual int HealSound
        {
            get
            {
                return 0x57;
            }
        }
        public virtual int HealStartRange
        {
            get
            {
                return 2;
            }
        }
        public virtual int HealEndRange
        {
            get
            {
                return this.RangePerception;
            }
        }
        public virtual double HealTrigger
        {
            get
            {
                return 0.78;
            }
        }
        public virtual double HealDelay
        {
            get
            {
                return 6.5;
            }
        }
        public virtual double HealInterval
        {
            get
            {
                return 0.0;
            }
        }
        public virtual bool HealFully
        {
            get
            {
                return true;
            }
        }
        public virtual double HealOwnerTrigger
        {
            get
            {
                return 0.78;
            }
        }
        public virtual double HealOwnerDelay
        {
            get
            {
                return 6.5;
            }
        }
        public virtual double HealOwnerInterval
        {
            get
            {
                return 30.0;
            }
        }
        public virtual bool HealOwnerFully
        {
            get
            {
                return false;
            }
        }

        private DateTime m_NextHealTime = DateTime.Now;
        private DateTime m_NextHealOwnerTime = DateTime.Now;
        private Timer m_HealTimer = null;

        public bool IsHealing
        {
            get
            {
                return (this.m_HealTimer != null);
            }
        }

        public virtual void HealStart(Mobile patient)
        {
            bool onSelf = (patient == this);

            //DoBeneficial( patient );

            this.RevealingAction();

            if (!onSelf)
            {
                patient.RevealingAction();
                patient.SendLocalizedMessage(1008078, false, this.Name); //  : Attempting to heal you.
            }

            double seconds = (onSelf ? this.HealDelay : this.HealOwnerDelay) + (patient.Alive ? 0.0 : 5.0);

            this.m_HealTimer = Timer.DelayCall(TimeSpan.FromSeconds(seconds), new TimerStateCallback(Heal_Callback), patient);
        }

        private void Heal_Callback(object state)
        {
            if (state is Mobile)
                this.Heal((Mobile)state);
        }

        public virtual void Heal(Mobile patient)
        {
            if (!this.Alive || this.Map == Map.Internal || !this.CanBeBeneficial(patient, true, true) || patient.Map != this.Map || !this.InRange(patient, this.HealEndRange))
            {
                this.StopHeal();
                return;
            }

            bool onSelf = (patient == this);

            if (!patient.Alive)
            {
            }
            else if (patient.Poisoned)
            {
                int poisonLevel = patient.Poison.Level;

                double healing = this.Skills.Healing.Value;
                double anatomy = this.Skills.Anatomy.Value;
                double chance = (healing - 30.0) / 50.0 - poisonLevel * 0.1;

                if ((healing >= 60.0 && anatomy >= 60.0) && chance > Utility.RandomDouble())
                {
                    if (patient.CurePoison(this))
                    {
                        patient.SendLocalizedMessage(1010059); // You have been cured of all poisons.

                        this.CheckSkill(SkillName.Healing, 0.0, 60.0 + poisonLevel * 10.0); // TODO: Verify formula
                        this.CheckSkill(SkillName.Anatomy, 0.0, 100.0);
                    }
                }
            }
            else if (BleedAttack.IsBleeding(patient))
            {
                patient.SendLocalizedMessage(1060167); // The bleeding wounds have healed, you are no longer bleeding!
                BleedAttack.EndBleed(patient, false);
            }
            else
            {
                double healing = this.Skills.Healing.Value;
                double anatomy = this.Skills.Anatomy.Value;
                double chance = (healing + 10.0) / 100.0;

                if (chance > Utility.RandomDouble())
                {
                    double min, max;

                    min = (anatomy / 10.0) + (healing / 6.0) + 4.0;
                    max = (anatomy / 8.0) + (healing / 3.0) + 4.0;

                    if (onSelf)
                        max += 10;

                    double toHeal = min + (Utility.RandomDouble() * (max - min));

                    toHeal *= this.HealScalar;

                    patient.Heal((int)toHeal);

                    this.CheckSkill(SkillName.Healing, 0.0, 90.0);
                    this.CheckSkill(SkillName.Anatomy, 0.0, 100.0);
                }
            }

            this.HealEffect(patient);

            this.StopHeal();

            if ((onSelf && this.HealFully && this.Hits >= this.HealTrigger * this.HitsMax && this.Hits < this.HitsMax) || (!onSelf && this.HealOwnerFully && patient.Hits >= this.HealOwnerTrigger * patient.HitsMax && patient.Hits < patient.HitsMax))
                this.HealStart(patient);
        }

        public virtual void StopHeal()
        {
            if (this.m_HealTimer != null)
                this.m_HealTimer.Stop();

            this.m_HealTimer = null;
        }

        public virtual void HealEffect(Mobile patient)
        {
            patient.PlaySound(this.HealSound);
        }

        #endregion

        #region Damaging Aura
        private DateTime m_NextAura;

        public virtual bool HasAura
        {
            get
            {
                return false;
            }
        }
        public virtual TimeSpan AuraInterval
        {
            get
            {
                return TimeSpan.FromSeconds(5);
            }
        }
        public virtual int AuraRange
        {
            get
            {
                return 4;
            }
        }

        public virtual int AuraBaseDamage
        {
            get
            {
                return 5;
            }
        }
        public virtual int AuraPhysicalDamage
        {
            get
            {
                return 0;
            }
        }
        public virtual int AuraFireDamage
        {
            get
            {
                return 100;
            }
        }
        public virtual int AuraColdDamage
        {
            get
            {
                return 0;
            }
        }
        public virtual int AuraPoisonDamage
        {
            get
            {
                return 0;
            }
        }
        public virtual int AuraEnergyDamage
        {
            get
            {
                return 0;
            }
        }
        public virtual int AuraChaosDamage
        {
            get
            {
                return 0;
            }
        }

        public virtual void AuraDamage()
        {
            if (!this.Alive || this.IsDeadBondedPet)
                return;

            List<Mobile> list = new List<Mobile>();

            foreach (Mobile m in this.GetMobilesInRange(this.AuraRange))
            {
                if (m == this || !this.CanBeHarmful(m, false) || (Core.AOS && !this.InLOS(m)))
                    continue;

                if (m is BaseCreature)
                {
                    BaseCreature bc = (BaseCreature)m;

                    if (bc.Controlled || bc.Summoned || bc.Team != this.Team)
                        list.Add(m);
                }
                else if (m.Player)
                {
                    list.Add(m);
                }
            }

            foreach (Mobile m in list)
            {
                AOS.Damage(m, this, this.AuraBaseDamage, this.AuraPhysicalDamage, this.AuraFireDamage, this.AuraColdDamage, this.AuraPoisonDamage, this.AuraEnergyDamage, this.AuraChaosDamage);
                this.AuraEffect(m);
            }
        }

        public virtual void AuraEffect(Mobile m)
        {
        }

        #endregion

        public virtual void OnThink()
        {
            if (EnableRummaging && this.CanRummageCorpses && !this.Summoned && !this.Controlled && DateTime.Now >= this.m_NextRummageTime)
            {
                double min, max;

                if (ChanceToRummage > Utility.RandomDouble() && this.Rummage())
                {
                    min = MinutesToNextRummageMin;
                    max = MinutesToNextRummageMax;
                }
                else
                {
                    min = MinutesToNextChanceMin;
                    max = MinutesToNextChanceMax;
                }

                double delay = min + (Utility.RandomDouble() * (max - min));
                this.m_NextRummageTime = DateTime.Now + TimeSpan.FromMinutes(delay);
            }

            if (this.CanBreath && DateTime.Now >= this.m_NextBreathTime) // tested: controlled dragons do breath fire, what about summoned skeletal dragons?
            {
                Mobile target = this.Combatant;

                if (target != null && target.Alive && !target.IsDeadBondedPet && this.CanBeHarmful(target) && target.Map == this.Map && !this.IsDeadBondedPet && target.InRange(this, this.BreathRange) && this.InLOS(target) && !this.BardPacified)
                {
                    if ((DateTime.Now - this.m_NextBreathTime) < TimeSpan.FromSeconds(30))
                    {
                        this.BreathStart(target);
                    }

                    this.m_NextBreathTime = DateTime.Now + TimeSpan.FromSeconds(this.BreathMinDelay + (Utility.RandomDouble() * this.BreathMaxDelay));
                }
            }

            if ((this.CanHeal || this.CanHealOwner) && this.Alive && !this.IsHealing && !this.BardPacified)
            {
                Mobile owner = this.ControlMaster;

                if (owner != null && this.CanHealOwner && DateTime.Now >= this.m_NextHealOwnerTime && this.CanBeBeneficial(owner, true, true) && owner.Map == this.Map && this.InRange(owner, this.HealStartRange) && this.InLOS(owner) && owner.Hits < this.HealOwnerTrigger * owner.HitsMax)
                {
                    this.HealStart(owner);

                    this.m_NextHealOwnerTime = DateTime.Now + TimeSpan.FromSeconds(this.HealOwnerInterval);
                }
                else if (this.CanHeal && DateTime.Now >= this.m_NextHealTime && this.CanBeBeneficial(this) && (this.Hits < this.HealTrigger * this.HitsMax || this.Poisoned))
                {
                    this.HealStart(this);

                    this.m_NextHealTime = DateTime.Now + TimeSpan.FromSeconds(this.HealInterval);
                }
            }

            if (this.HasAura && DateTime.Now >= this.m_NextAura)
            {
                this.AuraDamage();
                this.m_NextAura = DateTime.Now + this.AuraInterval;
            }
        }

        public virtual bool Rummage()
        {
            Corpse toRummage = null;

            foreach (Item item in this.GetItemsInRange(2))
            {
                if (item is Corpse && item.Items.Count > 0)
                {
                    toRummage = (Corpse)item;
                    break;
                }
            }

            if (toRummage == null)
                return false;

            Container pack = this.Backpack;

            if (pack == null)
                return false;

            List<Item> items = toRummage.Items;

            bool rejected;
            LRReason reason;

            for (int i = 0; i < items.Count; ++i)
            {
                Item item = items[Utility.Random(items.Count)];

                this.Lift(item, item.Amount, out rejected, out reason);

                if (!rejected && this.Drop(this, new Point3D(-1, -1, 0)))
                {
                    // *rummages through a corpse and takes an item*
                    this.PublicOverheadMessage(MessageType.Emote, 0x3B2, 1008086);
                    //!+ TODO: Instancing of Rummaged stuff.
                    return true;
                }
            }

            return false;
        }

        public void Pacify(Mobile master, DateTime endtime)
        {
            this.BardPacified = true;
            this.BardEndTime = endtime;
        }

        public override Mobile GetDamageMaster(Mobile damagee)
        {
            if (this.m_bBardProvoked && damagee == this.m_bBardTarget)
                return this.m_bBardMaster;
            else if (this.m_bControlled && this.m_ControlMaster != null)
                return this.m_ControlMaster;
            else if (this.m_bSummoned && this.m_SummonMaster != null)
                return this.m_SummonMaster;

            return base.GetDamageMaster(damagee);
        }

        public void Provoke(Mobile master, Mobile target, bool bSuccess)
        {
            this.BardProvoked = true;

            if (!Core.ML)
            {
                this.PublicOverheadMessage(MessageType.Emote, this.EmoteHue, false, "*looks furious*");
            }

            if (bSuccess)
            {
                this.PlaySound(this.GetIdleSound());

                this.BardMaster = master;
                this.BardTarget = target;
                this.Combatant = target;
                this.BardEndTime = DateTime.Now + TimeSpan.FromSeconds(30.0);

                if (target is BaseCreature)
                {
                    BaseCreature t = (BaseCreature)target;

                    if (t.Unprovokable || (t.IsParagon && BaseInstrument.GetBaseDifficulty(t) >= 160.0))
                        return;

                    t.BardProvoked = true;

                    t.BardMaster = master;
                    t.BardTarget = this;
                    t.Combatant = this;
                    t.BardEndTime = DateTime.Now + TimeSpan.FromSeconds(30.0);
                }
            }
            else
            {
                this.PlaySound(this.GetAngerSound());

                this.BardMaster = master;
                this.BardTarget = target;
            }
        }

        public bool FindMyName(string str, bool bWithAll)
        {
            int i, j;

            string name = this.Name;

            if (name == null || str.Length < name.Length)
                return false;

            string[] wordsString = str.Split(' ');
            string[] wordsName = name.Split(' ');

            for (j = 0; j < wordsName.Length; j++)
            {
                string wordName = wordsName[j];

                bool bFound = false;
                for (i = 0; i < wordsString.Length; i++)
                {
                    string word = wordsString[i];

                    if (Insensitive.Equals(word, wordName))
                        bFound = true;

                    if (bWithAll && Insensitive.Equals(word, "all"))
                        return true;
                }

                if (!bFound)
                    return false;
            }

            return true;
        }

        public static void TeleportPets(Mobile master, Point3D loc, Map map)
        {
            TeleportPets(master, loc, map, false);
        }

        public static void TeleportPets(Mobile master, Point3D loc, Map map, bool onlyBonded)
        {
            List<Mobile> move = new List<Mobile>();

            foreach (Mobile m in master.GetMobilesInRange(3))
            {
                if (m is BaseCreature)
                {
                    BaseCreature pet = (BaseCreature)m;

                    if (pet.Controlled && pet.ControlMaster == master)
                    {
                        if (!onlyBonded || pet.IsBonded)
                        {
                            if (pet.ControlOrder == OrderType.Guard || pet.ControlOrder == OrderType.Follow || pet.ControlOrder == OrderType.Come)
                                move.Add(pet);
                        }
                    }
                }
            }

            foreach (Mobile m in move)
                m.MoveToWorld(loc, map);
        }

        public virtual void ResurrectPet()
        {
            if (!this.IsDeadPet)
                return;

            this.OnBeforeResurrect();

            this.Poison = null;

            this.Warmode = false;

            this.Hits = 10;
            this.Stam = this.StamMax;
            this.Mana = 0;

            ProcessDeltaQueue();

            this.IsDeadPet = false;

            Effects.SendPacket(this.Location, this.Map, new BondedStatus(0, this.Serial, 0));

            this.SendIncomingPacket();
            this.SendIncomingPacket();

            this.OnAfterResurrect();

            Mobile owner = this.ControlMaster;

            if (owner == null || owner.Deleted || owner.Map != this.Map || !owner.InRange(this, 12) || !this.CanSee(owner) || !this.InLOS(owner))
            {
                if (this.OwnerAbandonTime == DateTime.MinValue)
                    this.OwnerAbandonTime = DateTime.Now;
            }
            else
            {
                this.OwnerAbandonTime = DateTime.MinValue;
            }

            this.CheckStatTimers();
        }

        public override bool CanBeDamaged()
        {
            if (this.IsDeadPet)
                return false;

            return base.CanBeDamaged();
        }

        public virtual bool PlayerRangeSensitive
        {
            get
            {
                return (this.CurrentWayPoint == null);
            }
        }//If they are following a waypoint, they'll continue to follow it even if players aren't around

        public override void OnSectorDeactivate()
        {
            if (this.PlayerRangeSensitive && this.m_AI != null)
                this.m_AI.Deactivate();

            base.OnSectorDeactivate();
        }

        public override void OnSectorActivate()
        {
            if (this.PlayerRangeSensitive && this.m_AI != null)
                this.m_AI.Activate();

            base.OnSectorActivate();
        }

        private bool m_RemoveIfUntamed;

        // used for deleting untamed creatures [in houses]
        private int m_RemoveStep;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool RemoveIfUntamed
        {
            get
            {
                return this.m_RemoveIfUntamed;
            }
            set
            {
                this.m_RemoveIfUntamed = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int RemoveStep
        {
            get
            {
                return this.m_RemoveStep;
            }
            set
            {
                this.m_RemoveStep = value;
            }
        }
    }

    public class LoyaltyTimer : Timer
    {
        private static readonly TimeSpan InternalDelay = TimeSpan.FromMinutes(5.0);

        public static void Initialize()
        {
            new LoyaltyTimer().Start();
        }

        public LoyaltyTimer()
            : base(InternalDelay, InternalDelay)
        {
            this.m_NextHourlyCheck = DateTime.Now + TimeSpan.FromHours(1.0);
            this.Priority = TimerPriority.FiveSeconds;
        }

        private DateTime m_NextHourlyCheck;

        protected override void OnTick()
        {
            if (DateTime.Now >= this.m_NextHourlyCheck)
                this.m_NextHourlyCheck = DateTime.Now + TimeSpan.FromHours(1.0);
            else
                return;

            List<BaseCreature> toRelease = new List<BaseCreature>();

            // added array for wild creatures in house regions to be removed
            List<BaseCreature> toRemove = new List<BaseCreature>();

            foreach (Mobile m in World.Mobiles.Values)
            {
                if (m is BaseMount && ((BaseMount)m).Rider != null)
                {
                    ((BaseCreature)m).OwnerAbandonTime = DateTime.MinValue;
                    continue;
                }

                if (m is BaseCreature)
                {
                    BaseCreature c = (BaseCreature)m;

                    if (c.IsDeadPet)
                    {
                        Mobile owner = c.ControlMaster;

                        if (!c.IsStabled && (owner == null || owner.Deleted || owner.Map != c.Map || !owner.InRange(c, 12) || !c.CanSee(owner) || !c.InLOS(owner)))
                        {
                            if (c.OwnerAbandonTime == DateTime.MinValue)
                                c.OwnerAbandonTime = DateTime.Now;
                            else if ((c.OwnerAbandonTime + c.BondingAbandonDelay) <= DateTime.Now)
                                toRemove.Add(c);
                        }
                        else
                        {
                            c.OwnerAbandonTime = DateTime.MinValue;
                        }
                    }
                    else if (c.Controlled && c.Commandable)
                    {
                        c.OwnerAbandonTime = DateTime.MinValue;

                        if (c.Map != Map.Internal)
                        {
                            c.Loyalty -= (BaseCreature.MaxLoyalty / 10);

                            if (c.Loyalty < (BaseCreature.MaxLoyalty / 10))
                            {
                                c.Say(1043270, c.Name); // * ~1_NAME~ looks around desperately *
                                c.PlaySound(c.GetIdleSound());
                            }

                            if (c.Loyalty <= 0)
                                toRelease.Add(c);
                        }
                    }

                    // added lines to check if a wild creature in a house region has to be removed or not
                    if (!c.Controlled && !c.IsStabled && ((c.Region.IsPartOf(typeof(HouseRegion)) && c.CanBeDamaged()) || (c.RemoveIfUntamed && c.Spawner == null)))
                    {
                        c.RemoveStep++;

                        if (c.RemoveStep >= 20)
                            toRemove.Add(c);
                    }
                    else
                    {
                        c.RemoveStep = 0;
                    }
                }
            }

            foreach (BaseCreature c in toRelease)
            {
                c.Say(1043255, c.Name); // ~1_NAME~ appears to have decided that is better off without a master!
                c.Loyalty = BaseCreature.MaxLoyalty; // Wonderfully Happy
                c.IsBonded = false;
                c.BondingBegin = DateTime.MinValue;
                c.OwnerAbandonTime = DateTime.MinValue;
                c.ControlTarget = null;
                //c.ControlOrder = OrderType.Release;
                c.AIObject.DoOrderRelease(); // this will prevent no release of creatures left alone with AI disabled (and consequent bug of Followers)
                c.DropBackpack();
            }

            // added code to handle removing of wild creatures in house regions
            foreach (BaseCreature c in toRemove)
            {
                c.Delete();
            }
        }
    }
}