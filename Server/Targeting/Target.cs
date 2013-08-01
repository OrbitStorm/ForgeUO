/***************************************************************************
*                                Target.cs
*                            -------------------
*   begin                : May 1, 2002
*   copyright            : (C) The RunUO Software Team
*   email                : info@runuo.com
*
*   $Id: Target.cs 644 2010-12-23 09:18:45Z asayre $
*
***************************************************************************/








/***************************************************************************
*
*   This program is free software; you can redistribute it and/or modify
*   it under the terms of the GNU General Public License as published by
*   the Free Software Foundation; either version 2 of the License, or
*   (at your option) any later version.
*
***************************************************************************/
using System;
using Server.Network;

namespace Server.Targeting
{
    public abstract class Target
    {
        private static int m_NextTargetID;
        private readonly int m_TargetID;
        private int m_Range;
        private bool m_AllowGround;
        private bool m_CheckLOS;
        private bool m_AllowNonlocal;
        private bool m_DisallowMultis;
        private TargetFlags m_Flags;
        private DateTime m_TimeoutTime;
        private Timer m_TimeoutTimer;
        protected Target(int range, bool allowGround, TargetFlags flags)
        {
            this.m_TargetID = ++m_NextTargetID;
            this.m_Range = range;
            this.m_AllowGround = allowGround;
            this.m_Flags = flags;

            this.m_CheckLOS = true;
        }

        public DateTime TimeoutTime
        {
            get
            {
                return this.m_TimeoutTime;
            }
        }
        public bool CheckLOS
        {
            get
            {
                return this.m_CheckLOS;
            }
            set
            {
                this.m_CheckLOS = value;
            }
        }
        public bool DisallowMultis
        {
            get
            {
                return this.m_DisallowMultis;
            }
            set
            {
                this.m_DisallowMultis = value;
            }
        }
        public bool AllowNonlocal
        {
            get
            {
                return this.m_AllowNonlocal;
            }
            set
            {
                this.m_AllowNonlocal = value;
            }
        }
        public int TargetID
        {
            get
            {
                return this.m_TargetID;
            }
        }
        public int Range
        {
            get
            {
                return this.m_Range;
            }
            set
            {
                this.m_Range = value;
            }
        }
        public bool AllowGround
        {
            get
            {
                return this.m_AllowGround;
            }
            set
            {
                this.m_AllowGround = value;
            }
        }
        public TargetFlags Flags
        {
            get
            {
                return this.m_Flags;
            }
            set
            {
                this.m_Flags = value;
            }
        }
        public static void Cancel(Mobile m)
        {
            NetState ns = m.NetState;

            if (ns != null)
                ns.Send(CancelTarget.Instance);

            Target targ = m.Target;

            if (targ != null)
                targ.OnTargetCancel(m, TargetCancelType.Canceled);
        }

        public void BeginTimeout(Mobile from, TimeSpan delay)
        {
            this.m_TimeoutTime = DateTime.Now + delay;

            if (this.m_TimeoutTimer != null)
                this.m_TimeoutTimer.Stop();

            this.m_TimeoutTimer = new TimeoutTimer(this, from, delay);
            this.m_TimeoutTimer.Start();
        }

        public void CancelTimeout()
        {
            if (this.m_TimeoutTimer != null)
                this.m_TimeoutTimer.Stop();

            this.m_TimeoutTimer = null;
        }

        public void Timeout(Mobile from)
        {
            this.CancelTimeout();
            from.ClearTarget();

            Cancel(from);

            this.OnTargetCancel(from, TargetCancelType.Timeout);
            this.OnTargetFinish(from);
        }

        public virtual Packet GetPacketFor(NetState ns)
        {
            return new TargetReq(this);
        }

        public void Cancel(Mobile from, TargetCancelType type)
        {
            this.CancelTimeout();
            from.ClearTarget();

            this.OnTargetCancel(from, type);
            this.OnTargetFinish(from);
        }

        public void Invoke(Mobile from, object targeted)
        {
            this.CancelTimeout();
            from.ClearTarget();

            if (from.Deleted)
            {
                this.OnTargetCancel(from, TargetCancelType.Canceled);
                this.OnTargetFinish(from);
                return;
            }

            Point3D loc;
            Map map;

            if (targeted is LandTarget)
            {
                loc = ((LandTarget)targeted).Location;
                map = from.Map;
            }
            else if (targeted is StaticTarget)
            {
                loc = ((StaticTarget)targeted).Location;
                map = from.Map;
            }
            else if (targeted is Mobile)
            {
                if (((Mobile)targeted).Deleted)
                {
                    this.OnTargetDeleted(from, targeted);
                    this.OnTargetFinish(from);
                    return;
                }
                else if (!((Mobile)targeted).CanTarget)
                {
                    this.OnTargetUntargetable(from, targeted);
                    this.OnTargetFinish(from);
                    return;
                }

                loc = ((Mobile)targeted).Location;
                map = ((Mobile)targeted).Map;
            }
            else if (targeted is Item)
            {
                Item item = (Item)targeted;

                if (item.Deleted)
                {
                    this.OnTargetDeleted(from, targeted);
                    this.OnTargetFinish(from);
                    return;
                }
                else if (!item.CanTarget)
                {
                    this.OnTargetUntargetable(from, targeted);
                    this.OnTargetFinish(from);
                    return;
                }

                object root = item.RootParent;

                if (!this.m_AllowNonlocal && root is Mobile && root != from && from.IsPlayer())
                {
                    this.OnNonlocalTarget(from, targeted);
                    this.OnTargetFinish(from);
                    return;
                }

                loc = item.GetWorldLocation();
                map = item.Map;
            }
            else
            {
                this.OnTargetCancel(from, TargetCancelType.Canceled);
                this.OnTargetFinish(from);
                return;
            }

            if (map == null || map != from.Map || (this.m_Range != -1 && !from.InRange(loc, this.m_Range)))
            {
                this.OnTargetOutOfRange(from, targeted);
            }
            else
            {
                if (!from.CanSee(targeted))
                    this.OnCantSeeTarget(from, targeted);
                else if (this.m_CheckLOS && !from.InLOS(targeted))
                    this.OnTargetOutOfLOS(from, targeted);
                else if (targeted is Item && ((Item)targeted).InSecureTrade)
                    this.OnTargetInSecureTrade(from, targeted);
                else if (targeted is Item && !((Item)targeted).IsAccessibleTo(from))
                    this.OnTargetNotAccessible(from, targeted);
                else if (targeted is Item && !((Item)targeted).CheckTarget(from, this, targeted))
                    this.OnTargetUntargetable(from, targeted);
                else if (targeted is Mobile && !((Mobile)targeted).CheckTarget(from, this, targeted))
                    this.OnTargetUntargetable(from, targeted);
                else if (from.Region.OnTarget(from, this, targeted))
                    this.OnTarget(from, targeted);
            }

            this.OnTargetFinish(from);
        }

        protected virtual void OnTarget(Mobile from, object targeted)
        {
        }

        protected virtual void OnTargetNotAccessible(Mobile from, object targeted)
        {
            from.SendLocalizedMessage(500447); // That is not accessible.
        }

        protected virtual void OnTargetInSecureTrade(Mobile from, object targeted)
        {
            from.SendLocalizedMessage(500447); // That is not accessible.
        }

        protected virtual void OnNonlocalTarget(Mobile from, object targeted)
        {
            from.SendLocalizedMessage(500447); // That is not accessible.
        }

        protected virtual void OnCantSeeTarget(Mobile from, object targeted)
        {
            from.SendLocalizedMessage(500237); // Target can not be seen.
        }

        protected virtual void OnTargetOutOfLOS(Mobile from, object targeted)
        {
            from.SendLocalizedMessage(500237); // Target can not be seen.
        }

        protected virtual void OnTargetOutOfRange(Mobile from, object targeted)
        {
            from.SendLocalizedMessage(500446); // That is too far away.
        }

        protected virtual void OnTargetDeleted(Mobile from, object targeted)
        {
        }

        protected virtual void OnTargetUntargetable(Mobile from, object targeted)
        {
            from.SendLocalizedMessage(500447); // That is not accessible.
        }

        protected virtual void OnTargetCancel(Mobile from, TargetCancelType cancelType)
        {
        }

        protected virtual void OnTargetFinish(Mobile from)
        {
        }

        private class TimeoutTimer : Timer
        {
            private static readonly TimeSpan ThirtySeconds = TimeSpan.FromSeconds(30.0);
            private static readonly TimeSpan TenSeconds = TimeSpan.FromSeconds(10.0);
            private static readonly TimeSpan OneSecond = TimeSpan.FromSeconds(1.0);
            private readonly Target m_Target;
            private readonly Mobile m_Mobile;
            public TimeoutTimer(Target target, Mobile m, TimeSpan delay)
                : base(delay)
            {
                this.m_Target = target;
                this.m_Mobile = m;

                if (delay >= ThirtySeconds)
                    this.Priority = TimerPriority.FiveSeconds;
                else if (delay >= TenSeconds)
                    this.Priority = TimerPriority.OneSecond;
                else if (delay >= OneSecond)
                    this.Priority = TimerPriority.TwoFiftyMS;
                else
                    this.Priority = TimerPriority.TwentyFiveMS;
            }

            protected override void OnTick()
            {
                if (this.m_Mobile.Target == this.m_Target)
                    this.m_Target.Timeout(this.m_Mobile);
            }
        }
    }
}