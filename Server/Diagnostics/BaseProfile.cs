/***************************************************************************
*                              PacketProfile.cs
*                            -------------------
*   begin                : May 1, 2002
*   copyright            : (C) The RunUO Software Team
*   email                : info@runuo.com
*
*   $Id: BaseProfile.cs 644 2010-12-23 09:18:45Z asayre $
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
using System.Diagnostics;
using System.IO;

namespace Server.Diagnostics
{
    public abstract class BaseProfile
    {
        private readonly string _name;
        private readonly Stopwatch _stopwatch;
        private long _count;
        private TimeSpan _totalTime;
        private TimeSpan _peakTime;
        protected BaseProfile(string name)
        {
            this._name = name;

            this._stopwatch = new Stopwatch();
        }

        public string Name
        {
            get
            {
                return this._name;
            }
        }
        public long Count
        {
            get
            {
                return this._count;
            }
        }
        public TimeSpan AverageTime
        {
            get
            {
                return TimeSpan.FromTicks(this._totalTime.Ticks / Math.Max(1, this._count));
            }
        }
        public TimeSpan PeakTime
        {
            get
            {
                return this._peakTime;
            }
        }
        public TimeSpan TotalTime
        {
            get
            {
                return this._totalTime;
            }
        }
        public static void WriteAll<T>(TextWriter op, IEnumerable<T> profiles) where T : BaseProfile
        {
            List<T> list = new List<T>(profiles);

            list.Sort(delegate(T a, T b)
            {
                return -a.TotalTime.CompareTo(b.TotalTime);
            });

            foreach (T prof in list)
            {
                prof.WriteTo(op);
                op.WriteLine();
            }
        }

        public virtual void Start()
        {
            if (this._stopwatch.IsRunning)
            {
                this._stopwatch.Reset();
            }

            this._stopwatch.Start();
        }

        public virtual void Finish()
        {
            TimeSpan elapsed = this._stopwatch.Elapsed;

            this._totalTime += elapsed;

            if (elapsed > this._peakTime)
            {
                this._peakTime = elapsed;
            }

            this._count++;

            this._stopwatch.Reset();
        }

        public virtual void WriteTo(TextWriter op)
        {
            op.Write("{0,-100} {1,12:N0} {2,12:F5} {3,-12:F5} {4,12:F5}", this.Name, this.Count, this.AverageTime.TotalSeconds, this.PeakTime.TotalSeconds, this.TotalTime.TotalSeconds);
        }
    }
}