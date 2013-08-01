/***************************************************************************
*                           ObjectPropertyList.cs
*                            -------------------
*   begin                : May 1, 2002
*   copyright            : (C) The RunUO Software Team
*   email                : info@runuo.com
*
*   $Id: ObjectPropertyList.cs 653 2010-12-31 11:09:18Z asayre $
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
using System.Text;
using Server.Network;

namespace Server
{
    public sealed class ObjectPropertyList : Packet
    {
        private static readonly Encoding m_Encoding = Encoding.Unicode;
        // Each of these are localized to "~1_NOTHING~" which allows the string argument to be used
        private static readonly int[] m_StringNumbers = new int[]
        {
            1042971,
            1070722
        };
        private static bool m_Enabled = false;
        private static byte[] m_Buffer = new byte[1024];
        private readonly IEntity m_Entity;
        private int m_Hash;
        private int m_Header;
        private int m_Strings;
        private string m_HeaderArgs;
        public ObjectPropertyList(IEntity e)
            : base(0xD6)
        {
            this.EnsureCapacity(128);

            this.m_Entity = e;

            this.m_Stream.Write((short)1);
            this.m_Stream.Write((int)e.Serial);
            this.m_Stream.Write((byte)0);
            this.m_Stream.Write((byte)0);
            this.m_Stream.Write((int)e.Serial);
        }

        public static bool Enabled
        {
            get
            {
                return m_Enabled;
            }
            set
            {
                m_Enabled = value;
            }
        }
        public IEntity Entity
        {
            get
            {
                return this.m_Entity;
            }
        }
        public int Hash
        {
            get
            {
                return 0x40000000 + this.m_Hash;
            }
        }
        public int Header
        {
            get
            {
                return this.m_Header;
            }
            set
            {
                this.m_Header = value;
            }
        }
        public string HeaderArgs
        {
            get
            {
                return this.m_HeaderArgs;
            }
            set
            {
                this.m_HeaderArgs = value;
            }
        }
        public void Add(int number)
        {
            if (number == 0)
                return;

            this.AddHash(number);

            if (this.m_Header == 0)
            {
                this.m_Header = number;
                this.m_HeaderArgs = "";
            }

            this.m_Stream.Write(number);
            this.m_Stream.Write((short)0);
        }

        public void Terminate()
        {
            this.m_Stream.Write((int)0);

            this.m_Stream.Seek(11, System.IO.SeekOrigin.Begin);
            this.m_Stream.Write((int)this.m_Hash);
        }

        public void AddHash(int val)
        {
            this.m_Hash ^= (val & 0x3FFFFFF);
            this.m_Hash ^= (val >> 26) & 0x3F;
        }

        public void Add(int number, string arguments)
        {
            if (number == 0)
                return;

            if (arguments == null)
                arguments = "";

            if (this.m_Header == 0)
            {
                this.m_Header = number;
                this.m_HeaderArgs = arguments;
            }

            this.AddHash(number);
            this.AddHash(arguments.GetHashCode());

            this.m_Stream.Write(number);

            int byteCount = m_Encoding.GetByteCount(arguments);

            if (byteCount > m_Buffer.Length)
                m_Buffer = new byte[byteCount];

            byteCount = m_Encoding.GetBytes(arguments, 0, arguments.Length, m_Buffer, 0);

            this.m_Stream.Write((short)byteCount);
            this.m_Stream.Write(m_Buffer, 0, byteCount);
        }

        public void Add(int number, string format, object arg0)
        {
            this.Add(number, String.Format(format, arg0));
        }

        public void Add(int number, string format, object arg0, object arg1)
        {
            this.Add(number, String.Format(format, arg0, arg1));
        }

        public void Add(int number, string format, object arg0, object arg1, object arg2)
        {
            this.Add(number, String.Format(format, arg0, arg1, arg2));
        }

        public void Add(int number, string format, params object[] args)
        {
            this.Add(number, String.Format(format, args));
        }

        public void Add(string text)
        {
            this.Add(this.GetStringNumber(), text);
        }

        public void Add(string format, string arg0)
        {
            this.Add(this.GetStringNumber(), String.Format(format, arg0));
        }

        public void Add(string format, string arg0, string arg1)
        {
            this.Add(this.GetStringNumber(), String.Format(format, arg0, arg1));
        }

        public void Add(string format, string arg0, string arg1, string arg2)
        {
            this.Add(this.GetStringNumber(), String.Format(format, arg0, arg1, arg2));
        }

        public void Add(string format, params object[] args)
        {
            this.Add(this.GetStringNumber(), String.Format(format, args));
        }

        private int GetStringNumber()
        {
            return m_StringNumbers[this.m_Strings++ % m_StringNumbers.Length];
        }
    }

    public sealed class OPLInfo : Packet
    {
        /*public OPLInfo( ObjectPropertyList list ) : base( 0xBF )
        {
        EnsureCapacity( 13 );
        m_Stream.Write( (short) 0x10 );
        m_Stream.Write( (int) list.Entity.Serial );
        m_Stream.Write( (int) list.Hash );
        }*/
        public OPLInfo(ObjectPropertyList list)
            : base(0xDC, 9)
        {
            this.m_Stream.Write((int)list.Entity.Serial);
            this.m_Stream.Write((int)list.Hash);
        }
    }
}