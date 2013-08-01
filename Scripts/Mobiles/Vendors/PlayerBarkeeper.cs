using System;
using System.Collections.Generic;
using Server.ContextMenus;
using Server.Gumps;
using Server.Items;
using Server.Multis;
using Server.Network;
using Server.Prompts;

namespace Server.Mobiles
{
    public class ChangeRumorMessagePrompt : Prompt
    {
        private readonly PlayerBarkeeper m_Barkeeper;
        private readonly int m_RumorIndex;
        public ChangeRumorMessagePrompt(PlayerBarkeeper barkeeper, int rumorIndex)
        {
            this.m_Barkeeper = barkeeper;
            this.m_RumorIndex = rumorIndex;
        }

        public override void OnCancel(Mobile from)
        {
            this.OnResponse(from, "");
        }

        public override void OnResponse(Mobile from, string text)
        {
            if (text.Length > 130)
                text = text.Substring(0, 130);

            this.m_Barkeeper.EndChangeRumor(from, this.m_RumorIndex, text);
        }
    }

    public class ChangeRumorKeywordPrompt : Prompt
    {
        private readonly PlayerBarkeeper m_Barkeeper;
        private readonly int m_RumorIndex;
        public ChangeRumorKeywordPrompt(PlayerBarkeeper barkeeper, int rumorIndex)
        {
            this.m_Barkeeper = barkeeper;
            this.m_RumorIndex = rumorIndex;
        }

        public override void OnCancel(Mobile from)
        {
            this.OnResponse(from, "");
        }

        public override void OnResponse(Mobile from, string text)
        {
            if (text.Length > 130)
                text = text.Substring(0, 130);

            this.m_Barkeeper.EndChangeKeyword(from, this.m_RumorIndex, text);
        }
    }

    public class ChangeTipMessagePrompt : Prompt
    {
        private readonly PlayerBarkeeper m_Barkeeper;
        public ChangeTipMessagePrompt(PlayerBarkeeper barkeeper)
        {
            this.m_Barkeeper = barkeeper;
        }

        public override void OnCancel(Mobile from)
        {
            this.OnResponse(from, "");
        }

        public override void OnResponse(Mobile from, string text)
        {
            if (text.Length > 130)
                text = text.Substring(0, 130);

            this.m_Barkeeper.EndChangeTip(from, text);
        }
    }

    public class BarkeeperRumor
    {
        private string m_Message;
        private string m_Keyword;
        public BarkeeperRumor(string message, string keyword)
        {
            this.m_Message = message;
            this.m_Keyword = keyword;
        }

        public string Message
        {
            get
            {
                return this.m_Message;
            }
            set
            {
                this.m_Message = value;
            }
        }
        public string Keyword
        {
            get
            {
                return this.m_Keyword;
            }
            set
            {
                this.m_Keyword = value;
            }
        }
        public static BarkeeperRumor Deserialize(GenericReader reader)
        {
            if (!reader.ReadBool())
                return null;

            return new BarkeeperRumor(reader.ReadString(), reader.ReadString());
        }

        public static void Serialize(GenericWriter writer, BarkeeperRumor rumor)
        {
            if (rumor == null)
            {
                writer.Write(false);
            }
            else
            {
                writer.Write(true);
                writer.Write(rumor.m_Message);
                writer.Write(rumor.m_Keyword);
            }
        }
    }

    public class ManageBarkeeperEntry : ContextMenuEntry
    {
        private readonly Mobile m_From;
        private readonly PlayerBarkeeper m_Barkeeper;
        public ManageBarkeeperEntry(Mobile from, PlayerBarkeeper barkeeper)
            : base(6151, 12)
        {
            this.m_From = from;
            this.m_Barkeeper = barkeeper;
        }

        public override void OnClick()
        {
            this.m_Barkeeper.BeginManagement(this.m_From);
        }
    }

    public class PlayerBarkeeper : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new List<SBInfo>();
        private Mobile m_Owner;
        private BaseHouse m_House;
        private string m_TipMessage;
        private BarkeeperRumor[] m_Rumors;
        private Timer m_NewsTimer;
        public PlayerBarkeeper(Mobile owner, BaseHouse house)
            : base("the barkeeper")
        {
            this.m_Owner = owner;
            this.House = house;
            this.m_Rumors = new BarkeeperRumor[3];

            this.LoadSBInfo();
        }

        public PlayerBarkeeper(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
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
        public BaseHouse House
        {
            get
            {
                return this.m_House;
            }
            set
            {
                if (this.m_House != null)
                    this.m_House.PlayerBarkeepers.Remove(this);

                if (value != null)
                    value.PlayerBarkeepers.Add(this);

                this.m_House = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public string TipMessage
        {
            get
            {
                return this.m_TipMessage;
            }
            set
            {
                this.m_TipMessage = value;
            }
        }
        public override bool IsActiveBuyer
        {
            get
            {
                return false;
            }
        }
        public override bool IsActiveSeller
        {
            get
            {
                return (this.m_SBInfos.Count > 0);
            }
        }
        public override bool DisallowAllMoves
        {
            get
            {
                return true;
            }
        }
        public override bool NoHouseRestrictions
        {
            get
            {
                return true;
            }
        }
        public BarkeeperRumor[] Rumors
        {
            get
            {
                return this.m_Rumors;
            }
        }
        public override VendorShoeType ShoeType
        {
            get
            {
                return Utility.RandomBool() ? VendorShoeType.ThighBoots : VendorShoeType.Boots;
            }
        }
        protected override List<SBInfo> SBInfos
        {
            get
            {
                return this.m_SBInfos;
            }
        }
        public override bool GetGender()
        {
            return false; // always starts as male
        }

        public override void InitOutfit()
        {
            base.InitOutfit();

            this.AddItem(new HalfApron(this.RandomBrightHue()));

            Container pack = this.Backpack;

            if (pack != null)
                pack.Delete();
        }

        public override void InitBody()
        {
            base.InitBody();

            if (this.BodyValue == 0x340 || this.BodyValue == 0x402)
                this.Hue = 0;
            else
                this.Hue = 0x83F4; // hue is not random

            Container pack = this.Backpack;

            if (pack != null)
                pack.Delete();
        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            if (this.InRange(from, 3))
                return true;

            return base.HandlesOnSpeech(from);
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            this.House = null;
        }

        public override bool OnBeforeDeath()
        {
            if (!base.OnBeforeDeath())
                return false;

            Item shoes = this.FindItemOnLayer(Layer.Shoes);

            if (shoes is Sandals)
                shoes.Hue = 0;

            return true;
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            base.OnSpeech(e);

            if (!e.Handled && this.InRange(e.Mobile, 3))
            {
                if (this.m_NewsTimer == null && e.HasKeyword(0x30)) // *news*
                {
                    TownCrierEntry tce = GlobalTownCrierEntryList.Instance.GetRandomEntry();

                    if (tce == null)
                    {
                        this.PublicOverheadMessage(MessageType.Regular, 0x3B2, 1005643); // I have no news at this time.
                    }
                    else
                    {
                        this.m_NewsTimer = Timer.DelayCall(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(3.0), new TimerStateCallback(ShoutNews_Callback), new object[] { tce, 0 });

                        this.PublicOverheadMessage(MessageType.Regular, 0x3B2, 502978); // Some of the latest news!
                    }
                }

                for (int i = 0; i < this.m_Rumors.Length; ++i)
                {
                    BarkeeperRumor rumor = this.m_Rumors[i];

                    if (rumor == null)
                        continue;

                    string keyword = rumor.Keyword;

                    if (keyword == null || (keyword = keyword.Trim()).Length == 0)
                        continue;

                    if (Insensitive.Equals(keyword, e.Speech))
                    {
                        string message = rumor.Message;

                        if (message == null || (message = message.Trim()).Length == 0)
                            continue;

                        this.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, message);
                    }
                }
            }
        }

        public override bool CheckGold(Mobile from, Item dropped)
        {
            if (dropped is Gold)
            {
                Gold g = (Gold)dropped;

                if (g.Amount > 50)
                {
                    this.PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, "I cannot accept so large a tip!", from.NetState);
                }
                else
                {
                    string tip = this.m_TipMessage;

                    if (tip == null || (tip = tip.Trim()).Length == 0)
                    {
                        this.PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, "It would not be fair of me to take your money and not offer you information in return.", from.NetState);
                    }
                    else
                    {
                        this.Direction = this.GetDirectionTo(from);
                        this.PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, tip, from.NetState);

                        g.Delete();
                        return true;
                    }
                }
            }

            return false;
        }

        public bool IsOwner(Mobile from)
        {
            if (from == null || from.Deleted || this.Deleted)
                return false;

            if (from.AccessLevel > AccessLevel.GameMaster)
                return true;

            return (this.m_Owner == from);
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            if (this.IsOwner(from) && from.InLOS(this))
                list.Add(new ManageBarkeeperEntry(from, this));
        }

        public void BeginManagement(Mobile from)
        {
            if (!this.IsOwner(from))
                return;

            from.SendGump(new BarkeeperGump(from, this));
        }

        public void Dismiss()
        {
            this.Delete();
        }

        public void BeginChangeRumor(Mobile from, int index)
        {
            if (index < 0 || index >= this.m_Rumors.Length)
                return;

            from.Prompt = new ChangeRumorMessagePrompt(this, index);
            this.PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, "Say what news you would like me to tell our guests.", from.NetState);
        }

        public void EndChangeRumor(Mobile from, int index, string text)
        {
            if (index < 0 || index >= this.m_Rumors.Length)
                return;

            if (this.m_Rumors[index] == null)
                this.m_Rumors[index] = new BarkeeperRumor(text, null);
            else
                this.m_Rumors[index].Message = text;

            from.Prompt = new ChangeRumorKeywordPrompt(this, index);
            this.PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, "What keyword should a guest say to me to get this news?", from.NetState);
        }

        public void EndChangeKeyword(Mobile from, int index, string text)
        {
            if (index < 0 || index >= this.m_Rumors.Length)
                return;

            if (this.m_Rumors[index] == null)
                this.m_Rumors[index] = new BarkeeperRumor(null, text);
            else
                this.m_Rumors[index].Keyword = text;

            this.PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, "I'll pass on the message.", from.NetState);
        }

        public void RemoveRumor(Mobile from, int index)
        {
            if (index < 0 || index >= this.m_Rumors.Length)
                return;

            this.m_Rumors[index] = null;
        }

        public void BeginChangeTip(Mobile from)
        {
            from.Prompt = new ChangeTipMessagePrompt(this);
            this.PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, "Say what you want me to tell guests when they give me a good tip.", from.NetState);
        }

        public void EndChangeTip(Mobile from, string text)
        {
            this.m_TipMessage = text;
            this.PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, "I'll say that to anyone who gives me a good tip.", from.NetState);
        }

        public void RemoveTip(Mobile from)
        {
            this.m_TipMessage = null;
        }

        public void BeginChangeTitle(Mobile from)
        {
            from.SendGump(new BarkeeperTitleGump(from, this));
        }

        public void EndChangeTitle(Mobile from, string title, bool vendor)
        {
            this.Title = title;

            this.LoadSBInfo();
        }

        public void CancelChangeTitle(Mobile from)
        {
            from.SendGump(new BarkeeperGump(from, this));
        }

        public void BeginChangeAppearance(Mobile from)
        {
            from.CloseGump(typeof(PlayerVendorCustomizeGump));
            from.SendGump(new PlayerVendorCustomizeGump(this, from));
        }

        public void ChangeGender(Mobile from)
        {
            this.Female = !this.Female;

            if (this.Female)
            {
                this.Body = 401;
                this.Name = NameList.RandomName("female");

                this.FacialHairItemID = 0;
            }
            else
            {
                this.Body = 400;
                this.Name = NameList.RandomName("male");
            }
        }

        public override void InitSBInfo()
        {
            if (this.Title == "the waiter" || this.Title == "the barkeeper" || this.Title == "the baker" || this.Title == "the innkeeper" || this.Title == "the chef")
            {
                if (this.m_SBInfos.Count == 0)
                    this.m_SBInfos.Add(new SBPlayerBarkeeper());
            }
            else
            {
                this.m_SBInfos.Clear();
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version;

            writer.Write((Item)this.m_House);

            writer.Write((Mobile)this.m_Owner);

            writer.WriteEncodedInt((int)this.m_Rumors.Length);

            for (int i = 0; i < this.m_Rumors.Length; ++i)
                BarkeeperRumor.Serialize(writer, this.m_Rumors[i]);

            writer.Write((string)this.m_TipMessage);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch ( version )
            {
                case 1:
                    {
                        this.House = (BaseHouse)reader.ReadItem();

                        goto case 0;
                    }
                case 0:
                    {
                        this.m_Owner = reader.ReadMobile();

                        this.m_Rumors = new BarkeeperRumor[reader.ReadEncodedInt()];

                        for (int i = 0; i < this.m_Rumors.Length; ++i)
                            this.m_Rumors[i] = BarkeeperRumor.Deserialize(reader);

                        this.m_TipMessage = reader.ReadString();

                        break;
                    }
            }

            if (version < 1)
                Timer.DelayCall(TimeSpan.Zero, new TimerCallback(UpgradeFromVersion0));
        }

        private void ShoutNews_Callback(object state)
        {
            object[] states = (object[])state;
            TownCrierEntry tce = (TownCrierEntry)states[0];
            int index = (int)states[1];

            if (index < 0 || index >= tce.Lines.Length)
            {
                if (this.m_NewsTimer != null)
                    this.m_NewsTimer.Stop();

                this.m_NewsTimer = null;
            }
            else
            {
                this.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, tce.Lines[index]);
                states[1] = index + 1;
            }
        }

        private void UpgradeFromVersion0()
        {
            this.House = BaseHouse.FindHouseAt(this);
        }
    }

    public class BarkeeperTitleGump : Gump
    {
        private static readonly Entry[] m_Entries = new Entry[]
        {
            new Entry("Alchemist"),
            new Entry("Animal Tamer"),
            new Entry("Apothecary"),
            new Entry("Artist"),
            new Entry("Baker", true),
            new Entry("Bard"),
            new Entry("Barkeep", "the barkeeper", true),
            new Entry("Beggar"),
            new Entry("Blacksmith"),
            new Entry("Bounty Hunter"),
            new Entry("Brigand"),
            new Entry("Butler"),
            new Entry("Carpenter"),
            new Entry("Chef", true),
            new Entry("Commander"),
            new Entry("Curator"),
            new Entry("Drunkard"),
            new Entry("Farmer"),
            new Entry("Fisherman"),
            new Entry("Gambler"),
            new Entry("Gypsy"),
            new Entry("Herald"),
            new Entry("Herbalist"),
            new Entry("Hermit"),
            new Entry("Innkeeper", true),
            new Entry("Jailor"),
            new Entry("Jester"),
            new Entry("Librarian"),
            new Entry("Mage"),
            new Entry("Mercenary"),
            new Entry("Merchant"),
            new Entry("Messenger"),
            new Entry("Miner"),
            new Entry("Monk"),
            new Entry("Noble"),
            new Entry("Paladin"),
            new Entry("Peasant"),
            new Entry("Pirate"),
            new Entry("Prisoner"),
            new Entry("Prophet"),
            new Entry("Ranger"),
            new Entry("Sage"),
            new Entry("Sailor"),
            new Entry("Scholar"),
            new Entry("Scribe"),
            new Entry("Sentry"),
            new Entry("Servant"),
            new Entry("Shepherd"),
            new Entry("Soothsayer"),
            new Entry("Stoic"),
            new Entry("Storyteller"),
            new Entry("Tailor"),
            new Entry("Thief"),
            new Entry("Tinker"),
            new Entry("Town Crier"),
            new Entry("Treasure Hunter"),
            new Entry("Waiter", true),
            new Entry("Warrior"),
            new Entry("Watchman"),
            new Entry("No Title", null, false)
        };
        private readonly Mobile m_From;
        private readonly PlayerBarkeeper m_Barkeeper;
        public BarkeeperTitleGump(Mobile from, PlayerBarkeeper barkeeper)
            : base(0, 0)
        {
            this.m_From = from;
            this.m_Barkeeper = barkeeper;

            from.CloseGump(typeof(BarkeeperGump));
            from.CloseGump(typeof(BarkeeperTitleGump));

            Entry[] entries = m_Entries;

            this.RenderBackground();

            int pageCount = (entries.Length + 19) / 20;

            for (int i = 0; i < pageCount; ++i)
                this.RenderPage(entries, i);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            int buttonID = info.ButtonID;

            if (buttonID > 0)
            {
                --buttonID;

                if (buttonID > 0)
                {
                    --buttonID;

                    if (buttonID >= 0 && buttonID < m_Entries.Length)
                        this.m_Barkeeper.EndChangeTitle(this.m_From, m_Entries[buttonID].m_Title, m_Entries[buttonID].m_Vendor);
                }
                else
                {
                    this.m_Barkeeper.CancelChangeTitle(this.m_From);
                }
            }
        }

        private void RenderBackground()
        {
            this.AddPage(0);

            this.AddBackground(30, 40, 585, 410, 5054);

            this.AddImage(30, 40, 9251);
            this.AddImage(180, 40, 9251);
            this.AddImage(30, 40, 9253);
            this.AddImage(30, 130, 9253);
            this.AddImage(598, 40, 9255);
            this.AddImage(598, 130, 9255);
            this.AddImage(30, 433, 9257);
            this.AddImage(180, 433, 9257);
            this.AddImage(30, 40, 9250);
            this.AddImage(598, 40, 9252);
            this.AddImage(598, 433, 9258);
            this.AddImage(30, 433, 9256);

            this.AddItem(30, 40, 6816);
            this.AddItem(30, 125, 6817);
            this.AddItem(30, 233, 6817);
            this.AddItem(30, 341, 6817);
            this.AddItem(580, 40, 6814);
            this.AddItem(588, 125, 6815);
            this.AddItem(588, 233, 6815);
            this.AddItem(588, 341, 6815);

            this.AddImage(560, 20, 1417);
            this.AddItem(580, 44, 4033);

            this.AddBackground(183, 25, 280, 30, 5054);

            this.AddImage(180, 25, 10460);
            this.AddImage(434, 25, 10460);

            this.AddHtml(223, 32, 200, 40, "BARKEEP CUSTOMIZATION MENU", false, false);
            this.AddBackground(243, 433, 150, 30, 5054);

            this.AddImage(240, 433, 10460);
            this.AddImage(375, 433, 10460);

            this.AddImage(80, 398, 2151);
            this.AddItem(72, 406, 2543);

            this.AddHtml(110, 412, 180, 25, "sells food and drink", false, false);
        }

        private void RenderPage(Entry[] entries, int page)
        {
            this.AddPage(1 + page);

            this.AddHtml(430, 70, 180, 25, String.Format("Page {0} of {1}", page + 1, (entries.Length + 19) / 20), false, false);

            for (int count = 0, i = (page * 20); count < 20 && i < entries.Length; ++count, ++i)
            {
                Entry entry = entries[i];

                this.AddButton(80 + ((count / 10) * 260), 100 + ((count % 10) * 30), 4005, 4007, 2 + i, GumpButtonType.Reply, 0);
                this.AddHtml(120 + ((count / 10) * 260), 100 + ((count % 10) * 30), entry.m_Vendor ? 148 : 180, 25, entry.m_Description, true, false);

                if (entry.m_Vendor)
                {
                    this.AddImage(270 + ((count / 10) * 260), 98 + ((count % 10) * 30), 2151);
                    this.AddItem(262 + ((count / 10) * 260), 106 + ((count % 10) * 30), 2543);
                }
            }

            this.AddButton(340, 400, 4005, 4007, 0, GumpButtonType.Page, 1 + ((page + 1) % ((entries.Length + 19) / 20)));
            this.AddHtml(380, 400, 180, 25, "More Job Titles", false, false);

            this.AddButton(338, 437, 4014, 4016, 1, GumpButtonType.Reply, 0);
            this.AddHtml(290, 440, 35, 40, "Back", false, false);
        }

        private class Entry
        {
            public readonly string m_Description;
            public readonly string m_Title;
            public readonly bool m_Vendor;
            public Entry(string desc)
                : this(desc, String.Format("the {0}", desc.ToLower()), false)
            {
            }

            public Entry(string desc, bool vendor)
                : this(desc, String.Format("the {0}", desc.ToLower()), vendor)
            {
            }

            public Entry(string desc, string title, bool vendor)
            {
                this.m_Description = desc;
                this.m_Title = title;
                this.m_Vendor = vendor;
            }
        }
    }

    public class BarkeeperGump : Gump
    {
        private readonly Mobile m_From;
        private readonly PlayerBarkeeper m_Barkeeper;
        public BarkeeperGump(Mobile from, PlayerBarkeeper barkeeper)
            : base(0, 0)
        {
            this.m_From = from;
            this.m_Barkeeper = barkeeper;

            from.CloseGump(typeof(BarkeeperGump));
            from.CloseGump(typeof(BarkeeperTitleGump));

            this.RenderBackground();
            this.RenderCategories();
            this.RenderMessageManagement();
            this.RenderDismissConfirmation();
            this.RenderMessageManagement_Message_AddOrChange();
            this.RenderMessageManagement_Message_Remove();
            this.RenderMessageManagement_Tip_AddOrChange();
            this.RenderMessageManagement_Tip_Remove();
            this.RenderAppearanceCategories();
        }

        public void RenderBackground()
        {
            this.AddPage(0);

            this.AddBackground(30, 40, 585, 410, 5054);

            this.AddImage(30, 40, 9251);
            this.AddImage(180, 40, 9251);
            this.AddImage(30, 40, 9253);
            this.AddImage(30, 130, 9253);
            this.AddImage(598, 40, 9255);
            this.AddImage(598, 130, 9255);
            this.AddImage(30, 433, 9257);
            this.AddImage(180, 433, 9257);
            this.AddImage(30, 40, 9250);
            this.AddImage(598, 40, 9252);
            this.AddImage(598, 433, 9258);
            this.AddImage(30, 433, 9256);

            this.AddItem(30, 40, 6816);
            this.AddItem(30, 125, 6817);
            this.AddItem(30, 233, 6817);
            this.AddItem(30, 341, 6817);
            this.AddItem(580, 40, 6814);
            this.AddItem(588, 125, 6815);
            this.AddItem(588, 233, 6815);
            this.AddItem(588, 341, 6815);

            this.AddBackground(183, 25, 280, 30, 5054);

            this.AddImage(180, 25, 10460);
            this.AddImage(434, 25, 10460);
            this.AddImage(560, 20, 1417);

            this.AddHtml(223, 32, 200, 40, "BARKEEP CUSTOMIZATION MENU", false, false);
            this.AddBackground(243, 433, 150, 30, 5054);

            this.AddImage(240, 433, 10460);
            this.AddImage(375, 433, 10460);
        }

        public void RenderCategories()
        {
            this.AddPage(1);

            this.AddButton(130, 120, 4005, 4007, 0, GumpButtonType.Page, 2);
            this.AddHtml(170, 120, 200, 40, "Message Control", false, false);

            this.AddButton(130, 200, 4005, 4007, 0, GumpButtonType.Page, 8);
            this.AddHtml(170, 200, 200, 40, "Customize your barkeep", false, false);

            this.AddButton(130, 280, 4005, 4007, 0, GumpButtonType.Page, 3);
            this.AddHtml(170, 280, 200, 40, "Dismiss your barkeep", false, false);

            this.AddButton(338, 437, 4014, 4016, 0, GumpButtonType.Reply, 0);
            this.AddHtml(290, 440, 35, 40, "Back", false, false);

            this.AddItem(574, 43, 5360);
        }

        public void RenderMessageManagement()
        {
            this.AddPage(2);

            this.AddButton(130, 120, 4005, 4007, 0, GumpButtonType.Page, 4);
            this.AddHtml(170, 120, 380, 20, "Add or change a message and keyword", false, false);

            this.AddButton(130, 200, 4005, 4007, 0, GumpButtonType.Page, 5);
            this.AddHtml(170, 200, 380, 20, "Remove a message and keyword from your barkeep", false, false);

            this.AddButton(130, 280, 4005, 4007, 0, GumpButtonType.Page, 6);
            this.AddHtml(170, 280, 380, 20, "Add or change your barkeeper's tip message", false, false);

            this.AddButton(130, 360, 4005, 4007, 0, GumpButtonType.Page, 7);
            this.AddHtml(170, 360, 380, 20, "Delete your barkeepers tip message", false, false);

            this.AddButton(338, 437, 4014, 4016, 0, GumpButtonType.Page, 1);
            this.AddHtml(290, 440, 35, 40, "Back", false, false);

            this.AddItem(580, 46, 4030);
        }

        public void RenderDismissConfirmation()
        {
            this.AddPage(3);

            this.AddHtml(170, 160, 380, 20, "Are you sure you want to dismiss your barkeeper?", false, false);

            this.AddButton(205, 280, 4005, 4007, this.GetButtonID(0, 0), GumpButtonType.Reply, 0);
            this.AddHtml(240, 280, 100, 20, @"Yes", false, false);

            this.AddButton(395, 280, 4005, 4007, 0, GumpButtonType.Reply, 0);
            this.AddHtml(430, 280, 100, 20, "No", false, false);

            this.AddButton(338, 437, 4014, 4016, 0, GumpButtonType.Page, 1);
            this.AddHtml(290, 440, 35, 40, "Back", false, false);

            this.AddItem(574, 43, 5360);
            this.AddItem(584, 34, 6579);
        }

        public void RenderMessageManagement_Message_AddOrChange()
        {
            this.AddPage(4);

            this.AddHtml(250, 60, 500, 25, "Add or change a message", false, false);

            BarkeeperRumor[] rumors = this.m_Barkeeper.Rumors;

            for (int i = 0; i < rumors.Length; ++i)
            {
                BarkeeperRumor rumor = rumors[i];

                this.AddHtml(100, 70 + (i * 120), 50, 20, "Message", false, false);
                this.AddHtml(100, 90 + (i * 120), 450, 40, rumor == null ? "No current message" : rumor.Message, true, false);
                this.AddHtml(100, 130 + (i * 120), 50, 20, "Keyword", false, false);
                this.AddHtml(100, 150 + (i * 120), 450, 40, rumor == null ? "None" : rumor.Keyword, true, false);

                this.AddButton(60, 90 + (i * 120), 4005, 4007, this.GetButtonID(1, i), GumpButtonType.Reply, 0);
            }

            this.AddButton(338, 437, 4014, 4016, 0, GumpButtonType.Page, 2);
            this.AddHtml(290, 440, 35, 40, "Back", false, false);

            this.AddItem(580, 46, 4030);
        }

        public void RenderMessageManagement_Message_Remove()
        {
            this.AddPage(5);

            this.AddHtml(190, 60, 500, 25, "Choose the message you would like to remove", false, false);

            BarkeeperRumor[] rumors = this.m_Barkeeper.Rumors;

            for (int i = 0; i < rumors.Length; ++i)
            {
                BarkeeperRumor rumor = rumors[i];

                this.AddHtml(100, 70 + (i * 120), 50, 20, "Message", false, false);
                this.AddHtml(100, 90 + (i * 120), 450, 40, rumor == null ? "No current message" : rumor.Message, true, false);
                this.AddHtml(100, 130 + (i * 120), 50, 20, "Keyword", false, false);
                this.AddHtml(100, 150 + (i * 120), 450, 40, rumor == null ? "None" : rumor.Keyword, true, false);

                this.AddButton(60, 90 + (i * 120), 4005, 4007, this.GetButtonID(2, i), GumpButtonType.Reply, 0);
            }

            this.AddButton(338, 437, 4014, 4016, 0, GumpButtonType.Page, 2);
            this.AddHtml(290, 440, 35, 40, "Back", false, false);

            this.AddItem(580, 46, 4030);
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            if (!this.m_Barkeeper.IsOwner(this.m_From))
                return;

            int index = info.ButtonID - 1;

            if (index < 0)
                return;

            int type = index % 6;
            index /= 6;

            switch ( type )
            {
                case 0: // Controls
                    {
                        switch ( index )
                        {
                            case 0: // Dismiss
                                {
                                    this.m_Barkeeper.Dismiss();
                                    break;
                                }
                        }

                        break;
                    }
                case 1: // Change message
                    {
                        this.m_Barkeeper.BeginChangeRumor(this.m_From, index);
                        break;
                    }
                case 2: // Remove message
                    {
                        this.m_Barkeeper.RemoveRumor(this.m_From, index);
                        break;
                    }
                case 3: // Change tip
                    {
                        this.m_Barkeeper.BeginChangeTip(this.m_From);
                        break;
                    }
                case 4: // Remove tip
                    {
                        this.m_Barkeeper.RemoveTip(this.m_From);
                        break;
                    }
                case 5: // Appearance category selection
                    {
                        switch ( index )
                        {
                            case 0:
                                this.m_Barkeeper.BeginChangeTitle(this.m_From);
                                break;
                            case 1:
                                this.m_Barkeeper.BeginChangeAppearance(this.m_From);
                                break;
                            case 2:
                                this.m_Barkeeper.ChangeGender(this.m_From);
                                break;
                        }

                        break;
                    }
            }
        }

        private int GetButtonID(int type, int index)
        {
            return 1 + (index * 6) + type;
        }

        private void RenderMessageManagement_Tip_AddOrChange()
        {
            this.AddPage(6);

            this.AddHtml(250, 95, 500, 20, "Change this tip message", false, false);
            this.AddHtml(100, 190, 50, 20, "Message", false, false);
            this.AddHtml(100, 210, 450, 40, this.m_Barkeeper.TipMessage == null ? "No current message" : this.m_Barkeeper.TipMessage, true, false);

            this.AddButton(60, 210, 4005, 4007, this.GetButtonID(3, 0), GumpButtonType.Reply, 0);

            this.AddButton(338, 437, 4014, 4016, 0, GumpButtonType.Page, 2);
            this.AddHtml(290, 440, 35, 40, "Back", false, false);

            this.AddItem(580, 46, 4030);
        }

        private void RenderMessageManagement_Tip_Remove()
        {
            this.AddPage(7);

            this.AddHtml(250, 95, 500, 20, "Remove this tip message", false, false);
            this.AddHtml(100, 190, 50, 20, "Message", false, false);
            this.AddHtml(100, 210, 450, 40, this.m_Barkeeper.TipMessage == null ? "No current message" : this.m_Barkeeper.TipMessage, true, false);

            this.AddButton(60, 210, 4005, 4007, this.GetButtonID(4, 0), GumpButtonType.Reply, 0);

            this.AddButton(338, 437, 4014, 4016, 0, GumpButtonType.Page, 2);
            this.AddHtml(290, 440, 35, 40, "Back", false, false);

            this.AddItem(580, 46, 4030);
        }

        private void RenderAppearanceCategories()
        {
            this.AddPage(8);

            this.AddButton(130, 120, 4005, 4007, this.GetButtonID(5, 0), GumpButtonType.Reply, 0);
            this.AddHtml(170, 120, 120, 20, "Title", false, false);

            if (this.m_Barkeeper.BodyValue != 0x340 && this.m_Barkeeper.BodyValue != 0x402)
            {
                this.AddButton(130, 200, 4005, 4007, this.GetButtonID(5, 1), GumpButtonType.Reply, 0);
                this.AddHtml(170, 200, 120, 20, "Appearance", false, false);

                this.AddButton(130, 280, 4005, 4007, this.GetButtonID(5, 2), GumpButtonType.Reply, 0);
                this.AddHtml(170, 280, 120, 20, "Male / Female", false, false);

                this.AddButton(338, 437, 4014, 4016, 0, GumpButtonType.Page, 1);
                this.AddHtml(290, 440, 35, 40, "Back", false, false);
            }

            this.AddItem(580, 44, 4033);
        }
    }
}