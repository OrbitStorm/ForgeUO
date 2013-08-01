using System;
using Server;

namespace CustomsFramework.Systems.ShardControl
{
    [PropertyObject]
    public sealed class ClientSettings : BaseSettings
    {
        #region Variables
        private bool _AutoDetectClient;
        private string _ClientPath;
        private OldClientResponse _OldClientResponse;
        private ClientVersion _RequiredVersion;
        private bool _AllowRegular, _AllowUOTD, _AllowGod;
        private TimeSpan _AgeLeniency;
        private TimeSpan _GameTimeLeniency;
        private TimeSpan _KickDelay;

        [CommandProperty(AccessLevel.Administrator)]
        public bool AutoDetectClient
        {
            get
            {
                return this._AutoDetectClient;
            }
            set
            {
                this._AutoDetectClient = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public string ClientPath
        {
            get
            {
                return this._ClientPath;
            }
            set
            {
                this._ClientPath = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public OldClientResponse OldClientResponse
        {
            get
            {
                return this._OldClientResponse;
            }
            set
            {
                this._OldClientResponse = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public ClientVersion RequiredClientVersion
        {
            get
            {
                return this._RequiredVersion;
            }
            set
            {
                this._RequiredVersion = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool AllowRegular
        {
            get
            {
                return this._AllowRegular;
            }
            set
            {
                this._AllowRegular = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool AllowUOTD
        {
            get
            {
                return this._AllowUOTD;
            }
            set
            {
                this._AllowUOTD = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool AllowGod
        {
            get
            {
                return this._AllowGod;
            }
            set
            {
                this._AllowGod = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public TimeSpan AgeLeniency
        {
            get
            {
                return this._AgeLeniency;
            }
            set
            {
                this._AgeLeniency = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public TimeSpan GameTimeLeniency
        {
            get
            {
                return this._GameTimeLeniency;
            }
            set
            {
                this._GameTimeLeniency = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public TimeSpan KickDelay
        {
            get
            {
                return this._KickDelay;
            }
            set
            {
                this._KickDelay = value;
            }
        }
        #endregion

        public ClientSettings(TimeSpan ageLeniency, TimeSpan gameTimeLeniency, TimeSpan kickDelay,
            bool autoDetectClient = false, string clientPath = null,
            OldClientResponse oldClientResponse = OldClientResponse.LenientKick,
            ClientVersion requiredVersion = null, bool allowRegular = true, bool allowUOTD = true,
            bool allowGod = true)
        {
            this._AutoDetectClient = autoDetectClient;
            this._ClientPath = clientPath;
            this._OldClientResponse = oldClientResponse;
            this._RequiredVersion = requiredVersion;
            this._AllowRegular = allowRegular;
            this._AllowUOTD = allowUOTD;
            this._AllowGod = allowGod;
            this._AgeLeniency = ageLeniency;
            this._GameTimeLeniency = gameTimeLeniency;
            this._KickDelay = kickDelay;
        }

        public override void Serialize(GenericWriter writer)
        {
            Utilities.WriteVersion(writer, 0);

            // Version 0
            writer.Write(this._AutoDetectClient);
            writer.Write(this._ClientPath);
            writer.Write((byte)this._OldClientResponse);

            writer.Write(this._RequiredVersion.Major);
            writer.Write(this._RequiredVersion.Minor);
            writer.Write(this._RequiredVersion.Revision);
            writer.Write(this._RequiredVersion.Patch);

            writer.Write(this._AllowRegular);
            writer.Write(this._AllowUOTD);
            writer.Write(this._AllowGod);
            writer.Write(this._AgeLeniency);
            writer.Write(this._GameTimeLeniency);
            writer.Write(this._KickDelay);
        }

        public ClientSettings(GenericReader reader)
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
                        this._AutoDetectClient = reader.ReadBool();
                        this._ClientPath = reader.ReadString();
                        this._OldClientResponse = (OldClientResponse)reader.ReadByte();

                        this._RequiredVersion = new ClientVersion(reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt());

                        this._AllowRegular = reader.ReadBool();
                        this._AllowUOTD = reader.ReadBool();
                        this._AllowGod = reader.ReadBool();
                        this._AgeLeniency = reader.ReadTimeSpan();
                        this._GameTimeLeniency = reader.ReadTimeSpan();
                        this._KickDelay = reader.ReadTimeSpan();
                        break;
                    }
            }
        }

        public override string ToString()
        {
            return @"Client Settings";
        }
    }
}