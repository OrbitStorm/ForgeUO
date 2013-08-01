using System;
using Server.Network;

namespace Server.Gumps
{
    public enum GumpButtonType
    {
        Page = 0,
        Reply = 1
    }

    public class GumpButton : GumpEntry, IInputEntry
    {
        public event GumpResponse OnGumpResponse;

        private static readonly byte[] _LayoutName = Gump.StringToBuffer("button");
        private int _EntryID;
        private GumpResponse _Callback;
        private int _ID1, _ID2;
        private string _Name;
        private int _Param;
        private GumpButtonType _Type;
        private int _X, _Y;

        public GumpButton(int x, int y, int normalID, int pressedID, GumpButtonType type, int param, string name)
            : this(x, y, normalID, pressedID, -1, type, param, null, name) { }

        public GumpButton(int x, int y, int normalID, int pressedID, GumpButtonType type, int param, GumpResponse callback, string name)
            : this(x, y, normalID, pressedID, -1, type, param, callback, name) { }

        public GumpButton(int x, int y, int normalID, int pressedID, int buttonID, GumpButtonType type, int param)
            : this(x, y, normalID, pressedID, buttonID, type, param, null, "") { }

        public GumpButton(int x, int y, int normalID, int pressedID, int buttonID, GumpButtonType type, int param, GumpResponse callback)
            : this(x, y, normalID, pressedID, buttonID, type, param, callback, "") { }

        public GumpButton(int x, int y, int normalID, int pressedID, int buttonID, GumpButtonType type, int param, GumpResponse callback, string name)
        {
            this._X = x;
            this._Y = y;
            this._ID1 = normalID;
            this._ID2 = pressedID;
            this._EntryID = buttonID;
            this._Type = type;
            this._Param = param;
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
                if (this._Type == value) return;

                this._Type = value;
                IGumpContainer parent = this.Parent;

                if (parent != null)
                    parent.Invalidate();
            }
        }

        public int Param
        {
            get { return this._Param; }
            set { this.Delta(ref this._Param, value); }
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
            return String.Format("{{ button {0} {1} {2} {3} {4} {5} {6} }}", this._X, this._Y, this._ID1, this._ID2,
                                 (int)this._Type, this._Param, this._EntryID);
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
        }
    }
}