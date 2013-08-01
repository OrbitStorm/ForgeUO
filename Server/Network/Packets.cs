using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Server.Accounting;
using Server.ContextMenus;
using Server.Diagnostics;
using Server.Gumps;
using Server.HuePickers;
using Server.Items;
using Server.Menus;
using Server.Menus.ItemLists;
using Server.Menus.Questions;
using Server.Mobiles;
using Server.Prompts;
using Server.Targeting;

namespace Server.Network
{
    public enum PMMessage : byte
    {
        CharNoExist = 1,
        CharExists = 2,
        CharInWorld = 5,
        LoginSyncError = 6,
        IdleWarning = 7
    }

    public enum LRReason : byte
    {
        CannotLift = 0,
        OutOfRange = 1,
        OutOfSight = 2,
        TryToSteal = 3,
        AreHolding = 4,
        Inspecific = 5
    }

    public enum CMEFlags
    {
        None = 0x00,
        Disabled = 0x01,
        Colored = 0x20
    }

    public enum EffectType
    {
        Moving = 0x00,
        Lightning = 0x01,
        FixedXYZ = 0x02,
        FixedFrom = 0x03
    }

    public enum ScreenEffectType
    {
        FadeOut = 0x00,
        FadeIn = 0x01,
        LightFlash = 0x02,
        FadeInOut = 0x03,
        DarkFlash = 0x04
    }

    public enum DeleteResultType
    {
        PasswordInvalid,
        CharNotExist,
        CharBeingPlayed,
        CharTooYoung,
        CharQueued,
        BadRequest
    }

    public enum ALRReason : byte
    {
        Invalid = 0x00,
        InUse = 0x01,
        Blocked = 0x02,
        BadPass = 0x03,
        Idle = 0xFE,
        BadComm = 0xFF
    }

    public enum AffixType : byte
    {
        Append = 0x00,
        Prepend = 0x01,
        System = 0x02
    }

    public interface IGumpWriter
    {
        int TextEntries { get; set; }
        int Switches { get; set; }
        void AppendLayout(bool val);

        void AppendLayout(int val);

        void AppendLayoutNS(int val);

        void AppendLayout(string text);

        void AppendLayout(byte[] buffer);

        void WriteStrings(List<string> strings);

        void Flush();
    }

    public static class AttributeNormalizer
    {
        private static int m_Maximum = 25;
        private static bool m_Enabled = true;
        public static int Maximum
        {
            get
            {
                return m_Maximum;
            }
            set
            {
                m_Maximum = value;
            }
        }
        public static bool Enabled
        {
            get
            {
                return m_Enabled;
            }
            set
            {
                m_Enabled = value;
            }
        }
        public static void Write(PacketWriter stream, int cur, int max)
        {
            if (m_Enabled && max != 0)
            {
                stream.Write((short)m_Maximum);
                stream.Write((short)((cur * m_Maximum) / max));
            }
            else
            {
                stream.Write((short)max);
                stream.Write((short)cur);
            }
        }

        public static void WriteReverse(PacketWriter stream, int cur, int max)
        {
            if (m_Enabled && max != 0)
            {
                stream.Write((short)((cur * m_Maximum) / max));
                stream.Write((short)m_Maximum);
            }
            else
            {
                stream.Write((short)cur);
                stream.Write((short)max);
            }
        }
    }

    /*public enum CMEFlags
    {
    None = 0x00,
    Locked = 0x01,
    Arrow = 0x02,
    x0004 = 0x04,
    Color = 0x20,
    x0040 = 0x40,
    x0080 = 0x80
    }*/
    public sealed class DamagePacketOld : Packet
    {
        public DamagePacketOld(Mobile m, int amount)
            : base(0xBF)
        {
            this.EnsureCapacity(11);

            this.m_Stream.Write((short)0x22);
            this.m_Stream.Write((byte)1);
            this.m_Stream.Write((int)m.Serial);

            if (amount > 255)
                amount = 255;
            else if (amount < 0)
                amount = 0;

            this.m_Stream.Write((byte)amount);
        }
    }

    public sealed class DamagePacket : Packet
    {
        public DamagePacket(Mobile m, int amount)
            : base(0x0B, 7)
        {
            this.m_Stream.Write((int)m.Serial);

            if (amount > 0xFFFF)
                amount = 0xFFFF;
            else if (amount < 0)
                amount = 0;

            this.m_Stream.Write((ushort)amount);
        }
        /*public DamagePacket( Mobile m, int amount ) : base( 0xBF )
        {
        EnsureCapacity( 11 );
        m_Stream.Write( (short) 0x22 );
        m_Stream.Write( (byte) 1 );
        m_Stream.Write( (int) m.Serial );
        if ( amount > 255 )
        amount = 255;
        else if ( amount < 0 )
        amount = 0;
        m_Stream.Write( (byte)amount );
        }*/
    }

    public sealed class CancelArrow : Packet
    {
        public CancelArrow()
            : base(0xBA, 6)
        {
            this.m_Stream.Write((byte)0);
            this.m_Stream.Write((short)-1);
            this.m_Stream.Write((short)-1);
        }
    }

    public sealed class SetArrow : Packet
    {
        public SetArrow(int x, int y)
            : base(0xBA, 6)
        {
            this.m_Stream.Write((byte)1);
            this.m_Stream.Write((short)x);
            this.m_Stream.Write((short)y);
        }
    }

    public sealed class CancelArrowHS : Packet
    {
        public CancelArrowHS(int x, int y, Serial s)
            : base(0xBA, 10)
        {
            this.m_Stream.Write((byte)0);
            this.m_Stream.Write((short)x);
            this.m_Stream.Write((short)y);
            this.m_Stream.Write((int)s);
        }
    }

    public sealed class SetArrowHS : Packet
    {
        public SetArrowHS(int x, int y, Serial s)
            : base(0xBA, 10)
        {
            this.m_Stream.Write((byte)1);
            this.m_Stream.Write((short)x);
            this.m_Stream.Write((short)y);
            this.m_Stream.Write((int)s);
        }
    }

    public sealed class DisplaySecureTrade : Packet
    {
        public DisplaySecureTrade(Mobile them, Container first, Container second, string name)
            : base(0x6F)
        {
            if (name == null)
                name = "";

            this.EnsureCapacity(18 + name.Length);

            this.m_Stream.Write((byte)0); // Display
            this.m_Stream.Write((int)them.Serial);
            this.m_Stream.Write((int)first.Serial);
            this.m_Stream.Write((int)second.Serial);
            this.m_Stream.Write((bool)true);

            this.m_Stream.WriteAsciiFixed(name, 30);
        }
    }

    public sealed class CloseSecureTrade : Packet
    {
        public CloseSecureTrade(Container cont)
            : base(0x6F)
        {
            this.EnsureCapacity(8);

            this.m_Stream.Write((byte)1); // Close
            this.m_Stream.Write((int)cont.Serial);
        }
    }

    public sealed class UpdateSecureTrade : Packet
    {
        public UpdateSecureTrade(Container cont, bool first, bool second)
            : base(0x6F)
        {
            this.EnsureCapacity(8);

            this.m_Stream.Write((byte)2); // Update
            this.m_Stream.Write((int)cont.Serial);
            this.m_Stream.Write((int)(first ? 1 : 0));
            this.m_Stream.Write((int)(second ? 1 : 0));
        }
    }

    public sealed class SecureTradeEquip : Packet
    {
        public SecureTradeEquip(Item item, Mobile m)
            : base(0x25, 20)
        {
            this.m_Stream.Write((int)item.Serial);
            this.m_Stream.Write((short)item.ItemID);
            this.m_Stream.Write((byte)0);
            this.m_Stream.Write((short)item.Amount);
            this.m_Stream.Write((short)item.X);
            this.m_Stream.Write((short)item.Y);
            this.m_Stream.Write((int)m.Serial);
            this.m_Stream.Write((short)item.Hue);
        }
    }

    public sealed class SecureTradeEquip6017 : Packet
    {
        public SecureTradeEquip6017(Item item, Mobile m)
            : base(0x25, 21)
        {
            this.m_Stream.Write((int)item.Serial);
            this.m_Stream.Write((short)item.ItemID);
            this.m_Stream.Write((byte)0);
            this.m_Stream.Write((short)item.Amount);
            this.m_Stream.Write((short)item.X);
            this.m_Stream.Write((short)item.Y);
            this.m_Stream.Write((byte)0); // Grid Location?
            this.m_Stream.Write((int)m.Serial);
            this.m_Stream.Write((short)item.Hue);
        }
    }

    public sealed class MapPatches : Packet
    {
        public MapPatches()
            : base(0xBF)
        {
            this.EnsureCapacity(9 + (3 * 8));

            this.m_Stream.Write((short)0x0018);

            this.m_Stream.Write((int)4);

            this.m_Stream.Write((int)Map.Felucca.Tiles.Patch.StaticBlocks);
            this.m_Stream.Write((int)Map.Felucca.Tiles.Patch.LandBlocks);

            this.m_Stream.Write((int)Map.Trammel.Tiles.Patch.StaticBlocks);
            this.m_Stream.Write((int)Map.Trammel.Tiles.Patch.LandBlocks);

            this.m_Stream.Write((int)Map.Ilshenar.Tiles.Patch.StaticBlocks);
            this.m_Stream.Write((int)Map.Ilshenar.Tiles.Patch.LandBlocks);

            this.m_Stream.Write((int)Map.Malas.Tiles.Patch.StaticBlocks);
            this.m_Stream.Write((int)Map.Malas.Tiles.Patch.LandBlocks);
        }
    }

    public sealed class ObjectHelpResponse : Packet
    {
        public ObjectHelpResponse(IEntity e, string text)
            : base(0xB7)
        {
            this.EnsureCapacity(9 + (text.Length * 2));

            this.m_Stream.Write((int)e.Serial);
            this.m_Stream.WriteBigUniNull(text);
        }
    }

    public sealed class VendorBuyContent : Packet
    {
        public VendorBuyContent(List<BuyItemState> list)
            : base(0x3c)
        {
            this.EnsureCapacity(list.Count * 19 + 5);

            this.m_Stream.Write((short)list.Count);

            //The client sorts these by their X/Y value.
            //OSI sends these in wierd order.  X/Y highest to lowest and serial loest to highest
            //These are already sorted by serial (done by the vendor class) but we have to send them by x/y
            //(the x74 packet is sent in 'correct' order.)
            for (int i = list.Count - 1; i >= 0; --i)
            {
                BuyItemState bis = (BuyItemState)list[i];
		
                this.m_Stream.Write((int)bis.MySerial);
                this.m_Stream.Write((ushort)bis.ItemID);
                this.m_Stream.Write((byte)0);//itemid offset
                this.m_Stream.Write((ushort)bis.Amount);
                this.m_Stream.Write((short)(i + 1));//x
                this.m_Stream.Write((short)1);//y
                this.m_Stream.Write((int)bis.ContainerSerial);
                this.m_Stream.Write((ushort)bis.Hue);
            }
        }
    }

    public sealed class VendorBuyContent6017 : Packet
    {
        public VendorBuyContent6017(List<BuyItemState> list)
            : base(0x3c)
        {
            this.EnsureCapacity(list.Count * 20 + 5);

            this.m_Stream.Write((short)list.Count);

            //The client sorts these by their X/Y value.
            //OSI sends these in wierd order.  X/Y highest to lowest and serial loest to highest
            //These are already sorted by serial (done by the vendor class) but we have to send them by x/y
            //(the x74 packet is sent in 'correct' order.)
            for (int i = list.Count - 1; i >= 0; --i)
            {
                BuyItemState bis = (BuyItemState)list[i];
		
                this.m_Stream.Write((int)bis.MySerial);
                this.m_Stream.Write((ushort)bis.ItemID);
                this.m_Stream.Write((byte)0);//itemid offset
                this.m_Stream.Write((ushort)bis.Amount);
                this.m_Stream.Write((short)(i + 1));//x
                this.m_Stream.Write((short)1);//y
                this.m_Stream.Write((byte)0); // Grid Location?
                this.m_Stream.Write((int)bis.ContainerSerial);
                this.m_Stream.Write((ushort)bis.Hue);
            }
        }
    }

    public sealed class DisplayBuyList : Packet
    {
        public DisplayBuyList(Mobile vendor)
            : base(0x24, 7)
        {
            this.m_Stream.Write((int)vendor.Serial);
            this.m_Stream.Write((short)0x30); // buy window id?
        }
    }

    public sealed class DisplayBuyListHS : Packet
    {
        public DisplayBuyListHS(Mobile vendor)
            : base(0x24, 9)
        {
            this.m_Stream.Write((int)vendor.Serial);
            this.m_Stream.Write((short)0x30); // buy window id?
            this.m_Stream.Write((short)0x00);
        }
    }

    public sealed class VendorBuyList : Packet
    {
        public VendorBuyList(Mobile vendor, List<BuyItemState> list)
            : base(0x74)
        {
            this.EnsureCapacity(256);

            Container BuyPack = vendor.FindItemOnLayer(Layer.ShopBuy) as Container;
            this.m_Stream.Write((int)(BuyPack == null ? Serial.MinusOne : BuyPack.Serial));

            this.m_Stream.Write((byte)list.Count);

            for (int i = 0; i < list.Count; ++i)
            {
                BuyItemState bis = list[i];

                this.m_Stream.Write((int)bis.Price);

                string desc = bis.Description;

                if (desc == null)
                    desc = "";

                this.m_Stream.Write((byte)(desc.Length + 1));
                this.m_Stream.WriteAsciiNull(desc);
            }
        }
    }

    public sealed class VendorSellList : Packet
    {
        public VendorSellList(Mobile shopkeeper, Hashtable table)
            : base(0x9E)
        {
            this.EnsureCapacity(256);

            this.m_Stream.Write((int)shopkeeper.Serial);

            this.m_Stream.Write((ushort)table.Count);

            foreach (SellItemState state in table.Values)
            {
                this.m_Stream.Write((int)state.Item.Serial);
                this.m_Stream.Write((ushort)state.Item.ItemID);
                this.m_Stream.Write((ushort)state.Item.Hue);
                this.m_Stream.Write((ushort)state.Item.Amount);
                this.m_Stream.Write((ushort)state.Price);

                string name = state.Item.Name;

                if (name == null || (name = name.Trim()).Length <= 0)
                    name = state.Name;

                if (name == null)
                    name = "";

                this.m_Stream.Write((ushort)(name.Length));
                this.m_Stream.WriteAsciiFixed(name, (ushort)(name.Length));
            }
        }
    }

    public sealed class EndVendorSell : Packet
    {
        public EndVendorSell(Mobile Vendor)
            : base(0x3B, 8)
        {
            this.m_Stream.Write((ushort)8);//length
            this.m_Stream.Write((int)Vendor.Serial);
            this.m_Stream.Write((byte)0);
        }
    }

    public sealed class EndVendorBuy : Packet
    {
        public EndVendorBuy(Mobile Vendor)
            : base(0x3B, 8)
        {
            this.m_Stream.Write((ushort)8);//length
            this.m_Stream.Write((int)Vendor.Serial);
            this.m_Stream.Write((byte)0);
        }
    }

    public sealed class DeathAnimation : Packet
    {
        public DeathAnimation(Mobile killed, Item corpse)
            : base(0xAF, 13)
        {
            this.m_Stream.Write((int)killed.Serial);
            this.m_Stream.Write((int)(corpse == null ? Serial.Zero : corpse.Serial));
            this.m_Stream.Write((int)0) ;
        }
    }

    public sealed class StatLockInfo : Packet
    {
        public StatLockInfo(Mobile m)
            : base(0xBF)
        {
            this.EnsureCapacity(12);

            this.m_Stream.Write((short)0x19);
            this.m_Stream.Write((byte)2);
            this.m_Stream.Write((int)m.Serial);
            this.m_Stream.Write((byte)0);

            int lockBits = 0;

            lockBits |= (int)m.StrLock << 4;
            lockBits |= (int)m.DexLock << 2;
            lockBits |= (int)m.IntLock;

            this.m_Stream.Write((byte)lockBits);
        }
    }

    public class EquipInfoAttribute
    {
        private readonly int m_Number;
        private readonly int m_Charges;
        public EquipInfoAttribute(int number)
            : this(number, -1)
        {
        }

        public EquipInfoAttribute(int number, int charges)
        {
            this.m_Number = number;
            this.m_Charges = charges;
        }

        public int Number
        {
            get
            {
                return this.m_Number;
            }
        }
        public int Charges
        {
            get
            {
                return this.m_Charges;
            }
        }
    }

    public class EquipmentInfo
    {
        private readonly int m_Number;
        private readonly Mobile m_Crafter;
        private readonly bool m_Unidentified;
        private readonly EquipInfoAttribute[] m_Attributes;
        public EquipmentInfo(int number, Mobile crafter, bool unidentified, EquipInfoAttribute[] attributes)
        {
            this.m_Number = number;
            this.m_Crafter = crafter;
            this.m_Unidentified = unidentified;
            this.m_Attributes = attributes;
        }

        public int Number
        {
            get
            {
                return this.m_Number;
            }
        }
        public Mobile Crafter
        {
            get
            {
                return this.m_Crafter;
            }
        }
        public bool Unidentified
        {
            get
            {
                return this.m_Unidentified;
            }
        }
        public EquipInfoAttribute[] Attributes
        {
            get
            {
                return this.m_Attributes;
            }
        }
    }

    public sealed class DisplayEquipmentInfo : Packet
    {
        public DisplayEquipmentInfo(Item item, EquipmentInfo info)
            : base(0xBF)
        {
            EquipInfoAttribute[] attrs = info.Attributes;

            this.EnsureCapacity(17 + (info.Crafter == null ? 0 : 6 + info.Crafter.Name == null ? 0 : info.Crafter.Name.Length) + (info.Unidentified ? 4 : 0) + (attrs.Length * 6));

            this.m_Stream.Write((short)0x10);
            this.m_Stream.Write((int)item.Serial);

            this.m_Stream.Write((int)info.Number);

            if (info.Crafter != null)
            {
                string name = info.Crafter.Name;

                this.m_Stream.Write((int)-3);

                if (name == null) 
                    this.m_Stream.Write((ushort)0);
                else
                {
                    int length = name.Length;
                    this.m_Stream.Write((ushort)length);
                    this.m_Stream.WriteAsciiFixed(name, length);
                }
            }

            if (info.Unidentified)
            {
                this.m_Stream.Write((int)-4);
            }

            for (int i = 0; i < attrs.Length; ++i)
            {
                this.m_Stream.Write((int)attrs[i].Number);
                this.m_Stream.Write((short)attrs[i].Charges);
            }

            this.m_Stream.Write((int)-1);
        }
    }

    public sealed class ChangeUpdateRange : Packet
    {
        private static readonly ChangeUpdateRange[] m_Cache = new ChangeUpdateRange[0x100];
        public ChangeUpdateRange(int range)
            : base(0xC8, 2)
        {
            this.m_Stream.Write((byte)range);
        }

        public static ChangeUpdateRange Instantiate(int range)
        {
            byte idx = (byte)range;
            ChangeUpdateRange p = m_Cache[idx];

            if (p == null)
            {
                m_Cache[idx] = p = new ChangeUpdateRange(range);
                p.SetStatic();
            }

            return p;
        }
    }

    public sealed class ChangeCombatant : Packet
    {
        public ChangeCombatant(Mobile combatant)
            : base(0xAA, 5)
        {
            this.m_Stream.Write(combatant != null ? combatant.Serial : Serial.Zero);
        }
    }

    public sealed class DisplayHuePicker : Packet
    {
        public DisplayHuePicker(HuePicker huePicker)
            : base(0x95, 9)
        {
            this.m_Stream.Write((int)huePicker.Serial);
            this.m_Stream.Write((short)0);
            this.m_Stream.Write((short)huePicker.ItemID);
        }
    }

    public sealed class TripTimeResponse : Packet
    {
        public TripTimeResponse(int unk)
            : base(0xC9, 6)
        {
            this.m_Stream.Write((byte)unk);
            this.m_Stream.Write((int)Environment.TickCount);
        }
    }

    public sealed class UTripTimeResponse : Packet
    {
        public UTripTimeResponse(int unk)
            : base(0xCA, 6)
        {
            this.m_Stream.Write((byte)unk);
            this.m_Stream.Write((int)Environment.TickCount);
        }
    }

    public sealed class UnicodePrompt : Packet
    {
        public UnicodePrompt(Prompt prompt)
            : base(0xC2)
        {
            this.EnsureCapacity(21);

            this.m_Stream.Write((int)prompt.Serial);
            this.m_Stream.Write((int)prompt.Serial);
            this.m_Stream.Write((int)0);
            this.m_Stream.Write((int)0);
            this.m_Stream.Write((short)0);
        }
    }

    public sealed class ChangeCharacter : Packet
    {
        public ChangeCharacter(IAccount a)
            : base(0x81)
        {
            this.EnsureCapacity(305);

            int count = 0;

            for (int i = 0; i < a.Length; ++i)
            {
                if (a[i] != null)
                    ++count;
            }

            this.m_Stream.Write((byte)count);
            this.m_Stream.Write((byte)0);

            for (int i = 0; i < a.Length; ++i)
            {
                if (a[i] != null)
                {
                    string name = a[i].Name;

                    if (name == null)
                        name = "-null-";
                    else if ((name = name.Trim()).Length == 0)
                        name = "-empty-";

                    this.m_Stream.WriteAsciiFixed(name, 30);
                    this.m_Stream.Fill(30); // password
                }
                else
                {
                    this.m_Stream.Fill(60);
                }
            }
        }
    }

    public sealed class DeathStatus : Packet
    {
        public static readonly Packet Dead = Packet.SetStatic(new DeathStatus(true));
        public static readonly Packet Alive = Packet.SetStatic(new DeathStatus(false));
        public DeathStatus(bool dead)
            : base(0x2C, 2)
        {
            this.m_Stream.Write((byte)(dead ? 0 : 2));
        }

        public static Packet Instantiate(bool dead)
        {
            return (dead ? Dead : Alive);
        }
    }

    public sealed class SpeedControl : Packet
    {
        public static readonly Packet WalkSpeed = Packet.SetStatic(new SpeedControl(2));
        public static readonly Packet MountSpeed = Packet.SetStatic(new SpeedControl(1));
        public static readonly Packet Disable = Packet.SetStatic(new SpeedControl(0));
        public SpeedControl(int speedControl)
            : base(0xBF)
        {
            this.EnsureCapacity(3);

            this.m_Stream.Write((short)0x26);
            this.m_Stream.Write((byte)speedControl);
        }
    }

    public sealed class InvalidMapEnable : Packet
    {
        public InvalidMapEnable()
            : base(0xC6, 1)
        {
        }
    }

    public sealed class BondedStatus : Packet
    {
        public BondedStatus(int val1, Serial serial, int val2)
            : base(0xBF)
        {
            this.EnsureCapacity(11);

            this.m_Stream.Write((short)0x19);
            this.m_Stream.Write((byte)val1);
            this.m_Stream.Write((int)serial);
            this.m_Stream.Write((byte)val2);
        }
    }

    public sealed class ToggleSpecialAbility : Packet
    {
        public ToggleSpecialAbility(int abilityID, bool active)
            : base(0xBF)
        {
            this.EnsureCapacity(7);

            this.m_Stream.Write((short)0x25);

            this.m_Stream.Write((short)abilityID);
            this.m_Stream.Write((bool)active);
        }
    }

    public sealed class DisplayItemListMenu : Packet
    {
        public DisplayItemListMenu(ItemListMenu menu)
            : base(0x7C)
        {
            this.EnsureCapacity(256);

            this.m_Stream.Write((int)((IMenu)menu).Serial);
            this.m_Stream.Write((short)0);

            string question = menu.Question;

            if (question == null)
                this.m_Stream.Write((byte)0);
            else
            {
                int questionLength = question.Length;
                this.m_Stream.Write((byte)questionLength);
                this.m_Stream.WriteAsciiFixed(question, questionLength);
            }

            ItemListEntry[] entries = menu.Entries;

            int entriesLength = (byte)entries.Length;

            this.m_Stream.Write((byte)entriesLength);

            for (int i = 0; i < entriesLength; ++i)
            {
                ItemListEntry e = entries[i];

                this.m_Stream.Write((ushort)e.ItemID);
                this.m_Stream.Write((short)e.Hue);

                string name = e.Name;

                if (name == null)
                    this.m_Stream.Write((byte)0);
                else
                {
                    int nameLength = name.Length;
                    this.m_Stream.Write((byte)nameLength);
                    this.m_Stream.WriteAsciiFixed(name, nameLength);
                }
            }
        }
    }

    public sealed class DisplayQuestionMenu : Packet
    {
        public DisplayQuestionMenu(QuestionMenu menu)
            : base(0x7C)
        {
            this.EnsureCapacity(256);

            this.m_Stream.Write((int)((IMenu)menu).Serial);
            this.m_Stream.Write((short)0);

            string question = menu.Question;

            if (question == null) 
                this.m_Stream.Write((byte)0);
            else
            {
                int questionLength = question.Length;
                this.m_Stream.Write((byte)questionLength);
                this.m_Stream.WriteAsciiFixed(question, questionLength);
            }

            string[] answers = menu.Answers;

            int answersLength = (byte)answers.Length;

            this.m_Stream.Write((byte)answersLength);

            for (int i = 0; i < answersLength; ++i)
            {
                this.m_Stream.Write((int)0);

                string answer = answers[i];

                if (answer == null) 
                    this.m_Stream.Write((byte)0);
                else
                {
                    int answerLength = answer.Length;
                    this.m_Stream.Write((byte)answerLength);
                    this.m_Stream.WriteAsciiFixed(answer, answerLength);
                }
            }
        }
    }

    public sealed class GlobalLightLevel : Packet
    {
        private static readonly GlobalLightLevel[] m_Cache = new GlobalLightLevel[0x100];
        public GlobalLightLevel(int level)
            : base(0x4F, 2)
        {
            this.m_Stream.Write((sbyte)level);
        }

        public static GlobalLightLevel Instantiate(int level)
        {
            byte lvl = (byte)level;
            GlobalLightLevel p = m_Cache[lvl];

            if (p == null)
            {
                m_Cache[lvl] = p = new GlobalLightLevel(level);
                p.SetStatic();
            }

            return p;
        }
    }

    public sealed class PersonalLightLevel : Packet
    {
        public PersonalLightLevel(Mobile m)
            : this(m, m.LightLevel)
        {
        }

        public PersonalLightLevel(Mobile m, int level)
            : base(0x4E, 6)
        {
            this.m_Stream.Write((int)m.Serial);
            this.m_Stream.Write((sbyte)level);
        }
    }

    public sealed class PersonalLightLevelZero : Packet
    {
        public PersonalLightLevelZero(Mobile m)
            : base(0x4E, 6)
        {
            this.m_Stream.Write((int)m.Serial);
            this.m_Stream.Write((sbyte)0);
        }
    }

    public sealed class DisplayContextMenu : Packet
    {
        public DisplayContextMenu(ContextMenu menu)
            : base(0xBF)
        {
            ContextMenuEntry[] entries = menu.Entries;
 
            int length = (byte)entries.Length;
 
            this.EnsureCapacity(12 + (length * 8));
 
            this.m_Stream.Write((short)0x14);
            this.m_Stream.Write((short)0x02); // New layout
 
            IEntity target = menu.Target as IEntity;
 
            this.m_Stream.Write((int)(target == null ? Serial.MinusOne : target.Serial));
 
            this.m_Stream.Write((byte)length);
 
            Point3D p;
 
            if (target is Mobile)
                p = target.Location;
            else if (target is Item)
                p = ((Item)target).GetWorldLocation();
            else
                p = Point3D.Zero;
 
            for (int i = 0; i < length; ++i)
            {
                ContextMenuEntry e = entries[i];
 
                if (e.Number <= 65535)
                    this.m_Stream.Write((uint)(e.Number + 3000000));
                else
                    this.m_Stream.Write((uint)e.Number);
 
                this.m_Stream.Write((short)i);
 
                int range = e.Range;
 
                if (range == -1)
                    range = 18;
 
                CMEFlags flags = (e.Enabled && menu.From.InRange(p, range)) ? CMEFlags.None : CMEFlags.Disabled;
 
                flags |= e.Flags;
 
                this.m_Stream.Write((short)flags);
 
            }
        }
    }

    public sealed class DisplayProfile : Packet
    {
        public DisplayProfile(bool realSerial, Mobile m, string header, string body, string footer)
            : base(0xB8)
        {
            if (header == null)
                header = "";

            if (body == null)
                body = "";

            if (footer == null)
                footer = "";

            this.EnsureCapacity(12 + header.Length + (footer.Length * 2) + (body.Length * 2));

            this.m_Stream.Write((int)(realSerial ? m.Serial : Serial.Zero));
            this.m_Stream.WriteAsciiNull(header);
            this.m_Stream.WriteBigUniNull(footer);
            this.m_Stream.WriteBigUniNull(body);
        }
    }

    public sealed class CloseGump : Packet
    {
        public CloseGump(int typeID, int buttonID)
            : base(0xBF)
        {
            this.EnsureCapacity(13);

            this.m_Stream.Write((short)0x04);
            this.m_Stream.Write((int)typeID);
            this.m_Stream.Write((int)buttonID);
        }
    }

    public sealed class EquipUpdate : Packet
    {
        public EquipUpdate(Item item)
            : base(0x2E, 15)
        {
            Serial parentSerial;

            if (item.Parent is Mobile)
            {
                parentSerial = ((Mobile)item.Parent).Serial;
            }
            else
            {
                Console.WriteLine("Warning: EquipUpdate on item with !(parent is Mobile)");
                parentSerial = Serial.Zero;
            }

            int hue = item.Hue;

            if (item.Parent is Mobile)
            {
                Mobile mob = (Mobile)item.Parent;

                if (mob.SolidHueOverride >= 0)
                    hue = mob.SolidHueOverride;
            }

            this.m_Stream.Write((int)item.Serial);
            this.m_Stream.Write((short)item.ItemID);
            this.m_Stream.Write((byte)0);
            this.m_Stream.Write((byte)item.Layer);
            this.m_Stream.Write((int)parentSerial);
            this.m_Stream.Write((short)hue);
        }
    }

    public sealed class WorldItem : Packet
    {
        public WorldItem(Item item)
            : base(0x1A)
        {
            this.EnsureCapacity(20);

            // 14 base length
            // +2 - Amount
            // +2 - Hue
            // +1 - Flags

            uint serial = (uint)item.Serial.Value;
            int itemID = item.ItemID & 0x3FFF;
            int amount = item.Amount;
            Point3D loc = item.Location;
            int x = loc.m_X;
            int y = loc.m_Y;
            int hue = item.Hue;
            int flags = item.GetPacketFlags();
            int direction = (int)item.Direction;

            if (amount != 0)
            {
                serial |= 0x80000000;
            }
            else
            {
                serial &= 0x7FFFFFFF;
            }

            this.m_Stream.Write((uint)serial);

            if (item is BaseMulti)
                this.m_Stream.Write((short)(itemID | 0x4000));
            else
                this.m_Stream.Write((short)itemID);

            if (amount != 0)
            {
                this.m_Stream.Write((short)amount);
            }

            x &= 0x7FFF;

            if (direction != 0)
            {
                x |= 0x8000;
            }

            this.m_Stream.Write((short)x);

            y &= 0x3FFF;

            if (hue != 0)
            {
                y |= 0x8000;
            }

            if (flags != 0)
            {
                y |= 0x4000;
            }

            this.m_Stream.Write((short)y);

            if (direction != 0)
                this.m_Stream.Write((byte)direction);

            this.m_Stream.Write((sbyte)loc.m_Z);

            if (hue != 0)
                this.m_Stream.Write((ushort)hue);

            if (flags != 0)
                this.m_Stream.Write((byte)flags);
        }
    }

    public sealed class WorldItemSA : Packet
    {
        public WorldItemSA(Item item)
            : base(0xF3, 24)
        {
            this.m_Stream.Write((short)0x1);

            int itemID = item.ItemID;

            if (item is BaseMulti)
            {
                this.m_Stream.Write((byte)0x02);

                this.m_Stream.Write((int)item.Serial);

                itemID &= 0x3FFF;

                this.m_Stream.Write((short)itemID); 

                this.m_Stream.Write((byte)0);
                /*} else if (  ) {
                m_Stream.Write( (byte) 0x01 );
                m_Stream.Write( (int) item.Serial );
                m_Stream.Write( (short) itemID ); 
                m_Stream.Write( (byte) item.Direction );*/
            }
            else
            {
                this.m_Stream.Write((byte)0x00);

                this.m_Stream.Write((int)item.Serial);

                itemID &= 0x7FFF;

                this.m_Stream.Write((short)itemID); 

                this.m_Stream.Write((byte)0);
            }

            int amount = item.Amount;
            this.m_Stream.Write((short)amount);
            this.m_Stream.Write((short)amount);

            Point3D loc = item.Location;
            int x = loc.m_X & 0x7FFF;
            int y = loc.m_Y & 0x3FFF;
            this.m_Stream.Write((short)x);
            this.m_Stream.Write((short)y);
            this.m_Stream.Write((sbyte)loc.m_Z);

            this.m_Stream.Write((byte)item.Light);
            this.m_Stream.Write((short)item.Hue);
            this.m_Stream.Write((byte)item.GetPacketFlags());
        }
    }

    public sealed class WorldItemHS : Packet
    {
        public WorldItemHS(Item item)
            : base(0xF3, 26)
        {
            this.m_Stream.Write((short)0x1);

            int itemID = item.ItemID;

            if (item is BaseMulti)
            {
                this.m_Stream.Write((byte)0x02);

                this.m_Stream.Write((int)item.Serial);

                itemID &= 0x3FFF;

                this.m_Stream.Write((ushort)itemID); 

                this.m_Stream.Write((byte)0);
                /*} else if (  ) {
                m_Stream.Write( (byte) 0x01 );
                m_Stream.Write( (int) item.Serial );
                m_Stream.Write( (ushort) itemID ); 
                m_Stream.Write( (byte) item.Direction );*/
            }
            else
            {
                this.m_Stream.Write((byte)0x00);

                this.m_Stream.Write((int)item.Serial);

                itemID &= 0xFFFF;

                this.m_Stream.Write((ushort)itemID); 

                this.m_Stream.Write((byte)0);
            }

            int amount = item.Amount;
            this.m_Stream.Write((short)amount);
            this.m_Stream.Write((short)amount);

            Point3D loc = item.Location;
            int x = loc.m_X & 0x7FFF;
            int y = loc.m_Y & 0x3FFF;
            this.m_Stream.Write((short)x);
            this.m_Stream.Write((short)y);
            this.m_Stream.Write((sbyte)loc.m_Z);

            this.m_Stream.Write((byte)item.Light);
            this.m_Stream.Write((short)item.Hue);
            this.m_Stream.Write((byte)item.GetPacketFlags());

            this.m_Stream.Write((short)0x00); // ??
        }
    }

    public sealed class LiftRej : Packet
    {
        public LiftRej(LRReason reason)
            : base(0x27, 2)
        {
            this.m_Stream.Write((byte)reason);
        }
    }

    public sealed class LogoutAck : Packet
    {
        public LogoutAck()
            : base(0xD1, 2)
        {
            this.m_Stream.Write((byte)0x01);
        }
    }

    public sealed class Weather : Packet
    {
        public Weather(int v1, int v2, int v3)
            : base(0x65, 4)
        {
            this.m_Stream.Write((byte)v1);
            this.m_Stream.Write((byte)v2);
            this.m_Stream.Write((byte)v3);
        }
    }

    public sealed class UnkD3 : Packet
    {
        public UnkD3(Mobile beholder, Mobile beheld)
            : base(0xD3)
        {
            this.EnsureCapacity(256);

            //int
            //short
            //short
            //short
            //byte
            //byte
            //short
            //byte
            //byte
            //short
            //short
            //short
            //while ( int != 0 )
            //{
            //short
            //byte
            //short
            //}

            this.m_Stream.Write((int)beheld.Serial);
            this.m_Stream.Write((short)beheld.Body);
            this.m_Stream.Write((short)beheld.X);
            this.m_Stream.Write((short)beheld.Y);
            this.m_Stream.Write((sbyte)beheld.Z);
            this.m_Stream.Write((byte)beheld.Direction);
            this.m_Stream.Write((ushort)beheld.Hue);
            this.m_Stream.Write((byte)beheld.GetPacketFlags());
            this.m_Stream.Write((byte)Notoriety.Compute(beholder, beheld));

            this.m_Stream.Write((short)0);
            this.m_Stream.Write((short)0);
            this.m_Stream.Write((short)0);

            this.m_Stream.Write((int)0);
        }
    }

    public sealed class GQRequest : Packet
    {
        public GQRequest()
            : base(0xC3)
        {
            this.EnsureCapacity(256);

            this.m_Stream.Write((int)1);
            this.m_Stream.Write((int)2); // ID
            this.m_Stream.Write((int)3); // Customer ? (this)
            this.m_Stream.Write((int)4); // Customer this (?)
            this.m_Stream.Write((int)0);
            this.m_Stream.Write((short)0);
            this.m_Stream.Write((short)6);
            this.m_Stream.Write((byte)'r');
            this.m_Stream.Write((byte)'e');
            this.m_Stream.Write((byte)'g');
            this.m_Stream.Write((byte)'i');
            this.m_Stream.Write((byte)'o');
            this.m_Stream.Write((byte)'n');
            this.m_Stream.Write((int)7); // Call time in seconds
            this.m_Stream.Write((short)2); // Map (0=fel,1=tram,2=ilsh)
            this.m_Stream.Write((int)8); // X
            this.m_Stream.Write((int)9); // Y
            this.m_Stream.Write((int)10); // Z
            this.m_Stream.Write((int)11); // Volume
            this.m_Stream.Write((int)12); // Rank
            this.m_Stream.Write((int)-1);
            this.m_Stream.Write((int)1); // type
        }
    }

    /// <summary>
    /// Causes the client to walk in a given direction. It does not send a movement request.
    /// </summary>
    public sealed class PlayerMove : Packet
    {
        public PlayerMove(Direction d)
            : base(0x97, 2)
        {
            this.m_Stream.Write((byte)d);
            // @4C63B0
        }
    }

    /// <summary>
    /// Displays a message "There are currently [count] available calls in the global queue.".
    /// </summary>
    public sealed class GQCount : Packet
    {
        public GQCount(int unk, int count)
            : base(0xCB, 7)
        {
            this.m_Stream.Write((short)unk);
            this.m_Stream.Write((int)count);
        }
    }

    /// <summary>
    /// Asks the client for it's version
    /// </summary>
    public sealed class ClientVersionReq : Packet
    {
        public ClientVersionReq()
            : base(0xBD)
        {
            this.EnsureCapacity(3);
        }
    }

    /// <summary>
    /// Asks the client for it's "assist version". (Perhaps for UOAssist?)
    /// </summary>
    public sealed class AssistVersionReq : Packet
    {
        public AssistVersionReq(int unk)
            : base(0xBE)
        {
            this.EnsureCapacity(7);

            this.m_Stream.Write((int)unk);
        }
    }

    public class ParticleEffect : Packet
    {
        public ParticleEffect(EffectType type, Serial from, Serial to, int itemID, Point3D fromPoint, Point3D toPoint, int speed, int duration, bool fixedDirection, bool explode, int hue, int renderMode, int effect, int explodeEffect, int explodeSound, Serial serial, int layer, int unknown)
            : base(0xC7, 49)
        {
            this.m_Stream.Write((byte)type);
            this.m_Stream.Write((int)from);
            this.m_Stream.Write((int)to);
            this.m_Stream.Write((short)itemID);
            this.m_Stream.Write((short)fromPoint.m_X);
            this.m_Stream.Write((short)fromPoint.m_Y);
            this.m_Stream.Write((sbyte)fromPoint.m_Z);
            this.m_Stream.Write((short)toPoint.m_X);
            this.m_Stream.Write((short)toPoint.m_Y);
            this.m_Stream.Write((sbyte)toPoint.m_Z);
            this.m_Stream.Write((byte)speed);
            this.m_Stream.Write((byte)duration);
            this.m_Stream.Write((byte)0);
            this.m_Stream.Write((byte)0);
            this.m_Stream.Write((bool)fixedDirection);
            this.m_Stream.Write((bool)explode);
            this.m_Stream.Write((int)hue);
            this.m_Stream.Write((int)renderMode);
            this.m_Stream.Write((short)effect);
            this.m_Stream.Write((short)explodeEffect);
            this.m_Stream.Write((short)explodeSound);
            this.m_Stream.Write((int)serial);
            this.m_Stream.Write((byte)layer);
            this.m_Stream.Write((short)unknown);
        }

        public ParticleEffect(EffectType type, Serial from, Serial to, int itemID, IPoint3D fromPoint, IPoint3D toPoint, int speed, int duration, bool fixedDirection, bool explode, int hue, int renderMode, int effect, int explodeEffect, int explodeSound, Serial serial, int layer, int unknown)
            : base(0xC7, 49)
        {
            this.m_Stream.Write((byte)type);
            this.m_Stream.Write((int)from);
            this.m_Stream.Write((int)to);
            this.m_Stream.Write((short)itemID);
            this.m_Stream.Write((short)fromPoint.X);
            this.m_Stream.Write((short)fromPoint.Y);
            this.m_Stream.Write((sbyte)fromPoint.Z);
            this.m_Stream.Write((short)toPoint.X);
            this.m_Stream.Write((short)toPoint.Y);
            this.m_Stream.Write((sbyte)toPoint.Z);
            this.m_Stream.Write((byte)speed);
            this.m_Stream.Write((byte)duration);
            this.m_Stream.Write((byte)0);
            this.m_Stream.Write((byte)0);
            this.m_Stream.Write((bool)fixedDirection);
            this.m_Stream.Write((bool)explode);
            this.m_Stream.Write((int)hue);
            this.m_Stream.Write((int)renderMode);
            this.m_Stream.Write((short)effect);
            this.m_Stream.Write((short)explodeEffect);
            this.m_Stream.Write((short)explodeSound);
            this.m_Stream.Write((int)serial);
            this.m_Stream.Write((byte)layer);
            this.m_Stream.Write((short)unknown);
        }
    }

    public class HuedEffect : Packet
    {
        public HuedEffect(EffectType type, Serial from, Serial to, int itemID, Point3D fromPoint, Point3D toPoint, int speed, int duration, bool fixedDirection, bool explode, int hue, int renderMode)
            : base(0xC0, 36)
        {
            this.m_Stream.Write((byte)type);
            this.m_Stream.Write((int)from);
            this.m_Stream.Write((int)to);
            this.m_Stream.Write((short)itemID);
            this.m_Stream.Write((short)fromPoint.m_X);
            this.m_Stream.Write((short)fromPoint.m_Y);
            this.m_Stream.Write((sbyte)fromPoint.m_Z);
            this.m_Stream.Write((short)toPoint.m_X);
            this.m_Stream.Write((short)toPoint.m_Y);
            this.m_Stream.Write((sbyte)toPoint.m_Z);
            this.m_Stream.Write((byte)speed);
            this.m_Stream.Write((byte)duration);
            this.m_Stream.Write((byte)0);
            this.m_Stream.Write((byte)0);
            this.m_Stream.Write((bool)fixedDirection);
            this.m_Stream.Write((bool)explode);
            this.m_Stream.Write((int)hue);
            this.m_Stream.Write((int)renderMode);
        }

        public HuedEffect(EffectType type, Serial from, Serial to, int itemID, IPoint3D fromPoint, IPoint3D toPoint, int speed, int duration, bool fixedDirection, bool explode, int hue, int renderMode)
            : base(0xC0, 36)
        {
            this.m_Stream.Write((byte)type);
            this.m_Stream.Write((int)from);
            this.m_Stream.Write((int)to);
            this.m_Stream.Write((short)itemID);
            this.m_Stream.Write((short)fromPoint.X);
            this.m_Stream.Write((short)fromPoint.Y);
            this.m_Stream.Write((sbyte)fromPoint.Z);
            this.m_Stream.Write((short)toPoint.X);
            this.m_Stream.Write((short)toPoint.Y);
            this.m_Stream.Write((sbyte)toPoint.Z);
            this.m_Stream.Write((byte)speed);
            this.m_Stream.Write((byte)duration);
            this.m_Stream.Write((byte)0);
            this.m_Stream.Write((byte)0);
            this.m_Stream.Write((bool)fixedDirection);
            this.m_Stream.Write((bool)explode);
            this.m_Stream.Write((int)hue);
            this.m_Stream.Write((int)renderMode);
        }
    }

    public sealed class TargetParticleEffect : ParticleEffect
    {
        public TargetParticleEffect(IEntity e, int itemID, int speed, int duration, int hue, int renderMode, int effect, int layer, int unknown)
            : base(EffectType.FixedFrom, e.Serial, Serial.Zero, itemID, e.Location, e.Location, speed, duration, true, false, hue, renderMode, effect, 1, 0, e.Serial, layer, unknown)
        {
        }
    }

    public sealed class TargetEffect : HuedEffect
    {
        public TargetEffect(IEntity e, int itemID, int speed, int duration, int hue, int renderMode)
            : base(EffectType.FixedFrom, e.Serial, Serial.Zero, itemID, e.Location, e.Location, speed, duration, true, false, hue, renderMode)
        {
        }
    }

    public sealed class LocationParticleEffect : ParticleEffect
    {
        public LocationParticleEffect(IEntity e, int itemID, int speed, int duration, int hue, int renderMode, int effect, int unknown)
            : base(EffectType.FixedXYZ, e.Serial, Serial.Zero, itemID, e.Location, e.Location, speed, duration, true, false, hue, renderMode, effect, 1, 0, e.Serial, 255, unknown)
        {
        }
    }

    public sealed class LocationEffect : HuedEffect
    {
        public LocationEffect(IPoint3D p, int itemID, int speed, int duration, int hue, int renderMode)
            : base(EffectType.FixedXYZ, Serial.Zero, Serial.Zero, itemID, p, p, speed, duration, true, false, hue, renderMode)
        {
        }
    }

    public sealed class MovingParticleEffect : ParticleEffect
    {
        public MovingParticleEffect(IEntity from, IEntity to, int itemID, int speed, int duration, bool fixedDirection, bool explodes, int hue, int renderMode, int effect, int explodeEffect, int explodeSound, EffectLayer layer, int unknown)
            : base(EffectType.Moving, from.Serial, to.Serial, itemID, from.Location, to.Location, speed, duration, fixedDirection, explodes, hue, renderMode, effect, explodeEffect, explodeSound, Serial.Zero, (int)layer, unknown)
        {
        }
    }

    public sealed class MovingEffect : HuedEffect
    {
        public MovingEffect(IEntity from, IEntity to, int itemID, int speed, int duration, bool fixedDirection, bool explodes, int hue, int renderMode)
            : base(EffectType.Moving, from.Serial, to.Serial, itemID, from.Location, to.Location, speed, duration, fixedDirection, explodes, hue, renderMode)
        {
        }
    }

    public class ScreenEffect : Packet
    {
        public ScreenEffect(ScreenEffectType type)
            : base(0x70, 28)
        {
            this.m_Stream.Write((byte)0x04);
            this.m_Stream.Fill(8);
            this.m_Stream.Write((short)type);
            this.m_Stream.Fill(16);
        }
    }

    public sealed class ScreenFadeOut : ScreenEffect
    {
        public static readonly Packet Instance = Packet.SetStatic(new ScreenFadeOut());
        public ScreenFadeOut()
            : base(ScreenEffectType.FadeOut)
        { 
        }
    }

    public sealed class ScreenFadeIn : ScreenEffect
    {
        public static readonly Packet Instance = Packet.SetStatic(new ScreenFadeIn());
        public ScreenFadeIn()
            : base(ScreenEffectType.FadeIn)
        {
        }
    }

    public sealed class ScreenFadeInOut : ScreenEffect
    {
        public static readonly Packet Instance = Packet.SetStatic(new ScreenFadeInOut());
        public ScreenFadeInOut()
            : base(ScreenEffectType.FadeInOut)
        { 
        }
    }

    public sealed class ScreenLightFlash : ScreenEffect
    {
        public static readonly Packet Instance = Packet.SetStatic(new ScreenLightFlash());
        public ScreenLightFlash()
            : base(ScreenEffectType.LightFlash)
        {
        }
    }

    public sealed class ScreenDarkFlash : ScreenEffect
    {
        public static readonly Packet Instance = Packet.SetStatic(new ScreenDarkFlash());
        public ScreenDarkFlash()
            : base(ScreenEffectType.DarkFlash)
        { 
        }
    }

    public sealed class DeleteResult : Packet
    {
        public DeleteResult(DeleteResultType res)
            : base(0x85, 2)
        {
            this.m_Stream.Write((byte)res);
        }
    }
    /*public sealed class MovingEffect : Packet
    {
    public MovingEffect( IEntity from, IEntity to, int itemID, int speed, int duration, bool fixedDirection, bool turn, int hue, int renderMode ) : base( 0xC0, 36 )
    {
    m_Stream.Write( (byte) 0x00 );
    m_Stream.Write( (int) from.Serial );
    m_Stream.Write( (int) to.Serial );
    m_Stream.Write( (short) itemID );
    m_Stream.Write( (short) from.Location.m_X );
    m_Stream.Write( (short) from.Location.m_Y );
    m_Stream.Write( (sbyte) from.Location.m_Z );
    m_Stream.Write( (short) to.Location.m_X );
    m_Stream.Write( (short) to.Location.m_Y );
    m_Stream.Write( (sbyte) to.Location.m_Z );
    m_Stream.Write( (byte) speed );
    m_Stream.Write( (byte) duration );
    m_Stream.Write( (byte) 0 );
    m_Stream.Write( (byte) 0 );
    m_Stream.Write( (bool) fixedDirection );
    m_Stream.Write( (bool) turn );
    m_Stream.Write( (int) hue );
    m_Stream.Write( (int) renderMode );
    }
    }*/

    /*public sealed class LocationEffect : Packet
    {
    public LocationEffect( IPoint3D p, int itemID, int duration, int hue, int renderMode ) : base( 0xC0, 36 )
    {
    m_Stream.Write( (byte) 0x02 );
    m_Stream.Write( (int) Serial.Zero );
    m_Stream.Write( (int) Serial.Zero );
    m_Stream.Write( (short) itemID );
    m_Stream.Write( (short) p.X );
    m_Stream.Write( (short) p.Y );
    m_Stream.Write( (sbyte) p.Z );
    m_Stream.Write( (short) p.X );
    m_Stream.Write( (short) p.Y );
    m_Stream.Write( (sbyte) p.Z );
    m_Stream.Write( (byte) 10 );
    m_Stream.Write( (byte) duration );
    m_Stream.Write( (byte) 0 );
    m_Stream.Write( (byte) 0 );
    m_Stream.Write( (byte) 1 );
    m_Stream.Write( (byte) 0 );
    m_Stream.Write( (int) hue );
    m_Stream.Write( (int) renderMode );
    }
    }*/
    public sealed class BoltEffect : Packet
    {
        public BoltEffect(IEntity target, int hue)
            : base(0xC0, 36)
        {
            this.m_Stream.Write((byte)0x01); // type
            this.m_Stream.Write((int)target.Serial);
            this.m_Stream.Write((int)Serial.Zero);
            this.m_Stream.Write((short)0); // itemID
            this.m_Stream.Write((short)target.X);
            this.m_Stream.Write((short)target.Y);
            this.m_Stream.Write((sbyte)target.Z);
            this.m_Stream.Write((short)target.X);
            this.m_Stream.Write((short)target.Y);
            this.m_Stream.Write((sbyte)target.Z);
            this.m_Stream.Write((byte)0); // speed
            this.m_Stream.Write((byte)0); // duration
            this.m_Stream.Write((short)0); // unk
            this.m_Stream.Write(false); // fixed direction
            this.m_Stream.Write(false); // explode
            this.m_Stream.Write((int)hue);
            this.m_Stream.Write((int)0); // render mode
        }
    }

    public sealed class DisplaySpellbook : Packet
    {
        public DisplaySpellbook(Item book)
            : base(0x24, 7)
        {
            this.m_Stream.Write((int)book.Serial);
            this.m_Stream.Write((short)-1);
        }
    }

    public sealed class DisplaySpellbookHS : Packet
    {
        public DisplaySpellbookHS(Item book)
            : base(0x24, 9)
        {
            this.m_Stream.Write((int)book.Serial);
            this.m_Stream.Write((short)-1);
            this.m_Stream.Write((short)0x7D);
        }
    }

    public sealed class NewSpellbookContent : Packet
    {
        public NewSpellbookContent(Item item, int graphic, int offset, ulong content)
            : base(0xBF)
        {
            this.EnsureCapacity(23);

            this.m_Stream.Write((short)0x1B);
            this.m_Stream.Write((short)0x01);

            this.m_Stream.Write((int)item.Serial);
            this.m_Stream.Write((short)graphic);
            this.m_Stream.Write((short)offset);

            for (int i = 0; i < 8; ++i)
                this.m_Stream.Write((byte)(content >> (i * 8)));
        }
    }

    public sealed class SpellbookContent : Packet
    {
        public SpellbookContent(int count, int offset, ulong content, Item item)
            : base(0x3C)
        {
            this.EnsureCapacity(5 + (count * 19));

            int written = 0;

            this.m_Stream.Write((ushort)0);

            ulong mask = 1;

            for (int i = 0; i < 64; ++i, mask <<= 1)
            {
                if ((content & mask) != 0)
                {
                    this.m_Stream.Write((int)(0x7FFFFFFF - i));
                    this.m_Stream.Write((ushort)0);
                    this.m_Stream.Write((byte)0);
                    this.m_Stream.Write((ushort)(i + offset));
                    this.m_Stream.Write((short)0);
                    this.m_Stream.Write((short)0);
                    this.m_Stream.Write((int)item.Serial);
                    this.m_Stream.Write((short)0);

                    ++written;
                }
            }

            this.m_Stream.Seek(3, SeekOrigin.Begin);
            this.m_Stream.Write((ushort)written);
        }
    }

    public sealed class SpellbookContent6017 : Packet
    {
        public SpellbookContent6017(int count, int offset, ulong content, Item item)
            : base(0x3C)
        {
            this.EnsureCapacity(5 + (count * 20));

            int written = 0;

            this.m_Stream.Write((ushort)0);

            ulong mask = 1;

            for (int i = 0; i < 64; ++i, mask <<= 1)
            {
                if ((content & mask) != 0)
                {
                    this.m_Stream.Write((int)(0x7FFFFFFF - i));
                    this.m_Stream.Write((ushort)0);
                    this.m_Stream.Write((byte)0);
                    this.m_Stream.Write((ushort)(i + offset));
                    this.m_Stream.Write((short)0);
                    this.m_Stream.Write((short)0);
                    this.m_Stream.Write((byte)0); // Grid Location?
                    this.m_Stream.Write((int)item.Serial);
                    this.m_Stream.Write((short)0);

                    ++written;
                }
            }

            this.m_Stream.Seek(3, SeekOrigin.Begin);
            this.m_Stream.Write((ushort)written);
        }
    }

    public sealed class ContainerDisplay : Packet
    {
        public ContainerDisplay(Container c)
            : base(0x24, 7)
        {
            this.m_Stream.Write((int)c.Serial);
            this.m_Stream.Write((short)c.GumpID);
        }
    }

    public sealed class ContainerDisplayHS : Packet
    {
        public ContainerDisplayHS(Container c)
            : base(0x24, 9)
        {
            this.m_Stream.Write((int)c.Serial);
            this.m_Stream.Write((short)c.GumpID);
            this.m_Stream.Write((short)0x7D);
        }
    }

    public sealed class ContainerContentUpdate : Packet
    {
        public ContainerContentUpdate(Item item)
            : base(0x25, 20)
        {
            Serial parentSerial;

            if (item.Parent is Item)
            {
                parentSerial = ((Item)item.Parent).Serial;
            }
            else
            {
                Console.WriteLine("Warning: ContainerContentUpdate on item with !(parent is Item)");
                parentSerial = Serial.Zero;
            }

            this.m_Stream.Write((int)item.Serial);
            this.m_Stream.Write((ushort)item.ItemID);
            this.m_Stream.Write((byte)0); // signed, itemID offset
            this.m_Stream.Write((ushort)item.Amount);
            this.m_Stream.Write((short)item.X);
            this.m_Stream.Write((short)item.Y);
            this.m_Stream.Write((int)parentSerial);
            this.m_Stream.Write((ushort)item.Hue);
        }
    }

    public sealed class ContainerContentUpdate6017 : Packet
    {
        public ContainerContentUpdate6017(Item item)
            : base(0x25, 21)
        {
            Serial parentSerial;

            if (item.Parent is Item)
            {
                parentSerial = ((Item)item.Parent).Serial;
            }
            else
            {
                Console.WriteLine("Warning: ContainerContentUpdate on item with !(parent is Item)");
                parentSerial = Serial.Zero;
            }

            this.m_Stream.Write((int)item.Serial);
            this.m_Stream.Write((ushort)item.ItemID);
            this.m_Stream.Write((byte)0); // signed, itemID offset
            this.m_Stream.Write((ushort)item.Amount);
            this.m_Stream.Write((short)item.X);
            this.m_Stream.Write((short)item.Y);
            this.m_Stream.Write((byte)0); // Grid Location?
            this.m_Stream.Write((int)parentSerial);
            this.m_Stream.Write((ushort)item.Hue);
        }
    }

    public sealed class ContainerContent : Packet
    {
        public ContainerContent(Mobile beholder, Item beheld)
            : base(0x3C)
        {
            List<Item> items = beheld.Items;
            int count = items.Count;

            this.EnsureCapacity(5 + (count * 19));

            long pos = this.m_Stream.Position;

            int written = 0;

            this.m_Stream.Write((ushort)0);

            for (int i = 0; i < count; ++i)
            {
                Item child = items[i];

                if (!child.Deleted && beholder.CanSee(child))
                {
                    Point3D loc = child.Location;

                    this.m_Stream.Write((int)child.Serial);
                    this.m_Stream.Write((ushort)child.ItemID);
                    this.m_Stream.Write((byte)0); // signed, itemID offset
                    this.m_Stream.Write((ushort)child.Amount);
                    this.m_Stream.Write((short)loc.m_X);
                    this.m_Stream.Write((short)loc.m_Y);
                    this.m_Stream.Write((int)beheld.Serial);
                    this.m_Stream.Write((ushort)child.Hue);

                    ++written;
                }
            }

            this.m_Stream.Seek(pos, SeekOrigin.Begin);
            this.m_Stream.Write((ushort)written);
        }
    }

    public sealed class ContainerContent6017 : Packet
    {
        public ContainerContent6017(Mobile beholder, Item beheld)
            : base(0x3C)
        {
            List<Item> items = beheld.Items;
            int count = items.Count;

            this.EnsureCapacity(5 + (count * 20));

            long pos = this.m_Stream.Position;

            int written = 0;

            this.m_Stream.Write((ushort)0);

            for (int i = 0; i < count; ++i)
            {
                Item child = items[i];

                if (!child.Deleted && beholder.CanSee(child))
                {
                    Point3D loc = child.Location;

                    this.m_Stream.Write((int)child.Serial);
                    this.m_Stream.Write((ushort)child.ItemID);
                    this.m_Stream.Write((byte)0); // signed, itemID offset
                    this.m_Stream.Write((ushort)child.Amount);
                    this.m_Stream.Write((short)loc.m_X);
                    this.m_Stream.Write((short)loc.m_Y);
                    this.m_Stream.Write((byte)0); // Grid Location?
                    this.m_Stream.Write((int)beheld.Serial);
                    this.m_Stream.Write((ushort)child.Hue);

                    ++written;
                }
            }

            this.m_Stream.Seek(pos, SeekOrigin.Begin);
            this.m_Stream.Write((ushort)written);
        }
    }

    public sealed class SetWarMode : Packet
    {
        public static readonly Packet InWarMode = Packet.SetStatic(new SetWarMode(true));
        public static readonly Packet InPeaceMode = Packet.SetStatic(new SetWarMode(false));
        public SetWarMode(bool mode)
            : base(0x72, 5)
        {
            this.m_Stream.Write(mode);
            this.m_Stream.Write((byte)0x00);
            this.m_Stream.Write((byte)0x32);
            this.m_Stream.Write((byte)0x00);
            //m_Stream.Fill();
        }

        public static Packet Instantiate(bool mode)
        {
            return (mode ? InWarMode : InPeaceMode);
        }
    }

    public sealed class Swing : Packet
    {
        public Swing(int flag, Mobile attacker, Mobile defender)
            : base(0x2F, 10)
        {
            this.m_Stream.Write((byte)flag);
            this.m_Stream.Write((int)attacker.Serial);
            this.m_Stream.Write((int)defender.Serial);
        }
    }

    public sealed class NullFastwalkStack : Packet
    {
        public NullFastwalkStack()
            : base(0xBF)
        {
            this.EnsureCapacity(256);
            this.m_Stream.Write((short)0x1);
            this.m_Stream.Write((int)0x0);
            this.m_Stream.Write((int)0x0);
            this.m_Stream.Write((int)0x0);
            this.m_Stream.Write((int)0x0);
            this.m_Stream.Write((int)0x0);
            this.m_Stream.Write((int)0x0);
        }
    }

    public sealed class RemoveItem : Packet
    {
        public RemoveItem(Item item)
            : base(0x1D, 5)
        {
            this.m_Stream.Write((int)item.Serial);
        }
    }

    public sealed class RemoveMobile : Packet
    {
        public RemoveMobile(Mobile m)
            : base(0x1D, 5)
        {
            this.m_Stream.Write((int)m.Serial);
        }
    }

    public sealed class ServerChange : Packet
    {
        public ServerChange(Mobile m, Map map)
            : base(0x76, 16)
        {
            this.m_Stream.Write((short)m.X);
            this.m_Stream.Write((short)m.Y);
            this.m_Stream.Write((short)m.Z);
            this.m_Stream.Write((byte)0);
            this.m_Stream.Write((short)0);
            this.m_Stream.Write((short)0);
            this.m_Stream.Write((short)map.Width);
            this.m_Stream.Write((short)map.Height);
        }
    }

    public sealed class SkillUpdate : Packet
    {
        public SkillUpdate(Skills skills)
            : base(0x3A)
        {
            this.EnsureCapacity(6 + (skills.Length * 9));

            this.m_Stream.Write((byte)0x02); // type: absolute, capped

            for (int i = 0; i < skills.Length; ++i)
            {
                Skill s = skills[i];

                double v = s.NonRacialValue;
                int uv = (int)(v * 10);

                if (uv < 0)
                    uv = 0;
                else if (uv >= 0x10000)
                    uv = 0xFFFF;

                this.m_Stream.Write((ushort)(s.Info.SkillID + 1));
                this.m_Stream.Write((ushort)uv);
                this.m_Stream.Write((ushort)s.BaseFixedPoint);
                this.m_Stream.Write((byte)s.Lock);
                this.m_Stream.Write((ushort)s.CapFixedPoint);
            }

            this.m_Stream.Write((short)0); // terminate
        }
    }

    public sealed class Sequence : Packet
    {
        public Sequence(int num)
            : base(0x7B, 2)
        {
            this.m_Stream.Write((byte)num);
        }
    }

    public sealed class SkillChange : Packet
    {
        public SkillChange(Skill skill)
            : base(0x3A)
        {
            this.EnsureCapacity(13);

            double v = skill.NonRacialValue;
            int uv = (int)(v * 10);

            if (uv < 0)
                uv = 0;
            else if (uv >= 0x10000)
                uv = 0xFFFF;

            this.m_Stream.Write((byte)0xDF); // type: delta, capped
            this.m_Stream.Write((ushort)skill.Info.SkillID);
            this.m_Stream.Write((ushort)uv);
            this.m_Stream.Write((ushort)skill.BaseFixedPoint);
            this.m_Stream.Write((byte)skill.Lock);
            this.m_Stream.Write((ushort)skill.CapFixedPoint);
            /*m_Stream.Write( (short) skill.Info.SkillID );
            m_Stream.Write( (short) (skill.Value * 10.0) );
            m_Stream.Write( (short) (skill.Base * 10.0) );
            m_Stream.Write( (byte) skill.Lock );
            m_Stream.Write( (short) skill.CapFixedPoint );*/
        }
    }

    public sealed class LaunchBrowser : Packet
    {
        public LaunchBrowser(string url)
            : base(0xA5)
        {
            if (url == null)
                url = "";

            this.EnsureCapacity(4 + url.Length);

            this.m_Stream.WriteAsciiNull(url);
        }
    }

    public sealed class MessageLocalized : Packet
    {
        private static readonly MessageLocalized[] m_Cache_IntLoc = new MessageLocalized[15000];
        private static readonly MessageLocalized[] m_Cache_CliLoc = new MessageLocalized[100000];
        private static readonly MessageLocalized[] m_Cache_CliLocCmp = new MessageLocalized[5000];
        public MessageLocalized(Serial serial, int graphic, MessageType type, int hue, int font, int number, string name, string args)
            : base(0xC1)
        {
            if (name == null)
                name = "";
            if (args == null)
                args = "";

            if (hue == 0)
                hue = 0x3B2;

            this.EnsureCapacity(50 + (args.Length * 2));

            this.m_Stream.Write((int)serial);
            this.m_Stream.Write((short)graphic);
            this.m_Stream.Write((byte)type);
            this.m_Stream.Write((short)hue);
            this.m_Stream.Write((short)font);
            this.m_Stream.Write((int)number);
            this.m_Stream.WriteAsciiFixed(name, 30);
            this.m_Stream.WriteLittleUniNull(args);
        }

        public static MessageLocalized InstantiateGeneric(int number)
        {
            MessageLocalized[] cache = null;
            int index = 0;

            if (number >= 3000000)
            {
                cache = m_Cache_IntLoc;
                index = number - 3000000;
            }
            else if (number >= 1000000)
            {
                cache = m_Cache_CliLoc;
                index = number - 1000000;
            }
            else if (number >= 500000)
            {
                cache = m_Cache_CliLocCmp;
                index = number - 500000;
            }

            MessageLocalized p;

            if (cache != null && index >= 0 && index < cache.Length)
            {
                p = cache[index];

                if (p == null)
                {
                    cache[index] = p = new MessageLocalized(Serial.MinusOne, -1, MessageType.Regular, 0x3B2, 3, number, "System", "");
                    p.SetStatic();
                }
            }
            else
            {
                p = new MessageLocalized(Serial.MinusOne, -1, MessageType.Regular, 0x3B2, 3, number, "System", "");
            }

            return p;
        }
    }

    public sealed class MobileMoving : Packet
    {
        public MobileMoving(Mobile m, int noto)
            : base(0x77, 17)
        {
            Point3D loc = m.Location;

            int hue = m.Hue;

            if (m.SolidHueOverride >= 0)
                hue = m.SolidHueOverride;

            this.m_Stream.Write((int)m.Serial);
            this.m_Stream.Write((short)m.Body);
            this.m_Stream.Write((short)loc.m_X);
            this.m_Stream.Write((short)loc.m_Y);
            this.m_Stream.Write((sbyte)loc.m_Z);
            this.m_Stream.Write((byte)m.Direction);
            this.m_Stream.Write((short)hue);
            this.m_Stream.Write((byte)m.GetPacketFlags());
            this.m_Stream.Write((byte)noto);
        }
    }

    // Pre-7.0.0.0 Mobile Moving
    public sealed class MobileMovingOld : Packet
    {
        public MobileMovingOld(Mobile m, int noto)
            : base(0x77, 17)
        {
            Point3D loc = m.Location;

            int hue = m.Hue;

            if (m.SolidHueOverride >= 0)
                hue = m.SolidHueOverride;

            this.m_Stream.Write((int)m.Serial);
            this.m_Stream.Write((short)m.Body);
            this.m_Stream.Write((short)loc.m_X);
            this.m_Stream.Write((short)loc.m_Y);
            this.m_Stream.Write((sbyte)loc.m_Z);
            this.m_Stream.Write((byte)m.Direction);
            this.m_Stream.Write((short)hue);
            this.m_Stream.Write((byte)m.GetOldPacketFlags());
            this.m_Stream.Write((byte)noto);
        }
    }

    public sealed class MultiTargetReqHS : Packet
    {
        public MultiTargetReqHS(MultiTarget t)
            : base(0x99, 30)
        {
            this.m_Stream.Write((bool)t.AllowGround);
            this.m_Stream.Write((int)t.TargetID);
            this.m_Stream.Write((byte)t.Flags);

            this.m_Stream.Fill();

            this.m_Stream.Seek(18, SeekOrigin.Begin);
            this.m_Stream.Write((short)t.MultiID);
            this.m_Stream.Write((short)t.Offset.X);
            this.m_Stream.Write((short)t.Offset.Y);
            this.m_Stream.Write((short)t.Offset.Z);
        }
    }

    public sealed class MultiTargetReq : Packet
    {
        public MultiTargetReq(MultiTarget t)
            : base(0x99, 26)
        {
            this.m_Stream.Write((bool)t.AllowGround);
            this.m_Stream.Write((int)t.TargetID);
            this.m_Stream.Write((byte)t.Flags);

            this.m_Stream.Fill();

            this.m_Stream.Seek(18, SeekOrigin.Begin);
            this.m_Stream.Write((short)t.MultiID);
            this.m_Stream.Write((short)t.Offset.X);
            this.m_Stream.Write((short)t.Offset.Y);
            this.m_Stream.Write((short)t.Offset.Z);
        }
    }

    public sealed class CancelTarget : Packet
    {
        public static readonly Packet Instance = Packet.SetStatic(new CancelTarget());
        public CancelTarget()
            : base(0x6C, 19)
        {
            this.m_Stream.Write((byte)0);
            this.m_Stream.Write((int)0);
            this.m_Stream.Write((byte)3);
            this.m_Stream.Fill();
        }
    }

    public sealed class TargetReq : Packet
    {
        public TargetReq(Target t)
            : base(0x6C, 19)
        {
            this.m_Stream.Write((bool)t.AllowGround);
            this.m_Stream.Write((int)t.TargetID);
            this.m_Stream.Write((byte)t.Flags);
            this.m_Stream.Fill();
        }
    }

    public sealed class DragEffect : Packet
    {
        public DragEffect(IEntity src, IEntity trg, int itemID, int hue, int amount)
            : base(0x23, 26)
        {
            this.m_Stream.Write((short)itemID);
            this.m_Stream.Write((byte)0);
            this.m_Stream.Write((short)hue);
            this.m_Stream.Write((short)amount);
            this.m_Stream.Write((int)src.Serial);
            this.m_Stream.Write((short)src.X);
            this.m_Stream.Write((short)src.Y);
            this.m_Stream.Write((sbyte)src.Z);
            this.m_Stream.Write((int)trg.Serial);
            this.m_Stream.Write((short)trg.X);
            this.m_Stream.Write((short)trg.Y);
            this.m_Stream.Write((sbyte)trg.Z);
        }
    }

    public sealed class DisplayGumpPacked : Packet, IGumpWriter
    {
        private static readonly byte[] m_True = Gump.StringToBuffer(" 1");
        private static readonly byte[] m_False = Gump.StringToBuffer(" 0");
        private static readonly byte[] m_BeginTextSeparator = Gump.StringToBuffer(" @");
        private static readonly byte[] m_EndTextSeparator = Gump.StringToBuffer("@");
        private static readonly byte[] m_Buffer = new byte[48];
        private static byte[] m_PackBuffer;
        private readonly Gump m_Gump;
        private readonly PacketWriter m_Layout;
        private readonly PacketWriter m_Strings;
        private int m_TextEntries, m_Switches;
        private int m_StringCount;
        public DisplayGumpPacked(Gump gump)
            : base(0xDD)
        {
            this.m_Gump = gump;

            this.m_Layout = PacketWriter.CreateInstance(8192);
            this.m_Strings = PacketWriter.CreateInstance(8192);
        }

        static DisplayGumpPacked()
        {
            m_Buffer[0] = (byte)' ';
        }

        public int TextEntries
        {
            get
            {
                return this.m_TextEntries;
            }
            set
            {
                this.m_TextEntries = value;
            }
        }
        public int Switches
        {
            get
            {
                return this.m_Switches;
            }
            set
            {
                this.m_Switches = value;
            }
        }
        public void AppendLayout(bool val)
        {
            this.AppendLayout(val ? m_True : m_False);
        }

        public void AppendLayout(int val)
        {
            string toString = val.ToString();
            int bytes = System.Text.Encoding.ASCII.GetBytes(toString, 0, toString.Length, m_Buffer, 1) + 1;

            this.m_Layout.Write(m_Buffer, 0, bytes);
        }

        public void AppendLayoutNS(int val)
        {
            string toString = val.ToString();
            int bytes = System.Text.Encoding.ASCII.GetBytes(toString, 0, toString.Length, m_Buffer, 1);

            this.m_Layout.Write(m_Buffer, 1, bytes);
        }

        public void AppendLayout(string text)
        {
            this.AppendLayout(m_BeginTextSeparator);

            this.m_Layout.WriteAsciiFixed(text, text.Length);

            this.AppendLayout(m_EndTextSeparator);
        }

        public void AppendLayout(byte[] buffer)
        {
            this.m_Layout.Write(buffer, 0, buffer.Length);
        }

        public void WriteStrings(List<string> strings)
        {
            this.m_StringCount = strings.Count;

            for (int i = 0; i < strings.Count; ++i)
            {
                string v = strings[i];

                if (v == null)
                    v = String.Empty;

                this.m_Strings.Write((ushort)v.Length);
                this.m_Strings.WriteBigUniFixed(v, v.Length);
            }
        }

        public void Flush()
        {
            this.EnsureCapacity(28 + (int)this.m_Layout.Length + (int)this.m_Strings.Length);

            this.m_Stream.Write((int)this.m_Gump.Serial);
            this.m_Stream.Write((int)this.m_Gump.TypeID);
            this.m_Stream.Write((int)this.m_Gump.X);
            this.m_Stream.Write((int)this.m_Gump.Y);

            // Note: layout MUST be null terminated (don't listen to krrios)
            this.m_Layout.Write((byte)0);
            this.WritePacked(this.m_Layout);

            this.m_Stream.Write((int)this.m_StringCount);

            this.WritePacked(this.m_Strings);

            PacketWriter.ReleaseInstance(this.m_Layout);
            PacketWriter.ReleaseInstance(this.m_Strings);
        }

        private void WritePacked(PacketWriter src)
        {
            byte[] buffer = src.UnderlyingStream.GetBuffer();
            int length = (int)src.Length;

            if (length == 0)
            {
                this.m_Stream.Write((int)0);
                return;
            }

            int wantLength = 1 + ((buffer.Length * 1024) / 1000);

            wantLength += 4095;
            wantLength &= ~4095;

            if (m_PackBuffer == null || m_PackBuffer.Length < wantLength)
                m_PackBuffer = new byte[wantLength];

            int packLength = m_PackBuffer.Length;

            Compression.Pack(m_PackBuffer, ref packLength, buffer, length, ZLibQuality.Default);

            this.m_Stream.Write((int)(4 + packLength));
            this.m_Stream.Write((int)length);
            this.m_Stream.Write(m_PackBuffer, 0, packLength);
        }
    }

    public sealed class DisplayGumpFast : Packet, IGumpWriter
    {
        private static readonly byte[] m_True = Gump.StringToBuffer(" 1");
        private static readonly byte[] m_False = Gump.StringToBuffer(" 0");
        private static readonly byte[] m_BeginTextSeparator = Gump.StringToBuffer(" @");
        private static readonly byte[] m_EndTextSeparator = Gump.StringToBuffer("@");
        private static readonly byte[] m_Buffer = new byte[48];
        private int m_TextEntries, m_Switches;
        private int m_LayoutLength;
        public DisplayGumpFast(Gump g)
            : base(0xB0)
        {
            this.EnsureCapacity(4096);

            this.m_Stream.Write((int)g.Serial);
            this.m_Stream.Write((int)g.TypeID);
            this.m_Stream.Write((int)g.X);
            this.m_Stream.Write((int)g.Y);
            this.m_Stream.Write((ushort)0xFFFF);
        }

        static DisplayGumpFast()
        {
            m_Buffer[0] = (byte)' ';
        }

        public int TextEntries
        {
            get
            {
                return this.m_TextEntries;
            }
            set
            {
                this.m_TextEntries = value;
            }
        }
        public int Switches
        {
            get
            {
                return this.m_Switches;
            }
            set
            {
                this.m_Switches = value;
            }
        }
        public void AppendLayout(bool val)
        {
            this.AppendLayout(val ? m_True : m_False);
        }

        public void AppendLayout(int val)
        {
            string toString = val.ToString();
            int bytes = System.Text.Encoding.ASCII.GetBytes(toString, 0, toString.Length, m_Buffer, 1) + 1;

            this.m_Stream.Write(m_Buffer, 0, bytes);
            this.m_LayoutLength += bytes;
        }

        public void AppendLayoutNS(int val)
        {
            string toString = val.ToString();
            int bytes = System.Text.Encoding.ASCII.GetBytes(toString, 0, toString.Length, m_Buffer, 1);

            this.m_Stream.Write(m_Buffer, 1, bytes);
            this.m_LayoutLength += bytes;
        }

        public void AppendLayout(string text)
        {
            this.AppendLayout(m_BeginTextSeparator);

            int length = text.Length;
            this.m_Stream.WriteAsciiFixed(text, length);
            this.m_LayoutLength += length;

            this.AppendLayout(m_EndTextSeparator);
        }

        public void AppendLayout(byte[] buffer)
        {
            int length = buffer.Length;
            this.m_Stream.Write(buffer, 0, length);
            this.m_LayoutLength += length;
        }

        public void WriteStrings(List<string> text)
        {
            this.m_Stream.Seek(19, SeekOrigin.Begin);
            this.m_Stream.Write((ushort)this.m_LayoutLength);
            this.m_Stream.Seek(0, SeekOrigin.End);

            this.m_Stream.Write((ushort)text.Count);

            for (int i = 0; i < text.Count; ++i)
            {
                string v = text[i];

                if (v == null)
                    v = String.Empty;

                int length = (ushort)v.Length;

                this.m_Stream.Write((ushort)length);
                this.m_Stream.WriteBigUniFixed(v, length);
            }
        }

        public void Flush()
        {
        }
    }

    public sealed class DisplayGump : Packet
    {
        public DisplayGump(Gump g, string layout, string[] text)
            : base(0xB0)
        {
            if (layout == null)
                layout = "";

            this.EnsureCapacity(256);

            this.m_Stream.Write((int)g.Serial);
            this.m_Stream.Write((int)g.TypeID);
            this.m_Stream.Write((int)g.X);
            this.m_Stream.Write((int)g.Y);
            this.m_Stream.Write((ushort)(layout.Length + 1));
            this.m_Stream.WriteAsciiNull(layout);

            this.m_Stream.Write((ushort)text.Length);

            for (int i = 0; i < text.Length; ++i)
            {
                string v = text[i];

                if (v == null)
                    v = "";

                int length = (ushort)v.Length;

                this.m_Stream.Write((ushort)length);
                this.m_Stream.WriteBigUniFixed(v, length);
            }
        }
    }

    public sealed class DisplayPaperdoll : Packet
    {
        public DisplayPaperdoll(Mobile m, string text, bool canLift)
            : base(0x88, 66)
        {
            byte flags = 0x00;

            if (m.Warmode)
                flags |= 0x01;

            if (canLift)
                flags |= 0x02;

            this.m_Stream.Write((int)m.Serial);
            this.m_Stream.WriteAsciiFixed(text, 60);
            this.m_Stream.Write((byte)flags);
        }
    }

    public sealed class PopupMessage : Packet
    {
        public PopupMessage(PMMessage msg)
            : base(0x53, 2)
        {
            this.m_Stream.Write((byte)msg);
        }
    }

    public sealed class PlaySound : Packet
    {
        public PlaySound(int soundID, IPoint3D target)
            : base(0x54, 12)
        {
            this.m_Stream.Write((byte)1); // flags
            this.m_Stream.Write((short)soundID);
            this.m_Stream.Write((short)0); // volume
            this.m_Stream.Write((short)target.X);
            this.m_Stream.Write((short)target.Y);
            this.m_Stream.Write((short)target.Z);
        }
    }

    public sealed class PlayMusic : Packet
    {
        public static readonly Packet InvalidInstance = Packet.SetStatic(new PlayMusic(MusicName.Invalid));
        private static readonly Packet[] m_Instances = new Packet[60];
        public PlayMusic(MusicName name)
            : base(0x6D, 3)
        {
            this.m_Stream.Write((short)name);
        }

        public static Packet GetInstance(MusicName name)
        {
            if (name == MusicName.Invalid)
                return InvalidInstance;

            int v = (int)name;
            Packet p;

            if (v >= 0 && v < m_Instances.Length)
            {
                p = m_Instances[v];

                if (p == null)
                    m_Instances[v] = p = Packet.SetStatic(new PlayMusic(name));
            }
            else
            {
                p = new PlayMusic(name);
            }

            return p;
        }
    }

    public sealed class ScrollMessage : Packet
    {
        public ScrollMessage(int type, int tip, string text)
            : base(0xA6)
        {
            if (text == null)
                text = "";

            this.EnsureCapacity(10 + text.Length);

            this.m_Stream.Write((byte)type);
            this.m_Stream.Write((int)tip);
            this.m_Stream.Write((ushort)text.Length);
            this.m_Stream.WriteAsciiFixed(text, text.Length);
        }
    }

    public sealed class CurrentTime : Packet
    {
        public CurrentTime()
            : base(0x5B, 4)
        {
            DateTime now = DateTime.Now;

            this.m_Stream.Write((byte)now.Hour);
            this.m_Stream.Write((byte)now.Minute);
            this.m_Stream.Write((byte)now.Second);
        }
    }

    public sealed class MapChange : Packet
    {
        public MapChange(Mobile m)
            : base(0xBF)
        {
            this.EnsureCapacity(6);

            this.m_Stream.Write((short)0x08);
            this.m_Stream.Write((byte)(m.Map == null ? 0 : m.Map.MapID));
        }
    }

    public sealed class SeasonChange : Packet
    {
        private static readonly SeasonChange[][] m_Cache = new SeasonChange[5][]
        {
            new SeasonChange[2],
            new SeasonChange[2],
            new SeasonChange[2],
            new SeasonChange[2],
            new SeasonChange[2]
        };
        public SeasonChange(int season)
            : this(season, true)
        {
        }

        public SeasonChange(int season, bool playSound)
            : base(0xBC, 3)
        {
            this.m_Stream.Write((byte)season);
            this.m_Stream.Write((bool)playSound);
        }

        public static SeasonChange Instantiate(int season)
        {
            return Instantiate(season, true);
        }

        public static SeasonChange Instantiate(int season, bool playSound)
        {
            if (season >= 0 && season < m_Cache.Length)
            {
                int idx = playSound ? 1 : 0;

                SeasonChange p = m_Cache[season][idx];

                if (p == null)
                {
                    m_Cache[season][idx] = p = new SeasonChange(season, playSound);
                    p.SetStatic();
                }

                return p;
            }
            else
            {
                return new SeasonChange(season, playSound);
            }
        }
    }

    public sealed class SupportedFeatures : Packet
    {
        private static FeatureFlags m_AdditionalFlags;
        public SupportedFeatures(NetState ns)
            : base(0xB9, ns.ExtendedSupportedFeatures ? 5 : 3)
        {
            FeatureFlags flags = ExpansionInfo.CurrentExpansion.SupportedFeatures;

            flags |= m_AdditionalFlags;

            IAccount acct = ns.Account as IAccount;

            if (acct != null && acct.Limit >= 6)
            {
                flags |= FeatureFlags.Unk7;
                flags &= ~FeatureFlags.UOTD;

                if (acct.Limit > 6)
                    flags |= FeatureFlags.SeventhCharacterSlot;
                else
                    flags |= FeatureFlags.SixthCharacterSlot;
            }

            if (ns.ExtendedSupportedFeatures)
            {
                this.m_Stream.Write((uint)flags);
            }
            else
            {
                this.m_Stream.Write((ushort)flags);
            }
        }

        public static FeatureFlags Value
        {
            get
            {
                return m_AdditionalFlags;
            }
            set
            {
                m_AdditionalFlags = value;
            }
        }
        public static SupportedFeatures Instantiate(NetState ns)
        {
            return new SupportedFeatures(ns);
        }
    }

    public sealed class MobileHits : Packet
    {
        public MobileHits(Mobile m)
            : base(0xA1, 9)
        {
            this.m_Stream.Write((int)m.Serial);
            this.m_Stream.Write((short)m.HitsMax);
            this.m_Stream.Write((short)m.Hits);
        }
    }

    public sealed class MobileHitsN : Packet
    {
        public MobileHitsN(Mobile m)
            : base(0xA1, 9)
        {
            this.m_Stream.Write((int)m.Serial);
            AttributeNormalizer.Write(this.m_Stream, m.Hits, m.HitsMax);
        }
    }

    public sealed class MobileMana : Packet
    {
        public MobileMana(Mobile m)
            : base(0xA2, 9)
        {
            this.m_Stream.Write((int)m.Serial);
            this.m_Stream.Write((short)m.ManaMax);
            this.m_Stream.Write((short)m.Mana);
        }
    }

    public sealed class MobileManaN : Packet
    {
        public MobileManaN(Mobile m)
            : base(0xA2, 9)
        {
            this.m_Stream.Write((int)m.Serial);
            AttributeNormalizer.Write(this.m_Stream, m.Mana, m.ManaMax);
        }
    }

    public sealed class MobileStam : Packet
    {
        public MobileStam(Mobile m)
            : base(0xA3, 9)
        {
            this.m_Stream.Write((int)m.Serial);
            this.m_Stream.Write((short)m.StamMax);
            this.m_Stream.Write((short)m.Stam);
        }
    }

    public sealed class MobileStamN : Packet
    {
        public MobileStamN(Mobile m)
            : base(0xA3, 9)
        {
            this.m_Stream.Write((int)m.Serial);
            AttributeNormalizer.Write(this.m_Stream, m.Stam, m.StamMax);
        }
    }

    public sealed class MobileAttributes : Packet
    {
        public MobileAttributes(Mobile m)
            : base(0x2D, 17)
        {
            this.m_Stream.Write(m.Serial);

            this.m_Stream.Write((short)m.HitsMax);
            this.m_Stream.Write((short)m.Hits);

            this.m_Stream.Write((short)m.ManaMax);
            this.m_Stream.Write((short)m.Mana);

            this.m_Stream.Write((short)m.StamMax);
            this.m_Stream.Write((short)m.Stam);
        }
    }

    public sealed class MobileAttributesN : Packet
    {
        public MobileAttributesN(Mobile m)
            : base(0x2D, 17)
        {
            this.m_Stream.Write(m.Serial);

            AttributeNormalizer.Write(this.m_Stream, m.Hits, m.HitsMax);
            AttributeNormalizer.Write(this.m_Stream, m.Mana, m.ManaMax);
            AttributeNormalizer.Write(this.m_Stream, m.Stam, m.StamMax);
        }
    }

    public sealed class PathfindMessage : Packet
    {
        public PathfindMessage(IPoint3D p)
            : base(0x38, 7)
        {
            this.m_Stream.Write((short)p.X);
            this.m_Stream.Write((short)p.Y);
            this.m_Stream.Write((short)p.Z);
        }
    }

    // unsure of proper format, client crashes
    public sealed class MobileName : Packet
    {
        public MobileName(Mobile m)
            : base(0x98)
        {
            string name = m.Name;

            if (name == null)
                name = "";

            this.EnsureCapacity(37);

            this.m_Stream.Write((int)m.Serial);
            this.m_Stream.WriteAsciiFixed(name, 30);
        }
    }

    public sealed class MobileAnimation : Packet
    {
        public MobileAnimation(Mobile m, int action, int frameCount, int repeatCount, bool forward, bool repeat, int delay)
            : base(0x6E, 14)
        {
            this.m_Stream.Write((int)m.Serial);
            this.m_Stream.Write((short)action);
            this.m_Stream.Write((short)frameCount);
            this.m_Stream.Write((short)repeatCount);
            this.m_Stream.Write((bool)!forward); // protocol has really "reverse" but I find this more intuitive
            this.m_Stream.Write((bool)repeat);
            this.m_Stream.Write((byte)delay);
        }
    }

    public sealed class NewMobileAnimation : Packet
    {
        public NewMobileAnimation(Mobile m, int action, int frameCount, int delay)
            : base(0xE2, 10)
        {
            this.m_Stream.Write((int)m.Serial);
            this.m_Stream.Write((short)action);
            this.m_Stream.Write((short)frameCount);
            this.m_Stream.Write((byte)delay);
        }
    }

    public sealed class MobileStatusCompact : Packet
    {
        public MobileStatusCompact(bool canBeRenamed, Mobile m)
            : base(0x11)
        {
            string name = m.Name;
            if (name == null)
                name = "";

            this.EnsureCapacity(43);

            this.m_Stream.Write((int)m.Serial);
            this.m_Stream.WriteAsciiFixed(name, 30);

            AttributeNormalizer.WriteReverse(this.m_Stream, m.Hits, m.HitsMax);

            this.m_Stream.Write(canBeRenamed);

            this.m_Stream.Write((byte)0); // type
        }
    }

    public sealed class MobileStatusExtended : Packet
    {
        public MobileStatusExtended(Mobile m)
            : this(m, m.NetState)
        {
        }

        public MobileStatusExtended(Mobile m, NetState ns)
            : base(0x11)
        {
            string name = m.Name;
            if (name == null)
                name = "";

            bool sendMLExtended = (Core.ML && ns != null && ns.SupportsExpansion(Expansion.ML));

            this.EnsureCapacity(sendMLExtended ? 91 : 88);

            this.m_Stream.Write((int)m.Serial);
            this.m_Stream.WriteAsciiFixed(name, 30);

            this.m_Stream.Write((short)m.Hits);
            this.m_Stream.Write((short)m.HitsMax);

            this.m_Stream.Write(m.CanBeRenamedBy(m));

            this.m_Stream.Write((byte)(sendMLExtended ? 0x05 : Core.AOS ? 0x04 : 0x03)); // type

            this.m_Stream.Write(m.Female);

            this.m_Stream.Write((short)m.Str);
            this.m_Stream.Write((short)m.Dex);
            this.m_Stream.Write((short)m.Int);

            this.m_Stream.Write((short)m.Stam);
            this.m_Stream.Write((short)m.StamMax);

            this.m_Stream.Write((short)m.Mana);
            this.m_Stream.Write((short)m.ManaMax);

            this.m_Stream.Write((int)m.TotalGold);
            this.m_Stream.Write((short)(Core.AOS ? m.PhysicalResistance : (int)(m.ArmorRating + 0.5)));
            this.m_Stream.Write((short)(Mobile.BodyWeight + m.TotalWeight));

            if (sendMLExtended)
            {
                this.m_Stream.Write((short)m.MaxWeight);
                this.m_Stream.Write((byte)(m.Race.RaceID + 1));	// Would be 0x00 if it's a non-ML enabled account but...
            }

            this.m_Stream.Write((short)m.StatCap);

            this.m_Stream.Write((byte)m.Followers);
            this.m_Stream.Write((byte)m.FollowersMax);

            if (Core.AOS)
            {
                this.m_Stream.Write((short)m.FireResistance); // Fire
                this.m_Stream.Write((short)m.ColdResistance); // Cold
                this.m_Stream.Write((short)m.PoisonResistance); // Poison
                this.m_Stream.Write((short)m.EnergyResistance); // Energy
                this.m_Stream.Write((short)m.Luck); // Luck

                IWeapon weapon = m.Weapon;

                int min = 0, max = 0;

                if (weapon != null)
                    weapon.GetStatusDamage(m, out min, out max);

                this.m_Stream.Write((short)min); // Damage min
                this.m_Stream.Write((short)max); // Damage max

                this.m_Stream.Write((int)m.TithingPoints);
            }
        }
    }

    public sealed class MobileStatus : Packet
    {
        public MobileStatus(Mobile beholder, Mobile beheld)
            : this(beholder, beheld, beheld.NetState)
        {
        }

        public MobileStatus(Mobile beholder, Mobile beheld, NetState ns)
            : base(0x11)
        {
            string name = beheld.Name;
            if (name == null)
                name = "";

            bool sendMLExtended = (Core.ML && ns != null && ns.SupportsExpansion(Expansion.ML));

            this.EnsureCapacity(43 + (beholder == beheld ? (sendMLExtended ? 48 : 45) : 0));

            this.m_Stream.Write(beheld.Serial);

            this.m_Stream.WriteAsciiFixed(name, 30);

            if (beholder == beheld)
                this.WriteAttr(beheld.Hits, beheld.HitsMax);
            else
                this.WriteAttrNorm(beheld.Hits, beheld.HitsMax);

            this.m_Stream.Write(beheld.CanBeRenamedBy(beholder));

            if (beholder == beheld)
            {
                this.m_Stream.Write((byte)(sendMLExtended ? 0x05 : Core.AOS ? 0x04 : 0x03)); // type

                this.m_Stream.Write(beheld.Female);

                this.m_Stream.Write((short)beheld.Str);
                this.m_Stream.Write((short)beheld.Dex);
                this.m_Stream.Write((short)beheld.Int);

                this.WriteAttr(beheld.Stam, beheld.StamMax);
                this.WriteAttr(beheld.Mana, beheld.ManaMax);

                this.m_Stream.Write((int)beheld.TotalGold);
                this.m_Stream.Write((short)(Core.AOS ? beheld.PhysicalResistance : (int)(beheld.ArmorRating + 0.5)));
                this.m_Stream.Write((short)(Mobile.BodyWeight + beheld.TotalWeight));

                if (sendMLExtended)
                {
                    this.m_Stream.Write((short)beheld.MaxWeight);
                    this.m_Stream.Write((byte)(beheld.Race.RaceID + 1));	// Would be 0x00 if it's a non-ML enabled account but...
                }

                this.m_Stream.Write((short)beheld.StatCap);

                this.m_Stream.Write((byte)beheld.Followers);
                this.m_Stream.Write((byte)beheld.FollowersMax);

                if (Core.AOS)
                {
                    this.m_Stream.Write((short)beheld.FireResistance); // Fire
                    this.m_Stream.Write((short)beheld.ColdResistance); // Cold
                    this.m_Stream.Write((short)beheld.PoisonResistance); // Poison
                    this.m_Stream.Write((short)beheld.EnergyResistance); // Energy
                    this.m_Stream.Write((short)beheld.Luck); // Luck

                    IWeapon weapon = beheld.Weapon;

                    int min = 0, max = 0;

                    if (weapon != null)
                        weapon.GetStatusDamage(beheld, out min, out max);

                    this.m_Stream.Write((short)min); // Damage min
                    this.m_Stream.Write((short)max); // Damage max

                    this.m_Stream.Write((int)beheld.TithingPoints);
                }
            }
            else
            {
                this.m_Stream.Write((byte)0x00);
            }
        }

        private void WriteAttr(int current, int maximum)
        {
            this.m_Stream.Write((short)current);
            this.m_Stream.Write((short)maximum);
        }

        private void WriteAttrNorm(int current, int maximum)
        {
            AttributeNormalizer.WriteReverse(this.m_Stream, current, maximum);
        }
    }

    public sealed class HealthbarPoison : Packet
    {
        public HealthbarPoison(Mobile m)
            : base(0x17)
        {
            this.EnsureCapacity(12);

            this.m_Stream.Write((int)m.Serial);
            this.m_Stream.Write((short)1);
			
            this.m_Stream.Write((short)1);

            Poison p = m.Poison;

            if (p != null)
            {
                this.m_Stream.Write((byte)(p.Level + 1));
            }
            else
            {
                this.m_Stream.Write((byte)0);
            }
        }
    }

    public sealed class HealthbarYellow : Packet
    {
        public HealthbarYellow(Mobile m)
            : base(0x17)
        {
            this.EnsureCapacity(12);

            this.m_Stream.Write((int)m.Serial);
            this.m_Stream.Write((short)1);

            this.m_Stream.Write((short)2);

            if (m.Blessed || m.YellowHealthbar)
            {
                this.m_Stream.Write((byte)1);
            }
            else
            {
                this.m_Stream.Write((byte)0);
            }
        }
    }

    public sealed class MobileUpdate : Packet
    {
        public MobileUpdate(Mobile m)
            : base(0x20, 19)
        {
            int hue = m.Hue;

            if (m.SolidHueOverride >= 0)
                hue = m.SolidHueOverride;

            this.m_Stream.Write((int)m.Serial);
            this.m_Stream.Write((short)m.Body);
            this.m_Stream.Write((byte)0);
            this.m_Stream.Write((short)hue);
            this.m_Stream.Write((byte)m.GetPacketFlags());
            this.m_Stream.Write((short)m.X);
            this.m_Stream.Write((short)m.Y);
            this.m_Stream.Write((short)0);
            this.m_Stream.Write((byte)m.Direction);
            this.m_Stream.Write((sbyte)m.Z);
        }
    }

    // Pre-7.0.0.0 Mobile Update
    public sealed class MobileUpdateOld : Packet
    {
        public MobileUpdateOld(Mobile m)
            : base(0x20, 19)
        {
            int hue = m.Hue;

            if (m.SolidHueOverride >= 0)
                hue = m.SolidHueOverride;

            this.m_Stream.Write((int)m.Serial);
            this.m_Stream.Write((short)m.Body);
            this.m_Stream.Write((byte)0);
            this.m_Stream.Write((short)hue);
            this.m_Stream.Write((byte)m.GetOldPacketFlags());
            this.m_Stream.Write((short)m.X);
            this.m_Stream.Write((short)m.Y);
            this.m_Stream.Write((short)0);
            this.m_Stream.Write((byte)m.Direction);
            this.m_Stream.Write((sbyte)m.Z);
        }
    }

    public sealed class MobileIncoming : Packet
    {
        public Mobile m_Beheld;
        private static readonly int[] m_DupedLayers = new int[256];
        private static int m_Version;
        public MobileIncoming(Mobile beholder, Mobile beheld)
            : base(0x78)
        {
            this.m_Beheld = beheld;
            ++m_Version;

            List<Item> eq = beheld.Items;
            int count = eq.Count;

            if (beheld.HairItemID > 0)
                count++;
            if (beheld.FacialHairItemID > 0)
                count++;

            this.EnsureCapacity(23 + (count * 9));

            int hue = beheld.Hue;

            if (beheld.SolidHueOverride >= 0)
                hue = beheld.SolidHueOverride;

            this.m_Stream.Write((int)beheld.Serial);
            this.m_Stream.Write((short)beheld.Body);
            this.m_Stream.Write((short)beheld.X);
            this.m_Stream.Write((short)beheld.Y);
            this.m_Stream.Write((sbyte)beheld.Z);
            this.m_Stream.Write((byte)beheld.Direction);
            this.m_Stream.Write((short)hue);
            this.m_Stream.Write((byte)beheld.GetPacketFlags());
            this.m_Stream.Write((byte)Notoriety.Compute(beholder, beheld));

            for (int i = 0; i < eq.Count; ++i)
            {
                Item item = eq[i];

                byte layer = (byte)item.Layer;

                if (!item.Deleted && beholder.CanSee(item) && m_DupedLayers[layer] != m_Version)
                {
                    m_DupedLayers[layer] = m_Version;

                    hue = item.Hue;

                    if (beheld.SolidHueOverride >= 0)
                        hue = beheld.SolidHueOverride;

                    int itemID = item.ItemID & 0x7FFF;
                    bool writeHue = (hue != 0);

                    if (writeHue)
                        itemID |= 0x8000;

                    this.m_Stream.Write((int)item.Serial);
                    this.m_Stream.Write((ushort)itemID);
                    this.m_Stream.Write((byte)layer);

                    if (writeHue)
                        this.m_Stream.Write((short)hue);
                }
            }

            if (beheld.HairItemID > 0)
            {
                if (m_DupedLayers[(int)Layer.Hair] != m_Version)
                {
                    m_DupedLayers[(int)Layer.Hair] = m_Version;
                    hue = beheld.HairHue;

                    if (beheld.SolidHueOverride >= 0)
                        hue = beheld.SolidHueOverride;

                    int itemID = beheld.HairItemID & 0x7FFF;

                    bool writeHue = (hue != 0);

                    if (writeHue)
                        itemID |= 0x8000;

                    this.m_Stream.Write((int)HairInfo.FakeSerial(beheld));
                    this.m_Stream.Write((ushort)itemID);
                    this.m_Stream.Write((byte)Layer.Hair);

                    if (writeHue)
                        this.m_Stream.Write((short)hue);
                }
            }

            if (beheld.FacialHairItemID > 0)
            {
                if (m_DupedLayers[(int)Layer.FacialHair] != m_Version)
                {
                    m_DupedLayers[(int)Layer.FacialHair] = m_Version;
                    hue = beheld.FacialHairHue;

                    if (beheld.SolidHueOverride >= 0)
                        hue = beheld.SolidHueOverride;

                    int itemID = beheld.FacialHairItemID & 0x7FFF;

                    bool writeHue = (hue != 0);

                    if (writeHue)
                        itemID |= 0x8000;

                    this.m_Stream.Write((int)FacialHairInfo.FakeSerial(beheld));
                    this.m_Stream.Write((ushort)itemID);
                    this.m_Stream.Write((byte)Layer.FacialHair);

                    if (writeHue)
                        this.m_Stream.Write((short)hue);
                }
            }

            this.m_Stream.Write((int)0); // terminate
        }
    }

    // Pre-7.0.0.0 Mobile Incoming
    public sealed class MobileIncomingOld : Packet
    {
        public Mobile m_Beheld;
        private static readonly int[] m_DupedLayers = new int[256];
        private static int m_Version;
        public MobileIncomingOld(Mobile beholder, Mobile beheld)
            : base(0x78)
        {
            this.m_Beheld = beheld;
            ++m_Version;

            List<Item> eq = beheld.Items;
            int count = eq.Count;

            if (beheld.HairItemID > 0)
                count++;
            if (beheld.FacialHairItemID > 0)
                count++;

            this.EnsureCapacity(23 + (count * 9));

            int hue = beheld.Hue;

            if (beheld.SolidHueOverride >= 0)
                hue = beheld.SolidHueOverride;

            this.m_Stream.Write((int)beheld.Serial);
            this.m_Stream.Write((short)beheld.Body);
            this.m_Stream.Write((short)beheld.X);
            this.m_Stream.Write((short)beheld.Y);
            this.m_Stream.Write((sbyte)beheld.Z);
            this.m_Stream.Write((byte)beheld.Direction);
            this.m_Stream.Write((short)hue);
            this.m_Stream.Write((byte)beheld.GetOldPacketFlags());
            this.m_Stream.Write((byte)Notoriety.Compute(beholder, beheld));

            for (int i = 0; i < eq.Count; ++i)
            {
                Item item = eq[i];

                byte layer = (byte)item.Layer;

                if (!item.Deleted && beholder.CanSee(item) && m_DupedLayers[layer] != m_Version)
                {
                    m_DupedLayers[layer] = m_Version;

                    hue = item.Hue;

                    if (beheld.SolidHueOverride >= 0)
                        hue = beheld.SolidHueOverride;

                    int itemID = item.ItemID & 0x7FFF;
                    bool writeHue = (hue != 0);

                    if (writeHue)
                        itemID |= 0x8000;

                    this.m_Stream.Write((int)item.Serial);
                    this.m_Stream.Write((ushort)itemID);
                    this.m_Stream.Write((byte)layer);

                    if (writeHue)
                        this.m_Stream.Write((short)hue);
                }
            }

            if (beheld.HairItemID > 0)
            {
                if (m_DupedLayers[(int)Layer.Hair] != m_Version)
                {
                    m_DupedLayers[(int)Layer.Hair] = m_Version;
                    hue = beheld.HairHue;

                    if (beheld.SolidHueOverride >= 0)
                        hue = beheld.SolidHueOverride;

                    int itemID = beheld.HairItemID & 0x7FFF;

                    bool writeHue = (hue != 0);

                    if (writeHue)
                        itemID |= 0x8000;

                    this.m_Stream.Write((int)HairInfo.FakeSerial(beheld));
                    this.m_Stream.Write((ushort)itemID);
                    this.m_Stream.Write((byte)Layer.Hair);

                    if (writeHue)
                        this.m_Stream.Write((short)hue);
                }
            }

            if (beheld.FacialHairItemID > 0)
            {
                if (m_DupedLayers[(int)Layer.FacialHair] != m_Version)
                {
                    m_DupedLayers[(int)Layer.FacialHair] = m_Version;
                    hue = beheld.FacialHairHue;

                    if (beheld.SolidHueOverride >= 0)
                        hue = beheld.SolidHueOverride;

                    int itemID = beheld.FacialHairItemID & 0x7FFF;

                    bool writeHue = (hue != 0);

                    if (writeHue)
                        itemID |= 0x8000;

                    this.m_Stream.Write((int)FacialHairInfo.FakeSerial(beheld));
                    this.m_Stream.Write((ushort)itemID);
                    this.m_Stream.Write((byte)Layer.FacialHair);

                    if (writeHue)
                        this.m_Stream.Write((short)hue);
                }
            }

            this.m_Stream.Write((int)0); // terminate
        }
    }

    public sealed class AsciiMessage : Packet
    {
        public AsciiMessage(Serial serial, int graphic, MessageType type, int hue, int font, string name, string text)
            : base(0x1C)
        {
            if (name == null)
                name = "";

            if (text == null)
                text = "";

            if (hue == 0)
                hue = 0x3B2;

            this.EnsureCapacity(45 + text.Length);

            this.m_Stream.Write((int)serial);
            this.m_Stream.Write((short)graphic);
            this.m_Stream.Write((byte)type);
            this.m_Stream.Write((short)hue);
            this.m_Stream.Write((short)font);
            this.m_Stream.WriteAsciiFixed(name, 30);
            this.m_Stream.WriteAsciiNull(text);
        }
    }

    public sealed class UnicodeMessage : Packet
    {
        public UnicodeMessage(Serial serial, int graphic, MessageType type, int hue, int font, string lang, string name, string text)
            : base(0xAE)
        {
            if (string.IsNullOrEmpty(lang))
                lang = "ENU";
            if (name == null)
                name = "";
            if (text == null)
                text = "";

            if (hue == 0)
                hue = 0x3B2;

            this.EnsureCapacity(50 + (text.Length * 2));

            this.m_Stream.Write((int)serial);
            this.m_Stream.Write((short)graphic);
            this.m_Stream.Write((byte)type);
            this.m_Stream.Write((short)hue);
            this.m_Stream.Write((short)font);
            this.m_Stream.WriteAsciiFixed(lang, 4);
            this.m_Stream.WriteAsciiFixed(name, 30);
            this.m_Stream.WriteBigUniNull(text);
        }
    }

    public sealed class PingAck : Packet
    {
        private static readonly PingAck[] m_Cache = new PingAck[0x100];
        public PingAck(byte ping)
            : base(0x73, 2)
        {
            this.m_Stream.Write(ping);
        }

        public static PingAck Instantiate(byte ping)
        {
            PingAck p = m_Cache[ping];

            if (p == null)
            {
                m_Cache[ping] = p = new PingAck(ping);
                p.SetStatic();
            }

            return p;
        }
    }

    public sealed class MovementRej : Packet
    {
        public MovementRej(int seq, Mobile m)
            : base(0x21, 8)
        {
            this.m_Stream.Write((byte)seq);
            this.m_Stream.Write((short)m.X);
            this.m_Stream.Write((short)m.Y);
            this.m_Stream.Write((byte)m.Direction);
            this.m_Stream.Write((sbyte)m.Z);
        }
    }

    public sealed class MovementAck : Packet
    {
        private static readonly MovementAck[][] m_Cache = new MovementAck[8][]
        {
            new MovementAck[256],
            new MovementAck[256],
            new MovementAck[256],
            new MovementAck[256],
            new MovementAck[256],
            new MovementAck[256],
            new MovementAck[256],
            new MovementAck[256]
        };
        private MovementAck(int seq, int noto)
            : base(0x22, 3)
        {
            this.m_Stream.Write((byte)seq);
            this.m_Stream.Write((byte)noto);
        }

        public static MovementAck Instantiate(int seq, Mobile m)
        {
            int noto = Notoriety.Compute(m, m);

            MovementAck p = m_Cache[noto][seq];

            if (p == null)
            {
                m_Cache[noto][seq] = p = new MovementAck(seq, noto);
                p.SetStatic();
            }

            return p;
        }
    }

    public sealed class LoginConfirm : Packet
    {
        public LoginConfirm(Mobile m)
            : base(0x1B, 37)
        {
            this.m_Stream.Write((int)m.Serial);
            this.m_Stream.Write((int)0);
            this.m_Stream.Write((short)m.Body);
            this.m_Stream.Write((short)m.X);
            this.m_Stream.Write((short)m.Y);
            this.m_Stream.Write((short)m.Z);
            this.m_Stream.Write((byte)m.Direction);
            this.m_Stream.Write((byte)0);
            this.m_Stream.Write((int)-1);

            Map map = m.Map;

            if (map == null || map == Map.Internal)
                map = m.LogoutMap;

            this.m_Stream.Write((short)0);
            this.m_Stream.Write((short)0);
            this.m_Stream.Write((short)(map == null ? 6144 : map.Width));
            this.m_Stream.Write((short)(map == null ? 4096 : map.Height));

            this.m_Stream.Fill();
        }
    }

    public sealed class LoginComplete : Packet
    {
        public static readonly Packet Instance = Packet.SetStatic(new LoginComplete());
        public LoginComplete()
            : base(0x55, 1)
        {
        }
    }

    public sealed class CityInfo
    {
        private string m_City;
        private string m_Building;
        private int m_Description;
        private Point3D m_Location;
        private Map m_Map;
        public CityInfo(string city, string building, int description, int x, int y, int z, Map m)
        {
            this.m_City = city;
            this.m_Building = building;
            this.m_Description = description;
            this.m_Location = new Point3D(x, y, z);
            this.m_Map = m;
        }

        public CityInfo(string city, string building, int x, int y, int z, Map m)
            : this(city, building, 0, x, y, z, m)
        {
        }

        public CityInfo(string city, string building, int description, int x, int y, int z)
            : this(city, building, description, x, y, z, Map.Trammel)
        {
        }

        public CityInfo(string city, string building, int x, int y, int z)
            : this(city, building, 0, x, y, z, Map.Trammel)
        {
        }

        public string City
        {
            get
            {
                return this.m_City;
            }
            set
            {
                this.m_City = value;
            }
        }
        public string Building
        {
            get
            {
                return this.m_Building;
            }
            set
            {
                this.m_Building = value;
            }
        }
        public int Description
        {
            get
            {
                return this.m_Description;
            }
            set
            {
                this.m_Description = value;
            }
        }
        public int X
        {
            get
            {
                return this.m_Location.X;
            }
            set
            {
                this.m_Location.X = value;
            }
        }
        public int Y
        {
            get
            {
                return this.m_Location.Y;
            }
            set
            {
                this.m_Location.Y = value;
            }
        }
        public int Z
        {
            get
            {
                return this.m_Location.Z;
            }
            set
            {
                this.m_Location.Z = value;
            }
        }
        public Point3D Location
        {
            get
            {
                return this.m_Location;
            }
            set
            {
                this.m_Location = value;
            }
        }
        public Map Map
        {
            get
            {
                return this.m_Map;
            }
            set
            {
                this.m_Map = value;
            }
        }
    }

    public sealed class CharacterListUpdate : Packet
    {
        public CharacterListUpdate(IAccount a)
            : base(0x86)
        {
            this.EnsureCapacity(4 + (a.Length * 60));

            int highSlot = -1;

            for (int i = 0; i < a.Length; ++i)
            {
                if (a[i] != null)
                    highSlot = i;
            }

            int count = Math.Max(Math.Max(highSlot + 1, a.Limit), 5);

            this.m_Stream.Write((byte)count);

            for (int i = 0; i < count; ++i)
            {
                Mobile m = a[i];

                if (m != null)
                {
                    this.m_Stream.WriteAsciiFixed(m.Name, 30);
                    this.m_Stream.Fill(30); // password
                }
                else
                {
                    this.m_Stream.Fill(60);
                }
            }
        }
    }

    public sealed class CharacterList : Packet
    {
        private static CharacterListFlags m_AdditionalFlags;
        public CharacterList(IAccount a, CityInfo[] info)
            : base(0xA9)
        {
            this.EnsureCapacity(11 + (a.Length * 60) + (info.Length * 89));

            int highSlot = -1;

            for (int i = 0; i < a.Length; ++i)
            {
                if (a[i] != null)
                    highSlot = i;
            }

            int count = Math.Max(Math.Max(highSlot + 1, a.Limit), 5);

            this.m_Stream.Write((byte)count);

            for (int i = 0; i < count; ++i)
            {
                if (a[i] != null)
                {
                    this.m_Stream.WriteAsciiFixed(a[i].Name, 30);
                    this.m_Stream.Fill(30); // password
                }
                else
                {
                    this.m_Stream.Fill(60);
                }
            }

            this.m_Stream.Write((byte)info.Length);

            for (int i = 0; i < info.Length; ++i)
            {
                CityInfo ci = info[i];

                this.m_Stream.Write((byte)i);
                this.m_Stream.WriteAsciiFixed(ci.City, 32);
                this.m_Stream.WriteAsciiFixed(ci.Building, 32);
                this.m_Stream.Write((int)ci.X);
                this.m_Stream.Write((int)ci.Y);
                this.m_Stream.Write((int)ci.Z);
                this.m_Stream.Write((int)ci.Map.MapID);
                this.m_Stream.Write((int)ci.Description);
                this.m_Stream.Write((int)0);
            }

            CharacterListFlags flags = ExpansionInfo.CurrentExpansion.CharacterListFlags;

            if (count > 6)
                flags |= (CharacterListFlags.SeventhCharacterSlot | CharacterListFlags.SixthCharacterSlot); // 7th Character Slot - TODO: Is SixthCharacterSlot Required?
            else if (count == 6)
                flags |= CharacterListFlags.SixthCharacterSlot; // 6th Character Slot
            else if (a.Limit == 1)
                flags |= (CharacterListFlags.SlotLimit & CharacterListFlags.OneCharacterSlot); // Limit Characters & One Character

            this.m_Stream.Write((int)(flags | m_AdditionalFlags)); // Additional Flags
        }

        public static CharacterListFlags AdditionalFlags
        {
            get
            {
                return m_AdditionalFlags;
            }
            set
            {
                m_AdditionalFlags = value;
            }
        }
    }

    public sealed class CharacterListOld : Packet
    {
        public CharacterListOld(IAccount a, CityInfo[] info)
            : base(0xA9)
        {
            this.EnsureCapacity(9 + (a.Length * 60) + (info.Length * 63));

            int highSlot = -1;

            for (int i = 0; i < a.Length; ++i)
            {
                if (a[i] != null)
                    highSlot = i;
            }

            int count = Math.Max(Math.Max(highSlot + 1, a.Limit), 5);

            this.m_Stream.Write((byte)count);

            for (int i = 0; i < count; ++i)
            {
                if (a[i] != null)
                {
                    this.m_Stream.WriteAsciiFixed(a[i].Name, 30);
                    this.m_Stream.Fill(30); // password
                }
                else
                {
                    this.m_Stream.Fill(60);
                }
            }

            this.m_Stream.Write((byte)info.Length);

            for (int i = 0; i < info.Length; ++i)
            {
                CityInfo ci = info[i];

                this.m_Stream.Write((byte)i);
                this.m_Stream.WriteAsciiFixed(ci.City, 31);
                this.m_Stream.WriteAsciiFixed(ci.Building, 31);
            }

            CharacterListFlags flags = ExpansionInfo.CurrentExpansion.CharacterListFlags;

            if (count > 6)
                flags |= (CharacterListFlags.SeventhCharacterSlot | CharacterListFlags.SixthCharacterSlot); // 7th Character Slot - TODO: Is SixthCharacterSlot Required?
            else if (count == 6)
                flags |= CharacterListFlags.SixthCharacterSlot; // 6th Character Slot
            else if (a.Limit == 1)
                flags |= (CharacterListFlags.SlotLimit & CharacterListFlags.OneCharacterSlot); // Limit Characters & One Character

            this.m_Stream.Write((int)(flags | CharacterList.AdditionalFlags)); // Additional Flags
        }
    }

    public sealed class ClearWeaponAbility : Packet
    {
        public static readonly Packet Instance = Packet.SetStatic(new ClearWeaponAbility());
        public ClearWeaponAbility()
            : base(0xBF)
        {
            this.EnsureCapacity(5);

            this.m_Stream.Write((short)0x21);
        }
    }

    public sealed class AccountLoginRej : Packet
    {
        public AccountLoginRej(ALRReason reason)
            : base(0x82, 2)
        {
            this.m_Stream.Write((byte)reason);
        }
    }

    public sealed class MessageLocalizedAffix : Packet
    {
        public MessageLocalizedAffix(Serial serial, int graphic, MessageType messageType, int hue, int font, int number, string name, AffixType affixType, string affix, string args)
            : base(0xCC)
        {
            if (name == null)
                name = "";
            if (affix == null)
                affix = "";
            if (args == null)
                args = "";

            if (hue == 0)
                hue = 0x3B2;

            this.EnsureCapacity(52 + affix.Length + (args.Length * 2));

            this.m_Stream.Write((int)serial);
            this.m_Stream.Write((short)graphic);
            this.m_Stream.Write((byte)messageType);
            this.m_Stream.Write((short)hue);
            this.m_Stream.Write((short)font);
            this.m_Stream.Write((int)number);
            this.m_Stream.Write((byte)affixType);
            this.m_Stream.WriteAsciiFixed(name, 30);
            this.m_Stream.WriteAsciiNull(affix);
            this.m_Stream.WriteBigUniNull(args);
        }
    }

    public sealed class ServerInfo
    {
        private string m_Name;
        private int m_FullPercent;
        private int m_TimeZone;
        private IPEndPoint m_Address;
        public ServerInfo(string name, int fullPercent, TimeZone tz, IPEndPoint address)
        {
            this.m_Name = name;
            this.m_FullPercent = fullPercent;
            this.m_TimeZone = tz.GetUtcOffset(DateTime.Now).Hours;
            this.m_Address = address;
        }

        public string Name
        {
            get
            {
                return this.m_Name;
            }
            set
            {
                this.m_Name = value;
            }
        }
        public int FullPercent
        {
            get
            {
                return this.m_FullPercent;
            }
            set
            {
                this.m_FullPercent = value;
            }
        }
        public int TimeZone
        {
            get
            {
                return this.m_TimeZone;
            }
            set
            {
                this.m_TimeZone = value;
            }
        }
        public IPEndPoint Address
        {
            get
            {
                return this.m_Address;
            }
            set
            {
                this.m_Address = value;
            }
        }
    }

    public sealed class FollowMessage : Packet
    {
        public FollowMessage(Serial serial1, Serial serial2)
            : base(0x15, 9)
        {
            this.m_Stream.Write((int)serial1);
            this.m_Stream.Write((int)serial2);
        }
    }

    public sealed class AccountLoginAck : Packet
    {
        public AccountLoginAck(ServerInfo[] info)
            : base(0xA8)
        {
            this.EnsureCapacity(6 + (info.Length * 40));

            this.m_Stream.Write((byte)0x5D); // Unknown

            this.m_Stream.Write((ushort)info.Length);

            for (int i = 0; i < info.Length; ++i)
            {
                ServerInfo si = info[i];

                this.m_Stream.Write((ushort)i);
                this.m_Stream.WriteAsciiFixed(si.Name, 32);
                this.m_Stream.Write((byte)si.FullPercent);
                this.m_Stream.Write((sbyte)si.TimeZone);
                this.m_Stream.Write((int)Utility.GetAddressValue(si.Address.Address));
            }
        }
    }

    public sealed class DisplaySignGump : Packet
    {
        public DisplaySignGump(Serial serial, int gumpID, string unknown, string caption)
            : base(0x8B)
        {
            if (unknown == null)
                unknown = "";
            if (caption == null)
                caption = "";

            this.EnsureCapacity(16 + unknown.Length + caption.Length);

            this.m_Stream.Write((int)serial);
            this.m_Stream.Write((short)gumpID);
            this.m_Stream.Write((short)(unknown.Length));
            this.m_Stream.WriteAsciiFixed(unknown, unknown.Length);
            this.m_Stream.Write((short)(caption.Length + 1));
            this.m_Stream.WriteAsciiFixed(caption, caption.Length + 1);
        }
    }

    public sealed class GodModeReply : Packet
    {
        public GodModeReply(bool reply)
            : base(0x2B, 2)
        {
            this.m_Stream.Write(reply);
        }
    }

    public sealed class PlayServerAck : Packet
    {
        internal static int m_AuthID = -1;
        public PlayServerAck(ServerInfo si)
            : base(0x8C, 11)
        {
            int addr = Utility.GetAddressValue(si.Address.Address);

            this.m_Stream.Write((byte)addr);
            this.m_Stream.Write((byte)(addr >> 8));
            this.m_Stream.Write((byte)(addr >> 16));
            this.m_Stream.Write((byte)(addr >> 24));

            this.m_Stream.Write((short)si.Address.Port);
            this.m_Stream.Write((int)m_AuthID);
        }
    }

    public abstract class Packet
    {
        protected PacketWriter m_Stream;
        private static readonly BufferPool m_Buffers = new BufferPool("Compressed", 16, BufferSize);
        private const int BufferSize = 4096;
        private readonly int m_PacketID;
        private readonly int m_Length;
        private State m_State;
        private byte[] m_CompiledBuffer;
        private int m_CompiledLength;
        protected Packet(int packetID)
        {
            this.m_PacketID = packetID;

            PacketSendProfile prof = PacketSendProfile.Acquire(this.GetType());

            if (prof != null)
            {
                prof.Created++;
            }
        }

        protected Packet(int packetID, int length)
        {
            this.m_PacketID = packetID;
            this.m_Length = length;

            this.m_Stream = PacketWriter.CreateInstance(length);// new PacketWriter( length );
            this.m_Stream.Write((byte)packetID);

            PacketSendProfile prof = PacketSendProfile.Acquire(this.GetType());

            if (prof != null)
            {
                prof.Created++;
            }
        }

        [Flags]
        private enum State
        {
            Inactive = 0x00,
            Static = 0x01,
            Acquired = 0x02,
            Accessed = 0x04,
            Buffered = 0x08,
            Warned = 0x10
        }
        public int PacketID
        {
            get
            {
                return this.m_PacketID;
            }
        }
        public PacketWriter UnderlyingStream
        {
            get
            {
                return this.m_Stream;
            }
        }
        public static Packet SetStatic(Packet p)
        {
            p.SetStatic();
            return p;
        }

        public static Packet Acquire(Packet p)
        {
            p.Acquire();
            return p;
        }

        public static void Release(ref ObjectPropertyList p)
        {
            if (p != null)
                p.Release();

            p = null;
        }

        public static void Release(ref RemoveItem p)
        {
            if (p != null)
                p.Release();

            p = null;
        }

        public static void Release(ref RemoveMobile p)
        {
            if (p != null)
                p.Release();

            p = null;
        }

        public static void Release(ref OPLInfo p)
        {
            if (p != null)
                p.Release();

            p = null;
        }

        public static void Release(ref Packet p)
        {
            if (p != null)
                p.Release();

            p = null;
        }

        public static void Release(Packet p)
        {
            if (p != null)
                p.Release();
        }

        public void EnsureCapacity(int length)
        {
            this.m_Stream = PacketWriter.CreateInstance(length);// new PacketWriter( length );
            this.m_Stream.Write((byte)this.m_PacketID);
            this.m_Stream.Write((short)0);
        }

        public void SetStatic()
        {
            this.m_State |= State.Static | State.Acquired;
        }

        public void Acquire()
        {
            this.m_State |= State.Acquired;
        }

        public void OnSend()
        {
            if ((this.m_State & (State.Acquired | State.Static)) == 0)
                this.Free();
        }

        public void Release()
        {
            if ((this.m_State & State.Acquired) != 0)
                this.Free();
        }

        public byte[] Compile(bool compress, out int length)
        {
            if (this.m_CompiledBuffer == null)
            {
                if ((this.m_State & State.Accessed) == 0)
                {
                    this.m_State |= State.Accessed;
                }
                else
                {
                    if ((this.m_State & State.Warned) == 0)
                    {
                        this.m_State |= State.Warned;

                        try
                        {
                            using (StreamWriter op = new StreamWriter("net_opt.log", true))
                            {
                                op.WriteLine("Redundant compile for packet {0}, use Acquire() and Release()", this.GetType());
                                op.WriteLine(new System.Diagnostics.StackTrace());
                            }
                        }
                        catch
                        {
                        }
                    }

                    this.m_CompiledBuffer = new byte[0];
                    this.m_CompiledLength = 0;

                    length = this.m_CompiledLength;
                    return this.m_CompiledBuffer;
                }

                this.InternalCompile(compress);
            }

            length = this.m_CompiledLength;
            return this.m_CompiledBuffer;
        }

        private void Free()
        {
            if (this.m_CompiledBuffer == null)
                return;

            if ((this.m_State & State.Buffered) != 0)
                m_Buffers.ReleaseBuffer(this.m_CompiledBuffer);

            this.m_State &= ~(State.Static | State.Acquired | State.Buffered);

            this.m_CompiledBuffer = null;
        }

        private void InternalCompile(bool compress)
        {
            if (this.m_Length == 0)
            {
                long streamLen = this.m_Stream.Length;

                this.m_Stream.Seek(1, SeekOrigin.Begin);
                this.m_Stream.Write((ushort)streamLen);
            }
            else if (this.m_Stream.Length != this.m_Length)
            {
                int diff = (int)this.m_Stream.Length - this.m_Length;

                Console.WriteLine("Packet: 0x{0:X2}: Bad packet length! ({1}{2} bytes)", this.m_PacketID, diff >= 0 ? "+" : "", diff);
            }

            MemoryStream ms = this.m_Stream.UnderlyingStream;

            this.m_CompiledBuffer = ms.GetBuffer();
            int length = (int)ms.Length;

            if (compress)
            {
                this.m_CompiledBuffer = Compression.Compress(
                    this.m_CompiledBuffer, 0, length,
                    ref length);
			
                if (this.m_CompiledBuffer == null)
                {
                    Console.WriteLine("Warning: Compression buffer overflowed on packet 0x{0:X2} ('{1}') (length={2})", this.m_PacketID, this.GetType().Name, length);
                    using (StreamWriter op = new StreamWriter("compression_overflow.log", true))
                    {
                        op.WriteLine("{0} Warning: Compression buffer overflowed on packet 0x{1:X2} ('{2}') (length={3})", DateTime.Now, this.m_PacketID, this.GetType().Name, length);
                        op.WriteLine(new System.Diagnostics.StackTrace());
                    }
                }
            }

            if (this.m_CompiledBuffer != null)
            {
                this.m_CompiledLength = length;

                byte[] old = this.m_CompiledBuffer;

                if (length > BufferSize || (this.m_State & State.Static) != 0)
                {
                    this.m_CompiledBuffer = new byte[length];
                }
                else
                {
                    this.m_CompiledBuffer = m_Buffers.AcquireBuffer();
                    this.m_State |= State.Buffered;
                }

                Buffer.BlockCopy(old, 0, this.m_CompiledBuffer, 0, length);
            }

            PacketWriter.ReleaseInstance(this.m_Stream);
            this.m_Stream = null;
        }
    }
}