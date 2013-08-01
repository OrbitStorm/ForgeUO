using System;
using Server.Network;

namespace Server.Gumps
{
    public abstract class GumpEntry : IGumpComponent
    {
        private IGumpContainer m_Parent;

        public IGumpContainer Parent
        {
            get { return this.m_Parent; }
            set
            {
                if (this.m_Parent != value)
                {
                    if (this.m_Parent != null)
                        this.m_Parent.Remove(this);

                    this.m_Parent = value;

                    if (this.m_Parent != null)
                        this.m_Parent.Add(this);
                }
            }
        }

        private Int32 _X = 0;
        private Int32 _Y = 0;

        public virtual Int32 X { get { return _X; } set { _X = value; } }
        public virtual Int32 Y { get { return _Y; } set { _Y = value; } }

        public abstract string Compile();

        public abstract void AppendTo(IGumpWriter disp);

        protected internal void AssignID()
        {
            if (this is IInputEntry && ((IInputEntry)this).EntryID < 0 && m_Parent != null && m_Parent.RootParent != null)
                ((IInputEntry)this).EntryID = m_Parent.RootParent.NewID();
        }

        protected void Delta(ref int var, int val)
        {
            if (var == val) return;

            var = val;

            if (this.m_Parent != null)
            {
                this.m_Parent.Invalidate();
            }
        }

        protected void Delta(ref bool var, bool val)
        {
            if (var == val) return;

            var = val;

            if (this.m_Parent != null)
            {
                this.m_Parent.Invalidate();
            }
        }

        protected void Delta(ref string var, string val)
        {
            if (var == val) return;

            var = val;

            if (this.m_Parent != null)
            {
                this.m_Parent.Invalidate();
            }
        }

        protected void Delta(ref object var, object val)
        {
            if (var == val) return;

            var = val;

            if (this.m_Parent != null)
            {
                this.m_Parent.Invalidate();
            }
        }

        protected void Delta(ref GumpResponse var, GumpResponse val)
        {
            if (var == val) return;

            var = val;

            if (this.m_Parent != null)
            {
                this.m_Parent.Invalidate();
            }
        }
    }
}