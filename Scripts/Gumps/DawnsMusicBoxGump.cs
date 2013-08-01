using System;
using Server.Items;
using Server.Network;

namespace Server.Gumps
{
    public class DawnsMusicBoxGump : Gump
    {
        private readonly DawnsMusicBox m_Box;
        public DawnsMusicBoxGump(DawnsMusicBox box)
            : base(60, 36)
        {
            this.m_Box = box;

            this.AddPage(0);

            this.AddBackground(0, 0, 273, 324, 0x13BE);
            this.AddImageTiled(10, 10, 253, 20, 0xA40);
            this.AddImageTiled(10, 40, 253, 244, 0xA40);
            this.AddImageTiled(10, 294, 253, 20, 0xA40);
            this.AddAlphaRegion(10, 10, 253, 304);
            this.AddButton(10, 294, 0xFB1, 0xFB2, 0, GumpButtonType.Reply, 0);
            this.AddHtmlLocalized(45, 296, 450, 20, 1060051, 0x7FFF, false, false); // CANCEL
            this.AddHtmlLocalized(14, 12, 273, 20, 1075130, 0x7FFF, false, false); // Choose a track to play

            int page = 1;
            int i, y = 49;

            this.AddPage(page);

            for (i = 0; i < this.m_Box.Tracks.Count; i++, y += 24)
            {
                DawnsMusicInfo info = DawnsMusicBox.GetInfo(this.m_Box.Tracks[i]);

                if (i > 0 && i % 10 == 0)
                {
                    this.AddButton(228, 294, 0xFA5, 0xFA6, 0, GumpButtonType.Page, page + 1);

                    this.AddPage(page + 1);
                    y = 49;

                    this.AddButton(193, 294, 0xFAE, 0xFAF, 0, GumpButtonType.Page, page);

                    page++;
                }

                this.AddButton(19, y, 0x845, 0x846, 100 + i, GumpButtonType.Reply, 0);
                this.AddHtmlLocalized(44, y - 2, 213, 20, info.Name, 0x7FFF, false, false);
            }

            if (i % 10 == 0)
            {
                this.AddButton(228, 294, 0xFA5, 0xFA6, 0, GumpButtonType.Page, page + 1);

                this.AddPage(page + 1);
                y = 49;

                this.AddButton(193, 294, 0xFAE, 0xFAF, 0, GumpButtonType.Page, page);
            }

            this.AddButton(19, y, 0x845, 0x846, 1, GumpButtonType.Reply, 0);
            this.AddHtmlLocalized(44, y - 2, 213, 20, 1075207, 0x7FFF, false, false); // Stop Song
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (this.m_Box == null || this.m_Box.Deleted)
                return;

            Mobile m = sender.Mobile;

            if (!this.m_Box.IsChildOf(m.Backpack) && !this.m_Box.IsLockedDown)
                m.SendLocalizedMessage(1061856); // You must have the item in your backpack or locked down in order to use it.
            else if (this.m_Box.IsLockedDown && !this.m_Box.HasAccces(m))
                m.SendLocalizedMessage(502691); // You must be the owner to use this.
            else if (info.ButtonID == 1)
                this.m_Box.EndMusic(m);
            else if (info.ButtonID >= 100 && info.ButtonID - 100 < this.m_Box.Tracks.Count)
                this.m_Box.PlayMusic(m, this.m_Box.Tracks[info.ButtonID - 100]);
        }
    }
}