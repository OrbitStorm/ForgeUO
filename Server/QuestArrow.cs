using System;
using Server.Network;

namespace Server
{
    public class QuestArrow
    {
        private readonly Mobile m_Mobile;
        private readonly Mobile m_Target;
        private bool m_Running;
        public QuestArrow(Mobile m, Mobile t)
        {
            this.m_Running = true;
            this.m_Mobile = m;
            this.m_Target = t;
        }

        public QuestArrow(Mobile m, Mobile t, int x, int y)
            : this(m, t)
        {
            this.Update(x, y);
        }

        public Mobile Mobile
        {
            get
            {
                return this.m_Mobile;
            }
        }
        public Mobile Target
        {
            get
            {
                return this.m_Target;
            }
        }
        public bool Running
        {
            get
            {
                return this.m_Running;
            }
        }
        public void Update()
        {
            this.Update(this.m_Target.X, this.m_Target.Y);
        }

        public void Update(int x, int y)
        {
            if (!this.m_Running)
                return;

            NetState ns = this.m_Mobile.NetState;

            if (ns == null)
                return;

            if (ns.HighSeas)
                ns.Send(new SetArrowHS(x, y, this.m_Target.Serial));
            else
                ns.Send(new SetArrow(x, y));
        }

        public void Stop()
        {
            this.Stop(this.m_Target.X, this.m_Target.Y);
        }

        public void Stop(int x, int y)
        {
            if (!this.m_Running)
                return;

            this.m_Mobile.ClearQuestArrow();

            NetState ns = this.m_Mobile.NetState;

            if (ns != null)
            {
                if (ns.HighSeas)
                    ns.Send(new CancelArrowHS(x, y, this.m_Target.Serial));
                else
                    ns.Send(new CancelArrow());
            }

            this.m_Running = false;
            this.OnStop();
        }

        public virtual void OnStop()
        {
        }

        public virtual void OnClick(bool rightClick)
        {
        }
    }
}