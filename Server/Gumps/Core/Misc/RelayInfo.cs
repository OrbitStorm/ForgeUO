using System.Linq;

namespace Server.Gumps
{
    public class TextRelay
    {
        private readonly int _EntryID;
        private readonly string _Text;
        public TextRelay(int entryID, string text)
        {
            this._EntryID = entryID;
            this._Text = text;
        }

        public int EntryID
        {
            get
            {
                return this._EntryID;
            }
        }
        public string Text
        {
            get
            {
                return this._Text;
            }
        }
    }

    public class RelayInfo
    {
        private readonly int _ButtonID;
        private readonly int[] _Switches;
        private readonly TextRelay[] _TextEntries;
        public RelayInfo(int buttonID, int[] switches, TextRelay[] textEntries)
        {
            this._ButtonID = buttonID;
            this._Switches = switches;
            this._TextEntries = textEntries;
        }

        public int ButtonID
        {
            get
            {
                return this._ButtonID;
            }
        }
        public int[] Switches
        {
            get
            {
                return this._Switches;
            }
        }
        public TextRelay[] TextEntries
        {
            get
            {
                return this._TextEntries;
            }
        }
        public bool IsSwitched(int switchID)
        {
            return this._Switches.Any(t => t == switchID);
        }

        public TextRelay GetTextEntry(int entryID)
        {
            return this._TextEntries.FirstOrDefault(t => t.EntryID == entryID);
        }
    }
}