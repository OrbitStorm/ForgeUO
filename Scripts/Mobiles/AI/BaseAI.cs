using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Server.ContextMenus;
using Server.Engines.Quests;
using Server.Engines.Quests.Necro;
using Server.Engines.XmlSpawner2;
using Server.Items;
using Server.Network;
using Server.Regions;
using Server.Spells;
using Server.Spells.Spellweaving;
using Server.Targets;
using MoveImpl = Server.Movement.MovementImpl;

namespace Server.Mobiles
{
    public enum AIType
    {
        AI_Use_Default,
        AI_Melee,
        AI_Animal,
        AI_Archer,
        AI_Healer,
        AI_Vendor,
        AI_Mage,
        AI_Berserk,
        AI_Predator,
        AI_Thief,
        AI_NecroMage,
        AI_OrcScout,
        AI_Spellbinder,
        AI_OmniAI
    }

    public enum ActionType
    {
        Wander,
        Combat,
        Guard,
        Flee,
        Backoff,
        Interact
    }

    public abstract class BaseAI
    {
        public Timer m_Timer;
        protected ActionType m_Action;
        private DateTime m_NextStopGuard;

        public BaseCreature m_Mobile;

        public BaseAI(BaseCreature m)
        {
            this.m_Mobile = m;

            this.m_Timer = new AITimer(this);

            bool activate;

            if (!m.PlayerRangeSensitive)
                activate = true;
            else if (World.Loading)
                activate = false;
            else if (m.Map == null || m.Map == Map.Internal || !m.Map.GetSector(m).Active)
                activate = false;
            else
                activate = true;

            if (activate)
                this.m_Timer.Start();

            this.Action = ActionType.Wander;
        }

        public ActionType Action
        {
            get
            {
                return this.m_Action;
            }
            set
            {
                this.m_Action = value;
                this.OnActionChanged();
            }
        }

        public virtual bool WasNamed(string speech)
        {
            string name = this.m_Mobile.Name;

            return (name != null && Insensitive.StartsWith(speech, name));
        }

        private class InternalEntry : ContextMenuEntry
        {
            private readonly Mobile m_From;
            private readonly BaseCreature m_Mobile;
            private readonly BaseAI m_AI;
            private readonly OrderType m_Order;

            public InternalEntry(Mobile from, int number, int range, BaseCreature mobile, BaseAI ai, OrderType order)
                : base(number, range)
            {
                this.m_From = from;
                this.m_Mobile = mobile;
                this.m_AI = ai;
                this.m_Order = order;

                if (mobile.IsDeadPet && (order == OrderType.Guard || order == OrderType.Attack || order == OrderType.Transfer || order == OrderType.Drop))
                    this.Enabled = false;
            }

            public override void OnClick()
            {
                if (!this.m_Mobile.Deleted && this.m_Mobile.Controlled && this.m_From.CheckAlive())
                {
                    if (this.m_Mobile.IsDeadPet && (this.m_Order == OrderType.Guard || this.m_Order == OrderType.Attack || this.m_Order == OrderType.Transfer || this.m_Order == OrderType.Drop))
                        return;

                    bool isOwner = (this.m_From == this.m_Mobile.ControlMaster);
                    bool isFriend = (!isOwner && this.m_Mobile.IsPetFriend(this.m_From));

                    if (!isOwner && !isFriend)
                        return;
                    else if (isFriend && this.m_Order != OrderType.Follow && this.m_Order != OrderType.Stay && this.m_Order != OrderType.Stop)
                        return;

                    switch( this.m_Order )
                    {
                        case OrderType.Follow:
                        case OrderType.Attack:
                        case OrderType.Transfer:
                        case OrderType.Friend:
                        case OrderType.Unfriend:
                            {
                                if (this.m_Order == OrderType.Transfer && this.m_From.HasTrade)
                                    this.m_From.SendLocalizedMessage(1010507); // You cannot transfer a pet with a trade pending
                                else if (this.m_Order == OrderType.Friend && this.m_From.HasTrade)
                                    this.m_From.SendLocalizedMessage(1070947); // You cannot friend a pet with a trade pending
                                else
                                    this.m_AI.BeginPickTarget(this.m_From, this.m_Order);

                                break;
                            }
                        case OrderType.Release:
                            {
                                if (this.m_Mobile.Summoned)
                                    goto default;
                                else
                                    this.m_From.SendGump(new Gumps.ConfirmReleaseGump(this.m_From, this.m_Mobile));

                                break;
                            }
                        default:
                            {
                                if (this.m_Mobile.CheckControlChance(this.m_From))
                                    this.m_Mobile.ControlOrder = this.m_Order;

                                break;
                            }
                    }
                }
            }
        }

        public virtual void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            if (from.Alive && this.m_Mobile.Controlled && from.InRange(this.m_Mobile, 14))
            {
                if (from == this.m_Mobile.ControlMaster)
                {
                    list.Add(new InternalEntry(from, 6107, 14, this.m_Mobile, this, OrderType.Guard));  // Command: Guard
                    list.Add(new InternalEntry(from, 6108, 14, this.m_Mobile, this, OrderType.Follow)); // Command: Follow

                    if (this.m_Mobile.CanDrop)
                        list.Add(new InternalEntry(from, 6109, 14, this.m_Mobile, this, OrderType.Drop));   // Command: Drop

                    list.Add(new InternalEntry(from, 6111, 14, this.m_Mobile, this, OrderType.Attack)); // Command: Kill

                    list.Add(new InternalEntry(from, 6112, 14, this.m_Mobile, this, OrderType.Stop));   // Command: Stop
                    list.Add(new InternalEntry(from, 6114, 14, this.m_Mobile, this, OrderType.Stay));   // Command: Stay

                    if (!this.m_Mobile.Summoned && !(this.m_Mobile is GrizzledMare))
                    {
                        list.Add(new InternalEntry(from, 6110, 14, this.m_Mobile, this, OrderType.Friend)); // Add Friend
                        list.Add(new InternalEntry(from, 6099, 14, this.m_Mobile, this, OrderType.Unfriend)); // Remove Friend
                        list.Add(new InternalEntry(from, 6113, 14, this.m_Mobile, this, OrderType.Transfer)); // Transfer
                    }

                    list.Add(new InternalEntry(from, 6118, 14, this.m_Mobile, this, OrderType.Release)); // Release
                }
                else if (this.m_Mobile.IsPetFriend(from))
                {
                    list.Add(new InternalEntry(from, 6108, 14, this.m_Mobile, this, OrderType.Follow)); // Command: Follow
                    list.Add(new InternalEntry(from, 6112, 14, this.m_Mobile, this, OrderType.Stop));   // Command: Stop
                    list.Add(new InternalEntry(from, 6114, 14, this.m_Mobile, this, OrderType.Stay));   // Command: Stay
                }
            }
        }

        public virtual void BeginPickTarget(Mobile from, OrderType order)
        {
            if (this.m_Mobile.Deleted || !this.m_Mobile.Controlled || !from.InRange(this.m_Mobile, 14) || from.Map != this.m_Mobile.Map)
                return;

            bool isOwner = (from == this.m_Mobile.ControlMaster);
            bool isFriend = (!isOwner && this.m_Mobile.IsPetFriend(from));

            if (!isOwner && !isFriend)
                return;
            else if (isFriend && order != OrderType.Follow && order != OrderType.Stay && order != OrderType.Stop)
                return;

            if (from.Target == null)
            {
                if (order == OrderType.Transfer)
                    from.SendLocalizedMessage(502038); // Click on the person to transfer ownership to.
                else if (order == OrderType.Friend)
                    from.SendLocalizedMessage(502020); // Click on the player whom you wish to make a co-owner.
                else if (order == OrderType.Unfriend)
                    from.SendLocalizedMessage(1070948); // Click on the player whom you wish to remove as a co-owner.

                from.Target = new AIControlMobileTarget(this, order);
            }
            else if (from.Target is AIControlMobileTarget)
            {
                AIControlMobileTarget t = (AIControlMobileTarget)from.Target;

                if (t.Order == order)
                    t.AddAI(this);
            }
        }

        public virtual void OnAggressiveAction(Mobile aggressor)
        {
            Mobile currentCombat = this.m_Mobile.Combatant;

            if (currentCombat != null && !aggressor.Hidden && currentCombat != aggressor && this.m_Mobile.GetDistanceToSqrt(currentCombat) > this.m_Mobile.GetDistanceToSqrt(aggressor))
                this.m_Mobile.Combatant = aggressor;
        }

        public virtual void EndPickTarget(Mobile from, Mobile target, OrderType order)
        {
            if (this.m_Mobile.Deleted || !this.m_Mobile.Controlled || !from.InRange(this.m_Mobile, 14) || from.Map != this.m_Mobile.Map || !from.CheckAlive())
                return;

            bool isOwner = (from == this.m_Mobile.ControlMaster);
            bool isFriend = (!isOwner && this.m_Mobile.IsPetFriend(from));

            if (!isOwner && !isFriend)
                return;
            else if (isFriend && order != OrderType.Follow && order != OrderType.Stay && order != OrderType.Stop)
                return;

            if (order == OrderType.Attack)
            {
                if (target is BaseCreature && ((BaseCreature)target).IsScaryToPets && this.m_Mobile.IsScaredOfScaryThings)
                {
                    this.m_Mobile.SayTo(from, "Your pet refuses to attack this creature!");
                    return;
                }

                if ((SolenHelper.CheckRedFriendship(from) &&
                     (target is RedSolenInfiltratorQueen ||
                      target is RedSolenInfiltratorWarrior ||
                      target is RedSolenQueen ||
                      target is RedSolenWarrior ||
                      target is RedSolenWorker)) ||
                    (SolenHelper.CheckBlackFriendship(from) &&
                     (target is BlackSolenInfiltratorQueen ||
                      target is BlackSolenInfiltratorWarrior ||
                      target is BlackSolenQueen ||
                      target is BlackSolenWarrior ||
                      target is BlackSolenWorker)))
                {
                    from.SendAsciiMessage("You can not force your pet to attack a creature you are protected from.");
                    return;
                }

                if (target is Factions.BaseFactionGuard)
                {
                    this.m_Mobile.SayTo(from, "Your pet refuses to attack the guard.");
                    return;
                }
            }

            if (this.m_Mobile.CheckControlChance(from))
            {
                this.m_Mobile.ControlTarget = target;
                this.m_Mobile.ControlOrder = order;
            }
        }

        public virtual bool HandlesOnSpeech(Mobile from)
        {
            if (from.AccessLevel >= AccessLevel.GameMaster)
                return true;

            if (from.Alive && this.m_Mobile.Controlled && this.m_Mobile.Commandable && (from == this.m_Mobile.ControlMaster || this.m_Mobile.IsPetFriend(from)))
                return true;

            return (from.Alive && from.InRange(this.m_Mobile.Location, 3) && this.m_Mobile.IsHumanInTown());
        }

        private static readonly SkillName[] m_KeywordTable = new SkillName[]
        {
            SkillName.Parry,
            SkillName.Healing,
            SkillName.Hiding,
            SkillName.Stealing,
            SkillName.Alchemy,
            SkillName.AnimalLore,
            SkillName.ItemID,
            SkillName.ArmsLore,
            SkillName.Begging,
            SkillName.Blacksmith,
            SkillName.Fletching,
            SkillName.Peacemaking,
            SkillName.Camping,
            SkillName.Carpentry,
            SkillName.Cartography,
            SkillName.Cooking,
            SkillName.DetectHidden,
            SkillName.Discordance, //??
            SkillName.EvalInt,
            SkillName.Fishing,
            SkillName.Provocation,
            SkillName.Lockpicking,
            SkillName.Magery,
            SkillName.MagicResist,
            SkillName.Tactics,
            SkillName.Snooping,
            SkillName.RemoveTrap,
            SkillName.Musicianship,
            SkillName.Poisoning,
            SkillName.Archery,
            SkillName.SpiritSpeak,
            SkillName.Tailoring,
            SkillName.AnimalTaming,
            SkillName.TasteID,
            SkillName.Tinkering,
            SkillName.Veterinary,
            SkillName.Forensics,
            SkillName.Herding,
            SkillName.Tracking,
            SkillName.Stealth,
            SkillName.Inscribe,
            SkillName.Swords,
            SkillName.Macing,
            SkillName.Fencing,
            SkillName.Wrestling,
            SkillName.Lumberjacking,
            SkillName.Mining,
            SkillName.Meditation
        };

        public virtual void OnSpeech(SpeechEventArgs e)
        {
            if (e.Mobile.Alive && e.Mobile.InRange(this.m_Mobile.Location, 3) && this.m_Mobile.IsHumanInTown())
            {
                if (e.HasKeyword(0x9D) && this.WasNamed(e.Speech)) // *move*
                {
                    if (this.m_Mobile.Combatant != null)
                    {
                        // I am too busy fighting to deal with thee!
                        this.m_Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, 501482);
                    }
                    else
                    {
                        // Excuse me?
                        this.m_Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, 501516);
                        this.WalkRandomInHome(2, 2, 1);
                    }
                }
                else if (e.HasKeyword(0x9E) && this.WasNamed(e.Speech)) // *time*
                {
                    if (this.m_Mobile.Combatant != null)
                    {
                        // I am too busy fighting to deal with thee!
                        this.m_Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, 501482);
                    }
                    else
                    {
                        int generalNumber;
                        string exactTime;

                        Clock.GetTime(this.m_Mobile, out generalNumber, out exactTime);

                        this.m_Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, generalNumber);
                    }
                }
                else if (e.HasKeyword(0x6C) && this.WasNamed(e.Speech)) // *train
                {
                    if (this.m_Mobile.Combatant != null)
                    {
                        // I am too busy fighting to deal with thee!
                        this.m_Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, 501482);
                    }
                    else
                    {
                        bool foundSomething = false;

                        Skills ourSkills = this.m_Mobile.Skills;
                        Skills theirSkills = e.Mobile.Skills;

                        for (int i = 0; i < ourSkills.Length && i < theirSkills.Length; ++i)
                        {
                            Skill skill = ourSkills[i];
                            Skill theirSkill = theirSkills[i];

                            if (skill != null && theirSkill != null && skill.Base >= 60.0 && this.m_Mobile.CheckTeach(skill.SkillName, e.Mobile))
                            {
                                double toTeach = skill.Base / 3.0;

                                if (toTeach > 42.0)
                                    toTeach = 42.0;

                                if (toTeach > theirSkill.Base)
                                {
                                    int number = 1043059 + i;

                                    if (number > 1043107)
                                        continue;

                                    if (!foundSomething)
                                        this.m_Mobile.Say(1043058); // I can train the following:

                                    this.m_Mobile.Say(number);

                                    foundSomething = true;
                                }
                            }
                        }

                        if (!foundSomething)
                            this.m_Mobile.Say(501505); // Alas, I cannot teach thee anything.
                    }
                }
                else
                {
                    SkillName toTrain = (SkillName)(-1);

                    for (int i = 0; toTrain == (SkillName)(-1) && i < e.Keywords.Length; ++i)
                    {
                        int keyword = e.Keywords[i];

                        if (keyword == 0x154)
                        {
                            toTrain = SkillName.Anatomy;
                        }
                        else if (keyword >= 0x6D && keyword <= 0x9C)
                        {
                            int index = keyword - 0x6D;

                            if (index >= 0 && index < m_KeywordTable.Length)
                                toTrain = m_KeywordTable[index];
                        }
                    }

                    if (toTrain != (SkillName)(-1) && this.WasNamed(e.Speech))
                    {
                        if (this.m_Mobile.Combatant != null)
                        {
                            // I am too busy fighting to deal with thee!
                            this.m_Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, 501482);
                        }
                        else
                        {
                            Skills skills = this.m_Mobile.Skills;
                            Skill skill = skills[toTrain];

                            if (skill == null || skill.Base < 60.0 || !this.m_Mobile.CheckTeach(toTrain, e.Mobile))
                            {
                                this.m_Mobile.Say(501507); // 'Tis not something I can teach thee of.
                            }
                            else
                            {
                                this.m_Mobile.Teach(toTrain, e.Mobile, 0, false);
                            }
                        }
                    }
                }
            }

            if (this.m_Mobile.Controlled && this.m_Mobile.Commandable)
            {
                this.m_Mobile.DebugSay("Listening...");

                bool isOwner = (e.Mobile == this.m_Mobile.ControlMaster);
                bool isFriend = (!isOwner && this.m_Mobile.IsPetFriend(e.Mobile));

                if (e.Mobile.Alive && (isOwner || isFriend))
                {
                    this.m_Mobile.DebugSay("It's from my master");

                    int[] keywords = e.Keywords;
                    string speech = e.Speech;

                    // First, check the all*
                    for (int i = 0; i < keywords.Length; ++i)
                    {
                        int keyword = keywords[i];

                        switch( keyword )
                        {
                            case 0x164: // all come
                                {
                                    if (!isOwner)
                                        break;

                                    if (this.m_Mobile.CheckControlChance(e.Mobile))
                                    {
                                        this.m_Mobile.ControlTarget = null;
                                        this.m_Mobile.ControlOrder = OrderType.Come;
                                    }

                                    return;
                                }
                            case 0x165: // all follow
                                {
                                    this.BeginPickTarget(e.Mobile, OrderType.Follow);
                                    return;
                                }
                            case 0x166: // all guard
                            case 0x16B: // all guard me
                                {
                                    if (!isOwner)
                                        break;

                                    if (this.m_Mobile.CheckControlChance(e.Mobile))
                                    {
                                        this.m_Mobile.ControlTarget = null;
                                        this.m_Mobile.ControlOrder = OrderType.Guard;
                                    }
                                    return;
                                }
                            case 0x167: // all stop
                                {
                                    if (this.m_Mobile.CheckControlChance(e.Mobile))
                                    {
                                        this.m_Mobile.ControlTarget = null;
                                        this.m_Mobile.ControlOrder = OrderType.Stop;
                                    }
                                    return;
                                }
                            case 0x168: // all kill
                            case 0x169: // all attack
                                {
                                    if (!isOwner)
                                        break;

                                    this.BeginPickTarget(e.Mobile, OrderType.Attack);
                                    return;
                                }
                            case 0x16C: // all follow me
                                {
                                    if (this.m_Mobile.CheckControlChance(e.Mobile))
                                    {
                                        this.m_Mobile.ControlTarget = e.Mobile;
                                        this.m_Mobile.ControlOrder = OrderType.Follow;
                                    }
                                    return;
                                }
                            case 0x170: // all stay
                                {
                                    if (this.m_Mobile.CheckControlChance(e.Mobile))
                                    {
                                        this.m_Mobile.ControlTarget = null;
                                        this.m_Mobile.ControlOrder = OrderType.Stay;
                                    }
                                    return;
                                }
                        }
                    }

                    // No all*, so check *command
                    for (int i = 0; i < keywords.Length; ++i)
                    {
                        int keyword = keywords[i];

                        switch( keyword )
                        {
                            case 0x155: // *come
                                {
                                    if (!isOwner)
                                        break;

                                    if (this.WasNamed(speech) && this.m_Mobile.CheckControlChance(e.Mobile))
                                    {
                                        this.m_Mobile.ControlTarget = null;
                                        this.m_Mobile.ControlOrder = OrderType.Come;
                                    }

                                    return;
                                }
                            case 0x156: // *drop
                                {
                                    if (!isOwner)
                                        break;

                                    if (!this.m_Mobile.IsDeadPet && !this.m_Mobile.Summoned && this.WasNamed(speech) && this.m_Mobile.CheckControlChance(e.Mobile))
                                    {
                                        this.m_Mobile.ControlTarget = null;
                                        this.m_Mobile.ControlOrder = OrderType.Drop;
                                    }

                                    return;
                                }
                            case 0x15A: // *follow
                                {
                                    if (this.WasNamed(speech) && this.m_Mobile.CheckControlChance(e.Mobile))
                                        this.BeginPickTarget(e.Mobile, OrderType.Follow);

                                    return;
                                }
                            case 0x15B: // *friend
                                {
                                    if (!isOwner)
                                        break;

                                    if (this.WasNamed(speech) && this.m_Mobile.CheckControlChance(e.Mobile))
                                    {
                                        if (this.m_Mobile.Summoned || (this.m_Mobile is GrizzledMare))
                                            e.Mobile.SendLocalizedMessage(1005481); // Summoned creatures are loyal only to their summoners.
                                        else if (e.Mobile.HasTrade)
                                            e.Mobile.SendLocalizedMessage(1070947); // You cannot friend a pet with a trade pending
                                        else
                                            this.BeginPickTarget(e.Mobile, OrderType.Friend);
                                    }

                                    return;
                                }
                            case 0x15C: // *guard
                                {
                                    if (!isOwner)
                                        break;

                                    if (!this.m_Mobile.IsDeadPet && this.WasNamed(speech) && this.m_Mobile.CheckControlChance(e.Mobile))
                                    {
                                        this.m_Mobile.ControlTarget = null;
                                        this.m_Mobile.ControlOrder = OrderType.Guard;
                                    }

                                    return;
                                }
                            case 0x15D: // *kill
                            case 0x15E: // *attack
                                {
                                    if (!isOwner)
                                        break;

                                    if (!this.m_Mobile.IsDeadPet && this.WasNamed(speech) && this.m_Mobile.CheckControlChance(e.Mobile))
                                        this.BeginPickTarget(e.Mobile, OrderType.Attack);

                                    return;
                                }
                            case 0x15F: // *patrol
                                {
                                    if (!isOwner)
                                        break;

                                    if (this.WasNamed(speech) && this.m_Mobile.CheckControlChance(e.Mobile))
                                    {
                                        this.m_Mobile.ControlTarget = null;
                                        this.m_Mobile.ControlOrder = OrderType.Patrol;
                                    }

                                    return;
                                }
                            case 0x161: // *stop
                                {
                                    if (this.WasNamed(speech) && this.m_Mobile.CheckControlChance(e.Mobile))
                                    {
                                        this.m_Mobile.ControlTarget = null;
                                        this.m_Mobile.ControlOrder = OrderType.Stop;
                                    }

                                    return;
                                }
                            case 0x163: // *follow me
                                {
                                    if (this.WasNamed(speech) && this.m_Mobile.CheckControlChance(e.Mobile))
                                    {
                                        this.m_Mobile.ControlTarget = e.Mobile;
                                        this.m_Mobile.ControlOrder = OrderType.Follow;
                                    }

                                    return;
                                }
                            case 0x16D: // *release
                                {
                                    if (!isOwner)
                                        break;

                                    if (this.WasNamed(speech) && this.m_Mobile.CheckControlChance(e.Mobile))
                                    {
                                        if (!this.m_Mobile.Summoned)
                                        {
                                            e.Mobile.SendGump(new Gumps.ConfirmReleaseGump(e.Mobile, this.m_Mobile));
                                        }
                                        else
                                        {
                                            this.m_Mobile.ControlTarget = null;
                                            this.m_Mobile.ControlOrder = OrderType.Release;
                                        }
                                    }

                                    return;
                                }
                            case 0x16E: // *transfer
                                {
                                    if (!isOwner)
                                        break;

                                    if (!this.m_Mobile.IsDeadPet && this.WasNamed(speech) && this.m_Mobile.CheckControlChance(e.Mobile))
                                    {
                                        if (this.m_Mobile.Summoned || (this.m_Mobile is GrizzledMare))
                                            e.Mobile.SendLocalizedMessage(1005487); // You cannot transfer ownership of a summoned creature.
                                        else if (e.Mobile.HasTrade)
                                            e.Mobile.SendLocalizedMessage(1010507); // You cannot transfer a pet with a trade pending
                                        else
                                            this.BeginPickTarget(e.Mobile, OrderType.Transfer);
                                    }

                                    return;
                                }
                            case 0x16F: // *stay
                                {
                                    if (this.WasNamed(speech) && this.m_Mobile.CheckControlChance(e.Mobile))
                                    {
                                        this.m_Mobile.ControlTarget = null;
                                        this.m_Mobile.ControlOrder = OrderType.Stay;
                                    }

                                    return;
                                }
                        }
                    }
                }
            }
            else
            {
                if (e.Mobile.AccessLevel >= AccessLevel.GameMaster)
                {
                    this.m_Mobile.DebugSay("It's from a GM");

                    if (this.m_Mobile.FindMyName(e.Speech, true))
                    {
                        string[] str = e.Speech.Split(' ');
                        int i;

                        for (i = 0; i < str.Length; i++)
                        {
                            string word = str[i];

                            if (Insensitive.Equals(word, "obey"))
                            {
                                this.m_Mobile.SetControlMaster(e.Mobile);

                                if (this.m_Mobile.Summoned)
                                    this.m_Mobile.SummonMaster = e.Mobile;

                                return;
                            }
                        }
                    }
                }
            }
        }

        public virtual bool Think()
        {
            if (this.m_Mobile.Deleted)
                return false;

            if (this.CheckFlee())
                return true;

            switch( this.Action )
            {
                case ActionType.Wander:
                    this.m_Mobile.OnActionWander();
                    return this.DoActionWander();

                case ActionType.Combat:
                    this.m_Mobile.OnActionCombat();
                    return this.DoActionCombat();

                case ActionType.Guard:
                    this.m_Mobile.OnActionGuard();
                    return this.DoActionGuard();

                case ActionType.Flee:
                    this.m_Mobile.OnActionFlee();
                    return this.DoActionFlee();

                case ActionType.Interact:
                    this.m_Mobile.OnActionInteract();
                    return this.DoActionInteract();

                case ActionType.Backoff:
                    this.m_Mobile.OnActionBackoff();
                    return this.DoActionBackoff();

                default:
                    return false;
            }
        }

        public virtual void OnActionChanged()
        {
            switch( this.Action )
            {
                case ActionType.Wander:
                    this.m_Mobile.Warmode = false;
                    this.m_Mobile.Combatant = null;
                    this.m_Mobile.FocusMob = null;
                    this.m_Mobile.CurrentSpeed = this.m_Mobile.PassiveSpeed;
                    break;
                case ActionType.Combat:
                    this.m_Mobile.Warmode = true;
                    this.m_Mobile.FocusMob = null;
                    this.m_Mobile.CurrentSpeed = this.m_Mobile.ActiveSpeed;
                    break;
                case ActionType.Guard:
                    this.m_Mobile.Warmode = true;
                    this.m_Mobile.FocusMob = null;
                    this.m_Mobile.Combatant = null;
                    this.m_Mobile.CurrentSpeed = this.m_Mobile.ActiveSpeed;
                    this.m_NextStopGuard = DateTime.Now + TimeSpan.FromSeconds(10);
                    this.m_Mobile.CurrentSpeed = this.m_Mobile.ActiveSpeed;
                    break;
                case ActionType.Flee:
                    this.m_Mobile.Warmode = true;
                    this.m_Mobile.FocusMob = null;
                    this.m_Mobile.CurrentSpeed = this.m_Mobile.ActiveSpeed;
                    break;
                case ActionType.Interact:
                    this.m_Mobile.Warmode = false;
                    this.m_Mobile.CurrentSpeed = this.m_Mobile.PassiveSpeed;
                    break;
                case ActionType.Backoff:
                    this.m_Mobile.Warmode = false;
                    this.m_Mobile.CurrentSpeed = this.m_Mobile.PassiveSpeed;
                    break;
            }
        }

        public virtual bool OnAtWayPoint()
        {
            return true;
        }

        public virtual bool DoActionWander()
        {
            int FollowRange = 1;

            if (this.m_Mobile.FollowRange > 0)
                FollowRange = this.m_Mobile.FollowRange;

            if (this.CheckHerding())
            {
                this.m_Mobile.DebugSay("Praise the shepherd!");
            }
            else if (this.m_Mobile.CurrentWayPoint != null)
            {
                WayPoint point = this.m_Mobile.CurrentWayPoint;
                if ((point.X != this.m_Mobile.Location.X || point.Y != this.m_Mobile.Location.Y) && point.Map == this.m_Mobile.Map && point.Parent == null && !point.Deleted)
                {
                    this.m_Mobile.DebugSay("I will move towards my waypoint.");
                    this.DoMove(this.m_Mobile.GetDirectionTo(this.m_Mobile.CurrentWayPoint));
                }
                else if (this.OnAtWayPoint())
                {
                    this.m_Mobile.DebugSay("I will go to the next waypoint");
                    this.m_Mobile.CurrentWayPoint = point.NextPoint;
                    if (point.NextPoint != null && point.NextPoint.Deleted)
                        this.m_Mobile.CurrentWayPoint = point.NextPoint = point.NextPoint.NextPoint;
                }
            }
            else if (this.m_Mobile.IsAnimatedDead || this.m_Mobile.FollowRange > 0)
            {
                // animated dead follow their master
                Mobile master = this.m_Mobile.SummonMaster;

                if (master != null && master.Map == this.m_Mobile.Map && master.InRange(this.m_Mobile, this.m_Mobile.RangePerception + FollowRange))
                    this.MoveTo(master, false, FollowRange);
                else
                    this.WalkRandomInHome(2, 2, 1);
            }
            else if (this.CheckMove())
            {
                if (!this.m_Mobile.CheckIdle())
                    this.WalkRandomInHome(2, 2, 1);
            }

            if (this.m_Mobile.Combatant != null && !this.m_Mobile.Combatant.Deleted && this.m_Mobile.Combatant.Alive && !this.m_Mobile.Combatant.IsDeadBondedPet)
            {
                this.m_Mobile.Direction = this.m_Mobile.GetDirectionTo(this.m_Mobile.Combatant);
            }

            return true;
        }

        public virtual bool DoActionCombat()
        {
            if (Core.AOS && this.CheckHerding())
            {
                this.m_Mobile.DebugSay("Praise the shepherd!");
            }
            else
            {
                Mobile c = this.m_Mobile.Combatant;

                if (c == null || c.Deleted || c.Map != this.m_Mobile.Map || !c.Alive || c.IsDeadBondedPet)
                    this.Action = ActionType.Wander;
                else
                    this.m_Mobile.Direction = this.m_Mobile.GetDirectionTo(c);
            }

            return true;
        }

        public virtual bool DoActionGuard()
        {
            if (Core.AOS && this.CheckHerding())
            {
                this.m_Mobile.DebugSay("Praise the shepherd!");
            }
            else if (DateTime.Now < this.m_NextStopGuard)
            {
                this.m_Mobile.DebugSay("I am on guard");
                //m_Mobile.Turn( Utility.Random(0, 2) - 1 );
            }
            else
            {
                this.m_Mobile.DebugSay("I stopped being on guard");
                this.Action = ActionType.Wander;
            }

            return true;
        }

        public virtual bool DoActionFlee()
        {
            Mobile from = this.m_Mobile.FocusMob;

            if (from == null || from.Deleted || from.Map != this.m_Mobile.Map)
            {
                this.m_Mobile.DebugSay("I have lost him");
                this.Action = ActionType.Guard;
                return true;
            }

            if (this.WalkMobileRange(from, 1, true, this.m_Mobile.RangePerception * 2, this.m_Mobile.RangePerception * 3))
            {
                this.m_Mobile.DebugSay("I have fled");
                this.Action = ActionType.Guard;
                return true;
            }
            else
            {
                this.m_Mobile.DebugSay("I am fleeing!");
            }

            return true;
        }

        public virtual bool DoActionInteract()
        {
            return true;
        }

        public virtual bool DoActionBackoff()
        {
            return true;
        }

        public virtual bool Obey()
        {
            if (this.m_Mobile.Deleted)
                return false;

            switch( this.m_Mobile.ControlOrder )
            {
                case OrderType.None:
                    return this.DoOrderNone();

                case OrderType.Come:
                    return this.DoOrderCome();

                case OrderType.Drop:
                    return this.DoOrderDrop();

                case OrderType.Friend:
                    return this.DoOrderFriend();

                case OrderType.Unfriend:
                    return this.DoOrderUnfriend();

                case OrderType.Guard:
                    return this.DoOrderGuard();

                case OrderType.Attack:
                    return this.DoOrderAttack();

                case OrderType.Patrol:
                    return this.DoOrderPatrol();

                case OrderType.Release:
                    return this.DoOrderRelease();

                case OrderType.Stay:
                    return this.DoOrderStay();

                case OrderType.Stop:
                    return this.DoOrderStop();

                case OrderType.Follow:
                    return this.DoOrderFollow();

                case OrderType.Transfer:
                    return this.DoOrderTransfer();

                default:
                    return false;
            }
        }

        public virtual void OnCurrentOrderChanged()
        {
            if (this.m_Mobile.Deleted || this.m_Mobile.ControlMaster == null || this.m_Mobile.ControlMaster.Deleted)
                return;

            switch( this.m_Mobile.ControlOrder )
            {
                case OrderType.None:
                    this.m_Mobile.ControlMaster.RevealingAction();
                    this.m_Mobile.Home = this.m_Mobile.Location;
                    this.m_Mobile.CurrentSpeed = this.m_Mobile.PassiveSpeed;
                    this.m_Mobile.PlaySound(this.m_Mobile.GetIdleSound());
                    this.m_Mobile.Warmode = false;
                    this.m_Mobile.Combatant = null;
                    break;
                case OrderType.Come:
                    this.m_Mobile.ControlMaster.RevealingAction();
                    this.m_Mobile.CurrentSpeed = this.m_Mobile.ActiveSpeed;
                    this.m_Mobile.PlaySound(this.m_Mobile.GetIdleSound());
                    this.m_Mobile.Warmode = false;
                    this.m_Mobile.Combatant = null;
                    break;
                case OrderType.Drop:
                    this.m_Mobile.ControlMaster.RevealingAction();
                    this.m_Mobile.CurrentSpeed = this.m_Mobile.PassiveSpeed;
                    this.m_Mobile.PlaySound(this.m_Mobile.GetIdleSound());
                    this.m_Mobile.Warmode = true;
                    this.m_Mobile.Combatant = null;
                    break;
                case OrderType.Friend:
                case OrderType.Unfriend:
                    this.m_Mobile.ControlMaster.RevealingAction();
                    break;
                case OrderType.Guard:
                    this.m_Mobile.ControlMaster.RevealingAction();
                    this.m_Mobile.CurrentSpeed = this.m_Mobile.ActiveSpeed;
                    this.m_Mobile.PlaySound(this.m_Mobile.GetIdleSound());
                    this.m_Mobile.Warmode = true;
                    this.m_Mobile.Combatant = null;
                    string petname = String.Format("{0}", this.m_Mobile.Name);
                    this.m_Mobile.ControlMaster.SendLocalizedMessage(1049671, petname);	//~1_PETNAME~ is now guarding you.
                    break;
                case OrderType.Attack:
                    this.m_Mobile.ControlMaster.RevealingAction();
                    this.m_Mobile.CurrentSpeed = this.m_Mobile.ActiveSpeed;
                    this.m_Mobile.PlaySound(this.m_Mobile.GetIdleSound());

                    this.m_Mobile.Warmode = true;
                    this.m_Mobile.Combatant = null;
                    break;
                case OrderType.Patrol:
                    this.m_Mobile.ControlMaster.RevealingAction();
                    this.m_Mobile.CurrentSpeed = this.m_Mobile.ActiveSpeed;
                    this.m_Mobile.PlaySound(this.m_Mobile.GetIdleSound());
                    this.m_Mobile.Warmode = false;
                    this.m_Mobile.Combatant = null;
                    break;
                case OrderType.Release:
                    this.m_Mobile.ControlMaster.RevealingAction();
                    this.m_Mobile.CurrentSpeed = this.m_Mobile.PassiveSpeed;
                    this.m_Mobile.PlaySound(this.m_Mobile.GetIdleSound());
                    this.m_Mobile.Warmode = false;
                    this.m_Mobile.Combatant = null;
                    break;
                case OrderType.Stay:
                    this.m_Mobile.ControlMaster.RevealingAction();
                    this.m_Mobile.CurrentSpeed = this.m_Mobile.PassiveSpeed;
                    this.m_Mobile.PlaySound(this.m_Mobile.GetIdleSound());
                    this.m_Mobile.Warmode = false;
                    this.m_Mobile.Combatant = null;
                    break;
                case OrderType.Stop:
                    this.m_Mobile.ControlMaster.RevealingAction();
                    this.m_Mobile.Home = this.m_Mobile.Location;
                    this.m_Mobile.CurrentSpeed = this.m_Mobile.PassiveSpeed;
                    this.m_Mobile.PlaySound(this.m_Mobile.GetIdleSound());
                    this.m_Mobile.Warmode = false;
                    this.m_Mobile.Combatant = null;
                    break;
                case OrderType.Follow:
                    this.m_Mobile.ControlMaster.RevealingAction();
                    this.m_Mobile.CurrentSpeed = this.m_Mobile.ActiveSpeed;
                    this.m_Mobile.PlaySound(this.m_Mobile.GetIdleSound());

                    this.m_Mobile.Warmode = false;
                    this.m_Mobile.Combatant = null;
                    break;
                case OrderType.Transfer:
                    this.m_Mobile.ControlMaster.RevealingAction();
                    this.m_Mobile.CurrentSpeed = this.m_Mobile.PassiveSpeed;
                    this.m_Mobile.PlaySound(this.m_Mobile.GetIdleSound());

                    this.m_Mobile.Warmode = false;
                    this.m_Mobile.Combatant = null;
                    break;
            }
        }

        public virtual bool DoOrderNone()
        {
            this.m_Mobile.DebugSay("I have no order");

            this.WalkRandomInHome(3, 2, 1);

            if (this.m_Mobile.Combatant != null && !this.m_Mobile.Combatant.Deleted && this.m_Mobile.Combatant.Alive && !this.m_Mobile.Combatant.IsDeadBondedPet)
            {
                this.m_Mobile.Warmode = true;
                this.m_Mobile.Direction = this.m_Mobile.GetDirectionTo(this.m_Mobile.Combatant);
            }
            else
            {
                this.m_Mobile.Warmode = false;
            }

            return true;
        }

        public virtual bool DoOrderCome()
        {
            if (this.m_Mobile.ControlMaster != null && !this.m_Mobile.ControlMaster.Deleted)
            {
                int iCurrDist = (int)this.m_Mobile.GetDistanceToSqrt(this.m_Mobile.ControlMaster);

                if (iCurrDist > this.m_Mobile.RangePerception)
                {
                    this.m_Mobile.DebugSay("I have lost my master. I stay here");
                    this.m_Mobile.ControlTarget = null;
                    this.m_Mobile.ControlOrder = OrderType.None;
                }
                else
                {
                    this.m_Mobile.DebugSay("My master told me come");

                    // Not exactly OSI style, but better than nothing.
                    bool bRun = (iCurrDist > 5);

                    if (this.WalkMobileRange(this.m_Mobile.ControlMaster, 1, bRun, 0, 1))
                    {
                        if (this.m_Mobile.Combatant != null && !this.m_Mobile.Combatant.Deleted && this.m_Mobile.Combatant.Alive && !this.m_Mobile.Combatant.IsDeadBondedPet)
                        {
                            this.m_Mobile.Warmode = true;
                            this.m_Mobile.Direction = this.m_Mobile.GetDirectionTo(this.m_Mobile.Combatant);
                        }
                        else
                        {
                            this.m_Mobile.Warmode = false;
                        }
                    }
                }
            }

            return true;
        }

        public virtual bool DoOrderDrop()
        {
            if (this.m_Mobile.IsDeadPet || !this.m_Mobile.CanDrop)
                return true;

            this.m_Mobile.DebugSay("I drop my stuff for my master");

            Container pack = this.m_Mobile.Backpack;

            if (pack != null)
            {
                List<Item> list = pack.Items;

                for (int i = list.Count - 1; i >= 0; --i)
                    if (i < list.Count)
                        list[i].MoveToWorld(this.m_Mobile.Location, this.m_Mobile.Map);
            }

            this.m_Mobile.ControlTarget = null;
            this.m_Mobile.ControlOrder = OrderType.None;

            return true;
        }

        public virtual bool CheckHerding()
        {
            IPoint2D target = this.m_Mobile.TargetLocation;

            if (target == null)
                return false; // Creature is not being herded

            double distance = this.m_Mobile.GetDistanceToSqrt(target);

            if (distance < 1 || distance > 15)
            {
                if (distance < 1 && target.X == 1076 && target.Y == 450 && (this.m_Mobile is HordeMinionFamiliar))
                {
                    PlayerMobile pm = this.m_Mobile.ControlMaster as PlayerMobile;

                    if (pm != null)
                    {
                        QuestSystem qs = pm.Quest;

                        if (qs is DarkTidesQuest)
                        {
                            QuestObjective obj = qs.FindObjective(typeof(FetchAbraxusScrollObjective));

                            if (obj != null && !obj.Completed)
                            {
                                this.m_Mobile.AddToBackpack(new ScrollOfAbraxus());
                                obj.Complete();
                            }
                        }
                    }
                }

                this.m_Mobile.TargetLocation = null;
                return false; // At the target or too far away
            }

            this.DoMove(this.m_Mobile.GetDirectionTo(target));

            return true;
        }

        public virtual bool DoOrderFollow()
        {
            if (this.CheckHerding())
            {
                this.m_Mobile.DebugSay("Praise the shepherd!");
            }
            else if (this.m_Mobile.ControlTarget != null && !this.m_Mobile.ControlTarget.Deleted && this.m_Mobile.ControlTarget != this.m_Mobile)
            {
                int iCurrDist = (int)this.m_Mobile.GetDistanceToSqrt(this.m_Mobile.ControlTarget);

                if (iCurrDist > this.m_Mobile.RangePerception)
                {
                    this.m_Mobile.DebugSay("I have lost the one to follow. I stay here");

                    if (this.m_Mobile.Combatant != null && !this.m_Mobile.Combatant.Deleted && this.m_Mobile.Combatant.Alive && !this.m_Mobile.Combatant.IsDeadBondedPet)
                    {
                        this.m_Mobile.Warmode = true;
                        this.m_Mobile.Direction = this.m_Mobile.GetDirectionTo(this.m_Mobile.Combatant);
                    }
                    else
                    {
                        this.m_Mobile.Warmode = false;
                    }
                }
                else
                {
                    this.m_Mobile.DebugSay("My master told me to follow: {0}", this.m_Mobile.ControlTarget.Name);

                    // Not exactly OSI style, but better than nothing.
                    bool bRun = (iCurrDist > 5);

                    if (this.WalkMobileRange(this.m_Mobile.ControlTarget, 1, bRun, 0, 1))
                    {
                        if (this.m_Mobile.Combatant != null && !this.m_Mobile.Combatant.Deleted && this.m_Mobile.Combatant.Alive && !this.m_Mobile.Combatant.IsDeadBondedPet)
                        {
                            this.m_Mobile.Warmode = true;
                            this.m_Mobile.Direction = this.m_Mobile.GetDirectionTo(this.m_Mobile.Combatant);
                        }
                        else
                        {
                            this.m_Mobile.Warmode = false;
                            if (Core.AOS)
                                this.m_Mobile.CurrentSpeed = 0.1;
                        }
                    }
                }
            }
            else
            {
                this.m_Mobile.DebugSay("I have nobody to follow");
                this.m_Mobile.ControlTarget = null;
                this.m_Mobile.ControlOrder = OrderType.None;
            }

            return true;
        }

        public virtual bool DoOrderFriend()
        {
            Mobile from = this.m_Mobile.ControlMaster;
            Mobile to = this.m_Mobile.ControlTarget;

            if (from == null || to == null || from == to || from.Deleted || to.Deleted || !to.Player)
            {
                this.m_Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, 502039); // *looks confused*
            }
            else
            {
                bool youngFrom = from is PlayerMobile ? ((PlayerMobile)from).Young : false;
                bool youngTo = to is PlayerMobile ? ((PlayerMobile)to).Young : false;

                if (youngFrom && !youngTo)
                {
                    from.SendLocalizedMessage(502040); // As a young player, you may not friend pets to older players.
                }
                else if (!youngFrom && youngTo)
                {
                    from.SendLocalizedMessage(502041); // As an older player, you may not friend pets to young players.
                }
                else if (from.CanBeBeneficial(to, true))
                {
                    NetState fromState = from.NetState, toState = to.NetState;

                    if (fromState != null && toState != null)
                    {
                        if (from.HasTrade)
                        {
                            from.SendLocalizedMessage(1070947); // You cannot friend a pet with a trade pending
                        }
                        else if (to.HasTrade)
                        {
                            to.SendLocalizedMessage(1070947); // You cannot friend a pet with a trade pending
                        }
                        else if (this.m_Mobile.IsPetFriend(to))
                        {
                            from.SendLocalizedMessage(1049691); // That person is already a friend.
                        }
                        else if (!this.m_Mobile.AllowNewPetFriend)
                        {
                            from.SendLocalizedMessage(1005482); // Your pet does not seem to be interested in making new friends right now.
                        }
                        else
                        {
                            // ~1_NAME~ will now accept movement commands from ~2_NAME~.
                            from.SendLocalizedMessage(1049676, String.Format("{0}\t{1}", this.m_Mobile.Name, to.Name));

                            /* ~1_NAME~ has granted you the ability to give orders to their pet ~2_PET_NAME~.
                            * This creature will now consider you as a friend.
                            */
                            to.SendLocalizedMessage(1043246, String.Format("{0}\t{1}", from.Name, this.m_Mobile.Name));

                            this.m_Mobile.AddPetFriend(to);

                            this.m_Mobile.ControlTarget = to;
                            this.m_Mobile.ControlOrder = OrderType.Follow;

                            return true;
                        }
                    }
                }
            }

            this.m_Mobile.ControlTarget = from;
            this.m_Mobile.ControlOrder = OrderType.Follow;

            return true;
        }

        public virtual bool DoOrderUnfriend()
        {
            Mobile from = this.m_Mobile.ControlMaster;
            Mobile to = this.m_Mobile.ControlTarget;

            if (from == null || to == null || from == to || from.Deleted || to.Deleted || !to.Player)
            {
                this.m_Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, 502039); // *looks confused*
            }
            else if (!this.m_Mobile.IsPetFriend(to))
            {
                from.SendLocalizedMessage(1070953); // That person is not a friend.
            }
            else
            {
                // ~1_NAME~ will no longer accept movement commands from ~2_NAME~.
                from.SendLocalizedMessage(1070951, String.Format("{0}\t{1}", this.m_Mobile.Name, to.Name));

                /* ~1_NAME~ has no longer granted you the ability to give orders to their pet ~2_PET_NAME~.
                * This creature will no longer consider you as a friend.
                */
                to.SendLocalizedMessage(1070952, String.Format("{0}\t{1}", from.Name, this.m_Mobile.Name));

                this.m_Mobile.RemovePetFriend(to);
            }

            this.m_Mobile.ControlTarget = from;
            this.m_Mobile.ControlOrder = OrderType.Follow;

            return true;
        }

        public virtual bool DoOrderGuard()
        {
            if (this.m_Mobile.IsDeadPet)
                return true;

            Mobile controlMaster = this.m_Mobile.ControlMaster;

            if (controlMaster == null || controlMaster.Deleted)
                return true;

            Mobile combatant = this.m_Mobile.Combatant;

            List<AggressorInfo> aggressors = controlMaster.Aggressors;

            if (aggressors.Count > 0)
            {
                for (int i = 0; i < aggressors.Count; ++i)
                {
                    AggressorInfo info = aggressors[i];
                    Mobile attacker = info.Attacker;

                    if (attacker != null && !attacker.Deleted && attacker.GetDistanceToSqrt(this.m_Mobile) <= this.m_Mobile.RangePerception)
                    {
                        if (combatant == null || attacker.GetDistanceToSqrt(controlMaster) < combatant.GetDistanceToSqrt(controlMaster))
                            combatant = attacker;
                    }
                }

                if (combatant != null)
                    this.m_Mobile.DebugSay("Crap, my master has been attacked! I will attack one of those bastards!");
            }

            if (combatant != null && combatant != this.m_Mobile && combatant != this.m_Mobile.ControlMaster && !combatant.Deleted && combatant.Alive && !combatant.IsDeadBondedPet && this.m_Mobile.CanSee(combatant) && this.m_Mobile.CanBeHarmful(combatant, false) && combatant.Map == this.m_Mobile.Map)
            {
                this.m_Mobile.DebugSay("Guarding from target...");

                this.m_Mobile.Combatant = combatant;
                this.m_Mobile.FocusMob = combatant;
                this.Action = ActionType.Combat;

                /*
                * We need to call Think() here or spell casting monsters will not use
                * spells when guarding because their target is never processed.
                */
                this.Think();
            }
            else
            {
                this.m_Mobile.DebugSay("Nothing to guard from");

                this.m_Mobile.Warmode = false;
                if (Core.AOS)
                    this.m_Mobile.CurrentSpeed = 0.1;

                this.WalkMobileRange(controlMaster, 1, false, 0, 1);
            }

            return true;
        }

        public virtual bool DoOrderAttack()
        {
            if (this.m_Mobile.IsDeadPet)
                return true;

            if (this.m_Mobile.ControlTarget == null || this.m_Mobile.ControlTarget.Deleted || this.m_Mobile.ControlTarget.Map != this.m_Mobile.Map || !this.m_Mobile.ControlTarget.Alive || this.m_Mobile.ControlTarget.IsDeadBondedPet)
            {
                this.m_Mobile.DebugSay("I think he might be dead. He's not anywhere around here at least. That's cool. I'm glad he's dead.");

                if (Core.AOS)
                {
                    this.m_Mobile.ControlTarget = this.m_Mobile.ControlMaster;
                    this.m_Mobile.ControlOrder = OrderType.Follow;
                }
                else
                {
                    this.m_Mobile.ControlTarget = null;
                    this.m_Mobile.ControlOrder = OrderType.None;
                }

                if (this.m_Mobile.FightMode == FightMode.Closest || this.m_Mobile.FightMode == FightMode.Aggressor)
                {
                    Mobile newCombatant = null;
                    double newScore = 0.0;

                    foreach (Mobile aggr in this.m_Mobile.GetMobilesInRange(this.m_Mobile.RangePerception))
                    {
                        if (!this.m_Mobile.CanSee(aggr) || aggr.Combatant != this.m_Mobile)
                            continue;

                        if (aggr.IsDeadBondedPet || !aggr.Alive)
                            continue;

                        double aggrScore = this.m_Mobile.GetFightModeRanking(aggr, FightMode.Closest, false);

                        if ((newCombatant == null || aggrScore > newScore) && this.m_Mobile.InLOS(aggr))
                        {
                            newCombatant = aggr;
                            newScore = aggrScore;
                        }
                    }

                    if (newCombatant != null)
                    {
                        this.m_Mobile.ControlTarget = newCombatant;
                        this.m_Mobile.ControlOrder = OrderType.Attack;
                        this.m_Mobile.Combatant = newCombatant;
                        this.m_Mobile.DebugSay("But -that- is not dead. Here we go again...");
                        this.Think();
                    }
                }
            }
            else
            {
                this.m_Mobile.DebugSay("Attacking target...");
                this.Think();
            }

            return true;
        }

        public virtual bool DoOrderPatrol()
        {
            this.m_Mobile.DebugSay("This order is not yet coded");
            return true;
        }

        public virtual bool DoOrderRelease()
        {
            this.m_Mobile.DebugSay("I have been released");

            this.m_Mobile.PlaySound(this.m_Mobile.GetAngerSound());

            this.m_Mobile.SetControlMaster(null);
            this.m_Mobile.SummonMaster = null;

            this.m_Mobile.BondingBegin = DateTime.MinValue;
            this.m_Mobile.OwnerAbandonTime = DateTime.MinValue;
            this.m_Mobile.IsBonded = false;

            SpawnEntry se = this.m_Mobile.Spawner as SpawnEntry;
            if (se != null && se.HomeLocation != Point3D.Zero)
            {
                this.m_Mobile.Home = se.HomeLocation;
                this.m_Mobile.RangeHome = se.HomeRange;
            }

            if (this.m_Mobile.DeleteOnRelease || this.m_Mobile.IsDeadPet)
                this.m_Mobile.Delete();
			
            this.m_Mobile.BeginDeleteTimer();
            this.m_Mobile.DropBackpack();

            return true;
        }

        public virtual bool DoOrderStay()
        {
            if (this.CheckHerding())
                this.m_Mobile.DebugSay("Praise the shepherd!");
            else
                this.m_Mobile.DebugSay("My master told me to stay");

            //m_Mobile.Direction = m_Mobile.GetDirectionTo( m_Mobile.ControlMaster );

            return true;
        }

        public virtual bool DoOrderStop()
        {
            if (this.m_Mobile.ControlMaster == null || this.m_Mobile.ControlMaster.Deleted)
                return true;

            this.m_Mobile.DebugSay("My master told me to stop.");

            this.m_Mobile.Direction = this.m_Mobile.GetDirectionTo(this.m_Mobile.ControlMaster);
            this.m_Mobile.Home = this.m_Mobile.Location;

            this.m_Mobile.ControlTarget = null;

            if (Core.ML)
            {
                this.WalkRandomInHome(3, 2, 1);
            }
            else
            {
                this.m_Mobile.ControlOrder = OrderType.None;
            }

            return true;
        }

        private class TransferItem : Item
        {
            public static bool IsInCombat(BaseCreature creature)
            {
                return (creature != null && (creature.Aggressors.Count > 0 || creature.Aggressed.Count > 0));
            }

            private readonly BaseCreature m_Creature;

            public TransferItem(BaseCreature creature)
                : base(ShrinkTable.Lookup(creature))
            {
                this.m_Creature = creature;

                this.Movable = false;

                if (!Core.AOS)
                {
                    this.Name = creature.Name;
                }
                else if (this.ItemID == ShrinkTable.DefaultItemID || creature.GetType().IsDefined(typeof(FriendlyNameAttribute), false) || creature is Reptalon)
                    this.Name = FriendlyNameAttribute.GetFriendlyNameFor(creature.GetType()).ToString();

                //(As Per OSI)No name.  Normally, set by the ItemID of the Shrink Item unless we either explicitly set it with an Attribute, or, no lookup found
				
                this.Hue = creature.Hue & 0x0FFF;
            }

            public TransferItem(Serial serial)
                : base(serial)
            {
            }

            public override void Serialize(GenericWriter writer)
            {
                base.Serialize(writer);

                writer.Write((int)0); // version
            }

            public override void Deserialize(GenericReader reader)
            {
                base.Deserialize(reader);

                int version = reader.ReadInt();

                this.Delete();
            }

            public override void GetProperties(ObjectPropertyList list)
            {
                base.GetProperties(list);

                list.Add(1041603); // This item represents a pet currently in consideration for trade
                list.Add(1041601, this.m_Creature.Name); // Pet Name: ~1_val~

                if (this.m_Creature.ControlMaster != null)
                    list.Add(1041602, this.m_Creature.ControlMaster.Name); // Owner: ~1_val~
            }

            public override bool AllowSecureTrade(Mobile from, Mobile to, Mobile newOwner, bool accepted)
            {
                if (!base.AllowSecureTrade(from, to, newOwner, accepted))
                    return false;

                if (this.Deleted || this.m_Creature == null || this.m_Creature.Deleted || this.m_Creature.ControlMaster != from || !from.CheckAlive() || !to.CheckAlive())
                    return false;

                if (from.Map != this.m_Creature.Map || !from.InRange(this.m_Creature, 14))
                    return false;

                bool youngFrom = from is PlayerMobile ? ((PlayerMobile)from).Young : false;
                bool youngTo = to is PlayerMobile ? ((PlayerMobile)to).Young : false;

                if (accepted && youngFrom && !youngTo)
                {
                    from.SendLocalizedMessage(502051); // As a young player, you may not transfer pets to older players.
                }
                else if (accepted && !youngFrom && youngTo)
                {
                    from.SendLocalizedMessage(502052); // As an older player, you may not transfer pets to young players.
                }
                else if (accepted && !this.m_Creature.CanBeControlledBy(to))
                {
                    string args = String.Format("{0}\t{1}\t ", to.Name, from.Name);

                    from.SendLocalizedMessage(1043248, args); // The pet refuses to be transferred because it will not obey ~1_NAME~.~3_BLANK~
                    to.SendLocalizedMessage(1043249, args); // The pet will not accept you as a master because it does not trust you.~3_BLANK~

                    return false;
                }
                else if (accepted && !this.m_Creature.CanBeControlledBy(from))
                {
                    string args = String.Format("{0}\t{1}\t ", to.Name, from.Name);

                    from.SendLocalizedMessage(1043250, args); // The pet refuses to be transferred because it will not obey you sufficiently.~3_BLANK~
                    to.SendLocalizedMessage(1043251, args); // The pet will not accept you as a master because it does not trust ~2_NAME~.~3_BLANK~
                }
                else if (accepted && (to.Followers + this.m_Creature.ControlSlots) > to.FollowersMax)
                {
                    to.SendLocalizedMessage(1049607); // You have too many followers to control that creature.

                    return false;
                }
                else if (accepted && IsInCombat(this.m_Creature))
                {
                    from.SendMessage("You may not transfer a pet that has recently been in combat.");
                    to.SendMessage("The pet may not be transfered to you because it has recently been in combat.");

                    return false;
                }

                return true;
            }

            public override void OnSecureTrade(Mobile from, Mobile to, Mobile newOwner, bool accepted)
            {
                if (this.Deleted)
                    return;

                this.Delete();

                if (this.m_Creature == null || this.m_Creature.Deleted || this.m_Creature.ControlMaster != from || !from.CheckAlive() || !to.CheckAlive())
                    return;

                if (from.Map != this.m_Creature.Map || !from.InRange(this.m_Creature, 14))
                    return;

                if (accepted)
                {
                    if (this.m_Creature.SetControlMaster(to))
                    {
                        if (this.m_Creature.Summoned)
                            this.m_Creature.SummonMaster = to;

                        this.m_Creature.ControlTarget = to;
                        this.m_Creature.ControlOrder = OrderType.Follow;

                        this.m_Creature.BondingBegin = DateTime.MinValue;
                        this.m_Creature.OwnerAbandonTime = DateTime.MinValue;
                        this.m_Creature.IsBonded = false;

                        this.m_Creature.PlaySound(this.m_Creature.GetIdleSound());

                        string args = String.Format("{0}\t{1}\t{2}", from.Name, this.m_Creature.Name, to.Name);

                        from.SendLocalizedMessage(1043253, args); // You have transferred your pet to ~3_GETTER~.
                        to.SendLocalizedMessage(1043252, args); // ~1_NAME~ has transferred the allegiance of ~2_PET_NAME~ to you.
                    }
                }
            }
        }

        public virtual bool DoOrderTransfer()
        {
            if (this.m_Mobile.IsDeadPet)
                return true;

            Mobile from = this.m_Mobile.ControlMaster;
            Mobile to = this.m_Mobile.ControlTarget;

            if (from != to && from != null && !from.Deleted && to != null && !to.Deleted && to.Player)
            {
                this.m_Mobile.DebugSay("Begin transfer with {0}", to.Name);

                bool youngFrom = from is PlayerMobile ? ((PlayerMobile)from).Young : false;
                bool youngTo = to is PlayerMobile ? ((PlayerMobile)to).Young : false;

                if (youngFrom && !youngTo)
                {
                    from.SendLocalizedMessage(502051); // As a young player, you may not transfer pets to older players.
                }
                else if (!youngFrom && youngTo)
                {
                    from.SendLocalizedMessage(502052); // As an older player, you may not transfer pets to young players.
                }
                else if (!this.m_Mobile.CanBeControlledBy(to))
                {
                    string args = String.Format("{0}\t{1}\t ", to.Name, from.Name);

                    from.SendLocalizedMessage(1043248, args); // The pet refuses to be transferred because it will not obey ~1_NAME~.~3_BLANK~
                    to.SendLocalizedMessage(1043249, args); // The pet will not accept you as a master because it does not trust you.~3_BLANK~
                }
                else if (!this.m_Mobile.CanBeControlledBy(from))
                {
                    string args = String.Format("{0}\t{1}\t ", to.Name, from.Name);

                    from.SendLocalizedMessage(1043250, args); // The pet refuses to be transferred because it will not obey you sufficiently.~3_BLANK~
                    to.SendLocalizedMessage(1043251, args); // The pet will not accept you as a master because it does not trust ~2_NAME~.~3_BLANK~
                }
                else if (TransferItem.IsInCombat(this.m_Mobile))
                {
                    from.SendMessage("You may not transfer a pet that has recently been in combat.");
                    to.SendMessage("The pet may not be transfered to you because it has recently been in combat.");
                }
                else
                {
                    NetState fromState = from.NetState, toState = to.NetState;

                    if (fromState != null && toState != null)
                    {
                        if (from.HasTrade)
                        {
                            from.SendLocalizedMessage(1010507); // You cannot transfer a pet with a trade pending
                        }
                        else if (to.HasTrade)
                        {
                            to.SendLocalizedMessage(1010507); // You cannot transfer a pet with a trade pending
                        }
                        else
                        {
                            Container c = fromState.AddTrade(toState);
                            c.DropItem(new TransferItem(this.m_Mobile));
                        }
                    }
                }
            }

            this.m_Mobile.ControlTarget = null;
            this.m_Mobile.ControlOrder = OrderType.Stay;

            return true;
        }

        public virtual bool DoBardPacified()
        {
            if (DateTime.Now < this.m_Mobile.BardEndTime)
            {
                this.m_Mobile.DebugSay("I am pacified, I wait");
                this.m_Mobile.Combatant = null;
                this.m_Mobile.Warmode = false;
            }
            else
            {
                this.m_Mobile.DebugSay("I'm not pacified any longer");
                this.m_Mobile.BardPacified = false;
            }

            return true;
        }

        public virtual bool DoBardProvoked()
        {
            if (DateTime.Now >= this.m_Mobile.BardEndTime && (this.m_Mobile.BardMaster == null || this.m_Mobile.BardMaster.Deleted || this.m_Mobile.BardMaster.Map != this.m_Mobile.Map || this.m_Mobile.GetDistanceToSqrt(this.m_Mobile.BardMaster) > this.m_Mobile.RangePerception))
            {
                this.m_Mobile.DebugSay("I have lost my provoker");
                this.m_Mobile.BardProvoked = false;
                this.m_Mobile.BardMaster = null;
                this.m_Mobile.BardTarget = null;

                this.m_Mobile.Combatant = null;
                this.m_Mobile.Warmode = false;
            }
            else
            {
                if (this.m_Mobile.BardTarget == null || this.m_Mobile.BardTarget.Deleted || this.m_Mobile.BardTarget.Map != this.m_Mobile.Map || this.m_Mobile.GetDistanceToSqrt(this.m_Mobile.BardTarget) > this.m_Mobile.RangePerception)
                {
                    this.m_Mobile.DebugSay("I have lost my provoke target");
                    this.m_Mobile.BardProvoked = false;
                    this.m_Mobile.BardMaster = null;
                    this.m_Mobile.BardTarget = null;

                    this.m_Mobile.Combatant = null;
                    this.m_Mobile.Warmode = false;
                }
                else
                {
                    this.m_Mobile.Combatant = this.m_Mobile.BardTarget;
                    this.m_Action = ActionType.Combat;

                    this.m_Mobile.OnThink();
                    this.Think();
                }
            }

            return true;
        }

        public virtual void WalkRandom(int iChanceToNotMove, int iChanceToDir, int iSteps)
        {
            if (this.m_Mobile.Deleted || this.m_Mobile.DisallowAllMoves)
                return;

            for (int i = 0; i < iSteps; i++)
            {
                if (Utility.Random(8 * iChanceToNotMove) <= 8)
                {
                    int iRndMove = Utility.Random(0, 8 + (9 * iChanceToDir));

                    switch( iRndMove )
                    {
                        case 0:
                            this.DoMove(Direction.Up);
                            break;
                        case 1:
                            this.DoMove(Direction.North);
                            break;
                        case 2:
                            this.DoMove(Direction.Left);
                            break;
                        case 3:
                            this.DoMove(Direction.West);
                            break;
                        case 5:
                            this.DoMove(Direction.Down);
                            break;
                        case 6:
                            this.DoMove(Direction.South);
                            break;
                        case 7:
                            this.DoMove(Direction.Right);
                            break;
                        case 8:
                            this.DoMove(Direction.East);
                            break;
                        default:
                            this.DoMove(this.m_Mobile.Direction);
                            break;
                    }
                }
            }
        }

        public double TransformMoveDelay(double delay)
        {
            bool isPassive = (delay == this.m_Mobile.PassiveSpeed);
            bool isControlled = (this.m_Mobile.Controlled || this.m_Mobile.Summoned);

            if (delay == 0.2)
                delay = 0.3;
            else if (delay == 0.25)
                delay = 0.45;
            else if (delay == 0.3)
                delay = 0.6;
            else if (delay == 0.4)
                delay = 0.9;
            else if (delay == 0.5)
                delay = 1.05;
            else if (delay == 0.6)
                delay = 1.2;
            else if (delay == 0.8)
                delay = 1.5;

            if (isPassive)
                delay += 0.2;

            if (!isControlled)
            {
                delay += 0.1;
            }
            else if (this.m_Mobile.Controlled)
            {
                if (this.m_Mobile.ControlOrder == OrderType.Follow && this.m_Mobile.ControlTarget == this.m_Mobile.ControlMaster)
                    delay *= 0.5;

                delay -= 0.075;
            }

            double speedfactor = 0.8;

            XmlValue a = (XmlValue)XmlAttach.FindAttachment(this.m_Mobile, typeof(XmlValue), "DamagedSpeedFactor");
            
            if (a != null)
            {
                speedfactor = (double)a.Value / 100.0;
            }

            if (this.m_Mobile.ReduceSpeedWithDamage || this.m_Mobile.IsSubdued)
            {
                double offset = (double)this.m_Mobile.Hits / this.m_Mobile.HitsMax;

                if (offset < 0.0)
                    offset = 0.0;
                else if (offset > 1.0)
                    offset = 1.0;

                offset = 1.0 - offset;

                delay += (offset * speedfactor);
            }

            if (delay < 0.0)
                delay = 0.0;

            if (double.IsNaN(delay))
            {
                using (StreamWriter op = new StreamWriter("nan_transform.txt", true))
                {
                    op.WriteLine(String.Format("NaN in TransformMoveDelay: {0}, {1}, {2}, {3}", DateTime.Now, this.GetType().ToString(), this.m_Mobile == null ? "null" : this.m_Mobile.GetType().ToString(), this.m_Mobile.HitsMax));
                }

                return 1.0;
            }

            return delay;
        }

        private DateTime m_NextMove;

        public DateTime NextMove
        {
            get
            {
                return this.m_NextMove;
            }
            set
            {
                this.m_NextMove = value;
            }
        }

        public virtual bool CheckMove()
        {
            return (DateTime.Now >= this.m_NextMove);
        }

        public virtual bool DoMove(Direction d)
        {
            return this.DoMove(d, false);
        }

        public virtual bool DoMove(Direction d, bool badStateOk)
        {
            MoveResult res = this.DoMoveImpl(d);

            return (res == MoveResult.Success || res == MoveResult.SuccessAutoTurn || (badStateOk && res == MoveResult.BadState));
        }

        private static readonly Queue m_Obstacles = new Queue();

        public virtual MoveResult DoMoveImpl(Direction d)
        {
            if (this.m_Mobile.Deleted || this.m_Mobile.Frozen || this.m_Mobile.Paralyzed || (this.m_Mobile.Spell != null && this.m_Mobile.Spell.IsCasting) || this.m_Mobile.DisallowAllMoves)
                return MoveResult.BadState;
            else if (!this.CheckMove())
                return MoveResult.BadState;

            // This makes them always move one step, never any direction changes
            this.m_Mobile.Direction = d;

            TimeSpan delay = TimeSpan.FromSeconds(this.TransformMoveDelay(this.m_Mobile.CurrentSpeed));

            this.m_NextMove += delay;

            if (this.m_NextMove < DateTime.Now)
                this.m_NextMove = DateTime.Now;

            this.m_Mobile.Pushing = false;

            MoveImpl.IgnoreMovableImpassables = (this.m_Mobile.CanMoveOverObstacles && !this.m_Mobile.CanDestroyObstacles);

            if ((this.m_Mobile.Direction & Direction.Mask) != (d & Direction.Mask))
            {
                bool v = this.m_Mobile.Move(d);

                MoveImpl.IgnoreMovableImpassables = false;
                return (v ? MoveResult.Success : MoveResult.Blocked);
            }
            else if (!this.m_Mobile.Move(d))
            {
                bool wasPushing = this.m_Mobile.Pushing;

                bool blocked = true;

                bool canOpenDoors = this.m_Mobile.CanOpenDoors;
                bool canDestroyObstacles = this.m_Mobile.CanDestroyObstacles;

                if (canOpenDoors || canDestroyObstacles)
                {
                    this.m_Mobile.DebugSay("My movement was blocked, I will try to clear some obstacles.");

                    Map map = this.m_Mobile.Map;

                    if (map != null)
                    {
                        int x = this.m_Mobile.X, y = this.m_Mobile.Y;
                        Movement.Movement.Offset(d, ref x, ref y);

                        int destroyables = 0;

                        IPooledEnumerable eable = map.GetItemsInRange(new Point3D(x, y, this.m_Mobile.Location.Z), 1);

                        foreach (Item item in eable)
                        {
                            if (canOpenDoors && item is BaseDoor && (item.Z + item.ItemData.Height) > this.m_Mobile.Z && (this.m_Mobile.Z + 16) > item.Z)
                            {
                                if (item.X != x || item.Y != y)
                                    continue;

                                BaseDoor door = (BaseDoor)item;

                                if (!door.Locked || !door.UseLocks())
                                    m_Obstacles.Enqueue(door);

                                if (!canDestroyObstacles)
                                    break;
                            }
                            else if (canDestroyObstacles && item.Movable && item.ItemData.Impassable && (item.Z + item.ItemData.Height) > this.m_Mobile.Z && (this.m_Mobile.Z + 16) > item.Z)
                            {
                                if (!this.m_Mobile.InRange(item.GetWorldLocation(), 1))
                                    continue;

                                m_Obstacles.Enqueue(item);
                                ++destroyables;
                            }
                        }

                        eable.Free();

                        if (destroyables > 0)
                            Effects.PlaySound(new Point3D(x, y, this.m_Mobile.Z), this.m_Mobile.Map, 0x3B3);

                        if (m_Obstacles.Count > 0)
                            blocked = false; // retry movement

                        while (m_Obstacles.Count > 0)
                        {
                            Item item = (Item)m_Obstacles.Dequeue();

                            if (item is BaseDoor)
                            {
                                this.m_Mobile.DebugSay("Little do they expect, I've learned how to open doors. Didn't they read the script??");
                                this.m_Mobile.DebugSay("*twist*");

                                ((BaseDoor)item).Use(this.m_Mobile);
                            }
                            else
                            {
                                this.m_Mobile.DebugSay("Ugabooga. I'm so big and tough I can destroy it: {0}", item.GetType().Name);

                                if (item is Container)
                                {
                                    Container cont = (Container)item;

                                    for (int i = 0; i < cont.Items.Count; ++i)
                                    {
                                        Item check = cont.Items[i];

                                        if (check.Movable && check.ItemData.Impassable && (item.Z + check.ItemData.Height) > this.m_Mobile.Z)
                                            m_Obstacles.Enqueue(check);
                                    }

                                    cont.Destroy();
                                }
                                else
                                {
                                    item.Delete();
                                }
                            }
                        }

                        if (!blocked)
                            blocked = !this.m_Mobile.Move(d);
                    }
                }

                if (blocked)
                {
                    int offset = (Utility.RandomDouble() >= 0.6 ? 1 : -1);

                    for (int i = 0; i < 2; ++i)
                    {
                        this.m_Mobile.TurnInternal(offset);

                        if (this.m_Mobile.Move(this.m_Mobile.Direction))
                        {
                            MoveImpl.IgnoreMovableImpassables = false;
                            return MoveResult.SuccessAutoTurn;
                        }
                    }

                    MoveImpl.IgnoreMovableImpassables = false;
                    return (wasPushing ? MoveResult.BadState : MoveResult.Blocked);
                }
                else
                {
                    MoveImpl.IgnoreMovableImpassables = false;
                    return MoveResult.Success;
                }
            }

            MoveImpl.IgnoreMovableImpassables = false;
            return MoveResult.Success;
        }

        public virtual void WalkRandomInHome(int iChanceToNotMove, int iChanceToDir, int iSteps)
        {
            if (this.m_Mobile.Deleted || this.m_Mobile.DisallowAllMoves)
                return;

            if (this.m_Mobile.Home == Point3D.Zero)
            {
                if (this.m_Mobile.Spawner is SpawnEntry)
                {
                    Region region = ((SpawnEntry)this.m_Mobile.Spawner).Region;

                    if (this.m_Mobile.Region.AcceptsSpawnsFrom(region))
                    {
                        this.m_Mobile.WalkRegion = region;
                        this.WalkRandom(iChanceToNotMove, iChanceToDir, iSteps);
                        this.m_Mobile.WalkRegion = null;
                    }
                    else
                    {
                        if (region.GoLocation != Point3D.Zero && Utility.Random(10) > 5)
                        {
                            this.DoMove(this.m_Mobile.GetDirectionTo(region.GoLocation));
                        }
                        else
                        {
                            this.WalkRandom(iChanceToNotMove, iChanceToDir, 1);
                        }
                    }
                }
                else
                {
                    this.WalkRandom(iChanceToNotMove, iChanceToDir, iSteps);
                }
            }
            else
            {
                for (int i = 0; i < iSteps; i++)
                {
                    if (this.m_Mobile.RangeHome != 0)
                    {
                        int iCurrDist = (int)this.m_Mobile.GetDistanceToSqrt(this.m_Mobile.Home);

                        if (iCurrDist < this.m_Mobile.RangeHome * 2 / 3)
                        {
                            this.WalkRandom(iChanceToNotMove, iChanceToDir, 1);
                        }
                        else if (iCurrDist > this.m_Mobile.RangeHome)
                        {
                            this.DoMove(this.m_Mobile.GetDirectionTo(this.m_Mobile.Home));
                        }
                        else
                        {
                            if (Utility.Random(10) > 5)
                            {
                                this.DoMove(this.m_Mobile.GetDirectionTo(this.m_Mobile.Home));
                            }
                            else
                            {
                                this.WalkRandom(iChanceToNotMove, iChanceToDir, 1);
                            }
                        }
                    }
                    else
                    {
                        if (this.m_Mobile.Location != this.m_Mobile.Home)
                        {
                            this.DoMove(this.m_Mobile.GetDirectionTo(this.m_Mobile.Home));
                        }
                    }
                }
            }
        }

        public virtual bool CheckFlee()
        {
            if (this.m_Mobile.CheckFlee())
            {
                Mobile combatant = this.m_Mobile.Combatant;

                if (combatant == null)
                {
                    this.WalkRandom(1, 2, 1);
                }
                else
                {
                    Direction d = combatant.GetDirectionTo(this.m_Mobile);

                    d = (Direction)((int)d + Utility.RandomMinMax(-1, +1));

                    this.m_Mobile.Direction = d;
                    this.m_Mobile.Move(d);
                }

                return true;
            }

            return false;
        }

        protected PathFollower m_Path;

        public virtual void OnTeleported()
        {
            if (this.m_Path != null)
            {
                this.m_Mobile.DebugSay("Teleported; repathing");
                this.m_Path.ForceRepath();
            }
        }

        public virtual bool MoveTo(Mobile m, bool run, int range)
        {
            if (this.m_Mobile.Deleted || this.m_Mobile.DisallowAllMoves || m == null || m.Deleted)
                return false;

            if (this.m_Mobile.InRange(m, range))
            {
                this.m_Path = null;
                return true;
            }

            if (this.m_Path != null && this.m_Path.Goal == m)
            {
                if (this.m_Path.Follow(run, 1))
                {
                    this.m_Path = null;
                    return true;
                }
            }
            else if (!this.DoMove(this.m_Mobile.GetDirectionTo(m), true))
            {
                this.m_Path = new PathFollower(this.m_Mobile, m);
                this.m_Path.Mover = new MoveMethod(DoMoveImpl);

                if (this.m_Path.Follow(run, 1))
                {
                    this.m_Path = null;
                    return true;
                }
            }
            else
            {
                this.m_Path = null;
                return true;
            }

            return false;
        }

        /*
        *  Walk at range distance from mobile
        * 
        *	iSteps : Number of steps
        *	bRun   : Do we run
        *	iWantDistMin : The minimum distance we want to be
        *  iWantDistMax : The maximum distance we want to be
        * 
        */
        public virtual bool WalkMobileRange(Mobile m, int iSteps, bool bRun, int iWantDistMin, int iWantDistMax)
        {
            if (this.m_Mobile.Deleted || this.m_Mobile.DisallowAllMoves)
                return false;

            if (m != null)
            {
                for (int i = 0; i < iSteps; i++)
                {
                    // Get the curent distance
                    int iCurrDist = (int)this.m_Mobile.GetDistanceToSqrt(m);

                    if (iCurrDist < iWantDistMin || iCurrDist > iWantDistMax)
                    {
                        bool needCloser = (iCurrDist > iWantDistMax);
                        bool needFurther = !needCloser;

                        if (needCloser && this.m_Path != null && this.m_Path.Goal == m)
                        {
                            if (this.m_Path.Follow(bRun, 1))
                                this.m_Path = null;
                        }
                        else
                        {
                            Direction dirTo;

                            if (iCurrDist > iWantDistMax)
                                dirTo = this.m_Mobile.GetDirectionTo(m);
                            else
                                dirTo = m.GetDirectionTo(this.m_Mobile);

                            // Add the run flag
                            if (bRun)
                                dirTo = dirTo | Direction.Running;

                            if (!this.DoMove(dirTo, true) && needCloser)
                            {
                                this.m_Path = new PathFollower(this.m_Mobile, m);
                                this.m_Path.Mover = new MoveMethod(DoMoveImpl);

                                if (this.m_Path.Follow(bRun, 1))
                                    this.m_Path = null;
                            }
                            else
                            {
                                this.m_Path = null;
                            }
                        }
                    }
                    else
                    {
                        return true;
                    }
                }

                // Get the curent distance
                int iNewDist = (int)this.m_Mobile.GetDistanceToSqrt(m);

                if (iNewDist >= iWantDistMin && iNewDist <= iWantDistMax)
                    return true;
                else
                    return false;
            }

            return false;
        }

        /*
        * Here we check to acquire a target from our surronding
        * 
        *  iRange : The range
        *  acqType : A type of acquire we want (closest, strongest, etc)
        *  bPlayerOnly : Don't bother with other creatures or NPCs, want a player
        *  bFacFriend : Check people in my faction
        *  bFacFoe : Check people in other factions
        * 
        */
        public virtual bool AcquireFocusMob(int iRange, FightMode acqType, bool bPlayerOnly, bool bFacFriend, bool bFacFoe)
        {
            if (this.m_Mobile.Deleted)
                return false;

            if (this.m_Mobile.BardProvoked)
            {
                if (this.m_Mobile.BardTarget == null || this.m_Mobile.BardTarget.Deleted)
                {
                    this.m_Mobile.FocusMob = null;
                    return false;
                }
                else
                {
                    this.m_Mobile.FocusMob = this.m_Mobile.BardTarget;
                    return (this.m_Mobile.FocusMob != null);
                }
            }
            else if (this.m_Mobile.Controlled)
            {
                if (this.m_Mobile.ControlTarget == null || this.m_Mobile.ControlTarget.Deleted || this.m_Mobile.ControlTarget.Hidden || !this.m_Mobile.ControlTarget.Alive || this.m_Mobile.ControlTarget.IsDeadBondedPet || !this.m_Mobile.InRange(this.m_Mobile.ControlTarget, this.m_Mobile.RangePerception * 2))
                {
                    if (this.m_Mobile.ControlTarget != null && this.m_Mobile.ControlTarget != this.m_Mobile.ControlMaster)
                        this.m_Mobile.ControlTarget = null;

                    this.m_Mobile.FocusMob = null;
                    return false;
                }
                else
                {
                    this.m_Mobile.FocusMob = this.m_Mobile.ControlTarget;
                    return (this.m_Mobile.FocusMob != null);
                }
            }

            if (this.m_Mobile.ConstantFocus != null)
            {
                this.m_Mobile.DebugSay("Acquired my constant focus");
                this.m_Mobile.FocusMob = this.m_Mobile.ConstantFocus;
                return true;
            }

            if (acqType == FightMode.None)
            {
                this.m_Mobile.FocusMob = null;
                return false;
            }

            if (acqType == FightMode.Aggressor && this.m_Mobile.Aggressors.Count == 0 && this.m_Mobile.Aggressed.Count == 0 && this.m_Mobile.FactionAllegiance == null && this.m_Mobile.EthicAllegiance == null)
            {
                this.m_Mobile.FocusMob = null;
                return false;
            }

            if (this.m_Mobile.NextReacquireTime > DateTime.Now)
            {
                this.m_Mobile.FocusMob = null;
                return false;
            }

            this.m_Mobile.NextReacquireTime = DateTime.Now + this.m_Mobile.ReacquireDelay;

            this.m_Mobile.DebugSay("Acquiring...");

            Map map = this.m_Mobile.Map;

            if (map != null)
            {
                Mobile newFocusMob = null;
                double val = double.MinValue;
                double theirVal;

                IPooledEnumerable eable = map.GetMobilesInRange(this.m_Mobile.Location, iRange);

                foreach (Mobile m in eable)
                {
                    if (m.Deleted || m.Blessed)
                        continue;

                    // Let's not target ourselves...
                    if (m == this.m_Mobile || m is BaseFamiliar)
                        continue;

                    // Dead targets are invalid.
                    if (!m.Alive || m.IsDeadBondedPet)
                        continue;

                    // Staff members cannot be targeted.
                    if (m.IsStaff())
                        continue;

                    // Does it have to be a player?
                    if (bPlayerOnly && !m.Player)
                        continue;

                    // Can't acquire a target we can't see.
                    if (!this.m_Mobile.CanSee(m))
                        continue;

                    // Xmlspawner faction check
                    //if (!Server.Engines.XmlSpawner2.XmlMobFactions.CheckAcquire(this.m_Mobile, m))
                        //continue;

                    if (this.m_Mobile.Summoned && this.m_Mobile.SummonMaster != null)
                    {
                        // If this is a summon, it can't target its controller.
                        if (m == this.m_Mobile.SummonMaster)
                            continue;

                        // It also must abide by harmful spell rules.
                        if (!Server.Spells.SpellHelper.ValidIndirectTarget(this.m_Mobile.SummonMaster, m))
                            continue;

                        // Animated creatures cannot attack players directly.
                        if (m is PlayerMobile && this.m_Mobile.IsAnimatedDead)
                            continue;
                    }

                    // If we only want faction friends, make sure it's one.
                    if (bFacFriend && !this.m_Mobile.IsFriend(m))
                        continue;

                    //Ignore anyone under EtherealVoyage
                    if (TransformationSpellHelper.UnderTransformation(m, typeof(EtherealVoyageSpell)))
                        continue;

                    // Ignore players with activated honor
                    if (m is PlayerMobile && ((PlayerMobile)m).HonorActive && !(this.m_Mobile.Combatant == m))
                        continue;

                    if (acqType == FightMode.Aggressor || acqType == FightMode.Evil || (m is BaseCreature) && ((BaseCreature)m).Summoned)
                    {
                        BaseCreature bc = m as BaseCreature;

                        bool bValid = this.IsHostile(m);

                        if (!bValid && (!(m is BaseCreature) || !bc.Summoned || bc.Controlled))
                        {
                            bValid = (this.m_Mobile.GetFactionAllegiance(m) == BaseCreature.Allegiance.Enemy || this.m_Mobile.GetEthicAllegiance(m) == BaseCreature.Allegiance.Enemy);

                            if (acqType == FightMode.Evil && !bValid)
                            {
                                if (m is BaseCreature && bc.Controlled && bc.ControlMaster != null)
                                {
                                    bValid = (bc.ControlMaster.Karma < 0);
                                }
                                else
                                {
                                    bValid = ((Core.AOS || m.Player) && m.Karma < 0);
                                }
                            }
                        }

                        if (!bValid)
                            continue;
                    }
                    else
                    {
                        // Same goes for faction enemies.
                        if (bFacFoe && !this.m_Mobile.IsEnemy(m))
                            continue;

                        // If it's an enemy factioned mobile, make sure we can be harmful to it.
                        if (bFacFoe && !bFacFriend && !this.m_Mobile.CanBeHarmful(m, false))
                            continue;
                    }

                    theirVal = this.m_Mobile.GetFightModeRanking(m, acqType, bPlayerOnly);

                    if (theirVal > val && this.m_Mobile.InLOS(m))
                    {
                        newFocusMob = m;
                        val = theirVal;
                    }
                }

                eable.Free();

                this.m_Mobile.FocusMob = newFocusMob;

                if (this.m_Mobile.FocusMob is BaseFamiliar)
                {
                    this.m_Mobile.FocusMob = null;
                }
            }

            return (this.m_Mobile.FocusMob != null);
        }

        private bool IsHostile(Mobile from)
        {
            int count = Math.Max(this.m_Mobile.Aggressors.Count, this.m_Mobile.Aggressed.Count);

            if (this.m_Mobile.Combatant == from || from.Combatant == this.m_Mobile)
            {
                return true;
            }

            if (count > 0)
            {
                for (int a = 0; a < count; ++a)
                {
                    if (a < this.m_Mobile.Aggressed.Count && this.m_Mobile.Aggressed[a].Attacker == from)
                    {
                        return true;
                    }

                    if (a < this.m_Mobile.Aggressors.Count && this.m_Mobile.Aggressors[a].Defender == from)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public virtual void DetectHidden()
        {
            if (this.m_Mobile.Deleted || this.m_Mobile.Map == null)
                return;

            this.m_Mobile.DebugSay("Checking for hidden players");

            double srcSkill = this.m_Mobile.Skills[SkillName.DetectHidden].Value;

            if (srcSkill <= 0)
                return;

            foreach (Mobile trg in this.m_Mobile.GetMobilesInRange(this.m_Mobile.RangePerception))
            {
                if (trg != this.m_Mobile && trg.Player && trg.Alive && trg.Hidden && trg.IsPlayer() && this.m_Mobile.InLOS(trg))
                {
                    this.m_Mobile.DebugSay("Trying to detect {0}", trg.Name);

                    double trgHiding = trg.Skills[SkillName.Hiding].Value / 2.9;
                    double trgStealth = trg.Skills[SkillName.Stealth].Value / 1.8;

                    double chance = srcSkill / 1.2 - Math.Min(trgHiding, trgStealth);

                    if (chance < srcSkill / 10)
                        chance = srcSkill / 10;

                    chance /= 100;

                    if (chance > Utility.RandomDouble())
                    {
                        trg.RevealingAction();
                        trg.SendLocalizedMessage(500814); // You have been revealed!
                    }
                }
            }
        }

        public virtual void Deactivate()
        {
            if (this.m_Mobile.PlayerRangeSensitive)
            {
                this.m_Timer.Stop();

                SpawnEntry se = this.m_Mobile.Spawner as SpawnEntry;

                if (se != null && se.ReturnOnDeactivate && !this.m_Mobile.Controlled)
                {
                    if (se.HomeLocation == Point3D.Zero)
                    {
                        if (!this.m_Mobile.Region.AcceptsSpawnsFrom(se.Region))
                        {
                            Timer.DelayCall(TimeSpan.Zero, new TimerCallback(ReturnToHome));
                        }
                    }
                    else if (!this.m_Mobile.InRange(se.HomeLocation, se.HomeRange))
                    {
                        Timer.DelayCall(TimeSpan.Zero, new TimerCallback(ReturnToHome));
                    }
                }
            }
        }

        private void ReturnToHome()
        {
            SpawnEntry se = this.m_Mobile.Spawner as SpawnEntry;

            if (se != null)
            {
                Point3D loc = se.RandomSpawnLocation(16, !this.m_Mobile.CantWalk, this.m_Mobile.CanSwim);

                if (loc != Point3D.Zero)
                {
                    this.m_Mobile.MoveToWorld(loc, se.Region.Map);
                    return;
                }
            }
        }

        public virtual void Activate()
        {
            if (!this.m_Timer.Running)
            {
                this.m_Timer.Delay = TimeSpan.Zero;
                this.m_Timer.Start();
            }
        }

        /*
        *  The mobile changed it speed, we must ajust the timer
        */
        public virtual void OnCurrentSpeedChanged()
        {
            this.m_Timer.Stop();
            this.m_Timer.Delay = TimeSpan.FromSeconds(Utility.RandomDouble());
            this.m_Timer.Interval = TimeSpan.FromSeconds(Math.Max(0.0, this.m_Mobile.CurrentSpeed));
            this.m_Timer.Start();
        }

        private DateTime m_NextDetectHidden;

        public virtual bool CanDetectHidden
        {
            get
            {
                return this.m_Mobile.Skills[SkillName.DetectHidden].Value > 0;
            }
        }

        /*
        *  The Timer object
        */
        private class AITimer : Timer
        {
            private readonly BaseAI m_Owner;

            public AITimer(BaseAI owner)
                : base(TimeSpan.FromSeconds(Utility.RandomDouble()), TimeSpan.FromSeconds(Math.Max(0.0, owner.m_Mobile.CurrentSpeed)))
            {
                this.m_Owner = owner;

                this.m_Owner.m_NextDetectHidden = DateTime.Now;

                this.Priority = TimerPriority.FiftyMS;
            }

            protected override void OnTick()
            {
                if (this.m_Owner.m_Mobile.Deleted)
                {
                    this.Stop();
                    return;
                }
                else if (this.m_Owner.m_Mobile.Map == null || this.m_Owner.m_Mobile.Map == Map.Internal)
                {
                    this.m_Owner.Deactivate();
                    return;
                }
                else if (this.m_Owner.m_Mobile.PlayerRangeSensitive)//have to check this in the timer....
                {
                    Sector sect = this.m_Owner.m_Mobile.Map.GetSector(this.m_Owner.m_Mobile);
                    if (!sect.Active)
                    {
                        this.m_Owner.Deactivate();
                        return;
                    }
                }

                this.m_Owner.m_Mobile.OnThink();

                if (this.m_Owner.m_Mobile.Deleted)
                {
                    this.Stop();
                    return;
                }
                else if (this.m_Owner.m_Mobile.Map == null || this.m_Owner.m_Mobile.Map == Map.Internal)
                {
                    this.m_Owner.Deactivate();
                    return;
                }

                if (this.m_Owner.m_Mobile.BardPacified)
                {
                    this.m_Owner.DoBardPacified();
                }
                else if (this.m_Owner.m_Mobile.BardProvoked)
                {
                    this.m_Owner.DoBardProvoked();
                }
                else
                {
                    if (!this.m_Owner.m_Mobile.Controlled)
                    {
                        if (!this.m_Owner.Think())
                        {
                            this.Stop();
                            return;
                        }
                    }
                    else
                    {
                        if (!this.m_Owner.Obey())
                        {
                            this.Stop();
                            return;
                        }
                    }
                }

                if (this.m_Owner.CanDetectHidden && DateTime.Now > this.m_Owner.m_NextDetectHidden)
                {
                    this.m_Owner.DetectHidden();

                    // Not exactly OSI style, approximation.
                    int delay = (15000 / this.m_Owner.m_Mobile.Int);

                    if (delay > 60)
                        delay = 60;

                    int min = delay * (9 / 10); // 13s at 1000 int, 33s at 400 int, 54s at <250 int
                    int max = delay * (10 / 9); // 16s at 1000 int, 41s at 400 int, 66s at <250 int

                    this.m_Owner.m_NextDetectHidden = DateTime.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(min, max));
                }
            }
        }
    }
}