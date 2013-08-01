using System;
using System.Collections.Generic;
using Server;

namespace CustomsFramework.Systems.ShardControl
{
    [PropertyObject]
    public sealed class SaveSettings : BaseSettings
    {
        public enum CompressionLevel
        {
            None = 0,
            Fast = 1,
            Low = 2,
            Normal = 3,
            High = 4,
            Ultra = 5,
        }

        #region Variables
        private bool m_SavesEnabled;
        private AccessLevel m_SaveAccessLevel;
        private SaveStrategy m_SaveStrategy;
        private bool m_AllowBackgroundWrite;
        private TimeSpan m_SaveDelay;
        private List<TimeSpan> m_WarningDelays;
        private int m_NoIOHour;

        private bool m_EnableEmergencyBackups;
        private int m_EmergencyBackupHour;
        private CompressionLevel m_CompressionLevel;

        [CommandProperty(AccessLevel.Administrator)]
        public bool SavesEnabled
        {
            get
            {
                return this.m_SavesEnabled;
            }
            set
            {
                this.m_SavesEnabled = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public AccessLevel SaveAccessLevel
        {
            get
            {
                return this.m_SaveAccessLevel;
            }
            set
            {
                this.m_SaveAccessLevel = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator, true)]
        public SaveStrategy SaveStrategy
        {
            get
            {
                return this.m_SaveStrategy;
            }
            set
            {
                if (!Core.MultiProcessor && !(value is StandardSaveStrategy))
                    this.m_SaveStrategy = new StandardSaveStrategy();
                else
                {
                    if (Core.ProcessorCount == 2 && (value is DualSaveStrategy || value is DynamicSaveStrategy))
                        this.m_SaveStrategy = value;
                    else if (Core.ProcessorCount > 2)
                        this.m_SaveStrategy = value;
                }
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool AllowBackgroundWrite
        {
            get
            {
                return this.m_AllowBackgroundWrite;
            }
            set
            {
                this.m_AllowBackgroundWrite = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public TimeSpan SaveDelay
        {
            get
            {
                return this.m_SaveDelay;
            }
            set
            {
                this.m_SaveDelay = value;
            }
        }

        // Create a method to verify proper delay order
        [CommandProperty(AccessLevel.Administrator, true)]
        public List<TimeSpan> WarningDelays
        {
            get
            {
                return this.m_WarningDelays;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public int NoIOHour
        {
            get
            {
                return this.m_NoIOHour;
            }
            set
            {
                this.m_NoIOHour = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool EnableEmergencyBackups
        {
            get
            {
                return this.m_EnableEmergencyBackups;
            }
            set
            {
                this.m_EnableEmergencyBackups = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public int EmergencyBackupHour
        {
            get
            {
                return this.m_EmergencyBackupHour;
            }
            set
            {
                this.m_EmergencyBackupHour = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public CompressionLevel Compression
        {
            get
            {
                return this.m_CompressionLevel;
            }
            set
            {
                this.m_CompressionLevel = value;
            }
        }

        #endregion

        public SaveSettings(SaveStrategy saveStrategy, List<TimeSpan> warningDelays, TimeSpan saveDelay,
            bool savesEnabled = true, AccessLevel saveAccessLevel = AccessLevel.Administrator,
            bool allowBackgroundWrite = false, int noIOHour = -1, bool enableEmergencyBackups = true,
            int emergencyBackupHour = 3, CompressionLevel compressionLevel = CompressionLevel.Normal)
        {
            this.m_SavesEnabled = savesEnabled;
            this.m_SaveAccessLevel = saveAccessLevel;
            this.m_SaveStrategy = saveStrategy;
            this.m_AllowBackgroundWrite = allowBackgroundWrite;
            this.m_SaveDelay = saveDelay;
            this.m_WarningDelays = warningDelays;
            this.m_NoIOHour = noIOHour;
            this.m_EnableEmergencyBackups = enableEmergencyBackups;
            this.m_EmergencyBackupHour = emergencyBackupHour;
            this.m_CompressionLevel = compressionLevel;
        }

        public override void Serialize(GenericWriter writer)
        {
            Utilities.WriteVersion(writer, 0);

            // Version 0
            writer.Write(this.m_SavesEnabled);
            writer.Write((byte)this.m_SaveAccessLevel);
            writer.Write((byte)Utilities.GetSaveType(this.m_SaveStrategy));
            writer.Write(this.m_AllowBackgroundWrite);
            writer.Write(this.m_SaveDelay);

            writer.Write(this.m_WarningDelays.Count);

            for (int i = 0; i < this.m_WarningDelays.Count; i++)
            {
                writer.Write(this.m_WarningDelays[i]);
            }

            writer.Write(this.m_NoIOHour);

            writer.Write(this.m_EnableEmergencyBackups);
            writer.Write(this.m_EmergencyBackupHour);
            writer.Write((byte)this.m_CompressionLevel);
        }

        public SaveSettings(GenericReader reader)
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
                        this.m_SavesEnabled = reader.ReadBool();
                        this.m_SaveAccessLevel = (AccessLevel)reader.ReadByte();
                        this.m_SaveStrategy = Utilities.GetSaveStrategy((SaveStrategyTypes)reader.ReadByte());
                        this.m_AllowBackgroundWrite = reader.ReadBool();
                        this.m_SaveDelay = reader.ReadTimeSpan();

                        this.m_WarningDelays = new List<TimeSpan>();
                        int count = reader.ReadInt();

                        for (int i = 0; i < count; i++)
                        {
                            this.m_WarningDelays.Add(reader.ReadTimeSpan());
                        }

                        this.m_NoIOHour = reader.ReadInt();

                        this.m_EnableEmergencyBackups = reader.ReadBool();
                        this.m_EmergencyBackupHour = reader.ReadInt();
                        this.m_CompressionLevel = (CompressionLevel)reader.ReadByte();
                        break;
                    }
            }
        }

        public override string ToString()
        {
            return @"Save Settings";
        }
    }
}