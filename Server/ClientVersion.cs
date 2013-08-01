/***************************************************************************
*                              ClientVersion.cs
*                            -------------------
*   begin                : May 1, 2002
*   copyright            : (C) The RunUO Software Team
*   email                : info@runuo.com
*
*   $Id: ClientVersion.cs 521 2010-06-17 07:11:43Z mark $
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
using System.Collections;
using System.Text;

namespace Server
{
    public enum ClientType
    {
        Regular,
        UOTD,
        God,
        SA
    }

    public class ClientVersion : IComparable, IComparer
    {
        private readonly int m_Major;
        private readonly int m_Minor;
        private readonly int m_Revision;
        private readonly int m_Patch;
        private readonly ClientType m_Type;
        private readonly string m_SourceString;
        public ClientVersion(int maj, int min, int rev, int pat)
            : this(maj, min, rev, pat, ClientType.Regular)
        {
        }

        public ClientVersion(int maj, int min, int rev, int pat, ClientType type)
        {
            this.m_Major = maj;
            this.m_Minor = min;
            this.m_Revision = rev;
            this.m_Patch = pat;
            this.m_Type = type;

            this.m_SourceString = this.ToString();
        }

        public ClientVersion(string fmt)
        {
            this.m_SourceString = fmt;

            try
            {
                fmt = fmt.ToLower();

                int br1 = fmt.IndexOf('.');
                int br2 = fmt.IndexOf('.', br1 + 1);

                int br3 = br2 + 1;
                while (br3 < fmt.Length && Char.IsDigit(fmt, br3))
                    br3++;

                this.m_Major = Utility.ToInt32(fmt.Substring(0, br1));
                this.m_Minor = Utility.ToInt32(fmt.Substring(br1 + 1, br2 - br1 - 1));
                this.m_Revision = Utility.ToInt32(fmt.Substring(br2 + 1, br3 - br2 - 1));

                if (br3 < fmt.Length)
                {
                    if (this.m_Major <= 5 && this.m_Minor <= 0 && this.m_Revision <= 6)	//Anything before 5.0.7
                    {
                        if (!Char.IsWhiteSpace(fmt, br3))
                            this.m_Patch = (fmt[br3] - 'a') + 1;
                    }
                    else
                    {
                        this.m_Patch = Utility.ToInt32(fmt.Substring(br3 + 1, fmt.Length - br3 - 1));
                    }
                }

                if (fmt.IndexOf("god") >= 0 || fmt.IndexOf("gq") >= 0)
                    this.m_Type = ClientType.God;
                else if (fmt.IndexOf("third dawn") >= 0 || fmt.IndexOf("uo:td") >= 0 || fmt.IndexOf("uotd") >= 0 || fmt.IndexOf("uo3d") >= 0 || fmt.IndexOf("uo:3d") >= 0)
                    this.m_Type = ClientType.UOTD;
                else
                    this.m_Type = ClientType.Regular;
            }
            catch
            {
                this.m_Major = 0;
                this.m_Minor = 0;
                this.m_Revision = 0;
                this.m_Patch = 0;
                this.m_Type = ClientType.Regular;
            }
        }

        public int Major
        {
            get
            {
                return this.m_Major;
            }
        }
        public int Minor
        {
            get
            {
                return this.m_Minor;
            }
        }
        public int Revision
        {
            get
            {
                return this.m_Revision;
            }
        }
        public int Patch
        {
            get
            {
                return this.m_Patch;
            }
        }
        public ClientType Type
        {
            get
            {
                return this.m_Type;
            }
        }
        public string SourceString
        {
            get
            {
                return this.m_SourceString;
            }
        }
        public static bool IsNull(object x)
        {
            return Object.ReferenceEquals(x, null);
        }

        public static int Compare(ClientVersion a, ClientVersion b)
        {
            if (IsNull(a) && IsNull(b))
                return 0;
            else if (IsNull(a))
                return -1;
            else if (IsNull(b))
                return 1;

            return a.CompareTo(b);
        }

        public override int GetHashCode()
        {
            return this.m_Major ^ this.m_Minor ^ this.m_Revision ^ this.m_Patch ^ (int)this.m_Type;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            ClientVersion v = obj as ClientVersion;

            if (v == null)
                return false;

            return this.m_Major == v.m_Major &&
                   this.m_Minor == v.m_Minor &&
                   this.m_Revision == v.m_Revision &&
                   this.m_Patch == v.m_Patch &&
                   this.m_Type == v.m_Type;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder(16);

            builder.Append(this.m_Major);
            builder.Append('.');
            builder.Append(this.m_Minor);
            builder.Append('.');
            builder.Append(this.m_Revision);

            if (this.m_Major <= 5 && this.m_Minor <= 0 && this.m_Revision <= 6)	//Anything before 5.0.7
            {
                if (this.m_Patch > 0)
                    builder.Append((char)('a' + (this.m_Patch - 1)));
            }
            else
            {
                builder.Append('.');
                builder.Append(this.m_Patch);
            }

            if (this.m_Type != ClientType.Regular)
            {
                builder.Append(' ');
                builder.Append(this.m_Type.ToString());
            }

            return builder.ToString();
        }

        public int CompareTo(object obj)
        {
            if (obj == null)
                return 1;

            ClientVersion o = obj as ClientVersion;

            if (o == null)
                throw new ArgumentException();

            if (this.m_Major > o.m_Major)
                return 1;
            else if (this.m_Major < o.m_Major)
                return -1;
            else if (this.m_Minor > o.m_Minor)
                return 1;
            else if (this.m_Minor < o.m_Minor)
                return -1;
            else if (this.m_Revision > o.m_Revision)
                return 1;
            else if (this.m_Revision < o.m_Revision)
                return -1;
            else if (this.m_Patch > o.m_Patch)
                return 1;
            else if (this.m_Patch < o.m_Patch)
                return -1;
            else
                return 0;
        }

        public int Compare(object x, object y)
        {
            if (IsNull(x) && IsNull(y))
                return 0;
            else if (IsNull(x))
                return -1;
            else if (IsNull(y))
                return 1;

            ClientVersion a = x as ClientVersion;
            ClientVersion b = y as ClientVersion;

            if (IsNull(a) || IsNull(b))
                throw new ArgumentException();

            return a.CompareTo(b);
        }

        public static bool operator ==(ClientVersion l, ClientVersion r)
        {
            return (Compare(l, r) == 0);
        }

        public static bool operator !=(ClientVersion l, ClientVersion r)
        {
            return (Compare(l, r) != 0);
        }

        public static bool operator >=(ClientVersion l, ClientVersion r)
        {
            return (Compare(l, r) >= 0);
        }

        public static bool operator >(ClientVersion l, ClientVersion r)
        {
            return (Compare(l, r) > 0);
        }

        public static bool operator <=(ClientVersion l, ClientVersion r)
        {
            return (Compare(l, r) <= 0);
        }

        public static bool operator <(ClientVersion l, ClientVersion r)
        {
            return (Compare(l, r) < 0);
        }
    }
}