/***************************************************************************
*                              PacketReader.cs
*                            -------------------
*   begin                : May 1, 2002
*   copyright            : (C) The RunUO Software Team
*   email                : info@runuo.com
*
*   $Id: PacketReader.cs 4 2006-06-15 04:28:39Z mark $
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
using System.IO;
using System.Text;

namespace Server.Network
{
    public class PacketReader
    {
        private readonly byte[] m_Data;
        private readonly int m_Size;
        private int m_Index;
        public PacketReader(byte[] data, int size, bool fixedSize)
        {
            this.m_Data = data;
            this.m_Size = size;
            this.m_Index = fixedSize ? 1 : 3;
        }

        public byte[] Buffer
        {
            get
            {
                return this.m_Data;
            }
        }
        public int Size
        {
            get
            {
                return this.m_Size;
            }
        }
        public void Trace(NetState state)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter("Packets.log", true))
                {
                    byte[] buffer = this.m_Data;

                    if (buffer.Length > 0)
                        sw.WriteLine("Client: {0}: Unhandled packet 0x{1:X2}", state, buffer[0]);

                    using (MemoryStream ms = new MemoryStream(buffer))
                        Utility.FormatBuffer(sw, ms, buffer.Length);

                    sw.WriteLine();
                    sw.WriteLine();
                }
            }
            catch
            {
            }
        }

        public int Seek(int offset, SeekOrigin origin)
        {
            switch ( origin )
            {
                case SeekOrigin.Begin:
                    this.m_Index = offset;
                    break;
                case SeekOrigin.Current:
                    this.m_Index += offset;
                    break;
                case SeekOrigin.End:
                    this.m_Index = this.m_Size - offset;
                    break;
            }

            return this.m_Index;
        }

        public int ReadInt32()
        {
            if ((this.m_Index + 4) > this.m_Size)
                return 0;

            return (this.m_Data[this.m_Index++] << 24) |
                   (this.m_Data[this.m_Index++] << 16) |
                   (this.m_Data[this.m_Index++] << 8) |
                   this.m_Data[this.m_Index++];
        }

        public short ReadInt16()
        {
            if ((this.m_Index + 2) > this.m_Size)
                return 0;

            return (short)((this.m_Data[this.m_Index++] << 8) | this.m_Data[this.m_Index++]);
        }

        public byte ReadByte()
        {
            if ((this.m_Index + 1) > this.m_Size)
                return 0;

            return this.m_Data[this.m_Index++];
        }

        public uint ReadUInt32()
        {
            if ((this.m_Index + 4) > this.m_Size)
                return 0;

            return (uint)((this.m_Data[this.m_Index++] << 24) | (this.m_Data[this.m_Index++] << 16) | (this.m_Data[this.m_Index++] << 8) | this.m_Data[this.m_Index++]);
        }

        public ushort ReadUInt16()
        {
            if ((this.m_Index + 2) > this.m_Size)
                return 0;

            return (ushort)((this.m_Data[this.m_Index++] << 8) | this.m_Data[this.m_Index++]);
        }

        public sbyte ReadSByte()
        {
            if ((this.m_Index + 1) > this.m_Size)
                return 0;

            return (sbyte)this.m_Data[this.m_Index++];
        }

        public bool ReadBoolean()
        {
            if ((this.m_Index + 1) > this.m_Size)
                return false;

            return (this.m_Data[this.m_Index++] != 0);
        }

        public string ReadUnicodeStringLE()
        {
            StringBuilder sb = new StringBuilder();

            int c;

            while ((this.m_Index + 1) < this.m_Size && (c = (this.m_Data[this.m_Index++] | (this.m_Data[this.m_Index++] << 8))) != 0)
                sb.Append((char)c);

            return sb.ToString();
        }

        public string ReadUnicodeStringLESafe(int fixedLength)
        {
            int bound = this.m_Index + (fixedLength << 1);
            int end = bound;

            if (bound > this.m_Size)
                bound = this.m_Size;

            StringBuilder sb = new StringBuilder();

            int c;

            while ((this.m_Index + 1) < bound && (c = (this.m_Data[this.m_Index++] | (this.m_Data[this.m_Index++] << 8))) != 0)
            {
                if (this.IsSafeChar(c))
                    sb.Append((char)c);
            }

            this.m_Index = end;

            return sb.ToString();
        }

        public string ReadUnicodeStringLESafe()
        {
            StringBuilder sb = new StringBuilder();

            int c;

            while ((this.m_Index + 1) < this.m_Size && (c = (this.m_Data[this.m_Index++] | (this.m_Data[this.m_Index++] << 8))) != 0)
            {
                if (this.IsSafeChar(c))
                    sb.Append((char)c);
            }

            return sb.ToString();
        }

        public string ReadUnicodeStringSafe()
        {
            StringBuilder sb = new StringBuilder();

            int c;

            while ((this.m_Index + 1) < this.m_Size && (c = ((this.m_Data[this.m_Index++] << 8) | this.m_Data[this.m_Index++])) != 0)
            {
                if (this.IsSafeChar(c))
                    sb.Append((char)c);
            }

            return sb.ToString();
        }

        public string ReadUnicodeString()
        {
            StringBuilder sb = new StringBuilder();

            int c;

            while ((this.m_Index + 1) < this.m_Size && (c = ((this.m_Data[this.m_Index++] << 8) | this.m_Data[this.m_Index++])) != 0)
                sb.Append((char)c);

            return sb.ToString();
        }

        public bool IsSafeChar(int c)
        {
            return (c >= 0x20 && c < 0xFFFE);
        }

        public string ReadUTF8StringSafe(int fixedLength)
        {
            if (this.m_Index >= this.m_Size)
            {
                this.m_Index += fixedLength;
                return String.Empty;
            }

            int bound = this.m_Index + fixedLength;
            //int end   = bound;

            if (bound > this.m_Size)
                bound = this.m_Size;

            int count = 0;
            int index = this.m_Index;
            int start = this.m_Index;

            while (index < bound && this.m_Data[index++] != 0)
                ++count;

            index = 0;

            byte[] buffer = new byte[count];
            int value = 0;

            while (this.m_Index < bound && (value = this.m_Data[this.m_Index++]) != 0)
                buffer[index++] = (byte)value;

            string s = Utility.UTF8.GetString(buffer);

            bool isSafe = true;

            for (int i = 0; isSafe && i < s.Length; ++i)
                isSafe = this.IsSafeChar((int)s[i]);

            this.m_Index = start + fixedLength;

            if (isSafe)
                return s;

            StringBuilder sb = new StringBuilder(s.Length);

            for (int i = 0; i < s.Length; ++i)
                if (this.IsSafeChar((int)s[i]))
                    sb.Append(s[i]);

            return sb.ToString();
        }

        public string ReadUTF8StringSafe()
        {
            if (this.m_Index >= this.m_Size)
                return String.Empty;

            int count = 0;
            int index = this.m_Index;

            while (index < this.m_Size && this.m_Data[index++] != 0)
                ++count;

            index = 0;

            byte[] buffer = new byte[count];
            int value = 0;

            while (this.m_Index < this.m_Size && (value = this.m_Data[this.m_Index++]) != 0)
                buffer[index++] = (byte)value;

            string s = Utility.UTF8.GetString(buffer);

            bool isSafe = true;

            for (int i = 0; isSafe && i < s.Length; ++i)
                isSafe = this.IsSafeChar((int)s[i]);

            if (isSafe)
                return s;

            StringBuilder sb = new StringBuilder(s.Length);

            for (int i = 0; i < s.Length; ++i)
            {
                if (this.IsSafeChar((int)s[i]))
                    sb.Append(s[i]);
            }

            return sb.ToString();
        }

        public string ReadUTF8String()
        {
            if (this.m_Index >= this.m_Size)
                return String.Empty;

            int count = 0;
            int index = this.m_Index;

            while (index < this.m_Size && this.m_Data[index++] != 0)
                ++count;

            index = 0;

            byte[] buffer = new byte[count];
            int value = 0;

            while (this.m_Index < this.m_Size && (value = this.m_Data[this.m_Index++]) != 0)
                buffer[index++] = (byte)value;

            return Utility.UTF8.GetString(buffer);
        }

        public string ReadString()
        {
            StringBuilder sb = new StringBuilder();

            int c;

            while (this.m_Index < this.m_Size && (c = this.m_Data[this.m_Index++]) != 0)
                sb.Append((char)c);

            return sb.ToString();
        }

        public string ReadStringSafe()
        {
            StringBuilder sb = new StringBuilder();

            int c;

            while (this.m_Index < this.m_Size && (c = this.m_Data[this.m_Index++]) != 0)
            {
                if (this.IsSafeChar(c))
                    sb.Append((char)c);
            }

            return sb.ToString();
        }

        public string ReadUnicodeStringSafe(int fixedLength)
        {
            int bound = this.m_Index + (fixedLength << 1);
            int end = bound;

            if (bound > this.m_Size)
                bound = this.m_Size;

            StringBuilder sb = new StringBuilder();

            int c;

            while ((this.m_Index + 1) < bound && (c = ((this.m_Data[this.m_Index++] << 8) | this.m_Data[this.m_Index++])) != 0)
            {
                if (this.IsSafeChar(c))
                    sb.Append((char)c);
            }

            this.m_Index = end;

            return sb.ToString();
        }

        public string ReadUnicodeString(int fixedLength)
        {
            int bound = this.m_Index + (fixedLength << 1);
            int end = bound;

            if (bound > this.m_Size)
                bound = this.m_Size;

            StringBuilder sb = new StringBuilder();

            int c;

            while ((this.m_Index + 1) < bound && (c = ((this.m_Data[this.m_Index++] << 8) | this.m_Data[this.m_Index++])) != 0)
                sb.Append((char)c);

            this.m_Index = end;

            return sb.ToString();
        }

        public string ReadStringSafe(int fixedLength)
        {
            int bound = this.m_Index + fixedLength;
            int end = bound;

            if (bound > this.m_Size)
                bound = this.m_Size;

            StringBuilder sb = new StringBuilder();

            int c;

            while (this.m_Index < bound && (c = this.m_Data[this.m_Index++]) != 0)
            {
                if (this.IsSafeChar(c))
                    sb.Append((char)c);
            }

            this.m_Index = end;

            return sb.ToString();
        }

        public string ReadString(int fixedLength)
        {
            int bound = this.m_Index + fixedLength;
            int end = bound;

            if (bound > this.m_Size)
                bound = this.m_Size;

            StringBuilder sb = new StringBuilder();

            int c;

            while (this.m_Index < bound && (c = this.m_Data[this.m_Index++]) != 0)
                sb.Append((char)c);

            this.m_Index = end;

            return sb.ToString();
        }
    }
}