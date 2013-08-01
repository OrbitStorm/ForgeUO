using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.Network;

namespace Server.Gumps
{
    public delegate void GumpResponse(IGumpComponent sender, object param);

    public interface IInputEntry
    {
        int EntryID { get; set; }
        string Name { get; set; }
        GumpResponse Callback { get; set; }
        void Invoke();
    }

    public interface IGumpContainer
    {
        void Add(IGumpComponent g);
        void Remove(IGumpComponent g);
        Gump RootParent { get; }
        void Invalidate();
    }

    public interface IGumpComponent
    {
        IGumpContainer Parent { get; set; }
        Int32 X { get; set; }
        Int32 Y { get; set; }
    }

    public partial class Gump : IGumpContainer
    {
        private static readonly byte[] _BeginLayout = StringToBuffer("{ ");
        private static readonly byte[] _EndLayout = StringToBuffer(" }");
        private static readonly byte[] _NoMove = StringToBuffer("{ nomove }");
        private static readonly byte[] _NoClose = StringToBuffer("{ noclose }");
        private static readonly byte[] _NoDispose = StringToBuffer("{ nodispose }");
        private static readonly byte[] _NoResize = StringToBuffer("{ noresize }");
        private static int _NextSerial = 1;
        private readonly List<GumpEntry> _Entries;
        private readonly List<string> _Strings;
        private readonly int _TypeID;
        private List<int> _UsedIDs = new List<int>();
        private List<Mobile> _Users = new List<Mobile>();
        private List<Mobile> _Viewers = new List<Mobile>();
        private Mobile _Address;
        private bool _Closable = true;
        private bool _Disposable = true;
        private bool _Dragable = true;
        //private Mobile _Hijacker;
        private bool _MacroProtection;
        private int _NewID = 1;
        private bool _Resizable = true;
        private int _Serial;
        private bool _SharedGump;
        internal int _Switches;
        internal int _TextEntries;
        private int _X, _Y;

        public Gump(int x, int y)
        {
            do
            {
                this._Serial = _NextSerial++;
            } while (this._Serial == 0);

            this._X = x;
            this._Y = y;

            this._TypeID = GetTypeID(this.GetType());

            this._Entries = new List<GumpEntry>();
            this._Strings = new List<string>();
        }

        public Gump(int x, int y, List<Mobile> users)
            : this(x, y)
        {
            if (users == null) return;

            this._Users = users;
            this._SharedGump = true;
        }

        public int TypeID
        {
            get { return this._TypeID; }
        }

        protected internal List<GumpEntry> Entries
        {
            get { return this._Entries; }
        }

        public int Serial
        {
            get { return this._Serial; }
            set
            {
                if (this._Serial != value)
                {
                    this._Serial = value;
                    this.Invalidate();
                }
            }
        }

        public int X
        {
            get { return this._X; }
            set
            {
                if (this._X == value) return;

                this._X = value;
                this.Invalidate();
            }
        }

        public int Y
        {
            get { return this._Y; }
            set
            {
                if (this._Y == value) return;

                this._Y = value;
                this.Invalidate();
            }
        }

        public Mobile Address
        {
            get { return this._Address; }
            set
            {
                if (this._Address == value) return;

                this._Address = value;
                this.Invalidate();
                this.OnAddressChange();
            }
        }

        public bool Disposable
        {
            get { return this._Disposable; }
            set
            {
                if (this._Disposable == value) return;

                this._Disposable = value;
                this.Invalidate();
            }
        }

        public bool Resizable
        {
            get { return this._Resizable; }
            set
            {
                if (this._Resizable == value) return;

                this._Resizable = value;
                this.Invalidate();
            }
        }

        public bool MacroProtection
        {
            get { return this._MacroProtection; }
            set
            {
                if (this._MacroProtection == value) return;

                this._MacroProtection = value;
                this.Invalidate();
            }
        }

        public bool SharedGump
        {
            get { return this._SharedGump; }
            set
            {
                if (this._SharedGump == value) return;

                this._SharedGump = value;
                this.Invalidate();
            }
        }

        public bool Dragable
        {
            get { return this._Dragable; }
            set
            {
                if (this._Dragable == value) return;

                this._Dragable = value;
                this.Invalidate();
            }
        }

        public bool Closable
        {
            get { return this._Closable; }
            set
            {
                if (this._Closable == value) return;

                this._Closable = value;
                this.Invalidate();
            }
        }

        public static int GetTypeID(Type type)
        {
            return type.FullName.GetHashCode();
        }

        public static byte[] StringToBuffer(string str)
        {
            return Encoding.ASCII.GetBytes(str);
        }

        public Gump RootParent { get { return this; } }

        public virtual void Invalidate()
        {
        }

        public virtual void OnAddressChange()
        {
        }

        public bool AddUser(Mobile from)
        {
            this.Invalidate();

            if (this._Users == null)
                this._Users = new List<Mobile>();

            if (this._Users.Contains(from))
                return false;

            this._Users.Add(from);

            this._SharedGump = true;

            return true;
        }

        public void AddPage(int page)
        {
            this.Add(new GumpPage(page));
        }

        public void AddAlphaRegion(int x, int y, int width, int height)
        {
            this.Add(new GumpAlphaRegion(x, y, width, height));
        }

        public void AddBackground(int x, int y, int width, int height, int gumpID)
        {
            this.Add(new GumpBackground(x, y, width, height, gumpID));
        }

        public void AddGroup(int group)
        {
            this.Add(new GumpGroup(group));
        }

        public void AddTooltip(int number)
        {
            this.Add(new GumpTooltip(number));
        }

        public void AddHtml(int x, int y, int width, int height, string text, bool background, bool scrollbar)
        {
            this.Add(new GumpHtml(x, y, width, height, text, background, scrollbar));
        }

        public void AddHtmlLocalized(int x, int y, int width, int height, int number, bool background, bool scrollbar)
        {
            this.Add(new GumpHtmlLocalized(x, y, width, height, number, background, scrollbar));
        }

        public void AddHtmlLocalized(int x, int y, int width, int height, int number, int color, bool background,
                                     bool scrollbar)
        {
            this.Add(new GumpHtmlLocalized(x, y, width, height, number, color, background, scrollbar));
        }

        public void AddHtmlLocalized(int x, int y, int width, int height, int number, string args, int color,
                                     bool background, bool scrollbar)
        {
            this.Add(new GumpHtmlLocalized(x, y, width, height, number, args, color, background, scrollbar));
        }

        public void AddImage(int x, int y, int gumpID)
        {
            this.Add(new GumpImage(x, y, gumpID));
        }

        public void AddImage(int x, int y, int gumpID, int hue)
        {
            this.Add(new GumpImage(x, y, gumpID, hue));
        }

        public void AddImageTiled(int x, int y, int width, int height, int gumpID)
        {
            this.Add(new GumpImageTiled(x, y, width, height, gumpID));
        }

        public void AddItem(int x, int y, int itemID)
        {
            this.Add(new GumpItem(x, y, itemID));
        }

        public void AddItem(int x, int y, int itemID, int hue)
        {
            this.Add(new GumpItem(x, y, itemID, hue));
        }

        public void AddLabel(int x, int y, int hue, string text)
        {
            this.Add(new GumpLabel(x, y, hue, text));
        }

        public void AddLabelCropped(int x, int y, int width, int height, int hue, string text)
        {
            this.Add(new GumpLabelCropped(x, y, width, height, hue, text));
        }

        public void Add(IGumpComponent g)
        {
            if (g.Parent == null)
                g.Parent = this;

            if (g is GumpEntry)
            {
                if (!this._Entries.Contains((GumpEntry)g))
                {
                    ((GumpEntry)g).AssignID();
                    this._Entries.Add((GumpEntry)g);
                    this.Invalidate();
                }
            }
            else if (g is Gumpling)
            {
                ((Gumpling)g).AddToGump(this);
            }
        }

        public void Remove(IGumpComponent g)
        {
            if (g is GumpEntry)
            {
                this._Entries.Remove((GumpEntry)g);
                g.Parent = null;
                this.Invalidate();
            }
            else if (g is Gumpling)
            {
                ((Gumpling)g).RemoveFromGump(this);
                this.Invalidate();
            }
        }

        protected internal int NewID()
        {
            int id;

            if (this._MacroProtection)
            {
                id = Utility.RandomMinMax(1, 65535);

                if (this._UsedIDs.Contains(id))
                    return this.NewID();
            }
            else
            {
                id = this._NewID;

                if (this._UsedIDs.Contains(id))
                {
                    this._NewID++;
                    return this.NewID();
                }
            }

            _UsedIDs.Add(id);

            return id;
        }

        public int Intern(string value)
        {
            int indexOf = this._Strings.IndexOf(value);

            if (indexOf >= 0)
            {
                return indexOf;
            }

            this.Invalidate();
            this._Strings.Add(value);
            return this._Strings.Count - 1;
        }

        public void SendTo(NetState state)
        {
            this.Address = state.Mobile;

            if (!this._SharedGump)
            {
                state.AddGump(this);
                state.Send(this.Compile(state));
            }
            else
            {
                foreach (Mobile m in _Users)
                {
                    m.NetState.AddGump(this);
                    m.NetState.Send(this.Compile(state));
                }

                foreach (Mobile m in _Viewers)
                {
                    m.NetState.AddGump(this);
                    m.NetState.Send(this.Compile(state, true));
                }
            }
        }

        public virtual void OnResponse(NetState sender, RelayInfo info)
        {
            int buttonID = info.ButtonID;

            foreach (GumpCheck entry in this._Entries.OfType<GumpCheck>())
            {
                entry.InitialState = info.IsSwitched(entry.EntryID);
                entry.Invoke();
            }

            foreach (GumpRadio entry in this.Entries.OfType<GumpRadio>())
            {
                entry.InitialState = info.IsSwitched(entry.EntryID);
                entry.Invoke();
            }
            
            foreach (GumpTextEntry entry in this._Entries.OfType<GumpTextEntry>())
            {
                entry.InitialText = info.GetTextEntry(entry.EntryID).Text;
                entry.Invoke();
            }
            
            foreach (GumpTextEntryLimited entry in this._Entries.OfType<GumpTextEntryLimited>())
            {
                entry.InitialText = info.GetTextEntry(entry.EntryID).Text;
                entry.Invoke();
            }

            foreach (
                GumpImageTileButton button in
                    this._Entries.OfType<GumpImageTileButton>().Where(button => button.EntryID == buttonID))
            {
                button.Invoke();
            }

            foreach (
                GumpButton button in this._Entries.OfType<GumpButton>().Where(button => button.EntryID == buttonID))
            {
                button.Invoke();
            }
        }

        public virtual void OnServerClose(NetState owner)
        {
        }

        protected Packet Compile()
        {
            return this.Compile(null);
        }

        protected Packet Compile(NetState ns, bool convertToViewer = false)
        {
            IGumpWriter disp;

            if (ns != null && ns.Unpack)
                disp = new DisplayGumpPacked(this);
            else
                disp = new DisplayGumpFast(this);

            if (!this._Dragable)
                disp.AppendLayout(_NoMove);

            if (!this._Closable)
                disp.AppendLayout(_NoClose);

            if (!this._Disposable)
                disp.AppendLayout(_NoDispose);

            if (!this._Resizable)
                disp.AppendLayout(_NoResize);

            int count = this._Entries.Count;

            for (int i = 0; i < count; ++i)
            {
                GumpEntry e = this._Entries[i];

                disp.AppendLayout(_BeginLayout);

                if (!convertToViewer)
                    e.AppendTo(disp);
                else
                {
                    GumpButton button = e as GumpButton;

                    if (button != null)
                        new GumpImage(button.X, button.Y, button.NormalID).AppendTo(disp);
                    else
                    {
                        GumpImageTileButton tileButton = e as GumpImageTileButton;

                        if (tileButton != null)
                            new GumpImageTiled(tileButton.X, tileButton.Y, tileButton.Width, tileButton.Height,
                                               tileButton.NormalID).AppendTo(disp);
                        else
                        {
                            GumpRadio radio = e as GumpRadio;

                            if (radio != null)
                                new GumpImage(radio.X, radio.Y, radio.InitialState ? radio.ActiveID : radio.InactiveID)
                                    .AppendTo(disp);
                            else
                            {
                                GumpCheck check = e as GumpCheck;

                                if (check != null)
                                    new GumpImage(check.X, check.Y,
                                                  check.InitialState ? check.ActiveID : check.InactiveID).AppendTo(disp);
                                // Process text fields
                            }
                        }
                    }
                }

                disp.AppendLayout(_EndLayout);
            }

            disp.WriteStrings(this._Strings);

            disp.Flush();

            this._TextEntries = disp.TextEntries;
            this._Switches = disp.Switches;

            return disp as Packet;
        }
    }
}