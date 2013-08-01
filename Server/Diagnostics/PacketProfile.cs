/***************************************************************************
*                              PacketProfile.cs
*                            -------------------
*   begin                : May 1, 2002
*   copyright            : (C) The RunUO Software Team
*   email                : info@runuo.com
*
*   $Id: PacketProfile.cs 644 2010-12-23 09:18:45Z asayre $
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
    public abstract class BasePacketProfile : BaseProfile
    {
        private long _totalLength;
        protected BasePacketProfile(string name)
            : base(name)
        {
        }

        public long TotalLength
        {
            get
            {
                return this._totalLength;
            }
        }
        public double AverageLength
        {
            get
            {
                return (double)this._totalLength / Math.Max(1, this.Count);
            }
        }
        public void Finish(int length)
        {
            this.Finish();

            this._totalLength += length;
        }

        public override void WriteTo(TextWriter op)
        {
            base.WriteTo(op);

            op.Write("\t{0,12:F2} {1,-12:N0}", this.AverageLength, this.TotalLength);
        }
    }

    public class PacketSendProfile : BasePacketProfile
    {
        private static readonly Dictionary<Type, PacketSendProfile> _profiles = new Dictionary<Type, PacketSendProfile>();
        private long _created;
        public PacketSendProfile(Type type)
            : base(type.FullName)
        {
        }

        public static IEnumerable<PacketSendProfile> Profiles
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
        public static PacketSendProfile Acquire(Type type)
        {
            if (!Core.Profiling)
            {
                return null;
            }

            PacketSendProfile prof;

            if (!_profiles.TryGetValue(type, out prof))
            {
                _profiles.Add(type, prof = new PacketSendProfile(type));
            }

            return prof;
        }

        public override void WriteTo(TextWriter op)
        {
            base.WriteTo(op);

            op.Write("\t{0,12:N0}", this.Created);
        }
    }

    public class PacketReceiveProfile : BasePacketProfile
    {
        private static readonly Dictionary<int, PacketReceiveProfile> _profiles = new Dictionary<int, PacketReceiveProfile>();
        public PacketReceiveProfile(int packetId)
            : base(String.Format("0x{0:X2}", packetId))
        {
        }

        public static IEnumerable<PacketReceiveProfile> Profiles
        {
            get
            {
                return _profiles.Values;
            }
        }
        public static PacketReceiveProfile Acquire(int packetId)
        {
            if (!Core.Profiling)
            {
                return null;
            }

            PacketReceiveProfile prof;

            if (!_profiles.TryGetValue(packetId, out prof))
            {
                _profiles.Add(packetId, prof = new PacketReceiveProfile(packetId));
            }

            return prof;
        }
    }
}