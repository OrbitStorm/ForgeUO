using System;
using System.Collections.Generic;
using System.Xml;
using Server.Commands;
using Server.Mobiles;

namespace Server.Regions
{
    public class GuardedRegion : BaseRegion
    {
        private static readonly object[] m_GuardParams = new object[1];
        private readonly Type m_GuardType;
        private readonly Dictionary<Mobile, GuardTimer> m_GuardCandidates = new Dictionary<Mobile, GuardTimer>();
        private bool m_Disabled;
        public GuardedRegion(string name, Map map, int priority, params Rectangle3D[] area)
            : base(name, map, priority, area)
        {
            this.m_GuardType = this.DefaultGuardType;
        }

        public GuardedRegion(string name, Map map, int priority, params Rectangle2D[] area)
            : base(name, map, priority, area)
        {
            this.m_GuardType = this.DefaultGuardType;
        }

        public GuardedRegion(XmlElement xml, Map map, Region parent)
            : base(xml, map, parent)
        {
            XmlElement el = xml["guards"];

            if (ReadType(el, "type", ref this.m_GuardType, false))
            {
                if (!typeof(Mobile).IsAssignableFrom(this.m_GuardType))
                {
                    Console.WriteLine("Invalid guard type for region '{0}'", this);
                    this.m_GuardType = this.DefaultGuardType;
                }
            }
            else
            {
                this.m_GuardType = this.DefaultGuardType;
            }

            bool disabled = false;
            if (ReadBoolean(el, "disabled", ref disabled, false))
                this.Disabled = disabled;
        }

        public bool Disabled
        {
            get
            {
                return this.m_Disabled;
            }
            set
            {
                this.m_Disabled = value;
            }
        }
        public virtual bool AllowReds
        {
            get
            {
                return Core.AOS;
            }
        }
        public virtual Type DefaultGuardType
        {
            get
            {
                if (this.Map == Map.Ilshenar || this.Map == Map.Malas)
                    return typeof(ArcherGuard);
                else
                    return typeof(WarriorGuard);
            }
        }
        public static void Initialize()
        {
            CommandSystem.Register("CheckGuarded", AccessLevel.GameMaster, new CommandEventHandler(CheckGuarded_OnCommand));
            CommandSystem.Register("SetGuarded", AccessLevel.Administrator, new CommandEventHandler(SetGuarded_OnCommand));
            CommandSystem.Register("ToggleGuarded", AccessLevel.Administrator, new CommandEventHandler(ToggleGuarded_OnCommand));
        }

        public static GuardedRegion Disable(GuardedRegion reg)
        {
            reg.Disabled = true;
            return reg;
        }

        public virtual bool IsDisabled()
        {
            return this.m_Disabled;
        }

        public virtual bool CheckVendorAccess(BaseVendor vendor, Mobile from)
        {
            if (from.AccessLevel >= AccessLevel.GameMaster || this.IsDisabled())
                return true;

            return (from.Kills < 5);
        }

        public override bool OnBeginSpellCast(Mobile m, ISpell s)
        {
            if (!this.IsDisabled() && !s.OnCastInTown(this))
            {
                m.SendLocalizedMessage(500946); // You cannot cast this in town!
                return false;
            }

            return base.OnBeginSpellCast(m, s);
        }

        public override bool AllowHousing(Mobile from, Point3D p)
        {
            return false;
        }

        public override void MakeGuard(Mobile focus)
        {
            BaseGuard useGuard = null;

            foreach (Mobile m in focus.GetMobilesInRange(8))
            {
                if (m is BaseGuard)
                {
                    BaseGuard g = (BaseGuard)m;

                    if (g.Focus == null) // idling
                    {
                        useGuard = g;
                        break;
                    }
                }
            }

            if (useGuard == null)
            {
                m_GuardParams[0] = focus;

                try
                {
                    Activator.CreateInstance(this.m_GuardType, m_GuardParams);
                }
                catch
                {
                }
            }
            else
                useGuard.Focus = focus;
        }

        public override void OnEnter(Mobile m)
        {
            if (this.IsDisabled())
                return;

            if (!this.AllowReds && m.Kills >= 5)
                this.CheckGuardCandidate(m);
        }

        public override void OnExit(Mobile m)
        {
            if (this.IsDisabled())
                return;
        }

        public override void OnSpeech(SpeechEventArgs args)
        {
            base.OnSpeech(args);

            if (this.IsDisabled())
                return;

            if (args.Mobile.Alive && args.HasKeyword(0x0007)) // *guards*
                this.CallGuards(args.Mobile.Location);
        }

        public override void OnAggressed(Mobile aggressor, Mobile aggressed, bool criminal)
        {
            base.OnAggressed(aggressor, aggressed, criminal);

            if (!this.IsDisabled() && aggressor != aggressed && criminal)
                this.CheckGuardCandidate(aggressor);
        }

        public override void OnGotBeneficialAction(Mobile helper, Mobile helped)
        {
            base.OnGotBeneficialAction(helper, helped);

            if (this.IsDisabled())
                return;

            int noto = Notoriety.Compute(helper, helped);

            if (helper != helped && (noto == Notoriety.Criminal || noto == Notoriety.Murderer))
                this.CheckGuardCandidate(helper);
        }

        public override void OnCriminalAction(Mobile m, bool message)
        {
            base.OnCriminalAction(m, message);

            if (!this.IsDisabled())
                this.CheckGuardCandidate(m);
        }

        public void CheckGuardCandidate(Mobile m)
        {
            if (this.IsDisabled())
                return;

            if (this.IsGuardCandidate(m))
            {
                GuardTimer timer = null;
                this.m_GuardCandidates.TryGetValue(m, out timer);

                if (timer == null)
                {
                    timer = new GuardTimer(m, this.m_GuardCandidates);
                    timer.Start();

                    this.m_GuardCandidates[m] = timer;
                    m.SendLocalizedMessage(502275); // Guards can now be called on you!

                    Map map = m.Map;

                    if (map != null)
                    {
                        Mobile fakeCall = null;
                        double prio = 0.0;

                        foreach (Mobile v in m.GetMobilesInRange(8))
                        {
                            if (!v.Player && v != m && !this.IsGuardCandidate(v) && ((v is BaseCreature) ? ((BaseCreature)v).IsHumanInTown() : (v.Body.IsHuman && v.Region.IsPartOf(this))))
                            {
                                double dist = m.GetDistanceToSqrt(v);

                                if (fakeCall == null || dist < prio)
                                {
                                    fakeCall = v;
                                    prio = dist;
                                }
                            }
                        }

                        if (fakeCall != null)
                        {
                            fakeCall.Say(Utility.RandomList(1007037, 501603, 1013037, 1013038, 1013039, 1013041, 1013042, 1013043, 1013052));
                            this.MakeGuard(m);
                            timer.Stop();
                            this.m_GuardCandidates.Remove(m);
                            m.SendLocalizedMessage(502276); // Guards can no longer be called on you.
                        }
                    }
                }
                else
                {
                    timer.Stop();
                    timer.Start();
                }
            }
        }

        public void CallGuards(Point3D p)
        {
            if (this.IsDisabled())
                return;

            IPooledEnumerable eable = this.Map.GetMobilesInRange(p, 14);

            foreach (Mobile m in eable)
            {
                if (this.IsGuardCandidate(m) && ((!this.AllowReds && m.Kills >= 5 && m.Region.IsPartOf(this)) || this.m_GuardCandidates.ContainsKey(m)))
                {
                    GuardTimer timer = null;
                    this.m_GuardCandidates.TryGetValue(m, out timer);

                    if (timer != null)
                    {
                        timer.Stop();
                        this.m_GuardCandidates.Remove(m);
                    }

                    this.MakeGuard(m);
                    m.SendLocalizedMessage(502276); // Guards can no longer be called on you.
                    break;
                }
            }

            eable.Free();
        }

        public bool IsGuardCandidate(Mobile m)
        {
            if (m is BaseGuard || !m.Alive || m.IsStaff() || m.Blessed || this.IsDisabled())
                return false;

            return (!this.AllowReds && m.Kills >= 5) || m.Criminal;
        }

        [Usage("CheckGuarded")]
        [Description("Returns a value indicating if the current region is guarded or not.")]
        private static void CheckGuarded_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            GuardedRegion reg = (GuardedRegion)from.Region.GetRegion(typeof(GuardedRegion));

            if (reg == null)
                from.SendMessage("You are not in a guardable region.");
            else if (reg.Disabled)
                from.SendMessage("The guards in this region have been disabled.");
            else
                from.SendMessage("This region is actively guarded.");
        }

        [Usage("SetGuarded <true|false>")]
        [Description("Enables or disables guards for the current region.")]
        private static void SetGuarded_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            if (e.Length == 1)
            {
                GuardedRegion reg = (GuardedRegion)from.Region.GetRegion(typeof(GuardedRegion));

                if (reg == null)
                {
                    from.SendMessage("You are not in a guardable region.");
                }
                else
                {
                    reg.Disabled = !e.GetBoolean(0);

                    if (reg.Disabled)
                        from.SendMessage("The guards in this region have been disabled.");
                    else
                        from.SendMessage("The guards in this region have been enabled.");
                }
            }
            else
            {
                from.SendMessage("Format: SetGuarded <true|false>");
            }
        }

        [Usage("ToggleGuarded")]
        [Description("Toggles the state of guards for the current region.")]
        private static void ToggleGuarded_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            GuardedRegion reg = (GuardedRegion)from.Region.GetRegion(typeof(GuardedRegion));

            if (reg == null)
            {
                from.SendMessage("You are not in a guardable region.");
            }
            else
            {
                reg.Disabled = !reg.Disabled;

                if (reg.Disabled)
                    from.SendMessage("The guards in this region have been disabled.");
                else
                    from.SendMessage("The guards in this region have been enabled.");
            }
        }

        private class GuardTimer : Timer
        {
            private readonly Mobile m_Mobile;
            private readonly Dictionary<Mobile, GuardTimer> m_Table;
            public GuardTimer(Mobile m, Dictionary<Mobile, GuardTimer> table)
                : base(TimeSpan.FromSeconds(15.0))
            {
                this.Priority = TimerPriority.TwoFiftyMS;

                this.m_Mobile = m;
                this.m_Table = table;
            }

            protected override void OnTick()
            {
                if (this.m_Table.ContainsKey(this.m_Mobile))
                {
                    this.m_Table.Remove(this.m_Mobile);
                    this.m_Mobile.SendLocalizedMessage(502276); // Guards can no longer be called on you.
                }
            }
        }
    }
}