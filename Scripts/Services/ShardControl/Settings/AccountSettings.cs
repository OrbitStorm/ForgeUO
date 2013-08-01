using System;
using Server;
using Server.Misc;

namespace CustomsFramework.Systems.ShardControl
{
    [PropertyObject]
    public sealed class AccountSettings : BaseSettings
    {
        #region Variables
        private int _AccountsPerIP;
        private int _HousesPerAccount;
        private int _MaxHousesPerAccount;
        private bool _AutoAccountCreation;
        private bool _RestrictDeletion;
        private TimeSpan _DeleteDelay;
        private PasswordProtection m_PasswordProtection;

        [CommandProperty(AccessLevel.Administrator)]
        public int AccountsPerIP
        {
            get
            {
                return this._AccountsPerIP;
            }
            set
            {
                this._AccountsPerIP = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public int HousesPerAccount
        {
            get
            {
                return this._HousesPerAccount;
            }
            set
            {
                this._HousesPerAccount = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public int MaxHousesPerAccount
        {
            get
            {
                return this._MaxHousesPerAccount;
            }
            set
            {
                this._MaxHousesPerAccount = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool AutoAccountCreation
        {
            get
            {
                return this._AutoAccountCreation;
            }
            set
            {
                this._AutoAccountCreation = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool RestrictDeletion
        {
            get
            {
                return this._RestrictDeletion;
            }
            set
            {
                this._RestrictDeletion = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public TimeSpan DeleteDelay
        {
            get
            {
                return this._DeleteDelay;
            }
            set
            {
                this._DeleteDelay = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public PasswordProtection PasswordProtection
        {
            get
            {
                return this.m_PasswordProtection;
            }
            set
            {
                this.m_PasswordProtection = value;
            }
        }
        #endregion

        public AccountSettings(TimeSpan deleteDelay, int accountsPerIP = 1,
            int housesPerAccount = 2, int maxHousesPerAccount = 4,
            bool autoAccountCreation = true, bool restrictDeletion = true,
            PasswordProtection passwordProtection = PasswordProtection.NewCrypt)
        {
            this._AccountsPerIP = accountsPerIP;
            this._HousesPerAccount = housesPerAccount;
            this._MaxHousesPerAccount = maxHousesPerAccount;
            this._AutoAccountCreation = autoAccountCreation;
            this._RestrictDeletion = restrictDeletion;
            this._DeleteDelay = deleteDelay;
            this.m_PasswordProtection = passwordProtection;
        }

        public override void Serialize(GenericWriter writer)
        {
            Utilities.WriteVersion(writer, 0);

            // Version 0
            writer.Write(this._AccountsPerIP);
            writer.Write(this._HousesPerAccount);
            writer.Write(this._MaxHousesPerAccount);
            writer.Write(this._AutoAccountCreation);
            writer.Write(this._RestrictDeletion);
            writer.Write(this._DeleteDelay);
            writer.Write((byte)this.m_PasswordProtection);
        }

        public AccountSettings(GenericReader reader)
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
                        this._AccountsPerIP = reader.ReadInt();
                        this._HousesPerAccount = reader.ReadInt();
                        this._MaxHousesPerAccount = reader.ReadInt();
                        this._AutoAccountCreation = reader.ReadBool();
                        this._RestrictDeletion = reader.ReadBool();
                        this._DeleteDelay = reader.ReadTimeSpan();
                        this.m_PasswordProtection = (PasswordProtection)reader.ReadByte();
                        break;
                    }
            }
        }

        public override string ToString()
        {
            return @"Account Settings";
        }
    }
}