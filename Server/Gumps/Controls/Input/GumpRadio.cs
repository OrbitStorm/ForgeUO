using System;
using Server.Network;

namespace Server.Gumps
{
    public class GumpRadio : GumpEntry, IInputEntry
    {
        public event GumpResponse OnGumpResponse;

        private static readonly byte[] _LayoutName = Gump.StringToBuffer("radio");
        private GumpResponse _Callback;
        private int _ID1, _ID2;
        private bool _InitialState;
        private string _Name;
        private int _EntryID;
        private int _X, _Y;

        public GumpRadio(int x, int y, int inactiveID, int activeID, bool initialState, string name)
            : this(x, y, inactiveID, activeID, initialState, -1, null, name) { }

        public GumpRadio(int x, int y, int inactiveID, int activeID, bool initialState, GumpResponse callback, string name)
            : this(x, y, inactiveID, activeID, initialState, -1, callback, name) { }

        public GumpRadio(int x, int y, int inactiveID, int activeID, bool initialState, int switchID)
            : this(x, y, inactiveID, activeID, initialState, switchID, null, "") { }

        public GumpRadio(int x, int y, int inactiveID, int activeID, bool initialState, int switchID, GumpResponse callback)
            : this(x, y, inactiveID, activeID, initialState, switchID, callback, "") { }

        public GumpRadio(int x, int y, int inactiveID, int activeID, bool initialState, int switchID, GumpResponse callback, string name)
        {
            this._X = x;
            this._Y = y;
            this._ID1 = inactiveID;
            this._ID2 = activeID;
            this._InitialState = initialState;
            this._EntryID = switchID;
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

        public int InactiveID
        {
            get { return this._ID1; }
            set { this.Delta(ref this._ID1, value); }
        }

        public int ActiveID
        {
            get { return this._ID2; }
            set { this.Delta(ref this._ID2, value); }
        }

        public bool InitialState
        {
            get { return this._InitialState; }
            set { this.Delta(ref this._InitialState, value); }
        }

        public int EntryID
        {
            get { return this._EntryID; }
            set { this.Delta(ref this._EntryID, value); }
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
                Callback(this, InitialState);

            if (OnGumpResponse != null)
                OnGumpResponse(this, InitialState);
        }

        public override string Compile()
        {
            return String.Format("{{ radio {0} {1} {2} {3} {4} {5} }}", this._X, this._Y, this._ID1, this._ID2,
                                 this._InitialState ? 1 : 0, this._EntryID);
        }

        public override void AppendTo(IGumpWriter disp)
        {
            disp.AppendLayout(_LayoutName);
            disp.AppendLayout(this._X);
            disp.AppendLayout(this._Y);
            disp.AppendLayout(this._ID1);
            disp.AppendLayout(this._ID2);
            disp.AppendLayout(this._InitialState);
            disp.AppendLayout(this._EntryID);

            disp.Switches++;
        }
    }
}