using System.Collections.Generic;
using System.Linq;

namespace Server.Gumps
{
    public partial class Gump
    {
        public List<Mobile> Users
        {
            get { return this._Users; }
            set
            {
                if (this._Users == value) return;

                this._Users = value;
                this.Invalidate();
            }
        }

        public List<Mobile> Viewers
        {
            get { return this._Viewers; }
            set
            {
                if (this._Viewers == value) return;

                this._Viewers = value;
                this.Invalidate();
            }
        }

        public bool RemoveUser(Mobile from)
        {
            this.Invalidate();

            bool result = false;

            if (this._Users.Contains(from))
            {
                this._Users.Remove(from);
                result = true;

                if (this._Users.FirstOrDefault() == null && this._Viewers.FirstOrDefault() == null)
                    this.SharedGump = false;
            }

            return result;
        }

        public bool AddViewer(Mobile from)
        {
            this.Invalidate();

            if (this._Viewers == null)
                this._Viewers = new List<Mobile>();

            if (this._Viewers.Contains(from))
                return false;

            this._Viewers.Add(from);

            this._SharedGump = true;

            return true;
        }

        public bool RemoveViewer(Mobile from)
        {
            this.Invalidate();

            bool result = false;

            if (this._Viewers.Remove(from))
            {
                result = true;

                if (this._Viewers.FirstOrDefault() == null && this._Users.FirstOrDefault() == null)
                    this.SharedGump = false;
            }

            return result;
        }
    }
}
