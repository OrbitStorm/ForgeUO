using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CustomsFramework;
using Server.Accounting;
using Server.Commands;
using Server.ContextMenus;
using Server.Guilds;
using Server.Gumps;
using Server.HuePickers;
using Server.Items;
using Server.Menus;
using Server.Mobiles;
using Server.Network;
using Server.Prompts;
using Server.Targeting;

namespace Server
{
	#region Callbacks
	public delegate void TargetCallback(Mobile from, object targeted);
	public delegate void TargetStateCallback(Mobile from, object targeted, object state);
	public delegate void TargetStateCallback<T>(Mobile from, object targeted, T state);

	public delegate void PromptCallback(Mobile from, string text);
	public delegate void PromptStateCallback(Mobile from, string text, object state);
	public delegate void PromptStateCallback<T>(Mobile from, string text, T state);
	#endregion

	#region [...]Mods
	public class TimedSkillMod : SkillMod
	{
		private readonly DateTime m_Expire;

		public TimedSkillMod(SkillName skill, bool relative, double value, TimeSpan delay)
			: this(skill, relative, value, DateTime.Now + delay)
		{
		}

		public TimedSkillMod(SkillName skill, bool relative, double value, DateTime expire)
			: base(skill, relative, value)
		{
			this.m_Expire = expire;
		}

		public override bool CheckCondition()
		{
			return (DateTime.Now < this.m_Expire);
		}
	}

	public class EquipedSkillMod : SkillMod
	{
		private readonly Item m_Item;
		private readonly Mobile m_Mobile;

		public EquipedSkillMod(SkillName skill, bool relative, double value, Item item, Mobile mobile)
			: base(skill, relative, value)
		{
			this.m_Item = item;
			this.m_Mobile = mobile;
		}

		public override bool CheckCondition()
		{
			return (!this.m_Item.Deleted && !this.m_Mobile.Deleted && this.m_Item.Parent == this.m_Mobile);
		}
	}

	public class DefaultSkillMod : SkillMod
	{
		public DefaultSkillMod(SkillName skill, bool relative, double value)
			: base(skill, relative, value)
		{
		}

		public override bool CheckCondition()
		{
			return true;
		}
	}

	public abstract class SkillMod
	{
		private Mobile m_Owner;
		private SkillName m_Skill;
		private bool m_Relative;
		private double m_Value;
		private bool m_ObeyCap;

		protected SkillMod(SkillName skill, bool relative, double value)
		{
			this.m_Skill = skill;
			this.m_Relative = relative;
			this.m_Value = value;
		}

		public bool ObeyCap
		{
			get
			{
				return this.m_ObeyCap;
			}
			set
			{
				this.m_ObeyCap = value;

				if (this.m_Owner != null)
				{
					Skill sk = this.m_Owner.Skills[this.m_Skill];

					if (sk != null)
						sk.Update();
				}
			}
		}

		public Mobile Owner
		{
			get
			{
				return this.m_Owner;
			}
			set
			{
				if (this.m_Owner != value)
				{
					if (this.m_Owner != null)
						this.m_Owner.RemoveSkillMod(this);

					this.m_Owner = value;

					if (this.m_Owner != value)
						this.m_Owner.AddSkillMod(this);
				}
			}
		}

		public void Remove()
		{
			this.Owner = null;
		}

		public SkillName Skill
		{
			get
			{
				return this.m_Skill;
			}
			set
			{
				if (this.m_Skill != value)
				{
					Skill oldUpdate = (this.m_Owner != null ? this.m_Owner.Skills[this.m_Skill] : null);

					this.m_Skill = value;

					if (this.m_Owner != null)
					{
						Skill sk = this.m_Owner.Skills[this.m_Skill];

						if (sk != null)
							sk.Update();
					}

					if (oldUpdate != null)
						oldUpdate.Update();
				}
			}
		}

		public bool Relative
		{
			get
			{
				return this.m_Relative;
			}
			set
			{
				if (this.m_Relative != value)
				{
					this.m_Relative = value;

					if (this.m_Owner != null)
					{
						Skill sk = this.m_Owner.Skills[this.m_Skill];

						if (sk != null)
							sk.Update();
					}
				}
			}
		}

		public bool Absolute
		{
			get
			{
				return !this.m_Relative;
			}
			set
			{
				if (this.m_Relative == value)
				{
					this.m_Relative = !value;

					if (this.m_Owner != null)
					{
						Skill sk = this.m_Owner.Skills[this.m_Skill];

						if (sk != null)
							sk.Update();
					}
				}
			}
		}

		public double Value
		{
			get
			{
				return this.m_Value;
			}
			set
			{
				if (this.m_Value != value)
				{
					this.m_Value = value;

					if (this.m_Owner != null)
					{
						Skill sk = this.m_Owner.Skills[this.m_Skill];

						if (sk != null)
							sk.Update();
					}
				}
			}
		}

		public abstract bool CheckCondition();
	}

	public class ResistanceMod
	{
		private Mobile m_Owner;
		private ResistanceType m_Type;
		private int m_Offset;

		public Mobile Owner
		{
			get
			{
				return this.m_Owner;
			}
			set
			{
				this.m_Owner = value;
			}
		}

		public ResistanceType Type
		{
			get
			{
				return this.m_Type;
			}
			set
			{
				if (this.m_Type != value)
				{
					this.m_Type = value;

					if (this.m_Owner != null)
						this.m_Owner.UpdateResistances();
				}
			}
		}

		public int Offset
		{
			get
			{
				return this.m_Offset;
			}
			set
			{
				if (this.m_Offset != value)
				{
					this.m_Offset = value;

					if (this.m_Owner != null)
						this.m_Owner.UpdateResistances();
				}
			}
		}

		public ResistanceMod(ResistanceType type, int offset)
		{
			this.m_Type = type;
			this.m_Offset = offset;
		}
	}

	public class StatMod
	{
		private readonly StatType m_Type;
		private readonly string m_Name;
		private readonly int m_Offset;
		private readonly TimeSpan m_Duration;
		private readonly DateTime m_Added;

		public StatType Type
		{
			get
			{
				return this.m_Type;
			}
		}
		public string Name
		{
			get
			{
				return this.m_Name;
			}
		}
		public int Offset
		{
			get
			{
				return this.m_Offset;
			}
		}

		public bool HasElapsed()
		{
			if (this.m_Duration == TimeSpan.Zero)
				return false;

			return (DateTime.Now - this.m_Added) >= this.m_Duration;
		}

		public StatMod(StatType type, string name, int offset, TimeSpan duration)
		{
			this.m_Type = type;
			this.m_Name = name;
			this.m_Offset = offset;
			this.m_Duration = duration;
			this.m_Added = DateTime.Now;
		}
	}

	#endregion

	public class DamageEntry
	{
		private readonly Mobile m_Damager;
		private int m_DamageGiven;
		private DateTime m_LastDamage;
		private List<DamageEntry> m_Responsible;

		public Mobile Damager
		{
			get
			{
				return this.m_Damager;
			}
		}
		public int DamageGiven
		{
			get
			{
				return this.m_DamageGiven;
			}
			set
			{
				this.m_DamageGiven = value;
			}
		}
		public DateTime LastDamage
		{
			get
			{
				return this.m_LastDamage;
			}
			set
			{
				this.m_LastDamage = value;
			}
		}
		public bool HasExpired
		{
			get
			{
				return (DateTime.Now > (this.m_LastDamage + m_ExpireDelay));
			}
		}
		public List<DamageEntry> Responsible
		{
			get
			{
				return this.m_Responsible;
			}
			set
			{
				this.m_Responsible = value;
			}
		}

		private static TimeSpan m_ExpireDelay = TimeSpan.FromMinutes(2.0);

		public static TimeSpan ExpireDelay
		{
			get
			{
				return m_ExpireDelay;
			}
			set
			{
				m_ExpireDelay = value;
			}
		}

		public DamageEntry(Mobile damager)
		{
			this.m_Damager = damager;
		}
	}

	#region Enums
	[Flags]
	public enum StatType
	{
		Str = 1,
		Dex = 2,
		Int = 4,
		All = 7
	}

	public enum StatLockType : byte
	{
		Up,
		Down,
		Locked
	}

	[CustomEnum(new string[] { "North", "Right", "East", "Down", "South", "Left", "West", "Up" })]
	public enum Direction : byte
	{
		North = 0x0,
		Right = 0x1,
		East = 0x2,
		Down = 0x3,
		South = 0x4,
		Left = 0x5,
		West = 0x6,
		Up = 0x7,

		Mask = 0x7,
		Running = 0x80,
		ValueMask = 0x87
	}

	[Flags]
	public enum MobileDelta
	{
		None = 0x00000000,
		Name = 0x00000001,
		Flags = 0x00000002,
		Hits = 0x00000004,
		Mana = 0x00000008,
		Stam = 0x00000010,
		Stat = 0x00000020,
		Noto = 0x00000040,
		Gold = 0x00000080,
		Weight = 0x00000100,
		Direction = 0x00000200,
		Hue = 0x00000400,
		Body = 0x00000800,
		Armor = 0x00001000,
		StatCap = 0x00002000,
		GhostUpdate = 0x00004000,
		Followers = 0x00008000,
		Properties = 0x00010000,
		TithingPoints = 0x00020000,
		Resistances = 0x00040000,
		WeaponDamage = 0x00080000,
		Hair = 0x00100000,
		FacialHair = 0x00200000,
		Race = 0x00400000,
		HealthbarYellow = 0x00800000,
		HealthbarPoison = 0x01000000,

		Attributes = 0x0000001C
	}

	public enum AccessLevel
	{
		Player,
		VIP,
		Counselor,
		Decorator,
		Spawner,
		GameMaster,
		Seer,
		Administrator,
		Developer,
		CoOwner,
		Owner
	}

	public enum VisibleDamageType
	{
		None,
		Related,
		Everyone
	}

	public enum ResistanceType
	{
		Physical,
		Fire,
		Cold,
		Poison,
		Energy
	}

	public enum ApplyPoisonResult
	{
		Poisoned,
		Immune,
		HigherPoisonActive,
		Cured
	}
	#endregion

	public class MobileNotConnectedException : Exception
	{
		public MobileNotConnectedException(Mobile source, string message)
			: base(message)
		{
			this.Source = source.ToString();
		}
	}

	#region Delegates

	public delegate bool SkillCheckTargetHandler(Mobile from, SkillName skill, object target, double minSkill, double maxSkill);
	public delegate bool SkillCheckLocationHandler(Mobile from, SkillName skill, double minSkill, double maxSkill);

	public delegate bool SkillCheckDirectTargetHandler(Mobile from, SkillName skill, object target, double chance);
	public delegate bool SkillCheckDirectLocationHandler(Mobile from, SkillName skill, double chance);

	public delegate TimeSpan RegenRateHandler(Mobile from);

	public delegate bool AllowBeneficialHandler(Mobile from, Mobile target);
	public delegate bool AllowHarmfulHandler(Mobile from, Mobile target);

	public delegate Container CreateCorpseHandler(Mobile from, HairInfo hair, FacialHairInfo facialhair, List<Item> initialContent, List<Item> equipedItems);

	#endregion

	/// <summary>
	/// Base class representing players, npcs, and creatures.
	/// </summary>
	public class Mobile : IEntity, IHued, IComparable<Mobile>, ISerializable, ISpawnable
	{
		#region CompareTo(...)
		public int CompareTo(IEntity other)
		{
			if (other == null)
				return -1;

			return this.m_Serial.CompareTo(other.Serial);
		}

		public int CompareTo(Mobile other)
		{
			return this.CompareTo((IEntity)other);
		}

		public int CompareTo(object other)
		{
			if (other == null || other is IEntity)
				return this.CompareTo((IEntity)other);

			throw new ArgumentException();
		}

		#endregion
		#region Customs Framework
		private List<BaseModule> m_Modules = new List<BaseModule>();

		[CommandProperty(AccessLevel.Developer)]
		public List<BaseModule> Modules { get { return m_Modules; } set { m_Modules = value; } }
		//public List<BaseModule> Modules { get; private set; }


		public BaseModule GetModule(string name)
		{
			return Modules.FirstOrDefault(mod => mod.Name == name);
		}

		public BaseModule GetModule(Type type)
		{
			return Modules.FirstOrDefault(mod => mod.GetType() == type);
		}

		public List<BaseModule> GetModules(string name)
		{
			return Modules.Where(mod => mod.Name == name).ToList();
		}

		public List<BaseModule> SearchModules(string search)
		{
			string[] keywords = search.ToLower().Split(' ');
			List<BaseModule> modules = new List<BaseModule>();

			foreach (BaseModule mod in Modules)
			{
				bool match = true;
				string name = mod.Name.ToLower();

				foreach (string keyword in keywords)
				{
					if (name.IndexOf(keyword, StringComparison.Ordinal) == -1)
						match = false;
				}

				if (match)
					modules.Add(mod);
			}

			return modules;
		}
		#endregion

		private static bool m_DragEffects = true;

		public static bool DragEffects
		{
			get
			{
				return m_DragEffects;
			}
			set
			{
				m_DragEffects = value;
			}
		}

		#region Handlers

		private static AllowBeneficialHandler m_AllowBeneficialHandler;
		private static AllowHarmfulHandler m_AllowHarmfulHandler;

		public static AllowBeneficialHandler AllowBeneficialHandler
		{
			get
			{
				return m_AllowBeneficialHandler;
			}
			set
			{
				m_AllowBeneficialHandler = value;
			}
		}

		public static AllowHarmfulHandler AllowHarmfulHandler
		{
			get
			{
				return m_AllowHarmfulHandler;
			}
			set
			{
				m_AllowHarmfulHandler = value;
			}
		}

		private static SkillCheckTargetHandler m_SkillCheckTargetHandler;
		private static SkillCheckLocationHandler m_SkillCheckLocationHandler;
		private static SkillCheckDirectTargetHandler m_SkillCheckDirectTargetHandler;
		private static SkillCheckDirectLocationHandler m_SkillCheckDirectLocationHandler;

		public static SkillCheckTargetHandler SkillCheckTargetHandler
		{
			get
			{
				return m_SkillCheckTargetHandler;
			}
			set
			{
				m_SkillCheckTargetHandler = value;
			}
		}

		public static SkillCheckLocationHandler SkillCheckLocationHandler
		{
			get
			{
				return m_SkillCheckLocationHandler;
			}
			set
			{
				m_SkillCheckLocationHandler = value;
			}
		}

		public static SkillCheckDirectTargetHandler SkillCheckDirectTargetHandler
		{
			get
			{
				return m_SkillCheckDirectTargetHandler;
			}
			set
			{
				m_SkillCheckDirectTargetHandler = value;
			}
		}

		public static SkillCheckDirectLocationHandler SkillCheckDirectLocationHandler
		{
			get
			{
				return m_SkillCheckDirectLocationHandler;
			}
			set
			{
				m_SkillCheckDirectLocationHandler = value;
			}
		}

		#endregion

		#region Regeneration

		private static RegenRateHandler m_HitsRegenRate, m_StamRegenRate, m_ManaRegenRate;
		private static TimeSpan m_DefaultHitsRate, m_DefaultStamRate, m_DefaultManaRate;

		public static RegenRateHandler HitsRegenRateHandler
		{
			get
			{
				return m_HitsRegenRate;
			}
			set
			{
				m_HitsRegenRate = value;
			}
		}

		public static TimeSpan DefaultHitsRate
		{
			get
			{
				return m_DefaultHitsRate;
			}
			set
			{
				m_DefaultHitsRate = value;
			}
		}

		public static RegenRateHandler StamRegenRateHandler
		{
			get
			{
				return m_StamRegenRate;
			}
			set
			{
				m_StamRegenRate = value;
			}
		}

		public static TimeSpan DefaultStamRate
		{
			get
			{
				return m_DefaultStamRate;
			}
			set
			{
				m_DefaultStamRate = value;
			}
		}

		public static RegenRateHandler ManaRegenRateHandler
		{
			get
			{
				return m_ManaRegenRate;
			}
			set
			{
				m_ManaRegenRate = value;
			}
		}

		public static TimeSpan DefaultManaRate
		{
			get
			{
				return m_DefaultManaRate;
			}
			set
			{
				m_DefaultManaRate = value;
			}
		}

		public static TimeSpan GetHitsRegenRate(Mobile m)
		{
			if (m_HitsRegenRate == null)
				return m_DefaultHitsRate;
			else
				return m_HitsRegenRate(m);
		}

		public static TimeSpan GetStamRegenRate(Mobile m)
		{
			if (m_StamRegenRate == null)
				return m_DefaultStamRate;
			else
				return m_StamRegenRate(m);
		}

		public static TimeSpan GetManaRegenRate(Mobile m)
		{
			if (m_ManaRegenRate == null)
				return m_DefaultManaRate;
			else
				return m_ManaRegenRate(m);
		}

		#endregion

		private class MovementRecord
		{
			public DateTime m_End;

			private static readonly Queue<MovementRecord> m_InstancePool = new Queue<MovementRecord>();

			public static MovementRecord NewInstance(DateTime end)
			{
				MovementRecord r;

				if (m_InstancePool.Count > 0)
				{
					r = m_InstancePool.Dequeue();

					r.m_End = end;
				}
				else
				{
					r = new MovementRecord(end);
				}

				return r;
			}

			private MovementRecord(DateTime end)
			{
				this.m_End = end;
			}

			public bool Expired()
			{
				bool v = (DateTime.Now >= this.m_End);

				if (v)
					m_InstancePool.Enqueue(this);

				return v;
			}
		}

		#region Var declarations
		private readonly Serial m_Serial;
		private Map m_Map;
		private Point3D m_Location;
		private Direction m_Direction;
		private Body m_Body;
		private int m_Hue;
		private Poison m_Poison;
		private Timer m_PoisonTimer;
		private BaseGuild m_Guild;
		private string m_GuildTitle;
		private bool m_Criminal;
		private string m_Name;
		private int m_Kills, m_ShortTermMurders;
		private int m_SpeechHue, m_EmoteHue, m_WhisperHue, m_YellHue;
		private string m_Language;
		private NetState m_NetState;
		private bool m_Female, m_Warmode, m_Hidden, m_Blessed, m_Flying;
		private int m_StatCap;
		private int m_Str, m_Dex, m_Int;
		private int m_Hits, m_Stam, m_Mana;
		private int m_Fame, m_Karma;
		private AccessLevel m_AccessLevel;
		private Skills m_Skills;
		private List<Item> m_Items;
		private bool m_Player;
		private string m_Title;
		private string m_Profile;
		private bool m_ProfileLocked;
		private int m_LightLevel;
		private int m_TotalGold, m_TotalItems, m_TotalWeight;
		private List<StatMod> m_StatMods;
		private ISpell m_Spell;
		private Target m_Target;
		private Prompt m_Prompt;
		private ContextMenu m_ContextMenu;
		private List<AggressorInfo> m_Aggressors, m_Aggressed;
		private Mobile m_Combatant;
		private List<Mobile> m_Stabled;
		private bool m_AutoPageNotify;
		private bool m_Meditating;
		private bool m_CanHearGhosts;
		private bool m_CanSwim, m_CantWalk;
		private int m_TithingPoints;
		private bool m_DisplayGuildTitle;
		private Mobile m_GuildFealty;
		private DateTime m_NextSpellTime;
		private DateTime[] m_StuckMenuUses;
		private Timer m_ExpireCombatant;
		private Timer m_ExpireCriminal;
		private Timer m_ExpireAggrTimer;
		private Timer m_LogoutTimer;
		private Timer m_CombatTimer;
		private Timer m_ManaTimer, m_HitsTimer, m_StamTimer;
		private DateTime m_NextSkillTime;
		private DateTime m_NextActionTime;
		private DateTime m_NextActionMessage;
		private bool m_Paralyzed;
		private bool _Sleep;
		private ParalyzedTimer m_ParaTimer;
		private SleepTimer _SleepTimer;
		private bool m_Frozen;
		private FrozenTimer m_FrozenTimer;
		private int m_AllowedStealthSteps;
		private int m_Hunger;
		private int m_NameHue = -1;
		private Region m_Region;
		private bool m_DisarmReady, m_StunReady;
		private int m_BaseSoundID;
		private int m_VirtualArmor;
		private bool m_Squelched;
		private int m_MeleeDamageAbsorb;
		private int m_MagicDamageAbsorb;
		private int m_Followers, m_FollowersMax;
		private List<object> _actions; // prefer List<object> over ArrayList for more specific profiling information
		private Queue<MovementRecord> m_MoveRecords;
		private int m_WarmodeChanges = 0;
		private DateTime m_NextWarmodeChange;
		private WarmodeTimer m_WarmodeTimer;
		private int m_Thirst, m_BAC;
		private int m_VirtualArmorMod;
		private VirtueInfo m_Virtues;
		private object m_Party;
		private List<SkillMod> m_SkillMods;
		private Body m_BodyMod;
		private DateTime m_LastStrGain;
		private DateTime m_LastIntGain;
		private DateTime m_LastDexGain;
		private Race m_Race;

		#endregion

		private static readonly TimeSpan WarmodeSpamCatch = TimeSpan.FromSeconds((Core.SE ? 1.0 : 0.5));
		private static readonly TimeSpan WarmodeSpamDelay = TimeSpan.FromSeconds((Core.SE ? 4.0 : 2.0));
		private const int WarmodeCatchCount = 4; // Allow four warmode changes in 0.5 seconds, any more will be delay for two seconds

		[CommandProperty(AccessLevel.Decorator)]
		public Race Race
		{
			get
			{
				if (this.m_Race == null)
					this.m_Race = Race.DefaultRace;

				return this.m_Race;
			}
			set
			{
				Race oldRace = this.Race;

				this.m_Race = value;

				if (this.m_Race == null)
					this.m_Race = Race.DefaultRace;

				this.Body = this.m_Race.Body(this);
				this.UpdateResistances();

				this.Delta(MobileDelta.Race);

				this.OnRaceChange(oldRace);
			}
		}

		protected virtual void OnRaceChange(Race oldRace)
		{
		}

		public virtual double RacialSkillBonus
		{
			get
			{
				return 0;
			}
		}

		private List<ResistanceMod> m_ResistMods;

		private int[] m_Resistances;

		public int[] Resistances
		{
			get
			{
				return this.m_Resistances;
			}
		}

		public virtual int BasePhysicalResistance
		{
			get
			{
				return 0;
			}
		}
		public virtual int BaseFireResistance
		{
			get
			{
				return 0;
			}
		}
		public virtual int BaseColdResistance
		{
			get
			{
				return 0;
			}
		}
		public virtual int BasePoisonResistance
		{
			get
			{
				return 0;
			}
		}
		public virtual int BaseEnergyResistance
		{
			get
			{
				return 0;
			}
		}

		public virtual void ComputeLightLevels(out int global, out int personal)
		{
			this.ComputeBaseLightLevels(out global, out personal);

			if (this.m_Region != null)
				this.m_Region.AlterLightLevel(this, ref global, ref personal);
		}

		public virtual void ComputeBaseLightLevels(out int global, out int personal)
		{
			global = 0;
			personal = this.m_LightLevel;
		}

		public virtual void CheckLightLevels(bool forceResend)
		{
		}

		[CommandProperty(AccessLevel.Counselor)]
		public virtual int PhysicalResistance
		{
			get
			{
				return this.GetResistance(ResistanceType.Physical);
			}
		}

		[CommandProperty(AccessLevel.Counselor)]
		public virtual int FireResistance
		{
			get
			{
				return this.GetResistance(ResistanceType.Fire);
			}
		}

		[CommandProperty(AccessLevel.Counselor)]
		public virtual int ColdResistance
		{
			get
			{
				return this.GetResistance(ResistanceType.Cold);
			}
		}

		[CommandProperty(AccessLevel.Counselor)]
		public virtual int PoisonResistance
		{
			get
			{
				return this.GetResistance(ResistanceType.Poison);
			}
		}

		[CommandProperty(AccessLevel.Counselor)]
		public virtual int EnergyResistance
		{
			get
			{
				return this.GetResistance(ResistanceType.Energy);
			}
		}

		public virtual void UpdateResistances()
		{
			if (this.m_Resistances == null)
				this.m_Resistances = new int[5] { int.MinValue, int.MinValue, int.MinValue, int.MinValue, int.MinValue };

			bool delta = false;

			for (int i = 0; i < this.m_Resistances.Length; ++i)
			{
				if (this.m_Resistances[i] != int.MinValue)
				{
					this.m_Resistances[i] = int.MinValue;
					delta = true;
				}
			}

			if (delta)
				this.Delta(MobileDelta.Resistances);
		}

		public virtual int GetResistance(ResistanceType type)
		{
			if (this.m_Resistances == null)
				this.m_Resistances = new int[5] { int.MinValue, int.MinValue, int.MinValue, int.MinValue, int.MinValue };

			int v = (int)type;

			if (v < 0 || v >= this.m_Resistances.Length)
				return 0;

			int res = this.m_Resistances[v];

			if (res == int.MinValue)
			{
				this.ComputeResistances();
				res = this.m_Resistances[v];
			}

			return res;
		}

		public List<ResistanceMod> ResistanceMods
		{
			get
			{
				return this.m_ResistMods;
			}
			set
			{
				this.m_ResistMods = value;
			}
		}

		public virtual void AddResistanceMod(ResistanceMod toAdd)
		{
			if (this.m_ResistMods == null)
			{
				this.m_ResistMods = new List<ResistanceMod>();
			}

			this.m_ResistMods.Add(toAdd);
			this.UpdateResistances();
		}

		public virtual void RemoveResistanceMod(ResistanceMod toRemove)
		{
			if (this.m_ResistMods != null)
			{
				this.m_ResistMods.Remove(toRemove);

				if (this.m_ResistMods.Count == 0)
					this.m_ResistMods = null;
			}

			this.UpdateResistances();
		}

		private static int m_MaxPlayerResistance = 70;

		public static int MaxPlayerResistance
		{
			get
			{
				return m_MaxPlayerResistance;
			}
			set
			{
				m_MaxPlayerResistance = value;
			}
		}

		public virtual void ComputeResistances()
		{
			if (this.m_Resistances == null)
				this.m_Resistances = new int[5] { int.MinValue, int.MinValue, int.MinValue, int.MinValue, int.MinValue };

			for (int i = 0; i < this.m_Resistances.Length; ++i)
				this.m_Resistances[i] = 0;

			this.m_Resistances[0] += this.BasePhysicalResistance;
			this.m_Resistances[1] += this.BaseFireResistance;
			this.m_Resistances[2] += this.BaseColdResistance;
			this.m_Resistances[3] += this.BasePoisonResistance;
			this.m_Resistances[4] += this.BaseEnergyResistance;

			for (int i = 0; this.m_ResistMods != null && i < this.m_ResistMods.Count; ++i)
			{
				ResistanceMod mod = this.m_ResistMods[i];
				int v = (int)mod.Type;

				if (v >= 0 && v < this.m_Resistances.Length)
					this.m_Resistances[v] += mod.Offset;
			}

			for (int i = 0; i < this.m_Items.Count; ++i)
			{
				Item item = this.m_Items[i];

				if (item.CheckPropertyConfliction(this))
					continue;

				this.m_Resistances[0] += item.PhysicalResistance;
				this.m_Resistances[1] += item.FireResistance;
				this.m_Resistances[2] += item.ColdResistance;
				this.m_Resistances[3] += item.PoisonResistance;
				this.m_Resistances[4] += item.EnergyResistance;
			}

			for (int i = 0; i < this.m_Resistances.Length; ++i)
			{
				int min = this.GetMinResistance((ResistanceType)i);
				int max = this.GetMaxResistance((ResistanceType)i);

				if (max < min)
					max = min;

				if (this.m_Resistances[i] > max)
					this.m_Resistances[i] = max;
				else if (this.m_Resistances[i] < min)
					this.m_Resistances[i] = min;
			}
		}

		public virtual int GetMinResistance(ResistanceType type)
		{
			return int.MinValue;
		}

		public virtual int GetMaxResistance(ResistanceType type)
		{
			if (this.m_Player)
				return m_MaxPlayerResistance;

			return int.MaxValue;
		}

		public virtual void SendPropertiesTo(Mobile from)
		{
			from.Send(this.PropertyList);
		}

		public virtual void OnAosSingleClick(Mobile from)
		{
			ObjectPropertyList opl = this.PropertyList;

			if (opl.Header > 0)
			{
				int hue;

				if (this.m_NameHue != -1)
					hue = this.m_NameHue;
				else if (this.IsStaff())
					hue = 11;
				else
					hue = Notoriety.GetHue(Notoriety.Compute(from, this));

				from.Send(new MessageLocalized(this.m_Serial, this.Body, MessageType.Label, hue, 3, opl.Header, this.Name, opl.HeaderArgs));
			}
		}

		public virtual string ApplyNameSuffix(string suffix)
		{
			return suffix;
		}

		public virtual void AddNameProperties(ObjectPropertyList list)
		{
			string name = this.Name;

			if (name == null)
				name = String.Empty;

			string prefix = "";

			if (this.ShowFameTitle && (this.m_Player || this.m_Body.IsHuman) && this.m_Fame >= 10000)
				prefix = this.m_Female ? "Lady" : "Lord";

			string suffix = "";

			if (this.PropertyTitle && this.Title != null && this.Title.Length > 0)
				suffix = this.Title;

			BaseGuild guild = this.m_Guild;

			if (guild != null && (this.m_Player || this.m_DisplayGuildTitle))
			{
				if (suffix.Length > 0)
					suffix = String.Format("{0} [{1}]", suffix, Utility.FixHtml(guild.Abbreviation));
				else
					suffix = String.Format("[{0}]", Utility.FixHtml(guild.Abbreviation));
			}

			suffix = this.ApplyNameSuffix(suffix);

			list.Add(1050045, "{0} \t{1}\t {2}", prefix, name, suffix); // ~1_PREFIX~~2_NAME~~3_SUFFIX~

			if (guild != null && (this.m_DisplayGuildTitle || (this.m_Player && guild.Type != GuildType.Regular)))
			{
				string type;

				if (guild.Type >= 0 && (int)guild.Type < m_GuildTypes.Length)
					type = m_GuildTypes[(int)guild.Type];
				else
					type = "";

				string title = this.GuildTitle;

				if (title == null)
					title = "";
				else
					title = title.Trim();

				if (this.NewGuildDisplay && title.Length > 0)
				{
					list.Add("{0}, {1}", Utility.FixHtml(title), Utility.FixHtml(guild.Name));
				}
				else
				{
					if (title.Length > 0)
						list.Add("{0}, {1} Guild{2}", Utility.FixHtml(title), Utility.FixHtml(guild.Name), type);
					else
						list.Add(Utility.FixHtml(guild.Name));
				}
			}
		}

		public virtual bool NewGuildDisplay
		{
			get
			{
				return false;
			}
		}

		public virtual void GetProperties(ObjectPropertyList list)
		{
			this.AddNameProperties(list);
		}

		public virtual void GetChildProperties(ObjectPropertyList list, Item item)
		{
		}

		public virtual void GetChildNameProperties(ObjectPropertyList list, Item item)
		{
		}

		private void UpdateAggrExpire()
		{
			if (this.m_Deleted || (this.m_Aggressors.Count == 0 && this.m_Aggressed.Count == 0))
			{
				this.StopAggrExpire();
			}
			else if (this.m_ExpireAggrTimer == null)
			{
				this.m_ExpireAggrTimer = new ExpireAggressorsTimer(this);
				this.m_ExpireAggrTimer.Start();
			}
		}

		private void StopAggrExpire()
		{
			if (this.m_ExpireAggrTimer != null)
				this.m_ExpireAggrTimer.Stop();

			this.m_ExpireAggrTimer = null;
		}

		private void CheckAggrExpire()
		{
			for (int i = this.m_Aggressors.Count - 1; i >= 0; --i)
			{
				if (i >= this.m_Aggressors.Count)
					continue;

				AggressorInfo info = this.m_Aggressors[i];

				if (info.Expired)
				{
					Mobile attacker = info.Attacker;
					attacker.RemoveAggressed(this);

					this.m_Aggressors.RemoveAt(i);
					info.Free();

					if (this.m_NetState != null && this.CanSee(attacker) && Utility.InUpdateRange(this.m_Location, attacker.m_Location))
					{
						if (this.m_NetState.StygianAbyss)
						{
							this.m_NetState.Send(new MobileIncoming(this, attacker));
						}
						else
						{
							this.m_NetState.Send(new MobileIncomingOld(this, attacker));
						}
					}
				}
			}

			for (int i = this.m_Aggressed.Count - 1; i >= 0; --i)
			{
				if (i >= this.m_Aggressed.Count)
					continue;

				AggressorInfo info = this.m_Aggressed[i];

				if (info.Expired)
				{
					Mobile defender = info.Defender;
					defender.RemoveAggressor(this);

					this.m_Aggressed.RemoveAt(i);
					info.Free();

					if (this.m_NetState != null && this.CanSee(defender) && Utility.InUpdateRange(this.m_Location, defender.m_Location))
					{
						if (this.m_NetState.StygianAbyss)
						{
							this.m_NetState.Send(new MobileIncoming(this, defender));
						}
						else
						{
							this.m_NetState.Send(new MobileIncomingOld(this, defender));
						}
					}
				}
			}

			this.UpdateAggrExpire();
		}

		public List<Mobile> Stabled
		{
			get
			{
				return this.m_Stabled;
			}
		}

		[CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
		public VirtueInfo Virtues
		{
			get
			{
				return this.m_Virtues;
			}
			set
			{
			}
		}

		public object Party
		{
			get
			{
				return this.m_Party;
			}
			set
			{
				this.m_Party = value;
			}
		}
		public List<SkillMod> SkillMods
		{
			get
			{
				return this.m_SkillMods;
			}
		}

		[CommandProperty(AccessLevel.GameMaster)]
		public int VirtualArmorMod
		{
			get
			{
				return this.m_VirtualArmorMod;
			}
			set
			{
				if (this.m_VirtualArmorMod != value)
				{
					this.m_VirtualArmorMod = value;

					this.Delta(MobileDelta.Armor);
				}
			}
		}

		/// <summary>
		/// Overridable. Virtual event invoked when <paramref name="skill" /> changes in some way.
		/// </summary>
		public virtual void OnSkillInvalidated(Skill skill)
		{
		}

		public virtual void UpdateSkillMods()
		{
			this.ValidateSkillMods();

			for (int i = 0; i < this.m_SkillMods.Count; ++i)
			{
				SkillMod mod = this.m_SkillMods[i];

				Skill sk = this.m_Skills[mod.Skill];

				if (sk != null)
					sk.Update();
			}
		}

		public virtual void ValidateSkillMods()
		{
			for (int i = 0; i < this.m_SkillMods.Count;)
			{
				SkillMod mod = this.m_SkillMods[i];

				if (mod.CheckCondition())
					++i;
				else
					this.InternalRemoveSkillMod(mod);
			}
		}

		public virtual void AddSkillMod(SkillMod mod)
		{
			if (mod == null)
				return;

			this.ValidateSkillMods();

			if (!this.m_SkillMods.Contains(mod))
			{
				this.m_SkillMods.Add(mod);
				mod.Owner = this;

				Skill sk = this.m_Skills[mod.Skill];

				if (sk != null)
					sk.Update();
			}
		}

		public virtual void RemoveSkillMod(SkillMod mod)
		{
			if (mod == null)
				return;

			this.ValidateSkillMods();

			this.InternalRemoveSkillMod(mod);
		}

		private void InternalRemoveSkillMod(SkillMod mod)
		{
			if (this.m_SkillMods.Contains(mod))
			{
				this.m_SkillMods.Remove(mod);
				mod.Owner = null;

				Skill sk = this.m_Skills[mod.Skill];

				if (sk != null)
					sk.Update();
			}
		}

		private class WarmodeTimer : Timer
		{
			private readonly Mobile m_Mobile;
			private bool m_Value;

			public bool Value
			{
				get
				{
					return this.m_Value;
				}
				set
				{
					this.m_Value = value;
				}
			}

			public WarmodeTimer(Mobile m, bool value)
				: base(WarmodeSpamDelay)
			{
				this.m_Mobile = m;
				this.m_Value = value;
			}

			protected override void OnTick()
			{
				this.m_Mobile.Warmode = this.m_Value;
				this.m_Mobile.m_WarmodeChanges = 0;

				this.m_Mobile.m_WarmodeTimer = null;
			}
		}

		/// <summary>
		/// Overridable. Virtual event invoked when a client, <paramref name="from" />, invokes a 'help request' for the Mobile. Seemingly no longer functional in newer clients.
		/// </summary>
		public virtual void OnHelpRequest(Mobile from)
		{
		}

		public void DelayChangeWarmode(bool value)
		{
			if (this.m_WarmodeTimer != null)
			{
				this.m_WarmodeTimer.Value = value;
				return;
			}

			if (this.m_Warmode == value)
				return;

			DateTime now = DateTime.Now, next = this.m_NextWarmodeChange;

			if (now > next || this.m_WarmodeChanges == 0)
			{
				this.m_WarmodeChanges = 1;
				this.m_NextWarmodeChange = now + WarmodeSpamCatch;
			}
			else if (this.m_WarmodeChanges == WarmodeCatchCount)
			{
				this.m_WarmodeTimer = new WarmodeTimer(this, value);
				this.m_WarmodeTimer.Start();

				return;
			}
			else
			{
				++this.m_WarmodeChanges;
			}

			this.Warmode = value;
		}

		[CommandProperty(AccessLevel.GameMaster)]
		public int MeleeDamageAbsorb
		{
			get
			{
				return this.m_MeleeDamageAbsorb;
			}
			set
			{
				this.m_MeleeDamageAbsorb = value;
			}
		}

		[CommandProperty(AccessLevel.GameMaster)]
		public int MagicDamageAbsorb
		{
			get
			{
				return this.m_MagicDamageAbsorb;
			}
			set
			{
				this.m_MagicDamageAbsorb = value;
			}
		}

		[CommandProperty(AccessLevel.GameMaster)]
		public int SkillsTotal
		{
			get
			{
				return this.m_Skills == null ? 0 : this.m_Skills.Total;
			}
		}

		[CommandProperty(AccessLevel.GameMaster)]
		public int SkillsCap
		{
			get
			{
				return this.m_Skills == null ? 0 : this.m_Skills.Cap;
			}
			set
			{
				if (this.m_Skills != null)
					this.m_Skills.Cap = value;
			}
		}

		public bool InLOS(Mobile target)
		{
			if (this.m_Deleted || this.m_Map == null)
				return false;
			else if (target == this || this.IsStaff())
				return true;

			return this.m_Map.LineOfSight(this, target);
		}

		public bool InLOS(object target)
		{
			if (this.m_Deleted || this.m_Map == null)
				return false;
			else if (target == this || this.IsStaff())
				return true;
			else if (target is Item && ((Item)target).RootParent == this)
				return true;

			return this.m_Map.LineOfSight(this, target);
		}

		public bool InLOS(Point3D target)
		{
			if (this.m_Deleted || this.m_Map == null)
				return false;
			else if (this.IsStaff())
				return true;

			return this.m_Map.LineOfSight(this, target);
		}

		[CommandProperty(AccessLevel.GameMaster)]
		public int BaseSoundID
		{
			get
			{
				return this.m_BaseSoundID;
			}
			set
			{
				this.m_BaseSoundID = value;
			}
		}

		public DateTime NextCombatTime
		{
			get
			{
				return this.m_NextCombatTime;
			}
			set
			{
				this.m_NextCombatTime = value;
			}
		}

		public bool BeginAction(object toLock)
		{
			if (this._actions == null)
			{
				this._actions = new List<object>();

				this._actions.Add(toLock);

				return true;
			}
			else if (!this._actions.Contains(toLock))
			{
				this._actions.Add(toLock);

				return true;
			}

			return false;
		}

		public bool CanBeginAction(object toLock)
		{
			return (this._actions == null || !this._actions.Contains(toLock));
		}

		public void EndAction(object toLock)
		{
			if (this._actions != null)
			{
				this._actions.Remove(toLock);

				if (this._actions.Count == 0)
				{
					this._actions = null;
				}
			}
		}

		[CommandProperty(AccessLevel.Decorator)]
		public int NameHue
		{
			get
			{
				return this.m_NameHue;
			}
			set
			{
				this.m_NameHue = value;
			}
		}

		[CommandProperty(AccessLevel.GameMaster)]
		public int Hunger
		{
			get
			{
				return this.m_Hunger;
			}
			set
			{
				int oldValue = this.m_Hunger;

				if (oldValue != value)
				{
					this.m_Hunger = value;

					EventSink.InvokeHungerChanged(new HungerChangedEventArgs(this, oldValue));
				}
			}
		}

		[CommandProperty(AccessLevel.GameMaster)]
		public int Thirst
		{
			get
			{
				return this.m_Thirst;
			}
			set
			{
				this.m_Thirst = value;
			}
		}

		[CommandProperty(AccessLevel.Decorator)]
		public int BAC
		{
			get
			{
				return this.m_BAC;
			}
			set
			{
				this.m_BAC = value;
			}
		}

		private DateTime m_LastMoveTime;

		/// <summary>
		/// Gets or sets the number of steps this player may take when hidden before being revealed.
		/// </summary>
		[CommandProperty(AccessLevel.GameMaster)]
		public int AllowedStealthSteps
		{
			get
			{
				return this.m_AllowedStealthSteps;
			}
			set
			{
				this.m_AllowedStealthSteps = value;
			}
		}

		/* Logout:
		* 
		* When a client logs into mobile x
		*  - if ( x is Internalized ) move x to logout location and map
		* 
		* When a client attached to a mobile disconnects
		*  - LogoutTimer is started
		*	   - Delay is taken from Region.GetLogoutDelay to allow insta-logout regions.
		*     - OnTick : Location and map are stored, and mobile is internalized
		* 
		* Some things to consider:
		*  - An internalized person getting killed (say, by poison). Where does the body go?
		*  - Regions now have a GetLogoutDelay( Mobile m ); virtual function (see above)
		*/
		private Point3D m_LogoutLocation;
		private Map m_LogoutMap;

		public virtual TimeSpan GetLogoutDelay()
		{
			return this.Region.GetLogoutDelay(this);
		}

		private StatLockType m_StrLock, m_DexLock, m_IntLock;

		private Item m_Holding;

		public Item Holding
		{
			get
			{
				return this.m_Holding;
			}
			set
			{
				if (this.m_Holding != value)
				{
					if (this.m_Holding != null)
					{
						this.UpdateTotal(this.m_Holding, TotalType.Weight, -(this.m_Holding.TotalWeight + this.m_Holding.PileWeight));

						if (this.m_Holding.HeldBy == this)
							this.m_Holding.HeldBy = null;
					}

					if (value != null && this.m_Holding != null)
						this.DropHolding();

					this.m_Holding = value;

					if (this.m_Holding != null)
					{
						this.UpdateTotal(this.m_Holding, TotalType.Weight, this.m_Holding.TotalWeight + this.m_Holding.PileWeight);

						if (this.m_Holding.HeldBy == null)
							this.m_Holding.HeldBy = this;
					}
				}
			}
		}

		public DateTime LastMoveTime
		{
			get
			{
				return this.m_LastMoveTime;
			}
			set
			{
				this.m_LastMoveTime = value;
			}
		}

		[CommandProperty(AccessLevel.GameMaster)]
		public virtual bool Paralyzed
		{
			get
			{
				return this.m_Paralyzed;
			}
			set
			{
				if (this.m_Paralyzed != value)
				{
					this.m_Paralyzed = value;
					this.Delta(MobileDelta.Flags);

					this.SendLocalizedMessage(this.m_Paralyzed ? 502381 : 502382);

					if (this.m_ParaTimer != null)
					{
						this.m_ParaTimer.Stop();
						this.m_ParaTimer = null;
					}
				}
			}
		}

		[CommandProperty(AccessLevel.GameMaster)]
		public virtual bool Asleep
		{
			get
			{
				return this._Sleep;
			}
			set
			{
				if (this._Sleep != value)
				{
					this._Sleep = value;

					if (this._SleepTimer != null)
					{
						this.Send(SpeedControl.Disable);
						this._SleepTimer.Stop();
						this._SleepTimer = null;
					}
				}
			}
		}

		[CommandProperty(AccessLevel.GameMaster)]
		public bool DisarmReady
		{
			get
			{
				return this.m_DisarmReady;
			}
			set
			{
				this.m_DisarmReady = value;
				//SendLocalizedMessage( value ? 1019013 : 1019014 );
			}
		}

		[CommandProperty(AccessLevel.GameMaster)]
		public bool StunReady
		{
			get
			{
				return this.m_StunReady;
			}
			set
			{
				this.m_StunReady = value;
				//SendLocalizedMessage( value ? 1019011 : 1019012 );
			}
		}

		[CommandProperty(AccessLevel.Decorator)]
		public bool Frozen
		{
			get
			{
				return this.m_Frozen;
			}
			set
			{
				if (this.m_Frozen != value)
				{
					this.m_Frozen = value;
					this.Delta(MobileDelta.Flags);

					if (this.m_FrozenTimer != null)
					{
						this.m_FrozenTimer.Stop();
						this.m_FrozenTimer = null;
					}
				}
			}
		}

		public void Paralyze(TimeSpan duration)
		{
			if (!this.m_Paralyzed)
			{
				this.Paralyzed = true;

				this.m_ParaTimer = new ParalyzedTimer(this, duration);
				this.m_ParaTimer.Start();
			}
		}

		public void Sleep(TimeSpan duration)
		{
			if (!this._Sleep)
			{
				this.Asleep = true;
				this.Send(SpeedControl.WalkSpeed);

				this._SleepTimer = new SleepTimer(this, duration);
				this._SleepTimer.Start();
			}
		}

		public void Freeze(TimeSpan duration)
		{
			if (!this.m_Frozen)
			{
				this.Frozen = true;

				this.m_FrozenTimer = new FrozenTimer(this, duration);
				this.m_FrozenTimer.Start();
			}
		}

		/// <summary>
		/// Gets or sets the <see cref="StatLockType">lock state</see> for the <see cref="RawStr" /> property.
		/// </summary>
		[CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
		public StatLockType StrLock
		{
			get
			{
				return this.m_StrLock;
			}
			set
			{
				if (this.m_StrLock != value)
				{
					this.m_StrLock = value;

					if (this.m_NetState != null)
						this.m_NetState.Send(new StatLockInfo(this));
				}
			}
		}

		/// <summary>
		/// Gets or sets the <see cref="StatLockType">lock state</see> for the <see cref="RawDex" /> property.
		/// </summary>
		[CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
		public StatLockType DexLock
		{
			get
			{
				return this.m_DexLock;
			}
			set
			{
				if (this.m_DexLock != value)
				{
					this.m_DexLock = value;

					if (this.m_NetState != null)
						this.m_NetState.Send(new StatLockInfo(this));
				}
			}
		}

		/// <summary>
		/// Gets or sets the <see cref="StatLockType">lock state</see> for the <see cref="RawInt" /> property.
		/// </summary>
		[CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
		public StatLockType IntLock
		{
			get
			{
				return this.m_IntLock;
			}
			set
			{
				if (this.m_IntLock != value)
				{
					this.m_IntLock = value;

					if (this.m_NetState != null)
						this.m_NetState.Send(new StatLockInfo(this));
				}
			}
		}

		public override string ToString()
		{
			return String.Format("0x{0:X} \"{1}\"", this.m_Serial.Value, this.Name);
		}

		public DateTime NextActionTime
		{
			get
			{
				return this.m_NextActionTime;
			}
			set
			{
				this.m_NextActionTime = value;
			}
		}

		public DateTime NextActionMessage
		{
			get
			{
				return this.m_NextActionMessage;
			}
			set
			{
				this.m_NextActionMessage = value;
			}
		}

		private static TimeSpan m_ActionMessageDelay = TimeSpan.FromSeconds(0.125);

		public static TimeSpan ActionMessageDelay
		{
			get
			{
				return m_ActionMessageDelay;
			}
			set
			{
				m_ActionMessageDelay = value;
			}
		}

		public virtual void SendSkillMessage()
		{
			if (DateTime.Now < this.m_NextActionMessage)
				return;

			this.m_NextActionMessage = DateTime.Now + m_ActionMessageDelay;

			this.SendLocalizedMessage(500118); // You must wait a few moments to use another skill.
		}

		public virtual void SendActionMessage()
		{
			if (DateTime.Now < this.m_NextActionMessage)
				return;

			this.m_NextActionMessage = DateTime.Now + m_ActionMessageDelay;

			this.SendLocalizedMessage(500119); // You must wait to perform another action.
		}

		public virtual void ClearHands()
		{
			this.ClearHand(this.FindItemOnLayer(Layer.OneHanded));
			this.ClearHand(this.FindItemOnLayer(Layer.TwoHanded));
		}

		public virtual void ClearHand(Item item)
		{
			if (item != null && item.Movable && !item.AllowEquipedCast(this))
			{
				Container pack = this.Backpack;

				if (pack == null)
					this.AddToBackpack(item);
				else
					pack.DropItem(item);
			}
		}

		private static bool m_GlobalRegenThroughPoison = true;

		public static bool GlobalRegenThroughPoison
		{
			get
			{
				return m_GlobalRegenThroughPoison;
			}
			set
			{
				m_GlobalRegenThroughPoison = value;
			}
		}

		public virtual bool RegenThroughPoison
		{
			get
			{
				return m_GlobalRegenThroughPoison;
			}
		}

		public virtual bool CanRegenHits
		{
			get
			{
				return this.Alive && (this.RegenThroughPoison || !this.Poisoned);
			}
		}
		public virtual bool CanRegenStam
		{
			get
			{
				return this.Alive;
			}
		}
		public virtual bool CanRegenMana
		{
			get
			{
				return this.Alive;
			}
		}

		#region Timers

		private class ManaTimer : Timer
		{
			private readonly Mobile m_Owner;

			public ManaTimer(Mobile m)
				: base(Mobile.GetManaRegenRate(m), Mobile.GetManaRegenRate(m))
			{
				this.Priority = TimerPriority.FiftyMS;
				this.m_Owner = m;
			}

			protected override void OnTick()
			{
				if (this.m_Owner.CanRegenMana)// m_Owner.Alive )
					this.m_Owner.Mana++;

				this.Delay = this.Interval = Mobile.GetManaRegenRate(this.m_Owner);
			}
		}

		private class HitsTimer : Timer
		{
			private readonly Mobile m_Owner;

			public HitsTimer(Mobile m)
				: base(Mobile.GetHitsRegenRate(m), Mobile.GetHitsRegenRate(m))
			{
				this.Priority = TimerPriority.FiftyMS;
				this.m_Owner = m;
			}

			protected override void OnTick()
			{
				if (this.m_Owner.CanRegenHits)// m_Owner.Alive && !m_Owner.Poisoned )
					this.m_Owner.Hits++;

				this.Delay = this.Interval = Mobile.GetHitsRegenRate(this.m_Owner);
			}
		}

		private class StamTimer : Timer
		{
			private readonly Mobile m_Owner;

			public StamTimer(Mobile m)
				: base(Mobile.GetStamRegenRate(m), Mobile.GetStamRegenRate(m))
			{
				this.Priority = TimerPriority.FiftyMS;
				this.m_Owner = m;
			}

			protected override void OnTick()
			{
				if (this.m_Owner.CanRegenStam)// m_Owner.Alive )
					this.m_Owner.Stam++;

				this.Delay = this.Interval = Mobile.GetStamRegenRate(this.m_Owner);
			}
		}

		private class LogoutTimer : Timer
		{
			private readonly Mobile m_Mobile;

			public LogoutTimer(Mobile m)
				: base(TimeSpan.FromDays(1.0))
			{
				this.Priority = TimerPriority.OneSecond;
				this.m_Mobile = m;
			}

			protected override void OnTick()
			{
				if (this.m_Mobile.m_Map != Map.Internal)
				{
					EventSink.InvokeLogout(new LogoutEventArgs(this.m_Mobile));

					this.m_Mobile.m_LogoutLocation = this.m_Mobile.m_Location;
					this.m_Mobile.m_LogoutMap = this.m_Mobile.m_Map;

					this.m_Mobile.Internalize();
				}
			}
		}

		private class ParalyzedTimer : Timer
		{
			private readonly Mobile m_Mobile;

			public ParalyzedTimer(Mobile m, TimeSpan duration)
				: base(duration)
			{
				this.Priority = TimerPriority.TwentyFiveMS;
				this.m_Mobile = m;
			}

			protected override void OnTick()
			{
				this.m_Mobile.Paralyzed = false;
			}
		}

		private class SleepTimer : Timer
		{
			private readonly Mobile _Mobile;

			public SleepTimer(Mobile m, TimeSpan duration)
				: base(duration)
			{
				this.Priority = TimerPriority.TwentyFiveMS;
				this._Mobile = m;
			}

			protected override void OnTick()
			{
				this._Mobile.Asleep = false;
			}
		}

		private class FrozenTimer : Timer
		{
			private readonly Mobile m_Mobile;

			public FrozenTimer(Mobile m, TimeSpan duration)
				: base(duration)
			{
				this.Priority = TimerPriority.TwentyFiveMS;
				this.m_Mobile = m;
			}

			protected override void OnTick()
			{
				this.m_Mobile.Frozen = false;
			}
		}

		private class CombatTimer : Timer
		{
			private readonly Mobile m_Mobile;

			public CombatTimer(Mobile m)
				: base(TimeSpan.FromSeconds(0.0), TimeSpan.FromSeconds(0.01), 0)
			{
				this.m_Mobile = m;

				if (!this.m_Mobile.m_Player && this.m_Mobile.m_Dex <= 100)
					this.Priority = TimerPriority.FiftyMS;
			}

			protected override void OnTick()
			{
				if (DateTime.Now > this.m_Mobile.m_NextCombatTime)
				{
					Mobile combatant = this.m_Mobile.Combatant;

					// If no combatant, wrong map, one of us is a ghost, or cannot see, or deleted, then stop combat
					if (combatant == null || combatant.m_Deleted || this.m_Mobile.m_Deleted || combatant.m_Map != this.m_Mobile.m_Map || !combatant.Alive || !this.m_Mobile.Alive || !this.m_Mobile.CanSee(combatant) || combatant.IsDeadBondedPet || this.m_Mobile.IsDeadBondedPet)
					{
						this.m_Mobile.Combatant = null;
						return;
					}

					IWeapon weapon = this.m_Mobile.Weapon;

					if (!this.m_Mobile.InRange(combatant, weapon.MaxRange))
						return;

					if (this.m_Mobile.InLOS(combatant))
					{
						weapon.OnBeforeSwing(this.m_Mobile, combatant);	//OnBeforeSwing for checking in regards to being hidden and whatnot
						this.m_Mobile.RevealingAction();
						this.m_Mobile.m_NextCombatTime = DateTime.Now + weapon.OnSwing(this.m_Mobile, combatant);
					}
				}
			}
		}

		private class ExpireCombatantTimer : Timer
		{
			private readonly Mobile m_Mobile;

			public ExpireCombatantTimer(Mobile m)
				: base(TimeSpan.FromMinutes(1.0))
			{
				this.Priority = TimerPriority.FiveSeconds;
				this.m_Mobile = m;
			}

			protected override void OnTick()
			{
				this.m_Mobile.Combatant = null;
			}
		}

		private static TimeSpan m_ExpireCriminalDelay = TimeSpan.FromMinutes(2.0);

		public static TimeSpan ExpireCriminalDelay
		{
			get
			{
				return m_ExpireCriminalDelay;
			}
			set
			{
				m_ExpireCriminalDelay = value;
			}
		}

		private class ExpireCriminalTimer : Timer
		{
			private readonly Mobile m_Mobile;

			public ExpireCriminalTimer(Mobile m)
				: base(m_ExpireCriminalDelay)
			{
				this.Priority = TimerPriority.FiveSeconds;
				this.m_Mobile = m;
			}

			protected override void OnTick()
			{
				this.m_Mobile.Criminal = false;
			}
		}

		private class ExpireAggressorsTimer : Timer
		{
			private readonly Mobile m_Mobile;

			public ExpireAggressorsTimer(Mobile m)
				: base(TimeSpan.FromSeconds(5.0), TimeSpan.FromSeconds(5.0))
			{
				this.m_Mobile = m;
				this.Priority = TimerPriority.FiveSeconds;
			}

			protected override void OnTick()
			{
				if (this.m_Mobile.Deleted || (this.m_Mobile.Aggressors.Count == 0 && this.m_Mobile.Aggressed.Count == 0))
					this.m_Mobile.StopAggrExpire();
				else
					this.m_Mobile.CheckAggrExpire();
			}
		}

		#endregion

		private DateTime m_NextCombatTime;

		public DateTime NextSkillTime
		{
			get
			{
				return this.m_NextSkillTime;
			}
			set
			{
				this.m_NextSkillTime = value;
			}
		}

		public List<AggressorInfo> Aggressors
		{
			get
			{
				return this.m_Aggressors;
			}
		}

		public List<AggressorInfo> Aggressed
		{
			get
			{
				return this.m_Aggressed;
			}
		}

		private int m_ChangingCombatant;

		public bool ChangingCombatant
		{
			get
			{
				return (this.m_ChangingCombatant > 0);
			}
		}

		public virtual void Attack(Mobile m)
		{
			if (this.CheckAttack(m))
				this.Combatant = m;
		}

		public virtual bool CheckAttack(Mobile m)
		{
			return (Utility.InUpdateRange(this, m) && this.CanSee(m) && this.InLOS(m));
		}

		/// <summary>
		/// Overridable. Gets or sets which Mobile that this Mobile is currently engaged in combat with.
		/// <seealso cref="OnCombatantChange" />
		/// </summary>
		[CommandProperty(AccessLevel.GameMaster)]
		public virtual Mobile Combatant
		{
			get
			{
				return this.m_Combatant;
			}
			set
			{
				if (this.m_Deleted)
					return;

				if (this.m_Combatant != value && value != this)
				{
					Mobile old = this.m_Combatant;

					++this.m_ChangingCombatant;
					this.m_Combatant = value;

					if ((this.m_Combatant != null && !this.CanBeHarmful(this.m_Combatant, false)) || !this.Region.OnCombatantChange(this, old, this.m_Combatant))
					{
						this.m_Combatant = old;
						--this.m_ChangingCombatant;
						return;
					}

					if (this.m_NetState != null)
						this.m_NetState.Send(new ChangeCombatant(this.m_Combatant));

					if (this.m_Combatant == null)
					{
						if (this.m_ExpireCombatant != null)
							this.m_ExpireCombatant.Stop();

						if (this.m_CombatTimer != null)
							this.m_CombatTimer.Stop();

						this.m_ExpireCombatant = null;
						this.m_CombatTimer = null;
					}
					else
					{
						if (this.m_ExpireCombatant == null)
							this.m_ExpireCombatant = new ExpireCombatantTimer(this);

						this.m_ExpireCombatant.Start();

						if (this.m_CombatTimer == null)
							this.m_CombatTimer = new CombatTimer(this);

						this.m_CombatTimer.Start();
					}

					if (this.m_Combatant != null && this.CanBeHarmful(this.m_Combatant, false))
					{
						this.DoHarmful(this.m_Combatant);

						if (this.m_Combatant != null)
							this.m_Combatant.PlaySound(this.m_Combatant.GetAngerSound());
					}

					this.OnCombatantChange();
					--this.m_ChangingCombatant;
				}
			}
		}

		/// <summary>
		/// Overridable. Virtual event invoked after the <see cref="Combatant" /> property has changed.
		/// <seealso cref="Combatant" />
		/// </summary>
		public virtual void OnCombatantChange()
		{
		}

		public double GetDistanceToSqrt(Point3D p)
		{
			int xDelta = this.m_Location.m_X - p.m_X;
			int yDelta = this.m_Location.m_Y - p.m_Y;

			return Math.Sqrt((xDelta * xDelta) + (yDelta * yDelta));
		}

		public double GetDistanceToSqrt(Mobile m)
		{
			int xDelta = this.m_Location.m_X - m.m_Location.m_X;
			int yDelta = this.m_Location.m_Y - m.m_Location.m_Y;

			return Math.Sqrt((xDelta * xDelta) + (yDelta * yDelta));
		}

		public double GetDistanceToSqrt(IPoint2D p)
		{
			int xDelta = this.m_Location.m_X - p.X;
			int yDelta = this.m_Location.m_Y - p.Y;

			return Math.Sqrt((xDelta * xDelta) + (yDelta * yDelta));
		}

		public virtual void AggressiveAction(Mobile aggressor)
		{
			this.AggressiveAction(aggressor, false);
		}

		public virtual void AggressiveAction(Mobile aggressor, bool criminal)
		{
			if (aggressor == this)
				return;

			AggressiveActionEventArgs args = AggressiveActionEventArgs.Create(this, aggressor, criminal);

			EventSink.InvokeAggressiveAction(args);

			args.Free();

			if (this.Combatant == aggressor)
			{
				if (this.m_ExpireCombatant == null)
					this.m_ExpireCombatant = new ExpireCombatantTimer(this);
				else
					this.m_ExpireCombatant.Stop();

				this.m_ExpireCombatant.Start();
			}

			bool addAggressor = true;

			List<AggressorInfo> list = this.m_Aggressors;

			for (int i = 0; i < list.Count; ++i)
			{
				AggressorInfo info = list[i];

				if (info.Attacker == aggressor)
				{
					info.Refresh();
					info.CriminalAggression = criminal;
					info.CanReportMurder = criminal;

					addAggressor = false;
				}
			}

			list = aggressor.m_Aggressors;

			for (int i = 0; i < list.Count; ++i)
			{
				AggressorInfo info = list[i];

				if (info.Attacker == this)
				{
					info.Refresh();

					addAggressor = false;
				}
			}

			bool addAggressed = true;

			list = this.m_Aggressed;

			for (int i = 0; i < list.Count; ++i)
			{
				AggressorInfo info = list[i];

				if (info.Defender == aggressor)
				{
					info.Refresh();

					addAggressed = false;
				}
			}

			list = aggressor.m_Aggressed;

			for (int i = 0; i < list.Count; ++i)
			{
				AggressorInfo info = list[i];

				if (info.Defender == this)
				{
					info.Refresh();
					info.CriminalAggression = criminal;
					info.CanReportMurder = criminal;

					addAggressed = false;
				}
			}

			bool setCombatant = false;

			if (addAggressor)
			{
				this.m_Aggressors.Add(AggressorInfo.Create(aggressor, this, criminal)); // new AggressorInfo( aggressor, this, criminal, true ) );

				if (this.CanSee(aggressor) && this.m_NetState != null)
				{
					if (this.m_NetState.StygianAbyss)
					{
						this.m_NetState.Send(new MobileIncoming(this, aggressor));
					}
					else
					{
						this.m_NetState.Send(new MobileIncomingOld(this, aggressor));
					}
				}

				if (this.Combatant == null)
					setCombatant = true;

				this.UpdateAggrExpire();
			}

			if (addAggressed)
			{
				aggressor.m_Aggressed.Add(AggressorInfo.Create(aggressor, this, criminal)); // new AggressorInfo( aggressor, this, criminal, false ) );

				if (this.CanSee(aggressor) && this.m_NetState != null)
				{
					if (this.m_NetState.StygianAbyss)
					{
						this.m_NetState.Send(new MobileIncoming(this, aggressor));
					}
					else
					{
						this.m_NetState.Send(new MobileIncomingOld(this, aggressor));
					}
				}

				if (this.Combatant == null)
					setCombatant = true;

				this.UpdateAggrExpire();
			}

			if (setCombatant)
				this.Combatant = aggressor;

			this.Region.OnAggressed(aggressor, this, criminal);
		}

		public void RemoveAggressed(Mobile aggressed)
		{
			if (this.m_Deleted)
				return;

			List<AggressorInfo> list = this.m_Aggressed;

			for (int i = 0; i < list.Count; ++i)
			{
				AggressorInfo info = list[i];

				if (info.Defender == aggressed)
				{
					this.m_Aggressed.RemoveAt(i);
					info.Free();

					if (this.m_NetState != null && this.CanSee(aggressed))
					{
						if (this.m_NetState.StygianAbyss)
						{
							this.m_NetState.Send(new MobileIncoming(this, aggressed));
						}
						else
						{
							this.m_NetState.Send(new MobileIncomingOld(this, aggressed));
						}
					}

					break;
				}
			}

			this.UpdateAggrExpire();
		}

		public void RemoveAggressor(Mobile aggressor)
		{
			if (this.m_Deleted)
				return;

			List<AggressorInfo> list = this.m_Aggressors;

			for (int i = 0; i < list.Count; ++i)
			{
				AggressorInfo info = list[i];

				if (info.Attacker == aggressor)
				{
					this.m_Aggressors.RemoveAt(i);
					info.Free();

					if (this.m_NetState != null && this.CanSee(aggressor))
					{
						if (this.m_NetState.StygianAbyss)
						{
							this.m_NetState.Send(new MobileIncoming(this, aggressor));
						}
						else
						{
							this.m_NetState.Send(new MobileIncomingOld(this, aggressor));
						}
					}

					break;
				}
			}

			this.UpdateAggrExpire();
		}

		[CommandProperty(AccessLevel.GameMaster)]
		public int TotalGold
		{
			get
			{
				return this.GetTotal(TotalType.Gold);
			}
		}

		[CommandProperty(AccessLevel.GameMaster)]
		public int TotalItems
		{
			get
			{
				return this.GetTotal(TotalType.Items);
			}
		}

		[CommandProperty(AccessLevel.GameMaster)]
		public int TotalWeight
		{
			get
			{
				return this.GetTotal(TotalType.Weight);
			}
		}

		[CommandProperty(AccessLevel.GameMaster)]
		public int TithingPoints
		{
			get
			{
				return this.m_TithingPoints;
			}
			set
			{
				if (this.m_TithingPoints != value)
				{
					this.m_TithingPoints = value;

					this.Delta(MobileDelta.TithingPoints);
				}
			}
		}

		[CommandProperty(AccessLevel.GameMaster)]
		public int Followers
		{
			get
			{
				return this.m_Followers;
			}
			set
			{
				if (this.m_Followers != value)
				{
					this.m_Followers = value;

					this.Delta(MobileDelta.Followers);
				}
			}
		}

		[CommandProperty(AccessLevel.GameMaster)]
		public int FollowersMax
		{
			get
			{
				return this.m_FollowersMax;
			}
			set
			{
				if (this.m_FollowersMax != value)
				{
					this.m_FollowersMax = value;

					this.Delta(MobileDelta.Followers);
				}
			}
		}

		public virtual int GetTotal(TotalType type)
		{
			switch( type )
			{
				case TotalType.Gold:
					return this.m_TotalGold;

				case TotalType.Items:
					return this.m_TotalItems;

				case TotalType.Weight:
					return this.m_TotalWeight;
			}

			return 0;
		}

		public virtual void UpdateTotal(Item sender, TotalType type, int delta)
		{
			if (delta == 0 || sender.IsVirtualItem)
				return;

			switch( type )
			{
				case TotalType.Gold:
					this.m_TotalGold += delta;
					this.Delta(MobileDelta.Gold);
					break;
				case TotalType.Items:
					this.m_TotalItems += delta;
					break;
				case TotalType.Weight:
					this.m_TotalWeight += delta;
					this.Delta(MobileDelta.Weight);
					this.OnWeightChange(this.m_TotalWeight - delta);
					break;
			}
		}

		public virtual void UpdateTotals()
		{
			if (this.m_Items == null)
				return;

			int oldWeight = this.m_TotalWeight;

			this.m_TotalGold = 0;
			this.m_TotalItems = 0;
			this.m_TotalWeight = 0;

			for (int i = 0; i < this.m_Items.Count; ++i)
			{
				Item item = this.m_Items[i];

				item.UpdateTotals();

				if (item.IsVirtualItem)
					continue;

				this.m_TotalGold += item.TotalGold;
				this.m_TotalItems += item.TotalItems + 1;
				this.m_TotalWeight += item.TotalWeight + item.PileWeight;
			}

			if (this.m_Holding != null)
				this.m_TotalWeight += this.m_Holding.TotalWeight + this.m_Holding.PileWeight;

			if (this.m_TotalWeight != oldWeight)
				this.OnWeightChange(oldWeight);
		}

		public void ClearQuestArrow()
		{
			this.m_QuestArrow = null;
		}

		public void ClearTarget()
		{
			this.m_Target = null;
		}

		private bool m_TargetLocked;

		public bool TargetLocked
		{
			get
			{
				return this.m_TargetLocked;
			}
			set
			{
				this.m_TargetLocked = value;
			}
		}

		private class SimpleTarget : Target
		{
			private readonly TargetCallback m_Callback;

			public SimpleTarget(int range, TargetFlags flags, bool allowGround, TargetCallback callback)
				: base(range, allowGround, flags)
			{
				this.m_Callback = callback;
			}

			protected override void OnTarget(Mobile from, object targeted)
			{
				if (this.m_Callback != null)
					this.m_Callback(from, targeted);
			}
		}

		public Target BeginTarget(int range, bool allowGround, TargetFlags flags, TargetCallback callback)
		{
			Target t = new SimpleTarget(range, flags, allowGround, callback);

			this.Target = t;

			return t;
		}

		private class SimpleStateTarget : Target
		{
			private readonly TargetStateCallback m_Callback;
			private readonly object m_State;

			public SimpleStateTarget(int range, TargetFlags flags, bool allowGround, TargetStateCallback callback, object state)
				: base(range, allowGround, flags)
			{
				this.m_Callback = callback;
				this.m_State = state;
			}

			protected override void OnTarget(Mobile from, object targeted)
			{
				if (this.m_Callback != null)
					this.m_Callback(from, targeted, this.m_State);
			}
		}

		public Target BeginTarget(int range, bool allowGround, TargetFlags flags, TargetStateCallback callback, object state)
		{
			Target t = new SimpleStateTarget(range, flags, allowGround, callback, state);

			this.Target = t;

			return t;
		}

		private class SimpleStateTarget<T> : Target
		{
			private readonly TargetStateCallback<T> m_Callback;
			private readonly T m_State;

			public SimpleStateTarget(int range, TargetFlags flags, bool allowGround, TargetStateCallback<T> callback, T state)
				: base(range, allowGround, flags)
			{
				this.m_Callback = callback;
				this.m_State = state;
			}

			protected override void OnTarget(Mobile from, object targeted)
			{
				if (this.m_Callback != null)
					this.m_Callback(from, targeted, this.m_State);
			}
		}
		public Target BeginTarget<T>(int range, bool allowGround, TargetFlags flags, TargetStateCallback<T> callback, T state)
		{
			Target t = new SimpleStateTarget<T>(range, flags, allowGround, callback, state);

			this.Target = t;

			return t;
		}

		public Target Target
		{
			get
			{
				return this.m_Target;
			}
			set
			{
				Target oldTarget = this.m_Target;
				Target newTarget = value;

				if (oldTarget == newTarget)
					return;

				this.m_Target = null;

				if (oldTarget != null && newTarget != null)
					oldTarget.Cancel(this, TargetCancelType.Overriden);

				this.m_Target = newTarget;

				if (newTarget != null && this.m_NetState != null && !this.m_TargetLocked)
					this.m_NetState.Send(newTarget.GetPacketFor(this.m_NetState));

				this.OnTargetChange();
			}
		}

		/// <summary>
		/// Overridable. Virtual event invoked after the <see cref="Target">Target property</see> has changed.
		/// </summary>
		protected virtual void OnTargetChange()
		{
		}

		public ContextMenu ContextMenu
		{
			get
			{
				return this.m_ContextMenu;
			}
			set
			{
				this.m_ContextMenu = value;

				if (this.m_ContextMenu != null)
					this.Send(new DisplayContextMenu(this.m_ContextMenu));
			}
		}

		public virtual bool CheckContextMenuDisplay(IEntity target)
		{
			return true;
		}

		#region Prompts
		private class SimplePrompt : Prompt
		{
			private readonly PromptCallback m_Callback;
			private readonly PromptCallback m_CancelCallback;
			private readonly bool m_CallbackHandlesCancel;

			public SimplePrompt(PromptCallback callback, PromptCallback cancelCallback)
			{
				this.m_Callback = callback;
				this.m_CancelCallback = cancelCallback;
			}

			public SimplePrompt(PromptCallback callback, bool callbackHandlesCancel)
			{
				this.m_Callback = callback;
				this.m_CallbackHandlesCancel = callbackHandlesCancel;
			}

			public SimplePrompt(PromptCallback callback)
				: this(callback, false)
			{
			}

			public override void OnResponse(Mobile from, string text)
			{
				if (this.m_Callback != null)
					this.m_Callback(from, text);
			}

			public override void OnCancel(Mobile from)
			{
				if (this.m_CallbackHandlesCancel && this.m_Callback != null)
					this.m_Callback(from, "");
				else if (this.m_CancelCallback != null)
					this.m_CancelCallback(from, "");
			}
		}
		public Prompt BeginPrompt(PromptCallback callback, PromptCallback cancelCallback)
		{
			Prompt p = new SimplePrompt(callback, cancelCallback);

			this.Prompt = p;
			return p;
		}

		public Prompt BeginPrompt(PromptCallback callback, bool callbackHandlesCancel)
		{
			Prompt p = new SimplePrompt(callback, callbackHandlesCancel);

			this.Prompt = p;
			return p;
		}

		public Prompt BeginPrompt(PromptCallback callback)
		{
			return this.BeginPrompt(callback, false);
		}

		private class SimpleStatePrompt : Prompt
		{
			private readonly PromptStateCallback m_Callback;
			private readonly PromptStateCallback m_CancelCallback;

			private readonly bool m_CallbackHandlesCancel;

			private readonly object m_State;

			public SimpleStatePrompt(PromptStateCallback callback, PromptStateCallback cancelCallback, object state)
			{
				this.m_Callback = callback;
				this.m_CancelCallback = cancelCallback;
				this.m_State = state;
			}

			public SimpleStatePrompt(PromptStateCallback callback, bool callbackHandlesCancel, object state)
			{
				this.m_Callback = callback;
				this.m_State = state;
				this.m_CallbackHandlesCancel = callbackHandlesCancel;
			}

			public SimpleStatePrompt(PromptStateCallback callback, object state)
				: this(callback, false, state)
			{
			}

			public override void OnResponse(Mobile from, string text)
			{
				if (this.m_Callback != null)
					this.m_Callback(from, text, this.m_State);
			}

			public override void OnCancel(Mobile from)
			{
				if (this.m_CallbackHandlesCancel && this.m_Callback != null)
					this.m_Callback(from, "", this.m_State);
				else if (this.m_CancelCallback != null)
					this.m_CancelCallback(from, "", this.m_State);
			}
		}
		public Prompt BeginPrompt(PromptStateCallback callback, PromptStateCallback cancelCallback, object state)
		{
			Prompt p = new SimpleStatePrompt(callback, cancelCallback, state);

			this.Prompt = p;
			return p;
		}

		public Prompt BeginPrompt(PromptStateCallback callback, bool callbackHandlesCancel, object state)
		{
			Prompt p = new SimpleStatePrompt(callback, callbackHandlesCancel, state);

			this.Prompt = p;
			return p;
		}

		public Prompt BeginPrompt(PromptStateCallback callback, object state)
		{
			return this.BeginPrompt(callback, false, state);
		}

		private class SimpleStatePrompt<T> : Prompt
		{
			private readonly PromptStateCallback<T> m_Callback;
			private readonly PromptStateCallback<T> m_CancelCallback;

			private readonly bool m_CallbackHandlesCancel;

			private readonly T m_State;

			public SimpleStatePrompt(PromptStateCallback<T> callback, PromptStateCallback<T> cancelCallback, T state)
			{
				this.m_Callback = callback;
				this.m_CancelCallback = cancelCallback;
				this.m_State = state;
			}

			public SimpleStatePrompt(PromptStateCallback<T> callback, bool callbackHandlesCancel, T state)
			{
				this.m_Callback = callback;
				this.m_State = state;
				this.m_CallbackHandlesCancel = callbackHandlesCancel;
			}

			public SimpleStatePrompt(PromptStateCallback<T> callback, T state)
				: this(callback, false, state)
			{
			}

			public override void OnResponse(Mobile from, string text)
			{
				if (this.m_Callback != null)
					this.m_Callback(from, text, this.m_State);
			}

			public override void OnCancel(Mobile from)
			{
				if (this.m_CallbackHandlesCancel && this.m_Callback != null)
					this.m_Callback(from, "", this.m_State);
				else if (this.m_CancelCallback != null)
					this.m_CancelCallback(from, "", this.m_State);
			}
		}
		public Prompt BeginPrompt<T>(PromptStateCallback<T> callback, PromptStateCallback<T> cancelCallback, T state)
		{
			Prompt p = new SimpleStatePrompt<T>(callback, cancelCallback, state);

			this.Prompt = p;
			return p;
		}

		public Prompt BeginPrompt<T>(PromptStateCallback<T> callback, bool callbackHandlesCancel, T state)
		{
			Prompt p = new SimpleStatePrompt<T>(callback, callbackHandlesCancel, state);

			this.Prompt = p;
			return p;
		}

		public Prompt BeginPrompt<T>(PromptStateCallback<T> callback, T state)
		{
			return this.BeginPrompt(callback, false, state);
		}

		public Prompt Prompt
		{
			get
			{
				return this.m_Prompt;
			}
			set
			{
				Prompt oldPrompt = this.m_Prompt;
				Prompt newPrompt = value;

				if (oldPrompt == newPrompt)
					return;

				this.m_Prompt = null;

				if (oldPrompt != null && newPrompt != null)
					oldPrompt.OnCancel(this);

				this.m_Prompt = newPrompt;

				if (newPrompt != null)
					this.Send(new UnicodePrompt(newPrompt));
			}
		}
		#endregion

		private bool InternalOnMove(Direction d)
		{
			if (!this.OnMove(d))
				return false;

			MovementEventArgs e = MovementEventArgs.Create(this, d);

			EventSink.InvokeMovement(e);

			bool ret = !e.Blocked;

			e.Free();

			return ret;
		}

		/// <summary>
		/// Overridable. Event invoked before the Mobile <see cref="Move">moves</see>.
		/// </summary>
		/// <returns>True if the move is allowed, false if not.</returns>
		protected virtual bool OnMove(Direction d)
		{
			if (this.m_Hidden && this.IsPlayer())
			{
				if (this.m_AllowedStealthSteps-- <= 0 || (d & Direction.Running) != 0 || this.Mounted)
					this.RevealingAction();
			}

			return true;
		}

		private static readonly Packet[][] m_MovingPacketCache = new Packet[2][]
		{
			new Packet[8],
			new Packet[8]
		};

		private bool m_Pushing;

		public bool Pushing
		{
			get
			{
				return this.m_Pushing;
			}
			set
			{
				this.m_Pushing = value;
			}
		}

		private static TimeSpan m_WalkFoot = TimeSpan.FromSeconds(0.4);
		private static TimeSpan m_RunFoot = TimeSpan.FromSeconds(0.2);
		private static TimeSpan m_WalkMount = TimeSpan.FromSeconds(0.2);
		private static TimeSpan m_RunMount = TimeSpan.FromSeconds(0.1);

		public static TimeSpan WalkFoot
		{
			get
			{
				return m_WalkFoot;
			}
			set
			{
				m_WalkFoot = value;
			}
		}
		public static TimeSpan RunFoot
		{
			get
			{
				return m_RunFoot;
			}
			set
			{
				m_RunFoot = value;
			}
		}
		public static TimeSpan WalkMount
		{
			get
			{
				return m_WalkMount;
			}
			set
			{
				m_WalkMount = value;
			}
		}
		public static TimeSpan RunMount
		{
			get
			{
				return m_RunMount;
			}
			set
			{
				m_RunMount = value;
			}
		}

		private DateTime m_EndQueue;

		private static readonly ArrayList m_MoveList = new ArrayList();

		private static AccessLevel m_FwdAccessOverride = AccessLevel.Counselor;
		private static bool m_FwdEnabled = true;
		private static bool m_FwdUOTDOverride = false;
		private static int m_FwdMaxSteps = 4;

		public static AccessLevel FwdAccessOverride
		{
			get
			{
				return m_FwdAccessOverride;
			}
			set
			{
				m_FwdAccessOverride = value;
			}
		}
		public static bool FwdEnabled
		{
			get
			{
				return m_FwdEnabled;
			}
			set
			{
				m_FwdEnabled = value;
			}
		}
		public static bool FwdUOTDOverride
		{
			get
			{
				return m_FwdUOTDOverride;
			}
			set
			{
				m_FwdUOTDOverride = value;
			}
		}
		public static int FwdMaxSteps
		{
			get
			{
				return m_FwdMaxSteps;
			}
			set
			{
				m_FwdMaxSteps = value;
			}
		}

		public virtual void ClearFastwalkStack()
		{
			if (this.m_MoveRecords != null && this.m_MoveRecords.Count > 0)
				this.m_MoveRecords.Clear();

			this.m_EndQueue = DateTime.Now;
		}

		public virtual bool CheckMovement(Direction d, out int newZ)
		{
			return Movement.Movement.CheckMovement(this, d, out newZ);
		}

		public virtual bool Move(Direction d)
		{
			if (this.m_Deleted)
				return false;

			BankBox box = this.FindBankNoCreate();

			if (box != null && box.Opened)
				box.Close();

			Point3D newLocation = this.m_Location;
			Point3D oldLocation = newLocation;

			if ((this.m_Direction & Direction.Mask) == (d & Direction.Mask))
			{
				// We are actually moving (not just a direction change)
				if (this.m_Spell != null && !this.m_Spell.OnCasterMoving(d))
					return false;

				if (this.m_Paralyzed || this.m_Frozen)
				{
					this.SendLocalizedMessage(500111); // You are frozen and can not move.

					return false;
				}

				int newZ;

				if (this.CheckMovement(d, out newZ))
				{
					int x = oldLocation.m_X, y = oldLocation.m_Y;
					int oldX = x, oldY = y;
					int oldZ = oldLocation.m_Z;

					switch( d & Direction.Mask )
					{
						case Direction.North:
							--y;
							break;
						case Direction.Right:
							++x;
							--y;
							break;
						case Direction.East:
							++x;
							break;
						case Direction.Down:
							++x;
							++y;
							break;
						case Direction.South:
							++y;
							break;
						case Direction.Left:
							--x;
							++y;
							break;
						case Direction.West:
							--x;
							break;
						case Direction.Up:
							--x;
							--y;
							break;
					}

					newLocation.m_X = x;
					newLocation.m_Y = y;
					newLocation.m_Z = newZ;

					this.m_Pushing = false;

					Map map = this.m_Map;

					if (map != null)
					{
						Sector oldSector = map.GetSector(oldX, oldY);
						Sector newSector = map.GetSector(x, y);

						if (oldSector != newSector)
						{
							for (int i = 0; i < oldSector.Mobiles.Count; ++i)
							{
								Mobile m = oldSector.Mobiles[i];

								if (m != this && m.X == oldX && m.Y == oldY && (m.Z + 15) >= oldZ && (oldZ + 15) >= m.Z && !m.OnMoveOff(this))
									return false;
							}

							for (int i = 0; i < oldSector.Items.Count; ++i)
							{
								Item item = oldSector.Items[i];

								if (item.AtWorldPoint(oldX, oldY) && (item.Z == oldZ || ((item.Z + item.ItemData.Height) >= oldZ && (oldZ + 15) >= item.Z)) && !item.OnMoveOff(this))
									return false;
							}

							for (int i = 0; i < newSector.Mobiles.Count; ++i)
							{
								Mobile m = newSector.Mobiles[i];

								if (m.X == x && m.Y == y && (m.Z + 15) >= newZ && (newZ + 15) >= m.Z && !m.OnMoveOver(this))
									return false;
							}

							for (int i = 0; i < newSector.Items.Count; ++i)
							{
								Item item = newSector.Items[i];

								if (item.AtWorldPoint(x, y) && (item.Z == newZ || ((item.Z + item.ItemData.Height) >= newZ && (newZ + 15) >= item.Z)) && !item.OnMoveOver(this))
									return false;
							}
						}
						else
						{
							for (int i = 0; i < oldSector.Mobiles.Count; ++i)
							{
								Mobile m = oldSector.Mobiles[i];

								if (m != this && m.X == oldX && m.Y == oldY && (m.Z + 15) >= oldZ && (oldZ + 15) >= m.Z && !m.OnMoveOff(this))
									return false;
								else if (m.X == x && m.Y == y && (m.Z + 15) >= newZ && (newZ + 15) >= m.Z && !m.OnMoveOver(this))
									return false;
							}

							for (int i = 0; i < oldSector.Items.Count; ++i)
							{
								Item item = oldSector.Items[i];

								if (item.AtWorldPoint(oldX, oldY) && (item.Z == oldZ || ((item.Z + item.ItemData.Height) >= oldZ && (oldZ + 15) >= item.Z)) && !item.OnMoveOff(this))
									return false;
								else if (item.AtWorldPoint(x, y) && (item.Z == newZ || ((item.Z + item.ItemData.Height) >= newZ && (newZ + 15) >= item.Z)) && !item.OnMoveOver(this))
									return false;
							}
						}

						if (!Region.CanMove(this, d, newLocation, oldLocation, this.m_Map))
							return false;
					}
					else
					{
						return false;
					}

					if (!this.InternalOnMove(d))
						return false;

					if (m_FwdEnabled && this.m_NetState != null && this.m_AccessLevel < m_FwdAccessOverride && (!m_FwdUOTDOverride || !this.m_NetState.IsUOTDClient))
					{
						if (this.m_MoveRecords == null)
							this.m_MoveRecords = new Queue<MovementRecord>(6);

						while (this.m_MoveRecords.Count > 0)
						{
							MovementRecord r = this.m_MoveRecords.Peek();

							if (r.Expired())
								this.m_MoveRecords.Dequeue();
							else
								break;
						}

						if (this.m_MoveRecords.Count >= m_FwdMaxSteps)
						{
							FastWalkEventArgs fw = new FastWalkEventArgs(this.m_NetState);
							EventSink.InvokeFastWalk(fw);

							if (fw.Blocked)
								return false;
						}

						TimeSpan delay = this.ComputeMovementSpeed(d);

						DateTime end;

						if (this.m_MoveRecords.Count > 0)
							end = this.m_EndQueue + delay;
						else
							end = DateTime.Now + delay;

						this.m_MoveRecords.Enqueue(MovementRecord.NewInstance(end));

						this.m_EndQueue = end;
					}

					this.m_LastMoveTime = DateTime.Now;
				}
				else
				{
					return false;
				}

				this.DisruptiveAction();
			}

			if (this.m_NetState != null)
				this.m_NetState.Send(MovementAck.Instantiate(this.m_NetState.Sequence, this));//new MovementAck( m_NetState.Sequence, this ) );

			this.SetLocation(newLocation, false);
			this.SetDirection(d);

			if (this.m_Map != null)
			{
				IPooledEnumerable eable = this.m_Map.GetObjectsInRange(this.m_Location, Core.GlobalMaxUpdateRange);

				foreach (object o in eable)
				{
					if (o == this)
						continue;

					if (o is Mobile)
					{
						m_MoveList.Add(o);
					}
					else if (o is Item)
					{
						Item item = (Item)o;

						if (item.HandlesOnMovement)
							m_MoveList.Add(item);
					}
				}

				eable.Free();

				Packet[][] cache = m_MovingPacketCache;

				for (int i = 0; i < cache.Length; ++i)
					for (int j = 0; j < cache[i].Length; ++j)
						Packet.Release(ref cache[i][j]);

				for (int i = 0; i < m_MoveList.Count; ++i)
				{
					object o = m_MoveList[i];

					if (o is Mobile)
					{
						Mobile m = (Mobile)m_MoveList[i];
						NetState ns = m.NetState;

						if (ns != null && Utility.InUpdateRange(this.m_Location, m.m_Location) && m.CanSee(this))
						{
							Packet p = null;

							if (ns.StygianAbyss)
							{
								int noto = Notoriety.Compute(m, this);
								p = cache[0][noto];

								if (p == null)
									cache[0][noto] = p = Packet.Acquire(new MobileMoving(this, noto));
							}
							else
							{
								int noto = Notoriety.Compute(m, this);
								p = cache[1][noto];

								if (p == null)
									cache[1][noto] = p = Packet.Acquire(new MobileMovingOld(this, noto));
							}

							ns.Send(p);
						}

						m.OnMovement(this, oldLocation);
					}
					else if (o is Item)
					{
						((Item)o).OnMovement(this, oldLocation);
					}
				}

				for (int i = 0; i < cache.Length; ++i)
					for (int j = 0; j < cache[i].Length; ++j)
						Packet.Release(ref cache[i][j]);

				if (m_MoveList.Count > 0)
					m_MoveList.Clear();
			}

			this.OnAfterMove(oldLocation);
			return true;
		}

		public virtual void OnAfterMove(Point3D oldLocation)
		{
		}

		public TimeSpan ComputeMovementSpeed()
		{
			return this.ComputeMovementSpeed(this.Direction, false);
		}

		public TimeSpan ComputeMovementSpeed(Direction dir)
		{
			return this.ComputeMovementSpeed(dir, true);
		}

		public virtual TimeSpan ComputeMovementSpeed(Direction dir, bool checkTurning)
		{
			TimeSpan delay;

			if (this.Mounted)
				delay = (dir & Direction.Running) != 0 ? m_RunMount : m_WalkMount;
			else
				delay = (dir & Direction.Running) != 0 ? m_RunFoot : m_WalkFoot;

			return delay;
		}

		/// <summary>
		/// Overridable. Virtual event invoked when a Mobile <paramref name="m" /> moves off this Mobile.
		/// </summary>
		/// <returns>True if the move is allowed, false if not.</returns>
		public virtual bool OnMoveOff(Mobile m)
		{
			return true;
		}

		public virtual bool IsDeadBondedPet
		{
			get
			{
				return false;
			}
		}

		/// <summary>
		/// Overridable. Event invoked when a Mobile <paramref name="m" /> moves over this Mobile.
		/// </summary>
		/// <returns>True if the move is allowed, false if not.</returns>
		public virtual bool OnMoveOver(Mobile m)
		{
			if (this.m_Map == null || this.m_Deleted)
				return true;

			return m.CheckShove(this);
		}

		public virtual bool CheckShove(Mobile shoved)
		{
			if ((this.m_Map.Rules & MapRules.FreeMovement) == 0)
			{
				if (!shoved.Alive || !this.Alive || shoved.IsDeadBondedPet || this.IsDeadBondedPet)
					return true;
				else if (shoved.m_Hidden && shoved.IsStaff())
					return true;

				if (!this.m_Pushing)
				{
					this.m_Pushing = true;

					int number;

					if (this.IsStaff())
					{
						number = shoved.m_Hidden ? 1019041 : 1019040;
					}
					else
					{
						if (this.Stam == this.StamMax)
						{
							number = shoved.m_Hidden ? 1019043 : 1019042;
							this.Stam -= 10;

							this.RevealingAction();
						}
						else
						{
							return false;
						}
					}

					this.SendLocalizedMessage(number);
				}
			}
			return true;
		}

		/// <summary>
		/// Overridable. Virtual event invoked when the Mobile sees another Mobile, <paramref name="m" />, move.
		/// </summary>
		public virtual void OnMovement(Mobile m, Point3D oldLocation)
		{
		}

		public ISpell Spell
		{
			get
			{
				return this.m_Spell;
			}
			set
			{
				if (this.m_Spell != null && value != null)
					Console.WriteLine("Warning: Spell has been overwritten");

				this.m_Spell = value;
			}
		}

		[CommandProperty(AccessLevel.Administrator)]
		public bool AutoPageNotify
		{
			get
			{
				return this.m_AutoPageNotify;
			}
			set
			{
				this.m_AutoPageNotify = value;
			}
		}

		public virtual void CriminalAction(bool message)
		{
			if (this.m_Deleted)
				return;

			this.Criminal = true;

			this.Region.OnCriminalAction(this, message);
		}

		public virtual bool CanUseStuckMenu()
		{
			if (this.m_StuckMenuUses == null)
			{
				return true;
			}
			else
			{
				for (int i = 0; i < this.m_StuckMenuUses.Length; ++i)
				{
					if ((DateTime.Now - this.m_StuckMenuUses[i]) > TimeSpan.FromDays(1.0))
					{
						return true;
					}
				}

				return false;
			}
		}

		public virtual bool IsPlayer()
		{
			return Utilities.IsPlayer(this);
		}

		public virtual bool IsStaff()
		{
			return Utilities.IsStaff(this);
		}

		public virtual bool IsOwner()
		{
			return Utilities.IsOwner(this);
		}

		public virtual bool IsSnoop(Mobile from)
		{
			return (from != this);
		}

		/// <summary>
		/// Overridable. Any call to <see cref="Resurrect" /> will silently fail if this method returns false.
		/// <seealso cref="Resurrect" />
		/// </summary>
		public virtual bool CheckResurrect()
		{
			return true;
		}

		/// <summary>
		/// Overridable. Event invoked before the Mobile is <see cref="Resurrect">resurrected</see>.
		/// <seealso cref="Resurrect" />
		/// </summary>
		public virtual void OnBeforeResurrect()
		{
		}

		/// <summary>
		/// Overridable. Event invoked after the Mobile is <see cref="Resurrect">resurrected</see>.
		/// <seealso cref="Resurrect" />
		/// </summary>
		public virtual void OnAfterResurrect()
		{
		}

		public virtual void Resurrect()
		{
			if (!this.Alive)
			{
				if (!this.Region.OnResurrect(this))
					return;

				if (!this.CheckResurrect())
					return;

				this.OnBeforeResurrect();

				BankBox box = this.FindBankNoCreate();

				if (box != null && box.Opened)
					box.Close();

				this.Poison = null;

				this.Warmode = false;

				this.Hits = 10;
				this.Stam = this.StamMax;
				this.Mana = 0;

				this.BodyMod = 0;
				this.Body = this.Race.AliveBody(this);

				ProcessDeltaQueue();

				for (int i = this.m_Items.Count - 1; i >= 0; --i)
				{
					if (i >= this.m_Items.Count)
						continue;

					Item item = this.m_Items[i];

					if (item.ItemID == 0x204E)
						item.Delete();
				}

				this.SendIncomingPacket();
				this.SendIncomingPacket();

				this.OnAfterResurrect();
				//Send( new DeathStatus( false ) );
			}
		}

		private IAccount m_Account;

		[CommandProperty(AccessLevel.Counselor, AccessLevel.Owner)]
		public IAccount Account
		{
			get
			{
				return this.m_Account;
			}
			set
			{
				this.m_Account = value;
			}
		}

		private bool m_Deleted;

		public bool Deleted
		{
			get
			{
				return this.m_Deleted;
			}
		}

		[CommandProperty(AccessLevel.GameMaster)]
		public int VirtualArmor
		{
			get
			{
				return this.m_VirtualArmor;
			}
			set
			{
				if (this.m_VirtualArmor != value)
				{
					this.m_VirtualArmor = value;

					this.Delta(MobileDelta.Armor);
				}
			}
		}

		[CommandProperty(AccessLevel.GameMaster)]
		public virtual double ArmorRating
		{
			get
			{
				return 0.0;
			}
		}

		public void DropHolding()
		{
			Item holding = this.m_Holding;

			if (holding != null)
			{
				if (!holding.Deleted && holding.HeldBy == this && holding.Map == Map.Internal)
					this.AddToBackpack(holding);

				this.Holding = null;
				holding.ClearBounce();
			}
		}

		public virtual void Delete()
		{
			if (this.m_Deleted)
				return;
			else if (!World.OnDelete(this))
				return;

			if (this.m_NetState != null)
				this.m_NetState.CancelAllTrades();

			if (this.m_NetState != null)
				this.m_NetState.Dispose();

			this.DropHolding();

			Region.OnRegionChange(this, this.m_Region, null);

			this.m_Region = null;
			//Is the above line REALLY needed?  The old Region system did NOT have said line
			//and worked fine, because of this a LOT of extra checks have to be done everywhere...
			//I guess this should be there for Garbage collection purposes, but, still, is it /really/ needed?

			this.OnDelete();

			for (int i = this.m_Items.Count - 1; i >= 0; --i)
				if (i < this.m_Items.Count)
					this.m_Items[i].OnParentDeleted(this);

			for (int i = 0; i < this.m_Stabled.Count; i++)
				this.m_Stabled[i].Delete();

			this.SendRemovePacket();

			if (this.m_Guild != null)
				this.m_Guild.OnDelete(this);

			this.m_Deleted = true;

			if (this.m_Map != null)
			{
				this.m_Map.OnLeave(this);
				this.m_Map = null;
			}

			this.m_Hair = null;
			this.m_FacialHair = null;
			this.m_MountItem = null;

			World.RemoveMobile(this);

			this.OnAfterDelete();

			this.FreeCache();
		}

		/// <summary>
		/// Overridable. Virtual event invoked before the Mobile is deleted.
		/// </summary>
		public virtual void OnDelete()
		{
			if (this.m_Spawner != null)
			{
				this.m_Spawner.Remove(this);
				this.m_Spawner = null;
			}
		}

		/// <summary>
		/// Overridable. Returns true if the player is alive, false if otherwise. By default, this is computed by: <c>!Deleted &amp;&amp; (!Player || !Body.IsGhost)</c>
		/// </summary>
		[CommandProperty(AccessLevel.Counselor)]
		public virtual bool Alive
		{
			get
			{
				return !this.m_Deleted && (!this.m_Player || !this.m_Body.IsGhost);
			}
		}

		public virtual bool CheckSpellCast(ISpell spell)
		{
			return true;
		}

		/// <summary>
		/// Overridable. Virtual event invoked when the Mobile casts a <paramref name="spell" />.
		/// </summary>
		/// <param name="spell"></param>
		public virtual void OnSpellCast(ISpell spell)
		{
		}

		/// <summary>
		/// Overridable. Virtual event invoked after <see cref="TotalWeight" /> changes.
		/// </summary>
		public virtual void OnWeightChange(int oldValue)
		{
		}

		/// <summary>
		/// Overridable. Virtual event invoked when the <see cref="Skill.Base" /> or <see cref="Skill.BaseFixedPoint" /> property of <paramref name="skill" /> changes.
		/// </summary>
		public virtual void OnSkillChange(SkillName skill, double oldBase)
		{
		}

		/// <summary>
		/// Overridable. Invoked after the mobile is deleted. When overriden, be sure to call the base method.
		/// </summary>
		public virtual void OnAfterDelete()
		{
			this.StopAggrExpire();

			this.CheckAggrExpire();

			if (this.m_PoisonTimer != null)
				this.m_PoisonTimer.Stop();

			if (this.m_HitsTimer != null)
				this.m_HitsTimer.Stop();

			if (this.m_StamTimer != null)
				this.m_StamTimer.Stop();

			if (this.m_ManaTimer != null)
				this.m_ManaTimer.Stop();

			if (this.m_CombatTimer != null)
				this.m_CombatTimer.Stop();

			if (this.m_ExpireCombatant != null)
				this.m_ExpireCombatant.Stop();

			if (this.m_LogoutTimer != null)
				this.m_LogoutTimer.Stop();

			if (this.m_ExpireCriminal != null)
				this.m_ExpireCriminal.Stop();

			if (this.m_WarmodeTimer != null)
				this.m_WarmodeTimer.Stop();

			if (this.m_ParaTimer != null)
				this.m_ParaTimer.Stop();

			if (this._SleepTimer != null)
				this._SleepTimer.Stop();

			if (this.m_FrozenTimer != null)
				this.m_FrozenTimer.Stop();

			if (this.m_AutoManifestTimer != null)
				this.m_AutoManifestTimer.Stop();

			foreach (BaseModule module in World.GetModules(this))
			{
				if (module != null)
					module.Delete();
			}
		}

		public virtual bool AllowSkillUse(SkillName name)
		{
			return true;
		}

		public virtual bool UseSkill(SkillName name)
		{
			return Skills.UseSkill(this, name);
		}

		public virtual bool UseSkill(int skillID)
		{
			return Skills.UseSkill(this, skillID);
		}

		private static CreateCorpseHandler m_CreateCorpse;

		public static CreateCorpseHandler CreateCorpseHandler
		{
			get
			{
				return m_CreateCorpse;
			}
			set
			{
				m_CreateCorpse = value;
			}
		}

		public virtual DeathMoveResult GetParentMoveResultFor(Item item)
		{
			return item.OnParentDeath(this);
		}

		public virtual DeathMoveResult GetInventoryMoveResultFor(Item item)
		{
			return item.OnInventoryDeath(this);
		}

		public virtual bool RetainPackLocsOnDeath
		{
			get
			{
				return Core.AOS;
			}
		}

		public virtual void Kill()
		{
			if (!this.CanBeDamaged())
				return;
			else if (!this.Alive || this.IsDeadBondedPet)
				return;
			else if (this.m_Deleted)
				return;
			else if (!this.Region.OnBeforeDeath(this))
				return;
			else if (!this.OnBeforeDeath())
				return;

			BankBox box = this.FindBankNoCreate();

			if (box != null && box.Opened)
				box.Close();

			if (this.m_NetState != null)
				this.m_NetState.CancelAllTrades();

			if (this.m_Spell != null)
				this.m_Spell.OnCasterKilled();
			//m_Spell.Disturb( DisturbType.Kill );

			if (this.m_Target != null)
				this.m_Target.Cancel(this, TargetCancelType.Canceled);

			this.DisruptiveAction();

			this.Warmode = false;

			this.DropHolding();

			this.Hits = 0;
			this.Stam = 0;
			this.Mana = 0;

			this.Poison = null;
			this.Combatant = null;

			if (this.Paralyzed)
			{
				this.Paralyzed = false;

				if (this.m_ParaTimer != null)
					this.m_ParaTimer.Stop();
			}

			if (this.Asleep)
			{
				this.Asleep = false;
				this.Send(SpeedControl.Disable);

				if (this._SleepTimer != null)
					this._SleepTimer.Stop();
			}

			if (this.Frozen)
			{
				this.Frozen = false;

				if (this.m_FrozenTimer != null)
					this.m_FrozenTimer.Stop();
			}

			List<Item> content = new List<Item>();
			List<Item> equip = new List<Item>();
			List<Item> moveToPack = new List<Item>();

			List<Item> itemsCopy = new List<Item>(this.m_Items);

			Container pack = this.Backpack;

			for (int i = 0; i < itemsCopy.Count; ++i)
			{
				Item item = itemsCopy[i];

				if (item == pack)
					continue;

				if ((item.Insured || item.LootType == LootType.Blessed) && item.Parent == this && item.Layer != Layer.Mount)
				   equip.Add(item); 

				DeathMoveResult res = this.GetParentMoveResultFor(item);

				switch( res )
				{
					case DeathMoveResult.MoveToCorpse:
						{
							content.Add(item);
							equip.Add(item);
							break;
						}
					case DeathMoveResult.MoveToBackpack:
						{
							moveToPack.Add(item);
							break;
						}
				}
			}

			if (pack != null)
			{
				List<Item> packCopy = new List<Item>(pack.Items);
				List<Item> contCopy = new List<Item>();

				for (int i = 0; i < packCopy.Count; ++i)
				{
					Item item = packCopy[i];

					DeathMoveResult res = this.GetInventoryMoveResultFor(item);

					if (res == DeathMoveResult.MoveToCorpse)
					{
						content.Add(item);

						if (item is Container)
							contCopy.AddRange(item.Items);
					}
					else
						moveToPack.Add(item);

					while (contCopy.Count > 0)
					{
						Item child = contCopy[0];
						res = this.GetInventoryMoveResultFor(child);

						if (res != DeathMoveResult.MoveToBackpack)
						{
							if (child is Container)
								contCopy.AddRange(child.Items);
						}
						else
							moveToPack.Add(child);

						contCopy.RemoveAt(0);
					}
				}

				for (int i = 0; i < moveToPack.Count; ++i)
				{
					Item item = moveToPack[i];

					if (this.RetainPackLocsOnDeath && item.Parent == pack)
						continue;

					pack.DropItem(item);
				}
			}

			HairInfo hair = null;
			if (this.m_Hair != null)
				hair = new HairInfo(this.m_Hair.ItemID, this.m_Hair.Hue);

			FacialHairInfo facialhair = null;
			if (this.m_FacialHair != null)
				facialhair = new FacialHairInfo(this.m_FacialHair.ItemID, this.m_FacialHair.Hue);

			Container c = (m_CreateCorpse == null ? null : m_CreateCorpse(this, hair, facialhair, content, equip));

			/*m_Corpse = c;

			for ( int i = 0; c != null && i < content.Count; ++i )
			c.DropItem( (Item)content[i] );

			if ( c != null )
			c.MoveToWorld( this.Location, this.Map );*/

			if (this.m_Map != null)
			{
				Packet animPacket = null;//new DeathAnimation( this, c );
				Packet remPacket = null;//this.RemovePacket;

				IPooledEnumerable eable = this.m_Map.GetClientsInRange(this.m_Location);

				foreach (NetState state in eable)
				{
					if (state != this.m_NetState)
					{
						if (animPacket == null)
							animPacket = Packet.Acquire(new DeathAnimation(this, c));

						state.Send(animPacket);

						if (!state.Mobile.CanSee(this))
						{
							if (remPacket == null)
								remPacket = this.RemovePacket;

							state.Send(remPacket);
						}
					}
				}

				Packet.Release(animPacket);

				eable.Free();
			}

			this.Region.OnDeath(this);
			this.OnDeath(c);
		}

		private Container m_Corpse;

		[CommandProperty(AccessLevel.GameMaster)]
		public Container Corpse
		{
			get
			{
				return this.m_Corpse;
			}
			set
			{
				this.m_Corpse = value;
			}
		}

		/// <summary>
		/// Overridable. Event invoked before the Mobile is <see cref="Kill">killed</see>.
		/// <seealso cref="Kill" />
		/// <seealso cref="OnDeath" />
		/// </summary>
		/// <returns>True to continue with death, false to override it.</returns>
		public virtual bool OnBeforeDeath()
		{
			return true;
		}

		/// <summary>
		/// Overridable. Event invoked after the Mobile is <see cref="Kill">killed</see>. Primarily, this method is responsible for deleting an NPC or turning a PC into a ghost.
		/// <seealso cref="Kill" />
		/// <seealso cref="OnBeforeDeath" />
		/// </summary>
		public virtual void OnDeath(Container c)
		{
			int sound = this.GetDeathSound();

			if (sound >= 0)
				Effects.PlaySound(this, this.Map, sound);

			if (!this.m_Player)
			{
				this.Delete();
			}
			else
			{
				this.Send(DeathStatus.Instantiate(true));

				this.Warmode = false;

				this.BodyMod = 0;
				//Body = this.Female ? 0x193 : 0x192;
				this.Body = this.Race.GhostBody(this);

				Item deathShroud = new Item(0x204E);

				deathShroud.Movable = false;
				deathShroud.Layer = Layer.OuterTorso;

				this.AddItem(deathShroud);

				this.m_Items.Remove(deathShroud);
				this.m_Items.Insert(0, deathShroud);

				this.Poison = null;
				this.Combatant = null;

				this.Hits = 0;
				this.Stam = 0;
				this.Mana = 0;

				EventSink.InvokePlayerDeath(new PlayerDeathEventArgs(this));

				ProcessDeltaQueue();

				this.Send(DeathStatus.Instantiate(false));

				this.CheckStatTimers();
			}
		}

		#region Get*Sound

		public virtual int GetAngerSound()
		{
			if (this.m_BaseSoundID != 0)
				return this.m_BaseSoundID;

			return -1;
		}

		public virtual int GetIdleSound()
		{
			if (this.m_BaseSoundID != 0)
				return this.m_BaseSoundID + 1;

			return -1;
		}

		public virtual int GetAttackSound()
		{
			if (this.m_BaseSoundID != 0)
				return this.m_BaseSoundID + 2;

			return -1;
		}

		public virtual int GetHurtSound()
		{
			if (this.m_BaseSoundID != 0)
				return this.m_BaseSoundID + 3;

			return -1;
		}

		public virtual int GetDeathSound()
		{
			if (this.m_BaseSoundID != 0)
			{
				return this.m_BaseSoundID + 4;
			}
			else if (this.m_Body.IsHuman)
			{
				return Utility.Random(this.m_Female ? 0x314 : 0x423, this.m_Female ? 4 : 5);
			}
			else
			{
				return -1;
			}
		}

		#endregion

		private static char[] m_GhostChars = new char[2] { 'o', 'O' };

		public static char[] GhostChars
		{
			get
			{
				return m_GhostChars;
			}
			set
			{
				m_GhostChars = value;
			}
		}

		private static bool m_NoSpeechLOS;

		public static bool NoSpeechLOS
		{
			get
			{
				return m_NoSpeechLOS;
			}
			set
			{
				m_NoSpeechLOS = value;
			}
		}

		private static TimeSpan m_AutoManifestTimeout = TimeSpan.FromSeconds(5.0);

		public static TimeSpan AutoManifestTimeout
		{
			get
			{
				return m_AutoManifestTimeout;
			}
			set
			{
				m_AutoManifestTimeout = value;
			}
		}

		private Timer m_AutoManifestTimer;

		private class AutoManifestTimer : Timer
		{
			private readonly Mobile m_Mobile;

			public AutoManifestTimer(Mobile m, TimeSpan delay)
				: base(delay)
			{
				this.m_Mobile = m;
			}

			protected override void OnTick()
			{
				if (!this.m_Mobile.Alive)
					this.m_Mobile.Warmode = false;
			}
		}

		public virtual bool CheckTarget(Mobile from, Target targ, object targeted)
		{
			return true;
		}

		private static bool m_InsuranceEnabled;

		public static bool InsuranceEnabled
		{
			get
			{
				return m_InsuranceEnabled;
			}
			set
			{
				m_InsuranceEnabled = value;
			}
		}

		public virtual void Use(Item item)
		{
			if (item == null || item.Deleted || this.Deleted)
				return;

			this.DisruptiveAction();

			if (this.m_Spell != null && !this.m_Spell.OnCasterUsingObject(item))
				return;

			object root = item.RootParent;
			bool okay = false;

			if (!Utility.InUpdateRange(this, item.GetWorldLocation()))
				item.OnDoubleClickOutOfRange(this);
			else if (!this.CanSee(item))
				item.OnDoubleClickCantSee(this);
			else if (!item.IsAccessibleTo(this))
			{
				Region reg = Region.Find(item.GetWorldLocation(), item.Map);

				if (reg == null || !reg.SendInaccessibleMessage(item, this))
					item.OnDoubleClickNotAccessible(this);
			}
			else if (!this.CheckAlive(false))
				item.OnDoubleClickDead(this);
			else if (item.InSecureTrade)
				item.OnDoubleClickSecureTrade(this);
			else if (!this.AllowItemUse(item))
				okay = false;
			else if (!item.CheckItemUse(this, item))
				okay = false;
			else if (root != null && root is Mobile && ((Mobile)root).IsSnoop(this))
				item.OnSnoop(this);
			else if (this.Region.OnDoubleClick(this, item))
				okay = true;

			if (okay)
			{
				if (!item.Deleted)
					item.OnItemUsed(this, item);

				if (!item.Deleted)
					item.OnDoubleClick(this);
			}
		}

		public virtual void Use(Mobile m)
		{
			if (m == null || m.Deleted || this.Deleted)
				return;

			this.DisruptiveAction();

			if (this.m_Spell != null && !this.m_Spell.OnCasterUsingObject(m))
				return;

			if (!Utility.InUpdateRange(this, m))
				m.OnDoubleClickOutOfRange(this);
			else if (!this.CanSee(m))
				m.OnDoubleClickCantSee(this);
			else if (!this.CheckAlive(false))
				m.OnDoubleClickDead(this);
			else if (this.Region.OnDoubleClick(this, m) && !m.Deleted)
				m.OnDoubleClick(this);
		}

		public virtual void Lift(Item item, int amount, out bool rejected, out LRReason reject)
		{
			rejected = true;
			reject = LRReason.Inspecific;

			if (item == null)
				return;

			Mobile from = this;
			NetState state = this.m_NetState;

			if (from.AccessLevel >= AccessLevel.GameMaster || DateTime.Now >= from.NextActionTime)
			{
				if (from.CheckAlive())
				{
					from.DisruptiveAction();

					if (from.Holding != null)
					{
						reject = LRReason.AreHolding;
					}
					else if (from.AccessLevel < AccessLevel.GameMaster && !from.InRange(item.GetWorldLocation(), 2))
					{
						reject = LRReason.OutOfRange;
					}
					else if (!from.CanSee(item) || !from.InLOS(item))
					{
						reject = LRReason.OutOfSight;
					}
					else if (!item.VerifyMove(from))
					{
						reject = LRReason.CannotLift;
					}
					#region Mondain's Legacy
					else if (item.QuestItem && amount != item.Amount && from.AccessLevel < AccessLevel.GameMaster)
					{
						reject = LRReason.Inspecific;
						from.SendLocalizedMessage(1074868); // Stacks of quest items cannot be unstacked.
					}
					#endregion
					else if (!item.IsAccessibleTo(from))
					{
						reject = LRReason.CannotLift;
					}
					else if (!item.CheckLift(from, item, ref reject))
					{
					}
					else
					{
						object root = item.RootParent;

						if (root != null && root is Mobile && !((Mobile)root).CheckNonlocalLift(from, item))
						{
							reject = LRReason.TryToSteal;
						}
						else if (!from.OnDragLift(item) || !item.OnDragLift(from))
						{
							reject = LRReason.Inspecific;
						}
						else if (!from.CheckAlive())
						{
							reject = LRReason.Inspecific;
						}
						else
						{
							item.SetLastMoved();

							if (item.Spawner != null)
							{
								item.Spawner.Remove(item);
								item.Spawner = null;
							}

							if (amount == 0)
								amount = 1;

							if (amount > item.Amount)
								amount = item.Amount;

							int oldAmount = item.Amount;
							//item.Amount = amount; //Set in LiftItemDupe

							if (amount < oldAmount)
								LiftItemDupe(item, amount);
							//item.Dupe( oldAmount - amount );

							Map map = from.Map;

							if (Mobile.DragEffects && map != null && (root == null || root is Item))
							{
								IPooledEnumerable eable = map.GetClientsInRange(from.Location);
								Packet p = null;

								foreach (NetState ns in eable)
								{
									if (!ns.StygianAbyss && ns.Mobile != from && ns.Mobile.CanSee(from))
									{
										if (p == null)
										{
											IEntity src;

											if (root == null)
												src = new Entity(Serial.Zero, item.Location, map);
											else
												src = new Entity(((Item)root).Serial, ((Item)root).Location, map);

											p = Packet.Acquire(new DragEffect(src, from, item.ItemID, item.Hue, amount));
										}

										ns.Send(p);
									}
								}

								Packet.Release(p);

								eable.Free();
							}

							Point3D fixLoc = item.Location;
							Map fixMap = item.Map;
							bool shouldFix = (item.Parent == null);

							item.RecordBounce();
							item.OnItemLifted(from, item);
							item.Internalize();

							from.Holding = item;

							int liftSound = item.GetLiftSound(from);

							if (liftSound != -1)
								from.Send(new PlaySound(liftSound, from));

							from.NextActionTime = DateTime.Now + TimeSpan.FromSeconds(0.5);

							if (fixMap != null && shouldFix)
								fixMap.FixColumn(fixLoc.m_X, fixLoc.m_Y);

							reject = LRReason.Inspecific;
							rejected = false;
						}
					}
				}
				else
				{
					reject = LRReason.Inspecific;
				}
			}
			else
			{
				this.SendActionMessage();
				reject = LRReason.Inspecific;
			}

			if (rejected && state != null)
			{
				state.Send(new LiftRej(reject));

				if (item.Deleted)
					return;

				if (item.Parent is Item)
				{
					if (state.ContainerGridLines)
						state.Send(new ContainerContentUpdate6017(item));
					else
						state.Send(new ContainerContentUpdate(item));
				}
				else if (item.Parent is Mobile)
					state.Send(new EquipUpdate(item));
				else
					item.SendInfoTo(state);

				if (ObjectPropertyList.Enabled && item.Parent != null)
					state.Send(item.OPLPacket);
			}
		}

		public static Item LiftItemDupe(Item oldItem, int amount)
		{
			Item item;
			try
			{
				item = (Item)Activator.CreateInstance(oldItem.GetType());
			}
			catch
			{
				Console.WriteLine("Warning: 0x{0:X}: Item must have a zero paramater constructor to be separated from a stack. '{1}'.", oldItem.Serial.Value, oldItem.GetType().Name);
				return null;
			}
			item.Visible = oldItem.Visible;
			item.Movable = oldItem.Movable;
			item.LootType = oldItem.LootType;
			item.Direction = oldItem.Direction;
			item.Hue = oldItem.Hue;
			item.ItemID = oldItem.ItemID;
			item.Location = oldItem.Location;
			item.Layer = oldItem.Layer;
			item.Name = oldItem.Name;
			item.Weight = oldItem.Weight;

			item.Amount = oldItem.Amount - amount;
			item.Map = oldItem.Map;

			oldItem.Amount = amount;
			oldItem.OnAfterDuped(item);

			if (oldItem.Parent is Mobile)
			{
				((Mobile)oldItem.Parent).AddItem(item);
			}
			else if (oldItem.Parent is Item)
			{
				((Item)oldItem.Parent).AddItem(item);
			}

			item.Delta(ItemDelta.Update);

			return item;
		}

		public virtual void SendDropEffect(Item item)
		{
			if (Mobile.DragEffects)
			{
				Map map = this.m_Map;
				object root = item.RootParent;

				if (map != null && (root == null || root is Item))
				{
					IPooledEnumerable eable = map.GetClientsInRange(this.m_Location);
					Packet p = null;

					foreach (NetState ns in eable)
					{
						if (!ns.StygianAbyss && ns.Mobile != this && ns.Mobile.CanSee(this))
						{
							if (p == null)
							{
								IEntity trg;

								if (root == null)
									trg = new Entity(Serial.Zero, item.Location, map);
								else
									trg = new Entity(((Item)root).Serial, ((Item)root).Location, map);

								p = Packet.Acquire(new DragEffect(this, trg, item.ItemID, item.Hue, item.Amount));
							}

							ns.Send(p);
						}
					}

					Packet.Release(p);

					eable.Free();
				}
			}
		}

		public virtual bool Drop(Item to, Point3D loc)
		{
			Mobile from = this;
			Item item = from.Holding;

			bool valid = (item != null && item.HeldBy == from && item.Map == Map.Internal);

			from.Holding = null;

			if (!valid)
			{
				return false;
			}

			bool bounced = true;

			item.SetLastMoved();

			if (to == null || !item.DropToItem(from, to, loc))
				item.Bounce(from);
			else
				bounced = false;

			item.ClearBounce();

			if (!bounced)
				this.SendDropEffect(item);

			return !bounced;
		}

		public virtual bool Drop(Point3D loc)
		{
			Mobile from = this;
			Item item = from.Holding;

			bool valid = (item != null && item.HeldBy == from && item.Map == Map.Internal);

			from.Holding = null;

			if (!valid)
			{
				return false;
			}

			bool bounced = true;

			item.SetLastMoved();

			if (!item.DropToWorld(from, loc))
				item.Bounce(from);
			else
				bounced = false;

			item.ClearBounce();

			if (!bounced)
				this.SendDropEffect(item);

			return !bounced;
		}

		public virtual bool Drop(Mobile to, Point3D loc)
		{
			Mobile from = this;
			Item item = from.Holding;

			bool valid = (item != null && item.HeldBy == from && item.Map == Map.Internal);

			from.Holding = null;

			if (!valid)
			{
				return false;
			}

			bool bounced = true;

			item.SetLastMoved();

			if (to == null || !item.DropToMobile(from, to, loc))
				item.Bounce(from);
			else
				bounced = false;

			item.ClearBounce();

			if (!bounced)
				this.SendDropEffect(item);

			return !bounced;
		}

		private static readonly object m_GhostMutateContext = new object();

		public virtual bool MutateSpeech(List<Mobile> hears, ref string text, ref object context)
		{
			if (this.Alive)
				return false;

			StringBuilder sb = new StringBuilder(text.Length, text.Length);

			for (int i = 0; i < text.Length; ++i)
			{
				if (text[i] != ' ')
					sb.Append(m_GhostChars[Utility.Random(m_GhostChars.Length)]);
				else
					sb.Append(' ');
			}

			text = sb.ToString();
			context = m_GhostMutateContext;
			return true;
		}

		public virtual void Manifest(TimeSpan delay)
		{
			this.Warmode = true;

			if (this.m_AutoManifestTimer == null)
				this.m_AutoManifestTimer = new AutoManifestTimer(this, delay);
			else
				this.m_AutoManifestTimer.Stop();

			this.m_AutoManifestTimer.Start();
		}

		public virtual bool CheckSpeechManifest()
		{
			if (this.Alive)
				return false;

			TimeSpan delay = m_AutoManifestTimeout;

			if (delay > TimeSpan.Zero && (!this.Warmode || this.m_AutoManifestTimer != null))
			{
				this.Manifest(delay);
				return true;
			}

			return false;
		}

		public virtual bool CheckHearsMutatedSpeech(Mobile m, object context)
		{
			if (context == m_GhostMutateContext)
				return (m.Alive && !m.CanHearGhosts);

			return true;
		}

		private void AddSpeechItemsFrom(ArrayList list, Container cont)
		{
			for (int i = 0; i < cont.Items.Count; ++i)
			{
				Item item = cont.Items[i];

				if (item.HandlesOnSpeech)
					list.Add(item);

				if (item is Container)
					this.AddSpeechItemsFrom(list, (Container)item);
			}
		}

		private class LocationComparer : IComparer
		{
			private static LocationComparer m_Instance;

			public static LocationComparer GetInstance(IPoint3D relativeTo)
			{
				if (m_Instance == null)
					m_Instance = new LocationComparer(relativeTo);
				else
					m_Instance.m_RelativeTo = relativeTo;

				return m_Instance;
			}

			private IPoint3D m_RelativeTo;

			public IPoint3D RelativeTo
			{
				get
				{
					return this.m_RelativeTo;
				}
				set
				{
					this.m_RelativeTo = value;
				}
			}

			public LocationComparer(IPoint3D relativeTo)
			{
				this.m_RelativeTo = relativeTo;
			}

			private int GetDistance(IPoint3D p)
			{
				int x = this.m_RelativeTo.X - p.X;
				int y = this.m_RelativeTo.Y - p.Y;
				int z = this.m_RelativeTo.Z - p.Z;

				x *= 11;
				y *= 11;

				return (x * x) + (y * y) + (z * z);
			}

			public int Compare(object x, object y)
			{
				IPoint3D a = x as IPoint3D;
				IPoint3D b = y as IPoint3D;

				return this.GetDistance(a) - this.GetDistance(b);
			}
		}

		#region Get*InRange

		public IPooledEnumerable GetItemsInRange(int range)
		{
			Map map = this.m_Map;

			if (map == null)
				return Server.Map.NullEnumerable.Instance;

			return map.GetItemsInRange(this.m_Location, range);
		}

		public IPooledEnumerable GetObjectsInRange(int range)
		{
			Map map = this.m_Map;

			if (map == null)
				return Server.Map.NullEnumerable.Instance;

			return map.GetObjectsInRange(this.m_Location, range);
		}

		public IPooledEnumerable GetMobilesInRange(int range)
		{
			Map map = this.m_Map;

			if (map == null)
				return Server.Map.NullEnumerable.Instance;

			return map.GetMobilesInRange(this.m_Location, range);
		}

		public IPooledEnumerable GetClientsInRange(int range)
		{
			Map map = this.m_Map;

			if (map == null)
				return Server.Map.NullEnumerable.Instance;

			return map.GetClientsInRange(this.m_Location, range);
		}

		#endregion

		private static List<Mobile> m_Hears;
		private static ArrayList m_OnSpeech;

		public virtual void DoSpeech(string text, int[] keywords, MessageType type, int hue)
		{
			if (this.m_Deleted || CommandSystem.Handle(this, text, type))
				return;

			int range = 15;

			switch( type )
			{
				case MessageType.Regular:
					this.m_SpeechHue = hue;
					break;
				case MessageType.Emote:
					this.m_EmoteHue = hue;
					break;
				case MessageType.Whisper:
					this.m_WhisperHue = hue;
					range = 1;
					break;
				case MessageType.Yell:
					this.m_YellHue = hue;
					range = 18;
					break;
				default:
					type = MessageType.Regular;
					break;
			}

			SpeechEventArgs regArgs = new SpeechEventArgs(this, text, type, hue, keywords);

			EventSink.InvokeSpeech(regArgs);
			this.Region.OnSpeech(regArgs);
			this.OnSaid(regArgs);

			if (regArgs.Blocked)
				return;

			text = regArgs.Speech;

			if (string.IsNullOrEmpty(text))
				return;

			if (m_Hears == null)
				m_Hears = new List<Mobile>();
			else if (m_Hears.Count > 0)
				m_Hears.Clear();

			if (m_OnSpeech == null)
				m_OnSpeech = new ArrayList();
			else if (m_OnSpeech.Count > 0)
				m_OnSpeech.Clear();

			List<Mobile> hears = m_Hears;
			ArrayList onSpeech = m_OnSpeech;

			if (this.m_Map != null)
			{
				IPooledEnumerable eable = this.m_Map.GetObjectsInRange(this.m_Location, range);

				foreach (object o in eable)
				{
					if (o is Mobile)
					{
						Mobile heard = (Mobile)o;

						if (heard.CanSee(this) && (m_NoSpeechLOS || !heard.Player || heard.InLOS(this)))
						{
							if (heard.m_NetState != null)
								hears.Add(heard);

							if (heard.HandlesOnSpeech(this))
								onSpeech.Add(heard);

							for (int i = 0; i < heard.Items.Count; ++i)
							{
								Item item = heard.Items[i];

								if (item.HandlesOnSpeech)
									onSpeech.Add(item);

								if (item is Container)
									this.AddSpeechItemsFrom(onSpeech, (Container)item);
							}
						}
					}
					else if (o is Item)
					{
						if (((Item)o).HandlesOnSpeech)
							onSpeech.Add(o);

						if (o is Container)
							this.AddSpeechItemsFrom(onSpeech, (Container)o);
					}
				}

				//eable.Free();

				object mutateContext = null;
				string mutatedText = text;
				SpeechEventArgs mutatedArgs = null;

				if (this.MutateSpeech(hears, ref mutatedText, ref mutateContext))
					mutatedArgs = new SpeechEventArgs(this, mutatedText, type, hue, new int[0]);

				this.CheckSpeechManifest();

				this.ProcessDelta();

				Packet regp = null;
				Packet mutp = null;

				for (int i = 0; i < hears.Count; ++i)
				{
					Mobile heard = hears[i];

					if (mutatedArgs == null || !this.CheckHearsMutatedSpeech(heard, mutateContext))
					{
						heard.OnSpeech(regArgs);

						NetState ns = heard.NetState;

						if (ns != null)
						{
							if (regp == null)
								regp = Packet.Acquire(new UnicodeMessage(this.m_Serial, this.Body, type, hue, 3, this.m_Language, this.Name, text));

							ns.Send(regp);
						}
					}
					else
					{
						heard.OnSpeech(mutatedArgs);

						NetState ns = heard.NetState;

						if (ns != null)
						{
							if (mutp == null)
								mutp = Packet.Acquire(new UnicodeMessage(this.m_Serial, this.Body, type, hue, 3, this.m_Language, this.Name, mutatedText));

							ns.Send(mutp);
						}
					}
				}

				Packet.Release(regp);
				Packet.Release(mutp);

				if (onSpeech.Count > 1)
					onSpeech.Sort(LocationComparer.GetInstance(this));

				for (int i = 0; i < onSpeech.Count; ++i)
				{
					object obj = onSpeech[i];

					if (obj is Mobile)
					{
						Mobile heard = (Mobile)obj;

						if (mutatedArgs == null || !this.CheckHearsMutatedSpeech(heard, mutateContext))
							heard.OnSpeech(regArgs);
						else
							heard.OnSpeech(mutatedArgs);
					}
					else
					{
						Item item = (Item)obj;

						item.OnSpeech(regArgs);
					}
				}
			}
		}

		private static VisibleDamageType m_VisibleDamageType;

		public static VisibleDamageType VisibleDamageType
		{
			get
			{
				return m_VisibleDamageType;
			}
			set
			{
				m_VisibleDamageType = value;
			}
		}

		private List<DamageEntry> m_DamageEntries;

		public List<DamageEntry> DamageEntries
		{
			get
			{
				return this.m_DamageEntries;
			}
		}

		public static Mobile GetDamagerFrom(DamageEntry de)
		{
			return (de == null ? null : de.Damager);
		}

		public Mobile FindMostRecentDamager(bool allowSelf)
		{
			return GetDamagerFrom(this.FindMostRecentDamageEntry(allowSelf));
		}

		public DamageEntry FindMostRecentDamageEntry(bool allowSelf)
		{
			for (int i = this.m_DamageEntries.Count - 1; i >= 0; --i)
			{
				if (i >= this.m_DamageEntries.Count)
					continue;

				DamageEntry de = this.m_DamageEntries[i];

				if (de.HasExpired)
					this.m_DamageEntries.RemoveAt(i);
				else if (allowSelf || de.Damager != this)
					return de;
			}

			return null;
		}

		public Mobile FindLeastRecentDamager(bool allowSelf)
		{
			return GetDamagerFrom(this.FindLeastRecentDamageEntry(allowSelf));
		}

		public DamageEntry FindLeastRecentDamageEntry(bool allowSelf)
		{
			for (int i = 0; i < this.m_DamageEntries.Count; ++i)
			{
				if (i < 0)
					continue;

				DamageEntry de = this.m_DamageEntries[i];

				if (de.HasExpired)
				{
					this.m_DamageEntries.RemoveAt(i);
					--i;
				}
				else if (allowSelf || de.Damager != this)
				{
					return de;
				}
			}

			return null;
		}

		public Mobile FindMostTotalDamger(bool allowSelf)
		{
			return GetDamagerFrom(this.FindMostTotalDamageEntry(allowSelf));
		}

		public DamageEntry FindMostTotalDamageEntry(bool allowSelf)
		{
			DamageEntry mostTotal = null;

			for (int i = this.m_DamageEntries.Count - 1; i >= 0; --i)
			{
				if (i >= this.m_DamageEntries.Count)
					continue;

				DamageEntry de = this.m_DamageEntries[i];

				if (de.HasExpired)
					this.m_DamageEntries.RemoveAt(i);
				else if ((allowSelf || de.Damager != this) && (mostTotal == null || de.DamageGiven > mostTotal.DamageGiven))
					mostTotal = de;
			}

			return mostTotal;
		}

		public Mobile FindLeastTotalDamger(bool allowSelf)
		{
			return GetDamagerFrom(this.FindLeastTotalDamageEntry(allowSelf));
		}

		public DamageEntry FindLeastTotalDamageEntry(bool allowSelf)
		{
			DamageEntry mostTotal = null;

			for (int i = this.m_DamageEntries.Count - 1; i >= 0; --i)
			{
				if (i >= this.m_DamageEntries.Count)
					continue;

				DamageEntry de = this.m_DamageEntries[i];

				if (de.HasExpired)
					this.m_DamageEntries.RemoveAt(i);
				else if ((allowSelf || de.Damager != this) && (mostTotal == null || de.DamageGiven < mostTotal.DamageGiven))
					mostTotal = de;
			}

			return mostTotal;
		}

		public DamageEntry FindDamageEntryFor(Mobile m)
		{
			for (int i = this.m_DamageEntries.Count - 1; i >= 0; --i)
			{
				if (i >= this.m_DamageEntries.Count)
					continue;

				DamageEntry de = this.m_DamageEntries[i];

				if (de.HasExpired)
					this.m_DamageEntries.RemoveAt(i);
				else if (de.Damager == m)
					return de;
			}

			return null;
		}

		public virtual Mobile GetDamageMaster(Mobile damagee)
		{
			return null;
		}

		public virtual DamageEntry RegisterDamage(int amount, Mobile from)
		{
			DamageEntry de = this.FindDamageEntryFor(from);

			if (de == null)
				de = new DamageEntry(from);

			de.DamageGiven += amount;
			de.LastDamage = DateTime.Now;

			this.m_DamageEntries.Remove(de);
			this.m_DamageEntries.Add(de);

			Mobile master = from.GetDamageMaster(this);

			if (master != null)
			{
				List<DamageEntry> list = de.Responsible;

				if (list == null)
					de.Responsible = list = new List<DamageEntry>();

				DamageEntry resp = null;

				for (int i = 0; i < list.Count; ++i)
				{
					DamageEntry check = list[i];

					if (check.Damager == master)
					{
						resp = check;
						break;
					}
				}

				if (resp == null)
					list.Add(resp = new DamageEntry(master));

				resp.DamageGiven += amount;
				resp.LastDamage = DateTime.Now;
			}

			return de;
		}

		private Mobile m_LastKiller;

		[CommandProperty(AccessLevel.GameMaster)]
		public Mobile LastKiller
		{
			get
			{
				return this.m_LastKiller;
			}
			set
			{
				this.m_LastKiller = value;
			}
		}

		/// <summary>
		/// Overridable. Virtual event invoked when the Mobile is <see cref="Damage">damaged</see>. It is called before <see cref="Hits">hit points</see> are lowered or the Mobile is <see cref="Kill">killed</see>.
		/// <seealso cref="Damage" />
		/// <seealso cref="Hits" />
		/// <seealso cref="Kill" />
		/// </summary>
		public virtual void OnDamage(int amount, Mobile from, bool willKill)
		{
		}

		public virtual void Damage(int amount)
		{
			this.Damage(amount, null);
		}

		public virtual bool CanBeDamaged()
		{
			return !this.m_Blessed;
		}

		public virtual void Damage(int amount, Mobile from)
		{
			this.Damage(amount, from, true);
		}

		public virtual void Damage(int amount, Mobile from, bool informMount)
		{
			if (!this.CanBeDamaged() || this.m_Deleted)
				return;

			if (!this.Region.OnDamage(this, ref amount))
				return;

			if (amount > 0)
			{
				int oldHits = this.Hits;
				int newHits = oldHits - amount;

				if (this.m_Spell != null)
					this.m_Spell.OnCasterHurt();

				//if ( m_Spell != null && m_Spell.State == SpellState.Casting )
				//	m_Spell.Disturb( DisturbType.Hurt, false, true );

				if (from != null)
					this.RegisterDamage(amount, from);

				this.DisruptiveAction();

				this.Paralyzed = false;

				if (this.Asleep)
				{
					this.Asleep = false;

                    if (from != null)
                        from.Send(SpeedControl.Disable);
				}
				
				switch( m_VisibleDamageType )
				{
					case VisibleDamageType.Related:
						{
							NetState ourState = this.m_NetState, theirState = (from == null ? null : from.m_NetState);

							if (ourState == null)
							{
								Mobile master = this.GetDamageMaster(from);

								if (master != null)
									ourState = master.m_NetState;
							}

							if (theirState == null && from != null)
							{
								Mobile master = from.GetDamageMaster(this);

								if (master != null)
									theirState = master.m_NetState;
							}

							if (amount > 0 && (ourState != null || theirState != null))
							{
								Packet p = null;// = new DamagePacket( this, amount );

								if (ourState != null)
								{
									if (ourState.DamagePacket)
										p = Packet.Acquire(new DamagePacket(this, amount));
									else
										p = Packet.Acquire(new DamagePacketOld(this, amount));

									ourState.Send(p);
								}

								if (theirState != null && theirState != ourState)
								{
									bool newPacket = theirState.DamagePacket;

									if (newPacket && (p == null || !(p is DamagePacket)))
									{
										Packet.Release(p);
										p = Packet.Acquire(new DamagePacket(this, amount));
									}
									else if (!newPacket && (p == null || !(p is DamagePacketOld)))
									{
										Packet.Release(p);
										p = Packet.Acquire(new DamagePacketOld(this, amount));
									}

									theirState.Send(p);
								}

								Packet.Release(p);
							}

							break;
						}
					case VisibleDamageType.Everyone:
						{
							this.SendDamageToAll(amount);
							break;
						}
				}

				this.OnDamage(amount, from, newHits < 0);

				IMount m = this.Mount;
				if (m != null && informMount)
					m.OnRiderDamaged(amount, from, newHits < 0);

				if (newHits < 0)
				{
					this.m_LastKiller = from;

					this.Hits = 0;

					if (oldHits >= 0)
						this.Kill();
				}
				else
				{
					this.Hits = newHits;
				}
			}
		}

		public virtual void SendDamageToAll(int amount)
		{
			if (amount < 0)
				return;

			Map map = this.m_Map;

			if (map == null)
				return;

			IPooledEnumerable eable = map.GetClientsInRange(this.m_Location);

			Packet pNew = null;
			Packet pOld = null;

			foreach (NetState ns in eable)
			{
				if (ns.Mobile.CanSee(this))
				{
					Packet p;

					if (ns.DamagePacket)
					{
						if (pNew == null)
							pNew = Packet.Acquire(new DamagePacket(this, amount));

						p = pNew;
					}
					else
					{
						if (pOld == null)
							pOld = Packet.Acquire(new DamagePacketOld(this, amount));

						p = pOld;
					}

					ns.Send(p);
				}
			}

			Packet.Release(pNew);
			Packet.Release(pOld);

			eable.Free();
		}

		public void Heal(int amount)
		{
			this.Heal(amount, this, true);
		}

		public void Heal(int amount, Mobile from)
		{
			this.Heal(amount, from, true);
		}

		public void Heal(int amount, Mobile from, bool message)
		{
			if (!this.Alive || this.IsDeadBondedPet)
				return;

			if (!this.Region.OnHeal(this, ref amount))
				return;

			this.OnHeal(ref amount, from);

			if ((this.Hits + amount) > this.HitsMax)
			{
				amount = this.HitsMax - this.Hits;
			}

			this.Hits += amount;

			if (message && amount > 0 && this.m_NetState != null)
				this.m_NetState.Send(new MessageLocalizedAffix(Serial.MinusOne, -1, MessageType.Label, 0x3B2, 3, 1008158, "", AffixType.Append | AffixType.System, amount.ToString(), ""));
		}

		public virtual void OnHeal(ref int amount, Mobile from)
		{
		}

		public void UsedStuckMenu()
		{
			if (this.m_StuckMenuUses == null)
			{
				this.m_StuckMenuUses = new DateTime[2];
			}

			for (int i = 0; i < this.m_StuckMenuUses.Length; ++i)
			{
				if ((DateTime.Now - this.m_StuckMenuUses[i]) > TimeSpan.FromDays(1.0))
				{
					this.m_StuckMenuUses[i] = DateTime.Now;
					return;
				}
			}
		}

		[CommandProperty(AccessLevel.GameMaster)]
		public bool Squelched
		{
			get
			{
				return this.m_Squelched;
			}
			set
			{
				this.m_Squelched = value;
			}
		}

		public virtual void Deserialize(GenericReader reader)
		{
			int version = reader.ReadInt();

			switch( version )
			{
				case 31:
					{
						this.m_LastStrGain = reader.ReadDeltaTime();
						this.m_LastIntGain = reader.ReadDeltaTime();
						this.m_LastDexGain = reader.ReadDeltaTime();

						goto case 30;
					}
				case 30:
					{
						byte hairflag = reader.ReadByte();

						if ((hairflag & 0x01) != 0)
							this.m_Hair = new HairInfo(reader);
						if ((hairflag & 0x02) != 0)
							this.m_FacialHair = new FacialHairInfo(reader);

						goto case 29;
					}
				case 29:
					{
						this.m_Race = reader.ReadRace();
						goto case 28;
					}
				case 28:
					{
						if (version <= 30)
							this.LastStatGain = reader.ReadDeltaTime();

						goto case 27;
					}
				case 27:
					{
						this.m_TithingPoints = reader.ReadInt();

						goto case 26;
					}
				case 26:
				case 25:
				case 24:
					{
						this.m_Corpse = reader.ReadItem() as Container;

						goto case 23;
					}
				case 23:
					{
						this.m_CreationTime = reader.ReadDateTime();

						goto case 22;
					}
				case 22: // Just removed followers
				case 21:
					{
						this.m_Stabled = reader.ReadStrongMobileList();

						goto case 20;
					}
				case 20:
					{
						this.m_CantWalk = reader.ReadBool();

						goto case 19;
					}
				case 19: // Just removed variables
				case 18:
					{
						this.m_Virtues = new VirtueInfo(reader);

						goto case 17;
					}
				case 17:
					{
						this.m_Thirst = reader.ReadInt();
						this.m_BAC = reader.ReadInt();

						goto case 16;
					}
				case 16:
					{
						this.m_ShortTermMurders = reader.ReadInt();

						if (version <= 24)
						{
							reader.ReadDateTime();
							reader.ReadDateTime();
						}

						goto case 15;
					}
				case 15:
					{
						if (version < 22)
							reader.ReadInt(); // followers

						this.m_FollowersMax = reader.ReadInt();

						goto case 14;
					}
				case 14:
					{
						this.m_MagicDamageAbsorb = reader.ReadInt();

						goto case 13;
					}
				case 13:
					{
						this.m_GuildFealty = reader.ReadMobile();

						goto case 12;
					}
				case 12:
					{
						this.m_Guild = reader.ReadGuild();

						goto case 11;
					}
				case 11:
					{
						this.m_DisplayGuildTitle = reader.ReadBool();

						goto case 10;
					}
				case 10:
					{
						this.m_CanSwim = reader.ReadBool();

						goto case 9;
					}
				case 9:
					{
						this.m_Squelched = reader.ReadBool();

						goto case 8;
					}
				case 8:
					{
						this.m_Holding = reader.ReadItem();

						goto case 7;
					}
				case 7:
					{
						this.m_VirtualArmor = reader.ReadInt();

						goto case 6;
					}
				case 6:
					{
						this.m_BaseSoundID = reader.ReadInt();

						goto case 5;
					}
				case 5:
					{
						this.m_DisarmReady = reader.ReadBool();
						this.m_StunReady = reader.ReadBool();

						goto case 4;
					}
				case 4:
					{
						if (version <= 25)
						{
							Poison.Deserialize(reader);
						}

						goto case 3;
					}
				case 3:
					{
						this.m_StatCap = reader.ReadInt();

						goto case 2;
					}
				case 2:
					{
						this.m_NameHue = reader.ReadInt();

						goto case 1;
					}
				case 1:
					{
						this.m_Hunger = reader.ReadInt();

						goto case 0;
					}
				case 0:
					{
						if (version < 21)
							this.m_Stabled = new List<Mobile>();

						if (version < 18)
							this.m_Virtues = new VirtueInfo();

						if (version < 11)
							this.m_DisplayGuildTitle = true;

						if (version < 3)
							this.m_StatCap = 225;

						if (version < 15)
						{
							this.m_Followers = 0;
							this.m_FollowersMax = 5;
						}

						this.m_Location = reader.ReadPoint3D();
						this.m_Body = new Body(reader.ReadInt());
						this.m_Name = reader.ReadString();
						this.m_GuildTitle = reader.ReadString();
						this.m_Criminal = reader.ReadBool();
						this.m_Kills = reader.ReadInt();
						this.m_SpeechHue = reader.ReadInt();
						this.m_EmoteHue = reader.ReadInt();
						this.m_WhisperHue = reader.ReadInt();
						this.m_YellHue = reader.ReadInt();
						this.m_Language = reader.ReadString();
						this.m_Female = reader.ReadBool();
						this.m_Warmode = reader.ReadBool();
						this.m_Hidden = reader.ReadBool();
						this.m_Direction = (Direction)reader.ReadByte();
						this.m_Hue = reader.ReadInt();
						this.m_Str = reader.ReadInt();
						this.m_Dex = reader.ReadInt();
						this.m_Int = reader.ReadInt();
						this.m_Hits = reader.ReadInt();
						this.m_Stam = reader.ReadInt();
						this.m_Mana = reader.ReadInt();
						this.m_Map = reader.ReadMap();
						this.m_Blessed = reader.ReadBool();
						this.m_Fame = reader.ReadInt();
						this.m_Karma = reader.ReadInt();
						this.m_AccessLevel = (AccessLevel)reader.ReadByte();

						this.m_Skills = new Skills(this, reader);

						this.m_Items = reader.ReadStrongItemList();

						this.m_Player = reader.ReadBool();
						this.m_Title = reader.ReadString();
						this.m_Profile = reader.ReadString();
						this.m_ProfileLocked = reader.ReadBool();

						if (version <= 18)
						{
							reader.ReadInt();
							reader.ReadInt();
							reader.ReadInt();
						}

						this.m_AutoPageNotify = reader.ReadBool();

						this.m_LogoutLocation = reader.ReadPoint3D();
						this.m_LogoutMap = reader.ReadMap();

						this.m_StrLock = (StatLockType)reader.ReadByte();
						this.m_DexLock = (StatLockType)reader.ReadByte();
						this.m_IntLock = (StatLockType)reader.ReadByte();

						this.m_StatMods = new List<StatMod>();
						this.m_SkillMods = new List<SkillMod>();

						if (reader.ReadBool())
						{
							this.m_StuckMenuUses = new DateTime[reader.ReadInt()];

							for (int i = 0; i < this.m_StuckMenuUses.Length; ++i)
							{
								this.m_StuckMenuUses[i] = reader.ReadDateTime();
							}
						}
						else
						{
							this.m_StuckMenuUses = null;
						}

						if (this.m_Player && this.m_Map != Map.Internal)
						{
							this.m_LogoutLocation = this.m_Location;
							this.m_LogoutMap = this.m_Map;

							this.m_Map = Map.Internal;
						}

						if (this.m_Map != null)
							this.m_Map.OnEnter(this);

						if (this.m_Criminal)
						{
							if (this.m_ExpireCriminal == null)
								this.m_ExpireCriminal = new ExpireCriminalTimer(this);

							this.m_ExpireCriminal.Start();
						}

						if (this.ShouldCheckStatTimers)
							this.CheckStatTimers();

						if (!this.m_Player && this.m_Dex <= 100 && this.m_CombatTimer != null)
							this.m_CombatTimer.Priority = TimerPriority.FiftyMS;
						else if (this.m_CombatTimer != null)
							this.m_CombatTimer.Priority = TimerPriority.EveryTick;

						this.UpdateRegion();

						this.UpdateResistances();

						break;
					}
			}

			if (!this.m_Player)
				Utility.Intern(ref this.m_Name);

			Utility.Intern(ref this.m_Title);
			Utility.Intern(ref this.m_Language);
			/*	//Moved into cleanup in scripts.
			if( version < 30 )
			Timer.DelayCall( TimeSpan.Zero, new TimerCallback( ConvertHair ) );
			* */
		}

		public void ConvertHair()
		{
			Item hair;

			if ((hair = this.FindItemOnLayer(Layer.Hair)) != null)
			{
				this.HairItemID = hair.ItemID;
				this.HairHue = hair.Hue;
				hair.Delete();
			}

			if ((hair = this.FindItemOnLayer(Layer.FacialHair)) != null)
			{
				this.FacialHairItemID = hair.ItemID;
				this.FacialHairHue = hair.Hue;
				hair.Delete();
			}
		}

		public virtual bool ShouldCheckStatTimers
		{
			get
			{
				return true;
			}
		}

		public virtual void CheckStatTimers()
		{
			if (this.m_Deleted)
				return;

			if (this.Hits < this.HitsMax)
			{
				if (this.CanRegenHits)
				{
					if (this.m_HitsTimer == null)
						this.m_HitsTimer = new HitsTimer(this);

					this.m_HitsTimer.Start();
				}
				else if (this.m_HitsTimer != null)
				{
					this.m_HitsTimer.Stop();
				}
			}
			else
			{
				this.Hits = this.HitsMax;
			}

			if (this.Stam < this.StamMax)
			{
				if (this.CanRegenStam)
				{
					if (this.m_StamTimer == null)
						this.m_StamTimer = new StamTimer(this);

					this.m_StamTimer.Start();
				}
				else if (this.m_StamTimer != null)
				{
					this.m_StamTimer.Stop();
				}
			}
			else
			{
				this.Stam = this.StamMax;
			}

			if (this.Mana < this.ManaMax)
			{
				if (this.CanRegenMana)
				{
					if (this.m_ManaTimer == null)
						this.m_ManaTimer = new ManaTimer(this);

					this.m_ManaTimer.Start();
				}
				else if (this.m_ManaTimer != null)
				{
					this.m_ManaTimer.Stop();
				}
			}
			else
			{
				this.Mana = this.ManaMax;
			}
		}

		private DateTime m_CreationTime;

		[CommandProperty(AccessLevel.GameMaster)]
		public DateTime CreationTime
		{
			get
			{
				return this.m_CreationTime;
			}
		}

		int ISerializable.TypeReference
		{
			get
			{
				return this.m_TypeRef;
			}
		}

		int ISerializable.SerialIdentity
		{
			get
			{
				return this.m_Serial;
			}
		}

		public virtual void Serialize(GenericWriter writer)
		{
			writer.Write((int)31); // version

			writer.WriteDeltaTime(this.m_LastStrGain);
			writer.WriteDeltaTime(this.m_LastIntGain);
			writer.WriteDeltaTime(this.m_LastDexGain);

			byte hairflag = 0x00;

			if (this.m_Hair != null)
				hairflag |= 0x01;
			if (this.m_FacialHair != null)
				hairflag |= 0x02;

			writer.Write((byte)hairflag);

			if ((hairflag & 0x01) != 0)
				this.m_Hair.Serialize(writer);
			if ((hairflag & 0x02) != 0)
				this.m_FacialHair.Serialize(writer);

			writer.Write(this.Race);

			writer.Write((int)this.m_TithingPoints);

			writer.Write(this.m_Corpse);

			writer.Write(this.m_CreationTime);

			writer.Write(this.m_Stabled, true);

			writer.Write(this.m_CantWalk);

			VirtueInfo.Serialize(writer, this.m_Virtues);

			writer.Write(this.m_Thirst);
			writer.Write(this.m_BAC);

			writer.Write(this.m_ShortTermMurders);
			//writer.Write( m_ShortTermElapse );
			//writer.Write( m_LongTermElapse );

			//writer.Write( m_Followers );
			writer.Write(this.m_FollowersMax);

			writer.Write(this.m_MagicDamageAbsorb);

			writer.Write(this.m_GuildFealty);

			writer.Write(this.m_Guild);

			writer.Write(this.m_DisplayGuildTitle);

			writer.Write(this.m_CanSwim);

			writer.Write(this.m_Squelched);

			writer.Write(this.m_Holding);

			writer.Write(this.m_VirtualArmor);

			writer.Write(this.m_BaseSoundID);

			writer.Write(this.m_DisarmReady);
			writer.Write(this.m_StunReady);

			//Poison.Serialize( m_Poison, writer );

			writer.Write(this.m_StatCap);

			writer.Write(this.m_NameHue);

			writer.Write(this.m_Hunger);

			writer.Write(this.m_Location);
			writer.Write((int)this.m_Body);
			writer.Write(this.m_Name);
			writer.Write(this.m_GuildTitle);
			writer.Write(this.m_Criminal);
			writer.Write(this.m_Kills);
			writer.Write(this.m_SpeechHue);
			writer.Write(this.m_EmoteHue);
			writer.Write(this.m_WhisperHue);
			writer.Write(this.m_YellHue);
			writer.Write(this.m_Language);
			writer.Write(this.m_Female);
			writer.Write(this.m_Warmode);
			writer.Write(this.m_Hidden);
			writer.Write((byte)this.m_Direction);
			writer.Write(this.m_Hue);
			writer.Write(this.m_Str);
			writer.Write(this.m_Dex);
			writer.Write(this.m_Int);
			writer.Write(this.m_Hits);
			writer.Write(this.m_Stam);
			writer.Write(this.m_Mana);

			writer.Write(this.m_Map);

			writer.Write(this.m_Blessed);
			writer.Write(this.m_Fame);
			writer.Write(this.m_Karma);
			writer.Write((byte)this.m_AccessLevel);
			this.m_Skills.Serialize(writer);

			writer.Write(this.m_Items);

			writer.Write(this.m_Player);
			writer.Write(this.m_Title);
			writer.Write(this.m_Profile);
			writer.Write(this.m_ProfileLocked);
			writer.Write(this.m_AutoPageNotify);

			writer.Write(this.m_LogoutLocation);
			writer.Write(this.m_LogoutMap);

			writer.Write((byte)this.m_StrLock);
			writer.Write((byte)this.m_DexLock);
			writer.Write((byte)this.m_IntLock);

			if (this.m_StuckMenuUses != null)
			{
				writer.Write(true);

				writer.Write(this.m_StuckMenuUses.Length);

				for (int i = 0; i < this.m_StuckMenuUses.Length; ++i)
				{
					writer.Write(this.m_StuckMenuUses[i]);
				}
			}
			else
			{
				writer.Write(false);
			}
		}

		[CommandProperty(AccessLevel.GameMaster)]
		public int LightLevel
		{
			get
			{
				return this.m_LightLevel;
			}
			set
			{
				if (this.m_LightLevel != value)
				{
					this.m_LightLevel = value;

					this.CheckLightLevels(false);
					/*if ( m_NetState != null )
					m_NetState.Send( new PersonalLightLevel( this ) );*/
				}
			}
		}

		[CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
		public string Profile
		{
			get
			{
				return this.m_Profile;
			}
			set
			{
				this.m_Profile = value;
			}
		}

		[CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
		public bool ProfileLocked
		{
			get
			{
				return this.m_ProfileLocked;
			}
			set
			{
				this.m_ProfileLocked = value;
			}
		}

		[CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
		public bool Player
		{
			get
			{
				return this.m_Player;
			}
			set
			{
				this.m_Player = value;
				this.InvalidateProperties();

				if (!this.m_Player && this.m_Dex <= 100 && this.m_CombatTimer != null)
					this.m_CombatTimer.Priority = TimerPriority.FiftyMS;
				else if (this.m_CombatTimer != null)
					this.m_CombatTimer.Priority = TimerPriority.EveryTick;

				this.CheckStatTimers();
			}
		}

		[CommandProperty(AccessLevel.Decorator)]
		public string Title
		{
			get
			{
				return this.m_Title;
			}
			set
			{
				this.m_Title = value;
				this.InvalidateProperties();
			}
		}

		private static readonly string[] m_AccessLevelNames = new string[]
		{
			"Player",
			"VIP Player",
			"Counselor",
			"Decorator",
			"Spawner",
			"Game Master",
			"Seer",
			"Administrator",
			"Developer",
			"Co-Owner",
			"Owner"
		};

		public static string GetAccessLevelName(AccessLevel level)
		{
			return m_AccessLevelNames[(int)level];
		}

		public virtual bool CanPaperdollBeOpenedBy(Mobile from)
		{
			return (this.Body.IsHuman || this.Body.IsGhost || this.IsBodyMod);
		}

		public virtual void GetChildContextMenuEntries(Mobile from, List<ContextMenuEntry> list, Item item)
		{
		}

		public virtual void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
		{
			if (this.m_Deleted)
				return;

			if (this.CanPaperdollBeOpenedBy(from))
				list.Add(new PaperdollEntry(this));

			if (from == this && this.Backpack != null && this.CanSee(this.Backpack) && this.CheckAlive(false))
				list.Add(new OpenBackpackEntry(this));
		}

		public void Internalize()
		{
			this.Map = Map.Internal;
		}

		public List<Item> Items
		{
			get
			{
				return this.m_Items;
			}
		}

		/// <summary>
		/// Overridable. Virtual event invoked when <paramref name="item" /> is <see cref="AddItem">added</see> from the Mobile, such as when it is equiped.
		/// <seealso cref="Items" />
		/// <seealso cref="OnItemRemoved" />
		/// </summary>
		public virtual void OnItemAdded(Item item)
		{
		}

		/// <summary>
		/// Overridable. Virtual event invoked when <paramref name="item" /> is <see cref="RemoveItem">removed</see> from the Mobile.
		/// <seealso cref="Items" />
		/// <seealso cref="OnItemAdded" />
		/// </summary>
		public virtual void OnItemRemoved(Item item)
		{
		}

		/// <summary>
		/// Overridable. Virtual event invoked when <paramref name="item" /> is becomes a child of the Mobile; it's worn or contained at some level of the Mobile's <see cref="Mobile.Backpack">backpack</see> or <see cref="Mobile.BankBox">bank box</see>
		/// <seealso cref="OnSubItemRemoved" />
		/// <seealso cref="OnItemAdded" />
		/// </summary>
		public virtual void OnSubItemAdded(Item item)
		{
		}

		/// <summary>
		/// Overridable. Virtual event invoked when <paramref name="item" /> is removed from the Mobile, its <see cref="Mobile.Backpack">backpack</see>, or its <see cref="Mobile.BankBox">bank box</see>.
		/// <seealso cref="OnSubItemAdded" />
		/// <seealso cref="OnItemRemoved" />
		/// </summary>
		public virtual void OnSubItemRemoved(Item item)
		{
		}

		public virtual void OnItemBounceCleared(Item item)
		{
		}

		public virtual void OnSubItemBounceCleared(Item item)
		{
		}

		public virtual int MaxWeight
		{
			get
			{
				return int.MaxValue;
			}
		}

		public void AddItem(Item item)
		{
			if (item == null || item.Deleted)
				return;

			if (item.Parent == this)
				return;
			else if (item.Parent is Mobile)
				((Mobile)item.Parent).RemoveItem(item);
			else if (item.Parent is Item)
				((Item)item.Parent).RemoveItem(item);
			else
				item.SendRemovePacket();

			item.Parent = this;
			item.Map = this.m_Map;

			this.m_Items.Add(item);

			if (!item.IsVirtualItem)
			{
				this.UpdateTotal(item, TotalType.Gold, item.TotalGold);
				this.UpdateTotal(item, TotalType.Items, item.TotalItems + 1);
				this.UpdateTotal(item, TotalType.Weight, item.TotalWeight + item.PileWeight);
			}

			item.Delta(ItemDelta.Update);

			item.OnAdded(this);
			this.OnItemAdded(item);

			if (item.PhysicalResistance != 0 || item.FireResistance != 0 || item.ColdResistance != 0 ||
				item.PoisonResistance != 0 || item.EnergyResistance != 0)
				this.UpdateResistances();
		}

		private static IWeapon m_DefaultWeapon;

		public static IWeapon DefaultWeapon
		{
			get
			{
				return m_DefaultWeapon;
			}
			set
			{
				m_DefaultWeapon = value;
			}
		}

		public void RemoveItem(Item item)
		{
			if (item == null || this.m_Items == null)
				return;

			if (this.m_Items.Contains(item))
			{
				item.SendRemovePacket();

				//int oldCount = m_Items.Count;

				this.m_Items.Remove(item);

				if (!item.IsVirtualItem)
				{
					this.UpdateTotal(item, TotalType.Gold, -item.TotalGold);
					this.UpdateTotal(item, TotalType.Items, -(item.TotalItems + 1));
					this.UpdateTotal(item, TotalType.Weight, -(item.TotalWeight + item.PileWeight));
				}

				item.Parent = null;

				item.OnRemoved(this);
				this.OnItemRemoved(item);

				if (item.PhysicalResistance != 0 || item.FireResistance != 0 || item.ColdResistance != 0 ||
					item.PoisonResistance != 0 || item.EnergyResistance != 0)
					this.UpdateResistances();
			}
		}

		public virtual void Animate(int action, int frameCount, int repeatCount, bool forward, bool repeat, int delay)
		{
			Map map = this.m_Map;

			if (map != null)
			{
				this.ProcessDelta();

				Packet p = null;
				//Packet pNew = null;

				IPooledEnumerable eable = map.GetClientsInRange(this.m_Location);

				foreach (NetState state in eable)
				{
					if (state.Mobile.CanSee(this))
					{
						state.Mobile.ProcessDelta();

						#region SA
						if (p == null)
						{
							if (this.Race == Race.Gargoyle)
							{
								frameCount = 10;

								if (action >= 200 && action <= 259 && !this.Flying)
									action = 17;
								if (action >= 260 && action <= 270 && !this.Flying)
									action = 16;
								if (action >= 200 && action <= 259 && this.Flying)
									action = 75;
								if (action >= 260 && action <= 270 && this.Flying)
									action = 75;

								if (action == 31 && this.Flying)
									action = 71;
								if (action == 20 && this.Flying)
									action = 77;
								if (action >= 9 && action <= 11 && this.Flying)
									action = 71;
								if (action >= 12 && action <= 14 && this.Flying)
									action = 72;
								if (action == 34 && this.Flying)
									action = 78;
							}

							//if ( state.StygianAbyss )
							//	p = Packet.Acquire( new NewMobileAnimation( this, action, frameCount, delay ) );
							//else
							p = Packet.Acquire(new MobileAnimation(this, action, frameCount, repeatCount, forward, repeat, delay));
						}
						#endregion

						state.Send(p);
					}
				}

				Packet.Release(p);

				eable.Free();
			}
		}

		public void SendSound(int soundID)
		{
			if (soundID != -1 && this.m_NetState != null)
				this.Send(new PlaySound(soundID, this));
		}

		public void SendSound(int soundID, IPoint3D p)
		{
			if (soundID != -1 && this.m_NetState != null)
				this.Send(new PlaySound(soundID, p));
		}

		public void PlaySound(int soundID)
		{
			if (soundID == -1)
				return;

			if (this.m_Map != null)
			{
				Packet p = null;

				IPooledEnumerable eable = this.m_Map.GetClientsInRange(this.m_Location);

				foreach (NetState state in eable)
				{
					if (state.Mobile.CanSee(this))
					{
						if (p == null)
							p = Packet.Acquire(new PlaySound(soundID, this));

						state.Send(p);
					}
				}

				Packet.Release(p);

				eable.Free();
			}
		}

		[CommandProperty(AccessLevel.Counselor)]
		public Skills Skills
		{
			get
			{
				return this.m_Skills;
			}
			set
			{
			}
		}

		[CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
		public AccessLevel AccessLevel
		{
			get
			{
				return this.m_AccessLevel;
			}
			set
			{
				AccessLevel oldValue = this.m_AccessLevel;

				if (oldValue != value)
				{
					this.m_AccessLevel = value;
					this.Delta(MobileDelta.Noto);
					this.InvalidateProperties();

					this.SendMessage("Your access level has been changed. You are now {0}.", GetAccessLevelName(value));

					this.ClearScreen();
					this.SendEverything();

					this.OnAccessLevelChanged(oldValue);
				}
			}
		}

		public virtual void OnAccessLevelChanged(AccessLevel oldLevel)
		{
		}

		[CommandProperty(AccessLevel.Decorator)]
		public int Fame
		{
			get
			{
				return this.m_Fame;
			}
			set
			{
				int oldValue = this.m_Fame;

				if (oldValue != value)
				{
					this.m_Fame = value;

					if (this.ShowFameTitle && (this.m_Player || this.m_Body.IsHuman) && (oldValue >= 10000) != (value >= 10000))
						this.InvalidateProperties();

					this.OnFameChange(oldValue);
				}
			}
		}

		public virtual void OnFameChange(int oldValue)
		{
		}

		[CommandProperty(AccessLevel.Decorator)]
		public int Karma
		{
			get
			{
				return this.m_Karma;
			}
			set
			{
				int old = this.m_Karma;

				if (old != value)
				{
					this.m_Karma = value;
					this.OnKarmaChange(old);
				}
			}
		}

		public virtual void OnKarmaChange(int oldValue)
		{
		}

		// Mobile did something which should unhide him
		public virtual void RevealingAction()
		{
			if (this.m_Hidden && this.IsPlayer())
				this.Hidden = false;

			this.DisruptiveAction(); // Anything that unhides you will also distrupt meditation
		}

		#region Say/SayTo/Emote/Whisper/Yell
		public void SayTo(Mobile to, bool ascii, string text)
		{
			this.PrivateOverheadMessage(MessageType.Regular, this.m_SpeechHue, ascii, text, to.NetState);
		}

		public void SayTo(Mobile to, string text)
		{
			this.SayTo(to, false, text);
		}

		public void SayTo(Mobile to, string format, params object[] args)
		{
			this.SayTo(to, false, String.Format(format, args));
		}

		public void SayTo(Mobile to, bool ascii, string format, params object[] args)
		{
			this.SayTo(to, ascii, String.Format(format, args));
		}

		public void SayTo(Mobile to, int number)
		{
			to.Send(new MessageLocalized(this.m_Serial, this.Body, MessageType.Regular, this.m_SpeechHue, 3, number, this.Name, ""));
		}

		public void SayTo(Mobile to, int number, string args)
		{
			to.Send(new MessageLocalized(this.m_Serial, this.Body, MessageType.Regular, this.m_SpeechHue, 3, number, this.Name, args));
		}

		public void Say(bool ascii, string text)
		{
			this.PublicOverheadMessage(MessageType.Regular, this.m_SpeechHue, ascii, text);
		}

		public void Say(string text)
		{
			this.PublicOverheadMessage(MessageType.Regular, this.m_SpeechHue, false, text);
		}

		public void Say(string format, params object[] args)
		{
			this.Say(String.Format(format, args));
		}

		public void Say(int number, AffixType type, string affix, string args)
		{
			this.PublicOverheadMessage(MessageType.Regular, this.m_SpeechHue, number, type, affix, args);
		}

		public void Say(int number)
		{
			this.Say(number, "");
		}

		public void Say(int number, string args)
		{
			this.PublicOverheadMessage(MessageType.Regular, this.m_SpeechHue, number, args);
		}

		public void Emote(string text)
		{
			this.PublicOverheadMessage(MessageType.Emote, this.m_EmoteHue, false, text);
		}

		public void Emote(string format, params object[] args)
		{
			this.Emote(String.Format(format, args));
		}

		public void Emote(int number)
		{
			this.Emote(number, "");
		}

		public void Emote(int number, string args)
		{
			this.PublicOverheadMessage(MessageType.Emote, this.m_EmoteHue, number, args);
		}

		public void Whisper(string text)
		{
			this.PublicOverheadMessage(MessageType.Whisper, this.m_WhisperHue, false, text);
		}

		public void Whisper(string format, params object[] args)
		{
			this.Whisper(String.Format(format, args));
		}

		public void Whisper(int number)
		{
			this.Whisper(number, "");
		}

		public void Whisper(int number, string args)
		{
			this.PublicOverheadMessage(MessageType.Whisper, this.m_WhisperHue, number, args);
		}

		public void Yell(string text)
		{
			this.PublicOverheadMessage(MessageType.Yell, this.m_YellHue, false, text);
		}

		public void Yell(string format, params object[] args)
		{
			this.Yell(String.Format(format, args));
		}

		public void Yell(int number)
		{
			this.Yell(number, "");
		}

		public void Yell(int number, string args)
		{
			this.PublicOverheadMessage(MessageType.Yell, this.m_YellHue, number, args);
		}

		#endregion

		[CommandProperty(AccessLevel.Decorator)]
		public bool Blessed
		{
			get
			{
				return this.m_Blessed;
			}
			set
			{
				if (this.m_Blessed != value)
				{
					this.m_Blessed = value;
					this.Delta(MobileDelta.HealthbarYellow);
				}
			}
		}

		public void SendRemovePacket()
		{
			this.SendRemovePacket(true);
		}

		public void SendRemovePacket(bool everyone)
		{
			if (this.m_Map != null)
			{
				Packet p = null;

				IPooledEnumerable eable = this.m_Map.GetClientsInRange(this.m_Location);

				foreach (NetState state in eable)
				{
					if (state != this.m_NetState && (everyone || !state.Mobile.CanSee(this)))
					{
						if (p == null)
							p = this.RemovePacket;

						state.Send(p);
					}
				}

				eable.Free();
			}
		}

		public void ClearScreen()
		{
			NetState ns = this.m_NetState;

			if (this.m_Map != null && ns != null)
			{
				IPooledEnumerable eable = this.m_Map.GetObjectsInRange(this.m_Location, Core.GlobalMaxUpdateRange);

				foreach (object o in eable)
				{
					if (o is Mobile)
					{
						Mobile m = (Mobile)o;

						if (m != this && Utility.InUpdateRange(this.m_Location, m.m_Location))
							ns.Send(m.RemovePacket);
					}
					else if (o is Item)
					{
						Item item = (Item)o;

						if (this.InRange(item.Location, item.GetUpdateRange(this)))
							ns.Send(item.RemovePacket);
					}
				}

				eable.Free();
			}
		}

		public bool Send(Packet p)
		{
			return this.Send(p, false);
		}

		public bool Send(Packet p, bool throwOnOffline)
		{
			if (this.m_NetState != null)
			{
				this.m_NetState.Send(p);
				return true;
			}
			else if (throwOnOffline)
			{
				throw new MobileNotConnectedException(this, "Packet could not be sent.");
			}
			else
			{
				return false;
			}
		}

		#region Gumps/Menus

		public bool SendHuePicker(HuePicker p)
		{
			return this.SendHuePicker(p, false);
		}

		public bool SendHuePicker(HuePicker p, bool throwOnOffline)
		{
			if (this.m_NetState != null)
			{
				p.SendTo(this.m_NetState);
				return true;
			}
			else if (throwOnOffline)
			{
				throw new MobileNotConnectedException(this, "Hue picker could not be sent.");
			}
			else
			{
				return false;
			}
		}

		public Gump FindGump(Type type)
		{
			NetState ns = this.m_NetState;

			if (ns != null)
			{
				foreach (Gump gump in ns.Gumps)
				{
					if (type.IsAssignableFrom(gump.GetType()))
					{
						return gump;
					}
				}
			}

			return null;
		}

		public bool CloseGump(Type type)
		{
			if (this.m_NetState != null)
			{
				Gump gump = this.FindGump(type);

				if (gump != null)
				{
					this.m_NetState.Send(new CloseGump(gump.TypeID, 0));

					this.m_NetState.RemoveGump(gump);

					gump.OnServerClose(this.m_NetState);
				}

				return true;
			}
			else
			{
				return false;
			}
		}

		[Obsolete("Use CloseGump( Type ) instead.")]
		public bool CloseGump(Type type, int buttonID)
		{
			return this.CloseGump(type);
		}

		[Obsolete("Use CloseGump( Type ) instead.")]
		public bool CloseGump(Type type, int buttonID, bool throwOnOffline)
		{
			return this.CloseGump(type);
		}

		public bool CloseAllGumps()
		{
			NetState ns = this.m_NetState;

			if (ns != null)
			{
				List<Gump> gumps = new List<Gump>(ns.Gumps);

				ns.ClearGumps();

				foreach (Gump gump in gumps)
				{
					ns.Send(new CloseGump(gump.TypeID, 0));

					gump.OnServerClose(ns);
				}

				return true;
			}
			else
			{
				return false;
			}
		}

		[Obsolete("Use CloseAllGumps() instead.", false)]
		public bool CloseAllGumps(bool throwOnOffline)
		{
			return this.CloseAllGumps();
		}

		public bool HasGump(Type type)
		{
			return (this.FindGump(type) != null);
		}

		[Obsolete("Use HasGump( Type ) instead.", false)]
		public bool HasGump(Type type, bool throwOnOffline)
		{
			return this.HasGump(type);
		}

		public bool SendGump(Gump g)
		{
			return this.SendGump(g, false);
		}

		public bool SendGump(Gump g, bool throwOnOffline)
		{
			if (this.m_NetState != null)
			{
				g.SendTo(this.m_NetState);
				return true;
			}
			else if (throwOnOffline)
			{
				throw new MobileNotConnectedException(this, "Gump could not be sent.");
			}
			else
			{
				return false;
			}
		}

		public bool SendMenu(IMenu m)
		{
			return this.SendMenu(m, false);
		}

		public bool SendMenu(IMenu m, bool throwOnOffline)
		{
			if (this.m_NetState != null)
			{
				m.SendTo(this.m_NetState);
				return true;
			}
			else if (throwOnOffline)
			{
				throw new MobileNotConnectedException(this, "Menu could not be sent.");
			}
			else
			{
				return false;
			}
		}

		#endregion

		/// <summary>
		/// Overridable. Event invoked before the Mobile says something.
		/// <seealso cref="DoSpeech" />
		/// </summary>
		public virtual void OnSaid(SpeechEventArgs e)
		{
			if (this.m_Squelched)
			{
				if (Core.ML)
					this.SendLocalizedMessage(500168); // You can not say anything, you have been muted.
				else
					this.SendMessage("You can not say anything, you have been squelched."); //Cliloc ITSELF changed during ML.

				e.Blocked = true;
			}

			if (!e.Blocked)
				this.RevealingAction();
		}

		public virtual bool HandlesOnSpeech(Mobile from)
		{
			return false;
		}

		/// <summary>
		/// Overridable. Virtual event invoked when the Mobile hears speech. This event will only be invoked if <see cref="HandlesOnSpeech" /> returns true.
		/// <seealso cref="DoSpeech" />
		/// </summary>
		public virtual void OnSpeech(SpeechEventArgs e)
		{
		}

		public void SendEverything()
		{
			NetState ns = this.m_NetState;

			if (this.m_Map != null && ns != null)
			{
				IPooledEnumerable eable = this.m_Map.GetObjectsInRange(this.m_Location, Core.GlobalMaxUpdateRange);

				foreach (object o in eable)
				{
					if (o is Item)
					{
						Item item = (Item)o;

						if (this.CanSee(item) && this.InRange(item.Location, item.GetUpdateRange(this)))
							item.SendInfoTo(ns);
					}
					else if (o is Mobile)
					{
						Mobile m = (Mobile)o;

						if (this.CanSee(m) && Utility.InUpdateRange(this.m_Location, m.m_Location))
						{
							if (ns.StygianAbyss)
							{
								ns.Send(new MobileIncoming(this, m));

								if (m.Poisoned)
									ns.Send(new HealthbarPoison(m));

								if (m.Blessed || m.YellowHealthbar)
									ns.Send(new HealthbarYellow(m));
							}
							else
							{
								ns.Send(new MobileIncomingOld(this, m));
							}

							if (m.IsDeadBondedPet)
								ns.Send(new BondedStatus(0, m.m_Serial, 1));

							if (ObjectPropertyList.Enabled)
							{
								ns.Send(m.OPLPacket);
								//foreach ( Item item in m.m_Items )
								//	ns.Send( item.OPLPacket );
							}
						}
					}
				}

				eable.Free();
			}
		}

		[CommandProperty(AccessLevel.Counselor, AccessLevel.Decorator)]
		public Map Map
		{
			get
			{
				return this.m_Map;
			}
			set
			{
				if (this.m_Deleted)
					return;

				if (this.m_Map != value)
				{
					if (this.m_NetState != null)
						this.m_NetState.ValidateAllTrades();

					Map oldMap = this.m_Map;

					if (this.m_Map != null)
					{
						this.m_Map.OnLeave(this);

						this.ClearScreen();
						this.SendRemovePacket();
					}

					for (int i = 0; i < this.m_Items.Count; ++i)
						this.m_Items[i].Map = value;

					this.m_Map = value;

					this.UpdateRegion();

					if (this.m_Map != null)
						this.m_Map.OnEnter(this);

					NetState ns = this.m_NetState;

					if (ns != null && this.m_Map != null)
					{
						ns.Sequence = 0;
						ns.Send(new MapChange(this));
						ns.Send(new MapPatches());
						ns.Send(SeasonChange.Instantiate(this.GetSeason(), true));

						if (ns.StygianAbyss)
							ns.Send(new MobileUpdate(this));
						else
							ns.Send(new MobileUpdateOld(this));

						this.ClearFastwalkStack();
					}

					if (ns != null)
					{
						if (this.m_Map != null)
							this.Send(new ServerChange(this, this.m_Map));

						ns.Sequence = 0;
						this.ClearFastwalkStack();

						if (ns.StygianAbyss)
						{
							this.Send(new MobileIncoming(this, this));
							this.Send(new MobileUpdate(this));
							this.CheckLightLevels(true);
							this.Send(new MobileUpdate(this));
						}
						else
						{
							this.Send(new MobileIncomingOld(this, this));
							this.Send(new MobileUpdateOld(this));
							this.CheckLightLevels(true);
							this.Send(new MobileUpdateOld(this));
						}
					}

					this.SendEverything();
					this.SendIncomingPacket();

					if (ns != null)
					{
						ns.Sequence = 0;
						this.ClearFastwalkStack();

						if (ns.StygianAbyss)
						{
							this.Send(new MobileIncoming(this, this));
							this.Send(SupportedFeatures.Instantiate(ns));
							this.Send(new MobileUpdate(this));
							this.Send(new MobileAttributes(this));
						}
						else
						{
							this.Send(new MobileIncomingOld(this, this));
							this.Send(SupportedFeatures.Instantiate(ns));
							this.Send(new MobileUpdateOld(this));
							this.Send(new MobileAttributes(this));
						}
					}

					this.OnMapChange(oldMap);
				}
			}
		}

		public void UpdateRegion()
		{
			if (this.m_Deleted)
				return;

			Region newRegion = Region.Find(this.m_Location, this.m_Map);

			if (newRegion != this.m_Region)
			{
				Region.OnRegionChange(this, this.m_Region, newRegion);

				this.m_Region = newRegion;
				this.OnRegionChange(this.m_Region, newRegion);
			}
		}

		/// <summary>
		/// Overridable. Virtual event invoked when <see cref="Map" /> changes.
		/// </summary>
		protected virtual void OnMapChange(Map oldMap)
		{
		}

		#region Beneficial Checks/Actions

		public virtual bool CanBeBeneficial(Mobile target)
		{
			return this.CanBeBeneficial(target, true, false);
		}

		public virtual bool CanBeBeneficial(Mobile target, bool message)
		{
			return this.CanBeBeneficial(target, message, false);
		}

		public virtual bool CanBeBeneficial(Mobile target, bool message, bool allowDead)
		{
			if (target == null)
				return false;

			if (this.m_Deleted || target.m_Deleted || !this.Alive || this.IsDeadBondedPet || (!allowDead && (!target.Alive || target.IsDeadBondedPet)))
			{
				if (message)
					this.SendLocalizedMessage(1001017); // You can not perform beneficial acts on your target.

				return false;
			}

			if (target == this)
				return true;

			if (/*m_Player &&*/ !this.Region.AllowBeneficial(this, target))
			{
				// TODO: Pets
				//if ( !(target.m_Player || target.Body.IsHuman || target.Body.IsAnimal) )
				//{
				if (message)
					this.SendLocalizedMessage(1001017); // You can not perform beneficial acts on your target.

				return false;
				//}
			}

			return true;
		}

		public virtual bool IsBeneficialCriminal(Mobile target)
		{
			if (this == target)
				return false;

			int n = Notoriety.Compute(this, target);

			return (n == Notoriety.Criminal || n == Notoriety.Murderer);
		}

		/// <summary>
		/// Overridable. Event invoked when the Mobile <see cref="DoBeneficial">does a beneficial action</see>.
		/// </summary>
		public virtual void OnBeneficialAction(Mobile target, bool isCriminal)
		{
			if (isCriminal)
				this.CriminalAction(false);
		}

		public virtual void DoBeneficial(Mobile target)
		{
			if (target == null)
				return;

			this.OnBeneficialAction(target, this.IsBeneficialCriminal(target));

			this.Region.OnBeneficialAction(this, target);
			target.Region.OnGotBeneficialAction(this, target);
		}

		public virtual bool BeneficialCheck(Mobile target)
		{
			if (this.CanBeBeneficial(target, true))
			{
				this.DoBeneficial(target);
				return true;
			}

			return false;
		}
		
		#endregion

		#region Harmful Checks/Actions

		public virtual bool CanBeHarmful(Mobile target)
		{
			return this.CanBeHarmful(target, true);
		}

		public virtual bool CanBeHarmful(Mobile target, bool message)
		{
			return this.CanBeHarmful(target, message, false);
		}

		public virtual bool CanBeHarmful(Mobile target, bool message, bool ignoreOurBlessedness)
		{
			if (target == null)
				return false;

			if (this.m_Deleted || (!ignoreOurBlessedness && this.m_Blessed) || target.m_Deleted || target.m_Blessed || !this.Alive || this.IsDeadBondedPet || !target.Alive || target.IsDeadBondedPet)
			{
				if (message)
					this.SendLocalizedMessage(1001018); // You can not perform negative acts on your target.

				return false;
			}

			if (target == this)
				return true;

			// TODO: Pets
			if (/*m_Player &&*/ !this.Region.AllowHarmful(this, target))//(target.m_Player || target.Body.IsHuman) && !Region.AllowHarmful( this, target )  )
			{
				if (message)
					this.SendLocalizedMessage(1001018); // You can not perform negative acts on your target.

				return false;
			}

			return true;
		}

		public virtual bool IsHarmfulCriminal(Mobile target)
		{
			if (this == target)
				return false;

			return (Notoriety.Compute(this, target) == Notoriety.Innocent);
		}

		/// <summary>
		/// Overridable. Event invoked when the Mobile <see cref="DoHarmful">does a harmful action</see>.
		/// </summary>
		public virtual void OnHarmfulAction(Mobile target, bool isCriminal)
		{
			if (isCriminal)
				this.CriminalAction(false);
		}

		public virtual void DoHarmful(Mobile target)
		{
			this.DoHarmful(target, false);
		}

		public virtual void DoHarmful(Mobile target, bool indirect)
		{
			if (target == null || this.m_Deleted)
				return;

			bool isCriminal = this.IsHarmfulCriminal(target);

			this.OnHarmfulAction(target, isCriminal);
			target.AggressiveAction(this, isCriminal);

			this.Region.OnDidHarmful(this, target);
			target.Region.OnGotHarmful(this, target);

			if (!indirect)
				this.Combatant = target;

			if (this.m_ExpireCombatant == null)
				this.m_ExpireCombatant = new ExpireCombatantTimer(this);
			else
				this.m_ExpireCombatant.Stop();

			this.m_ExpireCombatant.Start();
		}

		public virtual bool HarmfulCheck(Mobile target)
		{
			if (this.CanBeHarmful(target))
			{
				this.DoHarmful(target);
				return true;
			}

			return false;
		}

		#endregion

		#region Stats

		/// <summary>
		/// Gets a list of all <see cref="StatMod">StatMod's</see> currently active for the Mobile.
		/// </summary>
		public List<StatMod> StatMods
		{
			get
			{
				return this.m_StatMods;
			}
		}

		public bool RemoveStatMod(string name)
		{
			for (int i = 0; i < this.m_StatMods.Count; ++i)
			{
				StatMod check = this.m_StatMods[i];

				if (check.Name == name)
				{
					this.m_StatMods.RemoveAt(i);
					this.CheckStatTimers();
					this.Delta(MobileDelta.Stat | this.GetStatDelta(check.Type));
					return true;
				}
			}

			return false;
		}

		public StatMod GetStatMod(string name)
		{
			for (int i = 0; i < this.m_StatMods.Count; ++i)
			{
				StatMod check = this.m_StatMods[i];

				if (check.Name == name)
					return check;
			}

			return null;
		}

		public void AddStatMod(StatMod mod)
		{
			for (int i = 0; i < this.m_StatMods.Count; ++i)
			{
				StatMod check = this.m_StatMods[i];

				if (check.Name == mod.Name)
				{
					this.Delta(MobileDelta.Stat | this.GetStatDelta(check.Type));
					this.m_StatMods.RemoveAt(i);
					break;
				}
			}

			this.m_StatMods.Add(mod);
			this.Delta(MobileDelta.Stat | this.GetStatDelta(mod.Type));
			this.CheckStatTimers();
		}

		private MobileDelta GetStatDelta(StatType type)
		{
			MobileDelta delta = 0;

			if ((type & StatType.Str) != 0)
				delta |= MobileDelta.Hits;

			if ((type & StatType.Dex) != 0)
				delta |= MobileDelta.Stam;

			if ((type & StatType.Int) != 0)
				delta |= MobileDelta.Mana;

			return delta;
		}

		/// <summary>
		/// Computes the total modified offset for the specified stat type. Expired <see cref="StatMod" /> instances are removed.
		/// </summary>
		public int GetStatOffset(StatType type)
		{
			int offset = 0;

			for (int i = 0; i < this.m_StatMods.Count; ++i)
			{
				StatMod mod = this.m_StatMods[i];

				if (mod.HasElapsed())
				{
					this.m_StatMods.RemoveAt(i);
					this.Delta(MobileDelta.Stat | this.GetStatDelta(mod.Type));
					this.CheckStatTimers();

					--i;
				}
				else if ((mod.Type & type) != 0)
				{
					offset += mod.Offset;
				}
			}

			return offset;
		}

		/// <summary>
		/// Overridable. Virtual event invoked when the <see cref="RawStr" /> changes.
		/// <seealso cref="RawStr" />
		/// <seealso cref="OnRawStatChange" />
		/// </summary>
		public virtual void OnRawStrChange(int oldValue)
		{
		}

		/// <summary>
		/// Overridable. Virtual event invoked when <see cref="RawDex" /> changes.
		/// <seealso cref="RawDex" />
		/// <seealso cref="OnRawStatChange" />
		/// </summary>
		public virtual void OnRawDexChange(int oldValue)
		{
		}

		/// <summary>
		/// Overridable. Virtual event invoked when the <see cref="RawInt" /> changes.
		/// <seealso cref="RawInt" />
		/// <seealso cref="OnRawStatChange" />
		/// </summary>
		public virtual void OnRawIntChange(int oldValue)
		{
		}

		/// <summary>
		/// Overridable. Virtual event invoked when the <see cref="RawStr" />, <see cref="RawDex" />, or <see cref="RawInt" /> changes.
		/// <seealso cref="OnRawStrChange" />
		/// <seealso cref="OnRawDexChange" />
		/// <seealso cref="OnRawIntChange" />
		/// </summary>
		public virtual void OnRawStatChange(StatType stat, int oldValue)
		{
		}

		/// <summary>
		/// Gets or sets the base, unmodified, strength of the Mobile. Ranges from 1 to 65000, inclusive.
		/// <seealso cref="Str" />
		/// <seealso cref="StatMod" />
		/// <seealso cref="OnRawStrChange" />
		/// <seealso cref="OnRawStatChange" />
		/// </summary>
		[CommandProperty(AccessLevel.GameMaster)]
		public int RawStr
		{
			get
			{
				return this.m_Str;
			}
			set
			{
				if (value < 1)
					value = 1;
				else if (value > 65000)
					value = 65000;

				if (this.m_Str != value)
				{
					int oldValue = this.m_Str;

					this.m_Str = value;
					this.Delta(MobileDelta.Stat | MobileDelta.Hits);

					if (this.Hits < this.HitsMax)
					{
						if (this.m_HitsTimer == null)
							this.m_HitsTimer = new HitsTimer(this);

						this.m_HitsTimer.Start();
					}
					else if (this.Hits > this.HitsMax)
					{
						this.Hits = this.HitsMax;
					}

					this.OnRawStrChange(oldValue);
					this.OnRawStatChange(StatType.Str, oldValue);
				}
			}
		}

		/// <summary>
		/// Gets or sets the effective strength of the Mobile. This is the sum of the <see cref="RawStr" /> plus any additional modifiers. Any attempts to set this value when under the influence of a <see cref="StatMod" /> will result in no change. It ranges from 1 to 65000, inclusive.
		/// <seealso cref="RawStr" />
		/// <seealso cref="StatMod" />
		/// </summary>
		[CommandProperty(AccessLevel.GameMaster)]
		public virtual int Str
		{
			get
			{
				int value = this.m_Str + this.GetStatOffset(StatType.Str);

				if (value < 1)
					value = 1;
				else if (value > 65000)
					value = 65000;

				return value;
			}
			set
			{
				if (this.m_StatMods.Count == 0)
					this.RawStr = value;
			}
		}

		/// <summary>
		/// Gets or sets the base, unmodified, dexterity of the Mobile. Ranges from 1 to 65000, inclusive.
		/// <seealso cref="Dex" />
		/// <seealso cref="StatMod" />
		/// <seealso cref="OnRawDexChange" />
		/// <seealso cref="OnRawStatChange" />
		/// </summary>
		[CommandProperty(AccessLevel.GameMaster)]
		public int RawDex
		{
			get
			{
				return this.m_Dex;
			}
			set
			{
				if (value < 1)
					value = 1;
				else if (value > 65000)
					value = 65000;

				if (this.m_Dex != value)
				{
					int oldValue = this.m_Dex;

					this.m_Dex = value;
					this.Delta(MobileDelta.Stat | MobileDelta.Stam);

					if (this.Stam < this.StamMax)
					{
						if (this.m_StamTimer == null)
							this.m_StamTimer = new StamTimer(this);

						this.m_StamTimer.Start();
					}
					else if (this.Stam > this.StamMax)
					{
						this.Stam = this.StamMax;
					}

					this.OnRawDexChange(oldValue);
					this.OnRawStatChange(StatType.Dex, oldValue);
				}
			}
		}

		/// <summary>
		/// Gets or sets the effective dexterity of the Mobile. This is the sum of the <see cref="RawDex" /> plus any additional modifiers. Any attempts to set this value when under the influence of a <see cref="StatMod" /> will result in no change. It ranges from 1 to 65000, inclusive.
		/// <seealso cref="RawDex" />
		/// <seealso cref="StatMod" />
		/// </summary>
		[CommandProperty(AccessLevel.GameMaster)]
		public virtual int Dex
		{
			get
			{
				int value = this.m_Dex + this.GetStatOffset(StatType.Dex);

				if (value < 1)
					value = 1;
				else if (value > 65000)
					value = 65000;

				return value;
			}
			set
			{
				if (this.m_StatMods.Count == 0)
					this.RawDex = value;
			}
		}

		/// <summary>
		/// Gets or sets the base, unmodified, intelligence of the Mobile. Ranges from 1 to 65000, inclusive.
		/// <seealso cref="Int" />
		/// <seealso cref="StatMod" />
		/// <seealso cref="OnRawIntChange" />
		/// <seealso cref="OnRawStatChange" />
		/// </summary>
		[CommandProperty(AccessLevel.GameMaster)]
		public int RawInt
		{
			get
			{
				return this.m_Int;
			}
			set
			{
				if (value < 1)
					value = 1;
				else if (value > 65000)
					value = 65000;

				if (this.m_Int != value)
				{
					int oldValue = this.m_Int;

					this.m_Int = value;
					this.Delta(MobileDelta.Stat | MobileDelta.Mana);

					if (this.Mana < this.ManaMax)
					{
						if (this.m_ManaTimer == null)
							this.m_ManaTimer = new ManaTimer(this);

						this.m_ManaTimer.Start();
					}
					else if (this.Mana > this.ManaMax)
					{
						this.Mana = this.ManaMax;
					}

					this.OnRawIntChange(oldValue);
					this.OnRawStatChange(StatType.Int, oldValue);
				}
			}
		}

		/// <summary>
		/// Gets or sets the effective intelligence of the Mobile. This is the sum of the <see cref="RawInt" /> plus any additional modifiers. Any attempts to set this value when under the influence of a <see cref="StatMod" /> will result in no change. It ranges from 1 to 65000, inclusive.
		/// <seealso cref="RawInt" />
		/// <seealso cref="StatMod" />
		/// </summary>
		[CommandProperty(AccessLevel.GameMaster)]
		public virtual int Int
		{
			get
			{
				int value = this.m_Int + this.GetStatOffset(StatType.Int);

				if (value < 1)
					value = 1;
				else if (value > 65000)
					value = 65000;

				return value;
			}
			set
			{
				if (this.m_StatMods.Count == 0)
					this.RawInt = value;
			}
		}

		public virtual void OnHitsChange(int oldValue)
		{
		}

		public virtual void OnStamChange(int oldValue)
		{
		}

		public virtual void OnManaChange(int oldValue)
		{
		}

		/// <summary>
		/// Gets or sets the current hit point of the Mobile. This value ranges from 0 to <see cref="HitsMax" />, inclusive. When set to the value of <see cref="HitsMax" />, the <see cref="AggressorInfo.CanReportMurder">CanReportMurder</see> flag of all aggressors is reset to false, and the list of damage entries is cleared.
		/// </summary>
		[CommandProperty(AccessLevel.GameMaster)]
		public int Hits
		{
			get
			{
				return this.m_Hits;
			}
			set
			{
				if (this.m_Deleted)
					return;

				if (value < 0)
				{
					value = 0;
				}
				else if (value >= this.HitsMax)
				{
					value = this.HitsMax;

					if (this.m_HitsTimer != null)
						this.m_HitsTimer.Stop();

					for (int i = 0; i < this.m_Aggressors.Count; i++) //reset reports on full HP
						this.m_Aggressors[i].CanReportMurder = false;

					if (this.m_DamageEntries.Count > 0)
						this.m_DamageEntries.Clear(); // reset damage entries on full HP
				}

				if (value < this.HitsMax)
				{
					if (this.CanRegenHits)
					{
						if (this.m_HitsTimer == null)
							this.m_HitsTimer = new HitsTimer(this);

						this.m_HitsTimer.Start();
					}
					else if (this.m_HitsTimer != null)
					{
						this.m_HitsTimer.Stop();
					}
				}

				if (this.m_Hits != value)
				{
					int oldValue = this.m_Hits;
					this.m_Hits = value;
					this.Delta(MobileDelta.Hits);
					this.OnHitsChange(oldValue);
				}
			}
		}

		/// <summary>
		/// Overridable. Gets the maximum hit point of the Mobile. By default, this returns: <c>50 + (<see cref="Str" /> / 2)</c>
		/// </summary>
		[CommandProperty(AccessLevel.GameMaster)]
		public virtual int HitsMax
		{
			get
			{
				return 50 + (this.Str / 2);
			}
		}

		/// <summary>
		/// Gets or sets the current stamina of the Mobile. This value ranges from 0 to <see cref="StamMax" />, inclusive.
		/// </summary>
		[CommandProperty(AccessLevel.GameMaster)]
		public int Stam
		{
			get
			{
				return this.m_Stam;
			}
			set
			{
				if (this.m_Deleted)
					return;

				if (value < 0)
				{
					value = 0;
				}
				else if (value >= this.StamMax)
				{
					value = this.StamMax;

					if (this.m_StamTimer != null)
						this.m_StamTimer.Stop();
				}

				if (value < this.StamMax)
				{
					if (this.CanRegenStam)
					{
						if (this.m_StamTimer == null)
							this.m_StamTimer = new StamTimer(this);

						this.m_StamTimer.Start();
					}
					else if (this.m_StamTimer != null)
					{
						this.m_StamTimer.Stop();
					}
				}

				if (this.m_Stam != value)
				{
					int oldValue = this.m_Stam;
					this.m_Stam = value;
					this.Delta(MobileDelta.Stam);
					this.OnStamChange(oldValue);
				}
			}
		}

		/// <summary>
		/// Overridable. Gets the maximum stamina of the Mobile. By default, this returns: <c><see cref="Dex" /></c>
		/// </summary>
		[CommandProperty(AccessLevel.GameMaster)]
		public virtual int StamMax
		{
			get
			{
				return this.Dex;
			}
		}

		/// <summary>
		/// Gets or sets the current stamina of the Mobile. This value ranges from 0 to <see cref="ManaMax" />, inclusive.
		/// </summary>
		[CommandProperty(AccessLevel.GameMaster)]
		public int Mana
		{
			get
			{
				return this.m_Mana;
			}
			set
			{
				if (this.m_Deleted)
					return;

				if (value < 0)
				{
					value = 0;
				}
				else if (value >= this.ManaMax)
				{
					value = this.ManaMax;

					if (this.m_ManaTimer != null)
						this.m_ManaTimer.Stop();

					if (this.Meditating)
					{
						this.Meditating = false;
						this.SendLocalizedMessage(501846); // You are at peace.
					}
				}

				if (value < this.ManaMax)
				{
					if (this.CanRegenMana)
					{
						if (this.m_ManaTimer == null)
							this.m_ManaTimer = new ManaTimer(this);

						this.m_ManaTimer.Start();
					}
					else if (this.m_ManaTimer != null)
					{
						this.m_ManaTimer.Stop();
					}
				}

				if (this.m_Mana != value)
				{
					int oldValue = this.m_Mana;
					this.m_Mana = value;
					this.Delta(MobileDelta.Mana);
					this.OnManaChange(oldValue);
				}
			}
		}

		/// <summary>
		/// Overridable. Gets the maximum mana of the Mobile. By default, this returns: <c><see cref="Int" /></c>
		/// </summary>
		[CommandProperty(AccessLevel.GameMaster)]
		public virtual int ManaMax
		{
			get
			{
				return this.Int;
			}
		}

		#endregion
		
		public virtual int Luck
		{
			get
			{
				return 0;
			}
		}
		
		public virtual int HuedItemID
		{
			get
			{
				return (this.m_Female ? 0x2107 : 0x2106);
			}
		}

		private int m_HueMod = -1;

		[Hue, CommandProperty(AccessLevel.Decorator)]
		public int HueMod
		{
			get
			{
				return this.m_HueMod;
			}
			set
			{
				if (this.m_HueMod != value)
				{
					this.m_HueMod = value;

					this.Delta(MobileDelta.Hue);
				}
			}
		}

		[Hue, CommandProperty(AccessLevel.Decorator)]
		public virtual int Hue
		{
			get
			{
				if (this.m_HueMod != -1)
					return this.m_HueMod;

				return this.m_Hue;
			}
			set
			{
				int oldHue = this.m_Hue;

				if (oldHue != value)
				{
					this.m_Hue = value;

					this.Delta(MobileDelta.Hue);
				}
			}
		}

		public void SetDirection(Direction dir)
		{
			this.m_Direction = dir;
		}

		[CommandProperty(AccessLevel.Decorator)]
		public Direction Direction
		{
			get
			{
				return this.m_Direction;
			}
			set
			{
				if (this.m_Direction != value)
				{
					this.m_Direction = value;

					this.Delta(MobileDelta.Direction);
					//ProcessDelta();
				}
			}
		}

		public virtual int GetSeason()
		{
			if (this.m_Map != null)
				return this.m_Map.Season;

			return 1;
		}

		public virtual int GetPacketFlags()
		{
			int flags = 0x0;

			if (this.m_Paralyzed || this.m_Frozen)
				flags |= 0x01;

			if (this.m_Female)
				flags |= 0x02;

			if (this.m_Flying)
				flags |= 0x04;

			if (this.m_Blessed || this.m_YellowHealthbar)
				flags |= 0x08;

			if (this.m_Warmode)
				flags |= 0x40;

			if (this.m_Hidden)
				flags |= 0x80;

			return flags;
		}

		// Pre-7.0.0.0 Packet Flags
		public virtual int GetOldPacketFlags()
		{
			int flags = 0x0;

			if (this.m_Paralyzed || this.m_Frozen)
				flags |= 0x01;

			if (this.m_Female)
				flags |= 0x02;

			if (this.m_Poison != null)
				flags |= 0x04;

			if (this.m_Blessed || this.m_YellowHealthbar)
				flags |= 0x08;

			if (this.m_Warmode)
				flags |= 0x40;

			if (this.m_Hidden)
				flags |= 0x80;

			return flags;
		}

		[CommandProperty(AccessLevel.Decorator)]
		public bool Female
		{
			get
			{
				return this.m_Female;
			}
			set
			{
				if (this.m_Female != value)
				{
					this.m_Female = value;
					this.Delta(MobileDelta.Flags);
					this.OnGenderChanged(!this.m_Female);
				}
			}
		}

		public virtual void OnGenderChanged(bool oldFemale)
		{
		}

		[CommandProperty(AccessLevel.Decorator)]
		public bool Flying
		{
			get
			{
				return this.m_Flying;
			}
			set
			{
				if (this.m_Flying != value)
				{
					this.m_Flying = value;
					this.Delta(MobileDelta.Flags);
				}
			}
		}

		#region Stygian Abyss
		public virtual void ToggleFlying()
		{
		}

		#endregion

		[CommandProperty(AccessLevel.Decorator)]
		public bool Warmode
		{
			get
			{
				return this.m_Warmode;
			}
			set
			{
				if (this.m_Deleted)
					return;

				if (this.m_Warmode != value)
				{
					if (this.m_AutoManifestTimer != null)
					{
						this.m_AutoManifestTimer.Stop();
						this.m_AutoManifestTimer = null;
					}

					this.m_Warmode = value;
					this.Delta(MobileDelta.Flags);

					if (this.m_NetState != null)
						this.Send(SetWarMode.Instantiate(value));

					if (!this.m_Warmode)
						this.Combatant = null;

					if (!this.Alive)
					{
						if (value)
							this.Delta(MobileDelta.GhostUpdate);
						else
							this.SendRemovePacket(false);
					}

					this.OnWarmodeChanged();
				}
			}
		}

		/// <summary>
		/// Overridable. Virtual event invoked after the Warmode property has changed.
		/// </summary>
		public virtual void OnWarmodeChanged()
		{
		}

		[CommandProperty(AccessLevel.GameMaster)]
		public virtual bool Hidden
		{
			get
			{
				return this.m_Hidden;
			}
			set
			{
				if (this.m_Hidden != value)
				{
					this.m_AllowedStealthSteps = 0;

					this.m_Hidden = value;
					//Delta( MobileDelta.Flags );

					if (this.m_Map != null)
					{
						Packet p = null;

						IPooledEnumerable eable = this.m_Map.GetClientsInRange(this.m_Location);

						foreach (NetState state in eable)
						{
							if (!state.Mobile.CanSee(this))
							{
								if (p == null)
									p = this.RemovePacket;

								state.Send(p);
							}
							else
							{
								if (state.StygianAbyss)
									state.Send(new MobileIncoming(state.Mobile, this));
								else
									state.Send(new MobileIncomingOld(state.Mobile, this));

								if (this.IsDeadBondedPet)
									state.Send(new BondedStatus(0, this.m_Serial, 1));

								if (ObjectPropertyList.Enabled)
								{
									state.Send(this.OPLPacket);
									//foreach ( Item item in m_Items )
									//	state.Send( item.OPLPacket );
								}
							}
						}

						eable.Free();
					}
				}
			}
		}

		public virtual void OnConnected()
		{
		}

		public virtual void OnDisconnected()
		{
		}

		public virtual void OnNetStateChanged()
		{
		}

		[CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
		public NetState NetState
		{
			get
			{
				if (this.m_NetState != null && this.m_NetState.Socket == null)
					this.NetState = null;

				return this.m_NetState;
			}
			set
			{
				if (this.m_NetState != value)
				{
					if (this.m_Map != null)
						this.m_Map.OnClientChange(this.m_NetState, value, this);

					if (this.m_Target != null)
						this.m_Target.Cancel(this, TargetCancelType.Disconnected);

					if (this.m_QuestArrow != null)
						this.QuestArrow = null;

					if (this.m_Spell != null)
						this.m_Spell.OnConnectionChanged();

					//if ( m_Spell != null )
					//	m_Spell.FinishSequence();

					if (this.m_NetState != null)
						this.m_NetState.CancelAllTrades();

					BankBox box = this.FindBankNoCreate();

					if (box != null && box.Opened)
						box.Close();

					// REMOVED:
					//m_Actions.Clear();

					this.m_NetState = value;

					if (this.m_NetState == null)
					{
						this.OnDisconnected();
						EventSink.InvokeDisconnected(new DisconnectedEventArgs(this));

						// Disconnected, start the logout timer

						if (this.m_LogoutTimer == null)
							this.m_LogoutTimer = new LogoutTimer(this);
						else
							this.m_LogoutTimer.Stop();

						this.m_LogoutTimer.Delay = this.GetLogoutDelay();
						this.m_LogoutTimer.Start();
					}
					else
					{
						this.OnConnected();
						EventSink.InvokeConnected(new ConnectedEventArgs(this));

						// Connected, stop the logout timer and if needed, move to the world

						if (this.m_LogoutTimer != null)
							this.m_LogoutTimer.Stop();

						this.m_LogoutTimer = null;

						if (this.m_Map == Map.Internal && this.m_LogoutMap != null)
						{
							this.Map = this.m_LogoutMap;
							this.Location = this.m_LogoutLocation;
						}
					}

					for (int i = this.m_Items.Count - 1; i >= 0; --i)
					{
						if (i >= this.m_Items.Count)
							continue;

						Item item = this.m_Items[i];

						if (item is SecureTradeContainer)
						{
							for (int j = item.Items.Count - 1; j >= 0; --j)
							{
								if (j < item.Items.Count)
								{
									item.Items[j].OnSecureTrade(this, this, this, false);
									this.AddToBackpack(item.Items[j]);
								}
							}

							Timer.DelayCall(TimeSpan.Zero, delegate { item.Delete(); });
						}
					}

					this.DropHolding();
					this.OnNetStateChanged();
				}
			}
		}

		public virtual bool CanSee(object o)
		{
			if (o is Item)
			{
				return this.CanSee((Item)o);
			}
			else if (o is Mobile)
			{
				return this.CanSee((Mobile)o);
			}
			else
			{
				return true;
			}
		}

		public virtual bool CanSee(Item item)
		{
			if (this.m_Map == Map.Internal)
				return false;
			else if (item.Map == Map.Internal)
				return false;

			if (item.Parent != null)
			{
				if (item.Parent is Item)
				{
					Item parent = item.Parent as Item;

					if (!(this.CanSee(parent) && parent.IsChildVisibleTo(this, item)))
						return false;
				}
				else if (item.Parent is Mobile)
				{
					if (!this.CanSee((Mobile)item.Parent))
						return false;
				}
			}

			if (item is BankBox)
			{
				BankBox box = item as BankBox;

				if (box != null && this.IsPlayer() && (box.Owner != this || !box.Opened))
					return false;
			}
			else if (item is SecureTradeContainer)
			{
				SecureTrade trade = ((SecureTradeContainer)item).Trade;

				if (trade != null && trade.From.Mobile != this && trade.To.Mobile != this)
					return false;
			}

			return !item.Deleted && item.Map == this.m_Map && (item.Visible || this.m_AccessLevel > AccessLevel.Counselor);
		}

		public virtual bool CanSee(Mobile m)
		{
			if (this.m_Deleted || m.m_Deleted || this.m_Map == Map.Internal || m.m_Map == Map.Internal)
				return false;

			return this == m || (
								 m.m_Map == this.m_Map &&
								 (!m.Hidden || (this.IsStaff() && this.m_AccessLevel >= m.AccessLevel)) &&
								 ((m.Alive || (Core.SE && this.Skills.SpiritSpeak.Value >= 100.0)) || !this.Alive || this.IsStaff() || m.Warmode));
		}

		public virtual bool CanBeRenamedBy(Mobile from)
		{
			return (from.AccessLevel >= AccessLevel.Decorator && from.m_AccessLevel > this.m_AccessLevel);
		}

		[CommandProperty(AccessLevel.GameMaster)]
		public string Language
		{
			get
			{
				return this.m_Language;
			}
			set
			{
				if (this.m_Language != value)
					this.m_Language = value;
			}
		}

		[CommandProperty(AccessLevel.Decorator)]
		public int SpeechHue
		{
			get
			{
				return this.m_SpeechHue;
			}
			set
			{
				this.m_SpeechHue = value;
			}
		}

		[CommandProperty(AccessLevel.Decorator)]
		public int EmoteHue
		{
			get
			{
				return this.m_EmoteHue;
			}
			set
			{
				this.m_EmoteHue = value;
			}
		}

		[CommandProperty(AccessLevel.Decorator)]
		public int WhisperHue
		{
			get
			{
				return this.m_WhisperHue;
			}
			set
			{
				this.m_WhisperHue = value;
			}
		}

		[CommandProperty(AccessLevel.Decorator)]
		public int YellHue
		{
			get
			{
				return this.m_YellHue;
			}
			set
			{
				this.m_YellHue = value;
			}
		}

		[CommandProperty(AccessLevel.Decorator)]
		public string GuildTitle
		{
			get
			{
				return this.m_GuildTitle;
			}
			set
			{
				string old = this.m_GuildTitle;

				if (old != value)
				{
					this.m_GuildTitle = value;

					if (this.m_Guild != null && !this.m_Guild.Disbanded && this.m_GuildTitle != null)
						this.SendLocalizedMessage(1018026, true, this.m_GuildTitle); // Your guild title has changed :

					this.InvalidateProperties();

					this.OnGuildTitleChange(old);
				}
			}
		}

		public virtual void OnGuildTitleChange(string oldTitle)
		{
		}

		[CommandProperty(AccessLevel.Decorator)]
		public bool DisplayGuildTitle
		{
			get
			{
				return this.m_DisplayGuildTitle;
			}
			set
			{
				this.m_DisplayGuildTitle = value;
				this.InvalidateProperties();
			}
		}

		[CommandProperty(AccessLevel.Decorator)]
		public Mobile GuildFealty
		{
			get
			{
				return this.m_GuildFealty;
			}
			set
			{
				this.m_GuildFealty = value;
			}
		}

		private string m_NameMod;

		[CommandProperty(AccessLevel.Decorator)]
		public string NameMod
		{
			get
			{
				return this.m_NameMod;
			}
			set
			{
				if (this.m_NameMod != value)
				{
					this.m_NameMod = value;
					this.Delta(MobileDelta.Name);
					this.InvalidateProperties();
				}
			}
		}

		private bool m_YellowHealthbar;

		[CommandProperty(AccessLevel.Decorator)]
		public bool YellowHealthbar
		{
			get
			{
				return this.m_YellowHealthbar;
			}
			set
			{
				this.m_YellowHealthbar = value;
				this.Delta(MobileDelta.HealthbarYellow);
			}
		}

		[CommandProperty(AccessLevel.Decorator)]
		public string RawName
		{
			get
			{
				return this.m_Name;
			}
			set
			{
				this.Name = value;
			}
		}

		[CommandProperty(AccessLevel.Decorator)]
		public string Name
		{
			get
			{
				if (this.m_NameMod != null)
					return this.m_NameMod;

				return this.m_Name;
			}
			set
			{
				if (this.m_Name != value) // I'm leaving out the && m_NameMod == null
				{
					this.m_Name = value;
					this.Delta(MobileDelta.Name);
					this.InvalidateProperties();
				}
			}
		}

		[CommandProperty(AccessLevel.GameMaster)]
		public DateTime LastStrGain
		{
			get
			{
				return this.m_LastStrGain;
			}
			set
			{
				this.m_LastStrGain = value;
			}
		}

		[CommandProperty(AccessLevel.GameMaster)]
		public DateTime LastIntGain
		{
			get
			{
				return this.m_LastIntGain;
			}
			set
			{
				this.m_LastIntGain = value;
			}
		}

		[CommandProperty(AccessLevel.GameMaster)]
		public DateTime LastDexGain
		{
			get
			{
				return this.m_LastDexGain;
			}
			set
			{
				this.m_LastDexGain = value;
			}
		}

		public DateTime LastStatGain
		{
			get
			{
				DateTime d = this.m_LastStrGain;

				if (this.m_LastIntGain > d)
					d = this.m_LastIntGain;

				if (this.m_LastDexGain > d)
					d = this.m_LastDexGain;

				return d;
			}
			set
			{
				this.m_LastStrGain = value;
				this.m_LastIntGain = value;
				this.m_LastDexGain = value;
			}
		}

		public BaseGuild Guild
		{
			get
			{
				return this.m_Guild;
			}
			set
			{
				BaseGuild old = this.m_Guild;

				if (old != value)
				{
					if (value == null)
						this.GuildTitle = null;

					this.m_Guild = value;

					this.Delta(MobileDelta.Noto);
					this.InvalidateProperties();

					this.OnGuildChange(old);
				}
			}
		}

		public virtual void OnGuildChange(BaseGuild oldGuild)
		{
		}

		#region Poison/Curing

		public Timer PoisonTimer
		{
			get
			{
				return this.m_PoisonTimer;
			}
		}

		[CommandProperty(AccessLevel.GameMaster)]
		public Poison Poison
		{
			get
			{
				return this.m_Poison;
			}
			set
			{
				/*if ( m_Poison != value && (m_Poison == null || value == null || m_Poison.Level < value.Level) )
				{*/
				this.m_Poison = value;
				this.Delta(MobileDelta.HealthbarPoison);

				if (this.m_PoisonTimer != null)
				{
					this.m_PoisonTimer.Stop();
					this.m_PoisonTimer = null;
				}

				if (this.m_Poison != null)
				{
					this.m_PoisonTimer = this.m_Poison.ConstructTimer(this);

					if (this.m_PoisonTimer != null)
						this.m_PoisonTimer.Start();
				}

				this.CheckStatTimers();
				/*}*/
			}
		}

		/// <summary>
		/// Overridable. Event invoked when a call to <see cref="ApplyPoison" /> failed because <see cref="CheckPoisonImmunity" /> returned false: the Mobile was resistant to the poison. By default, this broadcasts an overhead message: * The poison seems to have no effect. *
		/// <seealso cref="CheckPoisonImmunity" />
		/// <seealso cref="ApplyPoison" />
		/// <seealso cref="Poison" />
		/// </summary>
		public virtual void OnPoisonImmunity(Mobile from, Poison poison)
		{
			this.PublicOverheadMessage(MessageType.Emote, 0x3B2, 1005534); // * The poison seems to have no effect. *
		}

		/// <summary>
		/// Overridable. Virtual event invoked when a call to <see cref="ApplyPoison" /> failed because <see cref="CheckHigherPoison" /> returned false: the Mobile was already poisoned by an equal or greater strength poison.
		/// <seealso cref="CheckHigherPoison" />
		/// <seealso cref="ApplyPoison" />
		/// <seealso cref="Poison" />
		/// </summary>
		public virtual void OnHigherPoison(Mobile from, Poison poison)
		{
		}

		/// <summary>
		/// Overridable. Event invoked when a call to <see cref="ApplyPoison" /> succeeded. By default, this broadcasts an overhead message varying by the level of the poison. Example: * Zippy begins to spasm uncontrollably. *
		/// <seealso cref="ApplyPoison" />
		/// <seealso cref="Poison" />
		/// </summary>
		public virtual void OnPoisoned(Mobile from, Poison poison, Poison oldPoison)
		{
			if (poison != null)
			{
				#region Mondain's Legacy
				this.LocalOverheadMessage(MessageType.Regular, 0x21, 1042857 + (poison.RealLevel * 2));
				this.NonlocalOverheadMessage(MessageType.Regular, 0x21, 1042858 + (poison.RealLevel * 2), this.Name);
				#endregion
			}
		}

		/// <summary>
		/// Overridable. Called from <see cref="ApplyPoison" />, this method checks if the Mobile is immune to some <see cref="Poison" />. If true, <see cref="OnPoisonImmunity" /> will be invoked and <see cref="ApplyPoisonResult.Immune" /> is returned.
		/// <seealso cref="OnPoisonImmunity" />
		/// <seealso cref="ApplyPoison" />
		/// <seealso cref="Poison" />
		/// </summary>
		public virtual bool CheckPoisonImmunity(Mobile from, Poison poison)
		{
			return false;
		}

		/// <summary>
		/// Overridable. Called from <see cref="ApplyPoison" />, this method checks if the Mobile is already poisoned by some <see cref="Poison" /> of equal or greater strength. If true, <see cref="OnHigherPoison" /> will be invoked and <see cref="ApplyPoisonResult.HigherPoisonActive" /> is returned.
		/// <seealso cref="OnHigherPoison" />
		/// <seealso cref="ApplyPoison" />
		/// <seealso cref="Poison" />
		/// </summary>
		public virtual bool CheckHigherPoison(Mobile from, Poison poison)
		{
			#region Mondain's Legacy
			return (this.m_Poison != null && this.m_Poison.RealLevel >= poison.RealLevel);
			#endregion
		}

		/// <summary>
		/// Overridable. Attempts to apply poison to the Mobile. Checks are made such that no <see cref="CheckHigherPoison">higher poison is active</see> and that the Mobile is not <see cref="CheckPoisonImmunity">immune to the poison</see>. Provided those assertions are true, the <paramref name="poison" /> is applied and <see cref="OnPoisoned" /> is invoked.
		/// <seealso cref="Poison" />
		/// <seealso cref="CurePoison" />
		/// </summary>
		/// <returns>One of four possible values:
		/// <list type="table">
		/// <item>
		/// <term><see cref="ApplyPoisonResult.Cured">Cured</see></term>
		/// <description>The <paramref name="poison" /> parameter was null and so <see cref="CurePoison" /> was invoked.</description>
		/// </item>
		/// <item>
		/// <term><see cref="ApplyPoisonResult.HigherPoisonActive">HigherPoisonActive</see></term>
		/// <description>The call to <see cref="CheckHigherPoison" /> returned false.</description>
		/// </item>
		/// <item>
		/// <term><see cref="ApplyPoisonResult.Immune">Immune</see></term>
		/// <description>The call to <see cref="CheckPoisonImmunity" /> returned false.</description>
		/// </item>
		/// <item>
		/// <term><see cref="ApplyPoisonResult.Poisoned">Poisoned</see></term>
		/// <description>The <paramref name="poison" /> was successfully applied.</description>
		/// </item>
		/// </list>
		/// </returns>
		public virtual ApplyPoisonResult ApplyPoison(Mobile from, Poison poison)
		{
			if (poison == null)
			{
				this.CurePoison(from);
				return ApplyPoisonResult.Cured;
			}

			if (this.CheckHigherPoison(from, poison))
			{
				this.OnHigherPoison(from, poison);
				return ApplyPoisonResult.HigherPoisonActive;
			}

			if (this.CheckPoisonImmunity(from, poison))
			{
				this.OnPoisonImmunity(from, poison);
				return ApplyPoisonResult.Immune;
			}

			Poison oldPoison = this.m_Poison;
			this.Poison = poison;

			this.OnPoisoned(from, poison, oldPoison);

			return ApplyPoisonResult.Poisoned;
		}

		/// <summary>
		/// Overridable. Called from <see cref="CurePoison" />, this method checks to see that the Mobile can be cured of <see cref="Poison" />
		/// <seealso cref="CurePoison" />
		/// <seealso cref="Poison" />
		/// </summary>
		public virtual bool CheckCure(Mobile from)
		{
			return true;
		}

		/// <summary>
		/// Overridable. Virtual event invoked when a call to <see cref="CurePoison" /> succeeded.
		/// <seealso cref="CurePoison" />
		/// <seealso cref="CheckCure" />
		/// <seealso cref="Poison" />
		/// </summary>
		public virtual void OnCured(Mobile from, Poison oldPoison)
		{
		}

		/// <summary>
		/// Overridable. Virtual event invoked when a call to <see cref="CurePoison" /> failed.
		/// <seealso cref="CurePoison" />
		/// <seealso cref="CheckCure" />
		/// <seealso cref="Poison" />
		/// </summary>
		public virtual void OnFailedCure(Mobile from)
		{
		}

		/// <summary>
		/// Overridable. Attempts to cure any poison that is currently active.
		/// </summary>
		/// <returns>True if poison was cured, false if otherwise.</returns>
		public virtual bool CurePoison(Mobile from)
		{
			if (this.CheckCure(from))
			{
				Poison oldPoison = this.m_Poison;
				this.Poison = null;

				this.OnCured(from, oldPoison);

				return true;
			}

			this.OnFailedCure(from);

			return false;
		}

		#endregion

		private ISpawner m_Spawner;

		public ISpawner Spawner
		{
			get
			{
				return this.m_Spawner;
			}
			set
			{
				this.m_Spawner = value;
			}
		}

		private Region m_WalkRegion;

		public Region WalkRegion
		{
			get
			{
				return this.m_WalkRegion;
			}
			set
			{
				this.m_WalkRegion = value;
			}
		}

		public virtual void OnBeforeSpawn(Point3D location, Map m)
		{
		}

		public virtual void OnAfterSpawn()
		{
		}

		[CommandProperty(AccessLevel.GameMaster)]
		public bool Poisoned
		{
			get
			{
				return (this.m_Poison != null);
			}
		}

		[CommandProperty(AccessLevel.GameMaster)]
		public bool IsBodyMod
		{
			get
			{
				return (this.m_BodyMod.BodyID != 0);
			}
		}

		[CommandProperty(AccessLevel.Decorator)]
		public Body BodyMod
		{
			get
			{
				return this.m_BodyMod;
			}
			set
			{
				if (this.m_BodyMod != value)
				{
					this.m_BodyMod = value;

					this.Delta(MobileDelta.Body);
					this.InvalidateProperties();

					this.CheckStatTimers();
				}
			}
		}

		private static readonly int[] m_InvalidBodies = new int[]
		{
			//32,		// Dunno why is blocked
			//95,		// Used for Turkey
			//156,		// Dunno why is blocked
			//197,		// ML Dragon
			//198,		// ML Dragon
		};

		[Body, CommandProperty(AccessLevel.Decorator)]
		public Body Body
		{
			get
			{
				if (this.IsBodyMod)
					return this.m_BodyMod;

				return this.m_Body;
			}
			set
			{
				if (this.m_Body != value && !this.IsBodyMod)
				{
					this.m_Body = this.SafeBody(value);

					this.Delta(MobileDelta.Body);
					this.InvalidateProperties();

					this.CheckStatTimers();
				}
			}
		}

		public virtual int SafeBody(int body)
		{
			int delta = -1;

			for (int i = 0; delta < 0 && i < m_InvalidBodies.Length; ++i)
				delta = (m_InvalidBodies[i] - body);

			if (delta != 0)
				return body;

			return 0;
		}

		[Body, CommandProperty(AccessLevel.Decorator)]
		public int BodyValue
		{
			get
			{
				return this.Body.BodyID;
			}
			set
			{
				this.Body = value;
			}
		}

		[CommandProperty(AccessLevel.Counselor)]
		public Serial Serial
		{
			get
			{
				return this.m_Serial;
			}
		}

		[CommandProperty(AccessLevel.Counselor, AccessLevel.Decorator)]
		public Point3D Location
		{
			get
			{
				return this.m_Location;
			}
			set
			{
				this.SetLocation(value, true);
			}
		}

		[CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
		public Point3D LogoutLocation
		{
			get
			{
				return this.m_LogoutLocation;
			}
			set
			{
				this.m_LogoutLocation = value;
			}
		}

		[CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
		public Map LogoutMap
		{
			get
			{
				return this.m_LogoutMap;
			}
			set
			{
				this.m_LogoutMap = value;
			}
		}

		public Region Region
		{
			get
			{
				if (this.m_Region == null) if (this.Map == null)
					return Map.Internal.DefaultRegion;
				else
					return this.Map.DefaultRegion;
				else
					return this.m_Region;
			}
		}

		public void FreeCache()
		{
			Packet.Release(ref this.m_RemovePacket);
			Packet.Release(ref this.m_PropertyList);
			Packet.Release(ref this.m_OPLPacket);
		}

		private Packet m_RemovePacket;

		public Packet RemovePacket
		{
			get
			{
				if (this.m_RemovePacket == null)
				{
					this.m_RemovePacket = new RemoveMobile(this);
					this.m_RemovePacket.SetStatic();
				}

				return this.m_RemovePacket;
			}
		}

		private Packet m_OPLPacket;

		public Packet OPLPacket
		{
			get
			{
				if (this.m_OPLPacket == null)
				{
					this.m_OPLPacket = new OPLInfo(this.PropertyList);
					this.m_OPLPacket.SetStatic();
				}

				return this.m_OPLPacket;
			}
		}

		private ObjectPropertyList m_PropertyList;

		public ObjectPropertyList PropertyList
		{
			get
			{
				if (this.m_PropertyList == null)
				{
					this.m_PropertyList = new ObjectPropertyList(this);

					this.GetProperties(this.m_PropertyList);

					this.m_PropertyList.Terminate();
					this.m_PropertyList.SetStatic();
				}

				return this.m_PropertyList;
			}
		}

		public void ClearProperties()
		{
			Packet.Release(ref this.m_PropertyList);
			Packet.Release(ref this.m_OPLPacket);
		}

		public void InvalidateProperties()
		{
			if (!ObjectPropertyList.Enabled)
				return;

			if (this.m_Map != null && this.m_Map != Map.Internal && !World.Loading)
			{
				ObjectPropertyList oldList = this.m_PropertyList;
				Packet.Release(ref this.m_PropertyList);
				ObjectPropertyList newList = this.PropertyList;

				if (oldList == null || oldList.Hash != newList.Hash)
				{
					Packet.Release(ref this.m_OPLPacket);
					this.Delta(MobileDelta.Properties);
				}
			}
			else
			{
				this.ClearProperties();
			}
		}

		private int m_SolidHueOverride = -1;

		[CommandProperty(AccessLevel.Decorator)]
		public int SolidHueOverride
		{
			get
			{
				return this.m_SolidHueOverride;
			}
			set
			{
				if (this.m_SolidHueOverride == value)
					return;
				this.m_SolidHueOverride = value;
				this.Delta(MobileDelta.Hue | MobileDelta.Body);
			}
		}

		public virtual void MoveToWorld(Point3D newLocation, Map map)
		{
			if (this.m_Deleted)
				return;

			if (this.m_Map == map)
			{
				this.SetLocation(newLocation, true);
				return;
			}

			BankBox box = this.FindBankNoCreate();

			if (box != null && box.Opened)
				box.Close();

			Point3D oldLocation = this.m_Location;
			Map oldMap = this.m_Map;

			Region oldRegion = this.m_Region;

			if (oldMap != null)
			{
				oldMap.OnLeave(this);

				this.ClearScreen();
				this.SendRemovePacket();
			}

			for (int i = 0; i < this.m_Items.Count; ++i)
				this.m_Items[i].Map = map;

			this.m_Map = map;

			this.m_Location = newLocation;

			NetState ns = this.m_NetState;

			if (this.m_Map != null)
			{
				this.m_Map.OnEnter(this);

				this.UpdateRegion();

				if (ns != null && this.m_Map != null)
				{
					ns.Sequence = 0;
					ns.Send(new MapChange(this));
					ns.Send(new MapPatches());
					ns.Send(SeasonChange.Instantiate(this.GetSeason(), true));

					if (ns.StygianAbyss)
						ns.Send(new MobileUpdate(this));
					else
						ns.Send(new MobileUpdateOld(this));

					this.ClearFastwalkStack();
				}
			}
			else
			{
				this.UpdateRegion();
			}

			if (ns != null)
			{
				if (this.m_Map != null)
					this.Send(new ServerChange(this, this.m_Map));

				ns.Sequence = 0;
				this.ClearFastwalkStack();

				if (ns.StygianAbyss)
				{
					this.Send(new MobileIncoming(this, this));
					this.Send(new MobileUpdate(this));
					this.CheckLightLevels(true);
					this.Send(new MobileUpdate(this));
				}
				else
				{
					this.Send(new MobileIncomingOld(this, this));
					this.Send(new MobileUpdateOld(this));
					this.CheckLightLevels(true);
					this.Send(new MobileUpdateOld(this));
				}
			}

			this.SendEverything();
			this.SendIncomingPacket();

			if (ns != null)
			{
				ns.Sequence = 0;
				this.ClearFastwalkStack();

				if (ns.StygianAbyss)
				{
					this.Send(new MobileIncoming(this, this));
					this.Send(SupportedFeatures.Instantiate(ns));
					this.Send(new MobileUpdate(this));
					this.Send(new MobileAttributes(this));
				}
				else
				{
					this.Send(new MobileIncomingOld(this, this));
					this.Send(SupportedFeatures.Instantiate(ns));
					this.Send(new MobileUpdateOld(this));
					this.Send(new MobileAttributes(this));
				}
			}

			this.OnMapChange(oldMap);
			this.OnLocationChange(oldLocation);

			if (this.m_Region != null)
				this.m_Region.OnLocationChanged(this, oldLocation);
		}

		public virtual void SetLocation(Point3D newLocation, bool isTeleport)
		{
			if (this.m_Deleted)
				return;

			Point3D oldLocation = this.m_Location;

			if (oldLocation != newLocation)
			{
				this.m_Location = newLocation;
				this.UpdateRegion();

				BankBox box = this.FindBankNoCreate();

				if (box != null && box.Opened)
					box.Close();

				if (this.m_NetState != null)
					this.m_NetState.ValidateAllTrades();

				if (this.m_Map != null)
					this.m_Map.OnMove(oldLocation, this);

				if (isTeleport && this.m_NetState != null)
				{
					this.m_NetState.Sequence = 0;

					if (this.m_NetState.StygianAbyss)
						this.m_NetState.Send(new MobileUpdate(this));
					else
						this.m_NetState.Send(new MobileUpdateOld(this));

					this.ClearFastwalkStack();
				}

				Map map = this.m_Map;

				if (map != null)
				{
					// First, send a remove message to everyone who can no longer see us. (inOldRange && !inNewRange)
					Packet removeThis = null;

					IPooledEnumerable eable = map.GetClientsInRange(oldLocation);

					foreach (NetState ns in eable)
					{
						if (ns != this.m_NetState && !Utility.InUpdateRange(newLocation, ns.Mobile.Location))
						{
							if (removeThis == null)
								removeThis = this.RemovePacket;

							ns.Send(removeThis);
						}
					}

					eable.Free();

					NetState ourState = this.m_NetState;

					// Check to see if we are attached to a client
					if (ourState != null)
					{
						eable = map.GetObjectsInRange(newLocation, Core.GlobalMaxUpdateRange);

						// We are attached to a client, so it's a bit more complex. We need to send new items and people to ourself, and ourself to other clients
						foreach (object o in eable)
						{
							if (o is Item)
							{
								Item item = (Item)o;

								int range = item.GetUpdateRange(this);
								Point3D loc = item.Location;

								if (!Utility.InRange(oldLocation, loc, range) && Utility.InRange(newLocation, loc, range) && this.CanSee(item))
									item.SendInfoTo(ourState);
							}
							else if (o != this && o is Mobile)
							{
								Mobile m = (Mobile)o;

								if (!Utility.InUpdateRange(newLocation, m.m_Location))
									continue;

								bool inOldRange = Utility.InUpdateRange(oldLocation, m.m_Location);

								if ((isTeleport || !inOldRange) && m.m_NetState != null && m.CanSee(this))
								{
									if (m.m_NetState.StygianAbyss)
									{
										m.m_NetState.Send(new MobileIncoming(m, this));

										if (this.m_Poison != null)
											m.m_NetState.Send(new HealthbarPoison(this));

										if (this.m_Blessed || this.m_YellowHealthbar)
											m.m_NetState.Send(new HealthbarYellow(this));
									}
									else
									{
										m.m_NetState.Send(new MobileIncomingOld(m, this));
									}

									if (this.IsDeadBondedPet)
										m.m_NetState.Send(new BondedStatus(0, this.m_Serial, 1));

									if (ObjectPropertyList.Enabled)
									{
										m.m_NetState.Send(this.OPLPacket);
										//foreach ( Item item in m_Items )
										//	m.m_NetState.Send( item.OPLPacket );
									}
								}

								if (!inOldRange && this.CanSee(m))
								{
									if (ourState.StygianAbyss)
									{
										ourState.Send(new MobileIncoming(this, m));

										if (m.Poisoned)
											ourState.Send(new HealthbarPoison(m));

										if (m.Blessed || m.YellowHealthbar)
											ourState.Send(new HealthbarYellow(m));
									}
									else
									{
										ourState.Send(new MobileIncomingOld(this, m));
									}

									if (m.IsDeadBondedPet)
										ourState.Send(new BondedStatus(0, m.m_Serial, 1));

									if (ObjectPropertyList.Enabled)
									{
										ourState.Send(m.OPLPacket);
										//foreach ( Item item in m.m_Items )
										//	ourState.Send( item.OPLPacket );
									}
								}
							}
						}

						eable.Free();
					}
					else
					{
						eable = map.GetClientsInRange(newLocation);

						// We're not attached to a client, so simply send an Incoming
						foreach (NetState ns in eable)
						{
							if ((isTeleport || !Utility.InUpdateRange(oldLocation, ns.Mobile.Location)) && ns.Mobile.CanSee(this))
							{
								if (ns.StygianAbyss)
								{
									ns.Send(new MobileIncoming(ns.Mobile, this));

									if (this.m_Poison != null)
										ns.Send(new HealthbarPoison(this));

									if (this.m_Blessed || this.m_YellowHealthbar)
										ns.Send(new HealthbarYellow(this));
								}
								else
								{
									ns.Send(new MobileIncomingOld(ns.Mobile, this));
								}

								if (this.IsDeadBondedPet)
									ns.Send(new BondedStatus(0, this.m_Serial, 1));

								if (ObjectPropertyList.Enabled)
								{
									ns.Send(this.OPLPacket);
									//foreach ( Item item in m_Items )
									//	ns.Send( item.OPLPacket );
								}
							}
						}

						eable.Free();
					}
				}

				this.OnLocationChange(oldLocation);

				this.Region.OnLocationChanged(this, oldLocation);
			}
		}

		/// <summary>
		/// Overridable. Virtual event invoked when <see cref="Location" /> changes.
		/// </summary>
		protected virtual void OnLocationChange(Point3D oldLocation)
		{
		}

		#region Hair

		private HairInfo m_Hair;
		private FacialHairInfo m_FacialHair;

		[CommandProperty(AccessLevel.Decorator)]
		public int HairItemID
		{
			get
			{
				if (this.m_Hair == null)
					return 0;

				return this.m_Hair.ItemID;
			}
			set
			{
				if (this.m_Hair == null && value > 0)
					this.m_Hair = new HairInfo(value);
				else if (value <= 0)
					this.m_Hair = null;
				else
					this.m_Hair.ItemID = value;

				this.Delta(MobileDelta.Hair);
			}
		}

		//		[CommandProperty( AccessLevel.GameMaster )]
		//		public int HairSerial { get { return HairInfo.FakeSerial( this ); } }

		[CommandProperty(AccessLevel.Decorator)]
		public int FacialHairItemID
		{
			get
			{
				if (this.m_FacialHair == null)
					return 0;

				return this.m_FacialHair.ItemID;
			}
			set
			{
				if (this.m_FacialHair == null && value > 0)
					this.m_FacialHair = new FacialHairInfo(value);
				else if (value <= 0)
					this.m_FacialHair = null;
				else
					this.m_FacialHair.ItemID = value;

				this.Delta(MobileDelta.FacialHair);
			}
		}

		//		[CommandProperty( AccessLevel.GameMaster )]
		//		public int FacialHairSerial { get { return FacialHairInfo.FakeSerial( this ); } }

		[CommandProperty(AccessLevel.Decorator)]
		public int HairHue
		{
			get
			{
				if (this.m_Hair == null)
					return 0;
				return this.m_Hair.Hue;
			}
			set
			{
				if (this.m_Hair != null)
				{
					this.m_Hair.Hue = value;
					this.Delta(MobileDelta.Hair);
				}
			}
		}

		[CommandProperty(AccessLevel.Decorator)]
		public int FacialHairHue
		{
			get
			{
				if (this.m_FacialHair == null)
					return 0;

				return this.m_FacialHair.Hue;
			}
			set
			{
				if (this.m_FacialHair != null)
				{
					this.m_FacialHair.Hue = value;
					this.Delta(MobileDelta.FacialHair);
				}
			}
		}

		#endregion

		public bool HasFreeHand()
		{
			return this.FindItemOnLayer(Layer.TwoHanded) == null;
		}

		private IWeapon m_Weapon;

		[CommandProperty(AccessLevel.GameMaster)]
		public virtual IWeapon Weapon
		{
			get
			{
				Item item = this.m_Weapon as Item;

				if (item != null && !item.Deleted && item.Parent == this && this.CanSee(item))
					return this.m_Weapon;

				this.m_Weapon = null;

				item = this.FindItemOnLayer(Layer.OneHanded);

				if (item == null)
					item = this.FindItemOnLayer(Layer.TwoHanded);

				if (item is IWeapon)
					return (this.m_Weapon = (IWeapon)item);
				else
					return this.GetDefaultWeapon();
			}
		}

		public virtual IWeapon GetDefaultWeapon()
		{
			return m_DefaultWeapon;
		}

		private BankBox m_BankBox;

		[CommandProperty(AccessLevel.GameMaster)]
		public BankBox BankBox
		{
			get
			{
				if (this.m_BankBox != null && !this.m_BankBox.Deleted && this.m_BankBox.Parent == this)
					return this.m_BankBox;

				this.m_BankBox = this.FindItemOnLayer(Layer.Bank) as BankBox;

				if (this.m_BankBox == null)
					this.AddItem(this.m_BankBox = new BankBox(this));

				return this.m_BankBox;
			}
		}

		public BankBox FindBankNoCreate()
		{
			if (this.m_BankBox != null && !this.m_BankBox.Deleted && this.m_BankBox.Parent == this)
				return this.m_BankBox;

			this.m_BankBox = this.FindItemOnLayer(Layer.Bank) as BankBox;

			return this.m_BankBox;
		}

		private Container m_Backpack;

		[CommandProperty(AccessLevel.GameMaster)]
		public Container Backpack
		{
			get
			{
				if (this.m_Backpack != null && !this.m_Backpack.Deleted && this.m_Backpack.Parent == this)
					return this.m_Backpack;

				return (this.m_Backpack = (this.FindItemOnLayer(Layer.Backpack) as Container));
			}
		}

		public virtual bool KeepsItemsOnDeath
		{
			get
			{
				return this.IsStaff();
			}
		}

		public Item FindItemOnLayer(Layer layer)
		{
			List<Item> eq = this.m_Items;
			int count = eq.Count;

			for (int i = 0; i < count; ++i)
			{
				Item item = eq[i];

				if (!item.Deleted && item.Layer == layer)
				{
					return item;
				}
			}

			return null;
		}

		[CommandProperty(AccessLevel.Counselor, AccessLevel.Decorator)]
		public int X
		{
			get
			{
				return this.m_Location.m_X;
			}
			set
			{
				this.Location = new Point3D(value, this.m_Location.m_Y, this.m_Location.m_Z);
			}
		}

		[CommandProperty(AccessLevel.Counselor, AccessLevel.Decorator)]
		public int Y
		{
			get
			{
				return this.m_Location.m_Y;
			}
			set
			{
				this.Location = new Point3D(this.m_Location.m_X, value, this.m_Location.m_Z);
			}
		}

		[CommandProperty(AccessLevel.Counselor, AccessLevel.Decorator)]
		public int Z
		{
			get
			{
				return this.m_Location.m_Z;
			}
			set
			{
				this.Location = new Point3D(this.m_Location.m_X, this.m_Location.m_Y, value);
			}
		}

		#region Effects & Particles

		public void MovingEffect(IEntity to, int itemID, int speed, int duration, bool fixedDirection, bool explodes, int hue, int renderMode)
		{
			Effects.SendMovingEffect(this, to, itemID, speed, duration, fixedDirection, explodes, hue, renderMode);
		}

		public void MovingEffect(IEntity to, int itemID, int speed, int duration, bool fixedDirection, bool explodes)
		{
			Effects.SendMovingEffect(this, to, itemID, speed, duration, fixedDirection, explodes, 0, 0);
		}

		public void MovingParticles(IEntity to, int itemID, int speed, int duration, bool fixedDirection, bool explodes, int hue, int renderMode, int effect, int explodeEffect, int explodeSound, EffectLayer layer, int unknown)
		{
			Effects.SendMovingParticles(this, to, itemID, speed, duration, fixedDirection, explodes, hue, renderMode, effect, explodeEffect, explodeSound, layer, unknown);
		}

		public void MovingParticles(IEntity to, int itemID, int speed, int duration, bool fixedDirection, bool explodes, int hue, int renderMode, int effect, int explodeEffect, int explodeSound, int unknown)
		{
			Effects.SendMovingParticles(this, to, itemID, speed, duration, fixedDirection, explodes, hue, renderMode, effect, explodeEffect, explodeSound, (EffectLayer)255, unknown);
		}

		public void MovingParticles(IEntity to, int itemID, int speed, int duration, bool fixedDirection, bool explodes, int effect, int explodeEffect, int explodeSound, int unknown)
		{
			Effects.SendMovingParticles(this, to, itemID, speed, duration, fixedDirection, explodes, effect, explodeEffect, explodeSound, unknown);
		}

		public void MovingParticles(IEntity to, int itemID, int speed, int duration, bool fixedDirection, bool explodes, int effect, int explodeEffect, int explodeSound)
		{
			Effects.SendMovingParticles(this, to, itemID, speed, duration, fixedDirection, explodes, 0, 0, effect, explodeEffect, explodeSound, 0);
		}

		public void FixedEffect(int itemID, int speed, int duration, int hue, int renderMode)
		{
			Effects.SendTargetEffect(this, itemID, speed, duration, hue, renderMode);
		}

		public void FixedEffect(int itemID, int speed, int duration)
		{
			Effects.SendTargetEffect(this, itemID, speed, duration, 0, 0);
		}

		public void FixedParticles(int itemID, int speed, int duration, int effect, int hue, int renderMode, EffectLayer layer, int unknown)
		{
			Effects.SendTargetParticles(this, itemID, speed, duration, hue, renderMode, effect, layer, unknown);
		}

		public void FixedParticles(int itemID, int speed, int duration, int effect, int hue, int renderMode, EffectLayer layer)
		{
			Effects.SendTargetParticles(this, itemID, speed, duration, hue, renderMode, effect, layer, 0);
		}

		public void FixedParticles(int itemID, int speed, int duration, int effect, EffectLayer layer, int unknown)
		{
			Effects.SendTargetParticles(this, itemID, speed, duration, 0, 0, effect, layer, unknown);
		}

		public void FixedParticles(int itemID, int speed, int duration, int effect, EffectLayer layer)
		{
			Effects.SendTargetParticles(this, itemID, speed, duration, 0, 0, effect, layer, 0);
		}

		public void BoltEffect(int hue)
		{
			Effects.SendBoltEffect(this, true, hue);
		}

		#endregion

		public void SendIncomingPacket()
		{
			if (this.m_Map != null)
			{
				IPooledEnumerable eable = this.m_Map.GetClientsInRange(this.m_Location);

				foreach (NetState state in eable)
				{
					if (state.Mobile.CanSee(this))
					{
						if (state.StygianAbyss)
						{
							state.Send(new MobileIncoming(state.Mobile, this));

							if (this.m_Poison != null)
								state.Send(new HealthbarPoison(this));

							if (this.m_Blessed || this.m_YellowHealthbar)
								state.Send(new HealthbarYellow(this));
						}
						else
						{
							state.Send(new MobileIncomingOld(state.Mobile, this));
						}

						if (this.IsDeadBondedPet)
							state.Send(new BondedStatus(0, this.m_Serial, 1));

						if (ObjectPropertyList.Enabled)
						{
							state.Send(this.OPLPacket);
							//foreach ( Item item in m_Items )
							//	state.Send( item.OPLPacket );
						}
					}
				}

				eable.Free();
			}
		}

		public bool PlaceInBackpack(Item item)
		{
			if (item.Deleted)
				return false;

			Container pack = this.Backpack;

			return pack != null && pack.TryDropItem(this, item, false);
		}

		public bool AddToBackpack(Item item)
		{
			if (item.Deleted)
				return false;

			if (!this.PlaceInBackpack(item))
			{
				Point3D loc = this.m_Location;
				Map map = this.m_Map;

				if ((map == null || map == Map.Internal) && this.m_LogoutMap != null)
				{
					loc = this.m_LogoutLocation;
					map = this.m_LogoutMap;
				}

				item.MoveToWorld(loc, map);
				return false;
			}

			return true;
		}

		public virtual bool CheckLift(Mobile from, Item item, ref LRReason reject)
		{
			return true;
		}

		public virtual bool CheckNonlocalLift(Mobile from, Item item)
		{
			if (from == this || (from.AccessLevel > this.AccessLevel && from.AccessLevel >= AccessLevel.GameMaster))
				return true;

			return false;
		}

		public bool HasTrade
		{
			get
			{
				if (this.m_NetState != null)
					return this.m_NetState.Trades.Count > 0;

				return false;
			}
		}

		public virtual bool CheckTrade(Mobile to, Item item, SecureTradeContainer cont, bool message, bool checkItems, int plusItems, int plusWeight)
		{
			return true;
		}

		/// <summary>
		/// Overridable. Event invoked when a Mobile (<paramref name="from" />) drops an <see cref="Item"><paramref name="dropped" /></see> onto the Mobile.
		/// </summary>
		public virtual bool OnDragDrop(Mobile from, Item dropped)
		{
			if (from == this)
			{
				Container pack = this.Backpack;

				if (pack != null)
					return dropped.DropToItem(from, pack, new Point3D(-1, -1, 0));

				return false;
			}
			else if (from.Player && this.Player && from.Alive && this.Alive && from.InRange(this.Location, 2))
			{
				NetState ourState = this.m_NetState;
				NetState theirState = from.m_NetState;

				if (ourState != null && theirState != null)
				{
					SecureTradeContainer cont = theirState.FindTradeContainer(this);

					if (!from.CheckTrade(this, dropped, cont, true, true, 0, 0))
						return false;

					if (cont == null)
						cont = theirState.AddTrade(ourState);

					cont.DropItem(dropped);

					return true;
				}

				return false;
			}
			else
			{
				return false;
			}
		}

		public virtual bool CheckEquip(Item item)
		{
			for (int i = 0; i < this.m_Items.Count; ++i)
				if (this.m_Items[i].CheckConflictingLayer(this, item, item.Layer) || item.CheckConflictingLayer(this, this.m_Items[i], this.m_Items[i].Layer))
					return false;

			return true;
		}

		/// <summary>
		/// Overridable. Virtual event invoked when the Mobile attempts to wear <paramref name="item" />.
		/// </summary>
		/// <returns>True if the request is accepted, false if otherwise.</returns>
		public virtual bool OnEquip(Item item)
		{
			return true;
		}

		/// <summary>
		/// Overridable. Virtual event invoked when the Mobile attempts to lift <paramref name="item" />.
		/// </summary>
		/// <returns>True if the lift is allowed, false if otherwise.</returns>
		/// <example>
		/// The following example demonstrates usage. It will disallow any attempts to pick up a pick axe if the Mobile does not have enough strength.
		/// <code>
		/// public override bool OnDragLift( Item item )
		/// {
		///		if ( item is Pickaxe &amp;&amp; this.Str &lt; 60 )
		///		{
		///			SendMessage( "That is too heavy for you to lift." );
		///			return false;
		///		}
		///		
		///		return base.OnDragLift( item );
		/// }</code>
		/// </example>
		public virtual bool OnDragLift(Item item)
		{
			return true;
		}

		/// <summary>
		/// Overridable. Virtual event invoked when the Mobile attempts to drop <paramref name="item" /> into a <see cref="Container"><paramref name="container" /></see>.
		/// </summary>
		/// <returns>True if the drop is allowed, false if otherwise.</returns>
		public virtual bool OnDroppedItemInto(Item item, Container container, Point3D loc)
		{
			return true;
		}

		/// <summary>
		/// Overridable. Virtual event invoked when the Mobile attempts to drop <paramref name="item" /> directly onto another <see cref="Item" />, <paramref name="target" />. This is the case of stacking items.
		/// </summary>
		/// <returns>True if the drop is allowed, false if otherwise.</returns>
		public virtual bool OnDroppedItemOnto(Item item, Item target)
		{
			return true;
		}

		/// <summary>
		/// Overridable. Virtual event invoked when the Mobile attempts to drop <paramref name="item" /> into another <see cref="Item" />, <paramref name="target" />. The target item is most likely a <see cref="Container" />.
		/// </summary>
		/// <returns>True if the drop is allowed, false if otherwise.</returns>
		public virtual bool OnDroppedItemToItem(Item item, Item target, Point3D loc)
		{
			return true;
		}

		/// <summary>
		/// Overridable. Virtual event invoked when the Mobile attempts to give <paramref name="item" /> to a Mobile (<paramref name="target" />).
		/// </summary>
		/// <returns>True if the drop is allowed, false if otherwise.</returns>
		public virtual bool OnDroppedItemToMobile(Item item, Mobile target)
		{
			return true;
		}

		/// <summary>
		/// Overridable. Virtual event invoked when the Mobile attempts to drop <paramref name="item" /> to the world at a <see cref="Point3D"><paramref name="location" /></see>.
		/// </summary>
		/// <returns>True if the drop is allowed, false if otherwise.</returns>
		public virtual bool OnDroppedItemToWorld(Item item, Point3D location)
		{
			return true;
		}

		/// <summary>
		/// Overridable. Virtual event when <paramref name="from" /> successfully uses <paramref name="item" /> while it's on this Mobile.
		/// <seealso cref="Item.OnItemUsed" />
		/// </summary>
		public virtual void OnItemUsed(Mobile from, Item item)
		{
			EventSink.InvokeOnItemUse(new OnItemUseEventArgs(from, item));
		}

		public virtual bool CheckNonlocalDrop(Mobile from, Item item, Item target)
		{
			if (from == this || (from.AccessLevel > this.AccessLevel && from.AccessLevel >= AccessLevel.GameMaster))
				return true;

			return false;
		}

		public virtual bool CheckItemUse(Mobile from, Item item)
		{
			return true;
		}

		/// <summary>
		/// Overridable. Virtual event invoked when <paramref name="from" /> successfully lifts <paramref name="item" /> from this Mobile.
		/// <seealso cref="Item.OnItemLifted" />
		/// </summary>
		public virtual void OnItemLifted(Mobile from, Item item)
		{
		}

		public virtual bool AllowItemUse(Item item)
		{
			return true;
		}

		public virtual bool AllowEquipFrom(Mobile mob)
		{
			return (mob == this || (mob.AccessLevel >= AccessLevel.Decorator && mob.AccessLevel > this.AccessLevel));
		}

		public virtual bool EquipItem(Item item)
		{
			if (item == null || item.Deleted || !item.CanEquip(this))
				return false;

			if (this.CheckEquip(item) && this.OnEquip(item) && item.OnEquip(this))
			{
				if (this.m_Spell != null && !this.m_Spell.OnCasterEquiping(item))
					return false;

				//if ( m_Spell != null && m_Spell.State == SpellState.Casting )
				//	m_Spell.Disturb( DisturbType.EquipRequest );

				this.AddItem(item);
				return true;
			}

			return false;
		}

		internal int m_TypeRef;

		public Mobile(Serial serial)
		{
			this.m_Region = Map.Internal.DefaultRegion;
			this.m_Serial = serial;
			this.m_Aggressors = new List<AggressorInfo>();
			this.m_Aggressed = new List<AggressorInfo>();
			this.m_NextSkillTime = DateTime.MinValue;
			this.m_DamageEntries = new List<DamageEntry>();

			Type ourType = this.GetType();
			this.m_TypeRef = World.m_MobileTypes.IndexOf(ourType);

			if (this.m_TypeRef == -1)
			{
				World.m_MobileTypes.Add(ourType);
				this.m_TypeRef = World.m_MobileTypes.Count - 1;
			}
		}

		public Mobile()
		{
			this.m_Region = Map.Internal.DefaultRegion;
			this.m_Serial = Server.Serial.NewMobile;

			this.DefaultMobileInit();

			World.AddMobile(this);

			Type ourType = this.GetType();
			this.m_TypeRef = World.m_MobileTypes.IndexOf(ourType);

			if (this.m_TypeRef == -1)
			{
				World.m_MobileTypes.Add(ourType);
				this.m_TypeRef = World.m_MobileTypes.Count - 1;
			}
		}

		public void DefaultMobileInit()
		{
			this.m_StatCap = 225;
			this.m_FollowersMax = 5;
			this.m_Skills = new Skills(this);
			this.m_Items = new List<Item>();
			this.m_StatMods = new List<StatMod>();
			this.m_SkillMods = new List<SkillMod>();
			this.Map = Map.Internal;
			this.m_AutoPageNotify = true;
			this.m_Aggressors = new List<AggressorInfo>();
			this.m_Aggressed = new List<AggressorInfo>();
			this.m_Virtues = new VirtueInfo();
			this.m_Stabled = new List<Mobile>();
			this.m_DamageEntries = new List<DamageEntry>();

			this.m_NextSkillTime = DateTime.MinValue;
			this.m_CreationTime = DateTime.Now;
		}

		private static readonly Queue<Mobile> m_DeltaQueue = new Queue<Mobile>();

		private bool m_InDeltaQueue;
		private MobileDelta m_DeltaFlags;

		public virtual void Delta(MobileDelta flag)
		{
			if (this.m_Map == null || this.m_Map == Map.Internal || this.m_Deleted)
				return;

			this.m_DeltaFlags |= flag;

			if (!this.m_InDeltaQueue)
			{
				this.m_InDeltaQueue = true;

				m_DeltaQueue.Enqueue(this);
			}

			Core.Set();
		}

		#region GetDirectionTo[..]

		public Direction GetDirectionTo(int x, int y)
		{
			int dx = this.m_Location.m_X - x;
			int dy = this.m_Location.m_Y - y;

			int rx = (dx - dy) * 44;
			int ry = (dx + dy) * 44;

			int ax = Math.Abs(rx);
			int ay = Math.Abs(ry);

			Direction ret;

			if (((ay >> 1) - ax) >= 0)
				ret = (ry > 0) ? Direction.Up : Direction.Down;
			else if (((ax >> 1) - ay) >= 0)
				ret = (rx > 0) ? Direction.Left : Direction.Right;
			else if (rx >= 0 && ry >= 0)
				ret = Direction.West;
			else if (rx >= 0 && ry < 0)
				ret = Direction.South;
			else if (rx < 0 && ry < 0)
				ret = Direction.East;
			else
				ret = Direction.North;

			return ret;
		}

		public Direction GetDirectionTo(Point2D p)
		{
			return this.GetDirectionTo(p.m_X, p.m_Y);
		}

		public Direction GetDirectionTo(Point3D p)
		{
			return this.GetDirectionTo(p.m_X, p.m_Y);
		}

		public Direction GetDirectionTo(IPoint2D p)
		{
			if (p == null)
				return Direction.North;

			return this.GetDirectionTo(p.X, p.Y);
		}

		#endregion

		public virtual void ProcessDelta()
		{
			Mobile m = this;
			MobileDelta delta;

			delta = m.m_DeltaFlags;

			if (delta == MobileDelta.None)
				return;

			MobileDelta attrs = delta & MobileDelta.Attributes;

			m.m_DeltaFlags = MobileDelta.None;
			m.m_InDeltaQueue = false;

			bool sendHits = false, sendStam = false, sendMana = false, sendAll = false, sendAny = false;
			bool sendIncoming = false, sendNonlocalIncoming = false;
			bool sendUpdate = false, sendRemove = false;
			bool sendPublicStats = false, sendPrivateStats = false;
			bool sendMoving = false, sendNonlocalMoving = false;
			bool sendOPLUpdate = ObjectPropertyList.Enabled && (delta & MobileDelta.Properties) != 0;

			bool sendHair = false, sendFacialHair = false, removeHair = false, removeFacialHair = false;

			bool sendHealthbarPoison = false, sendHealthbarYellow = false;

			if (attrs != MobileDelta.None)
			{
				sendAny = true;

				if (attrs == MobileDelta.Attributes)
				{
					sendAll = true;
				}
				else
				{
					sendHits = ((attrs & MobileDelta.Hits) != 0);
					sendStam = ((attrs & MobileDelta.Stam) != 0);
					sendMana = ((attrs & MobileDelta.Mana) != 0);
				}
			}

			if ((delta & MobileDelta.GhostUpdate) != 0)
			{
				sendNonlocalIncoming = true;
			}

			if ((delta & MobileDelta.Hue) != 0)
			{
				sendNonlocalIncoming = true;
				sendUpdate = true;
				sendRemove = true;
			}

			if ((delta & MobileDelta.Direction) != 0)
			{
				sendNonlocalMoving = true;
				sendUpdate = true;
			}

			if ((delta & MobileDelta.Body) != 0)
			{
				sendUpdate = true;
				sendIncoming = true;
			}

			/*if ( (delta & MobileDelta.Hue) != 0 )
			{
			sendNonlocalIncoming = true;
			sendUpdate = true;
			}
			else if ( (delta & (MobileDelta.Direction | MobileDelta.Body)) != 0 )
			{
			sendNonlocalMoving = true;
			sendUpdate = true;
			}
			else*/
			if ((delta & (MobileDelta.Flags | MobileDelta.Noto)) != 0)
			{
				sendMoving = true;
			}

			if ((delta & MobileDelta.HealthbarPoison) != 0)
			{
				sendHealthbarPoison = true;
			}

			if ((delta & MobileDelta.HealthbarYellow) != 0)
			{
				sendHealthbarYellow = true;
			}

			if ((delta & MobileDelta.Name) != 0)
			{
				sendAll = false;
				sendHits = false;
				sendAny = sendStam || sendMana;
				sendPublicStats = true;
			}

			if ((delta & (MobileDelta.WeaponDamage | MobileDelta.Resistances | MobileDelta.Stat | MobileDelta.Weight | MobileDelta.Gold | MobileDelta.Armor | MobileDelta.StatCap | MobileDelta.Followers | MobileDelta.TithingPoints | MobileDelta.Race)) != 0)
			{
				sendPrivateStats = true;
			}

			if ((delta & MobileDelta.Hair) != 0)
			{
				if (m.HairItemID <= 0)
					removeHair = true;

				sendHair = true;
			}

			if ((delta & MobileDelta.FacialHair) != 0)
			{
				if (m.FacialHairItemID <= 0)
					removeFacialHair = true;

				sendFacialHair = true;
			}

			Packet[][] cache = m_MovingPacketCache;

			if (sendMoving || sendNonlocalMoving || sendHealthbarPoison || sendHealthbarYellow)
			{
				for (int i = 0; i < cache.Length; ++i)
					for (int j = 0; j < cache[i].Length; ++j)
						Packet.Release(ref cache[i][j]);
			}

			NetState ourState = m.m_NetState;

			if (ourState != null)
			{
				if (sendUpdate)
				{
					ourState.Sequence = 0;

					if (ourState.StygianAbyss)
						ourState.Send(new MobileUpdate(m));
					else
						ourState.Send(new MobileUpdateOld(m));

					this.ClearFastwalkStack();
				}

				if (ourState.StygianAbyss)
				{
					if (sendIncoming)
						ourState.Send(new MobileIncoming(m, m));

					if (sendMoving)
					{
						int noto = Notoriety.Compute(m, m);
						ourState.Send(cache[0][noto] = Packet.Acquire(new MobileMoving(m, noto)));
					}

					if (sendHealthbarPoison)
						ourState.Send(new HealthbarPoison(m));

					if (sendHealthbarYellow)
						ourState.Send(new HealthbarYellow(m));
				}
				else
				{
					if (sendIncoming)
						ourState.Send(new MobileIncomingOld(m, m));

					if (sendMoving || sendHealthbarPoison || sendHealthbarYellow)
					{
						int noto = Notoriety.Compute(m, m);
						ourState.Send(cache[1][noto] = Packet.Acquire(new MobileMovingOld(m, noto)));
					}
				}

				if (sendPublicStats || sendPrivateStats)
				{
					ourState.Send(new MobileStatusExtended(m, this.m_NetState));
				}
				else if (sendAll)
				{
					ourState.Send(new MobileAttributes(m));
				}
				else if (sendAny)
				{
					if (sendHits)
						ourState.Send(new MobileHits(m));

					if (sendStam)
						ourState.Send(new MobileStam(m));

					if (sendMana)
						ourState.Send(new MobileMana(m));
				}

				if (sendStam || sendMana)
				{
					IParty ip = this.m_Party as IParty;

					if (ip != null && sendStam)
						ip.OnStamChanged(this);

					if (ip != null && sendMana)
						ip.OnManaChanged(this);
				}

				if (sendHair)
				{
					if (removeHair)
						ourState.Send(new RemoveHair(m));
					else
						ourState.Send(new HairEquipUpdate(m));
				}

				if (sendFacialHair)
				{
					if (removeFacialHair)
						ourState.Send(new RemoveFacialHair(m));
					else
						ourState.Send(new FacialHairEquipUpdate(m));
				}

				if (sendOPLUpdate)
					ourState.Send(this.OPLPacket);
			}

			sendMoving = sendMoving || sendNonlocalMoving;
			sendIncoming = sendIncoming || sendNonlocalIncoming;
			sendHits = sendHits || sendAll;

			if (m.m_Map != null && (sendRemove || sendIncoming || sendPublicStats || sendHits || sendMoving || sendOPLUpdate || sendHair || sendFacialHair || sendHealthbarPoison || sendHealthbarYellow))
			{
				Mobile beholder;

				IPooledEnumerable eable = m.Map.GetClientsInRange(m.m_Location);

				Packet hitsPacket = null;
				Packet statPacketTrue = null, statPacketFalse = null;
				Packet deadPacket = null;
				Packet hairPacket = null, facialhairPacket = null;
				Packet hbpPacket = null, hbyPacket = null;

				foreach (NetState state in eable)
				{
					beholder = state.Mobile;

					if (beholder != m && beholder.CanSee(m))
					{
						if (sendRemove)
							state.Send(m.RemovePacket);

						if (sendIncoming)
						{
							if (state.StygianAbyss)
							{
								state.Send(new MobileIncoming(beholder, m));
							}
							else
							{
								state.Send(new MobileIncomingOld(beholder, m));
							}

							if (m.IsDeadBondedPet)
							{
								if (deadPacket == null)
									deadPacket = Packet.Acquire(new BondedStatus(0, m.m_Serial, 1));

								state.Send(deadPacket);
							}
						}

						if (state.StygianAbyss)
						{
							if (sendMoving)
							{
								int noto = Notoriety.Compute(beholder, m);

								Packet p = cache[0][noto];

								if (p == null)
									cache[0][noto] = p = Packet.Acquire(new MobileMoving(m, noto));

								state.Send(p);
							}

							if (sendHealthbarPoison)
							{
								if (hbpPacket == null)
									hbpPacket = Packet.Acquire(new HealthbarPoison(m));
								
								state.Send(hbpPacket);
							}

							if (sendHealthbarYellow)
							{
								if (hbyPacket == null)
									hbyPacket = Packet.Acquire(new HealthbarYellow(m));
								state.Send(hbyPacket);
							}
						}
						else
						{
							if (sendMoving || sendHealthbarPoison || sendHealthbarYellow)
							{
								int noto = Notoriety.Compute(beholder, m);

								Packet p = cache[1][noto];

								if (p == null)
									cache[1][noto] = p = Packet.Acquire(new MobileMovingOld(m, noto));

								state.Send(p);
							}
						}

						if (sendPublicStats)
						{
							if (m.CanBeRenamedBy(beholder))
							{
								if (statPacketTrue == null)
									statPacketTrue = Packet.Acquire(new MobileStatusCompact(true, m));

								state.Send(statPacketTrue);
							}
							else
							{
								if (statPacketFalse == null)
									statPacketFalse = Packet.Acquire(new MobileStatusCompact(false, m));

								state.Send(statPacketFalse);
							}
						}
						else if (sendHits)
						{
							if (hitsPacket == null)
								hitsPacket = Packet.Acquire(new MobileHitsN(m));

							state.Send(hitsPacket);
						}

						if (sendHair)
						{
							if (hairPacket == null)
							{
								if (removeHair)
									hairPacket = Packet.Acquire(new RemoveHair(m));
								else
									hairPacket = Packet.Acquire(new HairEquipUpdate(m));
							}

							state.Send(hairPacket);
						}

						if (sendFacialHair)
						{
							if (facialhairPacket == null)
							{
								if (removeFacialHair)
									facialhairPacket = Packet.Acquire(new RemoveFacialHair(m));
								else
									facialhairPacket = Packet.Acquire(new FacialHairEquipUpdate(m));
							}

							state.Send(facialhairPacket);
						}

						if (sendOPLUpdate)
							state.Send(this.OPLPacket);
					}
				}

				Packet.Release(hitsPacket);
				Packet.Release(statPacketTrue);
				Packet.Release(statPacketFalse);
				Packet.Release(deadPacket);
				Packet.Release(hairPacket);
				Packet.Release(facialhairPacket);
				Packet.Release(hbpPacket);
				Packet.Release(hbyPacket);

				eable.Free();
			}

			if (sendMoving || sendNonlocalMoving || sendHealthbarPoison || sendHealthbarYellow)
			{
				for (int i = 0; i < cache.Length; ++i)
					for (int j = 0; j < cache.Length; ++j)
						Packet.Release(ref cache[i][j]);
			}
		}

		public static void ProcessDeltaQueue()
		{
			int count = m_DeltaQueue.Count;
			int index = 0;

			while (m_DeltaQueue.Count > 0 && index++ < count)
				m_DeltaQueue.Dequeue().ProcessDelta();
		}

		[CommandProperty(AccessLevel.Counselor, AccessLevel.Decorator)]
		public int Kills
		{
			get
			{
				return this.m_Kills;
			}
			set
			{
				int oldValue = this.m_Kills;

				if (this.m_Kills != value)
				{
					this.m_Kills = value;

					if (this.m_Kills < 0)
						this.m_Kills = 0;

					if ((oldValue >= 5) != (this.m_Kills >= 5))
					{
						this.Delta(MobileDelta.Noto);
						this.InvalidateProperties();
					}

					this.OnKillsChange(oldValue);
				}
			}
		}

		public virtual void OnKillsChange(int oldValue)
		{
		}

		[CommandProperty(AccessLevel.GameMaster)]
		public int ShortTermMurders
		{
			get
			{
				return this.m_ShortTermMurders;
			}
			set
			{
				if (this.m_ShortTermMurders != value)
				{
					this.m_ShortTermMurders = value;

					if (this.m_ShortTermMurders < 0)
						this.m_ShortTermMurders = 0;
				}
			}
		}

		[CommandProperty(AccessLevel.Counselor, AccessLevel.Decorator)]
		public bool Criminal
		{
			get
			{
				return this.m_Criminal;
			}
			set
			{
				if (this.m_Criminal != value)
				{
					this.m_Criminal = value;
					this.Delta(MobileDelta.Noto);
					this.InvalidateProperties();
				}

				if (this.m_Criminal)
				{
					if (this.m_ExpireCriminal == null)
						this.m_ExpireCriminal = new ExpireCriminalTimer(this);
					else
						this.m_ExpireCriminal.Stop();

					this.m_ExpireCriminal.Start();
				}
				else if (this.m_ExpireCriminal != null)
				{
					this.m_ExpireCriminal.Stop();
					this.m_ExpireCriminal = null;
				}
			}
		}

		public bool CheckAlive()
		{
			return this.CheckAlive(true);
		}

		public bool CheckAlive(bool message)
		{
			if (!this.Alive)
			{
				if (message)
					this.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019048); // I am dead and cannot do that.

				return false;
			}
			else
			{
				return true;
			}
		}

		#region Overhead messages

		public void PublicOverheadMessage(MessageType type, int hue, bool ascii, string text)
		{
			this.PublicOverheadMessage(type, hue, ascii, text, true);
		}

		public void PublicOverheadMessage(MessageType type, int hue, bool ascii, string text, bool noLineOfSight)
		{
			if (this.m_Map != null)
			{
				Packet p = null;

				IPooledEnumerable eable = this.m_Map.GetClientsInRange(this.m_Location);

				foreach (NetState state in eable)
				{
					if (state.Mobile.CanSee(this) && (noLineOfSight || state.Mobile.InLOS(this)))
					{
						if (p == null)
						{
							if (ascii)
								p = new AsciiMessage(this.m_Serial, this.Body, type, hue, 3, this.Name, text);
							else
								p = new UnicodeMessage(this.m_Serial, this.Body, type, hue, 3, this.m_Language, this.Name, text);

							p.Acquire();
						}

						state.Send(p);
					}
				}

				Packet.Release(p);

				eable.Free();
			}
		}

		public void PublicOverheadMessage(MessageType type, int hue, int number)
		{
			this.PublicOverheadMessage(type, hue, number, "", true);
		}

		public void PublicOverheadMessage(MessageType type, int hue, int number, string args)
		{
			this.PublicOverheadMessage(type, hue, number, args, true);
		}

		public void PublicOverheadMessage(MessageType type, int hue, int number, string args, bool noLineOfSight)
		{
			if (this.m_Map != null)
			{
				Packet p = null;

				IPooledEnumerable eable = this.m_Map.GetClientsInRange(this.m_Location);

				foreach (NetState state in eable)
				{
					if (state.Mobile.CanSee(this) && (noLineOfSight || state.Mobile.InLOS(this)))
					{
						if (p == null)
							p = Packet.Acquire(new MessageLocalized(this.m_Serial, this.Body, type, hue, 3, number, this.Name, args));

						state.Send(p);
					}
				}

				Packet.Release(p);

				eable.Free();
			}
		}

		public void PublicOverheadMessage(MessageType type, int hue, int number, AffixType affixType, string affix, string args)
		{
			this.PublicOverheadMessage(type, hue, number, affixType, affix, args, true);
		}

		public void PublicOverheadMessage(MessageType type, int hue, int number, AffixType affixType, string affix, string args, bool noLineOfSight)
		{
			if (this.m_Map != null)
			{
				Packet p = null;

				IPooledEnumerable eable = this.m_Map.GetClientsInRange(this.m_Location);

				foreach (NetState state in eable)
				{
					if (state.Mobile.CanSee(this) && (noLineOfSight || state.Mobile.InLOS(this)))
					{
						if (p == null)
							p = Packet.Acquire(new MessageLocalizedAffix(this.m_Serial, this.Body, type, hue, 3, number, this.Name, affixType, affix, args));

						state.Send(p);
					}
				}

				Packet.Release(p);

				eable.Free();
			}
		}

		public void PrivateOverheadMessage(MessageType type, int hue, bool ascii, string text, NetState state)
		{
			if (state == null)
				return;

			if (ascii)
				state.Send(new AsciiMessage(this.m_Serial, this.Body, type, hue, 3, this.Name, text));
			else
				state.Send(new UnicodeMessage(this.m_Serial, this.Body, type, hue, 3, this.m_Language, this.Name, text));
		}

		public void PrivateOverheadMessage(MessageType type, int hue, int number, NetState state)
		{
			this.PrivateOverheadMessage(type, hue, number, "", state);
		}

		public void PrivateOverheadMessage(MessageType type, int hue, int number, string args, NetState state)
		{
			if (state == null)
				return;

			state.Send(new MessageLocalized(this.m_Serial, this.Body, type, hue, 3, number, this.Name, args));
		}

		public void LocalOverheadMessage(MessageType type, int hue, bool ascii, string text)
		{
			NetState ns = this.m_NetState;

			if (ns != null)
			{
				if (ascii)
					ns.Send(new AsciiMessage(this.m_Serial, this.Body, type, hue, 3, this.Name, text));
				else
					ns.Send(new UnicodeMessage(this.m_Serial, this.Body, type, hue, 3, this.m_Language, this.Name, text));
			}
		}

		public void LocalOverheadMessage(MessageType type, int hue, int number)
		{
			this.LocalOverheadMessage(type, hue, number, "");
		}

		public void LocalOverheadMessage(MessageType type, int hue, int number, string args)
		{
			NetState ns = this.m_NetState;

			if (ns != null)
				ns.Send(new MessageLocalized(this.m_Serial, this.Body, type, hue, 3, number, this.Name, args));
		}

		public void NonlocalOverheadMessage(MessageType type, int hue, int number)
		{
			this.NonlocalOverheadMessage(type, hue, number, "");
		}

		public void NonlocalOverheadMessage(MessageType type, int hue, int number, string args)
		{
			if (this.m_Map != null)
			{
				Packet p = null;

				IPooledEnumerable eable = this.m_Map.GetClientsInRange(this.m_Location);

				foreach (NetState state in eable)
				{
					if (state != this.m_NetState && state.Mobile.CanSee(this))
					{
						if (p == null)
							p = Packet.Acquire(new MessageLocalized(this.m_Serial, this.Body, type, hue, 3, number, this.Name, args));

						state.Send(p);
					}
				}

				Packet.Release(p);

				eable.Free();
			}
		}

		public void NonlocalOverheadMessage(MessageType type, int hue, bool ascii, string text)
		{
			if (this.m_Map != null)
			{
				Packet p = null;

				IPooledEnumerable eable = this.m_Map.GetClientsInRange(this.m_Location);

				foreach (NetState state in eable)
				{
					if (state != this.m_NetState && state.Mobile.CanSee(this))
					{
						if (p == null)
						{
							if (ascii)
								p = new AsciiMessage(this.m_Serial, this.Body, type, hue, 3, this.Name, text);
							else
								p = new UnicodeMessage(this.m_Serial, this.Body, type, hue, 3, this.Language, this.Name, text);

							p.Acquire();
						}

						state.Send(p);
					}
				}

				Packet.Release(p);

				eable.Free();
			}
		}

		#endregion

		#region SendLocalizedMessage

		public void SendLocalizedMessage(int number)
		{
			NetState ns = this.m_NetState;

			if (ns != null)
				ns.Send(MessageLocalized.InstantiateGeneric(number));
		}

		public void SendLocalizedMessage(int number, string args)
		{
			this.SendLocalizedMessage(number, args, 0x3B2);
		}

		public void SendLocalizedMessage(int number, string args, int hue)
		{
			if (hue == 0x3B2 && (args == null || args.Length == 0))
			{
				NetState ns = this.m_NetState;

				if (ns != null)
					ns.Send(MessageLocalized.InstantiateGeneric(number));
			}
			else
			{
				NetState ns = this.m_NetState;

				if (ns != null)
					ns.Send(new MessageLocalized(Serial.MinusOne, -1, MessageType.Regular, hue, 3, number, "System", args));
			}
		}

		public void SendLocalizedMessage(int number, bool append, string affix)
		{
			this.SendLocalizedMessage(number, append, affix, "", 0x3B2);
		}

		public void SendLocalizedMessage(int number, bool append, string affix, string args)
		{
			this.SendLocalizedMessage(number, append, affix, args, 0x3B2);
		}

		public void SendLocalizedMessage(int number, bool append, string affix, string args, int hue)
		{
			NetState ns = this.m_NetState;

			if (ns != null)
				ns.Send(new MessageLocalizedAffix(Serial.MinusOne, -1, MessageType.Regular, hue, 3, number, "System", (append ? AffixType.Append : AffixType.Prepend) | AffixType.System, affix, args));
		}

		#endregion

		public void LaunchBrowser(string url)
		{
			if (this.m_NetState != null)
				this.m_NetState.LaunchBrowser(url);
		}

		#region Send[ASCII]Message

		public void SendMessage(string text)
		{
			this.SendMessage(0x3B2, text);
		}

		public void SendMessage(string format, params object[] args)
		{
			this.SendMessage(0x3B2, String.Format(format, args));
		}

		public void SendMessage(int hue, string text)
		{
			NetState ns = this.m_NetState;

			if (ns != null)
				ns.Send(new UnicodeMessage(Serial.MinusOne, -1, MessageType.Regular, hue, 3, "ENU", "System", text));
		}

		public void SendMessage(int hue, string format, params object[] args)
		{
			this.SendMessage(hue, String.Format(format, args));
		}

		public void SendAsciiMessage(string text)
		{
			this.SendAsciiMessage(0x3B2, text);
		}

		public void SendAsciiMessage(string format, params object[] args)
		{
			this.SendAsciiMessage(0x3B2, String.Format(format, args));
		}

		public void SendAsciiMessage(int hue, string text)
		{
			NetState ns = this.m_NetState;

			if (ns != null)
				ns.Send(new AsciiMessage(Serial.MinusOne, -1, MessageType.Regular, hue, 3, "System", text));
		}

		public void SendAsciiMessage(int hue, string format, params object[] args)
		{
			this.SendAsciiMessage(hue, String.Format(format, args));
		}

		#endregion

		#region InRange
		public bool InRange(Point2D p, int range)
		{
			return (p.m_X >= (this.m_Location.m_X - range)) &&
				   (p.m_X <= (this.m_Location.m_X + range)) &&
				   (p.m_Y >= (this.m_Location.m_Y - range)) &&
				   (p.m_Y <= (this.m_Location.m_Y + range));
		}

		public bool InRange(Point3D p, int range)
		{
			return (p.m_X >= (this.m_Location.m_X - range)) &&
				   (p.m_X <= (this.m_Location.m_X + range)) &&
				   (p.m_Y >= (this.m_Location.m_Y - range)) &&
				   (p.m_Y <= (this.m_Location.m_Y + range));
		}

		public bool InRange(IPoint2D p, int range)
		{
			return (p.X >= (this.m_Location.m_X - range)) &&
				   (p.X <= (this.m_Location.m_X + range)) &&
				   (p.Y >= (this.m_Location.m_Y - range)) &&
				   (p.Y <= (this.m_Location.m_Y + range));
		}

		#endregion

		public void InitStats(int str, int dex, int intel)
		{
			this.m_Str = str;
			this.m_Dex = dex;
			this.m_Int = intel;

			this.Hits = this.HitsMax;
			this.Stam = this.StamMax;
			this.Mana = this.ManaMax;

			this.Delta(MobileDelta.Stat | MobileDelta.Hits | MobileDelta.Stam | MobileDelta.Mana);
		}

		public virtual void DisplayPaperdollTo(Mobile to)
		{
			EventSink.InvokePaperdollRequest(new PaperdollRequestEventArgs(to, this));
		}

		private static bool m_DisableDismountInWarmode;

		public static bool DisableDismountInWarmode
		{
			get
			{
				return m_DisableDismountInWarmode;
			}
			set
			{
				m_DisableDismountInWarmode = value;
			}
		}
		
		#region OnDoubleClick[..]

		/// <summary>
		/// Overridable. Event invoked when the Mobile is double clicked. By default, this method can either dismount or open the paperdoll.
		/// <seealso cref="CanPaperdollBeOpenedBy" />
		/// <seealso cref="DisplayPaperdollTo" />
		/// </summary>
		public virtual void OnDoubleClick(Mobile from)
		{
			if (this == from && (!m_DisableDismountInWarmode || !this.m_Warmode))
			{
				IMount mount = this.Mount;

				if (mount != null)
				{
					mount.Rider = null;
					return;
				}
			}

			if (this.CanPaperdollBeOpenedBy(from))
				this.DisplayPaperdollTo(from);
		}

		/// <summary>
		/// Overridable. Virtual event invoked when the Mobile is double clicked by someone who is over 18 tiles away.
		/// <seealso cref="OnDoubleClick" />
		/// </summary>
		public virtual void OnDoubleClickOutOfRange(Mobile from)
		{
		}

		/// <summary>
		/// Overridable. Virtual event invoked when the Mobile is double clicked by someone who can no longer see the Mobile. This may happen, for example, using 'Last Object' after the Mobile has hidden.
		/// <seealso cref="OnDoubleClick" />
		/// </summary>
		public virtual void OnDoubleClickCantSee(Mobile from)
		{
		}

		/// <summary>
		/// Overridable. Event invoked when the Mobile is double clicked by someone who is not alive. Similar to <see cref="OnDoubleClick" />, this method will show the paperdoll. It does not, however, provide any dismount functionality.
		/// <seealso cref="OnDoubleClick" />
		/// </summary>
		public virtual void OnDoubleClickDead(Mobile from)
		{
			if (this.CanPaperdollBeOpenedBy(from))
				this.DisplayPaperdollTo(from);
		}

		#endregion

		/// <summary>
		/// Overridable. Event invoked when the Mobile requests to open his own paperdoll via the 'Open Paperdoll' macro.
		/// </summary>
		public virtual void OnPaperdollRequest()
		{
			if (this.CanPaperdollBeOpenedBy(this))
				this.DisplayPaperdollTo(this);
		}

		private static int m_BodyWeight = 14;

		public static int BodyWeight
		{
			get
			{
				return m_BodyWeight;
			}
			set
			{
				m_BodyWeight = value;
			}
		}

		/// <summary>
		/// Overridable. Event invoked when <paramref name="from" /> wants to see this Mobile's stats.
		/// </summary>
		/// <param name="from"></param>
		public virtual void OnStatsQuery(Mobile from)
		{
			if (from.Map == this.Map && Utility.InUpdateRange(this, from) && from.CanSee(this))
				from.Send(new MobileStatus(from, this, this.m_NetState));

			if (from == this)
				this.Send(new StatLockInfo(this));

			IParty ip = this.m_Party as IParty;

			if (ip != null)
				ip.OnStatsQuery(from, this);
		}

		/// <summary>
		/// Overridable. Event invoked when <paramref name="from" /> wants to see this Mobile's skills.
		/// </summary>
		public virtual void OnSkillsQuery(Mobile from)
		{
			if (from == this)
				this.Send(new SkillUpdate(this.m_Skills));
		}

		/// <summary>
		/// Overridable. Virtual event invoked when <see cref="Region" /> changes.
		/// </summary>
		public virtual void OnRegionChange(Region Old, Region New)
		{
		}

		private Item m_MountItem;

		[CommandProperty(AccessLevel.Decorator)]
		public IMount Mount
		{
			get
			{
				IMountItem mountItem = null;

				if (this.m_MountItem != null && !this.m_MountItem.Deleted && this.m_MountItem.Parent == this)
					mountItem = (IMountItem)this.m_MountItem;

				if (mountItem == null)
					this.m_MountItem = (mountItem = (this.FindItemOnLayer(Layer.Mount) as IMountItem)) as Item;

				return mountItem == null ? null : mountItem.Mount;
			}
		}

		[CommandProperty(AccessLevel.Decorator)]
		public bool Mounted
		{
			get
			{
				return (this.Mount != null);
			}
		}

		private QuestArrow m_QuestArrow;

		public QuestArrow QuestArrow
		{
			get
			{
				return this.m_QuestArrow;
			}
			set
			{
				if (this.m_QuestArrow != value)
				{
					if (this.m_QuestArrow != null)
						this.m_QuestArrow.Stop();

					this.m_QuestArrow = value;
				}
			}
		}

		private static readonly string[] m_GuildTypes = new string[]
		{
			"",
			" (Chaos)",
			" (Order)"
		};

		public virtual bool CanTarget
		{
			get
			{
				return true;
			}
		}
		public virtual bool ClickTitle
		{
			get
			{
				return true;
			}
		}

		public virtual bool PropertyTitle
		{
			get
			{
				return m_OldPropertyTitles ? this.ClickTitle : true;
			}
		}

		private static bool m_DisableHiddenSelfClick = true;
		private static bool m_AsciiClickMessage = true;
		private static bool m_GuildClickMessage = true;
		private static bool m_OldPropertyTitles;

		public static bool DisableHiddenSelfClick
		{
			get
			{
				return m_DisableHiddenSelfClick;
			}
			set
			{
				m_DisableHiddenSelfClick = value;
			}
		}
		public static bool AsciiClickMessage
		{
			get
			{
				return m_AsciiClickMessage;
			}
			set
			{
				m_AsciiClickMessage = value;
			}
		}
		public static bool GuildClickMessage
		{
			get
			{
				return m_GuildClickMessage;
			}
			set
			{
				m_GuildClickMessage = value;
			}
		}
		public static bool OldPropertyTitles
		{
			get
			{
				return m_OldPropertyTitles;
			}
			set
			{
				m_OldPropertyTitles = value;
			}
		}

		public virtual bool ShowFameTitle
		{
			get
			{
				return true;
			}
		}//(m_Player || m_Body.IsHuman) && m_Fame >= 10000; } 

		/// <summary>
		/// Overridable. Event invoked when the Mobile is single clicked.
		/// </summary>
		public virtual void OnSingleClick(Mobile from)
		{
			if (this.m_Deleted)
				return;
			else if (this.IsPlayer() && DisableHiddenSelfClick && this.Hidden && from == this)
				return;

			if (m_GuildClickMessage)
			{
				BaseGuild guild = this.m_Guild;

				if (guild != null && (this.m_DisplayGuildTitle || (this.m_Player && guild.Type != GuildType.Regular)))
				{
					string title = this.GuildTitle;
					string type;

					if (title == null)
						title = "";
					else
						title = title.Trim();

					if (guild.Type >= 0 && (int)guild.Type < m_GuildTypes.Length)
						type = m_GuildTypes[(int)guild.Type];
					else
						type = "";

					string text = String.Format(title.Length <= 0 ? "[{1}]{2}" : "[{0}, {1}]{2}", title, guild.Abbreviation, type);

					this.PrivateOverheadMessage(MessageType.Regular, this.SpeechHue, true, text, from.NetState);
				}
			}

			int hue;

			if (this.m_NameHue != -1)
				hue = this.m_NameHue;
			else if (this.IsStaff())
				hue = 11;
			else
				hue = Notoriety.GetHue(Notoriety.Compute(from, this));

			string name = this.Name;

			if (name == null)
				name = String.Empty;

			string prefix = "";

			if (this.ShowFameTitle && (this.m_Player || this.m_Body.IsHuman) && this.m_Fame >= 10000)
				prefix = (this.m_Female ? "Lady" : "Lord");

			string suffix = "";

			if (this.ClickTitle && this.Title != null && this.Title.Length > 0)
				suffix = this.Title;

			suffix = this.ApplyNameSuffix(suffix);

			string val;

			if (prefix.Length > 0 && suffix.Length > 0)
				val = String.Concat(prefix, " ", name, " ", suffix);
			else if (prefix.Length > 0)
				val = String.Concat(prefix, " ", name);
			else if (suffix.Length > 0)
				val = String.Concat(name, " ", suffix);
			else
				val = name;

			this.PrivateOverheadMessage(MessageType.Label, hue, m_AsciiClickMessage, val, from.NetState);
		}

		public bool CheckSkill(SkillName skill, double minSkill, double maxSkill)
		{
			if (m_SkillCheckLocationHandler == null)
				return false;
			else
				return m_SkillCheckLocationHandler(this, skill, minSkill, maxSkill);
		}

		public bool CheckSkill(SkillName skill, double chance)
		{
			if (m_SkillCheckDirectLocationHandler == null)
				return false;
			else
				return m_SkillCheckDirectLocationHandler(this, skill, chance);
		}

		public bool CheckTargetSkill(SkillName skill, object target, double minSkill, double maxSkill)
		{
			if (m_SkillCheckTargetHandler == null)
				return false;
			else
				return m_SkillCheckTargetHandler(this, skill, target, minSkill, maxSkill);
		}

		public bool CheckTargetSkill(SkillName skill, object target, double chance)
		{
			if (m_SkillCheckDirectTargetHandler == null)
				return false;
			else
				return m_SkillCheckDirectTargetHandler(this, skill, target, chance);
		}

		public virtual void DisruptiveAction()
		{
			if (this.Meditating)
			{
				this.Meditating = false;
				this.SendLocalizedMessage(500134); // You stop meditating.
			}
		}

		#region Armor
		public Item ShieldArmor
		{
			get
			{
				return this.FindItemOnLayer(Layer.TwoHanded) as Item;
			}
		}

		public Item NeckArmor
		{
			get
			{
				return this.FindItemOnLayer(Layer.Neck) as Item;
			}
		}

		public Item HandArmor
		{
			get
			{
				return this.FindItemOnLayer(Layer.Gloves) as Item;
			}
		}

		public Item HeadArmor
		{
			get
			{
				return this.FindItemOnLayer(Layer.Helm) as Item;
			}
		}

		public Item ArmsArmor
		{
			get
			{
				return this.FindItemOnLayer(Layer.Arms) as Item;
			}
		}

		public Item LegsArmor
		{
			get
			{
				Item ar = this.FindItemOnLayer(Layer.InnerLegs) as Item;

				if (ar == null)
					ar = this.FindItemOnLayer(Layer.Pants) as Item;

				return ar;
			}
		}

		public Item ChestArmor
		{
			get
			{
				Item ar = this.FindItemOnLayer(Layer.InnerTorso) as Item;

				if (ar == null)
					ar = this.FindItemOnLayer(Layer.Shirt) as Item;

				return ar;
			}
		}

		public Item Talisman
		{
			get
			{
				return this.FindItemOnLayer(Layer.Talisman) as Item;
			}
		}
		#endregion

		/// <summary>
		/// Gets or sets the maximum attainable value for <see cref="RawStr" />, <see cref="RawDex" />, and <see cref="RawInt" />.
		/// </summary>
		[CommandProperty(AccessLevel.GameMaster)]
		public int StatCap
		{
			get
			{
				return this.m_StatCap;
			}
			set
			{
				if (this.m_StatCap != value)
				{
					this.m_StatCap = value;

					this.Delta(MobileDelta.StatCap);
				}
			}
		}

		[CommandProperty(AccessLevel.GameMaster)]
		public bool Meditating
		{
			get
			{
				return this.m_Meditating;
			}
			set
			{
				this.m_Meditating = value;
			}
		}

		[CommandProperty(AccessLevel.Decorator)]
		public bool CanSwim
		{
			get
			{
				return this.m_CanSwim;
			}
			set
			{
				this.m_CanSwim = value;
			}
		}

		[CommandProperty(AccessLevel.Decorator)]
		public bool CantWalk
		{
			get
			{
				return this.m_CantWalk;
			}
			set
			{
				this.m_CantWalk = value;
			}
		}

		[CommandProperty(AccessLevel.GameMaster)]
		public bool CanHearGhosts
		{
			get
			{
				return this.m_CanHearGhosts || this.AccessLevel >= AccessLevel.Counselor;
			}
			set
			{
				this.m_CanHearGhosts = value;
			}
		}

		[CommandProperty(AccessLevel.GameMaster)]
		public int RawStatTotal
		{
			get
			{
				return this.RawStr + this.RawDex + this.RawInt;
			}
		}

		public DateTime NextSpellTime
		{
			get
			{
				return this.m_NextSpellTime;
			}
			set
			{
				this.m_NextSpellTime = value;
			}
		}

		/// <summary>
		/// Overridable. Virtual event invoked when the sector this Mobile is in gets <see cref="Sector.Activate">activated</see>.
		/// </summary>
		public virtual void OnSectorActivate()
		{
		}

		/// <summary>
		/// Overridable. Virtual event invoked when the sector this Mobile is in gets <see cref="Sector.Deactivate">deactivated</see>.
		/// </summary>
		public virtual void OnSectorDeactivate()
		{
		}
	}
}