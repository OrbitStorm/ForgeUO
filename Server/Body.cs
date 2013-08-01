using System;
using System.IO;

namespace Server
{
    public enum BodyType : byte
    {
        Empty,
        Monster,
        Sea,
        Animal,
        Human,
        Equipment
    }

    public struct Body
    {
        private readonly int m_BodyID;

        private static BodyType[] m_Types;

        static Body()
        {
            if (File.Exists("Data/bodyTable.cfg"))
            {
                using (StreamReader ip = new StreamReader("Data/bodyTable.cfg"))
                {
                    m_Types = new BodyType[0x1000];

                    string line;

                    while ((line = ip.ReadLine()) != null)
                    {
                        if (line.Length == 0 || line.StartsWith("#"))
                            continue;

                        string[] split = line.Split('\t');

                        BodyType type;
                        int bodyID;

                        if (int.TryParse(split[0], out bodyID) && Enum.TryParse(split[1], true, out type) && bodyID >= 0 && bodyID < m_Types.Length)
                        {
                            m_Types[bodyID] = type;
                        }
                        else
                        {
                            Console.WriteLine("Warning: Invalid bodyTable entry:");
                            Console.WriteLine(line);
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("Warning: Data/bodyTable.cfg does not exist");

                m_Types = new BodyType[0];
            }
        }

        public Body(int bodyID)
        {
            this.m_BodyID = bodyID;
        }

        public BodyType Type
        {
            get
            {
                if (this.m_BodyID >= 0 && this.m_BodyID < m_Types.Length)
                    return m_Types[this.m_BodyID];
                else
                    return BodyType.Empty;
            }
        }

        public bool IsHuman
        {
            get
            {
                #region Stygian Abyss
                return (this.m_BodyID >= 0 &&
                        this.m_BodyID < m_Types.Length &&
                        m_Types[this.m_BodyID] == BodyType.Human &&
                        this.m_BodyID != 402 &&
                        this.m_BodyID != 403 &&
                        this.m_BodyID != 607 &&
                        this.m_BodyID != 608 &&
                        this.m_BodyID != 970) || this.m_BodyID == 694 || this.m_BodyID == 695;
                #endregion
            }
        }

        public bool IsMale
        {
            get
            {
                return this.m_BodyID == 183 ||
                       this.m_BodyID == 185 ||
                       this.m_BodyID == 400 ||
                       this.m_BodyID == 402 ||
                       this.m_BodyID == 605 ||
                       this.m_BodyID == 607 ||
                       this.m_BodyID == 750
                       #region Stygian Abyss
                       ||
                       this.m_BodyID == 666;
                #endregion
            }
        }

        public bool IsFemale
        {
            get
            {
                return this.m_BodyID == 184 ||
                       this.m_BodyID == 186 ||
                       this.m_BodyID == 401 ||
                       this.m_BodyID == 403 ||
                       this.m_BodyID == 606 ||
                       this.m_BodyID == 608 ||
                       this.m_BodyID == 751
                       #region Stygian Abyss
                       ||
                       this.m_BodyID == 667;
                #endregion
            }
        }

        public bool IsGhost
        {
            get
            {
                return this.m_BodyID == 402 ||
                       this.m_BodyID == 403 ||
                       this.m_BodyID == 607 ||
                       this.m_BodyID == 608 ||
                       this.m_BodyID == 694 ||
                       this.m_BodyID == 695 ||
                       this.m_BodyID == 970;
            }
        }

        public bool IsMonster
        {
            get
            {
                return this.m_BodyID >= 0 &&
                       this.m_BodyID < m_Types.Length &&
                       m_Types[this.m_BodyID] == BodyType.Monster;
            }
        }

        public bool IsAnimal
        {
            get
            {
                return this.m_BodyID >= 0 &&
                       this.m_BodyID < m_Types.Length &&
                       m_Types[this.m_BodyID] == BodyType.Animal;
            }
        }

        public bool IsEmpty
        {
            get
            {
                return this.m_BodyID >= 0 &&
                       this.m_BodyID < m_Types.Length &&
                       m_Types[this.m_BodyID] == BodyType.Empty;
            }
        }

        public bool IsSea
        {
            get
            {
                return this.m_BodyID >= 0 &&
                       this.m_BodyID < m_Types.Length &&
                       m_Types[this.m_BodyID] == BodyType.Sea;
            }
        }

        public bool IsEquipment
        {
            get
            {
                return this.m_BodyID >= 0 &&
                       this.m_BodyID < m_Types.Length &&
                       m_Types[this.m_BodyID] == BodyType.Equipment;
            }
        }

        public int BodyID
        {
            get
            {
                return this.m_BodyID;
            }
        }

        public static implicit operator int(Body a)
        {
            return a.m_BodyID;
        }

        public static implicit operator Body(int a)
        {
            return new Body(a);
        }

        public override string ToString()
        {
            return string.Format("0x{0:X}", this.m_BodyID);
        }

        public override int GetHashCode()
        {
            return this.m_BodyID;
        }

        public override bool Equals(object o)
        {
            if (o == null || !(o is Body))
                return false;

            return ((Body)o).m_BodyID == this.m_BodyID;
        }

        public static bool operator ==(Body l, Body r)
        {
            return l.m_BodyID == r.m_BodyID;
        }

        public static bool operator !=(Body l, Body r)
        {
            return l.m_BodyID != r.m_BodyID;
        }

        public static bool operator >(Body l, Body r)
        {
            return l.m_BodyID > r.m_BodyID;
        }

        public static bool operator >=(Body l, Body r)
        {
            return l.m_BodyID >= r.m_BodyID;
        }

        public static bool operator <(Body l, Body r)
        {
            return l.m_BodyID < r.m_BodyID;
        }

        public static bool operator <=(Body l, Body r)
        {
            return l.m_BodyID <= r.m_BodyID;
        }
    }
}