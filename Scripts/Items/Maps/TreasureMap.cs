using System;
using System.Collections.Generic;
using System.IO;
using Server.ContextMenus;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Items
{
    public class TreasureMap : MapItem
    {
        public const double LootChance = 0.01;// 1% chance to appear as loot
        private static readonly Type[][] m_SpawnTypes = new Type[][]
        {
            new Type[] { typeof(HeadlessOne), typeof(Skeleton) },
            new Type[] { typeof(Mongbat), typeof(Ratman), typeof(HeadlessOne), typeof(Skeleton), typeof(Zombie) },
            new Type[] { typeof(OrcishMage), typeof(Gargoyle), typeof(Gazer), typeof(HellHound), typeof(EarthElemental) },
            new Type[] { typeof(Lich), typeof(OgreLord), typeof(DreadSpider), typeof(AirElemental), typeof(FireElemental) },
            new Type[] { typeof(DreadSpider), typeof(LichLord), typeof(Daemon), typeof(ElderGazer), typeof(OgreLord) },
            new Type[] { typeof(LichLord), typeof(Daemon), typeof(ElderGazer), typeof(PoisonElemental), typeof(BloodElemental) },
            new Type[] { typeof(AncientWyrm), typeof(Balron), typeof(BloodElemental), typeof(PoisonElemental), typeof(Titan) }
        };
        private static Point2D[] m_Locations;
        private static Point2D[] m_HavenLocations;
        private int m_Level;
        private bool m_Completed;
        private Mobile m_CompletedBy;
        private Mobile m_Decoder;
        private Map m_Map;
        private Point2D m_Location;
        [Constructable]
        public TreasureMap(int level, Map map)
        {
            this.m_Level = level;
            this.m_Map = map;

            if (level == 0)
                this.m_Location = GetRandomHavenLocation();
            else
                this.m_Location = GetRandomLocation();

            this.Width = 300;
            this.Height = 300;

            int width = 600;
            int height = 600;

            int x1 = this.m_Location.X - Utility.RandomMinMax(width / 4, (width / 4) * 3);
            int y1 = this.m_Location.Y - Utility.RandomMinMax(height / 4, (height / 4) * 3);

            if (x1 < 0)
                x1 = 0;

            if (y1 < 0)
                y1 = 0;

            int x2 = x1 + width;
            int y2 = y1 + height;

            if (x2 >= 5120)
                x2 = 5119;

            if (y2 >= 4096)
                y2 = 4095;

            x1 = x2 - width;
            y1 = y2 - height;

            this.Bounds = new Rectangle2D(x1, y1, width, height);
            this.Protected = true;

            this.AddWorldPin(this.m_Location.X, this.m_Location.Y);
        }

        public TreasureMap(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Level
        {
            get
            {
                return this.m_Level;
            }
            set
            {
                this.m_Level = value;
                this.InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool Completed
        {
            get
            {
                return this.m_Completed;
            }
            set
            {
                this.m_Completed = value;
                this.InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile CompletedBy
        {
            get
            {
                return this.m_CompletedBy;
            }
            set
            {
                this.m_CompletedBy = value;
                this.InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Decoder
        {
            get
            {
                return this.m_Decoder;
            }
            set
            {
                this.m_Decoder = value;
                this.InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public Map ChestMap
        {
            get
            {
                return this.m_Map;
            }
            set
            {
                this.m_Map = value;
                this.InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public Point2D ChestLocation
        {
            get
            {
                return this.m_Location;
            }
            set
            {
                this.m_Location = value;
            }
        }
        public override int LabelNumber
        {
            get
            {
                if (this.m_Decoder != null)
                {
                    if (this.m_Level == 6)
                        return 1063453;
                    else
                        return 1041516 + this.m_Level;
                }
                else if (this.m_Level == 6)
                    return 1063452;
                else
                    return 1041510 + this.m_Level;
            }
        }
        public static Point2D GetRandomLocation()
        {
            if (m_Locations == null)
                LoadLocations();

            if (m_Locations.Length > 0)
                return m_Locations[Utility.Random(m_Locations.Length)];

            return Point2D.Zero;
        }

        public static Point2D GetRandomHavenLocation()
        {
            if (m_HavenLocations == null)
                LoadLocations();

            if (m_HavenLocations.Length > 0)
                return m_HavenLocations[Utility.Random(m_HavenLocations.Length)];

            return Point2D.Zero;
        }

        public static bool IsInHavenIsland(IPoint2D loc)
        {
            return (loc.X >= 3314 && loc.X <= 3814 && loc.Y >= 2345 && loc.Y <= 3095);
        }

        public static BaseCreature Spawn(int level, Point3D p, bool guardian)
        {
            if (level >= 0 && level < m_SpawnTypes.Length)
            {
                BaseCreature bc;

                try
                {
                    bc = (BaseCreature)Activator.CreateInstance(m_SpawnTypes[level][Utility.Random(m_SpawnTypes[level].Length)]);
                }
                catch
                {
                    return null;
                }

                bc.Home = p;
                bc.RangeHome = 5;

                if (guardian && level == 0)
                {
                    bc.Name = "a chest guardian";
                    bc.Hue = 0x835;
                }

                return bc;
            }

            return null;
        }

        public static BaseCreature Spawn(int level, Point3D p, Map map, Mobile target, bool guardian)
        {
            if (map == null)
                return null;

            BaseCreature c = Spawn(level, p, guardian);

            if (c != null)
            {
                bool spawned = false;

                for (int i = 0; !spawned && i < 10; ++i)
                {
                    int x = p.X - 3 + Utility.Random(7);
                    int y = p.Y - 3 + Utility.Random(7);

                    if (map.CanSpawnMobile(x, y, p.Z))
                    {
                        c.MoveToWorld(new Point3D(x, y, p.Z), map);
                        spawned = true;
                    }
                    else
                    {
                        int z = map.GetAverageZ(x, y);

                        if (map.CanSpawnMobile(x, y, z))
                        {
                            c.MoveToWorld(new Point3D(x, y, z), map);
                            spawned = true;
                        }
                    }
                }

                if (!spawned)
                {
                    c.Delete();
                    return null;
                }

                if (target != null)
                    c.Combatant = target;

                return c;
            }

            return null;
        }

        public static bool HasDiggingTool(Mobile m)
        {
            if (m.Backpack == null)
                return false;

            List<BaseHarvestTool> items = m.Backpack.FindItemsByType<BaseHarvestTool>();

            foreach (BaseHarvestTool tool in items)
            {
                if (tool.HarvestSystem == Engines.Harvest.Mining.System)
                    return true;
            }

            return false;
        }

        public void OnBeginDig(Mobile from)
        {
            if (this.m_Completed)
            {
                from.SendLocalizedMessage(503028); // The treasure for this map has already been found.
            }
            else if (this.m_Level == 0 && !this.CheckYoung(from))
            {
                from.SendLocalizedMessage(1046447); // Only a young player may use this treasure map.
            }
            /*
            else if ( from != m_Decoder )
            {
            from.SendLocalizedMessage( 503016 ); // Only the person who decoded this map may actually dig up the treasure.
            }
            */
            else if (this.m_Decoder != from && !this.HasRequiredSkill(from))
            {
                from.SendLocalizedMessage(503031); // You did not decode this map and have no clue where to look for the treasure.
            }
            else if (!from.CanBeginAction(typeof(TreasureMap)))
            {
                from.SendLocalizedMessage(503020); // You are already digging treasure.
            }
            else if (from.Map != this.m_Map)
            {
                from.SendLocalizedMessage(1010479); // You seem to be in the right place, but may be on the wrong facet!
            }
            else
            {
                from.SendLocalizedMessage(503033); // Where do you wish to dig?
                from.Target = new DigTarget(this);
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.InRange(this.GetWorldLocation(), 2))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
                return;
            }

            if (!this.m_Completed && this.m_Decoder == null)
                this.Decode(from);
            else
                this.DisplayTo(from);
        }

        public void Decode(Mobile from)
        {
            if (this.m_Completed || this.m_Decoder != null)
                return;

            if (this.m_Level == 0)
            {
                if (!this.CheckYoung(from))
                {
                    from.SendLocalizedMessage(1046447); // Only a young player may use this treasure map.
                    return;
                }
            }
            else
            {
                double minSkill = this.GetMinSkillLevel();

                if (from.Skills[SkillName.Cartography].Value < minSkill)
                    from.SendLocalizedMessage(503013); // The map is too difficult to attempt to decode.

                double maxSkill = minSkill + 60.0;

                if (!from.CheckSkill(SkillName.Cartography, minSkill, maxSkill))
                {
                    from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 503018); // You fail to make anything of the map.
                    return;
                }
            }

            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 503019); // You successfully decode a treasure map!
            this.Decoder = from;

            if (Core.AOS)
                this.LootType = LootType.Blessed;

            this.DisplayTo(from);
        }

        public override void DisplayTo(Mobile from)
        {
            if (this.m_Completed)
            {
                this.SendLocalizedMessageTo(from, 503014); // This treasure hunt has already been completed.
            }
            else if (this.m_Level == 0 && !this.CheckYoung(from))
            {
                from.SendLocalizedMessage(1046447); // Only a young player may use this treasure map.
                return;
            }
            else if (this.m_Decoder != from && !this.HasRequiredSkill(from))
            {
                from.SendLocalizedMessage(503031); // You did not decode this map and have no clue where to look for the treasure.
                return;
            }
            else
            {
                this.SendLocalizedMessageTo(from, 503017); // The treasure is marked by the red pin. Grab a shovel and go dig it up!
            }

            from.PlaySound(0x249);
            base.DisplayTo(from);
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            if (!this.m_Completed)
            {
                if (this.m_Decoder == null)
                {
                    list.Add(new DecodeMapEntry(this));
                }
                else
                {
                    bool digTool = HasDiggingTool(from);

                    list.Add(new OpenMapEntry(this));
                    list.Add(new DigEntry(this, digTool));
                }
            }
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add(this.m_Map == Map.Felucca ? 1041502 : 1041503); // for somewhere in Felucca : for somewhere in Trammel

            if (this.m_Completed)
            {
                list.Add(1041507, this.m_CompletedBy == null ? "someone" : this.m_CompletedBy.Name); // completed by ~1_val~
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            if (this.m_Completed)
            {
                from.Send(new MessageLocalizedAffix(this.Serial, this.ItemID, MessageType.Label, 0x3B2, 3, 1048030, "", AffixType.Append, String.Format(" completed by {0}", this.m_CompletedBy == null ? "someone" : this.m_CompletedBy.Name), ""));
            }
            else if (this.m_Decoder != null)
            {
                if (this.m_Level == 6)
                    this.LabelTo(from, 1063453);
                else
                    this.LabelTo(from, 1041516 + this.m_Level);
            }
            else
            {
                if (this.m_Level == 6)
                    this.LabelTo(from, 1041522, String.Format("#{0}\t \t#{1}", 1063452, this.m_Map == Map.Felucca ? 1041502 : 1041503));
                else
                    this.LabelTo(from, 1041522, String.Format("#{0}\t \t#{1}", 1041510 + this.m_Level, this.m_Map == Map.Felucca ? 1041502 : 1041503));
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1);

            writer.Write((Mobile)this.m_CompletedBy);

            writer.Write(this.m_Level);
            writer.Write(this.m_Completed);
            writer.Write(this.m_Decoder);
            writer.Write(this.m_Map);
            writer.Write(this.m_Location);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch ( version )
            {
                case 1:
                    {
                        this.m_CompletedBy = reader.ReadMobile();

                        goto case 0;
                    }
                case 0:
                    {
                        this.m_Level = (int)reader.ReadInt();
                        this.m_Completed = reader.ReadBool();
                        this.m_Decoder = reader.ReadMobile();
                        this.m_Map = reader.ReadMap();
                        this.m_Location = reader.ReadPoint2D();

                        if (version == 0 && this.m_Completed)
                            this.m_CompletedBy = this.m_Decoder;

                        break;
                    }
            }
            if (Core.AOS && this.m_Decoder != null && this.LootType == LootType.Regular)
                this.LootType = LootType.Blessed;
        }

        private static void LoadLocations()
        {
            string filePath = Path.Combine(Core.BaseDirectory, "Data/treasure.cfg");

            List<Point2D> list = new List<Point2D>();
            List<Point2D> havenList = new List<Point2D>();

            if (File.Exists(filePath))
            {
                using (StreamReader ip = new StreamReader(filePath))
                {
                    string line;

                    while ((line = ip.ReadLine()) != null)
                    {
                        try
                        {
                            string[] split = line.Split(' ');

                            int x = Convert.ToInt32(split[0]), y = Convert.ToInt32(split[1]);

                            Point2D loc = new Point2D(x, y);
                            list.Add(loc);

                            if (IsInHavenIsland(loc))
                                havenList.Add(loc);
                        }
                        catch
                        {
                        }
                    }
                }
            }

            m_Locations = list.ToArray();
            m_HavenLocations = havenList.ToArray();
        }

        private bool CheckYoung(Mobile from)
        {
            if (from.AccessLevel >= AccessLevel.GameMaster)
                return true;

            if (from is PlayerMobile && ((PlayerMobile)from).Young)
                return true;

            if (from == this.Decoder)
            {
                this.Level = 1;
                from.SendLocalizedMessage(1046446); // This is now a level one treasure map.
                return true;
            }

            return false;
        }

        private double GetMinSkillLevel()
        {
            switch ( this.m_Level )
            {
                case 1:
                    return -3.0;
                case 2:
                    return 41.0;
                case 3:
                    return 51.0;
                case 4:
                    return 61.0;
                case 5:
                    return 70.0;
                case 6:
                    return 70.0;

                default:
                    return 0.0;
            }
        }

        private bool HasRequiredSkill(Mobile from)
        {
            return (from.Skills[SkillName.Cartography].Value >= this.GetMinSkillLevel());
        }

        private class DigTarget : Target
        {
            private readonly TreasureMap m_Map;
            public DigTarget(TreasureMap map)
                : base(6, true, TargetFlags.None)
            {
                this.m_Map = map;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (this.m_Map.Deleted)
                    return;

                Map map = this.m_Map.m_Map;

                if (this.m_Map.m_Completed)
                {
                    from.SendLocalizedMessage(503028); // The treasure for this map has already been found.
                }
                /*
                else if ( from != m_Map.m_Decoder )
                {
                from.SendLocalizedMessage( 503016 ); // Only the person who decoded this map may actually dig up the treasure.
                }
                */
                else if (this.m_Map.m_Decoder != from && !this.m_Map.HasRequiredSkill(from))
                {
                    from.SendLocalizedMessage(503031); // You did not decode this map and have no clue where to look for the treasure.
                    return;
                }
                else if (!from.CanBeginAction(typeof(TreasureMap)))
                {
                    from.SendLocalizedMessage(503020); // You are already digging treasure.
                }
                else if (!HasDiggingTool(from))
                {
                    from.SendMessage("You must have a digging tool to dig for treasure.");
                }
                else if (from.Map != map)
                {
                    from.SendLocalizedMessage(1010479); // You seem to be in the right place, but may be on the wrong facet!
                }
                else
                {
                    IPoint3D p = targeted as IPoint3D;

                    Point3D targ3D;
                    if (p is Item)
                        targ3D = ((Item)p).GetWorldLocation();
                    else
                        targ3D = new Point3D(p);

                    int maxRange;
                    double skillValue = from.Skills[SkillName.Mining].Value;

                    if (skillValue >= 100.0)
                        maxRange = 4;
                    else if (skillValue >= 81.0)
                        maxRange = 3;
                    else if (skillValue >= 51.0)
                        maxRange = 2;
                    else
                        maxRange = 1;

                    Point2D loc = this.m_Map.m_Location;
                    int x = loc.X, y = loc.Y;

                    Point3D chest3D0 = new Point3D(loc, 0);

                    if (Utility.InRange(targ3D, chest3D0, maxRange))
                    {
                        if (from.Location.X == x && from.Location.Y == y)
                        {
                            from.SendLocalizedMessage(503030); // The chest can't be dug up because you are standing on top of it.
                        }
                        else if (map != null)
                        {
                            int z = map.GetAverageZ(x, y);

                            if (!map.CanFit(x, y, z, 16, true, true))
                            {
                                from.SendLocalizedMessage(503021); // You have found the treasure chest but something is keeping it from being dug up.
                            }
                            else if (from.BeginAction(typeof(TreasureMap)))
                            {
                                new DigTimer(from, this.m_Map, new Point3D(x, y, z), map).Start();
                            }
                            else
                            {
                                from.SendLocalizedMessage(503020); // You are already digging treasure.
                            }
                        }
                    }
                    else if (this.m_Map.Level > 0)
                    {
                        if (Utility.InRange(targ3D, chest3D0, 8)) // We're close, but not quite
                        {
                            from.SendLocalizedMessage(503032); // You dig and dig but no treasure seems to be here.
                        }
                        else
                        {
                            from.SendLocalizedMessage(503035); // You dig and dig but fail to find any treasure.
                        }
                    }
                    else
                    {
                        if (Utility.InRange(targ3D, chest3D0, 8)) // We're close, but not quite
                        {
                            from.SendAsciiMessage(0x44, "The treasure chest is very close!");
                        }
                        else
                        {
                            Direction dir = Utility.GetDirection(targ3D, chest3D0);

                            string sDir;
                            switch ( dir )
                            {
                                case Direction.North:
                                    sDir = "north";
                                    break;
                                case Direction.Right:
                                    sDir = "northeast";
                                    break;
                                case Direction.East:
                                    sDir = "east";
                                    break;
                                case Direction.Down:
                                    sDir = "southeast";
                                    break;
                                case Direction.South:
                                    sDir = "south";
                                    break;
                                case Direction.Left:
                                    sDir = "southwest";
                                    break;
                                case Direction.West:
                                    sDir = "west";
                                    break;
                                default:
                                    sDir = "northwest";
                                    break;
                            }

                            from.SendAsciiMessage(0x44, "Try looking for the treasure chest more to the {0}.", sDir);
                        }
                    }
                }
            }
        }

        private class DigTimer : Timer
        {
            private readonly Mobile m_From;
            private readonly TreasureMap m_TreasureMap;
            private readonly Point3D m_Location;
            private readonly Map m_Map;
            private readonly DateTime m_NextSkillTime;
            private readonly DateTime m_NextSpellTime;
            private readonly DateTime m_NextActionTime;
            private readonly DateTime m_LastMoveTime;
            private TreasureChestDirt m_Dirt1;
            private TreasureChestDirt m_Dirt2;
            private TreasureMapChest m_Chest;
            private int m_Count;
            public DigTimer(Mobile from, TreasureMap treasureMap, Point3D location, Map map)
                : base(TimeSpan.Zero, TimeSpan.FromSeconds(1.0))
            {
                this.m_From = from;
                this.m_TreasureMap = treasureMap;

                this.m_Location = location;
                this.m_Map = map;

                this.m_NextSkillTime = from.NextSkillTime;
                this.m_NextSpellTime = from.NextSpellTime;
                this.m_NextActionTime = from.NextActionTime;
                this.m_LastMoveTime = from.LastMoveTime;

                this.Priority = TimerPriority.TenMS;
            }

            protected override void OnTick()
            {
                if (this.m_NextSkillTime != this.m_From.NextSkillTime || this.m_NextSpellTime != this.m_From.NextSpellTime || this.m_NextActionTime != this.m_From.NextActionTime)
                {
                    this.Terminate();
                    return;
                }

                if (this.m_LastMoveTime != this.m_From.LastMoveTime)
                {
                    this.m_From.SendLocalizedMessage(503023); // You cannot move around while digging up treasure. You will need to start digging anew.
                    this.Terminate();
                    return;
                }

                int z = (this.m_Chest != null) ? this.m_Chest.Z + this.m_Chest.ItemData.Height : int.MinValue;
                int height = 16;

                if (z > this.m_Location.Z)
                    height -= (z - this.m_Location.Z);
                else
                    z = this.m_Location.Z;

                if (!this.m_Map.CanFit(this.m_Location.X, this.m_Location.Y, z, height, true, true, false))
                {
                    this.m_From.SendLocalizedMessage(503024); // You stop digging because something is directly on top of the treasure chest.
                    this.Terminate();
                    return;
                }

                this.m_Count++;

                this.m_From.RevealingAction();
                this.m_From.Direction = this.m_From.GetDirectionTo(this.m_Location);

                if (this.m_Count > 1 && this.m_Dirt1 == null)
                {
                    this.m_Dirt1 = new TreasureChestDirt();
                    this.m_Dirt1.MoveToWorld(this.m_Location, this.m_Map);

                    this.m_Dirt2 = new TreasureChestDirt();
                    this.m_Dirt2.MoveToWorld(new Point3D(this.m_Location.X, this.m_Location.Y - 1, this.m_Location.Z), this.m_Map);
                }

                if (this.m_Count == 5)
                {
                    this.m_Dirt1.Turn1();
                }
                else if (this.m_Count == 10)
                {
                    this.m_Dirt1.Turn2();
                    this.m_Dirt2.Turn2();
                }
                else if (this.m_Count > 10)
                {
                    if (this.m_Chest == null)
                    {
                        this.m_Chest = new TreasureMapChest(this.m_From, this.m_TreasureMap.Level, true);
                        this.m_Chest.MoveToWorld(new Point3D(this.m_Location.X, this.m_Location.Y, this.m_Location.Z - 15), this.m_Map);
                    }
                    else
                    {
                        this.m_Chest.Z++;
                    }

                    Effects.PlaySound(this.m_Chest, this.m_Map, 0x33B);
                }

                if (this.m_Chest != null && this.m_Chest.Location.Z >= this.m_Location.Z)
                {
                    this.Stop();
                    this.m_From.EndAction(typeof(TreasureMap));

                    this.m_Chest.Temporary = false;
                    this.m_TreasureMap.Completed = true;
                    this.m_TreasureMap.CompletedBy = this.m_From;

                    int spawns;
                    switch ( this.m_TreasureMap.Level )
                    {
                        case 0:
                            spawns = 3;
                            break;
                        case 1:
                            spawns = 0;
                            break;
                        default:
                            spawns = 4;
                            break;
                    }

                    for (int i = 0; i < spawns; ++i)
                    {
                        BaseCreature bc = Spawn(this.m_TreasureMap.Level, this.m_Chest.Location, this.m_Chest.Map, null, true);

                        if (bc != null)
                            this.m_Chest.Guardians.Add(bc);
                    }
                }
                else
                {
                    if (this.m_From.Body.IsHuman && !this.m_From.Mounted)
                        this.m_From.Animate(11, 5, 1, true, false, 0);

                    new SoundTimer(this.m_From, 0x125 + (this.m_Count % 2)).Start();
                }
            }

            private void Terminate()
            {
                this.Stop();
                this.m_From.EndAction(typeof(TreasureMap));

                if (this.m_Chest != null)
                    this.m_Chest.Delete();

                if (this.m_Dirt1 != null)
                {
                    this.m_Dirt1.Delete();
                    this.m_Dirt2.Delete();
                }
            }

            private class SoundTimer : Timer
            {
                private readonly Mobile m_From;
                private readonly int m_SoundID;
                public SoundTimer(Mobile from, int soundID)
                    : base(TimeSpan.FromSeconds(0.9))
                {
                    this.m_From = from;
                    this.m_SoundID = soundID;

                    this.Priority = TimerPriority.TenMS;
                }

                protected override void OnTick()
                {
                    this.m_From.PlaySound(this.m_SoundID);
                }
            }
        }

        private class DecodeMapEntry : ContextMenuEntry
        {
            private readonly TreasureMap m_Map;
            public DecodeMapEntry(TreasureMap map)
                : base(6147, 2)
            {
                this.m_Map = map;
            }

            public override void OnClick()
            {
                if (!this.m_Map.Deleted)
                    this.m_Map.Decode(this.Owner.From);
            }
        }

        private class OpenMapEntry : ContextMenuEntry
        {
            private readonly TreasureMap m_Map;
            public OpenMapEntry(TreasureMap map)
                : base(6150, 2)
            {
                this.m_Map = map;
            }

            public override void OnClick()
            {
                if (!this.m_Map.Deleted)
                    this.m_Map.DisplayTo(this.Owner.From);
            }
        }

        private class DigEntry : ContextMenuEntry
        {
            private readonly TreasureMap m_Map;
            public DigEntry(TreasureMap map, bool enabled)
                : base(6148, 2)
            {
                this.m_Map = map;

                if (!enabled)
                    this.Flags |= CMEFlags.Disabled;
            }

            public override void OnClick()
            {
                if (this.m_Map.Deleted)
                    return;

                Mobile from = this.Owner.From;

                if (HasDiggingTool(from))
                    this.m_Map.OnBeginDig(from);
                else
                    from.SendMessage("You must have a digging tool to dig for treasure.");
            }
        }
    }

    public class TreasureChestDirt : Item
    {
        public TreasureChestDirt()
            : base(0x912)
        {
            this.Movable = false;

            Timer.DelayCall(TimeSpan.FromMinutes(2.0), new TimerCallback(Delete));
        }

        public TreasureChestDirt(Serial serial)
            : base(serial)
        {
        }

        public void Turn1()
        {
            this.ItemID = 0x913;
        }

        public void Turn2()
        {
            this.ItemID = 0x914;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();

            this.Delete();
        }
    }
}