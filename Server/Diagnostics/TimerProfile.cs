/***************************************************************************
*                              PacketProfile.cs
*                            -------------------
*   begin                : May 1, 2002
*   copyright            : (C) The RunUO Software Team
*   email                : info@runuo.com
*
*   $Id: TimerProfile.cs 169 2007-04-22 07:31:23Z krrios $
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

namespace Server.Diagnostics
{
    public class TimerProfile : BaseProfile
    {
        private static readonly Dictionary<string, TimerProfile> _profiles = new Dictionary<string, TimerProfile>();
        private long _created, _started, _stopped;
        public TimerProfile(string name)
            : base(name)
        {
        }

        public static IEnumerable<TimerProfile> Profiles
        {
            get
            {
                return _profiles.Values;
            }
        }
        public long Created
        {
            get
            {
                return this._created;
            }
            set
            {
                this._created = value;
            }
        }
        public long Started
        {
            get
            {
                return this._started;
            }
            set
            {
                this._started = value;
            }
        }
        public long Stopped
        {
            get
            {
                return this._stopped;
            }
            set
            {
                this._stopped = value;
            }
        }
        public static TimerProfile Acquire(string name)
        {
            if (!Core.Profiling)
            {
                return null;
            }

            TimerProfile prof;

            if (!_profiles.TryGetValue(name, out prof))
            {
                _profiles.Add(name, prof = new TimerProfile(name));
            }

            return prof;
        }

        public override void WriteTo(TextWriter op)
        {
            base.WriteTo(op);

            op.Write("\t{0,12:N0} {1,12:N0} {2,-12:N0}", this._created, this._started, this._stopped);
        }
    }
}