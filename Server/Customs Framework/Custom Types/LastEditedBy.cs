using System;
using CustomsFramework;

namespace Server
{
    public class LastEditedBy
    {
        private Mobile _Mobile;
        private DateTime _Time;
        public LastEditedBy(Mobile mobile)
        {
            this._Mobile = mobile;
            this._Time = DateTime.Now;
        }

        public LastEditedBy(GenericReader reader)
        {
            this.Deserialize(reader);
        }

        [CommandProperty(AccessLevel.Decorator)]
        public Mobile Mobile
        {
            get
            {
                return this._Mobile;
            }
            set
            {
                this._Mobile = value;
            }
        }
        [CommandProperty(AccessLevel.Decorator)]
        public DateTime Time
        {
            get
            {
                return this._Time;
            }
            set
            {
                this._Time = value;
            }
        }
        public void Serialize(GenericWriter writer)
        {
            Utilities.WriteVersion(writer, 0);

            // Version 0
            writer.Write(this._Mobile);
            writer.Write(this._Time);
        }

        private void Deserialize(GenericReader reader)
        {
            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        this._Mobile = reader.ReadMobile();
                        this._Time = reader.ReadDateTime();
                        break;
                    }
            }
        }
    }
}