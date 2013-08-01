using CustomsFramework;

namespace Server
{
    public class Place
    {
        private Map _Map;
        private Point3D _Location;
        public Place()
        {
            this._Map = Map.Internal;
            this._Location = new Point3D(0, 0, 0);
        }

        public Place(Map map, Point3D location)
        {
            this._Map = map;
            this._Location = location;
        }

        public Place(GenericReader reader)
        {
            this.Deserialize(reader);
        }

        [CommandProperty(AccessLevel.Decorator)]
        public Map Map
        {
            get
            {
                return this._Map;
            }
            set
            {
                this._Map = value;
            }
        }
        [CommandProperty(AccessLevel.Decorator)]
        public Point3D Location
        {
            get
            {
                return this._Location;
            }
            set
            {
                this._Location = value;
            }
        }
        public void Serialize(GenericWriter writer)
        {
            Utilities.WriteVersion(writer, 0);

            // Version 0
            writer.Write(this._Map);
            writer.Write(this._Location);
        }

        private void Deserialize(GenericReader reader)
        {
            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        this._Map = reader.ReadMap();
                        this._Location = reader.ReadPoint3D();
                        break;
                    }
            }
        }
    }
}