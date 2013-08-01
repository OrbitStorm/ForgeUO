using System;
using System.Collections.Generic;
using System.Text;
using Server.Mobiles;
using Server.Network;
using Server.Spells;

namespace Server.Items
{
    public class Teleporter : Item
    {
        private bool m_Active, m_Creatures, m_CombatCheck, m_CriminalCheck;
        private Point3D m_PointDest;
        private Map m_MapDest;
        private bool m_SourceEffect;
        private bool m_DestEffect;
        private int m_SoundID;
        private TimeSpan m_Delay;
        [Constructable]
        public Teleporter()
            : this(new Point3D(0, 0, 0), null, false)
        {
        }

        [Constructable]
        public Teleporter(Point3D pointDest, Map mapDest)
            : this(pointDest, mapDest, false)
        {
        }

        [Constructable]
        public Teleporter(Point3D pointDest, Map mapDest, bool creatures)
            : base(0x1BC3)
        {
            this.Movable = false;
            this.Visible = false;

            this.m_Active = true;
            this.m_PointDest = pointDest;
            this.m_MapDest = mapDest;
            this.m_Creatures = creatures;

            this.m_CombatCheck = false;
            this.m_CriminalCheck = false;
        }

        public Teleporter(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool SourceEffect
        {
            get
            {
                return this.m_SourceEffect;
            }
            set
            {
                this.m_SourceEffect = value;
                this.InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool DestEffect
        {
            get
            {
                return this.m_DestEffect;
            }
            set
            {
                this.m_DestEffect = value;
                this.InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public int SoundID
        {
            get
            {
                return this.m_SoundID;
            }
            set
            {
                this.m_SoundID = value;
                this.InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan Delay
        {
            get
            {
                return this.m_Delay;
            }
            set
            {
                this.m_Delay = value;
                this.InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool Active
        {
            get
            {
                return this.m_Active;
            }
            set
            {
                this.m_Active = value;
                this.InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D PointDest
        {
            get
            {
                return this.m_PointDest;
            }
            set
            {
                this.m_PointDest = value;
                this.InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public Map MapDest
        {
            get
            {
                return this.m_MapDest;
            }
            set
            {
                this.m_MapDest = value;
                this.InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool Creatures
        {
            get
            {
                return this.m_Creatures;
            }
            set
            {
                this.m_Creatures = value;
                this.InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool CombatCheck
        {
            get
            {
                return this.m_CombatCheck;
            }
            set
            {
                this.m_CombatCheck = value;
                this.InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool CriminalCheck
        {
            get
            {
                return this.m_CriminalCheck;
            }
            set
            {
                this.m_CriminalCheck = value;
                this.InvalidateProperties();
            }
        }
        public override int LabelNumber
        {
            get
            {
                return 1026095;
            }
        }// teleporter
        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (this.m_Active)
                list.Add(1060742); // active
            else
                list.Add(1060743); // inactive

            if (this.m_MapDest != null)
                list.Add(1060658, "Map\t{0}", this.m_MapDest);

            if (this.m_PointDest != Point3D.Zero)
                list.Add(1060659, "Coords\t{0}", this.m_PointDest);

            list.Add(1060660, "Creatures\t{0}", this.m_Creatures ? "Yes" : "No");
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (this.m_Active)
            {
                if (this.m_MapDest != null && this.m_PointDest != Point3D.Zero)
                    this.LabelTo(from, "{0} [{1}]", this.m_PointDest, this.m_MapDest);
                else if (this.m_MapDest != null)
                    this.LabelTo(from, "[{0}]", this.m_MapDest);
                else if (this.m_PointDest != Point3D.Zero)
                    this.LabelTo(from, this.m_PointDest.ToString());
            }
            else
            {
                this.LabelTo(from, "(inactive)");
            }
        }

        public virtual bool CanTeleport(Mobile m)
        {
            if (!this.m_Creatures && !m.Player)
            {
                return false;
            }
            else if (this.m_CriminalCheck && m.Criminal)
            {
                m.SendLocalizedMessage(1005561, "", 0x22); // Thou'rt a criminal and cannot escape so easily.
                return false;
            }
            else if (this.m_CombatCheck && SpellHelper.CheckCombat(m))
            {
                m.SendLocalizedMessage(1005564, "", 0x22); // Wouldst thou flee during the heat of battle??
                return false;
            }

            return true;
        }

        public virtual void StartTeleport(Mobile m)
        {
            if (this.m_Delay == TimeSpan.Zero)
                this.DoTeleport(m);
            else
                Timer.DelayCall<Mobile>(this.m_Delay, DoTeleport, m);
        }

        public virtual void DoTeleport(Mobile m)
        {
            Map map = this.m_MapDest;

            if (map == null || map == Map.Internal)
                map = m.Map;

            Point3D p = this.m_PointDest;

            if (p == Point3D.Zero)
                p = m.Location;

            Server.Mobiles.BaseCreature.TeleportPets(m, p, map);

            bool sendEffect = (!m.Hidden || m.IsPlayer());

            if (this.m_SourceEffect && sendEffect)
                Effects.SendLocationEffect(m.Location, m.Map, 0x3728, 10, 10);

            m.MoveToWorld(p, map);

            if (this.m_DestEffect && sendEffect)
                Effects.SendLocationEffect(m.Location, m.Map, 0x3728, 10, 10);

            if (this.m_SoundID > 0 && sendEffect)
                Effects.PlaySound(m.Location, m.Map, this.m_SoundID);
        }

        public override bool OnMoveOver(Mobile m)
        {
            if (this.m_Active && this.CanTeleport(m))
            {
                this.StartTeleport(m);
                return false;
            }

            return true;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)4); // version

            writer.Write((bool)this.m_CriminalCheck);
            writer.Write((bool)this.m_CombatCheck);

            writer.Write((bool)this.m_SourceEffect);
            writer.Write((bool)this.m_DestEffect);
            writer.Write((TimeSpan)this.m_Delay);
            writer.WriteEncodedInt((int)this.m_SoundID);

            writer.Write(this.m_Creatures);

            writer.Write(this.m_Active);
            writer.Write(this.m_PointDest);
            writer.Write(this.m_MapDest);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch ( version )
            {
                case 4:
                    {
                        this.m_CriminalCheck = reader.ReadBool();
                        goto case 3;
                    }
                case 3:
                    {
                        this.m_CombatCheck = reader.ReadBool();
                        goto case 2;
                    }
                case 2:
                    {
                        this.m_SourceEffect = reader.ReadBool();
                        this.m_DestEffect = reader.ReadBool();
                        this.m_Delay = reader.ReadTimeSpan();
                        this.m_SoundID = reader.ReadEncodedInt();

                        goto case 1;
                    }
                case 1:
                    {
                        this.m_Creatures = reader.ReadBool();

                        goto case 0;
                    }
                case 0:
                    {
                        this.m_Active = reader.ReadBool();
                        this.m_PointDest = reader.ReadPoint3D();
                        this.m_MapDest = reader.ReadMap();

                        break;
                    }
            }
        }
    }

    public class SkillTeleporter : Teleporter
    {
        private SkillName m_Skill;
        private double m_Required;
        private string m_MessageString;
        private int m_MessageNumber;
        [Constructable]
        public SkillTeleporter()
        {
        }

        public SkillTeleporter(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public SkillName Skill
        {
            get
            {
                return this.m_Skill;
            }
            set
            {
                this.m_Skill = value;
                this.InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public double Required
        {
            get
            {
                return this.m_Required;
            }
            set
            {
                this.m_Required = value;
                this.InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public string MessageString
        {
            get
            {
                return this.m_MessageString;
            }
            set
            {
                this.m_MessageString = value;
                this.InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public int MessageNumber
        {
            get
            {
                return this.m_MessageNumber;
            }
            set
            {
                this.m_MessageNumber = value;
                this.InvalidateProperties();
            }
        }
        public override bool CanTeleport(Mobile m)
        {
            if (!base.CanTeleport(m))
                return false;

            Skill sk = m.Skills[this.m_Skill];

            if (sk == null || sk.Base < this.m_Required)
            {
                if (m.BeginAction(this))
                {
                    if (this.m_MessageString != null)
                        m.Send(new UnicodeMessage(this.Serial, this.ItemID, MessageType.Regular, 0x3B2, 3, "ENU", null, this.m_MessageString));
                    else if (this.m_MessageNumber != 0)
                        m.Send(new MessageLocalized(this.Serial, this.ItemID, MessageType.Regular, 0x3B2, 3, this.m_MessageNumber, null, ""));

                    Timer.DelayCall(TimeSpan.FromSeconds(5.0), new TimerStateCallback(EndMessageLock), m);
                }

                return false;
            }

            return true;
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            int skillIndex = (int)this.m_Skill;
            string skillName;

            if (skillIndex >= 0 && skillIndex < SkillInfo.Table.Length)
                skillName = SkillInfo.Table[skillIndex].Name;
            else
                skillName = "(Invalid)";

            list.Add(1060661, "{0}\t{1:F1}", skillName, this.m_Required);

            if (this.m_MessageString != null)
                list.Add(1060662, "Message\t{0}", this.m_MessageString);
            else if (this.m_MessageNumber != 0)
                list.Add(1060662, "Message\t#{0}", this.m_MessageNumber);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((int)this.m_Skill);
            writer.Write((double)this.m_Required);
            writer.Write((string)this.m_MessageString);
            writer.Write((int)this.m_MessageNumber);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch ( version )
            {
                case 0:
                    {
                        this.m_Skill = (SkillName)reader.ReadInt();
                        this.m_Required = reader.ReadDouble();
                        this.m_MessageString = reader.ReadString();
                        this.m_MessageNumber = reader.ReadInt();

                        break;
                    }
            }
        }

        private void EndMessageLock(object state)
        {
            ((Mobile)state).EndAction(this);
        }
    }

    public class KeywordTeleporter : Teleporter
    {
        private string m_Substring;
        private int m_Keyword;
        private int m_Range;
        [Constructable]
        public KeywordTeleporter()
        {
            this.m_Keyword = -1;
            this.m_Substring = null;
        }

        public KeywordTeleporter(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Substring
        {
            get
            {
                return this.m_Substring;
            }
            set
            {
                this.m_Substring = value;
                this.InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public int Keyword
        {
            get
            {
                return this.m_Keyword;
            }
            set
            {
                this.m_Keyword = value;
                this.InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public int Range
        {
            get
            {
                return this.m_Range;
            }
            set
            {
                this.m_Range = value;
                this.InvalidateProperties();
            }
        }
        public override bool HandlesOnSpeech
        {
            get
            {
                return true;
            }
        }
        public override void OnSpeech(SpeechEventArgs e)
        {
            if (!e.Handled && this.Active)
            {
                Mobile m = e.Mobile;

                if (!m.InRange(this.GetWorldLocation(), this.m_Range))
                    return;

                bool isMatch = false;

                if (this.m_Keyword >= 0 && e.HasKeyword(this.m_Keyword))
                    isMatch = true;
                else if (this.m_Substring != null && e.Speech.ToLower().IndexOf(this.m_Substring.ToLower()) >= 0)
                    isMatch = true;

                if (!isMatch || !this.CanTeleport(m))
                    return;

                e.Handled = true;
                this.StartTeleport(m);
            }
        }

        public override void DoTeleport(Mobile m)
        {
            if (!m.InRange(this.GetWorldLocation(), this.m_Range) || m.Map != this.Map)
                return;

            base.DoTeleport(m);
        }

        public override bool OnMoveOver(Mobile m)
        {
            return true;
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1060661, "Range\t{0}", this.m_Range);

            if (this.m_Keyword >= 0)
                list.Add(1060662, "Keyword\t{0}", this.m_Keyword);

            if (this.m_Substring != null)
                list.Add(1060663, "Substring\t{0}", this.m_Substring);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write(this.m_Substring);
            writer.Write(this.m_Keyword);
            writer.Write(this.m_Range);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch ( version )
            {
                case 0:
                    {
                        this.m_Substring = reader.ReadString();
                        this.m_Keyword = reader.ReadInt();
                        this.m_Range = reader.ReadInt();

                        break;
                    }
            }
        }
    }

    public class WaitTeleporter : KeywordTeleporter
    {
        private static Dictionary<Mobile, TeleportingInfo> m_Table;
        private int m_StartNumber;
        private string m_StartMessage;
        private int m_ProgressNumber;
        private string m_ProgressMessage;
        private bool m_ShowTimeRemaining;
        [Constructable]
        public WaitTeleporter()
        {
        }

        public WaitTeleporter(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int StartNumber
        {
            get
            {
                return this.m_StartNumber;
            }
            set
            {
                this.m_StartNumber = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public string StartMessage
        {
            get
            {
                return this.m_StartMessage;
            }
            set
            {
                this.m_StartMessage = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public int ProgressNumber
        {
            get
            {
                return this.m_ProgressNumber;
            }
            set
            {
                this.m_ProgressNumber = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public string ProgressMessage
        {
            get
            {
                return this.m_ProgressMessage;
            }
            set
            {
                this.m_ProgressMessage = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool ShowTimeRemaining
        {
            get
            {
                return this.m_ShowTimeRemaining;
            }
            set
            {
                this.m_ShowTimeRemaining = value;
            }
        }
        public static void Initialize()
        {
            m_Table = new Dictionary<Mobile, TeleportingInfo>();

            EventSink.Logout += new LogoutEventHandler(EventSink_Logout);
        }

        public static void EventSink_Logout(LogoutEventArgs e)
        {
            Mobile from = e.Mobile;
            TeleportingInfo info;

            if (from == null || !m_Table.TryGetValue(from, out info))
                return;

            info.Timer.Stop();
            m_Table.Remove(from);
        }

        public static string FormatTime(TimeSpan ts)
        {
            if (ts.TotalHours >= 1)
            {
                int h = (int)Math.Round(ts.TotalHours);
                return String.Format("{0} hour{1}", h, (h == 1) ? "" : "s");
            }
            else if (ts.TotalMinutes >= 1)
            {
                int m = (int)Math.Round(ts.TotalMinutes);
                return String.Format("{0} minute{1}", m, (m == 1) ? "" : "s");
            }

            int s = Math.Max((int)Math.Round(ts.TotalSeconds), 0);
            return String.Format("{0} second{1}", s, (s == 1) ? "" : "s");
        }

        public override void StartTeleport(Mobile m)
        {
            TeleportingInfo info;

            if (m_Table.TryGetValue(m, out info))
            {
                if (info.Teleporter == this)
                {
                    if (m.BeginAction(this))
                    {
                        if (this.m_ProgressMessage != null)
                            m.SendMessage(this.m_ProgressMessage);
                        else if (this.m_ProgressNumber != 0)
                            m.SendLocalizedMessage(this.m_ProgressNumber);

                        if (this.m_ShowTimeRemaining)
                            m.SendMessage("Time remaining: {0}", FormatTime(m_Table[m].Timer.Next - DateTime.Now));

                        Timer.DelayCall<Mobile>(TimeSpan.FromSeconds(5), EndLock, m);
                    }

                    return;
                }
                else
                {
                    info.Timer.Stop();
                }
            }

            if (this.m_StartMessage != null)
                m.SendMessage(this.m_StartMessage);
            else if (this.m_StartNumber != 0)
                m.SendLocalizedMessage(this.m_StartNumber);

            if (this.Delay == TimeSpan.Zero)
                this.DoTeleport(m);
            else
                m_Table[m] = new TeleportingInfo(this, Timer.DelayCall<Mobile>(this.Delay, DoTeleport, m));
        }

        public override void DoTeleport(Mobile m)
        {
            m_Table.Remove(m);

            base.DoTeleport(m);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write(this.m_StartNumber);
            writer.Write(this.m_StartMessage);
            writer.Write(this.m_ProgressNumber);
            writer.Write(this.m_ProgressMessage);
            writer.Write(this.m_ShowTimeRemaining);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            this.m_StartNumber = reader.ReadInt();
            this.m_StartMessage = reader.ReadString();
            this.m_ProgressNumber = reader.ReadInt();
            this.m_ProgressMessage = reader.ReadString();
            this.m_ShowTimeRemaining = reader.ReadBool();
        }

        private void EndLock(Mobile m)
        {
            m.EndAction(this);
        }

        private class TeleportingInfo
        {
            private readonly WaitTeleporter m_Teleporter;
            private readonly Timer m_Timer;
            public TeleportingInfo(WaitTeleporter tele, Timer t)
            {
                this.m_Teleporter = tele;
                this.m_Timer = t;
            }

            public WaitTeleporter Teleporter
            {
                get
                {
                    return this.m_Teleporter;
                }
            }
            public Timer Timer
            {
                get
                {
                    return this.m_Timer;
                }
            }
        }
    }

    public class TimeoutTeleporter : Teleporter
    {
        private TimeSpan m_TimeoutDelay;
        private Dictionary<Mobile, Timer> m_Teleporting;
        [Constructable]
        public TimeoutTeleporter()
            : this(new Point3D(0, 0, 0), null, false)
        {
        }

        [Constructable]
        public TimeoutTeleporter(Point3D pointDest, Map mapDest)
            : this(pointDest, mapDest, false)
        {
        }

        [Constructable]
        public TimeoutTeleporter(Point3D pointDest, Map mapDest, bool creatures)
            : base(pointDest, mapDest, creatures)
        {
            this.m_Teleporting = new Dictionary<Mobile, Timer>();
        }

        public TimeoutTeleporter(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan TimeoutDelay
        {
            get
            {
                return this.m_TimeoutDelay;
            }
            set
            {
                this.m_TimeoutDelay = value;
            }
        }
        public void StartTimer(Mobile m)
        {
            this.StartTimer(m, this.m_TimeoutDelay);
        }

        public void StopTimer(Mobile m)
        {
            Timer t;

            if (this.m_Teleporting.TryGetValue(m, out t))
            {
                t.Stop();
                this.m_Teleporting.Remove(m);
            }
        }

        public override void DoTeleport(Mobile m)
        {
            this.m_Teleporting.Remove(m);

            base.DoTeleport(m);
        }

        public override bool OnMoveOver(Mobile m)
        {
            if (this.Active)
            {
                if (!this.CanTeleport(m))
                    return false;

                this.StartTimer(m);
            }

            return true;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write(this.m_TimeoutDelay);
            writer.Write(this.m_Teleporting.Count);

            foreach (KeyValuePair<Mobile, Timer> kvp in this.m_Teleporting)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value.Next);
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            this.m_TimeoutDelay = reader.ReadTimeSpan();
            this.m_Teleporting = new Dictionary<Mobile, Timer>();

            int count = reader.ReadInt();

            for (int i = 0; i < count; ++i)
            {
                Mobile m = reader.ReadMobile();
                DateTime end = reader.ReadDateTime();

                this.StartTimer(m, end - DateTime.Now);
            }
        }

        private void StartTimer(Mobile m, TimeSpan delay)
        {
            Timer t;

            if (this.m_Teleporting.TryGetValue(m, out t))
                t.Stop();

            this.m_Teleporting[m] = Timer.DelayCall<Mobile>(delay, StartTeleport, m);
        }
    }

    public class TimeoutGoal : Item
    {
        private TimeoutTeleporter m_Teleporter;
        [Constructable]
        public TimeoutGoal()
            : base(0x1822)
        {
            this.Movable = false;
            this.Visible = false;

            this.Hue = 1154;
        }

        public TimeoutGoal(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeoutTeleporter Teleporter
        {
            get
            {
                return this.m_Teleporter;
            }
            set
            {
                this.m_Teleporter = value;
            }
        }
        public override string DefaultName
        {
            get
            {
                return "timeout teleporter goal";
            }
        }
        public override bool OnMoveOver(Mobile m)
        {
            if (this.m_Teleporter != null)
                this.m_Teleporter.StopTimer(m);

            return true;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.WriteItem<TimeoutTeleporter>(this.m_Teleporter);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            this.m_Teleporter = reader.ReadItem<TimeoutTeleporter>();
        }
    }

    public class ConditionTeleporter : Teleporter
    {
        private ConditionFlag m_Flags;
        [Constructable]
        public ConditionTeleporter()
        {
        }

        public ConditionTeleporter(Serial serial)
            : base(serial)
        {
        }

        [Flags]
        protected enum ConditionFlag
        {
            None = 0x00,
            DenyMounted = 0x01,
            DenyFollowers = 0x02,
            DenyPackContents = 0x04,
            DenyHolding = 0x08,
            DenyEquipment = 0x10,
            DenyTransformed = 0x20,
            StaffOnly = 0x40
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool DenyMounted
        {
            get
            {
                return this.GetFlag(ConditionFlag.DenyMounted);
            }
            set
            {
                this.SetFlag(ConditionFlag.DenyMounted, value);
                this.InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool DenyFollowers
        {
            get
            {
                return this.GetFlag(ConditionFlag.DenyFollowers);
            }
            set
            {
                this.SetFlag(ConditionFlag.DenyFollowers, value);
                this.InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool DenyPackContents
        {
            get
            {
                return this.GetFlag(ConditionFlag.DenyPackContents);
            }
            set
            {
                this.SetFlag(ConditionFlag.DenyPackContents, value);
                this.InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool DenyHolding
        {
            get
            {
                return this.GetFlag(ConditionFlag.DenyHolding);
            }
            set
            {
                this.SetFlag(ConditionFlag.DenyHolding, value);
                this.InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool DenyEquipment
        {
            get
            {
                return this.GetFlag(ConditionFlag.DenyEquipment);
            }
            set
            {
                this.SetFlag(ConditionFlag.DenyEquipment, value);
                this.InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool DenyTransformed
        {
            get
            {
                return this.GetFlag(ConditionFlag.DenyTransformed);
            }
            set
            {
                this.SetFlag(ConditionFlag.DenyTransformed, value);
                this.InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool StaffOnly
        {
            get
            {
                return this.GetFlag(ConditionFlag.StaffOnly);
            }
            set
            {
                this.SetFlag(ConditionFlag.StaffOnly, value);
                this.InvalidateProperties();
            }
        }
        public override bool CanTeleport(Mobile m)
        {
            if (!base.CanTeleport(m))
                return false;

            if (this.GetFlag(ConditionFlag.StaffOnly) && m.IsPlayer())
                return false;

            if (this.GetFlag(ConditionFlag.DenyMounted) && m.Mounted)
            {
                m.SendLocalizedMessage(1077252); // You must dismount before proceeding.
                return false;
            }

            if (this.GetFlag(ConditionFlag.DenyFollowers) && (m.Followers != 0 || (m is PlayerMobile && ((PlayerMobile)m).AutoStabled.Count != 0)))
            {
                m.SendLocalizedMessage(1077250); // No pets permitted beyond this point.
                return false;
            }

            Container pack;

            if (this.GetFlag(ConditionFlag.DenyPackContents) && (pack = m.Backpack) != null && pack.TotalItems != 0)
            {
                m.SendMessage("You must empty your backpack before proceeding.");
                return false;
            }

            if (this.GetFlag(ConditionFlag.DenyHolding) && m.Holding != null)
            {
                m.SendMessage("You must let go of what you are holding before proceeding.");
                return false;
            }

            if (this.GetFlag(ConditionFlag.DenyEquipment))
            {
                foreach (Item item in m.Items)
                {
                    switch ( item.Layer )
                    {
                        case Layer.Hair:
                        case Layer.FacialHair:
                        case Layer.Backpack:
                        case Layer.Mount:
                        case Layer.Bank:
                            {
                                continue; // ignore
                            }
                        default:
                            {
                                m.SendMessage("You must remove all of your equipment before proceeding.");
                                return false;
                            }
                    }
                }
            }

            if (this.GetFlag(ConditionFlag.DenyTransformed) && m.IsBodyMod)
            {
                m.SendMessage("You cannot go there in this form.");
                return false;
            }

            return true;
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            StringBuilder props = new StringBuilder();

            if (this.GetFlag(ConditionFlag.DenyMounted))
                props.Append("<BR>Deny Mounted");

            if (this.GetFlag(ConditionFlag.DenyFollowers))
                props.Append("<BR>Deny Followers");

            if (this.GetFlag(ConditionFlag.DenyPackContents))
                props.Append("<BR>Deny Pack Contents");

            if (this.GetFlag(ConditionFlag.DenyHolding))
                props.Append("<BR>Deny Holding");

            if (this.GetFlag(ConditionFlag.DenyEquipment))
                props.Append("<BR>Deny Equipment");

            if (this.GetFlag(ConditionFlag.DenyTransformed))
                props.Append("<BR>Deny Transformed");

            if (this.GetFlag(ConditionFlag.StaffOnly))
                props.Append("<BR>Staff Only");

            if (props.Length != 0)
            {
                props.Remove(0, 4);
                list.Add(props.ToString());
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((int)this.m_Flags);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            this.m_Flags = (ConditionFlag)reader.ReadInt();
        }

        protected bool GetFlag(ConditionFlag flag)
        {
            return ((this.m_Flags & flag) != 0);
        }

        protected void SetFlag(ConditionFlag flag, bool value)
        {
            if (value)
                this.m_Flags |= flag;
            else
                this.m_Flags &= ~flag;
        }
    }
}