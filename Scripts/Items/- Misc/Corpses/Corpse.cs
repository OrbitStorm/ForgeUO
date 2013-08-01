using System;
using System.Collections.Generic;
using Server.ContextMenus;
using Server.Engines.PartySystem;
using Server.Engines.Quests;
using Server.Engines.Quests.Doom;
using Server.Engines.Quests.Haven;
using Server.Engines.XmlSpawner2;
using Server.Guilds;
using Server.Misc;
using Server.Mobiles;
using Server.Network;

namespace Server.Items
{
    public interface IDevourer
    {
        bool Devour(Corpse corpse);
    }

    [Flags]
    public enum CorpseFlag
    {
        None = 0x00000000,

        /// <summary>
        /// Has this corpse been carved?
        /// </summary>
        Carved = 0x00000001,

        /// <summary>
        /// If true, this corpse will not turn into bones
        /// </summary>
        NoBones = 0x00000002,

        /// <summary>
        /// If true, the corpse has turned into bones
        /// </summary>
        IsBones = 0x00000004,

        /// <summary>
        /// Has this corpse yet been visited by a taxidermist?
        /// </summary>
        VisitedByTaxidermist = 0x00000008,

        /// <summary>
        /// Has this corpse yet been used to channel spiritual energy? (AOS Spirit Speak)
        /// </summary>
        Channeled = 0x00000010,

        /// <summary>
        /// Was the owner criminal when he died?
        /// </summary>
        Criminal = 0x00000020,

        /// <summary>
        /// Has this corpse been animated?
        /// </summary>
        Animated = 0x00000040,

        /// <summary>
        /// Has this corpse been self looted?
        /// </summary>
        SelfLooted = 0x00000080,
    }

    public class Corpse : Container, ICarvable
    {
        private Mobile m_Owner;				// Whos corpse is this?
        private Mobile m_Killer;				// Who killed the owner?
        private CorpseFlag m_Flags;				// @see CorpseFlag

        private List<Mobile> m_Looters;				// Who's looted this corpse?
        private List<Item> m_EquipItems;			// List of dropped equipment when the owner died. Ingame, these items display /on/ the corpse, not just inside
        private List<Item> m_RestoreEquip;			// List of items equipped when the owner died. Includes insured and blessed items.
        private List<Mobile> m_Aggressors;			// Anyone from this list will be able to loot this corpse; we attacked them, or they attacked us when we were freely attackable

        private string m_CorpseName;			// Value of the CorpseNameAttribute attached to the owner when he died -or- null if the owner had no CorpseNameAttribute; use "the remains of ~name~"
        private IDevourer m_Devourer;				// The creature that devoured this corpse

        // For notoriety:
        private AccessLevel m_AccessLevel;			// Which AccessLevel the owner had when he died
        private readonly Guild m_Guild;				// Which Guild the owner was in when he died
        private int m_Kills;				// How many kills the owner had when he died

        private DateTime m_TimeOfDeath;			// What time was this corpse created?

        private readonly HairInfo m_Hair;					// This contains the hair of the owner
        private readonly FacialHairInfo m_FacialHair;			// This contains the facial hair of the owner

        // For Forensics Evaluation
        public string m_Forensicist;			// Name of the first PlayerMobile who used Forensic Evaluation on the corpse

        public static readonly TimeSpan MonsterLootRightSacrifice = TimeSpan.FromMinutes(2.0);

        public static readonly TimeSpan InstancedCorpseTime = TimeSpan.FromMinutes(3.0);

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool InstancedCorpse
        {
            get
            {
                if (!Core.SE)
                    return false;

                return (DateTime.Now < (this.m_TimeOfDeath + InstancedCorpseTime));
            }
        }

        private Dictionary<Item, InstancedItemInfo> m_InstancedItems;

        private class InstancedItemInfo
        {
            private readonly Mobile m_Mobile;
            private readonly Item m_Item;

            private bool m_Perpetual;   //Needed for Rummaged stuff.  CONTRARY to the Patchlog, cause a later FoF contradicts it.  Verify on OSI.
            public bool Perpetual
            {
                get
                {
                    return this.m_Perpetual;
                }
                set
                {
                    this.m_Perpetual = value;
                }
            }

            public InstancedItemInfo(Item i, Mobile m)
            {
                this.m_Item = i;
                this.m_Mobile = m;
            }

            public bool IsOwner(Mobile m)
            {
                if (this.m_Item.LootType == LootType.Cursed)   //Cursed Items are part of everyone's instanced corpse... (?)
                    return true;

                if (m == null)
                    return false;   //sanity

                if (this.m_Mobile == m)
                    return true;

                Party myParty = Party.Get(this.m_Mobile);

                return (myParty != null && myParty == Party.Get(m));
            }
        }

        public override bool IsChildVisibleTo(Mobile m, Item child)
        {
            if (!m.Player || m.IsStaff())   //Staff and creatures not subject to instancing.
                return true;

            if (this.m_InstancedItems != null)
            {
                InstancedItemInfo info;

                if (this.m_InstancedItems.TryGetValue(child, out info) && (this.InstancedCorpse || info.Perpetual))
                {
                    return info.IsOwner(m);   //IsOwner checks Party stuff.
                }
            }

            return true;
        }

        private void AssignInstancedLoot()
        {
            if (this.m_Aggressors.Count == 0 || this.Items.Count == 0)
                return;

            if (this.m_InstancedItems == null)
                this.m_InstancedItems = new Dictionary<Item, InstancedItemInfo>();

            List<Item> m_Stackables = new List<Item>();
            List<Item> m_Unstackables = new List<Item>();

            for (int i = 0; i < this.Items.Count; i++)
            {
                Item item = this.Items[i];

                if (item.LootType != LootType.Cursed) //Don't have curesd items take up someone's item spot.. (?)
                {
                    if (item.Stackable)
                        m_Stackables.Add(item);
                    else
                        m_Unstackables.Add(item);
                }
            }

            List<Mobile> attackers = new List<Mobile>(this.m_Aggressors);

            for (int i = 1; i < attackers.Count - 1; i++)  //randomize
            {
                int rand = Utility.Random(i + 1);

                Mobile temp = attackers[rand];
                attackers[rand] = attackers[i];
                attackers[i] = temp;
            }

            //stackables first, for the remaining stackables, have those be randomly added after

            for (int i = 0; i < m_Stackables.Count; i++)
            {
                Item item = m_Stackables[i];

                if (item.Amount >= attackers.Count)
                {
                    int amountPerAttacker = (item.Amount / attackers.Count);
                    int remainder = (item.Amount % attackers.Count);

                    for (int j = 0; j < ((remainder == 0) ? attackers.Count - 1 : attackers.Count); j++)
                    {
                        Item splitItem = Mobile.LiftItemDupe(item, item.Amount - amountPerAttacker);  //LiftItemDupe automagically adds it as a child item to the corpse

                        this.m_InstancedItems.Add(splitItem, new InstancedItemInfo(splitItem, attackers[j]));
                        //What happens to the remaining portion?  TEMP FOR NOW UNTIL OSI VERIFICATION:  Treat as Single Item.
                    }

                    if (remainder == 0)
                    {
                        this.m_InstancedItems.Add(item, new InstancedItemInfo(item, attackers[attackers.Count - 1]));
                        //Add in the original item (which has an equal amount as the others) to the instance for the last attacker, cause it wasn't added above.
                    }
                    else
                    {
                        m_Unstackables.Add(item);
                    }
                }
                else
                {
                    //What happens in this case?  TEMP FOR NOW UNTIL OSI VERIFICATION:  Treat as Single Item.
                    m_Unstackables.Add(item);
                }
            }

            for (int i = 0; i < m_Unstackables.Count; i++)
            {
                Mobile m = attackers[i % attackers.Count];
                Item item = m_Unstackables[i];

                this.m_InstancedItems.Add(item, new InstancedItemInfo(item, m));
            }
        }

        public void AddCarvedItem(Item carved, Mobile carver)
        {
            this.DropItem(carved);

            if (this.InstancedCorpse)
            {
                if (this.m_InstancedItems == null)
                    this.m_InstancedItems = new Dictionary<Item, InstancedItemInfo>();

                this.m_InstancedItems.Add(carved, new InstancedItemInfo(carved, carver));
            }
        }

        public override bool IsDecoContainer
        {
            get
            {
                return false;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime TimeOfDeath
        {
            get
            {
                return this.m_TimeOfDeath;
            }
            set
            {
                this.m_TimeOfDeath = value;
            }
        }

        public override bool DisplayWeight
        {
            get
            {
                return false;
            }
        }

        public HairInfo Hair
        {
            get
            {
                return this.m_Hair;
            }
        }
        public FacialHairInfo FacialHair
        {
            get
            {
                return this.m_FacialHair;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsBones
        {
            get
            {
                return this.GetFlag(CorpseFlag.IsBones);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Devoured
        {
            get
            {
                return (this.m_Devourer != null);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Carved
        {
            get
            {
                return this.GetFlag(CorpseFlag.Carved);
            }
            set
            {
                this.SetFlag(CorpseFlag.Carved, value);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool VisitedByTaxidermist
        {
            get
            {
                return this.GetFlag(CorpseFlag.VisitedByTaxidermist);
            }
            set
            {
                this.SetFlag(CorpseFlag.VisitedByTaxidermist, value);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Channeled
        {
            get
            {
                return this.GetFlag(CorpseFlag.Channeled);
            }
            set
            {
                this.SetFlag(CorpseFlag.Channeled, value);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Animated
        {
            get
            {
                return this.GetFlag(CorpseFlag.Animated);
            }
            set
            {
                this.SetFlag(CorpseFlag.Animated, value);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool SelfLooted
        {
            get
            {
                return this.GetFlag(CorpseFlag.SelfLooted);
            }
            set
            {
                this.SetFlag(CorpseFlag.SelfLooted, value);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public AccessLevel AccessLevel
        {
            get
            {
                return this.m_AccessLevel;
            }
        }

        public List<Mobile> Aggressors
        {
            get
            {
                return this.m_Aggressors;
            }
        }

        public List<Mobile> Looters
        {
            get
            {
                return this.m_Looters;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Killer
        {
            get
            {
                return this.m_Killer;
            }
        }

        public List<Item> EquipItems
        {
            get
            {
                return this.m_EquipItems;
            }
        }

        public List<Item> RestoreEquip
        {
            get
            {
                return this.m_RestoreEquip;
            }
            set
            {
                this.m_RestoreEquip = value;
            }
        }

        public Guild Guild
        {
            get
            {
                return this.m_Guild;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Kills
        {
            get
            {
                return this.m_Kills;
            }
            set
            {
                this.m_Kills = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Criminal
        {
            get
            {
                return this.GetFlag(CorpseFlag.Criminal);
            }
            set
            {
                this.SetFlag(CorpseFlag.Criminal, value);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Owner
        {
            get
            {
                return this.m_Owner;
            }
        }

        public void TurnToBones()
        {
            if (this.Deleted)
                return;

            this.ProcessDelta();
            this.SendRemovePacket();
            this.ItemID = Utility.Random(0xECA, 9); // bone graphic
            this.Hue = 0;
            this.ProcessDelta();

            this.SetFlag(CorpseFlag.NoBones, true);
            this.SetFlag(CorpseFlag.IsBones, true);

            this.BeginDecay(m_BoneDecayTime);
        }

        private static readonly TimeSpan m_DefaultDecayTime = TimeSpan.FromMinutes(7.0);
        private static readonly TimeSpan m_BoneDecayTime = TimeSpan.FromMinutes(7.0);

        private Timer m_DecayTimer;
        private DateTime m_DecayTime;

        public void BeginDecay(TimeSpan delay)
        {
            if (this.m_DecayTimer != null)
                this.m_DecayTimer.Stop();

            this.m_DecayTime = DateTime.Now + delay;

            this.m_DecayTimer = new InternalTimer(this, delay);
            this.m_DecayTimer.Start();
        }

        public override void OnAfterDelete()
        {
            if (this.m_DecayTimer != null)
                this.m_DecayTimer.Stop();

            this.m_DecayTimer = null;
        }

        private class InternalTimer : Timer
        {
            private readonly Corpse m_Corpse;

            public InternalTimer(Corpse c, TimeSpan delay)
                : base(delay)
            {
                this.m_Corpse = c;
                this.Priority = TimerPriority.FiveSeconds;
            }

            protected override void OnTick()
            {
                if (!this.m_Corpse.GetFlag(CorpseFlag.NoBones))
                    this.m_Corpse.TurnToBones();
                else
                    this.m_Corpse.Delete();
            }
        }

        public static string GetCorpseName(Mobile m)
        {
            XmlData x = (XmlData)XmlAttach.FindAttachment(m, typeof(XmlData), "CorpseName");
  			
            if (x != null)
                return x.Data;

            if (m is BaseCreature)
            {
                BaseCreature bc = (BaseCreature)m;

                if (bc.CorpseNameOverride != null)
                    return bc.CorpseNameOverride;
            }

            Type t = m.GetType();

            object[] attrs = t.GetCustomAttributes(typeof(CorpseNameAttribute), true);

            if (attrs != null && attrs.Length > 0)
            {
                CorpseNameAttribute attr = attrs[0] as CorpseNameAttribute;

                if (attr != null)
                    return attr.Name;
            }

            return null;
        }

        public static void Initialize()
        {
            Mobile.CreateCorpseHandler += new CreateCorpseHandler(Mobile_CreateCorpseHandler);
        }

        public static Container Mobile_CreateCorpseHandler(Mobile owner, HairInfo hair, FacialHairInfo facialhair, List<Item> initialContent, List<Item> equipItems)
        {
            bool shouldFillCorpse = true;

            //if ( owner is BaseCreature )
            //	shouldFillCorpse = !((BaseCreature)owner).IsBonded;

            Corpse c;
            if (owner is MilitiaFighter)
                c = new MilitiaFighterCorpse(owner, hair, facialhair, shouldFillCorpse ? equipItems : new List<Item>());
            else
                c = new Corpse(owner, hair, facialhair, shouldFillCorpse ? equipItems : new List<Item>());

            owner.Corpse = c;

            if (shouldFillCorpse)
            {
                for (int i = 0; i < initialContent.Count; ++i)
                {
                    Item item = initialContent[i];

                    if (Core.AOS && owner.Player && item.Parent == owner.Backpack)
                        c.AddItem(item);
                    else
                        c.DropItem(item);

                    if (owner.Player && Core.AOS)
                        c.SetRestoreInfo(item, item.Location);
                }

                if (!owner.Player)
                {
                    c.AssignInstancedLoot();
                }
                else if (Core.AOS)
                {
                    PlayerMobile pm = owner as PlayerMobile;

                    if (pm != null)
                        c.RestoreEquip = pm.EquipSnapshot;
                }
            }
            else
            {
                c.Carved = true; // TODO: Is it needed?
            }

            Point3D loc = owner.Location;
            Map map = owner.Map;

            if (map == null || map == Map.Internal)
            {
                loc = owner.LogoutLocation;
                map = owner.LogoutMap;
            }

            c.MoveToWorld(loc, map);

            return c;
        }

        public override bool IsPublicContainer
        {
            get
            {
                return true;
            }
        }

        public Corpse(Mobile owner, List<Item> equipItems)
            : this(owner, null, null, equipItems)
        {
        }

        public Corpse(Mobile owner, HairInfo hair, FacialHairInfo facialhair, List<Item> equipItems)
            : base(0x2006)
        {
            // To supress console warnings, stackable must be true
            this.Stackable = true;
            this.Amount = owner.Body; // protocol defines that for itemid 0x2006, amount=body
            this.Stackable = false;

            this.Movable = false;
            this.Hue = owner.Hue;
            this.Direction = owner.Direction;
            this.Name = owner.Name;

            this.m_Owner = owner;

            this.m_CorpseName = GetCorpseName(owner);

            this.m_TimeOfDeath = DateTime.Now;

            this.m_AccessLevel = owner.AccessLevel;
            this.m_Guild = owner.Guild as Guild;
            this.m_Kills = owner.Kills;
            this.SetFlag(CorpseFlag.Criminal, owner.Criminal);

            this.m_Hair = hair;
            this.m_FacialHair = facialhair;

            // This corpse does not turn to bones if: the owner is not a player
            this.SetFlag(CorpseFlag.NoBones, !owner.Player);

            this.m_Looters = new List<Mobile>();
            this.m_EquipItems = equipItems;

            this.m_Aggressors = new List<Mobile>(owner.Aggressors.Count + owner.Aggressed.Count);
            //bool addToAggressors = !( owner is BaseCreature );

            bool isBaseCreature = (owner is BaseCreature);

            TimeSpan lastTime = TimeSpan.MaxValue;

            for (int i = 0; i < owner.Aggressors.Count; ++i)
            {
                AggressorInfo info = owner.Aggressors[i];

                if ((DateTime.Now - info.LastCombatTime) < lastTime)
                {
                    this.m_Killer = info.Attacker;
                    lastTime = (DateTime.Now - info.LastCombatTime);
                }

                if (!isBaseCreature && !info.CriminalAggression)
                    this.m_Aggressors.Add(info.Attacker);
            }

            for (int i = 0; i < owner.Aggressed.Count; ++i)
            {
                AggressorInfo info = owner.Aggressed[i];

                if ((DateTime.Now - info.LastCombatTime) < lastTime)
                {
                    this.m_Killer = info.Defender;
                    lastTime = (DateTime.Now - info.LastCombatTime);
                }

                if (!isBaseCreature)
                    this.m_Aggressors.Add(info.Defender);
            }

            if (isBaseCreature)
            {
                BaseCreature bc = (BaseCreature)owner;

                Mobile master = bc.GetMaster();
                if (master != null)
                    this.m_Aggressors.Add(master);

                List<DamageStore> rights = BaseCreature.GetLootingRights(bc.DamageEntries, bc.HitsMax);
                for (int i = 0; i < rights.Count; ++i)
                {
                    DamageStore ds = rights[i];

                    if (ds.m_HasRight)
                        this.m_Aggressors.Add(ds.m_Mobile);
                }
            }

            this.BeginDecay(m_DefaultDecayTime);

            this.DevourCorpse();
        }

        public Corpse(Serial serial)
            : base(serial)
        {
        }

        protected bool GetFlag(CorpseFlag flag)
        {
            return ((this.m_Flags & flag) != 0);
        }

        protected void SetFlag(CorpseFlag flag, bool on)
        {
            this.m_Flags = (on ? this.m_Flags | flag : this.m_Flags & ~flag);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)12); // version

            if (this.m_RestoreEquip == null)
            {
                writer.Write(false);
            }
            else
            {
                writer.Write(true);
                writer.Write(this.m_RestoreEquip);
            }

            writer.Write((int)this.m_Flags);

            writer.WriteDeltaTime(this.m_TimeOfDeath);

            List<KeyValuePair<Item, Point3D>> list = (this.m_RestoreTable == null ? null : new List<KeyValuePair<Item, Point3D>>(this.m_RestoreTable));
            int count = (list == null ? 0 : list.Count);

            writer.Write(count);

            for (int i = 0; i < count; ++i)
            {
                KeyValuePair<Item, Point3D> kvp = list[i];
                Item item = kvp.Key;
                Point3D loc = kvp.Value;

                writer.Write(item);

                if (item.Location == loc)
                {
                    writer.Write(false);
                }
                else
                {
                    writer.Write(true);
                    writer.Write(loc);
                }
            }

            writer.Write(this.m_DecayTimer != null);

            if (this.m_DecayTimer != null)
                writer.WriteDeltaTime(this.m_DecayTime);

            writer.Write(this.m_Looters);
            writer.Write(this.m_Killer);

            writer.Write(this.m_Aggressors);

            writer.Write(this.m_Owner);

            writer.Write((string)this.m_CorpseName);

            writer.Write((int)this.m_AccessLevel);
            writer.Write((Guild)this.m_Guild);
            writer.Write((int)this.m_Kills);

            writer.Write(this.m_EquipItems);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch ( version )
            {
                case 12:
                    {
                        if (reader.ReadBool())
                            this.m_RestoreEquip = reader.ReadStrongItemList();

                        goto case 11;
                    }
                case 11:
                    {
                        // Version 11, we move all bools to a CorpseFlag
                        this.m_Flags = (CorpseFlag)reader.ReadInt();

                        this.m_TimeOfDeath = reader.ReadDeltaTime();

                        int count = reader.ReadInt();

                        for (int i = 0; i < count; ++i)
                        {
                            Item item = reader.ReadItem();

                            if (reader.ReadBool())
                                this.SetRestoreInfo(item, reader.ReadPoint3D());
                            else if (item != null)
                                this.SetRestoreInfo(item, item.Location);
                        }

                        if (reader.ReadBool())
                            this.BeginDecay(reader.ReadDeltaTime() - DateTime.Now);

                        this.m_Looters = reader.ReadStrongMobileList();
                        this.m_Killer = reader.ReadMobile();

                        this.m_Aggressors = reader.ReadStrongMobileList();
                        this.m_Owner = reader.ReadMobile();

                        this.m_CorpseName = reader.ReadString();

                        this.m_AccessLevel = (AccessLevel)reader.ReadInt();
                        reader.ReadInt(); // guild reserve
                        this.m_Kills = reader.ReadInt();

                        this.m_EquipItems = reader.ReadStrongItemList();
                        break;
                    }
                case 10:
                    {
                        this.m_TimeOfDeath = reader.ReadDeltaTime();

                        goto case 9;
                    }
                case 9:
                    {
                        int count = reader.ReadInt();

                        for (int i = 0; i < count; ++i)
                        {
                            Item item = reader.ReadItem();

                            if (reader.ReadBool())
                                this.SetRestoreInfo(item, reader.ReadPoint3D());
                            else if (item != null)
                                this.SetRestoreInfo(item, item.Location);
                        }

                        goto case 8;
                    }
                case 8:
                    {
                        this.SetFlag(CorpseFlag.VisitedByTaxidermist, reader.ReadBool());

                        goto case 7;
                    }
                case 7:
                    {
                        if (reader.ReadBool())
                            this.BeginDecay(reader.ReadDeltaTime() - DateTime.Now);

                        goto case 6;
                    }
                case 6:
                    {
                        this.m_Looters = reader.ReadStrongMobileList();
                        this.m_Killer = reader.ReadMobile();

                        goto case 5;
                    }
                case 5:
                    {
                        this.SetFlag(CorpseFlag.Carved, reader.ReadBool());

                        goto case 4;
                    }
                case 4:
                    {
                        this.m_Aggressors = reader.ReadStrongMobileList();

                        goto case 3;
                    }
                case 3:
                    {
                        this.m_Owner = reader.ReadMobile();

                        goto case 2;
                    }
                case 2:
                    {
                        this.SetFlag(CorpseFlag.NoBones, reader.ReadBool());

                        goto case 1;
                    }
                case 1:
                    {
                        this.m_CorpseName = reader.ReadString();

                        goto case 0;
                    }
                case 0:
                    {
                        if (version < 10)
                            this.m_TimeOfDeath = DateTime.Now;

                        if (version < 7)
                            this.BeginDecay(m_DefaultDecayTime);

                        if (version < 6)
                            this.m_Looters = new List<Mobile>();

                        if (version < 4)
                            this.m_Aggressors = new List<Mobile>();

                        this.m_AccessLevel = (AccessLevel)reader.ReadInt();
                        reader.ReadInt(); // guild reserve
                        this.m_Kills = reader.ReadInt();
                        this.SetFlag(CorpseFlag.Criminal, reader.ReadBool());

                        this.m_EquipItems = reader.ReadStrongItemList();

                        break;
                    }
            }
        }

        public bool DevourCorpse()
        {
            if (this.Devoured || this.Deleted || this.m_Killer == null || this.m_Killer.Deleted || !this.m_Killer.Alive || !(this.m_Killer is IDevourer) || this.m_Owner == null || this.m_Owner.Deleted)
                return false;

            this.m_Devourer = (IDevourer)this.m_Killer; // Set the devourer the killer
            return this.m_Devourer.Devour(this); // Devour the corpse if it hasn't
        }

        public override void SendInfoTo(NetState state, bool sendOplPacket)
        {
            base.SendInfoTo(state, sendOplPacket);

            if (this.ItemID == 0x2006)
            {
                state.Send(new CorpseContent(state.Mobile, this));
                state.Send(new CorpseEquip(state.Mobile, this));
            }
        }

        public bool IsCriminalAction(Mobile from)
        {
            if (from == this.m_Owner || from.AccessLevel >= AccessLevel.GameMaster)
                return false;

            Party p = Party.Get(this.m_Owner);

            if (p != null && p.Contains(from))
            {
                PartyMemberInfo pmi = p[this.m_Owner];

                if (pmi != null && pmi.CanLoot)
                    return false;
            }

            return (NotorietyHandlers.CorpseNotoriety(from, this) == Notoriety.Innocent);
        }

        public override bool CheckItemUse(Mobile from, Item item)
        {
            if (!base.CheckItemUse(from, item))
                return false;

            if (item != this)
                return this.CanLoot(from, item);

            return true;
        }

        public override bool CheckLift(Mobile from, Item item, ref LRReason reject)
        {
            if (!base.CheckLift(from, item, ref reject))
                return false;

            return this.CanLoot(from, item);
        }

        public override void OnItemUsed(Mobile from, Item item)
        {
            base.OnItemUsed(from, item);

            if (item is Food)
                from.RevealingAction();

            if (item != this && this.IsCriminalAction(from))
                from.CriminalAction(true);

            if (!this.m_Looters.Contains(from))
                this.m_Looters.Add(from);

            if (this.m_InstancedItems != null && this.m_InstancedItems.ContainsKey(item))
                this.m_InstancedItems.Remove(item);
        }

        public override void OnItemLifted(Mobile from, Item item)
        {
            base.OnItemLifted(from, item);

            if (item != this && from != this.m_Owner)
                from.RevealingAction();

            if (item != this && this.IsCriminalAction(from))
                from.CriminalAction(true);

            if (!this.m_Looters.Contains(from))
                this.m_Looters.Add(from);

            if (this.m_InstancedItems != null && this.m_InstancedItems.ContainsKey(item))
                this.m_InstancedItems.Remove(item);
        }

        private class OpenCorpseEntry : ContextMenuEntry
        {
            public OpenCorpseEntry()
                : base(6215, 2)
            {
            }

            public override void OnClick()
            {
                Corpse corpse = this.Owner.Target as Corpse;

                if (corpse != null && this.Owner.From.CheckAlive())
                    corpse.Open(this.Owner.From, false);
            }
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            if (Core.AOS && this.m_Owner == from && from.Alive)
                list.Add(new OpenCorpseEntry());
        }

        private Dictionary<Item, Point3D> m_RestoreTable;

        public bool GetRestoreInfo(Item item, ref Point3D loc)
        {
            if (this.m_RestoreTable == null || item == null)
                return false;

            return this.m_RestoreTable.TryGetValue(item, out loc);
        }

        public void SetRestoreInfo(Item item, Point3D loc)
        {
            if (item == null)
                return;

            if (this.m_RestoreTable == null)
                this.m_RestoreTable = new Dictionary<Item, Point3D>();

            this.m_RestoreTable[item] = loc;
        }

        public void ClearRestoreInfo(Item item)
        {
            if (this.m_RestoreTable == null || item == null)
                return;

            this.m_RestoreTable.Remove(item);

            if (this.m_RestoreTable.Count == 0)
                this.m_RestoreTable = null;
        }

        public bool CanLoot(Mobile from, Item item)
        {
            if (!this.IsCriminalAction(from))
                return true;

            Map map = this.Map;

            if (map == null || (map.Rules & MapRules.HarmfulRestrictions) != 0)
                return false;

            return true;
        }

        public bool CheckLoot(Mobile from, Item item)
        {
            if (!this.CanLoot(from, item))
            {
                if (this.m_Owner == null || !this.m_Owner.Player)
                    from.SendLocalizedMessage(1005035); // You did not earn the right to loot this creature!
                else
                    from.SendLocalizedMessage(1010049); // You may not loot this corpse.

                return false;
            }
            else if (this.IsCriminalAction(from))
            {
                if (this.m_Owner == null || !this.m_Owner.Player)
                    from.SendLocalizedMessage(1005036); // Looting this monster corpse will be a criminal act!
                else
                    from.SendLocalizedMessage(1005038); // Looting this corpse will be a criminal act!
            }

            return true;
        }

        public virtual void Open(Mobile from, bool checkSelfLoot)
        {
            if (from.IsStaff() || from.InRange(this.GetWorldLocation(), 2))
            {
                #region Self Looting
                bool selfLoot = (checkSelfLoot && (from == m_Owner));

                if (selfLoot)
                {
                    List<Item> items = new List<Item>(this.Items);

                    bool gathered = false;

                    for (int k = 0; k < EquipItems.Count; ++k)
                    {
                        Item item2 = EquipItems[k];

                        if (!items.Contains(item2) && item2.IsChildOf(from.Backpack))
                        {
                            items.Add(item2);
                            gathered = true;
                        }
                    }

                    bool didntFit = false;

                    Container pack = from.Backpack;

                    bool checkRobe = true;

                    for (int i = 0; !didntFit && i < items.Count; ++i)
                    {
                        Item item = items[i];
                        Point3D loc = item.Location;

                        if ((item.Layer == Layer.Hair || item.Layer == Layer.FacialHair) || !item.Movable)
                            continue;

                        if (checkRobe)
                        {
                            DeathRobe robe = from.FindItemOnLayer(Layer.OuterTorso) as DeathRobe;

                            if (robe != null)
                            {
                                if (Core.SA)
                                {
                                    robe.Delete();
                                }
                                else
                                {
                                    Map map = from.Map;

                                    if (map != null && map != Map.Internal)
                                        robe.MoveToWorld(from.Location, map);
                                }
                            }
                        }

                        if (m_EquipItems.Contains(item) && from.EquipItem(item))
                        {
                            gathered = true;
                        }
                        else if (pack != null && pack.CheckHold(from, item, false, true))
                        {
                            item.Location = loc;
                            pack.AddItem(item);
                            gathered = true;
                        }
                        else
                        {
                            didntFit = true;
                        }
                    }

                    if (gathered && !didntFit)
                    {
                        SetFlag(CorpseFlag.Carved, true);

                        if (ItemID == 0x2006)
                        {
                            ProcessDelta();
                            SendRemovePacket();
                            ItemID = Utility.Random(0xECA, 9); // bone graphic
                            Hue = 0;
                            ProcessDelta();
                        }

                        from.PlaySound(0x3E3);
                        from.SendLocalizedMessage(1062471); // You quickly gather all of your belongings.
                        items.Clear();
                        m_EquipItems.Clear();
                        return;
                    }

                    if (gathered && didntFit)
                        from.SendLocalizedMessage(1062472); // You gather some of your belongings. The rest remain on the corpse.
                }
                #endregion

                if (!this.CheckLoot(from, null))
                    return;

                #region Quests
                PlayerMobile player = from as PlayerMobile;

                if (player != null)
                {
                    QuestSystem qs = player.Quest;

                    if (qs is UzeraanTurmoilQuest)
                    {
                        GetDaemonBoneObjective obj = qs.FindObjective(typeof(GetDaemonBoneObjective)) as GetDaemonBoneObjective;

                        if (obj != null && obj.CorpseWithBone == this && (!obj.Completed || UzeraanTurmoilQuest.HasLostDaemonBone(player)))
                        {
                            Item bone = new QuestDaemonBone();

                            if (player.PlaceInBackpack(bone))
                            {
                                obj.CorpseWithBone = null;
                                player.SendLocalizedMessage(1049341, "", 0x22); // You rummage through the bones and find a Daemon Bone!  You quickly place the item in your pack.

                                if (!obj.Completed)
                                    obj.Complete();
                            }
                            else
                            {
                                bone.Delete();
                                player.SendLocalizedMessage(1049342, "", 0x22); // Rummaging through the bones you find a Daemon Bone, but can't pick it up because your pack is too full.  Come back when you have more room in your pack.
                            }

                            return;
                        }
                    }
                    else if (qs is TheSummoningQuest)
                    {
                        VanquishDaemonObjective obj = qs.FindObjective(typeof(VanquishDaemonObjective)) as VanquishDaemonObjective;

                        if (obj != null && obj.Completed && obj.CorpseWithSkull == this)
                        {
                            GoldenSkull sk = new GoldenSkull();

                            if (player.PlaceInBackpack(sk))
                            {
                                obj.CorpseWithSkull = null;
                                player.SendLocalizedMessage(1050022); // For your valor in combating the devourer, you have been awarded a golden skull.
                                qs.Complete();
                            }
                            else
                            {
                                sk.Delete();
                                player.SendLocalizedMessage(1050023); // You find a golden skull, but your backpack is too full to carry it.
                            }
                        }
                    }
                }

                #endregion

                base.OnDoubleClick(from);
            }
            else
            {
                from.SendLocalizedMessage(500446); // That is too far away.
                return;
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            this.Open(from, Core.AOS);
        }

        public override bool CheckContentDisplay(Mobile from)
        {
            return false;
        }

        public override bool DisplaysContent
        {
            get
            {
                return false;
            }
        }

        public override void AddNameProperty(ObjectPropertyList list)
        {
            if (this.ItemID == 0x2006) // Corpse form
            {
                if (this.m_CorpseName != null)
                    list.Add(this.m_CorpseName);
                else
                    list.Add(1046414, this.Name); // the remains of ~1_NAME~
            }
            else // Bone form
            {
                list.Add(1046414, this.Name); // the remains of ~1_NAME~
            }
        }

        public override void OnAosSingleClick(Mobile from)
        {
            int hue = Notoriety.GetHue(NotorietyHandlers.CorpseNotoriety(from, this));
            ObjectPropertyList opl = this.PropertyList;

            if (opl.Header > 0)
                from.Send(new MessageLocalized(this.Serial, this.ItemID, MessageType.Label, hue, 3, opl.Header, this.Name, opl.HeaderArgs));
        }

        public override void OnSingleClick(Mobile from)
        {
            int hue = Notoriety.GetHue(NotorietyHandlers.CorpseNotoriety(from, this));

            if (this.ItemID == 0x2006) // Corpse form
            {
                if (this.m_CorpseName != null)
                    from.Send(new AsciiMessage(this.Serial, this.ItemID, MessageType.Label, hue, 3, "", this.m_CorpseName));
                else
                    from.Send(new MessageLocalized(this.Serial, this.ItemID, MessageType.Label, hue, 3, 1046414, "", this.Name));
            }
            else // Bone form
            {
                from.Send(new MessageLocalized(this.Serial, this.ItemID, MessageType.Label, hue, 3, 1046414, "", this.Name));
            }
        }

        public void Carve(Mobile from, Item item)
        {
            if (this.IsCriminalAction(from) && this.Map != null && (this.Map.Rules & MapRules.HarmfulRestrictions) != 0)
            {
                if (this.m_Owner == null || !this.m_Owner.Player)
                    from.SendLocalizedMessage(1005035); // You did not earn the right to loot this creature!
                else
                    from.SendLocalizedMessage(1010049); // You may not loot this corpse.

                return;
            }

            Mobile dead = this.m_Owner;

            if (this.GetFlag(CorpseFlag.Carved) || dead == null)
            {
                from.SendLocalizedMessage(500485); // You see nothing useful to carve from the corpse.
            }
            else if (((Body)this.Amount).IsHuman && this.ItemID == 0x2006)
            {
                new Blood(0x122D).MoveToWorld(this.Location, this.Map);

                new Torso().MoveToWorld(this.Location, this.Map);
                new LeftLeg().MoveToWorld(this.Location, this.Map);
                new LeftArm().MoveToWorld(this.Location, this.Map);
                new RightLeg().MoveToWorld(this.Location, this.Map);
                new RightArm().MoveToWorld(this.Location, this.Map);
                new Head(dead.Name).MoveToWorld(this.Location, this.Map);

                this.SetFlag(CorpseFlag.Carved, true);

                this.ProcessDelta();
                this.SendRemovePacket();
                this.ItemID = Utility.Random(0xECA, 9); // bone graphic
                this.Hue = 0;
                this.ProcessDelta();

                if (this.IsCriminalAction(from))
                    from.CriminalAction(true);
            }
            else if (dead is BaseCreature)
            {
                ((BaseCreature)dead).OnCarve(from, this, item);
            }
            else
            {
                from.SendLocalizedMessage(500485); // You see nothing useful to carve from the corpse.
            }
        }
    }
}