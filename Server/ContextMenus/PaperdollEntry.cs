using System;

namespace Server.ContextMenus
{
    public class PaperdollEntry : ContextMenuEntry
    {
        private readonly Mobile m_Mobile;
        public PaperdollEntry(Mobile m)
            : base(6123, 18)
        {
            this.m_Mobile = m;
        }

        public override void OnClick()
        {
            if (this.m_Mobile.CanPaperdollBeOpenedBy(this.Owner.From))
                this.m_Mobile.DisplayPaperdollTo(this.Owner.From);
        }
    }
}