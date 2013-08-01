using System;

namespace Server.Commands
{
    public class TimeStamp
    {
        public static void Initialize()
        {
            CommandSystem.Register("TimeStamp", AccessLevel.Player, new CommandEventHandler(CheckTime_OnCommand));
        }

        [Usage("TimeStamp")]
        [Description("Check's Your Servers Current Date And Time")]
        public static void CheckTime_OnCommand(CommandEventArgs e)
        {
            Mobile m = e.Mobile;
            DateTime now = DateTime.Now;
            m.SendMessage("The Current Date And Time Is " + now + "(EST)");         
        }
    }
}