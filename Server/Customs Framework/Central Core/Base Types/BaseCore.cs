using System;
using Server;
using Server.Gumps;

namespace CustomsFramework
{
    public partial class BaseCore : SaveData, ICustomsEntity, ISerializable
    {
        public static event BaseCoreEventHandler OnEnabledChanged;

        private bool _Enabled;
        public BaseCore()
        {
        }

        public BaseCore(CustomSerial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.Developer)]
        public bool Enabled
        {
            get
            {
                return this._Enabled;
            }
            set
            {
                if (value != this._Enabled)
                {
                    this._Enabled = value;

                    if (OnEnabledChanged != null)
                        OnEnabledChanged(new BaseCoreEventArgs(this));
                }
            }
        }
        public override string Name
        {
            get
            {
                return @"Base Core";
            }
        }
        public virtual string Description
        {
            get
            {
                return @"Base Core, inherit from this class and override the interface items.";
            }
        }
        public virtual string Version
        {
            get
            {
                return "1.0";
            }
        }
        public virtual AccessLevel EditLevel
        {
            get
            {
                return AccessLevel.Developer;
            }
        }
        // TODO: Implement Custom Systems Control
        public virtual Gump SettingsGump
        {
            get
            {
                return null;
            }
        }
        public virtual bool ShardControlEnabled
        {
            get { return false; }
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
        }

        public override void Serialize(GenericWriter writer)
        {
            Utilities.WriteVersion(writer, 0);

            // Version 0
            writer.Write(this._Enabled);
        }

        public override void Deserialize(GenericReader reader)
        {
            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        this._Enabled = reader.ReadBool();
                        break;
                    }
            }
        }
    }
}