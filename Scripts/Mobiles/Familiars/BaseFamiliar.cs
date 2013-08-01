using System;
using System.Collections.Generic;
using Server.ContextMenus;
using Server.Items;

namespace Server.Mobiles
{
    public abstract class BaseFamiliar : BaseCreature
    {
        private bool m_LastHidden;
        public BaseFamiliar()
            : base(AIType.AI_Melee, FightMode.Closest, 10, 1, -1, -1)
        {
        }

        public BaseFamiliar(Serial serial)
            : base(serial)
        {
        }

        public override bool BardImmune
        {
            get
            {
                return true;
            }
        }
        public override Poison PoisonImmune
        {
            get
            {
                return Poison.Lethal;
            }
        }
        public override bool Commandable
        {
            get
            {
                return false;
            }
        }
        public override bool PlayerRangeSensitive 
        { 
            get 
            { 
                return false; 
            } 
        }

        public virtual void RangeCheck()
        {
            if (!this.Deleted && this.ControlMaster != null && !this.ControlMaster.Deleted)
            {
                int range = (this.RangeHome - 2);

                if (!this.InRange(ControlMaster.Location, RangeHome))
                {
                    Mobile master = this.ControlMaster;

                    Point3D m_Loc = Point3D.Zero;

                    if (this.Map == master.Map)
                    {
                        int x = (this.X > master.X) ? (master.X + range) : (master.X - range);
                        int y = (this.Y > master.Y) ? (master.Y + range) : (master.Y - range);

                        for (int i = 0; i < 10; i++)
                        {
                            m_Loc.X = x + Utility.RandomMinMax(-1, 1);
                            m_Loc.Y = y + Utility.RandomMinMax(-1, 1);

                            m_Loc.Z = this.Map.GetAverageZ(m_Loc.X, m_Loc.Y);

                            if (this.Map.CanSpawnMobile(m_Loc))
                            {
                                break;
                            }

                            m_Loc = master.Location;
                        }

                        if (!this.Deleted)
                        {
                            this.SetLocation(m_Loc, true);
                        }
                    }
                }
            }
        }

        public override void OnThink()
        {
            Mobile master = this.ControlMaster;

            if (this.Deleted)
                return;

            if (master == null || master.Deleted)
            {
                this.DropPackContents();
                this.EndRelease(null);
            }

            this.RangeCheck();

            if (this.m_LastHidden != master.Hidden)
                this.Hidden = this.m_LastHidden = master.Hidden;

            if (this.AIObject != null && this.AIObject.WalkMobileRange(master, 5, true, 1, 1))
            {
                this.Warmode = master.Warmode;
                this.Combatant = master.Combatant;

                this.CurrentSpeed = 0.10;
            }
            else
            {
                this.Warmode = false;
                this.FocusMob = this.Combatant = null;

                this.CurrentSpeed = .01;
            }
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            if (from.Alive && this.Controlled && from == this.ControlMaster && from.InRange(this, 14))
                list.Add(new ReleaseEntry(from, this));
        }

        public virtual void BeginRelease(Mobile from)
        {
            if (!this.Deleted && this.Controlled && from == this.ControlMaster && from.CheckAlive())
                this.EndRelease(from);
        }

        public virtual void EndRelease(Mobile from)
        {
            if (from == null || (!this.Deleted && this.Controlled && from == this.ControlMaster && from.CheckAlive()))
            {
                Effects.SendLocationParticles(EffectItem.Create(this.Location, this.Map, EffectItem.DefaultDuration), 0x3728, 1, 13, 2100, 3, 5042, 0);
                this.PlaySound(0x201);
                this.Delete();
            }
        }

        public virtual void DropPackContents()
        {
            Map map = this.Map;
            Container pack = this.Backpack;

            if (map != null && map != Map.Internal && pack != null)
            {
                List<Item> list = new List<Item>(pack.Items);

                for (int i = 0; i < list.Count; ++i)
                    list[i].MoveToWorld(this.Location, map);
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            ValidationQueue<BaseFamiliar>.Add(this);
        }

        public void Validate()
        {
            this.DropPackContents();
            this.Delete();
        }

        private class ReleaseEntry : ContextMenuEntry
        {
            private readonly Mobile m_From;
            private readonly BaseFamiliar m_Familiar;
            public ReleaseEntry(Mobile from, BaseFamiliar familiar)
                : base(6118, 14)
            {
                this.m_From = from;
                this.m_Familiar = familiar;
            }

            public override void OnClick()
            {
                if (!this.m_Familiar.Deleted && this.m_Familiar.Controlled && this.m_From == this.m_Familiar.ControlMaster && this.m_From.CheckAlive())
                    this.m_Familiar.BeginRelease(this.m_From);
            }
        }
    }
}