#region Header
// **********
// Item Name Dye Tool
// ¤ Designed to hue a targeted item's name, ONCE.
// ¤ Can be converted to unlimited uses by removing lines 116 & 145.
// ¤ Feel free to customize messages/colors to your preference.
// **********
// Author:  Orbit Storm
// Created: 3/17/2010
// Revamp:  7/20/2012
// Version: v1.2
// **********
#endregion

#region References  
using System;
using System.Text;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Multis;
using Server.Network;
using Server.Targeting;
using Server.ContextMenus;
using Server.Gumps;
#endregion

namespace Server.TreasureSystem
{
    #region Cliloc_Reader
    public class StringEntry
    {
        private int m_Number;
        private string m_Text;

        public int Number { get { return m_Number; } }
        public string Text { get { return m_Text; } }

        public StringEntry(int number, string text)
        {
            m_Number = number;
            m_Text = text;
        }
    }
    public class StringList
    {
        private Hashtable m_Table;
        private StringEntry[] m_Entries;

        public StringEntry[] Entries { get { return m_Entries; } }
        public Hashtable Table { get { return m_Table; } }

        private static byte[] m_Buffer = new byte[1024];

        public StringList()
        {
            m_Table = new Hashtable();

            string path = "./Cliloc.enu";

            if (path == null)
            {
                m_Entries = new StringEntry[0];
                    return;
            }

            ArrayList list = new ArrayList();

            using (BinaryReader bin = new BinaryReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                bin.ReadInt32();
                bin.ReadInt16();

                while (bin.BaseStream.Length != bin.BaseStream.Position)
                {
                    int number = bin.ReadInt32();
                    bin.ReadByte();
                    int length = bin.ReadInt16();

                    if (length > m_Buffer.Length)
                        m_Buffer = new byte[(length + 1023) & ~1023];

                    bin.Read(m_Buffer, 0, length);
                    string text = Encoding.UTF8.GetString(m_Buffer, 0, length);

                    list.Add(new StringEntry(number, text));
                    m_Table[number] = text;
                }
            }

            m_Entries = (StringEntry[])list.ToArray(typeof(StringEntry));
        }
    }
    #endregion

    #region NameHueGump
    public class NameHueGump : Gump
    {
        #region InternalTarget
        private class InternalTarget : Target 
        {
            private NameDyeTool m_Tool;

            public InternalTarget(NameDyeTool tool)
                : base(1, false, TargetFlags.None)
            {
                m_Tool = tool;
            }
            protected override void OnTarget(Mobile from, object targeted)
            {
                Item a = from.Backpack.FindItemByType( typeof( NameDyeTool ) );

                if (!(targeted is Item || (!(targeted is BaseOuterTorso))))
                {
                    from.SendMessage("You may only target items!");
                    return;
                }
                
                if (targeted != null)
                {
                    if (targeted is Item)
                    {
                        Item item = (Item)targeted;

                        if (item.IsChildOf(from.Backpack))
                        {
                            if (item.Name != null)
                            {
                                if (item.Name.Substring(0, 1) == "<")
                                {
                                    from.SendMessage(38, "You have already colored this item's name!");
                                }
                                else
                                {
                                    string name = item.Name.Substring(0, 1).ToUpper() + item.Name.Substring(1, item.Name.Length - 1);
                                    item.Name = string.Format("<basefont color=#{0}>{1}", m_Tool.m_Hue, name);
                                    a.Delete();
                                }
                            }
                            else
                            {
                                StringList sl=new StringList();
                                string key = sl.Table[item.LabelNumber] as string;
                                string name = key.Substring(0, 1).ToUpper() + key.Substring(1, key.Length-1);
                                item.Name = string.Format("<basefont color=#{0}>{1}", m_Tool.m_Hue, name);   
                                a.Delete();
                            }
                        }
                        else
                        {
                            from.SendMessage("The tool must be in your backpack to use it!");
                        }
                    }
                }
            }
        }
        #endregion

        Mobile caller;
        NameDyeTool m_Toole;

        public NameHueGump(Mobile from,NameDyeTool fromToole)
            : this()
        {
            caller = from;
            m_Toole = fromToole;
        }

        public NameHueGump()
            : base(100, 100)
        {
            this.Closable = false;
            this.Disposable = true;
            this.Dragable = true;
            this.Resizable = false;

            AddPage(0);
            AddBackground(0, 0, 297, 306, 2600);
            AddLabel(60, 21, 2116, @"Select a hue!");
            AddButton(115, 254, 247, 248, 0, GumpButtonType.Reply, 0);
            AddRadio(50, 77, 208, 209, false, 1);
            AddLabel(73, 77, 312, @"7329AD");
            AddRadio(50, 102, 208, 209, false, 2);
            AddLabel(73, 102, 2, @"0000CE");
            AddRadio(50, 127, 208, 209, false, 3);
            AddLabel(73, 127, 5, @"7B7BD6");
            AddRadio(50, 152, 208, 209, false, 4);
            AddLabel(73, 152, 31, @"A50029");
            AddRadio(50, 177, 208, 209, false, 5);
            AddLabel(73, 177, 37, @"CE1800");
            AddRadio(50, 202, 208, 209, false, 6);
            AddLabel(73, 202, 48, @"CE9C29");
            AddRadio(50, 227, 208, 209, false, 7);
            AddLabel(73, 227, 2116, @"CE4229");
            AddRadio(148, 77, 208, 209, false, 8);
            AddLabel(171, 77, 56, @"73A500");
            AddRadio(148, 102, 208, 209, false, 9);
            AddLabel(171, 102, 62, @"52CE00");
            AddRadio(148, 127, 208, 209, false, 10);
            AddLabel(171, 127, 84, @"52D6B5");
            AddRadio(148, 152, 208, 209, false, 11);
            AddLabel(171, 152, 116, @"8C089C");
            AddRadio(148, 177, 208, 209, false, 12);
            AddLabel(171, 177, 762, @"638452");
            AddRadio(148, 202, 208, 209, false, 13);
            AddLabel(171, 202, 1208, @"9C2931");
            AddRadio(148, 227, 208, 209, false, 14);
            AddLabel(171, 227, 600, @"949CBD");
  
            AddLabel(39, 40, 2116, @"You can only dye an item's name once!");
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;
            int id = 0;
            switch (info.ButtonID)
            {
                case 0:
                {
                    for( int i = 0; i < info.Switches.Length; i++ )
                    {

                        id = info.Switches[i];
                        switch (id)
                        {
                            case 1:
                                m_Toole.m_Hue = "7329AD";
                                break;
                            case 2:
                                m_Toole.m_Hue = "0000CE";
                                break;
                            case 3:
                                m_Toole.m_Hue = "7B7BD6";
                                break;
                            case 4:
                                m_Toole.m_Hue = "A50029";
                                break;
                            case 5:
                                m_Toole.m_Hue = "CE1800";
                                break;
                            case 6:
                                m_Toole.m_Hue = "CE9C29";
                                break;
                            case 7:
                                m_Toole.m_Hue = "CE4229";
                                break;
                            case 8:
                                m_Toole.m_Hue = "73A500";
                                break;
                            case 9:
                                m_Toole.m_Hue = "52CE00";
                                break;
                            case 10:
                                m_Toole.m_Hue = "52D6B5";
                                break;
                            case 11:
                                m_Toole.m_Hue = "8C089C";
                                break;
                            case 12:
                                m_Toole.m_Hue = "638452";
                                break;
                            case 13:
                                m_Toole.m_Hue = "9C2931";
                                break;
                            case 14:
                                m_Toole.m_Hue = "949CBD";
                                break;
                            default:
                                m_Toole.m_Hue = string.Empty;
                                break;
                        }
                    }

                    if (m_Toole.m_Hue != string.Empty)
                        from.Target = new InternalTarget(m_Toole);
                    else
                            from.SendMessage("You need to select a color!");
                        break;
                }
            }
        }
    }
    #endregion

    public class NameDyeTool : Item, ISecurable
    {
        public string m_Hue;
        private SecureLevel m_SecureLevel;
        
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
            writer.Write((string)m_Hue);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                {
                    m_Hue = reader.ReadString();
                    break;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public SecureLevel Level
        {
            get
            {
                return m_SecureLevel;
            }
            set
            {
                m_SecureLevel = value;
            }
        }

        [Constructable]
        public NameDyeTool()
            : base(0x2D61)
        {
            Weight = 2.0;
			Name = "Item Name Dye Tool";
			Hue = 2500;
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);
            SetSecureLevelEntry.AddTo(from, this, list);
        }

        public NameDyeTool(Serial serial)
            : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.InRange(this.GetWorldLocation(), 1))
            {
                from.SendMessage(39, "Target an item to change its name color.");
                if (from.HasGump(typeof(NameHueGump )))
                    from.CloseGump(typeof(NameHueGump ));
                from.SendGump(new NameHueGump (from,this));
                
            }
            else
            {
                from.SendLocalizedMessage(500446); // That is too far away.
            }
        }      
    }
}