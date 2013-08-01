using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using CustomsFramework;
using Server.ContextMenus;
using Server.Items;
using Server.Network;

namespace Server
{
    /// <summary>
    /// Enumeration of item layer values.
    /// </summary>
    public enum Layer : byte
    {
        /// <summary>
        /// Invalid layer.
        /// </summary>
        Invalid = 0x00,
        /// <summary>
        /// First valid layer. Equivalent to <c>Layer.OneHanded</c>.
        /// </summary>
        FirstValid = 0x01,
        /// <summary>
        /// One handed weapon.
        /// </summary>
        OneHanded = 0x01,
        /// <summary>
        /// Two handed weapon or shield.
        /// </summary>
        TwoHanded = 0x02,
        /// <summary>
        /// Shoes.
        /// </summary>
        Shoes = 0x03,
        /// <summary>
        /// Pants.
        /// </summary>
        Pants = 0x04,
        /// <summary>
        /// Shirts.
        /// </summary>
        Shirt = 0x05,
        /// <summary>
        /// Helmets, hats, and masks.
        /// </summary>
        Helm = 0x06,
        /// <summary>
        /// Gloves.
        /// </summary>
        Gloves = 0x07,
        /// <summary>
        /// Rings.
        /// </summary>
        Ring = 0x08,
        /// <summary>
        /// Talismans.
        /// </summary>
        Talisman = 0x09,
        /// <summary>
        /// Gorgets and necklaces.
        /// </summary>
        Neck = 0x0A,
        /// <summary>
        /// Hair.
        /// </summary>
        Hair = 0x0B,
        /// <summary>
        /// Half aprons.
        /// </summary>
        Waist = 0x0C,
        /// <summary>
        /// Torso, inner layer.
        /// </summary>
        InnerTorso = 0x0D,
        /// <summary>
        /// Bracelets.
        /// </summary>
        Bracelet = 0x0E,
        /// <summary>
        /// Unused.
        /// </summary>
        Unused_xF = 0x0F,
        /// <summary>
        /// Beards and mustaches.
        /// </summary>
        FacialHair = 0x10,
        /// <summary>
        /// Torso, outer layer.
        /// </summary>
        MiddleTorso = 0x11,
        /// <summary>
        /// Earings.
        /// </summary>
        Earrings = 0x12,
        /// <summary>
        /// Arms and sleeves.
        /// </summary>
        Arms = 0x13,
        /// <summary>
        /// Cloaks.
        /// </summary>
        Cloak = 0x14,
        /// <summary>
        /// Backpacks.
        /// </summary>
        Backpack = 0x15,
        /// <summary>
        /// Torso, outer layer.
        /// </summary>
        OuterTorso = 0x16,
        /// <summary>
        /// Leggings, outer layer.
        /// </summary>
        OuterLegs = 0x17,
        /// <summary>
        /// Leggings, inner layer.
        /// </summary>
        InnerLegs = 0x18,
        /// <summary>
        /// Last valid non-internal layer. Equivalent to <c>Layer.InnerLegs</c>.
        /// </summary>
        LastUserValid = 0x18,
        /// <summary>
        /// Mount item layer.
        /// </summary>
        Mount = 0x19,
        /// <summary>
        /// Vendor 'buy pack' layer.
        /// </summary>
        ShopBuy = 0x1A,
        /// <summary>
        /// Vendor 'resale pack' layer.
        /// </summary>
        ShopResale = 0x1B,
        /// <summary>
        /// Vendor 'sell pack' layer.
        /// </summary>
        ShopSell = 0x1C,
        /// <summary>
        /// Bank box layer.
        /// </summary>
        Bank = 0x1D,
        /// <summary>
        /// Last valid layer. Equivalent to <c>Layer.Bank</c>.
        /// </summary>
        LastValid = 0x1D
    }

    /// <summary>
    /// Internal flags used to signal how the item should be updated and resent to nearby clients.
    /// </summary>
    [Flags]
    public enum ItemDelta
    {
        /// <summary>
        /// Nothing.
        /// </summary>
        None = 0x00000000,
        /// <summary>
        /// Resend the item.
        /// </summary>
        Update = 0x00000001,
        /// <summary>
        /// Resend the item only if it is equipped.
        /// </summary>
        EquipOnly = 0x00000002,
        /// <summary>
        /// Resend the item's properties.
        /// </summary>
        Properties = 0x00000004
    }

    /// <summary>
    /// Enumeration containing possible ways to handle item ownership on death.
    /// </summary>
    public enum DeathMoveResult
    {
        /// <summary>
        /// The item should be placed onto the corpse.
        /// </summary>
        MoveToCorpse,
        /// <summary>
        /// The item should remain equipped.
        /// </summary>
        RemainEquiped,
        /// <summary>
        /// The item should be placed into the owners backpack.
        /// </summary>
        MoveToBackpack
    }

    /// <summary>
    /// Enumeration containing all possible light types. These are only applicable to light source items, like lanterns, candles, braziers, etc.
    /// </summary>
    public enum LightType
    {
        /// <summary>
        /// Window shape, arched, ray shining east.
        /// </summary>
        ArchedWindowEast,
        /// <summary>
        /// Medium circular shape.
        /// </summary>
        Circle225,
        /// <summary>
        /// Small circular shape.
        /// </summary>
        Circle150,
        /// <summary>
        /// Door shape, shining south.
        /// </summary>
        DoorSouth,
        /// <summary>
        /// Door shape, shining east.
        /// </summary>
        DoorEast,
        /// <summary>
        /// Large semicircular shape (180 degrees), north wall.
        /// </summary>
        NorthBig,
        /// <summary>
        /// Large pie shape (90 degrees), north-east corner.
        /// </summary>
        NorthEastBig,
        /// <summary>
        /// Large semicircular shape (180 degrees), east wall.
        /// </summary>
        EastBig,
        /// <summary>
        /// Large semicircular shape (180 degrees), west wall.
        /// </summary>
        WestBig,
        /// <summary>
        /// Large pie shape (90 degrees), south-west corner.
        /// </summary>
        SouthWestBig,
        /// <summary>
        /// Large semicircular shape (180 degrees), south wall.
        /// </summary>
        SouthBig,
        /// <summary>
        /// Medium semicircular shape (180 degrees), north wall.
        /// </summary>
        NorthSmall,
        /// <summary>
        /// Medium pie shape (90 degrees), north-east corner.
        /// </summary>
        NorthEastSmall,
        /// <summary>
        /// Medium semicircular shape (180 degrees), east wall.
        /// </summary>
        EastSmall,
        /// <summary>
        /// Medium semicircular shape (180 degrees), west wall.
        /// </summary>
        WestSmall,
        /// <summary>
        /// Medium semicircular shape (180 degrees), south wall.
        /// </summary>
        SouthSmall,
        /// <summary>
        /// Shaped like a wall decoration, north wall.
        /// </summary>
        DecorationNorth,
        /// <summary>
        /// Shaped like a wall decoration, north-east corner.
        /// </summary>
        DecorationNorthEast,
        /// <summary>
        /// Small semicircular shape (180 degrees), east wall.
        /// </summary>
        EastTiny,
        /// <summary>
        /// Shaped like a wall decoration, west wall.
        /// </summary>
        DecorationWest,
        /// <summary>
        /// Shaped like a wall decoration, south-west corner.
        /// </summary>
        DecorationSouthWest,
        /// <summary>
        /// Small semicircular shape (180 degrees), south wall.
        /// </summary>
        SouthTiny,
        /// <summary>
        /// Window shape, rectangular, no ray, shining south.
        /// </summary>
        RectWindowSouthNoRay,
        /// <summary>
        /// Window shape, rectangular, no ray, shining east.
        /// </summary>
        RectWindowEastNoRay,
        /// <summary>
        /// Window shape, rectangular, ray shining south.
        /// </summary>
        RectWindowSouth,
        /// <summary>
        /// Window shape, rectangular, ray shining east.
        /// </summary>
        RectWindowEast,
        /// <summary>
        /// Window shape, arched, no ray, shining south.
        /// </summary>
        ArchedWindowSouthNoRay,
        /// <summary>
        /// Window shape, arched, no ray, shining east.
        /// </summary>
        ArchedWindowEastNoRay,
        /// <summary>
        /// Window shape, arched, ray shining south.
        /// </summary>
        ArchedWindowSouth,
        /// <summary>
        /// Large circular shape.
        /// </summary>
        Circle300,
        /// <summary>
        /// Large pie shape (90 degrees), north-west corner.
        /// </summary>
        NorthWestBig,
        /// <summary>
        /// Negative light. Medium pie shape (90 degrees), south-east corner.
        /// </summary>
        DarkSouthEast,
        /// <summary>
        /// Negative light. Medium semicircular shape (180 degrees), south wall.
        /// </summary>
        DarkSouth,
        /// <summary>
        /// Negative light. Medium pie shape (90 degrees), north-west corner.
        /// </summary>
        DarkNorthWest,
        /// <summary>
        /// Negative light. Medium pie shape (90 degrees), south-east corner. Equivalent to <c>LightType.SouthEast</c>.
        /// </summary>
        DarkSouthEast2,
        /// <summary>
        /// Negative light. Medium circular shape (180 degrees), east wall.
        /// </summary>
        DarkEast,
        /// <summary>
        /// Negative light. Large circular shape.
        /// </summary>
        DarkCircle300,
        /// <summary>
        /// Opened door shape, shining south.
        /// </summary>
        DoorOpenSouth,
        /// <summary>
        /// Opened door shape, shining east.
        /// </summary>
        DoorOpenEast,
        /// <summary>
        /// Window shape, square, ray shining east.
        /// </summary>
        SquareWindowEast,
        /// <summary>
        /// Window shape, square, no ray, shining east.
        /// </summary>
        SquareWindowEastNoRay,
        /// <summary>
        /// Window shape, square, ray shining south.
        /// </summary>
        SquareWindowSouth,
        /// <summary>
        /// Window shape, square, no ray, shining south.
        /// </summary>
        SquareWindowSouthNoRay,
        /// <summary>
        /// Empty.
        /// </summary>
        Empty,
        /// <summary>
        /// Window shape, skinny, no ray, shining south.
        /// </summary>
        SkinnyWindowSouthNoRay,
        /// <summary>
        /// Window shape, skinny, ray shining east.
        /// </summary>
        SkinnyWindowEast,
        /// <summary>
        /// Window shape, skinny, no ray, shining east.
        /// </summary>
        SkinnyWindowEastNoRay,
        /// <summary>
        /// Shaped like a hole, shining south.
        /// </summary>
        HoleSouth,
        /// <summary>
        /// Shaped like a hole, shining south.
        /// </summary>
        HoleEast,
        /// <summary>
        /// Large circular shape with a moongate graphic embeded.
        /// </summary>
        Moongate,
        /// <summary>
        /// Unknown usage. Many rows of slightly angled lines.
        /// </summary>
        Strips,
        /// <summary>
        /// Shaped like a small hole, shining south.
        /// </summary>
        SmallHoleSouth,
        /// <summary>
        /// Shaped like a small hole, shining east.
        /// </summary>
        SmallHoleEast,
        /// <summary>
        /// Large semicircular shape (180 degrees), north wall. Identical graphic as <c>LightType.NorthBig</c>, but slightly different positioning.
        /// </summary>
        NorthBig2,
        /// <summary>
        /// Large semicircular shape (180 degrees), west wall. Identical graphic as <c>LightType.WestBig</c>, but slightly different positioning.
        /// </summary>
        WestBig2,
        /// <summary>
        /// Large pie shape (90 degrees), north-west corner. Equivalent to <c>LightType.NorthWestBig</c>.
        /// </summary>
        NorthWestBig2
    }

    /// <summary>
    /// Enumeration of an item's loot and steal state.
    /// </summary>
    public enum LootType : byte
    {
        /// <summary>
        /// Stealable. Lootable.
        /// </summary>
        Regular = 0,
        /// <summary>
        /// Unstealable. Unlootable, unless owned by a murderer.
        /// </summary>
        Newbied = 1,
        /// <summary>
        /// Unstealable. Unlootable, always.
        /// </summary>
        Blessed = 2,
        /// <summary>
        /// Stealable. Lootable, always.
        /// </summary>
        Cursed = 3
    }

    public class BounceInfo
    {
        public Map m_Map;
        public Point3D m_Location, m_WorldLoc;
        public object m_Parent;

        public BounceInfo(Item item)
        {
            this.m_Map = item.Map;
            this.m_Location = item.Location;
            this.m_WorldLoc = item.GetWorldLocation();
            this.m_Parent = item.Parent;
        }

        private BounceInfo(Map map, Point3D loc, Point3D worldLoc, object parent)
        {
            this.m_Map = map;
            this.m_Location = loc;
            this.m_WorldLoc = worldLoc;
            this.m_Parent = parent;
        }

        public static BounceInfo Deserialize(GenericReader reader)
        {
            if (reader.ReadBool())
            {
                Map map = reader.ReadMap();
                Point3D loc = reader.ReadPoint3D();
                Point3D worldLoc = reader.ReadPoint3D();

                object parent;

                Serial serial = reader.ReadInt();

                if (serial.IsItem)
                    parent = World.FindItem(serial);
                else if (serial.IsMobile)
                    parent = World.FindMobile(serial);
                else
                    parent = null;

                return new BounceInfo(map, loc, worldLoc, parent);
            }
            else
            {
                return null;
            }
        }

        public static void Serialize(BounceInfo info, GenericWriter writer)
        {
            if (info == null)
            {
                writer.Write(false);
            }
            else
            {
                writer.Write(true);

                writer.Write(info.m_Map);
                writer.Write(info.m_Location);
                writer.Write(info.m_WorldLoc);

                if (info.m_Parent is Mobile)
                    writer.Write((Mobile)info.m_Parent);
                else if (info.m_Parent is Item)
                    writer.Write((Item)info.m_Parent);
                else
                    writer.Write((Serial)0);
            }
        }
    }

    public enum TotalType
    {
        Gold,
        Items,
        Weight,
    }

    [Flags]
    public enum ExpandFlag
    {
        None = 0x000,

        Name = 0x001,
        Items = 0x002,
        Bounce = 0x004,
        Holder = 0x008,
        Blessed = 0x010,
        TempFlag = 0x020,
        SaveFlag = 0x040,
        Weight = 0x080,
        Spawner = 0x100
    }

    public class Item : IEntity, IHued, IComparable<Item>, ISerializable, ISpawnable
    {
        #region Customs Framework
        private List<BaseModule> m_Modules = new List<BaseModule>();

        [CommandProperty(AccessLevel.Developer)]
        public List<BaseModule> Modules { get { return m_Modules; } set { m_Modules = value; } }

        public BaseModule GetModule(string name)
        {
            return Modules.FirstOrDefault(mod => mod.Name == name);
        }

        public BaseModule GetModule(Type type)
        {
            return Modules.FirstOrDefault(mod => mod.GetType() == type);
        }

        public List<BaseModule> GetModules(string name)
        {
            return Modules.Where(mod => mod.Name == name).ToList();
        }

        public List<BaseModule> SearchModules(string search)
        {
            string[] keywords = search.ToLower().Split(' ');
            List<BaseModule> modules = new List<BaseModule>();

            foreach (BaseModule mod in Modules)
            {
                bool match = true;
                string name = mod.Name.ToLower();

                foreach (string keyword in keywords)
                {
                    if (name.IndexOf(keyword, StringComparison.Ordinal) == -1)
                        match = false;
                }

                if (match)
                    modules.Add(mod);
            }

            return modules;
        }
        #endregion

        public static readonly List<Item> EmptyItems = new List<Item>();

        public int CompareTo(IEntity other)
        {
            if (other == null)
                return -1;

            return this.m_Serial.CompareTo(other.Serial);
        }

        public int CompareTo(Item other)
        {
            return this.CompareTo((IEntity)other);
        }

        public int CompareTo(object other)
        {
            if (other == null || other is IEntity)
                return this.CompareTo((IEntity)other);

            throw new ArgumentException();
        }

        #region Standard fields
        private readonly Serial m_Serial;
        private Point3D m_Location;
        private int m_ItemID;
        private int m_Hue;
        private int m_Amount;
        private Layer m_Layer;
        private object m_Parent; // Mobile, Item, or null=World
        private Map m_Map;
        private LootType m_LootType;
        private DateTime m_LastMovedTime;
        private Direction m_Direction;
        #endregion

        private ItemDelta m_DeltaFlags;
        private ImplFlag m_Flags;

        #region Packet caches
        private Packet m_WorldPacket;
        private Packet m_WorldPacketSA;
        private Packet m_WorldPacketHS;
        private Packet m_RemovePacket;

        private Packet m_OPLPacket;
        private ObjectPropertyList m_PropertyList;
        #endregion

        public int TempFlags
        {
            get
            {
                CompactInfo info = this.LookupCompactInfo();

                if (info != null)
                    return info.m_TempFlags;

                return 0;
            }
            set
            {
                CompactInfo info = this.AcquireCompactInfo();

                info.m_TempFlags = value;

                if (info.m_TempFlags == 0)
                    this.VerifyCompactInfo();
            }
        }

        public int SavedFlags
        {
            get
            {
                CompactInfo info = this.LookupCompactInfo();

                if (info != null)
                    return info.m_SavedFlags;

                return 0;
            }
            set
            {
                CompactInfo info = this.AcquireCompactInfo();

                info.m_SavedFlags = value;

                if (info.m_SavedFlags == 0)
                    this.VerifyCompactInfo();
            }
        }

        /// <summary>
        /// The <see cref="Mobile" /> who is currently <see cref="Mobile.Holding">holding</see> this item.
        /// </summary>
        public Mobile HeldBy
        {
            get
            {
                CompactInfo info = this.LookupCompactInfo();

                if (info != null)
                    return info.m_HeldBy;

                return null;
            }
            set
            {
                CompactInfo info = this.AcquireCompactInfo();

                info.m_HeldBy = value;

                if (info.m_HeldBy == null)
                    this.VerifyCompactInfo();
            }
        }

        [Flags]
        private enum ImplFlag : byte
        {
            None = 0x00,
            Visible = 0x01,
            Movable = 0x02,
            Deleted = 0x04,
            Stackable = 0x08,
            InQueue = 0x10,
            Insured = 0x20,
            PayedInsurance = 0x40,
            QuestItem = 0x80
        }

        private class CompactInfo
        {
            public string m_Name;

            public List<Item> m_Items;
            public BounceInfo m_Bounce;

            public Mobile m_HeldBy;
            public Mobile m_BlessedFor;

            public ISpawner m_Spawner;

            public int m_TempFlags;
            public int m_SavedFlags;

            public double m_Weight = -1;
        }

        private CompactInfo m_CompactInfo;

        public ExpandFlag GetExpandFlags()
        {
            CompactInfo info = this.LookupCompactInfo();

            ExpandFlag flags = 0;

            if (info != null)
            {
                if (info.m_BlessedFor != null)
                    flags |= ExpandFlag.Blessed;

                if (info.m_Bounce != null)
                    flags |= ExpandFlag.Bounce;

                if (info.m_HeldBy != null)
                    flags |= ExpandFlag.Holder;

                if (info.m_Items != null)
                    flags |= ExpandFlag.Items;

                if (info.m_Name != null)
                    flags |= ExpandFlag.Name;

                if (info.m_Spawner != null)
                    flags |= ExpandFlag.Spawner;

                if (info.m_SavedFlags != 0)
                    flags |= ExpandFlag.SaveFlag;

                if (info.m_TempFlags != 0)
                    flags |= ExpandFlag.TempFlag;

                if (info.m_Weight != -1)
                    flags |= ExpandFlag.Weight;
            }

            return flags;
        }

        private CompactInfo LookupCompactInfo()
        {
            return this.m_CompactInfo;
        }

        private CompactInfo AcquireCompactInfo()
        {
            if (this.m_CompactInfo == null)
                this.m_CompactInfo = new CompactInfo();

            return this.m_CompactInfo;
        }

        private void ReleaseCompactInfo()
        {
            this.m_CompactInfo = null;
        }

        private void VerifyCompactInfo()
        {
            CompactInfo info = this.m_CompactInfo;

            if (info == null)
                return;

            bool isValid = (info.m_Name != null) ||
                           (info.m_Items != null) ||
                           (info.m_Bounce != null) ||
                           (info.m_HeldBy != null) ||
                           (info.m_BlessedFor != null) ||
                           (info.m_Spawner != null) ||
                           (info.m_TempFlags != 0) ||
                           (info.m_SavedFlags != 0) ||
                           (info.m_Weight != -1);

            if (!isValid)
                this.ReleaseCompactInfo();
        }

        public List<Item> LookupItems()
        {
            if (this is Container)
                return (this as Container).m_Items;

            CompactInfo info = this.LookupCompactInfo();

            if (info != null)
                return info.m_Items;

            return null;
        }

        public List<Item> AcquireItems()
        {
            if (this is Container)
            {
                Container cont = this as Container;

                if (cont.m_Items == null)
                    cont.m_Items = new List<Item>();

                return cont.m_Items;
            }

            CompactInfo info = this.AcquireCompactInfo();

            if (info.m_Items == null)
                info.m_Items = new List<Item>();

            return info.m_Items;
        }

        #region Mondain's Legacy

        public static System.Drawing.Bitmap GetBitmap(int itemID)
        {
            try 
            { 
                return OpenUOSDK.ArtFactory.GetStatic<Bitmap>(itemID);
            }
            catch
            {
                Utility.PushColor(ConsoleColor.Red);
                Console.WriteLine("Error: Not able to read client files.");
                Utility.PopColor();
            }

            return null;
        }

        public unsafe static void Measure(Bitmap bmp, out int xMin, out int yMin, out int xMax, out int yMax)
        {
            xMin = yMin = 0;
            xMax = yMax = -1;

            if (bmp == null || bmp.Width <= 0 || bmp.Height <= 0)
                return;

            BitmapData bd = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format16bppArgb1555);

            int delta = (bd.Stride >> 1) - bd.Width;
            int lineDelta = bd.Stride >> 1;

            ushort* pBuffer = (ushort*)bd.Scan0;
            ushort* pLineEnd = pBuffer + bd.Width;
            ushort* pEnd = pBuffer + (bd.Height * lineDelta);

            bool foundPixel = false;

            int x = 0, y = 0;

            while (pBuffer < pEnd)
            {
                while (pBuffer < pLineEnd)
                {
                    ushort c = *pBuffer++;

                    if ((c & 0x8000) != 0)
                    {
                        if (!foundPixel)
                        {
                            foundPixel = true;
                            xMin = xMax = x;
                            yMin = yMax = y;
                        }
                        else
                        {
                            if (x < xMin)
                                xMin = x;

                            if (y < yMin)
                                yMin = y;

                            if (x > xMax)
                                xMax = x;

                            if (y > yMax)
                                yMax = y;
                        }
                    }
                    ++x;
                }

                pBuffer += delta;
                pLineEnd += lineDelta;
                ++y;
                x = 0;
            }

            bmp.UnlockBits(bd);
        }

        #endregion

        private void SetFlag(ImplFlag flag, bool value)
        {
            if (value)
                this.m_Flags |= flag;
            else
                this.m_Flags &= ~flag;
        }

        private bool GetFlag(ImplFlag flag)
        {
            return ((this.m_Flags & flag) != 0);
        }

        public BounceInfo GetBounce()
        {
            CompactInfo info = this.LookupCompactInfo();

            if (info != null)
                return info.m_Bounce;

            return null;
        }

        public void RecordBounce()
        {
            CompactInfo info = this.AcquireCompactInfo();

            info.m_Bounce = new BounceInfo(this);
        }

        public void ClearBounce()
        {
            CompactInfo info = this.LookupCompactInfo();

            if (info != null)
            {
                BounceInfo bounce = info.m_Bounce;

                if (bounce != null)
                {
                    info.m_Bounce = null;

                    if (bounce.m_Parent is Item)
                    {
                        Item parent = (Item)bounce.m_Parent;

                        if (!parent.Deleted)
                            parent.OnItemBounceCleared(this);
                    }
                    else if (bounce.m_Parent is Mobile)
                    {
                        Mobile parent = (Mobile)bounce.m_Parent;

                        if (!parent.Deleted)
                            parent.OnItemBounceCleared(this);
                    }

                    this.VerifyCompactInfo();
                }
            }
        }

        /// <summary>
        /// Overridable. Virtual event invoked when a client, <paramref name="from" />, invokes a 'help request' for the Item. Seemingly no longer functional in newer clients.
        /// </summary>
        public virtual void OnHelpRequest(Mobile from)
        {
        }

        /// <summary>
        /// Overridable. Method checked to see if the item can be traded.
        /// </summary>
        /// <returns>True if the trade is allowed, false if not.</returns>
        public virtual bool AllowSecureTrade(Mobile from, Mobile to, Mobile newOwner, bool accepted)
        {
            return true;
        }

        /// <summary>
        /// Overridable. Virtual event invoked when a trade has completed, either successfully or not.
        /// </summary>
        public virtual void OnSecureTrade(Mobile from, Mobile to, Mobile newOwner, bool accepted)
        {
        }

        /// <summary>
        /// Overridable. Method checked to see if the elemental resistances of this Item conflict with another Item on the <see cref="Mobile" />.
        /// </summary>
        /// <returns>
        /// <list type="table">
        /// <item>
        /// <term>True</term>
        /// <description>There is a confliction. The elemental resistance bonuses of this Item should not be applied to the <see cref="Mobile" /></description>
        /// </item>
        /// <item>
        /// <term>False</term>
        /// <description>There is no confliction. The bonuses should be applied.</description>
        /// </item>
        /// </list>
        /// </returns>
        public virtual bool CheckPropertyConfliction(Mobile m)
        {
            return false;
        }

        /// <summary>
        /// Overridable. Sends the <see cref="PropertyList">object property list</see> to <paramref name="from" />.
        /// </summary>
        public virtual void SendPropertiesTo(Mobile from)
        {
            from.Send(this.PropertyList);
        }

        /// <summary>
        /// Overridable. Adds the name of this item to the given <see cref="ObjectPropertyList" />. This method should be overriden if the item requires a complex naming format.
        /// </summary>
        public virtual void AddNameProperty(ObjectPropertyList list)
        {
            string name = this.Name;

            if (name == null)
            {
                if (this.m_Amount <= 1)
                    list.Add(this.LabelNumber);
                else
                    list.Add(1050039, "{0}\t#{1}", this.m_Amount, this.LabelNumber); // ~1_NUMBER~ ~2_ITEMNAME~
            }
            else
            {
                if (this.m_Amount <= 1)
                    list.Add(name);
                else
                    list.Add(1050039, "{0}\t{1}", this.m_Amount, this.Name); // ~1_NUMBER~ ~2_ITEMNAME~
            }
        }

        /// <summary>
        /// Overridable. Adds the loot type of this item to the given <see cref="ObjectPropertyList" />. By default, this will be either 'blessed', 'cursed', or 'insured'.
        /// </summary>
        public virtual void AddLootTypeProperty(ObjectPropertyList list)
        {
            if (this.m_LootType == LootType.Blessed)
                list.Add(1038021); // blessed
            else if (this.m_LootType == LootType.Cursed)
                list.Add(1049643); // cursed
            else if (this.Insured)
                list.Add(1061682); // <b>insured</b>
        }

        /// <summary>
        /// Overridable. Adds any elemental resistances of this item to the given <see cref="ObjectPropertyList" />.
        /// </summary>
        public virtual void AddResistanceProperties(ObjectPropertyList list)
        {
            int v = this.PhysicalResistance;

            if (v != 0)
                list.Add(1060448, v.ToString()); // physical resist ~1_val~%

            v = this.FireResistance;

            if (v != 0)
                list.Add(1060447, v.ToString()); // fire resist ~1_val~%

            v = this.ColdResistance;

            if (v != 0)
                list.Add(1060445, v.ToString()); // cold resist ~1_val~%

            v = this.PoisonResistance;

            if (v != 0)
                list.Add(1060449, v.ToString()); // poison resist ~1_val~%

            v = this.EnergyResistance;

            if (v != 0)
                list.Add(1060446, v.ToString()); // energy resist ~1_val~%
        }

        /// <summary>
        /// Overridable. Determines whether the item will show <see cref="AddWeightProperty" />. 
        /// </summary>
        public virtual bool DisplayWeight 
        {
            get
            {
                if (!Core.ML)
                    return false;

                if (!this.Movable && !(this.IsLockedDown || this.IsSecure) && this.ItemData.Weight == 255)
                    return false;

                return true;
            }
        }

        /// <summary>
        /// Overridable. Displays cliloc 1072788-1072789. 
        /// </summary>
        public virtual void AddWeightProperty(ObjectPropertyList list)
        {
            int weight = this.PileWeight + this.TotalWeight;

            if (weight == 1)
            {
                list.Add(1072788, weight.ToString()); //Weight: ~1_WEIGHT~ stone
            }
            else
            {
                list.Add(1072789, weight.ToString()); //Weight: ~1_WEIGHT~ stones
            }
        }

        /// <summary>
        /// Overridable. Adds header properties. By default, this invokes <see cref="AddNameProperty" />, <see cref="AddBlessedForProperty" /> (if applicable), and <see cref="AddLootTypeProperty" /> (if <see cref="DisplayLootType" />).
        /// </summary>
        public virtual void AddNameProperties(ObjectPropertyList list)
        {
            this.AddNameProperty(list);

            if (this.IsSecure)
                this.AddSecureProperty(list);
            else if (this.IsLockedDown)
                this.AddLockedDownProperty(list);

            Mobile blessedFor = this.BlessedFor;

            if (blessedFor != null && !blessedFor.Deleted)
                this.AddBlessedForProperty(list, blessedFor);

            if (this.DisplayLootType)
                this.AddLootTypeProperty(list);

            if (this.DisplayWeight)
                this.AddWeightProperty(list);

            if (this.QuestItem)
                this.AddQuestItemProperty(list);

            this.AppendChildNameProperties(list);
        }

        /// <summary>
        /// Overridable. Adds the "Quest Item" property to the given <see cref="ObjectPropertyList" />.
        /// </summary>
        public virtual void AddQuestItemProperty(ObjectPropertyList list)
        {
            list.Add(1072351); // Quest Item
        }

        /// <summary>
        /// Overridable. Adds the "Locked Down & Secure" property to the given <see cref="ObjectPropertyList" />.
        /// </summary>
        public virtual void AddSecureProperty(ObjectPropertyList list)
        {
            list.Add(501644); // locked down & secure
        }

        /// <summary>
        /// Overridable. Adds the "Locked Down" property to the given <see cref="ObjectPropertyList" />.
        /// </summary>
        public virtual void AddLockedDownProperty(ObjectPropertyList list)
        {
            list.Add(501643); // locked down
        }

        /// <summary>
        /// Overridable. Adds the "Blessed for ~1_NAME~" property to the given <see cref="ObjectPropertyList" />.
        /// </summary>
        public virtual void AddBlessedForProperty(ObjectPropertyList list, Mobile m)
        {
            list.Add(1062203, "{0}", m.Name); // Blessed for ~1_NAME~
        }

        /// <summary>
        /// Overridable. Fills an <see cref="ObjectPropertyList" /> with everything applicable. By default, this invokes <see cref="AddNameProperties" />, then <see cref="Item.GetChildProperties">Item.GetChildProperties</see> or <see cref="Mobile.GetChildProperties">Mobile.GetChildProperties</see>. This method should be overriden to add any custom properties.
        /// </summary>
        public virtual void GetProperties(ObjectPropertyList list)
        {
            this.AddNameProperties(list);
        }

        /// <summary>
        /// Overridable. Event invoked when a child (<paramref name="item" />) is building it's <see cref="ObjectPropertyList" />. Recursively calls <see cref="Item.GetChildProperties">Item.GetChildProperties</see> or <see cref="Mobile.GetChildProperties">Mobile.GetChildProperties</see>.
        /// </summary>
        public virtual void GetChildProperties(ObjectPropertyList list, Item item)
        {
            if (this.m_Parent is Item)
                ((Item)this.m_Parent).GetChildProperties(list, item);
            else if (this.m_Parent is Mobile)
                ((Mobile)this.m_Parent).GetChildProperties(list, item);
        }

        /// <summary>
        /// Overridable. Event invoked when a child (<paramref name="item" />) is building it's Name <see cref="ObjectPropertyList" />. Recursively calls <see cref="Item.GetChildProperties">Item.GetChildNameProperties</see> or <see cref="Mobile.GetChildProperties">Mobile.GetChildNameProperties</see>.
        /// </summary>
        public virtual void GetChildNameProperties(ObjectPropertyList list, Item item)
        {
            if (this.m_Parent is Item)
                ((Item)this.m_Parent).GetChildNameProperties(list, item);
            else if (this.m_Parent is Mobile)
                ((Mobile)this.m_Parent).GetChildNameProperties(list, item);
        }

        public virtual bool IsChildVisibleTo(Mobile m, Item child)
        {
            return true;
        }

        public void Bounce(Mobile from)
        {
            if (this.m_Parent is Item)
                ((Item)this.m_Parent).RemoveItem(this);
            else if (this.m_Parent is Mobile)
                ((Mobile)this.m_Parent).RemoveItem(this);

            this.m_Parent = null;

            BounceInfo bounce = this.GetBounce();

            if (bounce != null)
            {
                object parent = bounce.m_Parent;

                if (parent is Item && !((Item)parent).Deleted)
                {
                    Item p = (Item)parent;
                    object root = p.RootParent;
                    if (p.IsAccessibleTo(from) && (!(root is Mobile) || ((Mobile)root).CheckNonlocalDrop(from, this, p)))
                    {
                        this.Location = bounce.m_Location;
                        p.AddItem(this);
                    }
                    else
                    {
                        this.MoveToWorld(from.Location, from.Map);
                    }
                }
                else if (parent is Mobile && !((Mobile)parent).Deleted)
                {
                    if (!((Mobile)parent).EquipItem(this))
                        this.MoveToWorld(bounce.m_WorldLoc, bounce.m_Map);
                }
                else
                {
                    this.MoveToWorld(bounce.m_WorldLoc, bounce.m_Map);
                }

                this.ClearBounce();
            }
            else
            {
                this.MoveToWorld(from.Location, from.Map);
            }
        }

        /// <summary>
        /// Overridable. Method checked to see if this item may be equiped while casting a spell. By default, this returns false. It is overriden on spellbook and spell channeling weapons or shields.
        /// </summary>
        /// <returns>True if it may, false if not.</returns>
        /// <example>
        /// <code>
        ///	public override bool AllowEquipedCast( Mobile from )
        ///	{
        ///		if ( from.Int &gt;= 100 )
        ///			return true;
        ///		
        ///		return base.AllowEquipedCast( from );
        /// }</code>
        /// 
        /// When placed in an Item script, the item may be cast when equiped if the <paramref name="from" /> has 100 or more intelligence. Otherwise, it will drop to their backpack.
        /// </example>
        public virtual bool AllowEquipedCast(Mobile from)
        {
            return false;
        }

        public virtual bool CheckConflictingLayer(Mobile m, Item item, Layer layer)
        {
            return (this.m_Layer == layer);
        }

        public virtual bool CanEquip(Mobile m)
        {
            return (this.m_Layer != Layer.Invalid && m.FindItemOnLayer(this.m_Layer) == null);
        }

        public virtual void GetChildContextMenuEntries(Mobile from, List<ContextMenuEntry> list, Item item)
        {
            if (this.m_Parent is Item)
                ((Item)this.m_Parent).GetChildContextMenuEntries(from, list, item);
            else if (this.m_Parent is Mobile)
                ((Mobile)this.m_Parent).GetChildContextMenuEntries(from, list, item);
        }

        public virtual void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            if (this.m_Parent is Item)
                ((Item)this.m_Parent).GetChildContextMenuEntries(from, list, this);
            else if (this.m_Parent is Mobile)
                ((Mobile)this.m_Parent).GetChildContextMenuEntries(from, list, this);
        }

        public virtual bool VerifyMove(Mobile from)
        {
            return this.Movable;
        }

        public virtual DeathMoveResult OnParentDeath(Mobile parent)
        {
            if (!this.Movable)
                return DeathMoveResult.RemainEquiped;
            else if (parent.KeepsItemsOnDeath)
                return DeathMoveResult.MoveToBackpack;
            else if (this.CheckBlessed(parent))
                return DeathMoveResult.MoveToBackpack;
            else if (this.CheckNewbied() && parent.Kills < 5)
                return DeathMoveResult.MoveToBackpack;
            else if (parent.Player && this.Nontransferable)
                return DeathMoveResult.MoveToBackpack;
            else
                return DeathMoveResult.MoveToCorpse;
        }

        public virtual DeathMoveResult OnInventoryDeath(Mobile parent)
        {
            if (!this.Movable)
                return DeathMoveResult.MoveToBackpack;
            else if (parent.KeepsItemsOnDeath)
                return DeathMoveResult.MoveToBackpack;
            else if (this.CheckBlessed(parent))
                return DeathMoveResult.MoveToBackpack;
            else if (this.CheckNewbied() && parent.Kills < 5)
                return DeathMoveResult.MoveToBackpack;
            else if (parent.Player && this.Nontransferable)
                return DeathMoveResult.MoveToBackpack;
            else
                return DeathMoveResult.MoveToCorpse;
        }

        /// <summary>
        /// Moves the Item to <paramref name="location" />. The Item does not change maps.
        /// </summary>
        public virtual void MoveToWorld(Point3D location)
        {
            this.MoveToWorld(location, this.m_Map);
        }

        public void LabelTo(Mobile to, int number)
        {
            to.Send(new MessageLocalized(this.m_Serial, this.m_ItemID, MessageType.Label, 0x3B2, 3, number, "", ""));
        }

        public void LabelTo(Mobile to, int number, string args)
        {
            to.Send(new MessageLocalized(this.m_Serial, this.m_ItemID, MessageType.Label, 0x3B2, 3, number, "", args));
        }

        public void LabelTo(Mobile to, string text)
        {
            to.Send(new UnicodeMessage(this.m_Serial, this.m_ItemID, MessageType.Label, 0x3B2, 3, "ENU", "", text));
        }

        public void LabelTo(Mobile to, string format, params object[] args)
        {
            this.LabelTo(to, String.Format(format, args));
        }

        public void LabelToAffix(Mobile to, int number, AffixType type, string affix)
        {
            to.Send(new MessageLocalizedAffix(this.m_Serial, this.m_ItemID, MessageType.Label, 0x3B2, 3, number, "", type, affix, ""));
        }

        public void LabelToAffix(Mobile to, int number, AffixType type, string affix, string args)
        {
            to.Send(new MessageLocalizedAffix(this.m_Serial, this.m_ItemID, MessageType.Label, 0x3B2, 3, number, "", type, affix, args));
        }

        public virtual void LabelLootTypeTo(Mobile to)
        {
            if (this.m_LootType == LootType.Blessed)
                this.LabelTo(to, 1041362); // (blessed)
            else if (this.m_LootType == LootType.Cursed)
                this.LabelTo(to, "(cursed)");
        }

        public bool AtWorldPoint(int x, int y)
        {
            return (this.m_Parent == null && this.m_Location.m_X == x && this.m_Location.m_Y == y);
        }

        public bool AtPoint(int x, int y)
        {
            return (this.m_Location.m_X == x && this.m_Location.m_Y == y);
        }

        /// <summary>
        /// Moves the Item to a given <paramref name="location" /> and <paramref name="map" />.
        /// </summary>
        public void MoveToWorld(Point3D location, Map map)
        {
            if (this.Deleted)
                return;

            Point3D oldLocation = this.GetWorldLocation();
            Point3D oldRealLocation = this.m_Location;

            this.SetLastMoved();

            if (this.Parent is Mobile)
                ((Mobile)this.Parent).RemoveItem(this);
            else if (this.Parent is Item)
                ((Item)this.Parent).RemoveItem(this);

            if (this.m_Map != map)
            {
                Map old = this.m_Map;

                if (this.m_Map != null)
                {
                    this.m_Map.OnLeave(this);

                    if (oldLocation.m_X != 0)
                    {
                        Packet remPacket = null;

                        IPooledEnumerable eable = this.m_Map.GetClientsInRange(oldLocation, this.GetMaxUpdateRange());

                        foreach (NetState state in eable)
                        {
                            Mobile m = state.Mobile;

                            if (m.InRange(oldLocation, this.GetUpdateRange(m)))
                            {
                                if (remPacket == null)
                                    remPacket = this.RemovePacket;

                                state.Send(remPacket);
                            }
                        }

                        eable.Free();
                    }
                }

                this.m_Location = location;
                this.OnLocationChange(oldRealLocation);

                this.ReleaseWorldPackets();

                List<Item> items = this.LookupItems();

                if (items != null)
                {
                    for (int i = 0; i < items.Count; ++i)
                        items[i].Map = map;
                }

                this.m_Map = map;

                if (this.m_Map != null)
                    this.m_Map.OnEnter(this);

                this.OnMapChange();

                if (this.m_Map != null)
                {
                    IPooledEnumerable eable = this.m_Map.GetClientsInRange(this.m_Location, this.GetMaxUpdateRange());

                    foreach (NetState state in eable)
                    {
                        Mobile m = state.Mobile;

                        if (m.CanSee(this) && m.InRange(this.m_Location, this.GetUpdateRange(m)))
                            this.SendInfoTo(state);
                    }

                    eable.Free();
                }

                this.RemDelta(ItemDelta.Update);

                if (old == null || old == Map.Internal)
                    this.InvalidateProperties();
            }
            else if (this.m_Map != null)
            {
                IPooledEnumerable eable;

                if (oldLocation.m_X != 0)
                {
                    Packet removeThis = null;

                    eable = this.m_Map.GetClientsInRange(oldLocation, this.GetMaxUpdateRange());

                    foreach (NetState state in eable)
                    {
                        Mobile m = state.Mobile;

                        if (!m.InRange(location, this.GetUpdateRange(m)))
                        {
                            if (removeThis == null)
                                removeThis = this.RemovePacket;

                            state.Send(removeThis);
                        }
                    }

                    eable.Free();
                }

                Point3D oldInternalLocation = this.m_Location;

                this.m_Location = location;
                this.OnLocationChange(oldRealLocation);

                this.ReleaseWorldPackets();

                eable = this.m_Map.GetClientsInRange(this.m_Location, this.GetMaxUpdateRange());

                foreach (NetState state in eable)
                {
                    Mobile m = state.Mobile;

                    if (m.CanSee(this) && m.InRange(this.m_Location, this.GetUpdateRange(m)))
                        this.SendInfoTo(state);
                }

                eable.Free();

                this.m_Map.OnMove(oldInternalLocation, this);

                this.RemDelta(ItemDelta.Update);
            }
            else
            {
                this.Map = map;
                this.Location = location;
            }
        }

        /// <summary>
        /// Has the item been deleted?
        /// </summary>
        public bool Deleted
        {
            get
            {
                return this.GetFlag(ImplFlag.Deleted);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public LootType LootType
        {
            get
            {
                return this.m_LootType;
            }
            set
            {
                if (this.m_LootType != value)
                {
                    this.m_LootType = value;

                    if (this.DisplayLootType)
                        this.InvalidateProperties();
                }
            }
        }

        private static TimeSpan m_DDT = TimeSpan.FromHours(1.0);

        public static TimeSpan DefaultDecayTime
        {
            get
            {
                return m_DDT;
            }
            set
            {
                m_DDT = value;
            }
        }

        [CommandProperty(AccessLevel.Decorator)]
        public virtual TimeSpan DecayTime
        {
            get
            {
                return m_DDT;
            }
        }

        [CommandProperty(AccessLevel.Decorator)]
        public virtual bool Decays
        {
            get
            {
                return (this.Movable && this.Visible);
            }
        }

        public virtual bool OnDecay()
        {
            return (this.Decays && this.Parent == null && this.Map != Map.Internal && Region.Find(this.Location, this.Map).OnDecay(this));
        }

        public void SetLastMoved()
        {
            this.m_LastMovedTime = DateTime.Now;
        }

        public DateTime LastMoved
        {
            get
            {
                return this.m_LastMovedTime;
            }
            set
            {
                this.m_LastMovedTime = value;
            }
        }

        public bool StackWith(Mobile from, Item dropped)
        {
            return this.StackWith(from, dropped, true);
        }

        public virtual bool StackWith(Mobile from, Item dropped, bool playSound)
        {
            if (dropped.Stackable && this.Stackable && dropped.GetType() == this.GetType() && dropped.ItemID == this.ItemID && dropped.Hue == this.Hue && dropped.Name == this.Name && (dropped.Amount + this.Amount) <= 60000 && dropped != this)
            {
                if (this.m_LootType != dropped.m_LootType)
                    this.m_LootType = LootType.Regular;

                this.Amount += dropped.Amount;
                dropped.Delete();

                if (playSound && from != null)
                {
                    int soundID = this.GetDropSound();

                    if (soundID == -1)
                        soundID = 0x42;

                    from.SendSound(soundID, this.GetWorldLocation());
                }

                return true;
            }

            return false;
        }

        public virtual bool OnDragDrop(Mobile from, Item dropped)
        {
            if (this.Parent is Container)
                return ((Container)this.Parent).OnStackAttempt(from, this, dropped);

            return this.StackWith(from, dropped);
        }

        public Rectangle2D GetGraphicBounds()
        {
            int itemID = this.m_ItemID;
            bool doubled = this.m_Amount > 1;

            if (itemID >= 0xEEA && itemID <= 0xEF2) // Are we coins?
            {
                int coinBase = (itemID - 0xEEA) / 3;
                coinBase *= 3;
                coinBase += 0xEEA;

                doubled = false;

                if (this.m_Amount <= 1)
                {
                    // A single coin
                    itemID = coinBase;
                }
                else if (this.m_Amount <= 5)
                {
                    // A stack of coins
                    itemID = coinBase + 1;
                }
                else // m_Amount > 5
                {
                    // A pile of coins
                    itemID = coinBase + 2;
                }
            }

            Rectangle2D bounds = ItemBounds.Table[itemID & 0x3FFF];

            if (doubled)
            {
                bounds.Set(bounds.X, bounds.Y, bounds.Width + 5, bounds.Height + 5);
            }

            return bounds;
        }

        [CommandProperty(AccessLevel.Decorator)]
        public bool Stackable
        {
            get
            {
                return this.GetFlag(ImplFlag.Stackable);
            }
            set
            {
                this.SetFlag(ImplFlag.Stackable, value);
            }
        }

        public Packet RemovePacket
        {
            get
            {
                if (this.m_RemovePacket == null)
                {
                    this.m_RemovePacket = new RemoveItem(this);
                    this.m_RemovePacket.SetStatic();
                }

                return this.m_RemovePacket;
            }
        }

        public Packet OPLPacket
        {
            get
            {
                if (this.m_OPLPacket == null)
                {
                    this.m_OPLPacket = new OPLInfo(this.PropertyList);
                    this.m_OPLPacket.SetStatic();
                }

                return this.m_OPLPacket;
            }
        }

        public ObjectPropertyList PropertyList
        {
            get
            {
                if (this.m_PropertyList == null)
                {
                    this.m_PropertyList = new ObjectPropertyList(this);

                    this.GetProperties(this.m_PropertyList);
                    this.AppendChildProperties(this.m_PropertyList);

                    this.m_PropertyList.Terminate();
                    this.m_PropertyList.SetStatic();
                }

                return this.m_PropertyList;
            }
        }

        public virtual void AppendChildProperties(ObjectPropertyList list)
        {
            if (this.m_Parent is Item)
                ((Item)this.m_Parent).GetChildProperties(list, this);
            else if (this.m_Parent is Mobile)
                ((Mobile)this.m_Parent).GetChildProperties(list, this);
        }

        public virtual void AppendChildNameProperties(ObjectPropertyList list)
        {
            if (this.m_Parent is Item)
                ((Item)this.m_Parent).GetChildNameProperties(list, this);
            else if (this.m_Parent is Mobile)
                ((Mobile)this.m_Parent).GetChildNameProperties(list, this);
        }

        public void ClearProperties()
        {
            Packet.Release(ref this.m_PropertyList);
            Packet.Release(ref this.m_OPLPacket);
        }

        public void InvalidateProperties()
        {
            if (!ObjectPropertyList.Enabled)
                return;

            if (this.m_Map != null && this.m_Map != Map.Internal && !World.Loading)
            {
                ObjectPropertyList oldList = this.m_PropertyList;
                this.m_PropertyList = null;
                ObjectPropertyList newList = this.PropertyList;

                if (oldList == null || oldList.Hash != newList.Hash)
                {
                    Packet.Release(ref this.m_OPLPacket);
                    this.Delta(ItemDelta.Properties);
                }
            }
            else
            {
                this.ClearProperties();
            }
        }

        public Packet WorldPacket
        {
            get
            {
                // This needs to be invalidated when any of the following changes:
                //  - ItemID
                //  - Amount
                //  - Location
                //  - Hue
                //  - Packet Flags
                //  - Direction
                if (this.m_WorldPacket == null)
                {
                    this.m_WorldPacket = new WorldItem(this);
                    this.m_WorldPacket.SetStatic();
                }

                return this.m_WorldPacket;
            }
        }

        public Packet WorldPacketSA
        {
            get
            {
                // This needs to be invalidated when any of the following changes:
                //  - ItemID
                //  - Amount
                //  - Location
                //  - Hue
                //  - Packet Flags
                //  - Direction
                if (this.m_WorldPacketSA == null)
                {
                    this.m_WorldPacketSA = new WorldItemSA(this);
                    this.m_WorldPacketSA.SetStatic();
                }

                return this.m_WorldPacketSA;
            }
        }

        public Packet WorldPacketHS
        {
            get
            {
                // This needs to be invalidated when any of the following changes:
                //  - ItemID
                //  - Amount
                //  - Location
                //  - Hue
                //  - Packet Flags
                //  - Direction
                if (this.m_WorldPacketHS == null)
                {
                    this.m_WorldPacketHS = new WorldItemHS(this);
                    this.m_WorldPacketHS.SetStatic();
                }

                return this.m_WorldPacketHS;
            }
        }

        public void ReleaseWorldPackets()
        {
            Packet.Release(ref this.m_WorldPacket);
            Packet.Release(ref this.m_WorldPacketSA);
            Packet.Release(ref this.m_WorldPacketHS);
        }

        [CommandProperty(AccessLevel.Decorator)]
        public bool Visible
        {
            get
            {
                return this.GetFlag(ImplFlag.Visible);
            }
            set
            {
                if (this.GetFlag(ImplFlag.Visible) != value)
                {
                    this.SetFlag(ImplFlag.Visible, value);
                    this.ReleaseWorldPackets();

                    if (this.m_Map != null)
                    {
                        Packet removeThis = null;
                        Point3D worldLoc = this.GetWorldLocation();

                        IPooledEnumerable eable = this.m_Map.GetClientsInRange(worldLoc, this.GetMaxUpdateRange());

                        foreach (NetState state in eable)
                        {
                            Mobile m = state.Mobile;

                            if (!m.CanSee(this) && m.InRange(worldLoc, this.GetUpdateRange(m)))
                            {
                                if (removeThis == null)
                                    removeThis = this.RemovePacket;

                                state.Send(removeThis);
                            }
                        }

                        eable.Free();
                    }

                    this.Delta(ItemDelta.Update);
                }
            }
        }

        [CommandProperty(AccessLevel.Decorator)]
        public bool Movable
        {
            get
            {
                return this.GetFlag(ImplFlag.Movable);
            }
            set
            {
                if (this.GetFlag(ImplFlag.Movable) != value)
                {
                    this.SetFlag(ImplFlag.Movable, value);
                    this.ReleaseWorldPackets();
                    this.Delta(ItemDelta.Update);
                }
            }
        }

        public virtual bool ForceShowProperties
        {
            get
            {
                return false;
            }
        }

        public virtual int GetPacketFlags()
        {
            int flags = 0;

            if (!this.Visible)
                flags |= 0x80;

            if (this.Movable || this.ForceShowProperties)
                flags |= 0x20;

            return flags;
        }

        public virtual bool OnMoveOff(Mobile m)
        {
            return true;
        }

        public virtual bool OnMoveOver(Mobile m)
        {
            return true;
        }

        public virtual bool HandlesOnMovement
        {
            get
            {
                return false;
            }
        }

        public virtual void OnMovement(Mobile m, Point3D oldLocation)
        {
        }

        public void Internalize()
        {
            this.MoveToWorld(Point3D.Zero, Map.Internal);
        }

        public virtual void OnMapChange()
        {
        }

        public virtual void OnRemoved(object parent)
        {
        }

        public virtual void OnAdded(object parent)
        {
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Decorator)]
        public Map Map
        {
            get
            {
                return this.m_Map;
            }
            set
            {
                if (this.m_Map != value)
                {
                    Map old = this.m_Map;

                    if (this.m_Map != null && this.m_Parent == null)
                    {
                        this.m_Map.OnLeave(this);
                        this.SendRemovePacket();
                    }

                    List<Item> items = this.LookupItems();

                    if (items != null)
                    {
                        for (int i = 0; i < items.Count; ++i)
                            items[i].Map = value;
                    }

                    this.m_Map = value;

                    if (this.m_Map != null && this.m_Parent == null)
                        this.m_Map.OnEnter(this);

                    this.Delta(ItemDelta.Update);

                    this.OnMapChange();

                    if (old == null || old == Map.Internal)
                        this.InvalidateProperties();
                }
            }
        }

        [Flags]
        private enum SaveFlag
        {
            None = 0x00000000,
            Direction = 0x00000001,
            Bounce = 0x00000002,
            LootType = 0x00000004,
            LocationFull = 0x00000008,
            ItemID = 0x00000010,
            Hue = 0x00000020,
            Amount = 0x00000040,
            Layer = 0x00000080,
            Name = 0x00000100,
            Parent = 0x00000200,
            Items = 0x00000400,
            WeightNot1or0 = 0x00000800,
            Map = 0x00001000,
            Visible = 0x00002000,
            Movable = 0x00004000,
            Stackable = 0x00008000,
            WeightIs0 = 0x00010000,
            LocationSByteZ = 0x00020000,
            LocationShortXY = 0x00040000,
            LocationByteXY = 0x00080000,
            ImplFlags = 0x00100000,
            InsuredFor = 0x00200000,
            BlessedFor = 0x00400000,
            HeldBy = 0x00800000,
            IntWeight = 0x01000000,
            SavedFlags = 0x02000000,
            NullWeight = 0x04000000
        }

        private static void SetSaveFlag(ref SaveFlag flags, SaveFlag toSet, bool setIf)
        {
            if (setIf)
                flags |= toSet;
        }

        private static bool GetSaveFlag(SaveFlag flags, SaveFlag toGet)
        {
            return ((flags & toGet) != 0);
        }

        int ISerializable.TypeReference
        {
            get
            {
                return this.m_TypeRef;
            }
        }

        int ISerializable.SerialIdentity
        {
            get
            {
                return this.m_Serial;
            }
        }

        public virtual void Serialize(GenericWriter writer)
        {
            writer.Write(9); // version

            SaveFlag flags = SaveFlag.None;

            int x = this.m_Location.m_X, y = this.m_Location.m_Y, z = this.m_Location.m_Z;

            if (x != 0 || y != 0 || z != 0)
            {
                if (x >= short.MinValue && x <= short.MaxValue && y >= short.MinValue && y <= short.MaxValue && z >= sbyte.MinValue && z <= sbyte.MaxValue)
                {
                    if (x != 0 || y != 0)
                    {
                        if (x >= byte.MinValue && x <= byte.MaxValue && y >= byte.MinValue && y <= byte.MaxValue)
                            flags |= SaveFlag.LocationByteXY;
                        else
                            flags |= SaveFlag.LocationShortXY;
                    }

                    if (z != 0)
                        flags |= SaveFlag.LocationSByteZ;
                }
                else
                {
                    flags |= SaveFlag.LocationFull;
                }
            }

            CompactInfo info = this.LookupCompactInfo();
            List<Item> items = this.LookupItems();

            if (this.m_Direction != Direction.North)
                flags |= SaveFlag.Direction;
            if (info != null && info.m_Bounce != null)
                flags |= SaveFlag.Bounce;
            if (this.m_LootType != LootType.Regular)
                flags |= SaveFlag.LootType;
            if (this.m_ItemID != 0)
                flags |= SaveFlag.ItemID;
            if (this.m_Hue != 0)
                flags |= SaveFlag.Hue;
            if (this.m_Amount != 1)
                flags |= SaveFlag.Amount;
            if (this.m_Layer != Layer.Invalid)
                flags |= SaveFlag.Layer;
            if (info != null && info.m_Name != null)
                flags |= SaveFlag.Name;
            if (this.m_Parent != null)
                flags |= SaveFlag.Parent;
            if (items != null && items.Count > 0)
                flags |= SaveFlag.Items;
            if (this.m_Map != Map.Internal)
                flags |= SaveFlag.Map;
            //if ( m_InsuredFor != null && !m_InsuredFor.Deleted )
            //flags |= SaveFlag.InsuredFor;
            if (info != null && info.m_BlessedFor != null && !info.m_BlessedFor.Deleted)
                flags |= SaveFlag.BlessedFor;
            if (info != null && info.m_HeldBy != null && !info.m_HeldBy.Deleted)
                flags |= SaveFlag.HeldBy;
            if (info != null && info.m_SavedFlags != 0)
                flags |= SaveFlag.SavedFlags;

            if (info == null || info.m_Weight == -1)
            {
                flags |= SaveFlag.NullWeight;
            }
            else
            {
                if (info.m_Weight == 0.0)
                {
                    flags |= SaveFlag.WeightIs0;
                }
                else if (info.m_Weight != 1.0)
                {
                    if (info.m_Weight == (int)info.m_Weight)
                        flags |= SaveFlag.IntWeight;
                    else
                        flags |= SaveFlag.WeightNot1or0;
                }
            }

            ImplFlag implFlags = (this.m_Flags & (ImplFlag.Visible | ImplFlag.Movable | ImplFlag.Stackable | ImplFlag.Insured | ImplFlag.PayedInsurance | ImplFlag.QuestItem));

            if (implFlags != (ImplFlag.Visible | ImplFlag.Movable))
                flags |= SaveFlag.ImplFlags;

            writer.Write((int)flags);

            /* begin last moved time optimization */
            long ticks = this.m_LastMovedTime.Ticks;
            long now = DateTime.Now.Ticks;

            TimeSpan d;

            try
            {
                d = new TimeSpan(ticks - now);
            }
            catch
            {
                d = TimeSpan.MaxValue;
            }

            double minutes = -d.TotalMinutes;

            if (minutes < int.MinValue)
                minutes = int.MinValue;
            else if (minutes > int.MaxValue)
                minutes = int.MaxValue;

            writer.WriteEncodedInt((int)minutes);
            /* end */

            if (GetSaveFlag(flags, SaveFlag.Direction))
                writer.Write((byte)this.m_Direction);

            if (GetSaveFlag(flags, SaveFlag.Bounce))
                BounceInfo.Serialize(info.m_Bounce, writer);

            if (GetSaveFlag(flags, SaveFlag.LootType))
                writer.Write((byte)this.m_LootType);

            if (GetSaveFlag(flags, SaveFlag.LocationFull))
            {
                writer.WriteEncodedInt(x);
                writer.WriteEncodedInt(y);
                writer.WriteEncodedInt(z);
            }
            else
            {
                if (GetSaveFlag(flags, SaveFlag.LocationByteXY))
                {
                    writer.Write((byte)x);
                    writer.Write((byte)y);
                }
                else if (GetSaveFlag(flags, SaveFlag.LocationShortXY))
                {
                    writer.Write((short)x);
                    writer.Write((short)y);
                }

                if (GetSaveFlag(flags, SaveFlag.LocationSByteZ))
                    writer.Write((sbyte)z);
            }

            if (GetSaveFlag(flags, SaveFlag.ItemID))
                writer.WriteEncodedInt((int)this.m_ItemID);

            if (GetSaveFlag(flags, SaveFlag.Hue))
                writer.WriteEncodedInt((int)this.m_Hue);

            if (GetSaveFlag(flags, SaveFlag.Amount))
                writer.WriteEncodedInt((int)this.m_Amount);

            if (GetSaveFlag(flags, SaveFlag.Layer))
                writer.Write((byte)this.m_Layer);

            if (GetSaveFlag(flags, SaveFlag.Name))
                writer.Write((string)info.m_Name);

            if (GetSaveFlag(flags, SaveFlag.Parent))
            {
                if (this.m_Parent is Mobile && !((Mobile)this.m_Parent).Deleted)
                    writer.Write(((Mobile)this.m_Parent).Serial);
                else if (this.m_Parent is Item && !((Item)this.m_Parent).Deleted)
                    writer.Write(((Item)this.m_Parent).Serial);
                else
                    writer.Write((int)Serial.MinusOne);
            }

            if (GetSaveFlag(flags, SaveFlag.Items))
                writer.Write(items, false);

            if (GetSaveFlag(flags, SaveFlag.IntWeight))
                writer.WriteEncodedInt((int)info.m_Weight);
            else if (GetSaveFlag(flags, SaveFlag.WeightNot1or0))
                writer.Write((double)info.m_Weight);

            if (GetSaveFlag(flags, SaveFlag.Map))
                writer.Write((Map)this.m_Map);

            if (GetSaveFlag(flags, SaveFlag.ImplFlags))
                writer.WriteEncodedInt((int)implFlags);

            if (GetSaveFlag(flags, SaveFlag.InsuredFor))
                writer.Write((Mobile)null);

            if (GetSaveFlag(flags, SaveFlag.BlessedFor))
                writer.Write(info.m_BlessedFor);

            if (GetSaveFlag(flags, SaveFlag.HeldBy))
                writer.Write(info.m_HeldBy);

            if (GetSaveFlag(flags, SaveFlag.SavedFlags))
                writer.WriteEncodedInt(info.m_SavedFlags);
        }

        public IPooledEnumerable GetObjectsInRange(int range)
        {
            Map map = this.m_Map;

            if (map == null)
                return Server.Map.NullEnumerable.Instance;

            if (this.m_Parent == null)
                return map.GetObjectsInRange(this.m_Location, range);

            return map.GetObjectsInRange(this.GetWorldLocation(), range);
        }

        public IPooledEnumerable GetItemsInRange(int range)
        {
            Map map = this.m_Map;

            if (map == null)
                return Server.Map.NullEnumerable.Instance;

            if (this.m_Parent == null)
                return map.GetItemsInRange(this.m_Location, range);

            return map.GetItemsInRange(this.GetWorldLocation(), range);
        }

        public IPooledEnumerable GetMobilesInRange(int range)
        {
            Map map = this.m_Map;

            if (map == null)
                return Server.Map.NullEnumerable.Instance;

            if (this.m_Parent == null)
                return map.GetMobilesInRange(this.m_Location, range);

            return map.GetMobilesInRange(this.GetWorldLocation(), range);
        }

        public IPooledEnumerable GetClientsInRange(int range)
        {
            Map map = this.m_Map;

            if (map == null)
                return Server.Map.NullEnumerable.Instance;

            if (this.m_Parent == null)
                return map.GetClientsInRange(this.m_Location, range);

            return map.GetClientsInRange(this.GetWorldLocation(), range);
        }

        private static int m_LockedDownFlag;
        private static int m_SecureFlag;

        public static int LockedDownFlag
        {
            get
            {
                return m_LockedDownFlag;
            }
            set
            {
                m_LockedDownFlag = value;
            }
        }

        public static int SecureFlag
        {
            get
            {
                return m_SecureFlag;
            }
            set
            {
                m_SecureFlag = value;
            }
        }

        public bool IsLockedDown
        {
            get
            {
                return this.GetTempFlag(m_LockedDownFlag);
            }
            set
            {
                this.SetTempFlag(m_LockedDownFlag, value);
                this.InvalidateProperties();
            }
        }

        public bool IsSecure
        {
            get
            {
                return this.GetTempFlag(m_SecureFlag);
            }
            set
            {
                this.SetTempFlag(m_SecureFlag, value);
                this.InvalidateProperties();
            }
        }

        public bool GetTempFlag(int flag)
        {
            CompactInfo info = this.LookupCompactInfo();

            if (info == null)
                return false;

            return ((info.m_TempFlags & flag) != 0);
        }

        public void SetTempFlag(int flag, bool value)
        {
            CompactInfo info = this.AcquireCompactInfo();

            if (value)
                info.m_TempFlags |= flag;
            else
                info.m_TempFlags &= ~flag;

            if (info.m_TempFlags == 0)
                this.VerifyCompactInfo();
        }

        public bool GetSavedFlag(int flag)
        {
            CompactInfo info = this.LookupCompactInfo();

            if (info == null)
                return false;

            return ((info.m_SavedFlags & flag) != 0);
        }

        public void SetSavedFlag(int flag, bool value)
        {
            CompactInfo info = this.AcquireCompactInfo();

            if (value)
                info.m_SavedFlags |= flag;
            else
                info.m_SavedFlags &= ~flag;

            if (info.m_SavedFlags == 0)
                this.VerifyCompactInfo();
        }

        public virtual void Deserialize(GenericReader reader)
        {
            int version = reader.ReadInt();

            this.SetLastMoved();

            switch ( version )
            {
                case 9:
                case 8:
                case 7:
                case 6:
                    {
                        SaveFlag flags = (SaveFlag)reader.ReadInt();

                        if (version < 7)
                        {
                            this.LastMoved = reader.ReadDeltaTime();
                        }
                        else
                        {
                            int minutes = reader.ReadEncodedInt();

                            try
                            {
                                this.LastMoved = DateTime.Now - TimeSpan.FromMinutes(minutes);
                            }
                            catch
                            {
                                this.LastMoved = DateTime.Now;
                            }
                        }

                        if (GetSaveFlag(flags, SaveFlag.Direction))
                            this.m_Direction = (Direction)reader.ReadByte();

                        if (GetSaveFlag(flags, SaveFlag.Bounce))
                            this.AcquireCompactInfo().m_Bounce = BounceInfo.Deserialize(reader);

                        if (GetSaveFlag(flags, SaveFlag.LootType))
                            this.m_LootType = (LootType)reader.ReadByte();

                        int x = 0, y = 0, z = 0;

                        if (GetSaveFlag(flags, SaveFlag.LocationFull))
                        {
                            x = reader.ReadEncodedInt();
                            y = reader.ReadEncodedInt();
                            z = reader.ReadEncodedInt();
                        }
                        else
                        {
                            if (GetSaveFlag(flags, SaveFlag.LocationByteXY))
                            {
                                x = reader.ReadByte();
                                y = reader.ReadByte();
                            }
                            else if (GetSaveFlag(flags, SaveFlag.LocationShortXY))
                            {
                                x = reader.ReadShort();
                                y = reader.ReadShort();
                            }

                            if (GetSaveFlag(flags, SaveFlag.LocationSByteZ))
                                z = reader.ReadSByte();
                        }

                        this.m_Location = new Point3D(x, y, z);

                        if (GetSaveFlag(flags, SaveFlag.ItemID))
                            this.m_ItemID = reader.ReadEncodedInt();

                        if (GetSaveFlag(flags, SaveFlag.Hue))
                            this.m_Hue = reader.ReadEncodedInt();

                        if (GetSaveFlag(flags, SaveFlag.Amount))
                            this.m_Amount = reader.ReadEncodedInt();
                        else
                            this.m_Amount = 1;

                        if (GetSaveFlag(flags, SaveFlag.Layer))
                            this.m_Layer = (Layer)reader.ReadByte();

                        if (GetSaveFlag(flags, SaveFlag.Name))
                        {
                            string name = reader.ReadString();

                            if (name != this.DefaultName)
                                this.AcquireCompactInfo().m_Name = name;
                        }

                        if (GetSaveFlag(flags, SaveFlag.Parent))
                        {
                            Serial parent = reader.ReadInt();

                            if (parent.IsMobile)
                                this.m_Parent = World.FindMobile(parent);
                            else if (parent.IsItem)
                                this.m_Parent = World.FindItem(parent);
                            else
                                this.m_Parent = null;

                            if (this.m_Parent == null && (parent.IsMobile || parent.IsItem))
                                this.Delete();
                        }

                        if (GetSaveFlag(flags, SaveFlag.Items))
                        {
                            List<Item> items = reader.ReadStrongItemList();

                            if (this is Container)
                                (this as Container).m_Items = items;
                            else
                                this.AcquireCompactInfo().m_Items = items;
                        }

                        if (version < 8 || !GetSaveFlag(flags, SaveFlag.NullWeight))
                        {
                            double weight;

                            if (GetSaveFlag(flags, SaveFlag.IntWeight))
                                weight = reader.ReadEncodedInt();
                            else if (GetSaveFlag(flags, SaveFlag.WeightNot1or0))
                                weight = reader.ReadDouble();
                            else if (GetSaveFlag(flags, SaveFlag.WeightIs0))
                                weight = 0.0;
                            else
                                weight = 1.0;

                            if (weight != this.DefaultWeight)
                                this.AcquireCompactInfo().m_Weight = weight;
                        }

                        if (GetSaveFlag(flags, SaveFlag.Map))
                            this.m_Map = reader.ReadMap();
                        else
                            this.m_Map = Map.Internal;

                        if (GetSaveFlag(flags, SaveFlag.Visible))
                            this.SetFlag(ImplFlag.Visible, reader.ReadBool());
                        else
                            this.SetFlag(ImplFlag.Visible, true);

                        if (GetSaveFlag(flags, SaveFlag.Movable))
                            this.SetFlag(ImplFlag.Movable, reader.ReadBool());
                        else
                            this.SetFlag(ImplFlag.Movable, true);

                        if (GetSaveFlag(flags, SaveFlag.Stackable))
                            this.SetFlag(ImplFlag.Stackable, reader.ReadBool());

                        if (GetSaveFlag(flags, SaveFlag.ImplFlags))
                            this.m_Flags = (ImplFlag)reader.ReadEncodedInt();

                        if (GetSaveFlag(flags, SaveFlag.InsuredFor))
                            /*m_InsuredFor = */reader.ReadMobile();

                        if (GetSaveFlag(flags, SaveFlag.BlessedFor))
                            this.AcquireCompactInfo().m_BlessedFor = reader.ReadMobile();

                        if (GetSaveFlag(flags, SaveFlag.HeldBy))
                            this.AcquireCompactInfo().m_HeldBy = reader.ReadMobile();

                        if (GetSaveFlag(flags, SaveFlag.SavedFlags))
                            this.AcquireCompactInfo().m_SavedFlags = reader.ReadEncodedInt();

                        if (this.m_Map != null && this.m_Parent == null)
                            this.m_Map.OnEnter(this);

                        break;
                    }
                case 5:
                    {
                        SaveFlag flags = (SaveFlag)reader.ReadInt();

                        this.LastMoved = reader.ReadDeltaTime();

                        if (GetSaveFlag(flags, SaveFlag.Direction))
                            this.m_Direction = (Direction)reader.ReadByte();

                        if (GetSaveFlag(flags, SaveFlag.Bounce))
                            this.AcquireCompactInfo().m_Bounce = BounceInfo.Deserialize(reader);

                        if (GetSaveFlag(flags, SaveFlag.LootType))
                            this.m_LootType = (LootType)reader.ReadByte();

                        if (GetSaveFlag(flags, SaveFlag.LocationFull))
                            this.m_Location = reader.ReadPoint3D();

                        if (GetSaveFlag(flags, SaveFlag.ItemID))
                            this.m_ItemID = reader.ReadInt();

                        if (GetSaveFlag(flags, SaveFlag.Hue))
                            this.m_Hue = reader.ReadInt();

                        if (GetSaveFlag(flags, SaveFlag.Amount))
                            this.m_Amount = reader.ReadInt();
                        else
                            this.m_Amount = 1;

                        if (GetSaveFlag(flags, SaveFlag.Layer))
                            this.m_Layer = (Layer)reader.ReadByte();

                        if (GetSaveFlag(flags, SaveFlag.Name))
                        {
                            string name = reader.ReadString();

                            if (name != this.DefaultName)
                                this.AcquireCompactInfo().m_Name = name;
                        }

                        if (GetSaveFlag(flags, SaveFlag.Parent))
                        {
                            Serial parent = reader.ReadInt();

                            if (parent.IsMobile)
                                this.m_Parent = World.FindMobile(parent);
                            else if (parent.IsItem)
                                this.m_Parent = World.FindItem(parent);
                            else
                                this.m_Parent = null;

                            if (this.m_Parent == null && (parent.IsMobile || parent.IsItem))
                                this.Delete();
                        }

                        if (GetSaveFlag(flags, SaveFlag.Items))
                        {
                            List<Item> items = reader.ReadStrongItemList();

                            if (this is Container)
                                (this as Container).m_Items = items;
                            else
                                this.AcquireCompactInfo().m_Items = items;
                        }

                        double weight;

                        if (GetSaveFlag(flags, SaveFlag.IntWeight))
                            weight = reader.ReadEncodedInt();
                        else if (GetSaveFlag(flags, SaveFlag.WeightNot1or0))
                            weight = reader.ReadDouble();
                        else if (GetSaveFlag(flags, SaveFlag.WeightIs0))
                            weight = 0.0;
                        else
                            weight = 1.0;

                        if (weight != this.DefaultWeight)
                            this.AcquireCompactInfo().m_Weight = weight;

                        if (GetSaveFlag(flags, SaveFlag.Map))
                            this.m_Map = reader.ReadMap();
                        else
                            this.m_Map = Map.Internal;

                        if (GetSaveFlag(flags, SaveFlag.Visible))
                            this.SetFlag(ImplFlag.Visible, reader.ReadBool());
                        else
                            this.SetFlag(ImplFlag.Visible, true);

                        if (GetSaveFlag(flags, SaveFlag.Movable))
                            this.SetFlag(ImplFlag.Movable, reader.ReadBool());
                        else
                            this.SetFlag(ImplFlag.Movable, true);

                        if (GetSaveFlag(flags, SaveFlag.Stackable))
                            this.SetFlag(ImplFlag.Stackable, reader.ReadBool());

                        if (this.m_Map != null && this.m_Parent == null)
                            this.m_Map.OnEnter(this);

                        break;
                    }
                case 4: // Just removed variables
                case 3:
                    {
                        this.m_Direction = (Direction)reader.ReadInt();

                        goto case 2;
                    }
                case 2:
                    {
                        this.AcquireCompactInfo().m_Bounce = BounceInfo.Deserialize(reader);
                        this.LastMoved = reader.ReadDeltaTime();

                        goto case 1;
                    }
                case 1:
                    {
                        this.m_LootType = (LootType)reader.ReadByte();//m_Newbied = reader.ReadBool();

                        goto case 0;
                    }
                case 0:
                    {
                        this.m_Location = reader.ReadPoint3D();
                        this.m_ItemID = reader.ReadInt();
                        this.m_Hue = reader.ReadInt();
                        this.m_Amount = reader.ReadInt();
                        this.m_Layer = (Layer)reader.ReadByte();

                        string name = reader.ReadString();

                        if (name != this.DefaultName)
                            this.AcquireCompactInfo().m_Name = name;

                        Serial parent = reader.ReadInt();

                        if (parent.IsMobile)
                            this.m_Parent = World.FindMobile(parent);
                        else if (parent.IsItem)
                            this.m_Parent = World.FindItem(parent);
                        else
                            this.m_Parent = null;

                        if (this.m_Parent == null && (parent.IsMobile || parent.IsItem))
                            this.Delete();

                        int count = reader.ReadInt();

                        if (count > 0)
                        {
                            List<Item> items = new List<Item>(count);

                            for (int i = 0; i < count; ++i)
                            {
                                Item item = reader.ReadItem();

                                if (item != null)
                                    items.Add(item);
                            }

                            if (this is Container)
                                (this as Container).m_Items = items;
                            else
                                this.AcquireCompactInfo().m_Items = items;
                        }

                        double weight = reader.ReadDouble();

                        if (weight != this.DefaultWeight)
                            this.AcquireCompactInfo().m_Weight = weight;

                        if (version <= 3)
                        {
                            reader.ReadInt();
                            reader.ReadInt();
                            reader.ReadInt();
                        }

                        this.m_Map = reader.ReadMap();
                        this.SetFlag(ImplFlag.Visible, reader.ReadBool());
                        this.SetFlag(ImplFlag.Movable, reader.ReadBool());

                        if (version <= 3)
                            /*m_Deleted =*/ reader.ReadBool();

                        this.Stackable = reader.ReadBool();

                        if (this.m_Map != null && this.m_Parent == null)
                            this.m_Map.OnEnter(this);

                        break;
                    }
            }

            if (this.HeldBy != null)
                Timer.DelayCall(TimeSpan.Zero, new TimerCallback(FixHolding_Sandbox));

            //if ( version < 9 )
            this.VerifyCompactInfo();
        }

        private void FixHolding_Sandbox()
        {
            Mobile heldBy = this.HeldBy;

            if (heldBy != null)
            {
                if (this.GetBounce() != null)
                {
                    this.Bounce(heldBy);
                }
                else
                {
                    heldBy.Holding = null;
                    heldBy.AddToBackpack(this);
                    this.ClearBounce();
                }
            }
        }

        public virtual int GetMaxUpdateRange()
        {
            return 18;
        }

        public virtual int GetUpdateRange(Mobile m)
        {
            return 18;
        }

        public void SendInfoTo(NetState state)
        {
            this.SendInfoTo(state, ObjectPropertyList.Enabled);
        }

        public virtual void SendInfoTo(NetState state, bool sendOplPacket)
        {
            state.Send(this.GetWorldPacketFor(state));

            if (sendOplPacket)
            {
                state.Send(this.OPLPacket);
            }
        }

        protected virtual Packet GetWorldPacketFor(NetState state)
        {
            if (state.HighSeas)
                return this.WorldPacketHS;
            else if (state.StygianAbyss)
                return this.WorldPacketSA;
            else
                return this.WorldPacket;
        }

        public virtual bool IsVirtualItem
        {
            get
            {
                return false;
            }
        }

        public virtual int GetTotal(TotalType type)
        {
            return 0;
        }

        public virtual void UpdateTotal(Item sender, TotalType type, int delta)
        {
            if (!this.IsVirtualItem)
            {
                if (this.m_Parent is Item)
                    (this.m_Parent as Item).UpdateTotal(sender, type, delta);
                else if (this.m_Parent is Mobile)
                    (this.m_Parent as Mobile).UpdateTotal(sender, type, delta);
                else if (this.HeldBy != null)
                    (this.HeldBy as Mobile).UpdateTotal(sender, type, delta);			
            }
        }

        public virtual void UpdateTotals()
        {
        }

        public virtual int LabelNumber
        {
            get
            {
                if (this.m_ItemID < 0x4000)
                    return 1020000 + this.m_ItemID;
                else
                    return 1078872 + this.m_ItemID;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int TotalGold
        {
            get
            {
                return this.GetTotal(TotalType.Gold);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int TotalItems
        {
            get
            {
                return this.GetTotal(TotalType.Items);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int TotalWeight
        {
            get
            {
                return this.GetTotal(TotalType.Weight);
            }
        }

        public virtual double DefaultWeight
        {
            get
            {
                if (this.m_ItemID < 0 || this.m_ItemID > TileData.MaxItemValue || this is BaseMulti)
                    return 0;

                int weight = TileData.ItemTable[this.m_ItemID].Weight;

                if (weight == 255 || weight == 0)
                    weight = 1;

                return weight;
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public double Weight
        {
            get
            {
                CompactInfo info = this.LookupCompactInfo();

                if (info != null && info.m_Weight != -1)
                    return info.m_Weight;

                return this.DefaultWeight;
            }
            set
            {
                if (this.Weight != value)
                {
                    CompactInfo info = this.AcquireCompactInfo();

                    int oldPileWeight = this.PileWeight;

                    info.m_Weight = value;

                    if (info.m_Weight == -1)
                        this.VerifyCompactInfo();

                    int newPileWeight = this.PileWeight;

                    this.UpdateTotal(this, TotalType.Weight, newPileWeight - oldPileWeight);

                    this.InvalidateProperties();
                }
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public int PileWeight
        {
            get
            {
                return (int)Math.Ceiling(this.Weight * this.Amount);
            }
        }

        public virtual int HuedItemID
        {
            get
            {
                return this.m_ItemID;
            }
        }

        [Hue, CommandProperty(AccessLevel.Decorator)]
        public virtual int Hue
        {
            get
            {
                return (this.QuestItem ? this.QuestItemHue : this.m_Hue);
            }
            set
            {
                if (this.m_Hue != value)
                {
                    this.m_Hue = value;
                    this.ReleaseWorldPackets();

                    this.Delta(ItemDelta.Update);
                }
            }
        }

        public virtual int QuestItemHue
        {
            get
            {
                return 0x04EA;
            }//HMMMM... For EA?
        }

        public virtual bool Nontransferable
        {
            get
            {
                return this.QuestItem;
            }
        }

        public virtual void HandleInvalidTransfer(Mobile from)
        {
            if (this.QuestItem)
                from.SendLocalizedMessage(1074769); // An item must be in your backpack (and not in a container within) to be toggled as a quest item.
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual Layer Layer
        {
            get
            {
                return this.m_Layer;
            }
            set
            {
                if (this.m_Layer != value)
                {
                    this.m_Layer = value;

                    this.Delta(ItemDelta.EquipOnly);
                }
            }
        }

        public List<Item> Items
        {
            get
            {
                List<Item> items = this.LookupItems();

                if (items == null)
                    items = EmptyItems;

                return items;
            }
        }

        public object RootParent
        {
            get
            {
                object p = this.m_Parent;

                while (p is Item)
                {
                    Item item = (Item)p;

                    if (item.m_Parent == null)
                    {
                        break;
                    }
                    else
                    {
                        p = item.m_Parent;
                    }
                }

                return p;
            }
        }

        public bool ParentsContain<T>() where T : Item
        {
            object p = this.m_Parent;

            while (p is Item)
            {
                if (p is T)
                    return true;

                Item item = (Item)p;

                if (item.m_Parent == null)
                {
                    break;
                }
                else
                {
                    p = item.m_Parent;
                }
            }

            return false;
        }

        public virtual void AddItem(Item item)
        {
            if (item == null || item.Deleted || item.m_Parent == this)
            {
                return;
            }
            else if (item == this)
            {
                Console.WriteLine("Warning: Adding item to itself: [0x{0:X} {1}].AddItem( [0x{2:X} {3}] )", this.Serial.Value, this.GetType().Name, item.Serial.Value, item.GetType().Name);
                Console.WriteLine(new System.Diagnostics.StackTrace());
                return;
            }
            else if (this.IsChildOf(item))
            {
                Console.WriteLine("Warning: Adding parent item to child: [0x{0:X} {1}].AddItem( [0x{2:X} {3}] )", this.Serial.Value, this.GetType().Name, item.Serial.Value, item.GetType().Name);
                Console.WriteLine(new System.Diagnostics.StackTrace());
                return;
            }
            else if (item.m_Parent is Mobile)
            {
                ((Mobile)item.m_Parent).RemoveItem(item);
            }
            else if (item.m_Parent is Item)
            {
                ((Item)item.m_Parent).RemoveItem(item);
            }
            else
            {
                item.SendRemovePacket();
            }

            item.Parent = this;
            item.Map = this.m_Map;

            List<Item> items = this.AcquireItems();

            items.Add(item);

            if (!item.IsVirtualItem)
            {
                this.UpdateTotal(item, TotalType.Gold, item.TotalGold);
                this.UpdateTotal(item, TotalType.Items, item.TotalItems + 1);
                this.UpdateTotal(item, TotalType.Weight, item.TotalWeight + item.PileWeight);
            }

            item.Delta(ItemDelta.Update);

            item.OnAdded(this);
            this.OnItemAdded(item);
        }

        private static readonly List<Item> m_DeltaQueue = new List<Item>();

        public void Delta(ItemDelta flags)
        {
            if (this.m_Map == null || this.m_Map == Map.Internal)
                return;

            this.m_DeltaFlags |= flags;

            if (!this.GetFlag(ImplFlag.InQueue))
            {
                this.SetFlag(ImplFlag.InQueue, true);

                m_DeltaQueue.Add(this);
            }

            Core.Set();
        }

        public void RemDelta(ItemDelta flags)
        {
            this.m_DeltaFlags &= ~flags;

            if (this.GetFlag(ImplFlag.InQueue) && this.m_DeltaFlags == ItemDelta.None)
            {
                this.SetFlag(ImplFlag.InQueue, false);

                m_DeltaQueue.Remove(this);
            }
        }

        public void ProcessDelta()
        {
            ItemDelta flags = this.m_DeltaFlags;

            this.SetFlag(ImplFlag.InQueue, false);
            this.m_DeltaFlags = ItemDelta.None;

            Map map = this.m_Map;

            if (map != null && !this.Deleted)
            {
                bool sendOPLUpdate = ObjectPropertyList.Enabled && (flags & ItemDelta.Properties) != 0;

                Container contParent = this.m_Parent as Container;

                if (contParent != null && !contParent.IsPublicContainer)
                {
                    if ((flags & ItemDelta.Update) != 0)
                    {
                        Point3D worldLoc = this.GetWorldLocation();

                        Mobile rootParent = contParent.RootParent as Mobile;
                        Mobile tradeRecip = null;

                        if (rootParent != null)
                        {
                            NetState ns = rootParent.NetState;

                            if (ns != null)
                            {
                                if (rootParent.CanSee(this) && rootParent.InRange(worldLoc, this.GetUpdateRange(rootParent)))
                                {
                                    if (ns.ContainerGridLines)
                                        ns.Send(new ContainerContentUpdate6017(this));
                                    else
                                        ns.Send(new ContainerContentUpdate(this));

                                    if (ObjectPropertyList.Enabled)
                                        ns.Send(this.OPLPacket);
                                }
                            }
                        }

                        SecureTradeContainer stc = this.GetSecureTradeCont();

                        if (stc != null)
                        {
                            SecureTrade st = stc.Trade;

                            if (st != null)
                            {
                                Mobile test = st.From.Mobile;

                                if (test != null && test != rootParent)
                                    tradeRecip = test;

                                test = st.To.Mobile;

                                if (test != null && test != rootParent)
                                    tradeRecip = test;

                                if (tradeRecip != null)
                                {
                                    NetState ns = tradeRecip.NetState;

                                    if (ns != null)
                                    {
                                        if (tradeRecip.CanSee(this) && tradeRecip.InRange(worldLoc, this.GetUpdateRange(tradeRecip)))
                                        {
                                            if (ns.ContainerGridLines)
                                                ns.Send(new ContainerContentUpdate6017(this));
                                            else
                                                ns.Send(new ContainerContentUpdate(this));

                                            if (ObjectPropertyList.Enabled)
                                                ns.Send(this.OPLPacket);
                                        }
                                    }
                                }
                            }
                        }

                        List<Mobile> openers = contParent.Openers;

                        if (openers != null)
                        {
                            for (int i = 0; i < openers.Count; ++i)
                            {
                                Mobile mob = openers[i];

                                int range = this.GetUpdateRange(mob);

                                if (mob.Map != map || !mob.InRange(worldLoc, range))
                                {
                                    openers.RemoveAt(i--);
                                }
                                else
                                {
                                    if (mob == rootParent || mob == tradeRecip)
                                        continue;

                                    NetState ns = mob.NetState;

                                    if (ns != null)
                                    {
                                        if (mob.CanSee(this))
                                        {
                                            if (ns.ContainerGridLines)
                                                ns.Send(new ContainerContentUpdate6017(this));
                                            else
                                                ns.Send(new ContainerContentUpdate(this));

                                            if (ObjectPropertyList.Enabled)
                                                ns.Send(this.OPLPacket);
                                        }
                                    }
                                }
                            }

                            if (openers.Count == 0)
                                contParent.Openers = null;
                        }
                        return;
                    }
                }

                if ((flags & ItemDelta.Update) != 0)
                {
                    Packet p = null;
                    Point3D worldLoc = this.GetWorldLocation();

                    IPooledEnumerable eable = map.GetClientsInRange(worldLoc, this.GetMaxUpdateRange());

                    foreach (NetState state in eable)
                    {
                        Mobile m = state.Mobile;

                        if (m.CanSee(this) && m.InRange(worldLoc, this.GetUpdateRange(m)))
                        {
                            if (this.m_Parent == null)
                            {
                                this.SendInfoTo(state, ObjectPropertyList.Enabled);
                            }
                            else
                            {
                                if (p == null)
                                {
                                    if (this.m_Parent is Item)
                                    {
                                        if (state.ContainerGridLines)
                                            state.Send(new ContainerContentUpdate6017(this));
                                        else
                                            state.Send(new ContainerContentUpdate(this));
                                    }
                                    else if (this.m_Parent is Mobile)
                                    {
                                        p = new EquipUpdate(this);
                                        p.Acquire();
                                        state.Send(p);
                                    }
                                }
                                else
                                {
                                    state.Send(p);
                                }

                                if (ObjectPropertyList.Enabled)
                                {
                                    state.Send(this.OPLPacket);
                                }
                            }
                        }
                    }

                    if (p != null)
                        Packet.Release(p);

                    eable.Free();
                    sendOPLUpdate = false;
                }
                else if ((flags & ItemDelta.EquipOnly) != 0)
                {
                    if (this.m_Parent is Mobile)
                    {
                        Packet p = null;
                        Point3D worldLoc = this.GetWorldLocation();

                        IPooledEnumerable eable = map.GetClientsInRange(worldLoc, this.GetMaxUpdateRange());

                        foreach (NetState state in eable)
                        {
                            Mobile m = state.Mobile;

                            if (m.CanSee(this) && m.InRange(worldLoc, this.GetUpdateRange(m)))
                            {
                                //if ( sendOPLUpdate )
                                //	state.Send( RemovePacket );
                                if (p == null)
                                    p = Packet.Acquire(new EquipUpdate(this));

                                state.Send(p);

                                if (ObjectPropertyList.Enabled)
                                    state.Send(this.OPLPacket);
                            }
                        }

                        Packet.Release(p);

                        eable.Free();
                        sendOPLUpdate = false;
                    }
                }

                if (sendOPLUpdate)
                {
                    Point3D worldLoc = this.GetWorldLocation();
                    IPooledEnumerable eable = map.GetClientsInRange(worldLoc, this.GetMaxUpdateRange());

                    foreach (NetState state in eable)
                    {
                        Mobile m = state.Mobile;

                        if (m.CanSee(this) && m.InRange(worldLoc, this.GetUpdateRange(m)))
                            state.Send(this.OPLPacket);
                    }

                    eable.Free();
                }
            }
        }

        public static void ProcessDeltaQueue()
        {
            int count = m_DeltaQueue.Count;

            for (int i = 0; i < m_DeltaQueue.Count; ++i)
            {
                m_DeltaQueue[i].ProcessDelta();

                if (i >= count)
                    break;
            }

            if (m_DeltaQueue.Count > 0)
                m_DeltaQueue.Clear();
        }

        public virtual void OnDelete()
        {
            if (this.Spawner != null)
            {
                this.Spawner.Remove(this);
                this.Spawner = null;
            }
        }

        public virtual void OnParentDeleted(object parent)
        {
            this.Delete();
        }

        public virtual void FreeCache()
        {
            this.ReleaseWorldPackets();
            Packet.Release(ref this.m_RemovePacket);
            Packet.Release(ref this.m_OPLPacket);
            Packet.Release(ref this.m_PropertyList);
        }

        public virtual void Delete()
        {
            if (this.Deleted)
                return;
            else if (!World.OnDelete(this))
                return;

            this.OnDelete();

            List<Item> items = this.LookupItems();

            if (items != null)
            {
                for (int i = items.Count - 1; i >= 0; --i)
                {
                    if (i < items.Count)
                        items[i].OnParentDeleted(this);
                }
            }

            this.SendRemovePacket();

            this.SetFlag(ImplFlag.Deleted, true);

            if (this.Parent is Mobile)
                ((Mobile)this.Parent).RemoveItem(this);
            else if (this.Parent is Item)
                ((Item)this.Parent).RemoveItem(this);

            this.ClearBounce();

            if (this.m_Map != null)
            {
                if (this.m_Parent == null)
                    this.m_Map.OnLeave(this);
                this.m_Map = null;
            }

            World.RemoveItem(this);

            this.OnAfterDelete();

            this.FreeCache();
        }

        public void PublicOverheadMessage(MessageType type, int hue, bool ascii, string text)
        {
            if (this.m_Map != null)
            {
                Packet p = null;
                Point3D worldLoc = this.GetWorldLocation();

                IPooledEnumerable eable = this.m_Map.GetClientsInRange(worldLoc, this.GetMaxUpdateRange());

                foreach (NetState state in eable)
                {
                    Mobile m = state.Mobile;

                    if (m.CanSee(this) && m.InRange(worldLoc, this.GetUpdateRange(m)))
                    {
                        if (p == null)
                        {
                            if (ascii)
                                p = new AsciiMessage(this.m_Serial, this.m_ItemID, type, hue, 3, this.Name, text);
                            else
                                p = new UnicodeMessage(this.m_Serial, this.m_ItemID, type, hue, 3, "ENU", this.Name, text);

                            p.Acquire();
                        }

                        state.Send(p);
                    }
                }

                Packet.Release(p);

                eable.Free();
            }
        }

        public void PublicOverheadMessage(MessageType type, int hue, int number)
        {
            this.PublicOverheadMessage(type, hue, number, "");
        }

        public void PublicOverheadMessage(MessageType type, int hue, int number, string args)
        {
            if (this.m_Map != null)
            {
                Packet p = null;
                Point3D worldLoc = this.GetWorldLocation();

                IPooledEnumerable eable = this.m_Map.GetClientsInRange(worldLoc, this.GetMaxUpdateRange());

                foreach (NetState state in eable)
                {
                    Mobile m = state.Mobile;

                    if (m.CanSee(this) && m.InRange(worldLoc, this.GetUpdateRange(m)))
                    {
                        if (p == null)
                            p = Packet.Acquire(new MessageLocalized(this.m_Serial, this.m_ItemID, type, hue, 3, number, this.Name, args));

                        state.Send(p);
                    }
                }

                Packet.Release(p);

                eable.Free();
            }
        }

        public virtual void OnAfterDelete()
        {
            foreach (BaseModule module in World.GetModules(this))
            {
                module.Delete();
            }
        }

        public virtual void RemoveItem(Item item)
        {
            List<Item> items = this.LookupItems();

            if (items != null && items.Contains(item))
            {
                item.SendRemovePacket();

                items.Remove(item);

                if (!item.IsVirtualItem)
                {
                    this.UpdateTotal(item, TotalType.Gold, -item.TotalGold);
                    this.UpdateTotal(item, TotalType.Items, -(item.TotalItems + 1));
                    this.UpdateTotal(item, TotalType.Weight, -(item.TotalWeight + item.PileWeight));
                }

                item.Parent = null;

                item.OnRemoved(this);
                this.OnItemRemoved(item);
            }
        }

        public virtual void OnAfterDuped(Item newItem)
        {
        }

        public virtual bool OnDragLift(Mobile from)
        {
            return true;
        }

        public virtual bool OnEquip(Mobile from)
        {
            return true;
        }

        public ISpawner Spawner
        { 
            get
            {
                CompactInfo info = this.LookupCompactInfo();

                if (info != null)
                    return info.m_Spawner;

                return null;
            }
            set
            {
                CompactInfo info = this.AcquireCompactInfo();

                info.m_Spawner = value;

                if (info.m_Spawner == null)
                    this.VerifyCompactInfo();
            }
        }

        public virtual void OnBeforeSpawn(Point3D location, Map m)
        {
        }

        public virtual void OnAfterSpawn()
        {
        }

        public virtual int PhysicalResistance
        {
            get
            {
                return 0;
            }
        }
        public virtual int FireResistance
        {
            get
            {
                return 0;
            }
        }
        public virtual int ColdResistance
        {
            get
            {
                return 0;
            }
        }
        public virtual int PoisonResistance
        {
            get
            {
                return 0;
            }
        }
        public virtual int EnergyResistance
        {
            get
            {
                return 0;
            }
        }

        [CommandProperty(AccessLevel.Counselor)]
        public Serial Serial
        {
            get
            {
                return this.m_Serial;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public IEntity ParentEntity
        {
            get
            {
                IEntity p = this.Parent as IEntity;

                return p;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public IEntity RootParentEntity
        {
            get
            {
                IEntity p = this.RootParent as IEntity;

                return p;
            }
        }

        #region Location Location Location!

        public virtual void OnLocationChange(Point3D oldLocation)
        {
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Decorator)]
        public virtual Point3D Location
        {
            get
            {
                return this.m_Location;
            }
            set
            {
                Point3D oldLocation = this.m_Location;

                if (oldLocation != value)
                {
                    if (this.m_Map != null)
                    {
                        if (this.m_Parent == null)
                        {
                            IPooledEnumerable eable;

                            if (this.m_Location.m_X != 0)
                            {
                                Packet removeThis = null;

                                eable = this.m_Map.GetClientsInRange(oldLocation, this.GetMaxUpdateRange());

                                foreach (NetState state in eable)
                                {
                                    Mobile m = state.Mobile;

                                    if (!m.InRange(value, this.GetUpdateRange(m)))
                                    {
                                        if (removeThis == null)
                                            removeThis = this.RemovePacket;

                                        state.Send(removeThis);
                                    }
                                }

                                eable.Free();
                            }

                            this.m_Location = value;
                            this.ReleaseWorldPackets();

                            this.SetLastMoved();

                            eable = this.m_Map.GetClientsInRange(this.m_Location, this.GetMaxUpdateRange());

                            foreach (NetState state in eable)
                            {
                                Mobile m = state.Mobile;

                                if (m.CanSee(this) && m.InRange(this.m_Location, this.GetUpdateRange(m)))
                                    this.SendInfoTo(state);
                            }

                            eable.Free();

                            this.RemDelta(ItemDelta.Update);
                        }
                        else if (this.m_Parent is Item)
                        {
                            this.m_Location = value;
                            this.ReleaseWorldPackets();

                            this.Delta(ItemDelta.Update);
                        }
                        else
                        {
                            this.m_Location = value;
                            this.ReleaseWorldPackets();
                        }

                        if (this.m_Parent == null)
                            this.m_Map.OnMove(oldLocation, this);
                    }
                    else
                    {
                        this.m_Location = value;
                        this.ReleaseWorldPackets();
                    }

                    this.OnLocationChange(oldLocation);
                }
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Decorator)]
        public int X
        {
            get
            {
                return this.m_Location.m_X;
            }
            set
            {
                this.Location = new Point3D(value, this.m_Location.m_Y, this.m_Location.m_Z);
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Decorator)]
        public int Y
        {
            get
            {
                return this.m_Location.m_Y;
            }
            set
            {
                this.Location = new Point3D(this.m_Location.m_X, value, this.m_Location.m_Z);
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Decorator)]
        public int Z
        {
            get
            {
                return this.m_Location.m_Z;
            }
            set
            {
                this.Location = new Point3D(this.m_Location.m_X, this.m_Location.m_Y, value);
            }
        }
        #endregion

        [CommandProperty(AccessLevel.Decorator)]
        public virtual int ItemID
        {
            get
            {
                return this.m_ItemID;
            }
            set
            {
                if (this.m_ItemID != value)
                {
                    int oldPileWeight = this.PileWeight;

                    this.m_ItemID = value;
                    this.ReleaseWorldPackets();

                    int newPileWeight = this.PileWeight;

                    this.UpdateTotal(this, TotalType.Weight, newPileWeight - oldPileWeight);

                    this.InvalidateProperties();
                    this.Delta(ItemDelta.Update);
                }
            }
        }

        public virtual string DefaultName
        {
            get
            {
                return null;
            }
        }

        [CommandProperty(AccessLevel.Decorator)]
        public string Name
        {
            get
            {
                CompactInfo info = this.LookupCompactInfo();

                if (info != null && info.m_Name != null)
                    return info.m_Name;

                return this.DefaultName;
            }
            set
            {
                if (value == null || value != this.DefaultName)
                {
                    CompactInfo info = this.AcquireCompactInfo();

                    info.m_Name = value;

                    if (info.m_Name == null)
                        this.VerifyCompactInfo();

                    this.InvalidateProperties();
                }
            }
        }

        public virtual object Parent
        {
            get
            {
                return this.m_Parent;
            }
            set
            {
                if (this.m_Parent == value)
                    return;

                object oldParent = this.m_Parent;

                this.m_Parent = value;

                if (this.m_Map != null)
                {
                    if (oldParent != null && this.m_Parent == null)
                        this.m_Map.OnEnter(this);
                    else if (this.m_Parent != null)
                        this.m_Map.OnLeave(this);
                }
            }
        }

        [CommandProperty(AccessLevel.Decorator)]
        public LightType Light
        {
            get
            {
                return (LightType)this.m_Direction;
            }
            set
            {
                if ((LightType)this.m_Direction != value)
                {
                    this.m_Direction = (Direction)value;
                    this.ReleaseWorldPackets();

                    this.Delta(ItemDelta.Update);
                }
            }
        }

        [CommandProperty(AccessLevel.Decorator)]
        public Direction Direction
        {
            get
            {
                return this.m_Direction;
            }
            set
            {
                if (this.m_Direction != value)
                {
                    this.m_Direction = value;
                    this.ReleaseWorldPackets();

                    this.Delta(ItemDelta.Update);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Amount
        {
            get
            {
                return this.m_Amount;
            }
            set
            {
                int oldValue = this.m_Amount;

                if (oldValue != value)
                {
                    int oldPileWeight = this.PileWeight;

                    this.m_Amount = value;
                    this.ReleaseWorldPackets();

                    int newPileWeight = this.PileWeight;

                    this.UpdateTotal(this, TotalType.Weight, newPileWeight - oldPileWeight);

                    this.OnAmountChange(oldValue);

                    this.Delta(ItemDelta.Update);

                    if (oldValue > 1 || value > 1)
                        this.InvalidateProperties();

                    if (!this.Stackable && this.m_Amount > 1)
                        Console.WriteLine("Warning: 0x{0:X}: Amount changed for non-stackable item '{2}'. ({1})", this.m_Serial.Value, this.m_Amount, this.GetType().Name);
                }
            }
        }

        protected virtual void OnAmountChange(int oldValue)
        {
        }

        public virtual bool HandlesOnSpeech
        {
            get
            {
                return false;
            }
        }

        public virtual void OnSpeech(SpeechEventArgs e)
        {
        }

        public virtual bool OnDroppedToMobile(Mobile from, Mobile target)
        {
            if (this.Nontransferable && from.Player && from.AccessLevel <= AccessLevel.GameMaster)
            {
                this.HandleInvalidTransfer(from);
                return false;
            }

            return true;
        }

        public virtual bool DropToMobile(Mobile from, Mobile target, Point3D p)
        {
            if (this.Deleted || from.Deleted || target.Deleted || from.Map != target.Map || from.Map == null || target.Map == null)
                return false;
            else if (from.AccessLevel < AccessLevel.GameMaster && !from.InRange(target.Location, 2))
                return false;
            else if (!from.CanSee(target) || !from.InLOS(target))
                return false;
            else if (!from.OnDroppedItemToMobile(this, target))
                return false;
            else if (!this.OnDroppedToMobile(from, target))
                return false;
            else if (!target.OnDragDrop(from, this))
                return false;
            else
                return true;
        }

        public virtual bool OnDroppedInto(Mobile from, Container target, Point3D p)
        {
            if (!from.OnDroppedItemInto(this, target, p))
            {
                return false;
            }
            else if (this.Nontransferable && from.Player && target != from.Backpack && from.AccessLevel <= AccessLevel.GameMaster)
            {
                this.HandleInvalidTransfer(from);
                return false;
            }

            return target.OnDragDropInto(from, this, p);
        }

        public virtual bool OnDroppedOnto(Mobile from, Item target)
        {
            if (this.Deleted || from.Deleted || target.Deleted || from.Map != target.Map || from.Map == null || target.Map == null)
                return false;
            else if (from.AccessLevel < AccessLevel.GameMaster && !from.InRange(target.GetWorldLocation(), 2))
                return false;
            else if (!from.CanSee(target) || !from.InLOS(target))
                return false;
            else if (!target.IsAccessibleTo(from))
                return false;
            else if (!from.OnDroppedItemOnto(this, target))
                return false;
            else if (this.Nontransferable && from.Player && from.AccessLevel <= AccessLevel.GameMaster)
            {
                this.HandleInvalidTransfer(from);
                return false;
            }
            else
                return target.OnDragDrop(from, this);
        }

        public virtual bool DropToItem(Mobile from, Item target, Point3D p)
        {
            if (this.Deleted || from.Deleted || target.Deleted || from.Map != target.Map || from.Map == null || target.Map == null)
                return false;

            object root = target.RootParent;

            if (from.AccessLevel < AccessLevel.GameMaster && !from.InRange(target.GetWorldLocation(), 2))
                return false;
            else if (!from.CanSee(target) || !from.InLOS(target))
                return false;
            else if (!target.IsAccessibleTo(from))
                return false;
            else if (root is Mobile && !((Mobile)root).CheckNonlocalDrop(from, this, target))
                return false;
            else if (!from.OnDroppedItemToItem(this, target, p))
                return false;
            else if (target is Container && p.m_X != -1 && p.m_Y != -1)
                return this.OnDroppedInto(from, (Container)target, p);
            else
                return this.OnDroppedOnto(from, target);
        }

        public virtual bool OnDroppedToWorld(Mobile from, Point3D p)
        {
            if (this.Nontransferable && from.Player && from.AccessLevel <= AccessLevel.GameMaster)
            {
                this.HandleInvalidTransfer(from);
                return false;
            }

            return true;
        }

        public virtual int GetLiftSound(Mobile from)
        {
            return 0x57;
        }

        private static int m_OpenSlots;

        public virtual bool DropToWorld(Mobile from, Point3D p)
        {
            if (this.Deleted || from.Deleted || from.Map == null)
                return false;
            else if (!from.InRange(p, 2))
                return false;

            Map map = from.Map;

            if (map == null)
                return false;

            int x = p.m_X, y = p.m_Y;
            int z = int.MinValue;

            int maxZ = from.Z + 16;

            LandTile landTile = map.Tiles.GetLandTile(x, y);
            TileFlag landFlags = TileData.LandTable[landTile.ID & TileData.MaxLandValue].Flags;

            int landZ = 0, landAvg = 0, landTop = 0;
            map.GetAverageZ(x, y, ref landZ, ref landAvg, ref landTop);

            if (!landTile.Ignored && (landFlags & TileFlag.Impassable) == 0)
            {
                if (landAvg <= maxZ)
                    z = landAvg;
            }

            StaticTile[] tiles = map.Tiles.GetStaticTiles(x, y, true);

            for (int i = 0; i < tiles.Length; ++i)
            {
                StaticTile tile = tiles[i];
                ItemData id = TileData.ItemTable[tile.ID & TileData.MaxItemValue];

                if (!id.Surface)
                    continue;

                int top = tile.Z + id.CalcHeight;

                if (top > maxZ || top < z)
                    continue;

                z = top;
            }

            List<Item> items = new List<Item>();

            IPooledEnumerable eable = map.GetItemsInRange(p, 0);

            foreach (Item item in eable)
            {
                if (item is BaseMulti || item.ItemID > TileData.MaxItemValue)
                    continue;

                items.Add(item);

                ItemData id = item.ItemData;

                if (!id.Surface)
                    continue;

                int top = item.Z + id.CalcHeight;

                if (top > maxZ || top < z)
                    continue;

                z = top;
            }

            eable.Free();

            if (z == int.MinValue)
                return false;

            if (z > maxZ)
                return false;

            m_OpenSlots = (1 << 20) - 1;

            int surfaceZ = z;

            for (int i = 0; i < tiles.Length; ++i)
            {
                StaticTile tile = tiles[i];
                ItemData id = TileData.ItemTable[tile.ID & TileData.MaxItemValue];

                int checkZ = tile.Z;
                int checkTop = checkZ + id.CalcHeight;

                if (checkTop == checkZ && !id.Surface)
                    ++checkTop;

                int zStart = checkZ - z;
                int zEnd = checkTop - z;

                if (zStart >= 20 || zEnd < 0)
                    continue;

                if (zStart < 0)
                    zStart = 0;

                if (zEnd > 19)
                    zEnd = 19;

                int bitCount = zEnd - zStart;

                m_OpenSlots &= ~(((1 << bitCount) - 1) << zStart);
            }

            for (int i = 0; i < items.Count; ++i)
            {
                Item item = items[i];
                ItemData id = item.ItemData;

                int checkZ = item.Z;
                int checkTop = checkZ + id.CalcHeight;

                if (checkTop == checkZ && !id.Surface)
                    ++checkTop;

                int zStart = checkZ - z;
                int zEnd = checkTop - z;

                if (zStart >= 20 || zEnd < 0)
                    continue;

                if (zStart < 0)
                    zStart = 0;

                if (zEnd > 19)
                    zEnd = 19;

                int bitCount = zEnd - zStart;

                m_OpenSlots &= ~(((1 << bitCount) - 1) << zStart);
            }

            int height = this.ItemData.Height;

            if (height == 0)
                ++height;

            if (height > 30)
                height = 30;

            int match = (1 << height) - 1;
            bool okay = false;

            for (int i = 0; i < 20; ++i)
            {
                if ((i + height) > 20)
                    match >>= 1;

                okay = ((m_OpenSlots >> i) & match) == match;

                if (okay)
                {
                    z += i;
                    break;
                }
            }

            if (!okay)
                return false;

            height = this.ItemData.Height;

            if (height == 0)
                ++height;

            if (landAvg > z && (z + height) > landZ)
                return false;
            else if ((landFlags & TileFlag.Impassable) != 0 && landAvg > surfaceZ && (z + height) > landZ)
                return false;

            for (int i = 0; i < tiles.Length; ++i)
            {
                StaticTile tile = tiles[i];
                ItemData id = TileData.ItemTable[tile.ID & TileData.MaxItemValue];

                int checkZ = tile.Z;
                int checkTop = checkZ + id.CalcHeight;

                if (checkTop > z && (z + height) > checkZ)
                    return false;
                else if ((id.Surface || id.Impassable) && checkTop > surfaceZ && (z + height) > checkZ)
                    return false;
            }

            for (int i = 0; i < items.Count; ++i)
            {
                Item item = items[i];
                ItemData id = item.ItemData;

                //int checkZ = item.Z;
                //int checkTop = checkZ + id.CalcHeight;

                if ((item.Z + id.CalcHeight) > z && (z + height) > item.Z)
                    return false;
            }

            p = new Point3D(x, y, z);

            if (!from.InLOS(new Point3D(x, y, z + 1)))
                return false;
            else if (!from.OnDroppedItemToWorld(this, p))
                return false;
            else if (!this.OnDroppedToWorld(from, p))
                return false;

            int soundID = this.GetDropSound();

            this.MoveToWorld(p, from.Map);

            from.SendSound(soundID == -1 ? 0x42 : soundID, this.GetWorldLocation());

            return true;
        }

        public void SendRemovePacket()
        {
            if (!this.Deleted && this.m_Map != null)
            {
                Packet p = null;
                Point3D worldLoc = this.GetWorldLocation();

                IPooledEnumerable eable = this.m_Map.GetClientsInRange(worldLoc, this.GetMaxUpdateRange());

                foreach (NetState state in eable)
                {
                    Mobile m = state.Mobile;

                    if (m.InRange(worldLoc, this.GetUpdateRange(m)))
                    {
                        if (p == null)
                            p = this.RemovePacket;

                        state.Send(p);
                    }
                }

                eable.Free();
            }
        }

        public virtual int GetDropSound()
        {
            return -1;
        }

        public Point3D GetWorldLocation()
        {
            object root = this.RootParent;

            if (root == null)
                return this.m_Location;
            else
                return ((IEntity)root).Location;
            //return root == null ? m_Location : new Point3D( (IPoint3D) root );
        }

        public virtual bool BlocksFit
        {
            get
            {
                return false;
            }
        }

        public Point3D GetSurfaceTop()
        {
            object root = this.RootParent;

            if (root == null)
                return new Point3D(this.m_Location.m_X, this.m_Location.m_Y, this.m_Location.m_Z + (this.ItemData.Surface ? this.ItemData.CalcHeight : 0));
            else
                return ((IEntity)root).Location;
        }

        public Point3D GetWorldTop()
        {
            object root = this.RootParent;

            if (root == null)
                return new Point3D(this.m_Location.m_X, this.m_Location.m_Y, this.m_Location.m_Z + this.ItemData.CalcHeight);
            else
                return ((IEntity)root).Location;
        }

        public void SendLocalizedMessageTo(Mobile to, int number)
        {
            if (this.Deleted || !to.CanSee(this))
                return;

            to.Send(new MessageLocalized(this.Serial, this.ItemID, MessageType.Regular, 0x3B2, 3, number, "", ""));
        }

        public void SendLocalizedMessageTo(Mobile to, int number, string args)
        {
            if (this.Deleted || !to.CanSee(this))
                return;

            to.Send(new MessageLocalized(this.Serial, this.ItemID, MessageType.Regular, 0x3B2, 3, number, "", args));
        }

        public void SendLocalizedMessageTo(Mobile to, int number, AffixType affixType, string affix, string args)
        {
            if (this.Deleted || !to.CanSee(this))
                return;

            to.Send(new MessageLocalizedAffix(this.Serial, this.ItemID, MessageType.Regular, 0x3B2, 3, number, "", affixType, affix, args));
        }

        #region OnDoubleClick[...]

        public virtual void OnDoubleClick(Mobile from)
        {
        }

        public virtual void OnDoubleClickOutOfRange(Mobile from)
        {
        }

        public virtual void OnDoubleClickCantSee(Mobile from)
        {
        }

        public virtual void OnDoubleClickDead(Mobile from)
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019048); // I am dead and cannot do that.
        }

        public virtual void OnDoubleClickNotAccessible(Mobile from)
        {
            from.SendLocalizedMessage(500447); // That is not accessible.
        }

        public virtual void OnDoubleClickSecureTrade(Mobile from)
        {
            from.SendLocalizedMessage(500447); // That is not accessible.
        }

        #endregion

        public virtual void OnSnoop(Mobile from)
        {
        }

        public bool InSecureTrade
        {
            get
            {
                return (this.GetSecureTradeCont() != null);
            }
        }

        public SecureTradeContainer GetSecureTradeCont()
        {
            object p = this;

            while (p is Item)
            {
                if (p is SecureTradeContainer)
                    return (SecureTradeContainer)p;

                p = ((Item)p).m_Parent;
            }

            return null;
        }

        public virtual void OnItemAdded(Item item)
        {
            if (this.m_Parent is Item)
                ((Item)this.m_Parent).OnSubItemAdded(item);
            else if (this.m_Parent is Mobile)
                ((Mobile)this.m_Parent).OnSubItemAdded(item);
        }

        public virtual void OnItemRemoved(Item item)
        {
            if (this.m_Parent is Item)
                ((Item)this.m_Parent).OnSubItemRemoved(item);
            else if (this.m_Parent is Mobile)
                ((Mobile)this.m_Parent).OnSubItemRemoved(item);
        }

        public virtual void OnSubItemAdded(Item item)
        {
            if (this.m_Parent is Item)
                ((Item)this.m_Parent).OnSubItemAdded(item);
            else if (this.m_Parent is Mobile)
                ((Mobile)this.m_Parent).OnSubItemAdded(item);
        }

        public virtual void OnSubItemRemoved(Item item)
        {
            if (this.m_Parent is Item)
                ((Item)this.m_Parent).OnSubItemRemoved(item);
            else if (this.m_Parent is Mobile)
                ((Mobile)this.m_Parent).OnSubItemRemoved(item);
        }

        public virtual void OnItemBounceCleared(Item item)
        {
            if (this.m_Parent is Item)
                ((Item)this.m_Parent).OnSubItemBounceCleared(item);
            else if (this.m_Parent is Mobile)
                ((Mobile)this.m_Parent).OnSubItemBounceCleared(item);
        }

        public virtual void OnSubItemBounceCleared(Item item)
        {
            if (this.m_Parent is Item)
                ((Item)this.m_Parent).OnSubItemBounceCleared(item);
            else if (this.m_Parent is Mobile)
                ((Mobile)this.m_Parent).OnSubItemBounceCleared(item);
        }

        public virtual bool CheckTarget(Mobile from, Server.Targeting.Target targ, object targeted)
        {
            if (this.m_Parent is Item)
                return ((Item)this.m_Parent).CheckTarget(from, targ, targeted);
            else if (this.m_Parent is Mobile)
                return ((Mobile)this.m_Parent).CheckTarget(from, targ, targeted);

            return true;
        }

        public virtual bool IsAccessibleTo(Mobile check)
        {
            if (this.m_Parent is Item)
                return ((Item)this.m_Parent).IsAccessibleTo(check);

            Region reg = Region.Find(this.GetWorldLocation(), this.m_Map);

            return reg.CheckAccessibility(this, check);
            /*SecureTradeContainer cont = GetSecureTradeCont();
            if ( cont != null && !cont.IsChildOf( check ) )
            return false;
            return true;*/
        }

        public bool IsChildOf(object o)
        {
            return this.IsChildOf(o, false);
        }

        public bool IsChildOf(object o, bool allowNull)
        {
            object p = this.m_Parent;

            if ((p == null || o == null) && !allowNull)
                return false;

            if (p == o)
                return true;

            while (p is Item)
            {
                Item item = (Item)p;

                if (item.m_Parent == null)
                {
                    break;
                }
                else
                {
                    p = item.m_Parent;

                    if (p == o)
                        return true;
                }
            }

            return false;
        }

        public ItemData ItemData
        {
            get
            {
                return TileData.ItemTable[this.m_ItemID & TileData.MaxItemValue];
            }
        }

        public virtual void OnItemUsed(Mobile from, Item item)
        {
            if (this.m_Parent is Item)
                ((Item)this.m_Parent).OnItemUsed(from, item);
            else if (this.m_Parent is Mobile)
                ((Mobile)this.m_Parent).OnItemUsed(from, item);
        }

        public bool CheckItemUse(Mobile from)
        {
            return this.CheckItemUse(from, this);
        }

        public virtual bool CheckItemUse(Mobile from, Item item)
        {
            if (this.m_Parent is Item)
                return ((Item)this.m_Parent).CheckItemUse(from, item);
            else if (this.m_Parent is Mobile)
                return ((Mobile)this.m_Parent).CheckItemUse(from, item);
            else
                return true;
        }

        public virtual void OnItemLifted(Mobile from, Item item)
        {
            if (this.m_Parent is Item)
                ((Item)this.m_Parent).OnItemLifted(from, item);
            else if (this.m_Parent is Mobile)
                ((Mobile)this.m_Parent).OnItemLifted(from, item);
        }

        public bool CheckLift(Mobile from)
        {
            LRReason reject = LRReason.Inspecific;

            return this.CheckLift(from, this, ref reject);
        }

        public virtual bool CheckLift(Mobile from, Item item, ref LRReason reject)
        {
            if (this.m_Parent is Item)
                return ((Item)this.m_Parent).CheckLift(from, item, ref reject);
            else if (this.m_Parent is Mobile)
                return ((Mobile)this.m_Parent).CheckLift(from, item, ref reject);
            else
                return true;
        }

        public virtual bool CanTarget
        {
            get
            {
                return true;
            }
        }
        public virtual bool DisplayLootType
        {
            get
            {
                return true;
            }
        }

        public virtual void OnSingleClickContained(Mobile from, Item item)
        {
            if (this.m_Parent is Item)
                ((Item)this.m_Parent).OnSingleClickContained(from, item);
        }

        public virtual void OnAosSingleClick(Mobile from)
        {
            ObjectPropertyList opl = this.PropertyList;

            if (opl.Header > 0)
                from.Send(new MessageLocalized(this.m_Serial, this.m_ItemID, MessageType.Label, 0x3B2, 3, opl.Header, this.Name, opl.HeaderArgs));
        }

        public virtual void OnSingleClick(Mobile from)
        {
            if (this.Deleted || !from.CanSee(this))
                return;

            if (this.DisplayLootType)
                this.LabelLootTypeTo(from);

            NetState ns = from.NetState;

            if (ns != null)
            {
                if (this.Name == null)
                {
                    if (this.m_Amount <= 1)
                        ns.Send(new MessageLocalized(this.m_Serial, this.m_ItemID, MessageType.Label, 0x3B2, 3, this.LabelNumber, "", ""));
                    else
                        ns.Send(new MessageLocalizedAffix(this.m_Serial, this.m_ItemID, MessageType.Label, 0x3B2, 3, this.LabelNumber, "", AffixType.Append, String.Format(" : {0}", this.m_Amount), ""));
                }
                else
                {
                    ns.Send(new UnicodeMessage(this.m_Serial, this.m_ItemID, MessageType.Label, 0x3B2, 3, "ENU", "", this.Name + (this.m_Amount > 1 ? " : " + this.m_Amount : "")));
                }
            }
        }

        private static bool m_ScissorCopyLootType;

        public static bool ScissorCopyLootType
        {
            get
            {
                return m_ScissorCopyLootType;
            }
            set
            {
                m_ScissorCopyLootType = value;
            }
        }

        public virtual void ScissorHelper(Mobile from, Item newItem, int amountPerOldItem)
        {
            this.ScissorHelper(from, newItem, amountPerOldItem, true);
        }

        public virtual void ScissorHelper(Mobile from, Item newItem, int amountPerOldItem, bool carryHue)
        {
            int amount = this.Amount;

            if (amount > (60000 / amountPerOldItem)) // let's not go over 60000
                amount = (60000 / amountPerOldItem);

            this.Amount -= amount;

            int ourHue = this.Hue;
            Map thisMap = this.Map;
            object thisParent = this.m_Parent;
            Point3D worldLoc = this.GetWorldLocation();
            LootType type = this.LootType;

            if (this.Amount == 0)
                this.Delete();

            newItem.Amount = amount * amountPerOldItem;

            if (carryHue)
                newItem.Hue = ourHue;

            if (m_ScissorCopyLootType)
                newItem.LootType = type;

            if (!(thisParent is Container) || !((Container)thisParent).TryDropItem(from, newItem, false))
                newItem.MoveToWorld(worldLoc, thisMap);
        }

        public virtual void Consume()
        {
            this.Consume(1);
        }

        public virtual void Consume(int amount)
        {
            this.Amount -= amount;

            if (this.Amount <= 0)
                this.Delete();
        }

        public virtual void ReplaceWith(Item newItem)
        {
            if (this.m_Parent is Container)
            {
                ((Container)this.m_Parent).AddItem(newItem);
                newItem.Location = this.m_Location;
            }
            else
            {
                newItem.MoveToWorld(this.GetWorldLocation(), this.m_Map);
            }

            this.Delete();
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool QuestItem
        {
            get
            {
                return this.GetFlag(ImplFlag.QuestItem);
            }
            set 
            { 
                this.SetFlag(ImplFlag.QuestItem, value); 

                this.InvalidateProperties();

                this.ReleaseWorldPackets();

                this.Delta(ItemDelta.Update);
            }
        }

        public bool Insured
        {
            get
            {
                return this.GetFlag(ImplFlag.Insured);
            }
            set
            {
                this.SetFlag(ImplFlag.Insured, value);
                this.InvalidateProperties();
            }
        }

        public bool PayedInsurance
        {
            get
            {
                return this.GetFlag(ImplFlag.PayedInsurance);
            }
            set
            {
                this.SetFlag(ImplFlag.PayedInsurance, value);
            }
        }

        public Mobile BlessedFor
        {
            get
            {
                CompactInfo info = this.LookupCompactInfo();

                if (info != null)
                    return info.m_BlessedFor;

                return null;
            }
            set
            {
                CompactInfo info = this.AcquireCompactInfo();

                info.m_BlessedFor = value;

                if (info.m_BlessedFor == null)
                    this.VerifyCompactInfo();

                this.InvalidateProperties();
            }
        }

        public virtual bool CheckBlessed(object obj)
        {
            return this.CheckBlessed(obj as Mobile);
        }

        public virtual bool CheckBlessed(Mobile m)
        {
            if (this.m_LootType == LootType.Blessed || (Mobile.InsuranceEnabled && this.Insured))
                return true;

            return (m != null && m == this.BlessedFor);
        }

        public virtual bool CheckNewbied()
        {
            return (this.m_LootType == LootType.Newbied);
        }

        public virtual bool IsStandardLoot()
        {
            if (Mobile.InsuranceEnabled && this.Insured)
                return false;

            if (this.BlessedFor != null)
                return false;

            return (this.m_LootType == LootType.Regular);
        }

        public override string ToString()
        {
            return String.Format("0x{0:X} \"{1}\"", this.m_Serial.Value, this.GetType().Name);
        }

        internal int m_TypeRef;

        public Item()
        {
            this.m_Serial = Serial.NewItem;

            //m_Items = new ArrayList( 1 );
            this.Visible = true;
            this.Movable = true;
            this.Amount = 1;
            this.m_Map = Map.Internal;

            this.SetLastMoved();

            World.AddItem(this);

            Type ourType = this.GetType();
            this.m_TypeRef = World.m_ItemTypes.IndexOf(ourType);

            if (this.m_TypeRef == -1)
            {
                World.m_ItemTypes.Add(ourType);
                this.m_TypeRef = World.m_ItemTypes.Count - 1;
            }
        }

        [Constructable]
        public Item(int itemID)
            : this()
        {
            this.m_ItemID = itemID;
        }

        public Item(Serial serial)
        {
            this.m_Serial = serial;

            Type ourType = this.GetType();
            this.m_TypeRef = World.m_ItemTypes.IndexOf(ourType);

            if (this.m_TypeRef == -1)
            {
                World.m_ItemTypes.Add(ourType);
                this.m_TypeRef = World.m_ItemTypes.Count - 1;
            }
        }

        public virtual void OnSectorActivate()
        {
        }

        public virtual void OnSectorDeactivate()
        {
        }
    }
}