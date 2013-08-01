/***************************************************************************
*                              AggressorInfo.cs
*                            -------------------
*   begin                : May 1, 2002
*   copyright            : (C) The RunUO Software Team
*   email                : info@runuo.com
*
*   $Id: AggressorInfo.cs 4 2006-06-15 04:28:39Z mark $
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
using System.Collections.Generic;
using System.IO;

namespace Server
{
    public class AggressorInfo
    {
        private static readonly Queue<AggressorInfo> m_Pool = new Queue<AggressorInfo>();
        private static TimeSpan m_ExpireDelay = TimeSpan.FromMinutes(2.0);
        private Mobile m_Attacker, m_Defender;
        private DateTime m_LastCombatTime;
        private bool m_CanReportMurder;
        private bool m_Reported;
        private bool m_CriminalAggression;
        private bool m_Queued;
        private AggressorInfo(Mobile attacker, Mobile defender, bool criminal)
        {
            this.m_Attacker = attacker;
            this.m_Defender = defender;

            this.m_CanReportMurder = criminal;
            this.m_CriminalAggression = criminal;

            this.Refresh();
        }

        public static TimeSpan ExpireDelay
        {
            get
            {
                return m_ExpireDelay;
            }
            set
            {
                m_ExpireDelay = value;
            }
        }
        public bool Expired
        {
            get
            {
                if (this.m_Queued)
                    DumpAccess();

                return (this.m_Attacker.Deleted || this.m_Defender.Deleted || DateTime.Now >= (this.m_LastCombatTime + m_ExpireDelay));
            }
        }
        public bool CriminalAggression
        {
            get
            {
                if (this.m_Queued)
                    DumpAccess();

                return this.m_CriminalAggression;
            }
            set
            {
                if (this.m_Queued)
                    DumpAccess();

                this.m_CriminalAggression = value;
            }
        }
        public Mobile Attacker
        {
            get
            {
                if (this.m_Queued)
                    DumpAccess();

                return this.m_Attacker;
            }
        }
        public Mobile Defender
        {
            get
            {
                if (this.m_Queued)
                    DumpAccess();

                return this.m_Defender;
            }
        }
        public DateTime LastCombatTime
        {
            get
            {
                if (this.m_Queued)
                    DumpAccess();

                return this.m_LastCombatTime;
            }
        }
        public bool Reported
        {
            get
            {
                if (this.m_Queued)
                    DumpAccess();

                return this.m_Reported;
            }
            set
            {
                if (this.m_Queued)
                    DumpAccess();

                this.m_Reported = value;
            }
        }
        public bool CanReportMurder
        {
            get
            {
                if (this.m_Queued)
                    DumpAccess();

                return this.m_CanReportMurder;
            }
            set
            {
                if (this.m_Queued)
                    DumpAccess();

                this.m_CanReportMurder = value;
            }
        }
        public static AggressorInfo Create(Mobile attacker, Mobile defender, bool criminal)
        {
            AggressorInfo info;

            if (m_Pool.Count > 0)
            {
                info = m_Pool.Dequeue();

                info.m_Attacker = attacker;
                info.m_Defender = defender;

                info.m_CanReportMurder = criminal;
                info.m_CriminalAggression = criminal;

                info.m_Queued = false;

                info.Refresh();
            }
            else
            {
                info = new AggressorInfo(attacker, defender, criminal);
            }

            return info;
        }

        public static void DumpAccess()
        {
            using (StreamWriter op = new StreamWriter("warnings.log", true))
            {
                op.WriteLine("Warning: Access to queued AggressorInfo:");
                op.WriteLine(new System.Diagnostics.StackTrace());
                op.WriteLine();
                op.WriteLine();
            }
        }

        public void Free()
        {
            if (this.m_Queued)
                return;

            this.m_Queued = true;
            m_Pool.Enqueue(this);
        }

        public void Refresh()
        {
            if (this.m_Queued)
                DumpAccess();

            this.m_LastCombatTime = DateTime.Now;
            this.m_Reported = false;
        }
    }
}