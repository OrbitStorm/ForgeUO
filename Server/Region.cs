using System;
using System.Collections.Generic;
using System.Xml;
using Server.Network;
using Server.Targeting;

namespace Server
{
    public enum MusicName
    {
        Invalid = -1,
        OldUlt01 = 0,
        Create1,
        DragFlit,
        OldUlt02,
        OldUlt03,
        OldUlt04,
        OldUlt05,
        OldUlt06,
        Stones2,
        Britain1,
        Britain2,
        Bucsden,
        Jhelom,
        LBCastle,
        Linelle,
        Magincia,
        Minoc,
        Ocllo,
        Samlethe,
        Serpents,
        Skarabra,
        Trinsic,
        Vesper,
        Wind,
        Yew,
        Cave01,
        Dungeon9,
        Forest_a,
        InTown01,
        Jungle_a,
        Mountn_a,
        Plains_a,
        Sailing,
        Swamp_a,
        Tavern01,
        Tavern02,
        Tavern03,
        Tavern04,
        Combat1,
        Combat2,
        Combat3,
        Approach,
        Death,
        Victory,
        BTCastle,
        Nujelm,
        Dungeon2,
        Cove,
        Moonglow,
        Zento,
        TokunoDungeon,
        Taiko,
        DreadHornArea,
        ElfCity,
        GrizzleDungeon,
        MelisandesLair,
        ParoxysmusLair,
        GwennoConversation,
        GoodEndGame,
        GoodVsEvil,
        GreatEarthSerpents,
        Humanoids_U9,
        MinocNegative,
        Paws,
        SelimsBar,
        SerpentIsleCombat_U7,
        ValoriaShips
    }

    public class Region : IComparable
    {
        public static readonly int DefaultPriority = 50;
        public static readonly int MinZ = sbyte.MinValue;
        public static readonly int MaxZ = sbyte.MaxValue + 1;
        private static readonly List<Region> m_Regions = new List<Region>();
        private static Type m_DefaultRegionType = typeof(Region);
        private static TimeSpan m_StaffLogoutDelay = TimeSpan.FromSeconds(10.0);
        private static TimeSpan m_DefaultLogoutDelay = TimeSpan.FromMinutes(5.0);
        private readonly string m_Name;
        private readonly Map m_Map;
        private readonly Region m_Parent;
        private readonly List<Region> m_Children = new List<Region>();
        private readonly Rectangle3D[] m_Area;
        private readonly bool m_Dynamic;
        private readonly int m_Priority;
        private readonly int m_ChildLevel;
        private Sector[] m_Sectors;
        private bool m_Registered;
        private Point3D m_GoLocation;
        private MusicName m_Music;
        public Region(string name, Map map, int priority, params Rectangle2D[] area)
            : this(name, map, priority, ConvertTo3D(area))
        {
        }

        public Region(string name, Map map, int priority, params Rectangle3D[] area)
            : this(name, map, null, area)
        {
            this.m_Priority = priority;
        }

        public Region(string name, Map map, Region parent, params Rectangle2D[] area)
            : this(name, map, parent, ConvertTo3D(area))
        {
        }

        public Region(string name, Map map, Region parent, params Rectangle3D[] area)
        {
            this.m_Name = name;
            this.m_Map = map;
            this.m_Parent = parent;
            this.m_Area = area;
            this.m_Dynamic = true;
            this.m_Music = this.DefaultMusic;

            if (this.m_Parent == null)
            {
                this.m_ChildLevel = 0;
                this.m_Priority = DefaultPriority;
            }
            else
            {
                this.m_ChildLevel = this.m_Parent.ChildLevel + 1;
                this.m_Priority = this.m_Parent.Priority;
            }
        }

        public Region(XmlElement xml, Map map, Region parent)
        {
            this.m_Map = map;
            this.m_Parent = parent;
            this.m_Dynamic = false;

            if (this.m_Parent == null)
            {
                this.m_ChildLevel = 0;
                this.m_Priority = DefaultPriority;
            }
            else
            {
                this.m_ChildLevel = this.m_Parent.ChildLevel + 1;
                this.m_Priority = this.m_Parent.Priority;
            }

            ReadString(xml, "name", ref this.m_Name, false);

            if (parent == null)
                ReadInt32(xml, "priority", ref this.m_Priority, false);

            int minZ = MinZ;
            int maxZ = MaxZ;

            XmlElement zrange = xml["zrange"];
            ReadInt32(zrange, "min", ref minZ, false);
            ReadInt32(zrange, "max", ref maxZ, false);

            List<Rectangle3D> area = new List<Rectangle3D>();
            foreach (XmlElement xmlRect in xml.SelectNodes("rect"))
            {
                Rectangle3D rect = new Rectangle3D();
                if (ReadRectangle3D(xmlRect, minZ, maxZ, ref rect))
                    area.Add(rect);
            }

            this.m_Area = area.ToArray();

            if (this.m_Area.Length == 0)
                Console.WriteLine("Empty area for region '{0}'", this);

            if (!ReadPoint3D(xml["go"], map, ref this.m_GoLocation, false) && this.m_Area.Length > 0)
            {
                Point3D start = this.m_Area[0].Start;
                Point3D end = this.m_Area[0].End;

                int x = start.X + (end.X - start.X) / 2;
                int y = start.Y + (end.Y - start.Y) / 2;

                this.m_GoLocation = new Point3D(x, y, this.m_Map.GetAverageZ(x, y));
            }

            MusicName music = this.DefaultMusic;

            ReadEnum(xml["music"], "name", ref music, false);

            this.m_Music = music;
        }

        public static List<Region> Regions
        {
            get
            {
                return m_Regions;
            }
        }
        public static Type DefaultRegionType
        {
            get
            {
                return m_DefaultRegionType;
            }
            set
            {
                m_DefaultRegionType = value;
            }
        }
        public static TimeSpan StaffLogoutDelay
        {
            get
            {
                return m_StaffLogoutDelay;
            }
            set
            {
                m_StaffLogoutDelay = value;
            }
        }
        public static TimeSpan DefaultLogoutDelay
        {
            get
            {
                return m_DefaultLogoutDelay;
            }
            set
            {
                m_DefaultLogoutDelay = value;
            }
        }
        public string Name
        {
            get
            {
                return this.m_Name;
            }
        }
        public Map Map
        {
            get
            {
                return this.m_Map;
            }
        }
        public Region Parent
        {
            get
            {
                return this.m_Parent;
            }
        }
        public List<Region> Children
        {
            get
            {
                return this.m_Children;
            }
        }
        public Rectangle3D[] Area
        {
            get
            {
                return this.m_Area;
            }
        }
        public Sector[] Sectors
        {
            get
            {
                return this.m_Sectors;
            }
        }
        public bool Dynamic
        {
            get
            {
                return this.m_Dynamic;
            }
        }
        public int Priority
        {
            get
            {
                return this.m_Priority;
            }
        }
        public int ChildLevel
        {
            get
            {
                return this.m_ChildLevel;
            }
        }
        public bool Registered
        {
            get
            {
                return this.m_Registered;
            }
        }
        public Point3D GoLocation
        {
            get
            {
                return this.m_GoLocation;
            }
            set
            {
                this.m_GoLocation = value;
            }
        }
        public MusicName Music
        {
            get
            {
                return this.m_Music;
            }
            set
            {
                this.m_Music = value;
            }
        }
        public bool IsDefault
        {
            get
            {
                return this.m_Map.DefaultRegion == this;
            }
        }
        public virtual MusicName DefaultMusic
        {
            get
            {
                return this.m_Parent != null ? this.m_Parent.Music : MusicName.Invalid;
            }
        }
        public static Region Find(Point3D p, Map map)
        {
            if (map == null)
                return Map.Internal.DefaultRegion;

            Sector sector = map.GetSector(p);
            List<RegionRect> list = sector.RegionRects;

            for (int i = 0; i < list.Count; ++i)
            {
                RegionRect regRect = list[i];

                if (regRect.Contains(p))
                    return regRect.Region;
            }

            return map.DefaultRegion;
        }

        public static Rectangle3D ConvertTo3D(Rectangle2D rect)
        {
            return new Rectangle3D(new Point3D(rect.Start, MinZ), new Point3D(rect.End, MaxZ));
        }

        public static Rectangle3D[] ConvertTo3D(Rectangle2D[] rects)
        {
            Rectangle3D[] ret = new Rectangle3D[rects.Length];

            for (int i = 0; i < ret.Length; i++)
            {
                ret[i] = ConvertTo3D(rects[i]);
            }

            return ret;
        }

        public static bool ReadString(XmlElement xml, string attribute, ref string value)
        {
            return ReadString(xml, attribute, ref value, true);
        }

        public static bool ReadString(XmlElement xml, string attribute, ref string value, bool mandatory)
        {
            string s = GetAttribute(xml, attribute, mandatory);

            if (s == null)
                return false;

            value = s;
            return true;
        }

        public static bool ReadInt32(XmlElement xml, string attribute, ref int value)
        {
            return ReadInt32(xml, attribute, ref value, true);
        }

        public static bool ReadInt32(XmlElement xml, string attribute, ref int value, bool mandatory)
        {
            string s = GetAttribute(xml, attribute, mandatory);

            if (s == null)
                return false;

            try
            {
                value = XmlConvert.ToInt32(s);
            }
            catch
            {
                Console.WriteLine("Could not parse integer attribute '{0}' in element '{1}'", attribute, xml.Name);
                return false;
            }

            return true;
        }

        public static bool ReadBoolean(XmlElement xml, string attribute, ref bool value)
        {
            return ReadBoolean(xml, attribute, ref value, true);
        }

        public static bool ReadBoolean(XmlElement xml, string attribute, ref bool value, bool mandatory)
        {
            string s = GetAttribute(xml, attribute, mandatory);

            if (s == null)
                return false;

            try
            {
                value = XmlConvert.ToBoolean(s);
            }
            catch
            {
                Console.WriteLine("Could not parse boolean attribute '{0}' in element '{1}'", attribute, xml.Name);
                return false;
            }

            return true;
        }

        public static bool ReadDateTime(XmlElement xml, string attribute, ref DateTime value)
        {
            return ReadDateTime(xml, attribute, ref value, true);
        }

        public static bool ReadDateTime(XmlElement xml, string attribute, ref DateTime value, bool mandatory)
        {
            string s = GetAttribute(xml, attribute, mandatory);

            if (s == null)
                return false;

            try
            {
                value = XmlConvert.ToDateTime(s, XmlDateTimeSerializationMode.Local);
            }
            catch
            {
                Console.WriteLine("Could not parse DateTime attribute '{0}' in element '{1}'", attribute, xml.Name);
                return false;
            }

            return true;
        }

        public static bool ReadTimeSpan(XmlElement xml, string attribute, ref TimeSpan value)
        {
            return ReadTimeSpan(xml, attribute, ref value, true);
        }

        public static bool ReadTimeSpan(XmlElement xml, string attribute, ref TimeSpan value, bool mandatory)
        {
            string s = GetAttribute(xml, attribute, mandatory);

            if (s == null)
                return false;

            try
            {
                value = XmlConvert.ToTimeSpan(s);
            }
            catch
            {
                Console.WriteLine("Could not parse TimeSpan attribute '{0}' in element '{1}'", attribute, xml.Name);
                return false;
            }

            return true;
        }

        public static bool ReadEnum<T>(XmlElement xml, string attribute, ref T value) where T : struct
        {
            return ReadEnum(xml, attribute, ref value, true);
        }

        public static bool ReadEnum<T>(XmlElement xml, string attribute, ref T value, bool mandatory) where T : struct // We can't limit the where clause to Enums only
        {
            string s = GetAttribute(xml, attribute, mandatory);

            if (s == null)
                return false;

            Type type = typeof(T);

            T tempVal;

            if (type.IsEnum && Enum.TryParse(s, true, out tempVal))
            {
                value = tempVal;
                return true;
            }
            else
            {
                Console.WriteLine("Could not parse {0} enum attribute '{1}' in element '{2}'", type, attribute, xml.Name);
                return false;
            }
        }

        public static bool ReadMap(XmlElement xml, string attribute, ref Map value)
        {
            return ReadMap(xml, attribute, ref value, true);
        }

        public static bool ReadMap(XmlElement xml, string attribute, ref Map value, bool mandatory)
        {
            string s = GetAttribute(xml, attribute, mandatory);

            if (s == null)
                return false;

            try
            {
                value = Map.Parse(s);
            }
            catch
            {
                Console.WriteLine("Could not parse Map attribute '{0}' in element '{1}'", attribute, xml.Name);
                return false;
            }

            return true;
        }

        public static bool ReadType(XmlElement xml, string attribute, ref Type value)
        {
            return ReadType(xml, attribute, ref value, true);
        }

        public static bool ReadType(XmlElement xml, string attribute, ref Type value, bool mandatory)
        {
            string s = GetAttribute(xml, attribute, mandatory);

            if (s == null)
                return false;

            Type type;
            try
            {
                type = ScriptCompiler.FindTypeByName(s, false);
            }
            catch
            {
                Console.WriteLine("Could not parse Type attribute '{0}' in element '{1}'", attribute, xml.Name);
                return false;
            }

            if (type == null)
            {
                Console.WriteLine("Could not find Type '{0}'", s);
                return false;
            }

            value = type;
            return true;
        }

        public static bool ReadPoint3D(XmlElement xml, Map map, ref Point3D value)
        {
            return ReadPoint3D(xml, map, ref value, true);
        }

        public static bool ReadPoint3D(XmlElement xml, Map map, ref Point3D value, bool mandatory)
        {
            int x = 0, y = 0, z = 0;

            bool xyOk = ReadInt32(xml, "x", ref x, mandatory) & ReadInt32(xml, "y", ref y, mandatory);
            bool zOk = ReadInt32(xml, "z", ref z, mandatory && map == null);

            if (xyOk && (zOk || map != null))
            {
                if (!zOk)
                    z = map.GetAverageZ(x, y);

                value = new Point3D(x, y, z);
                return true;
            }

            return false;
        }

        public static bool ReadRectangle3D(XmlElement xml, int defaultMinZ, int defaultMaxZ, ref Rectangle3D value)
        {
            return ReadRectangle3D(xml, defaultMinZ, defaultMaxZ, ref value, true);
        }

        public static bool ReadRectangle3D(XmlElement xml, int defaultMinZ, int defaultMaxZ, ref Rectangle3D value, bool mandatory)
        {
            int x1 = 0, y1 = 0, x2 = 0, y2 = 0;

            if (xml.HasAttribute("x"))
            {
                if (ReadInt32(xml, "x", ref x1, mandatory) &
                    ReadInt32(xml, "y", ref y1, mandatory) &
                    ReadInt32(xml, "width", ref x2, mandatory) &
                    ReadInt32(xml, "height", ref y2, mandatory))
                {
                    x2 += x1;
                    y2 += y1;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (!ReadInt32(xml, "x1", ref x1, mandatory) |
                    !ReadInt32(xml, "y1", ref y1, mandatory) |
                    !ReadInt32(xml, "x2", ref x2, mandatory) |
                    !ReadInt32(xml, "y2", ref y2, mandatory))
                {
                    return false;
                }
            }

            int z1 = defaultMinZ;
            int z2 = defaultMaxZ;

            ReadInt32(xml, "zmin", ref z1, false);
            ReadInt32(xml, "zmax", ref z2, false);

            value = new Rectangle3D(new Point3D(x1, y1, z1), new Point3D(x2, y2, z2));

            return true;
        }

        public void Register()
        {
            if (this.m_Registered)
                return;

            this.OnRegister();

            this.m_Registered = true;

            if (this.m_Parent != null)
            {
                this.m_Parent.m_Children.Add(this);
                this.m_Parent.OnChildAdded(this);
            }

            m_Regions.Add(this);

            this.m_Map.RegisterRegion(this);

            List<Sector> sectors = new List<Sector>();

            for (int i = 0; i < this.m_Area.Length; i++)
            {
                Rectangle3D rect = this.m_Area[i];

                Point2D start = this.m_Map.Bound(new Point2D(rect.Start));
                Point2D end = this.m_Map.Bound(new Point2D(rect.End));

                Sector startSector = this.m_Map.GetSector(start);
                Sector endSector = this.m_Map.GetSector(end);

                for (int x = startSector.X; x <= endSector.X; x++)
                {
                    for (int y = startSector.Y; y <= endSector.Y; y++)
                    {
                        Sector sector = this.m_Map.GetRealSector(x, y);

                        sector.OnEnter(this, rect);

                        if (!sectors.Contains(sector))
                            sectors.Add(sector);
                    }
                }
            }

            this.m_Sectors = sectors.ToArray();
        }

        public void Unregister()
        {
            if (!this.m_Registered)
                return;

            this.OnUnregister();

            this.m_Registered = false;

            if (this.m_Children.Count > 0)
                Console.WriteLine("Warning: Unregistering region '{0}' with children", this);

            if (this.m_Parent != null)
            {
                this.m_Parent.m_Children.Remove(this);
                this.m_Parent.OnChildRemoved(this);
            }

            m_Regions.Remove(this);

            this.m_Map.UnregisterRegion(this);

            if (this.m_Sectors != null)
            {
                for (int i = 0; i < this.m_Sectors.Length; i++)
                    this.m_Sectors[i].OnLeave(this);
            }

            this.m_Sectors = null;
        }

        public bool Contains(Point3D p)
        {
            for (int i = 0; i < this.m_Area.Length; i++)
            {
                Rectangle3D rect = this.m_Area[i];

                if (rect.Contains(p))
                    return true;
            }

            return false;
        }

        public bool IsChildOf(Region region)
        {
            if (region == null)
                return false;

            Region p = this.m_Parent;

            while (p != null)
            {
                if (p == region)
                    return true;

                p = p.m_Parent;
            }

            return false;
        }

        public Region GetRegion(Type regionType)
        {
            if (regionType == null)
                return null;

            Region r = this;

            do
            {
                if (regionType.IsAssignableFrom(r.GetType()))
                    return r;

                r = r.m_Parent;
            }
            while (r != null);

            return null;
        }

        public Region GetRegion(string regionName)
        {
            if (regionName == null)
                return null;

            Region r = this;

            do
            {
                if (r.m_Name == regionName)
                    return r;

                r = r.m_Parent;
            }
            while (r != null);

            return null;
        }

        public bool IsPartOf(Region region)
        {
            if (this == region)
                return true;

            return this.IsChildOf(region);
        }

        public bool IsPartOf(Type regionType)
        {
            return (this.GetRegion(regionType) != null);
        }

        public bool IsPartOf(string regionName)
        {
            return (this.GetRegion(regionName) != null);
        }

        public virtual bool AcceptsSpawnsFrom(Region region)
        {
            if (!this.AllowSpawn())
                return false;

            if (region == this)
                return true;

            if (this.m_Parent != null)
                return this.m_Parent.AcceptsSpawnsFrom(region);

            return false;
        }

        public List<Mobile> GetPlayers()
        {
            List<Mobile> list = new List<Mobile>();

            if (this.m_Sectors != null)
            {
                for (int i = 0; i < this.m_Sectors.Length; i++)
                {
                    Sector sector = this.m_Sectors[i];

                    foreach (Mobile player in sector.Players)
                    {
                        if (player.Region.IsPartOf(this))
                            list.Add(player);
                    }
                }
            }

            return list;
        }

        public int GetPlayerCount()
        {
            int count = 0;

            if (this.m_Sectors != null)
            {
                for (int i = 0; i < this.m_Sectors.Length; i++)
                {
                    Sector sector = this.m_Sectors[i];

                    foreach (Mobile player in sector.Players)
                    {
                        if (player.Region.IsPartOf(this))
                            count++;
                    }
                }
            }

            return count;
        }

        public List<Mobile> GetMobiles()
        {
            List<Mobile> list = new List<Mobile>();

            if (this.m_Sectors != null)
            {
                for (int i = 0; i < this.m_Sectors.Length; i++)
                {
                    Sector sector = this.m_Sectors[i];

                    foreach (Mobile mobile in sector.Mobiles)
                    {
                        if (mobile.Region.IsPartOf(this))
                            list.Add(mobile);
                    }
                }
            }

            return list;
        }

        public int GetMobileCount()
        {
            int count = 0;

            if (this.m_Sectors != null)
            {
                for (int i = 0; i < this.m_Sectors.Length; i++)
                {
                    Sector sector = this.m_Sectors[i];

                    foreach (Mobile mobile in sector.Mobiles)
                    {
                        if (mobile.Region.IsPartOf(this))
                            count++;
                    }
                }
            }

            return count;
        }

        public override string ToString()
        {
            if (this.m_Name != null)
                return this.m_Name;
            else
                return this.GetType().Name;
        }

        public virtual void OnRegister()
        {
        }

        public virtual void OnUnregister()
        {
        }

        public virtual void OnChildAdded(Region child)
        {
        }

        public virtual void OnChildRemoved(Region child)
        {
        }

        public virtual bool OnMoveInto(Mobile m, Direction d, Point3D newLocation, Point3D oldLocation)
        {
            return (m.WalkRegion == null || this.AcceptsSpawnsFrom(m.WalkRegion));
        }

        public virtual void OnEnter(Mobile m)
        {
        }

        public virtual void OnExit(Mobile m)
        {
        }

        public virtual void MakeGuard(Mobile focus)
        {
            if (this.m_Parent != null)
                this.m_Parent.MakeGuard(focus);
        }

        public virtual Type GetResource(Type type)
        {
            if (this.m_Parent != null)
                return this.m_Parent.GetResource(type);

            return type;
        }

        public virtual bool CanUseStuckMenu(Mobile m)
        {
            if (this.m_Parent != null)
                return this.m_Parent.CanUseStuckMenu(m);

            return true;
        }

        public virtual void OnAggressed(Mobile aggressor, Mobile aggressed, bool criminal)
        {
            if (this.m_Parent != null)
                this.m_Parent.OnAggressed(aggressor, aggressed, criminal);
        }

        public virtual void OnDidHarmful(Mobile harmer, Mobile harmed)
        {
            if (this.m_Parent != null)
                this.m_Parent.OnDidHarmful(harmer, harmed);
        }

        public virtual void OnGotHarmful(Mobile harmer, Mobile harmed)
        {
            if (this.m_Parent != null)
                this.m_Parent.OnGotHarmful(harmer, harmed);
        }

        public virtual void OnLocationChanged(Mobile m, Point3D oldLocation)
        {
            if (this.m_Parent != null)
                this.m_Parent.OnLocationChanged(m, oldLocation);
        }

        public virtual bool OnTarget(Mobile m, Target t, object o)
        {
            if (this.m_Parent != null)
                return this.m_Parent.OnTarget(m, t, o);

            return true;
        }

        public virtual bool OnCombatantChange(Mobile m, Mobile Old, Mobile New)
        {
            if (this.m_Parent != null)
                return this.m_Parent.OnCombatantChange(m, Old, New);

            return true;
        }

        public virtual bool AllowHousing(Mobile from, Point3D p)
        {
            if (this.m_Parent != null)
                return this.m_Parent.AllowHousing(from, p);

            return true;
        }

        public virtual bool SendInaccessibleMessage(Item item, Mobile from)
        {
            if (this.m_Parent != null)
                return this.m_Parent.SendInaccessibleMessage(item, from);

            return false;
        }

        public virtual bool CheckAccessibility(Item item, Mobile from)
        {
            if (this.m_Parent != null)
                return this.m_Parent.CheckAccessibility(item, from);

            return true;
        }

        public virtual bool OnDecay(Item item)
        {
            if (this.m_Parent != null)
                return this.m_Parent.OnDecay(item);

            return true;
        }

        public virtual bool AllowHarmful(Mobile from, Mobile target)
        {
            if (this.m_Parent != null)
                return this.m_Parent.AllowHarmful(from, target);

            if (Mobile.AllowHarmfulHandler != null)
                return Mobile.AllowHarmfulHandler(from, target);

            return true;
        }

        public virtual void OnCriminalAction(Mobile m, bool message)
        {
            if (this.m_Parent != null)
                this.m_Parent.OnCriminalAction(m, message);
            else if (message)
                m.SendLocalizedMessage(1005040); // You've committed a criminal act!!
        }

        public virtual bool AllowBeneficial(Mobile from, Mobile target)
        {
            if (this.m_Parent != null)
                return this.m_Parent.AllowBeneficial(from, target);

            if (Mobile.AllowBeneficialHandler != null)
                return Mobile.AllowBeneficialHandler(from, target);

            return true;
        }

        public virtual void OnBeneficialAction(Mobile helper, Mobile target)
        {
            if (this.m_Parent != null)
                this.m_Parent.OnBeneficialAction(helper, target);
        }

        public virtual void OnGotBeneficialAction(Mobile helper, Mobile target)
        {
            if (this.m_Parent != null)
                this.m_Parent.OnGotBeneficialAction(helper, target);
        }

        public virtual void SpellDamageScalar(Mobile caster, Mobile target, ref double damage)
        {
            if (this.m_Parent != null)
                this.m_Parent.SpellDamageScalar(caster, target, ref damage);
        }

        public virtual void OnSpeech(SpeechEventArgs args)
        {
            if (this.m_Parent != null)
                this.m_Parent.OnSpeech(args);
        }

        public virtual bool OnSkillUse(Mobile m, int Skill)
        {
            if (this.m_Parent != null)
                return this.m_Parent.OnSkillUse(m, Skill);

            return true;
        }

        public virtual bool OnBeginSpellCast(Mobile m, ISpell s)
        {
            if (this.m_Parent != null)
                return this.m_Parent.OnBeginSpellCast(m, s);

            return true;
        }

        public virtual void OnSpellCast(Mobile m, ISpell s)
        {
            if (this.m_Parent != null)
                this.m_Parent.OnSpellCast(m, s);
        }

        public virtual bool OnResurrect(Mobile m)
        {
            if (this.m_Parent != null)
                return this.m_Parent.OnResurrect(m);

            return true;
        }

        public virtual bool OnBeforeDeath(Mobile m)
        {
            if (this.m_Parent != null)
                return this.m_Parent.OnBeforeDeath(m);

            return true;
        }

        public virtual void OnDeath(Mobile m)
        {
            if (this.m_Parent != null)
                this.m_Parent.OnDeath(m);
        }

        public virtual bool OnDamage(Mobile m, ref int Damage)
        {
            if (this.m_Parent != null)
                return this.m_Parent.OnDamage(m, ref Damage);

            return true;
        }

        public virtual bool OnHeal(Mobile m, ref int Heal)
        {
            if (this.m_Parent != null)
                return this.m_Parent.OnHeal(m, ref Heal);

            return true;
        }

        public virtual bool OnDoubleClick(Mobile m, object o)
        {
            if (this.m_Parent != null)
                return this.m_Parent.OnDoubleClick(m, o);

            return true;
        }

        public virtual bool OnSingleClick(Mobile m, object o)
        {
            if (this.m_Parent != null)
                return this.m_Parent.OnSingleClick(m, o);

            return true;
        }

        public virtual bool AllowSpawn()
        {
            if (this.m_Parent != null)
                return this.m_Parent.AllowSpawn();

            return true;
        }

        public virtual void AlterLightLevel(Mobile m, ref int global, ref int personal)
        {
            if (this.m_Parent != null)
                this.m_Parent.AlterLightLevel(m, ref global, ref personal);
        }

        public virtual TimeSpan GetLogoutDelay(Mobile m)
        {
            if (this.m_Parent != null)
                return this.m_Parent.GetLogoutDelay(m);
            else if (m.IsStaff())
                return m_StaffLogoutDelay;
            else
                return m_DefaultLogoutDelay;
        }

        internal static bool CanMove(Mobile m, Direction d, Point3D newLocation, Point3D oldLocation, Map map)
        {
            Region oldRegion = m.Region;
            Region newRegion = Find(newLocation, map);

            while (oldRegion != newRegion)
            {
                if (!newRegion.OnMoveInto(m, d, newLocation, oldLocation))
                    return false;

                if (newRegion.m_Parent == null)
                    return true;

                newRegion = newRegion.m_Parent;
            }

            return true;
        }

        internal static void OnRegionChange(Mobile m, Region oldRegion, Region newRegion)
        {
            if (newRegion != null && m.NetState != null)
            {
                m.CheckLightLevels(false);

                if (oldRegion == null || oldRegion.Music != newRegion.Music)
                {
                    m.Send(PlayMusic.GetInstance(newRegion.Music));
                }
            }

            Region oldR = oldRegion;
            Region newR = newRegion;

            while (oldR != newR)
            {
                int oldRChild = (oldR != null ? oldR.ChildLevel : -1);
                int newRChild = (newR != null ? newR.ChildLevel : -1);

                if (oldRChild >= newRChild)
                {
                    oldR.OnExit(m);
                    oldR = oldR.Parent;
                }

                if (newRChild >= oldRChild)
                {
                    newR.OnEnter(m);
                    newR = newR.Parent;

                    EventSink.InvokeOnEnterRegion(new OnEnterRegionEventArgs(m, newR));
                }
            }
        }

        internal static void Load()
        {
            if (!System.IO.File.Exists("Data/Regions.xml"))
            {
                Utility.PushColor(ConsoleColor.Red);
                Console.WriteLine("Error: Data/Regions.xml does not exist");
                Utility.PopColor();
                return;
            }

            Utility.PushColor(ConsoleColor.Yellow);
            Console.Write("Regions: Loading...");
            Utility.PopColor();

            XmlDocument doc = new XmlDocument();
            doc.Load(System.IO.Path.Combine(Core.BaseDirectory, "Data/Regions.xml"));

            XmlElement root = doc["ServerRegions"];

            if (root == null)
            {
                Utility.PushColor(ConsoleColor.Red);
                Console.WriteLine("Could not find root element 'ServerRegions' in Regions.xml");
                Utility.PopColor();
            }
            else
            {
                foreach (XmlElement facet in root.SelectNodes("Facet"))
                {
                    Map map = null;
                    if (ReadMap(facet, "name", ref map))
                    {
                        if (map == Map.Internal)
                        {
                            Utility.PushColor(ConsoleColor.Red);
                            Console.WriteLine("Invalid internal map in a facet element");
                            Utility.PopColor();
                        }
                        else
                            LoadRegions(facet, map, null);
                    }
                }
            }

            Utility.PushColor(ConsoleColor.Green);
            Console.WriteLine("done");
            Utility.PopColor();
        }

        protected static string GetAttribute(XmlElement xml, string attribute, bool mandatory)
        {
            if (xml == null)
            {
                if (mandatory)
                    Console.WriteLine("Missing element for attribute '{0}'", attribute);

                return null;
            }
            else if (xml.HasAttribute(attribute))
            {
                return xml.GetAttribute(attribute);
            }
            else
            {
                if (mandatory)
                    Console.WriteLine("Missing attribute '{0}' in element '{1}'", attribute, xml.Name);

                return null;
            }
        }

        private static void LoadRegions(XmlElement xml, Map map, Region parent)
        {
            foreach (XmlElement xmlReg in xml.SelectNodes("region"))
            {
                Type type = DefaultRegionType;

                ReadType(xmlReg, "type", ref type, false);

                if (!typeof(Region).IsAssignableFrom(type))
                {
                    Console.WriteLine("Invalid region type '{0}' in regions.xml", type.FullName);
                    continue;
                }

                Region region = null;
                try
                {
                    region = (Region)Activator.CreateInstance(type, new object[] { xmlReg, map, parent });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error during the creation of region type '{0}': {1}", type.FullName, ex);
                    continue;
                }

                region.Register();

                LoadRegions(xmlReg, map, region);
            }
        }

        int IComparable.CompareTo(object obj)
        {
            if (obj == null)
                return 1;

            Region reg = obj as Region;

            if (reg == null)
                throw new ArgumentException("obj is not a Region", "obj");

            // Dynamic regions go first
            if (this.Dynamic)
            {
                if (!reg.Dynamic)
                    return -1;
            }
            else if (reg.Dynamic)
            {
                return 1;
            }

            int thisPriority = this.Priority;
            int regPriority = reg.Priority;

            if (thisPriority != regPriority)
                return (regPriority - thisPriority);

            return (reg.ChildLevel - this.ChildLevel);
        }
    }
}