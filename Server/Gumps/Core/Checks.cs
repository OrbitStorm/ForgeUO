namespace Server.Gumps
{
    public partial class Gump
    {
        public void AddCheck(int x, int y, int inactiveID, int activeID, bool initialState, int switchID,
                             string name = "")
        {
            this.Add(new GumpCheck(x, y, inactiveID, activeID, initialState, switchID, null, name));
        }

        public void AddCheck(int x, int y, int inactiveID, int activeID, bool initialState, int switchID,
                             GumpResponse callback, string name = "")
        {
            this.Add(new GumpCheck(x, y, inactiveID, activeID, initialState, switchID, callback, name));
        }

        public void AddCheck(int x, int y, int inactiveID, int activeID, bool initialState, GumpResponse callback,
                             string name = "")
        {
            this.Add(new GumpCheck(x, y, inactiveID, activeID, initialState, this.NewID(), callback, name));
        }
    }
}
