using System;
using Server;
using Server.Gumps;

namespace CustomsFramework
{
    public class BaseModule : SaveData, ICustomsEntity, ISerializable
    {
        private Mobile _LinkedMobile;
        private Item _LinkedItem;
        private DateTime _CreatedTime;
        private DateTime _LastEditedTime;

        public BaseModule()
        {
        }

        public BaseModule(Mobile from)
        {
            LinkMobile(from);
        }

        public BaseModule(Item item)
        {
            LinkItem(item);
        }

        public BaseModule(CustomSerial serial)
            : base(serial)
        {
        }

        public override string Name
        {
            get { return @"Base Module"; }
        }

        public virtual string Description
        {
            get { return "Base Module, inherit from this class and override all interface items."; }
        }

        public virtual string Version
        {
            get { return "1.0"; }
        }

        public virtual AccessLevel EditLevel
        {
            get { return AccessLevel.Developer; }
        }

        public virtual Gump SettingsGump
        {
            get { return null; }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public Mobile LinkedMobile
        {
            get { return this._LinkedMobile; }
            set { LinkMobile(value); }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public Item LinkedItem
        {
            get { return this._LinkedItem; }
            set { LinkItem(value); }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public DateTime CreatedTime
        {
            get
            {
                return this._CreatedTime;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public DateTime LastEditedTime
        {
            get { return this._LastEditedTime; }
        }

        public override string ToString()
        {
            return this.Name;
        }

        public override void Prep()
        {
        }

        public override void Delete()
        {
            if (this._LinkedMobile != null)
            {
                this._LinkedMobile.Modules.Remove(this);
                this._LinkedMobile = null;
            }

            if (this._LinkedItem != null)
            {
                this._LinkedItem.Modules.Remove(this);
                this._LinkedItem = null;
            }
        }

        public void Update()
        {
            this._LastEditedTime = DateTime.Now;
        }

        public bool LinkMobile(Mobile from)
        {
            if (_LinkedMobile != null || _LinkedMobile == from)
                return false;
            else
            {
                if (!from.Modules.Contains(this))
                    from.Modules.Add(this);

                _LinkedMobile = from;
                Update();
                return true;
            }
        }

        public bool LinkItem(Item item)
        {
            if (_LinkedItem != null || _LinkedItem == item)
                return false;
            else
            {
                if (!item.Modules.Contains(this))
                    item.Modules.Add(this);

                _LinkedItem = item;
                Update();
                return true;
            }
        }

        public bool UnlinkMobile()
        {
            if (_LinkedMobile == null)
                return false;
            else
            {
                if (_LinkedMobile.Modules.Contains(this))
                    _LinkedMobile.Modules.Remove(this);

                _LinkedMobile = null;
                Update();
                return true;
            }
        }

        public bool UnlinkItem()
        {
            if (_LinkedItem == null)
                return false;
            else
            {
                if (_LinkedItem.Modules.Contains(this))
                    _LinkedItem.Modules.Remove(this);

                _LinkedItem = null;
                Update();
                return true;
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            Utilities.WriteVersion(writer, 0);

            // Version 0
            writer.Write(this._LinkedMobile);
            writer.Write(this._LinkedItem);
            writer.Write(this._CreatedTime);
            writer.Write(this._LastEditedTime);
        }

        public override void Deserialize(GenericReader reader)
        {
            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        this.LinkedMobile = reader.ReadMobile();
                        this.LinkedItem = reader.ReadItem();
                        this._CreatedTime = reader.ReadDateTime();
                        this._LastEditedTime = reader.ReadDateTime();
                        break;
                    }
            }
        }
    }
}