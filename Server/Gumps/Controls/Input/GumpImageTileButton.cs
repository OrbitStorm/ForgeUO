using System;
using Server.Network;

namespace Server.Gumps
{
    public class GumpImageTileButton : GumpEntry, IInputEntry
    {
        public event GumpResponse OnGumpResponse;

        private static readonly byte[] _LayoutName = Gump.StringToBuffer("buttontileart");
        private static readonly byte[] _LayoutTooltip = Gump.StringToBuffer(" }{ tooltip");
        private int _EntryID;
        private GumpResponse _Callback;
        private int _Height;
        private int _Hue;
        private int _ID1, _ID2;
        private int _ItemID;
        private int _LocalizedTooltip;
        private string _Name;
        private int _Param;
        private GumpButtonType _Type;
        private int _Width;
        private int _X, _Y;

        public GumpImageTileButton(int x, int y, int normalID, int pressedID, GumpButtonType type, int param, int itemID, int hue, int width, int height, string name)
            : this(x, y, normalID, pressedID, -1, type, param, itemID, hue, width, height, null, -1, name) { }

        public GumpImageTileButton(int x, int y, int normalID, int pressedID, GumpButtonType type, int param, int itemID, int hue, int width, int height, GumpResponse callback, string name)
            : this(x, y, normalID, pressedID, -1, type, param, itemID, hue, width, height, callback, -1, name) { }

        public GumpImageTileButton(int x, int y, int normalID, int pressedID, GumpButtonType type, int param, int itemID, int hue, int width, int height, GumpResponse callback, int localizedTooltip, string name)
            : this(x, y, normalID, pressedID, -1, type, param, itemID, hue, width, height, callback, localizedTooltip, name) { }

        public GumpImageTileButton(int x, int y, int normalID, int pressedID, int buttonID, GumpButtonType type, int param, int itemID, int hue, int width, int height)
            : this(x, y, normalID, pressedID, buttonID, type, param, itemID, hue, width, height, null, -1, "") { }

        public GumpImageTileButton(int x, int y, int normalID, int pressedID, int buttonID, GumpButtonType type, int param, int itemID, int hue, int width, int height, GumpResponse callback)
            : this(x, y, normalID, pressedID, buttonID, type, param, itemID, hue, width, height, callback, -1, "") { }

        public GumpImageTileButton(int x, int y, int normalID, int pressedID, int buttonID, GumpButtonType type, int param, int itemID, int hue, int width, int height, GumpResponse callback, int localizedTooltip)
            : this(x, y, normalID, pressedID, buttonID, type, param, itemID, hue, width, height, callback, localizedTooltip, "") { }

        public GumpImageTileButton(int x, int y, int normalID, int pressedID, int buttonID, GumpButtonType type, int param, int itemID, int hue, int width, int height, GumpResponse callback, int localizedTooltip, string name)
        {
            this._X = x;
            this._Y = y;
            this._ID1 = normalID;
            this._ID2 = pressedID;
            this._EntryID = buttonID;
            this._Type = type;
            this._Param = param;

            this._ItemID = itemID;
            this._Hue = hue;
            this._Width = width;
            this._Height = height;

            this._LocalizedTooltip = localizedTooltip;

            this._Callback = callback;
            this._Name = (name != null ? name : "");
        }

        public override int X
        {
            get { return this._X; }
            set { this.Delta(ref this._X, value); }
        }

        public override int Y
        {
            get { return this._Y; }
            set { this.Delta(ref this._Y, value); }
        }

        public int NormalID
        {
            get { return this._ID1; }
            set { this.Delta(ref this._ID1, value); }
        }

        public int PressedID
        {
            get { return this._ID2; }
            set { this.Delta(ref this._ID2, value); }
        }

        public int EntryID
        {
            get { return this._EntryID; }
            set { this.Delta(ref this._EntryID, value); }
        }

        public GumpButtonType Type
        {
            get { return this._Type; }
            set
            {
                if (this._Type != value)
                {
                    this._Type = value;

                    IGumpContainer parent = this.Parent;

                    if (parent != null)
                        parent.Invalidate();
                }
            }
        }

        public int Param
        {
            get { return this._Param; }
            set { this.Delta(ref this._Param, value); }
        }

        public int ItemID
        {
            get { return this._ItemID; }
            set { this.Delta(ref this._ItemID, value); }
        }

        public int Hue
        {
            get { return this._Hue; }
            set { this.Delta(ref this._Hue, value); }
        }

        public int Width
        {
            get { return this._Width; }
            set { this.Delta(ref this._Width, value); }
        }

        public int Height
        {
            get { return this._Height; }
            set { this.Delta(ref this._Height, value); }
        }

        public int LocalizedTooltip
        {
            get { return this._LocalizedTooltip; }
            set { this.Delta(ref this._LocalizedTooltip, value); }
        }

        public string Name
        {
            get { return this._Name; }
            set { this.Delta(ref this._Name, value); }
        }

        public GumpResponse Callback
        {
            get { return this._Callback; }
            set { this.Delta(ref this._Callback, value); }
        }

        public void Invoke()
        {
            if (Callback != null)
                Callback(this, null);

            if (OnGumpResponse != null)
                OnGumpResponse(this, null);
        }

        public override string Compile()
        {
            return this._LocalizedTooltip > 0
                       ? String.Format(
                           "{{ buttontileart {0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} }}{{ tooltip {11} }}",
                           this._X, this._Y, this._ID1, this._ID2, (int) this._Type, this._Param, this._EntryID,
                           this._ItemID, this._Hue, this._Width, this._Height, this._LocalizedTooltip)
                       : String.Format("{{ buttontileart {0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} }}", this._X,
                                       this._Y, this._ID1, this._ID2, (int) this._Type, this._Param,
                                       this._EntryID, this._ItemID, this._Hue, this._Width, this._Height);
        }

        public override void AppendTo(IGumpWriter disp)
        {
            disp.AppendLayout(_LayoutName);
            disp.AppendLayout(this._X);
            disp.AppendLayout(this._Y);
            disp.AppendLayout(this._ID1);
            disp.AppendLayout(this._ID2);
            disp.AppendLayout((int) this._Type);
            disp.AppendLayout(this._Param);
            disp.AppendLayout(this._EntryID);

            disp.AppendLayout(this._ItemID);
            disp.AppendLayout(this._Hue);
            disp.AppendLayout(this._Width);
            disp.AppendLayout(this._Height);

            if (this._LocalizedTooltip > 0)
            {
                disp.AppendLayout(_LayoutTooltip);
                disp.AppendLayout(this._LocalizedTooltip);
            }
        }
    }
}