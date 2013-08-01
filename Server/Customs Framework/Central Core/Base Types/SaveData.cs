using System;
using Server;

namespace CustomsFramework
{
    public partial class SaveData : ICustomsEntity, IComparable<SaveData>, ISerializable
    {
        #region CompareTo
        public int CompareTo(ICustomsEntity other)
        {
            if (other == null)
                return -1;

            return this._Serial.CompareTo(other.Serial);
        }

        public int CompareTo(SaveData other)
        {
            return this.CompareTo((ICustomsEntity)other);
        }

        public int CompareTo(object other)
        {
            if (other == null || other is ICustomsEntity)
                return this.CompareTo((ICustomsEntity)other);

            throw new ArgumentException();
        }

        #endregion

        internal int _TypeID;

        int ISerializable.TypeReference
        {
            get
            {
                return this._TypeID;
            }
        }

        int ISerializable.SerialIdentity
        {
            get
            {
                return this._Serial;
            }
        }

        private bool _Deleted;
        private CustomSerial _Serial;

        [CommandProperty(AccessLevel.Developer)]
        public bool Deleted
        {
            get
            {
                return this._Deleted;
            }
            set
            {
                this._Deleted = value;
            }
        }

        [CommandProperty(AccessLevel.Developer)]
        public CustomSerial Serial
        {
            get
            {
                return this._Serial;
            }
            set
            {
                this._Serial = value;
            }
        }

        public virtual string Name
        {
            get
            {
                return @"Save Data";
            }
        }

        public SaveData(CustomSerial serial)
        {
            this._Serial = serial;

            Type dataType = this.GetType();
            this._TypeID = World._DataTypes.IndexOf(dataType);

            if (this._TypeID == -1)
            {
                World._DataTypes.Add(dataType);
                this._TypeID = World._DataTypes.Count - 1;
            }
        }

        public SaveData()
        {
            this._Serial = CustomSerial.NewCustom;

            World.AddData(this);

            Type dataType = this.GetType();
            this._TypeID = World._DataTypes.IndexOf(dataType);

            if (this._TypeID == -1)
            {
                World._DataTypes.Add(dataType);
                this._TypeID = World._DataTypes.Count - 1;
            }
        }

        public virtual void Prep()
        {
        }

        public virtual void Delete()
        {
        }

        public virtual void Serialize(GenericWriter writer)
        {
            Utilities.WriteVersion(writer, 0);

            // Version 0
            writer.Write(this._Deleted);
        }

        public virtual void Deserialize(GenericReader reader)
        {
            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        this._Deleted = reader.ReadBool();
                        break;
                    }
            }
        }
    }
}