using System;
using System.Collections.Generic;

namespace Server
{
    [Parsable]
    public abstract class Race
    {
        private static readonly Race[] m_Races = new Race[0x100];
        private static readonly List<Race> m_AllRaces = new List<Race>();
        private static string[] m_RaceNames;
        private static Race[] m_RaceValues;
        private readonly int m_RaceID;
        private readonly int m_RaceIndex;
        private readonly int m_MaleBody;
        private readonly int m_FemaleBody;
        private readonly int m_MaleGhostBody;
        private readonly int m_FemaleGhostBody;
        private readonly Expansion m_RequiredExpansion;
        private string m_Name, m_PluralName;
        protected Race(int raceID, int raceIndex, string name, string pluralName, int maleBody, int femaleBody, int maleGhostBody, int femaleGhostBody, Expansion requiredExpansion)
        {
            this.m_RaceID = raceID;
            this.m_RaceIndex = raceIndex;

            this.m_Name = name;

            this.m_MaleBody = maleBody;
            this.m_FemaleBody = femaleBody;
            this.m_MaleGhostBody = maleGhostBody;
            this.m_FemaleGhostBody = femaleGhostBody;

            this.m_RequiredExpansion = requiredExpansion;
            this.m_PluralName = pluralName;
        }

        public static Race DefaultRace
        {
            get
            {
                return m_Races[0];
            }
        }
        public static Race[] Races
        {
            get
            {
                return m_Races;
            }
        }
        public static Race Human
        {
            get
            {
                return m_Races[0];
            }
        }
        public static Race Elf
        {
            get
            {
                return m_Races[1];
            }
        }
        public static Race Gargoyle
        {
            get
            {
                return m_Races[2];
            }
        }
        public static List<Race> AllRaces
        {
            get
            {
                return m_AllRaces;
            }
        }
        public Expansion RequiredExpansion
        {
            get
            {
                return this.m_RequiredExpansion;
            }
        }
        public int MaleBody
        {
            get
            {
                return this.m_MaleBody;
            }
        }
        public int MaleGhostBody
        {
            get
            {
                return this.m_MaleGhostBody;
            }
        }
        public int FemaleBody
        {
            get
            {
                return this.m_FemaleBody;
            }
        }
        public int FemaleGhostBody
        {
            get
            {
                return this.m_FemaleGhostBody;
            }
        }
        public int RaceID
        {
            get
            {
                return this.m_RaceID;
            }
        }
        public int RaceIndex
        {
            get
            {
                return this.m_RaceIndex;
            }
        }
        public string Name
        {
            get
            {
                return this.m_Name;
            }
            set
            {
                this.m_Name = value;
            }
        }
        public string PluralName
        {
            get
            {
                return this.m_PluralName;
            }
            set
            {
                this.m_PluralName = value;
            }
        }
        public static string[] GetRaceNames()
        {
            CheckNamesAndValues();
            return m_RaceNames;
        }

        public static Race[] GetRaceValues()
        {
            CheckNamesAndValues();
            return m_RaceValues;
        }

        public static Race Parse(string value)
        {
            CheckNamesAndValues();

            for (int i = 0; i < m_RaceNames.Length; ++i)
            {
                if (Insensitive.Equals(m_RaceNames[i], value))
                    return m_RaceValues[i];
            }

            int index;
            if (int.TryParse(value, out index))
            {
                if (index >= 0 && index < m_Races.Length && m_Races[index] != null)
                    return m_Races[index];
            }

            throw new ArgumentException("Invalid race name");
        }

        public override string ToString()
        {
            return this.m_Name;
        }

        public virtual bool ValidateHair(Mobile m, int itemID)
        {
            return this.ValidateHair(m.Female, itemID);
        }

        public abstract bool ValidateHair(bool female, int itemID);

        public virtual int RandomHair(Mobile m)
        {
            return this.RandomHair(m.Female);
        }

        public abstract int RandomHair(bool female);

        public virtual bool ValidateFacialHair(Mobile m, int itemID)
        {
            return this.ValidateFacialHair(m.Female, itemID);
        }

        public abstract bool ValidateFacialHair(bool female, int itemID);

        public virtual int RandomFacialHair(Mobile m)
        {
            return this.RandomFacialHair(m.Female);
        }

        public abstract int RandomFacialHair(bool female);//For the *ahem* bearded ladies

        public abstract int ClipSkinHue(int hue);

        public abstract int RandomSkinHue();

        public abstract int ClipHairHue(int hue);

        public abstract int RandomHairHue();

        public virtual int Body(Mobile m)
        {
            if (m.Alive)
                return this.AliveBody(m.Female);

            return this.GhostBody(m.Female);
        }

        public virtual int AliveBody(Mobile m)
        {
            return this.AliveBody(m.Female);
        }

        public virtual int AliveBody(bool female)
        {
            return (female ? this.m_FemaleBody : this.m_MaleBody);
        }

        public virtual int GhostBody(Mobile m)
        {
            return this.GhostBody(m.Female);
        }

        public virtual int GhostBody(bool female)
        {
            return (female ? this.m_FemaleGhostBody : this.m_MaleGhostBody);
        }

        private static void CheckNamesAndValues()
        {
            if (m_RaceNames != null && m_RaceNames.Length == m_AllRaces.Count)
                return;

            m_RaceNames = new string[m_AllRaces.Count];
            m_RaceValues = new Race[m_AllRaces.Count];

            for (int i = 0; i < m_AllRaces.Count; ++i)
            {
                Race race = m_AllRaces[i];

                m_RaceNames[i] = race.Name;
                m_RaceValues[i] = race;
            }
        }
    }
}