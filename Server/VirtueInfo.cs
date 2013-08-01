/***************************************************************************
*                               VirtueInfo.cs
*                            -------------------
*   begin                : May 1, 2002
*   copyright            : (C) The RunUO Software Team
*   email                : info@runuo.com
*
*   $Id: VirtueInfo.cs 4 2006-06-15 04:28:39Z mark $
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

namespace Server
{
    [PropertyObject]
    public class VirtueInfo
    {
        private int[] m_Values;
        public VirtueInfo()
        {
        }

        public VirtueInfo(GenericReader reader)
        {
            int version = reader.ReadByte();

            switch ( version )
            {
                case 1:	//Changed the values throughout the virtue system
                case 0:
                    {
                        int mask = reader.ReadByte();

                        if (mask != 0)
                        {
                            this.m_Values = new int[8];

                            for (int i = 0; i < 8; ++i)
                                if ((mask & (1 << i)) != 0)
                                    this.m_Values[i] = reader.ReadInt();
                        }

                        break;
                    }
            }

            if (version == 0)
            {
                this.Compassion *= 200;
                this.Sacrifice *= 250;	//Even though 40 (the max) only gives 10k, It's because it was formerly too easy

                //No direct conversion factor for Justice, this is just an approximation
                this.Justice *= 500;
                //All the other virtues haven't been defined at 'version 0' point in time in the scripts.
            }
        }

        public int[] Values
        {
            get
            {
                return this.m_Values;
            }
        }
        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public int Humility
        {
            get
            {
                return this.GetValue(0);
            }
            set
            {
                this.SetValue(0, value);
            }
        }
        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public int Sacrifice
        {
            get
            {
                return this.GetValue(1);
            }
            set
            {
                this.SetValue(1, value);
            }
        }
        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public int Compassion
        {
            get
            {
                return this.GetValue(2);
            }
            set
            {
                this.SetValue(2, value);
            }
        }
        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public int Spirituality
        {
            get
            {
                return this.GetValue(3);
            }
            set
            {
                this.SetValue(3, value);
            }
        }
        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public int Valor
        {
            get
            {
                return this.GetValue(4);
            }
            set
            {
                this.SetValue(4, value);
            }
        }
        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public int Honor
        {
            get
            {
                return this.GetValue(5);
            }
            set
            {
                this.SetValue(5, value);
            }
        }
        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public int Justice
        {
            get
            {
                return this.GetValue(6);
            }
            set
            {
                this.SetValue(6, value);
            }
        }
        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public int Honesty
        {
            get
            {
                return this.GetValue(7);
            }
            set
            {
                this.SetValue(7, value);
            }
        }
        public static void Serialize(GenericWriter writer, VirtueInfo info)
        {
            writer.Write((byte)1); // version

            if (info.m_Values == null)
            {
                writer.Write((byte)0);
            }
            else
            {
                int mask = 0;

                for (int i = 0; i < 8; ++i)
                    if (info.m_Values[i] != 0)
                        mask |= 1 << i;

                writer.Write((byte)mask);

                for (int i = 0; i < 8; ++i)
                    if (info.m_Values[i] != 0)
                        writer.Write((int)info.m_Values[i]);
            }
        }

        public int GetValue(int index)
        {
            if (this.m_Values == null)
                return 0;
            else
                return this.m_Values[index];
        }

        public void SetValue(int index, int value)
        {
            if (this.m_Values == null)
                this.m_Values = new int[8];

            this.m_Values[index] = value;
        }

        public override string ToString()
        {
            return "...";
        }
    }
}