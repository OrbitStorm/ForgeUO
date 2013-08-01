using System;
using System.Collections.Generic;
using Server.Commands;
using Server.Commands.Generic;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Guilds
{
    #region Ranks
    [Flags]
    public enum RankFlags
    {
        None = 0x00000000,
        CanInvitePlayer = 0x00000001,
        AccessGuildItems = 0x00000002,
        RemoveLowestRank = 0x00000004,
        RemovePlayers = 0x00000008,
        CanPromoteDemote = 0x00000010,
        ControlWarStatus = 0x00000020,
        AllianceControl = 0x00000040,
        CanSetGuildTitle = 0x00000080,
        CanVote = 0x00000100,

        All = Member | CanInvitePlayer | RemovePlayers | CanPromoteDemote | ControlWarStatus | AllianceControl | CanSetGuildTitle,
        Member = RemoveLowestRank | AccessGuildItems | CanVote
    }

    public class RankDefinition
    {
        public static RankDefinition[] Ranks = new RankDefinition[]
        {
            new RankDefinition(1062963, 0, RankFlags.None), //Ronin
            new RankDefinition(1062962, 1, RankFlags.Member), //Member
            new RankDefinition(1062961, 2, RankFlags.Member | RankFlags.RemovePlayers | RankFlags.CanInvitePlayer | RankFlags.CanSetGuildTitle | RankFlags.CanPromoteDemote), //Emmissary
            new RankDefinition(1062960, 3, RankFlags.Member | RankFlags.ControlWarStatus), //Warlord
            new RankDefinition(1062959, 4, RankFlags.All)//Leader
        };
        public static RankDefinition Leader
        {
            get
            {
                return Ranks[4];
            }
        }
        public static RankDefinition Member
        {
            get
            {
                return Ranks[1];
            }
        }
        public static RankDefinition Lowest
        {
            get
            {
                return Ranks[0];
            }
        }

        private readonly TextDefinition m_Name;
        private readonly int m_Rank;
        private RankFlags m_Flags;

        public TextDefinition Name
        {
            get
            {
                return this.m_Name;
            }
        }
        public int Rank
        {
            get
            {
                return this.m_Rank;
            }
        }
        public RankFlags Flags
        {
            get
            {
                return this.m_Flags;
            }
        }

        public RankDefinition(TextDefinition name, int rank, RankFlags flags)
        {
            this.m_Name = name; 
            this.m_Rank = rank;
            this.m_Flags = flags;
        }

        public bool GetFlag(RankFlags flag)
        {
            return ((this.m_Flags & flag) != 0);
        }

        public void SetFlag(RankFlags flag, bool value)
        {
            if (value)
                this.m_Flags |= flag;
            else
                this.m_Flags &= ~flag;
        }
    }

    #endregion

    #region Alliances
    public class AllianceInfo
    {
        private static readonly Dictionary<string, AllianceInfo> m_Alliances = new Dictionary<string, AllianceInfo>();

        public static Dictionary<string, AllianceInfo> Alliances
        {
            get
            {
                return m_Alliances;
            }
        }

        private readonly string m_Name;
        private Guild m_Leader;
        private readonly List<Guild> m_Members;
        private readonly List<Guild> m_PendingMembers;
		
        public string Name
        {
            get
            {
                return this.m_Name;
            }
        }

        public void CalculateAllianceLeader()
        {
            this.m_Leader = ((this.m_Members.Count >= 2) ? this.m_Members[Utility.Random(this.m_Members.Count)] : null);
        }

        public void CheckLeader()
        {
            if (this.m_Leader == null || this.m_Leader.Disbanded)
            {
                this.CalculateAllianceLeader();

                if (this.m_Leader == null)
                    this.Disband();
            }
        }

        public Guild Leader
        {
            get
            {
                this.CheckLeader();
                return this.m_Leader; 
            }
            set
            {
                if (this.m_Leader != value && value != null)
                    this.AllianceMessage(1070765, value.Name); // Your Alliance is now led by ~1_GUILDNAME~

                this.m_Leader = value;

                if (this.m_Leader == null)
                    this.CalculateAllianceLeader();
            }
        }

        public bool IsPendingMember(Guild g)
        {
            if (g.Alliance != this)
                return false;

            return this.m_PendingMembers.Contains(g);
        }

        public bool IsMember(Guild g)
        {
            if (g.Alliance != this)
                return false;

            return this.m_Members.Contains(g);
        }

        public AllianceInfo(Guild leader, string name, Guild partner)
        {
            this.m_Leader = leader;
            this.m_Name = name;

            this.m_Members = new List<Guild>();
            this.m_PendingMembers = new List<Guild>();

            leader.Alliance = this;
            partner.Alliance = this;

            if (!m_Alliances.ContainsKey(this.m_Name.ToLower()))
                m_Alliances.Add(this.m_Name.ToLower(), this);
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write((int)0);	//Version

            writer.Write(this.m_Name);
            writer.Write(this.m_Leader);

            writer.WriteGuildList(this.m_Members, true);
            writer.WriteGuildList(this.m_PendingMembers, true);

            if (!m_Alliances.ContainsKey(this.m_Name.ToLower()))
                m_Alliances.Add(this.m_Name.ToLower(), this);
        }

        public AllianceInfo(GenericReader reader)
        {
            int version = reader.ReadInt();

            switch( version )
            {
                case 0:
                    {
                        this.m_Name = reader.ReadString();
                        this.m_Leader = reader.ReadGuild() as Guild;

                        this.m_Members = reader.ReadStrongGuildList<Guild>();
                        this.m_PendingMembers = reader.ReadStrongGuildList<Guild>();

                        break;
                    }
            }
        }

        public void AddPendingGuild(Guild g)
        {
            if (g.Alliance != this || this.m_PendingMembers.Contains(g) || this.m_Members.Contains(g))
                return;

            this.m_PendingMembers.Add(g);
        }

        public void TurnToMember(Guild g)
        {
            if (g.Alliance != this || !this.m_PendingMembers.Contains(g) || this.m_Members.Contains(g))
                return;

            g.GuildMessage(1070760, this.Name); // Your Guild has joined the ~1_ALLIANCENAME~ Alliance.
            this.AllianceMessage(1070761, g.Name); // A new Guild has joined your Alliance: ~1_GUILDNAME~

            this.m_PendingMembers.Remove(g);
            this.m_Members.Add(g);
            g.Alliance.InvalidateMemberProperties();
        }

        public void RemoveGuild(Guild g)
        {
            if (this.m_PendingMembers.Contains(g))
            {
                this.m_PendingMembers.Remove(g);
            }
			
            if (this.m_Members.Contains(g))	//Sanity, just incase someone with a custom script adds a character to BOTH arrays
            {
                this.m_Members.Remove(g);
                g.InvalidateMemberProperties();

                g.GuildMessage(1070763, this.Name); // Your Guild has been removed from the ~1_ALLIANCENAME~ Alliance.
                this.AllianceMessage(1070764, g.Name); // A Guild has left your Alliance: ~1_GUILDNAME~
            }

            //g.Alliance = null;	//NO G.Alliance call here.  Set the Guild's Alliance to null, if you JUST use RemoveGuild, it removes it from the alliance, but doesn't remove the link from the guild to the alliance.  setting g.Alliance will call this method.
            //to check on OSI: have 3 guilds, make 2 of them a member, one pending.  remove one of the memebers.  alliance still exist?
            //ANSWER: NO

            if (g == this.m_Leader)
            {
                this.CalculateAllianceLeader();
                /*
                if( m_Leader == null ) //only when m_members.count < 2
                Disband();
                else
                AllianceMessage( 1070765, m_Leader.Name ); // Your Alliance is now led by ~1_GUILDNAME~
                */
            }

            if (this.m_Members.Count < 2)
                this.Disband();
        }

        public void Disband()
        {
            this.AllianceMessage(1070762); // Your Alliance has dissolved.

            for (int i = 0; i < this.m_PendingMembers.Count; i++)
                this.m_PendingMembers[i].Alliance = null;

            for (int i = 0; i < this.m_Members.Count; i++)
                this.m_Members[i].Alliance = null;

            AllianceInfo aInfo = null;

            m_Alliances.TryGetValue(this.m_Name.ToLower(), out aInfo);

            if (aInfo == this)
                m_Alliances.Remove(this.m_Name.ToLower());
        }

        public void InvalidateMemberProperties()
        {
            this.InvalidateMemberProperties(false);
        }

        public void InvalidateMemberProperties(bool onlyOPL)
        {
            for (int i = 0; i < this.m_Members.Count; i++)
            {
                Guild g = this.m_Members[i];

                g.InvalidateMemberProperties(onlyOPL);
            }
        }

        public void InvalidateMemberNotoriety()
        {
            for (int i = 0; i < this.m_Members.Count; i++)
                this.m_Members[i].InvalidateMemberNotoriety();
        }

        #region Alliance[Text]Message(...)
        public void AllianceMessage(int num, bool append, string format, params object[] args)
        {
            this.AllianceMessage(num, append, String.Format(format, args));
        }

        public void AllianceMessage(int number)
        {
            for (int i = 0; i < this.m_Members.Count; ++i)
                this.m_Members[i].GuildMessage(number);
        }

        public void AllianceMessage(int number, string args)
        {
            this.AllianceMessage(number, args, 0x3B2);
        }

        public void AllianceMessage(int number, string args, int hue)
        {
            for (int i = 0; i < this.m_Members.Count; ++i)
                this.m_Members[i].GuildMessage(number, args, hue);
        }

        public void AllianceMessage(int number, bool append, string affix)
        {
            this.AllianceMessage(number, append, affix, "", 0x3B2);
        }

        public void AllianceMessage(int number, bool append, string affix, string args)
        {
            this.AllianceMessage(number, append, affix, args, 0x3B2);
        }

        public void AllianceMessage(int number, bool append, string affix, string args, int hue)
        {
            for (int i = 0; i < this.m_Members.Count; ++i)
                this.m_Members[i].GuildMessage(number, append, affix, args, hue);
        }

        public void AllianceTextMessage(string text)
        {
            this.AllianceTextMessage(0x3B2, text);
        }

        public void AllianceTextMessage(string format, params object[] args)
        {
            this.AllianceTextMessage(0x3B2, String.Format(format, args));
        }

        public void AllianceTextMessage(int hue, string text)
        {
            for (int i = 0; i < this.m_Members.Count; ++i)
                this.m_Members[i].GuildTextMessage(hue, text);
        }

        public void AllianceTextMessage(int hue, string format, params object[] args)
        {
            this.AllianceTextMessage(hue, String.Format(format, args));
        }

        public void AllianceChat(Mobile from, int hue, string text)
        {
            Packet p = null;
            for (int i = 0; i < this.m_Members.Count; i++)
            {
                Guild g = this.m_Members[i];

                for (int j = 0; j < g.Members.Count; j++)
                {
                    Mobile m = g.Members[j];

                    NetState state = m.NetState;

                    if (state != null)
                    {
                        if (p == null)
                            p = Packet.Acquire(new UnicodeMessage(from.Serial, from.Body, MessageType.Alliance, hue, 3, from.Language, from.Name, text));

                        state.Send(p);
                    }
                }
            }

            Packet.Release(p);
        }

        public void AllianceChat(Mobile from, string text)
        {
            PlayerMobile pm = from as PlayerMobile;

            this.AllianceChat(from, (pm == null) ? 0x3B2 : pm.AllianceMessageHue, text);
        }

        #endregion

        public class AllianceRosterGump : GuildDiplomacyGump
        {
            protected override bool AllowAdvancedSearch
            {
                get
                {
                    return false;
                }
            }

            private readonly AllianceInfo m_Alliance;

            public AllianceRosterGump(PlayerMobile pm, Guild g, AllianceInfo alliance)
                : base(pm, g, true, "", 0, alliance.m_Members, alliance.Name)
            {
                this.m_Alliance = alliance;
            }

            public AllianceRosterGump(PlayerMobile pm, Guild g, AllianceInfo alliance, IComparer<Guild> currentComparer, bool ascending, string filter, int startNumber)
                : base(pm, g, currentComparer, ascending, filter, startNumber, alliance.m_Members, alliance.Name)
            {
                this.m_Alliance = alliance;
            }

            public override Gump GetResentGump(PlayerMobile pm, Guild g, IComparer<Guild> comparer, bool ascending, string filter, int startNumber)
            {
                return new AllianceRosterGump(pm, g, this.m_Alliance, comparer, ascending, filter, startNumber);
            }

            public override void OnResponse(NetState sender, RelayInfo info)
            {
                if (info.ButtonID != 8) //So that they can't get to the AdvancedSearch button
                    base.OnResponse(sender, info);
            }
        }
    }
    #endregion

    #region Wars
    public enum WarStatus
    {
        InProgress = -1,
        Win,
        Lose,
        Draw,
        Pending
    }

    public class WarDeclaration
    {
        private int m_Kills;
        private int m_MaxKills;

        private TimeSpan m_WarLength;
        private DateTime m_WarBeginning;

        private readonly Guild m_Guild;
        private readonly Guild m_Opponent;

        private bool m_WarRequester;

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
        public int MaxKills
        {
            get
            {
                return this.m_MaxKills;
            }
            set
            {
                this.m_MaxKills = value;
            }
        }
        public TimeSpan WarLength
        {
            get
            {
                return this.m_WarLength;
            }
            set
            {
                this.m_WarLength = value;
            }
        }
        public Guild Opponent
        {
            get
            {
                return this.m_Opponent;
            }
        }
        public Guild Guild
        {
            get
            {
                return this.m_Guild;
            }
        }
        public DateTime WarBeginning
        {
            get
            {
                return this.m_WarBeginning;
            }
            set
            {
                this.m_WarBeginning = value;
            }
        }
        public bool WarRequester
        {
            get
            {
                return this.m_WarRequester;
            }
            set
            {
                this.m_WarRequester = value;
            }
        }

        public WarDeclaration(Guild g, Guild opponent, int maxKills, TimeSpan warLength, bool warRequester)
        {
            this.m_Guild = g;
            this.m_MaxKills = maxKills;
            this.m_Opponent = opponent;
            this.m_WarLength = warLength;
            this.m_WarRequester = warRequester;
        }

        public WarDeclaration(GenericReader reader)
        {
            int version = reader.ReadInt();

            switch ( version )
            {
                case 0:
                    {
                        this.m_Kills = reader.ReadInt();
                        this.m_MaxKills = reader.ReadInt();

                        this.m_WarLength = reader.ReadTimeSpan();
                        this.m_WarBeginning = reader.ReadDateTime();

                        this.m_Guild = reader.ReadGuild() as Guild;
                        this.m_Opponent = reader.ReadGuild() as Guild;

                        this.m_WarRequester = reader.ReadBool();

                        break;
                    }
            }
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write((int)0);	//version

            writer.Write(this.m_Kills);
            writer.Write(this.m_MaxKills);

            writer.Write(this.m_WarLength);
            writer.Write(this.m_WarBeginning);

            writer.Write(this.m_Guild);
            writer.Write(this.m_Opponent);

            writer.Write(this.m_WarRequester);
        }

        public WarStatus Status
        {
            get
            {
                if (this.m_Opponent == null || this.m_Opponent.Disbanded)
                    return WarStatus.Win;

                if (this.m_Guild == null || this.m_Guild.Disbanded)
                    return WarStatus.Lose;

                WarDeclaration w = this.m_Opponent.FindActiveWar(this.m_Guild);

                if (this.m_Opponent.FindPendingWar(this.m_Guild) != null && this.m_Guild.FindPendingWar(this.m_Opponent) != null)
                    return WarStatus.Pending;

                if (w == null)
                    return WarStatus.Win;

                if (this.m_WarLength != TimeSpan.Zero && (this.m_WarBeginning + this.m_WarLength) < DateTime.Now)
                {
                    if (this.m_Kills > w.m_Kills)
                        return WarStatus.Win;
                    else if (this.m_Kills < w.m_Kills)
                        return WarStatus.Lose;
                    else
                        return WarStatus.Draw;
                }
                else if (this.m_MaxKills > 0)
                {
                    if (this.m_Kills >= this.m_MaxKills)
                        return WarStatus.Win;
                    else if (w.m_Kills >= w.MaxKills)
                        return WarStatus.Lose;
                }

                return WarStatus.InProgress;
            }
        }
    }

    public class WarTimer : Timer
    {
        private static readonly TimeSpan InternalDelay = TimeSpan.FromMinutes(1.0);

        public static void Initialize()
        {
            if (Guild.NewGuildSystem)
                new WarTimer().Start();
        }

        public WarTimer()
            : base(InternalDelay, InternalDelay)
        {
            this.Priority = TimerPriority.FiveSeconds;
        }

        protected override void OnTick() 
        {
            foreach (Guild g in Guild.List.Values)
                g.CheckExpiredWars();
        }
    }

    #endregion

    public class Guild : BaseGuild
    {
        public static void Configure()
        {
            EventSink.CreateGuild += new CreateGuildHandler(EventSink_CreateGuild);
            EventSink.GuildGumpRequest += new GuildGumpRequestHandler(EventSink_GuildGumpRequest);

            CommandSystem.Register("GuildProps", AccessLevel.Counselor, new CommandEventHandler(GuildProps_OnCommand));
        }

        #region GuildProps
        [Usage("GuildProps")]
        [Description("Opens a menu where you can view and edit guild properties of a targeted player or guild stone.  If the new Guild system is active, also brings up the guild gump.")]
        private static void GuildProps_OnCommand(CommandEventArgs e)
        {
            string arg = e.ArgString.Trim();
            Mobile from = e.Mobile;

            if (arg.Length == 0)
            {
                e.Mobile.Target = new GuildPropsTarget();
            }
            else
            {
                Guild g = null;

                int id;

                if (int.TryParse(arg, out id))
                    g = Guild.Find(id) as Guild;

                if (g == null)
                {
                    g = Guild.FindByAbbrev(arg) as Guild;

                    if (g == null)
                        g = Guild.FindByName(arg) as Guild;
                }

                if (g != null)
                {
                    from.SendGump(new PropertiesGump(from, g));

                    if (NewGuildSystem && from.AccessLevel >= AccessLevel.GameMaster && from is PlayerMobile)
                        from.SendGump(new GuildInfoGump((PlayerMobile)from, g));
                }
            }
        }

        private class GuildPropsTarget : Target
        {
            public GuildPropsTarget()
                : base(-1, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object o)
            {
                if (!BaseCommand.IsAccessible(from, o))
                {
                    from.SendMessage("That is not accessible.");
                    return;
                }

                Guild g = null;

                if (o is Guildstone)
                {
                    Guildstone stone = o as Guildstone;
                    if (stone.Guild == null || stone.Guild.Disbanded)
                    {
                        from.SendMessage("The guild associated with that Guildstone no longer exists");
                        return;
                    }
                    else
                        g = stone.Guild;
                }
                else if (o is Mobile)
                {
                    g = ((Mobile)o).Guild as Guild;
                }

                if (g != null)
                {
                    from.SendGump(new PropertiesGump(from, g));

                    if (NewGuildSystem && from.AccessLevel >= AccessLevel.GameMaster && from is PlayerMobile)
                        from.SendGump(new GuildInfoGump((PlayerMobile)from, g));
                }
                else
                {
                    from.SendMessage("That is not in a guild!");
                }
            }
        }
        #endregion

        #region EventSinks
        public static void EventSink_GuildGumpRequest(GuildGumpRequestArgs args)
        {
            PlayerMobile pm = args.Mobile as PlayerMobile;
            if (!NewGuildSystem || pm == null)
                return;
			
            if (pm.Guild == null)
                pm.SendGump(new CreateGuildGump(pm));
            else
                pm.SendGump(new GuildInfoGump(pm, pm.Guild as Guild));
        }

        public static BaseGuild EventSink_CreateGuild(CreateGuildEventArgs args)
        {
            return (BaseGuild)(new Guild(args.Id));
        }

        #endregion

        public static bool NewGuildSystem
        {
            get
            {
                return Core.SE;
            }
        }

        public static readonly int RegistrationFee = 25000;
        public static readonly int AbbrevLimit = 4;
        public static readonly int NameLimit = 40;
        public static readonly int MajorityPercentage = 66;
        public static readonly TimeSpan InactiveTime = TimeSpan.FromDays(30);

        #region New Alliances

        public AllianceInfo Alliance
        {
            get
            {
                if (this.m_AllianceInfo != null)
                    return this.m_AllianceInfo;
                else if (this.m_AllianceLeader != null)
                    return this.m_AllianceLeader.m_AllianceInfo;
                else
                    return null;
            }
            set
            {
                AllianceInfo current = this.Alliance;

                if (value == current)
                    return;

                if (current != null)
                {
                    current.RemoveGuild(this);
                }

                if (value != null)
                {
                    if (value.Leader == this)
                        this.m_AllianceInfo = value;
                    else
                        this.m_AllianceLeader = value.Leader;

                    value.AddPendingGuild(this);
                }
                else
                {
                    this.m_AllianceInfo = null;
                    this.m_AllianceLeader = null;
                }
            }
        }

        [CommandProperty(AccessLevel.Counselor)]
        public string AllianceName
        {
            get
            {
                AllianceInfo al = this.Alliance;
                if (al != null)
                    return al.Name;

                return null;
            }
        }

        [CommandProperty(AccessLevel.Counselor)]
        public Guild AllianceLeader
        {
            get
            {
                AllianceInfo al = this.Alliance;

                if (al != null)
                    return al.Leader;

                return null;
            }
        }

        [CommandProperty(AccessLevel.Counselor)]
        public bool IsAllianceMember
        {
            get
            {
                AllianceInfo al = this.Alliance;

                if (al != null)
                    return al.IsMember(this);

                return false;
            }
        }

        [CommandProperty(AccessLevel.Counselor)]
        public bool IsAlliancePendingMember
        {
            get
            {
                AllianceInfo al = this.Alliance;

                if (al != null)
                    return al.IsPendingMember(this);

                return false;
            }
        }
		
        public static Guild GetAllianceLeader(Guild g)
        {
            AllianceInfo alliance = g.Alliance;
			
            if (alliance != null && alliance.Leader != null && alliance.IsMember(g))
                return alliance.Leader;
			
            return g;
        }

        #endregion

        #region New Wars

        public List<WarDeclaration> PendingWars
        {
            get
            {
                return this.m_PendingWars;
            }
        }
        public List<WarDeclaration> AcceptedWars
        {
            get
            {
                return this.m_AcceptedWars;
            }
        }

        public WarDeclaration FindPendingWar(Guild g)
        {
            for (int i = 0; i < this.PendingWars.Count; i++)
            {
                WarDeclaration w = this.PendingWars[i];

                if (w.Opponent == g)
                    return w;
            }

            return null;
        }

        public WarDeclaration FindActiveWar(Guild g)
        {
            for (int i = 0; i < this.AcceptedWars.Count; i++)
            {
                WarDeclaration w = this.AcceptedWars[i];

                if (w.Opponent == g)
                    return w;
            }

            return null;
        }

        public void CheckExpiredWars()
        {
            for (int i = 0; i < this.AcceptedWars.Count; i++)
            {
                WarDeclaration w = this.AcceptedWars[i];
                Guild g = w.Opponent;

                WarStatus status = w.Status;

                if (status != WarStatus.InProgress)
                {
                    AllianceInfo myAlliance = this.Alliance;
                    bool inAlliance = (myAlliance != null && myAlliance.IsMember(this));
					
                    AllianceInfo otherAlliance = ((g != null) ? g.Alliance : null);
                    bool otherInAlliance = (otherAlliance != null && otherAlliance.IsMember(this));

                    if (inAlliance)
                    {
                        myAlliance.AllianceMessage(1070739 + (int)status, (g == null) ? "a deleted opponent" : (otherInAlliance ? otherAlliance.Name : g.Name));
                        myAlliance.InvalidateMemberProperties();
                    }
                    else
                    {
                        this.GuildMessage(1070739 + (int)status, (g == null) ? "a deleted opponent" : (otherInAlliance ? otherAlliance.Name : g.Name));
                        this.InvalidateMemberProperties();
                    }

                    this.AcceptedWars.Remove(w);

                    if (g != null)
                    {
                        if (status != WarStatus.Draw)
                            status = (WarStatus)((int)status + 1 % 2);

                        if (otherInAlliance)
                        {
                            otherAlliance.AllianceMessage(1070739 + (int)status, (inAlliance ? this.Alliance.Name : this.Name));
                            otherAlliance.InvalidateMemberProperties();
                        }
                        else
                        {
                            g.GuildMessage(1070739 + (int)status, (inAlliance ? this.Alliance.Name : this.Name));
                            g.InvalidateMemberProperties();
                        }

                        g.AcceptedWars.Remove(g.FindActiveWar(this));
                    }
                }
            }

            for (int i = 0; i < this.PendingWars.Count; i++)
            {
                WarDeclaration w = this.PendingWars[i];
                Guild g = w.Opponent;

                if (w.Status != WarStatus.Pending)
                {
                    //All sanity in here
                    this.PendingWars.Remove(w);

                    if (g != null)
                    {
                        g.PendingWars.Remove(g.FindPendingWar(this));
                    }
                }
            }
        }

        public static void HandleDeath(Mobile victim)
        {
            HandleDeath(victim, null);
        }

        public static void HandleDeath(Mobile victim, Mobile killer)
        {
            if (!NewGuildSystem)
                return;

            if (killer == null)
                killer = victim.FindMostRecentDamager(false);

            if (killer == null || victim.Guild == null || killer.Guild == null)
                return;

            Guild victimGuild = GetAllianceLeader(victim.Guild as Guild);
            Guild killerGuild = GetAllianceLeader(killer.Guild as Guild);
			
            WarDeclaration war = killerGuild.FindActiveWar(victimGuild);

            if (war == null)
                return;
			
            war.Kills++;

            if (war.Opponent == victimGuild)
                killerGuild.CheckExpiredWars();
            else
                victimGuild.CheckExpiredWars();
        }

        #endregion

        #region Var declarations
        private Mobile m_Leader;

        private string m_Name;
        private string m_Abbreviation;

        private List<Guild> m_Allies;
        private List<Guild> m_Enemies;

        private List<Mobile> m_Members;

        private Item m_Guildstone;
        private Item m_Teleporter;

        private string m_Charter;
        private string m_Website;

        private DateTime m_LastFealty;

        private GuildType m_Type;
        private DateTime m_TypeLastChange;

        private List<Guild> m_AllyDeclarations, m_AllyInvitations;

        private List<Guild> m_WarDeclarations, m_WarInvitations;
        private List<Mobile> m_Candidates, m_Accepted;

        private List<WarDeclaration> m_PendingWars, m_AcceptedWars;

        private AllianceInfo m_AllianceInfo;
        private Guild m_AllianceLeader;
        #endregion

        public Guild(Mobile leader, string name, string abbreviation)
        {
            #region Ctor mumbo-jumbo
            this.m_Leader = leader;

            this.m_Members = new List<Mobile>();
            this.m_Allies = new List<Guild>();
            this.m_Enemies = new List<Guild>();
            this.m_WarDeclarations = new List<Guild>();
            this.m_WarInvitations = new List<Guild>();
            this.m_AllyDeclarations = new List<Guild>();
            this.m_AllyInvitations = new List<Guild>();
            this.m_Candidates = new List<Mobile>();
            this.m_Accepted = new List<Mobile>();

            this.m_LastFealty = DateTime.Now;

            this.m_Name = name;
            this.m_Abbreviation = abbreviation;

            this.m_TypeLastChange = DateTime.MinValue;

            this.AddMember(this.m_Leader);

            if (this.m_Leader is PlayerMobile)
                ((PlayerMobile)this.m_Leader).GuildRank = RankDefinition.Leader;

            this.m_AcceptedWars = new List<WarDeclaration>();
            this.m_PendingWars = new List<WarDeclaration>();
            #endregion
        }

        public Guild(int id)
            : base(id)//serialization ctor
        {
        }

        public void InvalidateMemberProperties()
        {
            this.InvalidateMemberProperties(false);
        }

        public void InvalidateMemberProperties(bool onlyOPL)
        {
            if (this.m_Members != null)
            {
                for (int i = 0; i < this.m_Members.Count; i++)
                {
                    Mobile m = this.m_Members[i];
                    m.InvalidateProperties();

                    if (!onlyOPL)
                        m.Delta(MobileDelta.Noto);
                }
            }
        }

        public void InvalidateMemberNotoriety()
        {
            if (this.m_Members != null)
            {
                for (int i = 0; i < this.m_Members.Count; i++)
                    this.m_Members[i].Delta(MobileDelta.Noto);
            }
        }
		
        public void InvalidateWarNotoriety()
        {
            Guild g = GetAllianceLeader(this);

            if (g.Alliance != null)
                g.Alliance.InvalidateMemberNotoriety();
            else
                g.InvalidateMemberNotoriety();
		
            if (g.AcceptedWars == null)
                return;

            foreach (WarDeclaration warDec in g.AcceptedWars)
            {
                Guild opponent = warDec.Opponent;
						
                if (opponent.Alliance != null)
                    opponent.Alliance.InvalidateMemberNotoriety();
                else
                    opponent.InvalidateMemberNotoriety();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Leader
        {
            get
            {
                if (this.m_Leader == null || this.m_Leader.Deleted || this.m_Leader.Guild != this)
                    this.CalculateGuildmaster();

                return this.m_Leader;
            }
            set
            {
                if (value != null)
                    this.AddMember(value); //Also removes from old guild.

                if (this.m_Leader is PlayerMobile && this.m_Leader.Guild == this)
                    ((PlayerMobile)this.m_Leader).GuildRank = RankDefinition.Member;

                this.m_Leader = value;

                if (this.m_Leader is PlayerMobile)
                    ((PlayerMobile)this.m_Leader).GuildRank = RankDefinition.Leader;
            }
        }

        public override bool Disbanded
        {
            get
            {
                return (this.m_Leader == null || this.m_Leader.Deleted);
            }
        }

        public override void OnDelete(Mobile mob)
        {
            this.RemoveMember(mob);
        }

        public void Disband()
        {
            this.m_Leader = null;

            BaseGuild.List.Remove(this.Id);

            foreach (Mobile m in this.m_Members)
            {
                m.SendLocalizedMessage(502131); // Your guild has disbanded.

                if (m is PlayerMobile)
                    ((PlayerMobile)m).GuildRank = RankDefinition.Lowest;

                m.Guild = null;
            }

            this.m_Members.Clear();

            for (int i = this.m_Allies.Count - 1; i >= 0; --i)
                if (i < this.m_Allies.Count)
                    this.RemoveAlly(this.m_Allies[i]);

            for (int i = this.m_Enemies.Count - 1; i >= 0; --i)
                if (i < this.m_Enemies.Count)
                    this.RemoveEnemy(this.m_Enemies[i]);

            if (!NewGuildSystem && this.m_Guildstone != null)
                this.m_Guildstone.Delete();

            this.m_Guildstone = null;

            this.CheckExpiredWars();

            this.Alliance = null;
        }

        #region Is<something>(...)
        public bool IsMember(Mobile m)
        {
            return this.m_Members.Contains(m);
        }

        public bool IsAlly(Guild g)
        {
            if (NewGuildSystem)
            {
                return (this.Alliance != null && this.Alliance.IsMember(this) && this.Alliance.IsMember(g));
            }

            return this.m_Allies.Contains(g);
        }

        public bool IsEnemy(Guild g)
        {
            if (NewGuildSystem)
                return this.IsWar(g);

            if (this.m_Type != GuildType.Regular && g.m_Type != GuildType.Regular && this.m_Type != g.m_Type)
                return true;

            return this.m_Enemies.Contains(g);
        }

        public bool IsWar(Guild g)
        {
            if (g == null)
                return false;

            if (NewGuildSystem)
            {
                Guild guild = GetAllianceLeader(this);
                Guild otherGuild = GetAllianceLeader(g);
				
                if (guild.FindActiveWar(otherGuild) != null)
                    return true;

                return false;
            }

            return this.m_Enemies.Contains(g);
        }

        #endregion

        #region Serialization
        public override void Serialize(GenericWriter writer)
        {
            if (this.LastFealty + TimeSpan.FromDays(1.0) < DateTime.Now)
                this.CalculateGuildmaster();

            this.CheckExpiredWars();

            if (this.Alliance != null)
                this.Alliance.CheckLeader();

            writer.Write((int)5);//version

            #region War Serialization
            writer.Write(this.m_PendingWars.Count);

            for (int i = 0; i < this.m_PendingWars.Count; i++)
            {
                this.m_PendingWars[i].Serialize(writer);
            }

            writer.Write(this.m_AcceptedWars.Count);

            for (int i = 0; i < this.m_AcceptedWars.Count; i++)
            {
                this.m_AcceptedWars[i].Serialize(writer);
            }
            #endregion

            #region Alliances

            bool isAllianceLeader = (this.m_AllianceLeader == null && this.m_AllianceInfo != null);
            writer.Write(isAllianceLeader);

            if (isAllianceLeader)
                this.m_AllianceInfo.Serialize(writer);
            else
                writer.Write(this.m_AllianceLeader);

            #endregion

            //

            writer.WriteGuildList(this.m_AllyDeclarations, true);
            writer.WriteGuildList(this.m_AllyInvitations, true);

            writer.Write(this.m_TypeLastChange);

            writer.Write((int)this.m_Type);

            writer.Write(this.m_LastFealty);

            writer.Write(this.m_Leader);
            writer.Write(this.m_Name);
            writer.Write(this.m_Abbreviation);

            writer.WriteGuildList<Guild>(this.m_Allies, true);
            writer.WriteGuildList<Guild>(this.m_Enemies, true);
            writer.WriteGuildList(this.m_WarDeclarations, true);
            writer.WriteGuildList(this.m_WarInvitations, true);

            writer.Write(this.m_Members, true);
            writer.Write(this.m_Candidates, true);
            writer.Write(this.m_Accepted, true);

            writer.Write(this.m_Guildstone);
            writer.Write(this.m_Teleporter);

            writer.Write(this.m_Charter);
            writer.Write(this.m_Website);
        }

        public override void Deserialize(GenericReader reader)
        {
            int version = reader.ReadInt();

            switch ( version )
            {
                case 5:
                    {
                        int count = reader.ReadInt();

                        this.m_PendingWars = new List<WarDeclaration>();
                        for (int i = 0; i < count; i++)
                        {
                            this.m_PendingWars.Add(new WarDeclaration(reader));
                        }

                        count = reader.ReadInt();
                        this.m_AcceptedWars = new List<WarDeclaration>();
                        for (int i = 0; i < count; i++)
                        {
                            this.m_AcceptedWars.Add(new WarDeclaration(reader));
                        }

                        bool isAllianceLeader = reader.ReadBool();

                        if (isAllianceLeader)
                            this.m_AllianceInfo = new AllianceInfo(reader);
                        else
                            this.m_AllianceLeader = reader.ReadGuild() as Guild;

                        goto case 4;
                    }
                case 4:
                    {
                        this.m_AllyDeclarations = reader.ReadStrongGuildList<Guild>();
                        this.m_AllyInvitations = reader.ReadStrongGuildList<Guild>();

                        goto case 3;
                    }
                case 3:
                    {
                        this.m_TypeLastChange = reader.ReadDateTime();

                        goto case 2;
                    }
                case 2:
                    {
                        this.m_Type = (GuildType)reader.ReadInt();

                        goto case 1;
                    }
                case 1:
                    {
                        this.m_LastFealty = reader.ReadDateTime();

                        goto case 0;
                    }
                case 0:
                    {
                        this.m_Leader = reader.ReadMobile();

                        if (this.m_Leader is PlayerMobile)
                            ((PlayerMobile)this.m_Leader).GuildRank = RankDefinition.Leader;

                        this.m_Name = reader.ReadString();
                        this.m_Abbreviation = reader.ReadString();

                        this.m_Allies = reader.ReadStrongGuildList<Guild>();
                        this.m_Enemies = reader.ReadStrongGuildList<Guild>();
                        this.m_WarDeclarations = reader.ReadStrongGuildList<Guild>();
                        this.m_WarInvitations = reader.ReadStrongGuildList<Guild>();

                        this.m_Members = reader.ReadStrongMobileList();
                        this.m_Candidates = reader.ReadStrongMobileList();
                        this.m_Accepted = reader.ReadStrongMobileList(); 

                        this.m_Guildstone = reader.ReadItem();
                        this.m_Teleporter = reader.ReadItem();

                        this.m_Charter = reader.ReadString();
                        this.m_Website = reader.ReadString();

                        break;
                    }
            }

            if (this.m_AllyDeclarations == null)
                this.m_AllyDeclarations = new List<Guild>();

            if (this.m_AllyInvitations == null)
                this.m_AllyInvitations = new List<Guild>();

            if (this.m_AcceptedWars == null)
                this.m_AcceptedWars = new List<WarDeclaration>();

            if (this.m_PendingWars == null)
                this.m_PendingWars = new List<WarDeclaration>();

            /*
            if ( ( !NewGuildSystem && m_Guildstone == null )|| m_Members.Count == 0 )
            Disband();
            */

            Timer.DelayCall(TimeSpan.Zero, new TimerCallback(VerifyGuild_Callback));
        }

        private void VerifyGuild_Callback()
        {
            if ((!NewGuildSystem && this.m_Guildstone == null) || this.m_Members.Count == 0)
                this.Disband();

            this.CheckExpiredWars();

            AllianceInfo alliance = this.Alliance;

            if (alliance != null)
                alliance.CheckLeader();

            alliance = this.Alliance;	//CheckLeader could possibly change the value of this.Alliance

            if (alliance != null && !alliance.IsMember(this) && !alliance.IsPendingMember(this))	//This block is there to fix a bug in the code in an older version.  
                this.Alliance = null;	//Will call Alliance.RemoveGuild which will set it null & perform all the pertient checks as far as alliacne disbanding
        }

        #endregion

        #region Add/Remove Member/Old Ally/Old Enemy
        public void AddMember(Mobile m)
        {
            if (!this.m_Members.Contains(m))
            {
                if (m.Guild != null && m.Guild != this)
                    ((Guild)m.Guild).RemoveMember(m);

                this.m_Members.Add(m);
                m.Guild = this;

                if (!NewGuildSystem)
                    m.GuildFealty = this.m_Leader;
                else
                    m.GuildFealty = null;

                if (m is PlayerMobile)
                    ((PlayerMobile)m).GuildRank = RankDefinition.Lowest;
				
                Guild guild = m.Guild as Guild;

                if (guild != null)
                    guild.InvalidateWarNotoriety();
            }
        }

        public void RemoveMember(Mobile m)
        {
            this.RemoveMember(m, 1018028); // You have been dismissed from your guild.
        }

        public void RemoveMember(Mobile m, int message)
        {
            if (this.m_Members.Contains(m))
            {
                this.m_Members.Remove(m);
				
                Guild guild = m.Guild as Guild;
				
                m.Guild = null;

                if (m is PlayerMobile)
                    ((PlayerMobile)m).GuildRank = RankDefinition.Lowest;

                if (message > 0)
                    m.SendLocalizedMessage(message);

                if (m == this.m_Leader)
                {
                    this.CalculateGuildmaster();

                    if (this.m_Leader == null)
                        this.Disband();
                }

                if (this.m_Members.Count == 0)
                    this.Disband();
				
                if (guild != null)
                    guild.InvalidateWarNotoriety();
				
                m.Delta(MobileDelta.Noto);
            }
        }

        public void AddAlly(Guild g)
        {
            if (!this.m_Allies.Contains(g))
            {
                this.m_Allies.Add(g);

                g.AddAlly(this);
            }
        }

        public void RemoveAlly(Guild g)
        {
            if (this.m_Allies.Contains(g))
            {
                this.m_Allies.Remove(g);

                g.RemoveAlly(this);
            }
        }

        public void AddEnemy(Guild g)
        {
            if (!this.m_Enemies.Contains(g))
            {
                this.m_Enemies.Add(g);

                g.AddEnemy(this);
            }
        }

        public void RemoveEnemy(Guild g)
        {
            if (this.m_Enemies != null && this.m_Enemies.Contains(g))
            {
                this.m_Enemies.Remove(g);

                g.RemoveEnemy(this);
            }
        }

        #endregion

        #region Guild[Text]Message(...)
        public void GuildMessage(int num, bool append, string format, params object[] args)
        {
            this.GuildMessage(num, append, String.Format(format, args));
        }

        public void GuildMessage(int number)
        {
            for (int i = 0; i < this.m_Members.Count; ++i)
                this.m_Members[i].SendLocalizedMessage(number);
        }

        public void GuildMessage(int number, string args)
        {
            this.GuildMessage(number, args, 0x3B2);
        }

        public void GuildMessage(int number, string args, int hue)
        {
            for (int i = 0; i < this.m_Members.Count; ++i)
                this.m_Members[i].SendLocalizedMessage(number, args, hue);
        }

        public void GuildMessage(int number, bool append, string affix)
        {
            this.GuildMessage(number, append, affix, "", 0x3B2);
        }

        public void GuildMessage(int number, bool append, string affix, string args)
        {
            this.GuildMessage(number, append, affix, args, 0x3B2);
        }

        public void GuildMessage(int number, bool append, string affix, string args, int hue)
        {
            for (int i = 0; i < this.m_Members.Count; ++i)
                this.m_Members[i].SendLocalizedMessage(number, append, affix, args, hue);
        }

        public void GuildTextMessage(string text)
        {
            this.GuildTextMessage(0x3B2, text);
        }

        public void GuildTextMessage(string format, params object[] args)
        {
            this.GuildTextMessage(0x3B2, String.Format(format, args));
        }

        public void GuildTextMessage(int hue, string text)
        {
            for (int i = 0; i < this.m_Members.Count; ++i)
                this.m_Members[i].SendMessage(hue, text);
        }

        public void GuildTextMessage(int hue, string format, params object[] args)
        {
            this.GuildTextMessage(hue, String.Format(format, args));
        }

        public void GuildChat(Mobile from, int hue, string text)
        {
            Packet p = null;
            for (int i = 0; i < this.m_Members.Count; i++)
            {
                Mobile m = this.m_Members[i];

                NetState state = m.NetState;

                if (state != null)
                {
                    if (p == null)
                        p = Packet.Acquire(new UnicodeMessage(from.Serial, from.Body, MessageType.Guild, hue, 3, from.Language, from.Name, text));

                    state.Send(p);
                }
            }

            Packet.Release(p);
        }

        public void GuildChat(Mobile from, string text)
        {
            PlayerMobile pm = from as PlayerMobile;

            this.GuildChat(from, (pm == null) ? 0x3B2 : pm.GuildMessageHue, text);
        }

        #endregion

        #region Voting
        public bool CanVote(Mobile m)
        {
            if (NewGuildSystem)
            {
                PlayerMobile pm = m as PlayerMobile;
                if (pm == null || !pm.GuildRank.GetFlag(RankFlags.CanVote))
                    return false;
            }

            return (m != null && !m.Deleted && m.Guild == this);
        }

        public bool CanBeVotedFor(Mobile m)
        {
            if (NewGuildSystem)
            {
                PlayerMobile pm = m as PlayerMobile;
                if (pm == null || pm.LastOnline + InactiveTime < DateTime.Now)
                    return false;
            }

            return (m != null && !m.Deleted && m.Guild == this);
        }

        public void CalculateGuildmaster()
        {
            Dictionary<Mobile, int> votes = new Dictionary<Mobile, int>();

            int votingMembers = 0;

            for (int i = 0; this.m_Members != null && i < this.m_Members.Count; ++i)
            {
                Mobile memb = this.m_Members[i];

                if (!this.CanVote(memb))
                    continue;

                Mobile m = memb.GuildFealty;

                if (!this.CanBeVotedFor(m))
                {
                    if (this.m_Leader != null && !this.m_Leader.Deleted && this.m_Leader.Guild == this)
                        m = this.m_Leader;
                    else 
                        m = memb;
                }

                if (m == null)
                    continue;

                int v;

                if (!votes.TryGetValue(m, out v))
                    votes[m] = 1;
                else
                    votes[m] = v + 1;
				
                votingMembers++;
            }

            Mobile winner = null;
            int highVotes = 0;

            foreach (KeyValuePair<Mobile, int> kvp in votes)
            {
                Mobile m = (Mobile)kvp.Key;
                int val = (int)kvp.Value;

                if (winner == null || val > highVotes)
                {
                    winner = m;
                    highVotes = val;
                }
            }

            if (NewGuildSystem && (highVotes * 100) / Math.Max(votingMembers, 1) < MajorityPercentage && this.m_Leader != null && winner != this.m_Leader && !this.m_Leader.Deleted && this.m_Leader.Guild == this)
                winner = this.m_Leader;

            if (this.m_Leader != winner && winner != null)
                this.GuildMessage(1018015, true, winner.Name); // Guild Message: Guildmaster changed to:

            this.Leader = winner;
            this.m_LastFealty = DateTime.Now;
        }

        #endregion

        #region Getters & Setters
        [CommandProperty(AccessLevel.GameMaster)]
        public Item Guildstone
        {
            get
            {
                return this.m_Guildstone;
            }
            set
            {
                this.m_Guildstone = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Item Teleporter
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

        [CommandProperty(AccessLevel.GameMaster)]
        public override string Name
        {
            get
            {
                return this.m_Name;
            }
            set
            {
                this.m_Name = value;

                this.InvalidateMemberProperties(true);

                if (this.m_Guildstone != null)
                    this.m_Guildstone.InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Website
        {
            get
            {
                return this.m_Website;
            }
            set
            {
                this.m_Website = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public override string Abbreviation
        {
            get
            {
                return this.m_Abbreviation;
            }
            set
            {
                this.m_Abbreviation = value;

                this.InvalidateMemberProperties(true);

                if (this.m_Guildstone != null)
                    this.m_Guildstone.InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Charter
        {
            get
            {
                return this.m_Charter;
            }
            set
            {
                this.m_Charter = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public override GuildType Type
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
                    this.m_TypeLastChange = DateTime.Now;

                    this.InvalidateMemberProperties();
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime LastFealty
        {
            get
            {
                return this.m_LastFealty;
            }
            set
            {
                this.m_LastFealty = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime TypeLastChange
        {
            get
            {
                return this.m_TypeLastChange;
            }
        }

        public List<Guild> Allies
        {
            get
            {
                return this.m_Allies;
            }
        }

        public List<Guild> Enemies
        {
            get
            {
                return this.m_Enemies;
            }
        }

        public List<Guild> AllyDeclarations
        {
            get
            {
                return this.m_AllyDeclarations;
            }
        }

        public List<Guild> AllyInvitations
        {
            get
            {
                return this.m_AllyInvitations;
            }
        }

        public List<Guild> WarDeclarations
        {
            get
            {
                return this.m_WarDeclarations;
            }
        }

        public List<Guild> WarInvitations
        {
            get
            {
                return this.m_WarInvitations;
            }
        }

        public List<Mobile> Candidates
        {
            get
            {
                return this.m_Candidates;
            }
        }

        public List<Mobile> Accepted
        {
            get
            {
                return this.m_Accepted;
            }
        }

        public List<Mobile> Members
        {
            get
            {
                return this.m_Members;
            }
        }
        #endregion
    }
}