using System;
using Server;

namespace CustomsFramework.Systems.ShardControl
{
    [PropertyObject]
    public sealed class GeneralSettings : BaseSettings
    {
        #region Variables
        private string _ShardName;
        private bool _AutoDetect;
        private string _Address;
        private int _Port;
        private Expansion _Expansion;
        private readonly AccessLevel _MaxPlayerLevel;
        private readonly AccessLevel _LowestStaffLevel;
        private readonly AccessLevel _LowestOwnerLevel;

        [CommandProperty(AccessLevel.Owner)]
        public string ShardName
        {
            get
            {
                return this._ShardName;
            }
            set
            {
                this._ShardName = value;
            }
        }

        [CommandProperty(AccessLevel.Owner)]
        public bool AutoDetect
        {
            get
            {
                return this._AutoDetect;
            }
            set
            {
                this._AutoDetect = value;
            }
        }

        [CommandProperty(AccessLevel.Owner)]
        public string Address
        {
            get
            {
                return this._Address;
            }
            set
            {
                this._Address = value;
            }
        }

        [CommandProperty(AccessLevel.Owner)]
        public int Port
        {
            get
            {
                return this._Port;
            }
            set
            {
                this._Port = value;
            }
        }

        [CommandProperty(AccessLevel.Owner)]
        public Expansion Expansion
        {
            get
            {
                return this._Expansion;
            }
            set
            {
                this._Expansion = value;
            }
        }
        #endregion

        public GeneralSettings(string shardName = "My Shard", bool autoDetect = true,
            string address = null, int port = 2593, Expansion expansion = Expansion.SA)
        {
            this._ShardName = shardName;
            this._AutoDetect = autoDetect;
            this._Address = address;
            this._Port = port;
            this._Expansion = expansion;
        }

        public override void Serialize(GenericWriter writer)
        {
            Utilities.WriteVersion(writer, 0);

            // Version 0
            writer.Write(this._ShardName);
            writer.Write(this._AutoDetect);
            writer.Write(this._Address);
            writer.Write(this._Port);
            writer.Write((byte)this._Expansion);
        }

        public GeneralSettings(GenericReader reader)
        {
            this.Deserialize(reader);
        }

        public sealed override void Deserialize(GenericReader reader)
        {
            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        this._ShardName = reader.ReadString();
                        this._AutoDetect = reader.ReadBool();
                        this._Address = reader.ReadString();
                        this._Port = reader.ReadInt();
                        this._Expansion = (Expansion)reader.ReadByte();
                        break;
                    }
            }
        }

        public override string ToString()
        {
            return @"General Settings";
        }
    }
}