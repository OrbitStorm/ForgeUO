using System.Linq;

namespace Server.Gumps
{
    public partial class Gump
    {
        public void AddTextEntry(int x, int y, int width, int height, int hue, int entryID, string initialText = "")
        {
            this.Add(new GumpTextEntry(x, y, width, height, hue, entryID, initialText, null, ""));
        }

        public void AddTextEntry(int x, int y, int width, int height, int hue, int entryID, GumpResponse callback,
                                 string initialText = "", string name = "")
        {
            this.Add(new GumpTextEntry(x, y, width, height, hue, entryID, initialText, callback, name));
        }

        public void AddTextEntry(int x, int y, int width, int height, int hue, GumpResponse callback,
                                 string initialText = "", string name = "")
        {
            this.Add(new GumpTextEntry(x, y, width, height, hue, this.NewID(), initialText, callback, name));
        }

        public void AddTextEntry(int x, int y, int width, int height, int hue, int entryID, string initialText, int size,
                                 string name = "")
        {
            this.Add(new GumpTextEntryLimited(x, y, width, height, hue, entryID, initialText, size, null, name));
        }

        public void AddTextEntry(int x, int y, int width, int height, int hue, int entryID, string initialText, int size,
                                 GumpResponse callback, string name = "")
        {
            this.Add(new GumpTextEntryLimited(x, y, width, height, hue, entryID, initialText, size, callback, name));
        }

        public void AddTextEntry(int x, int y, int width, int height, int hue, string initialText, int size,
                                 GumpResponse callback, string name = "")
        {
            this.Add(new GumpTextEntryLimited(x, y, width, height, hue, this.NewID(), initialText, size, callback, name));
        }

        public string GetTextEntry(int id)
        {
            foreach (GumpTextEntry entry in this._Entries.OfType<GumpTextEntry>().Where(entry => entry.EntryID == id))
            {
                return entry.InitialText;
            }

            return this._Entries.OfType<GumpTextEntryLimited>().Where(entry => entry.EntryID == id).Select(entry => entry.InitialText).FirstOrDefault();
        }

        public string GetTextEntry(string name)
        {
            foreach (GumpTextEntry entry in this._Entries.OfType<GumpTextEntry>().Where(entry => entry.Name == name))
            {
                return entry.InitialText;
            }

            return this._Entries.OfType<GumpTextEntryLimited>().Where(entry => entry.Name == name).Select(entry => entry.InitialText).FirstOrDefault();
        }
    }
}
