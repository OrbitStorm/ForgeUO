using System;
using System.Collections.Generic;
using Server.ContextMenus;
using Server.Engines.Craft;
using Server.Engines.Plants;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Items
{
    [FlipableAttribute(0x0DFC, 0x0DFD)]
    public class Clippers : Item, IUsesRemaining, ICraftable
    {
        private int m_UsesRemaining;
        private Mobile m_Crafter;
        private ToolQuality m_Quality;

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Crafter
        {
            get
            {
                return this.m_Crafter;
            }
            set
            {
                this.m_Crafter = value;
                this.InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public ToolQuality Quality
        {
            get
            {
                return this.m_Quality;
            }
            set
            {
                this.UnscaleUses();
                this.m_Quality = value;
                this.InvalidateProperties();
                this.ScaleUses();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int UsesRemaining
        {
            get
            {
                return this.m_UsesRemaining;
            }
            set
            {
                this.m_UsesRemaining = value;
                this.InvalidateProperties();
            }
        }

        public void ScaleUses()
        {
            this.m_UsesRemaining = (this.m_UsesRemaining * this.GetUsesScalar()) / 100;
            this.InvalidateProperties();
        }

        public void UnscaleUses()
        {
            this.m_UsesRemaining = (this.m_UsesRemaining * 100) / this.GetUsesScalar();
        }

        public int GetUsesScalar()
        {
            if (this.m_Quality == ToolQuality.Exceptional)
                return 200;

            return 100;
        }

        public bool ShowUsesRemaining
        {
            get
            {
                return true;
            }
            set
            {
            }
        }

        public override int LabelNumber
        {
            get
            {
                return 1112117;
            }
        }// clippers

        [Constructable]
        public Clippers()
            : base(0x0DFC)
        {
            this.Weight = 1.0;
            this.Hue = 1168;
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            //Makers mark not displayed on OSI
            if (this.m_Crafter != null)
                list.Add(1050043, this.m_Crafter.Name); // crafted by ~1_NAME~

            if (this.m_Quality == ToolQuality.Exceptional)
                list.Add(1060636); // exceptional

            list.Add(1060584, this.m_UsesRemaining.ToString()); // uses remaining: ~1_val~
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            AddContextMenuEntries(from, this, list);
        }

        public static void AddContextMenuEntries(Mobile from, Item item, List<ContextMenuEntry> list)
        {
            if (!item.IsChildOf(from.Backpack) && item.Parent != from)
                return;

            PlayerMobile pm = from as PlayerMobile;

            if (pm == null)
                return;

            int typeentry = 0;
            if (pm.ToggleCutClippings)
                typeentry = 1112282;
            if (pm.ToggleCutReeds)
                typeentry = 1112283;

            ContextMenuEntry clippingEntry = new ContextMenuEntry(typeentry);
            clippingEntry.Color = 0x421F;
            //list.Add( clippingEntry );

            list.Add(new ToggleClippings(pm, true, false, 1112282)); //set to clip plants
            list.Add(new ToggleClippings(pm, false, true, 1112283)); //Set to cut reeds
        }

        private class ToggleClippings : ContextMenuEntry
        {
            private readonly PlayerMobile m_Mobile;
            private readonly bool m_Valueclips;
            private readonly bool m_Valuereeds;
            public ToggleClippings(PlayerMobile mobile, bool valueclips, bool valuereeds, int number)
                : base(number)
            {
                this.m_Mobile = mobile;
                this.m_Valueclips = valueclips;
                this.m_Valuereeds = valuereeds;
            }

            public override void OnClick()
            {
                bool oldValueclips = this.m_Mobile.ToggleCutClippings;
                bool oldValuereeds = this.m_Mobile.ToggleCutReeds;
                if (this.m_Valueclips)
                {
                    if (oldValueclips)
                    {
                        this.m_Mobile.ToggleCutClippings = true;
                        this.m_Mobile.ToggleCutReeds = false;
                        this.m_Mobile.SendLocalizedMessage(1112284); // You are already set to make plant clippings 
                    }
                    else
                    {
                        this.m_Mobile.ToggleCutClippings = true;
                        this.m_Mobile.ToggleCutReeds = false;
                        this.m_Mobile.SendLocalizedMessage(1112285); // You are now set to make plant clippings
                    }
                }
                else if (this.m_Valuereeds)
                {
                    if (oldValuereeds)
                    {
                        this.m_Mobile.ToggleCutReeds = true;
                        this.m_Mobile.ToggleCutClippings = false;
                        this.m_Mobile.SendLocalizedMessage(1112287);// You are already set to cut reeds. 
                    }
                    else
                    {
                        this.m_Mobile.ToggleCutReeds = true;
                        this.m_Mobile.ToggleCutClippings = false;
                        this.m_Mobile.SendLocalizedMessage(1112286); // You are now set to cut reeds.
                    }
                }
            }
        }

        public Clippers(Serial serial)
            : base(serial)
        {
        }

        public PlantHue PlantHue
        {
            get
            {
                return this.m_PlantHue;
            }
        }
        private readonly PlantHue m_PlantHue;
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            writer.Write((int)this.m_UsesRemaining);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    this.m_UsesRemaining = reader.ReadInt();
                    break;
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            from.SendLocalizedMessage(1112118); // What plant do you wish to use these clippers on?

            from.Target = new InternalTarget(this);
        }

        private class InternalTarget : Target
        {
            private readonly Clippers m_Item;

            public InternalTarget(Clippers item)
                : base(2, false, TargetFlags.None)
            {
                this.m_Item = item;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                PlayerMobile pm = from as PlayerMobile;

                if (this.m_Item.Deleted)
                    return;

                PlantItem plant = targeted as PlantItem;

                if (null == plant || PlantStatus.DecorativePlant != plant.PlantStatus)
                {
                    from.SendLocalizedMessage(1112119); // You may only use these clippers on decorative plants.
                    return;
                }
                if (pm.ToggleCutClippings == true)
                {
                    /*PlantClippings clippings = new PlantClippings();
                    clippings.PlantHue = plant.PlantHue;
                    clippings.MoveToWorld(plant.Location, plant.Map);
                    plant.Delete();*/
                    from.PlaySound(0x248);
                    PlantClippings cl = new PlantClippings();
                    cl.Hue = ((PlantItem)targeted).Hue;
                    cl.PlantHue = plant.PlantHue;
                    from.AddToBackpack(cl);
                    plant.Delete();
                }
                else if (pm.ToggleCutReeds == true)
                {
                    /*DryReeds reeds = new DryReeds();
                    reeds.PlantHue = plant.PlantHue;
                    reeds.MoveToWorld(plant.Location, plant.Map);
                    plant.Delete();
                    from.PlaySound(0x248);*/
                    from.PlaySound(0x248);
                    DryReeds dr = new DryReeds();
                    dr.Hue = ((PlantItem)targeted).Hue;
                    dr.PlantHue = plant.PlantHue;
                    from.AddToBackpack(dr);
                    plant.Delete();
                }
                //TODO: Add in clipping hedges (short and tall) and juniperbushes for topiaries
            }
        }

        #region ICraftable Members

        public int OnCraft(int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool, CraftItem craftItem, int resHue)
        {
            this.Quality = (ToolQuality)quality;

            if (makersMark)
                this.Crafter = from;

            return quality;
        }
        #endregion
    }
}