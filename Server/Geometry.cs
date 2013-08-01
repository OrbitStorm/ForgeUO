/***************************************************************************
*                                Geometry.cs
*                            -------------------
*   begin                : May 1, 2002
*   copyright            : (C) The RunUO Software Team
*   email                : info@runuo.com
*
*   $Id: Geometry.cs 4 2006-06-15 04:28:39Z mark $
*
***************************************************************************/








/***************************************************************************
*
*   This program is free software; you can redistribute it and/or modify
*   it under the terms of the GNU General Public License as published by
*   the Free Software Foundation; either version 2 of the License, or
*   (at your option) any later version.
*
***************************************************************************/
using System;

namespace Server
{
    [Parsable]
    public struct Point2D : IPoint2D, IComparable, IComparable<Point2D>
    {
        public static readonly Point2D Zero = new Point2D(0, 0);
        internal int m_X;
        internal int m_Y;
        public Point2D(int x, int y)
        {
            this.m_X = x;
            this.m_Y = y;
        }

        public Point2D(IPoint2D p)
            : this(p.X, p.Y)
        {
        }

        [CommandProperty(AccessLevel.Counselor)]
        public int X
        {
            get
            {
                return this.m_X;
            }
            set
            {
                this.m_X = value;
            }
        }
        [CommandProperty(AccessLevel.Counselor)]
        public int Y
        {
            get
            {
                return this.m_Y;
            }
            set
            {
                this.m_Y = value;
            }
        }
        public static Point2D Parse(string value)
        {
            int start = value.IndexOf('(');
            int end = value.IndexOf(',', start + 1);

            string param1 = value.Substring(start + 1, end - (start + 1)).Trim();

            start = end;
            end = value.IndexOf(')', start + 1);

            string param2 = value.Substring(start + 1, end - (start + 1)).Trim();

            return new Point2D(Convert.ToInt32(param1), Convert.ToInt32(param2));
        }

        public override string ToString()
        {
            return String.Format("({0}, {1})", this.m_X, this.m_Y);
        }

        public int CompareTo(Point2D other)
        {
            int v = (this.m_X.CompareTo(other.m_X));

            if (v == 0)
                v = (this.m_Y.CompareTo(other.m_Y));

            return v;
        }

        public int CompareTo(object other)
        {
            if (other is Point2D)
                return this.CompareTo((Point2D)other);
            else if (other == null)
                return -1;

            throw new ArgumentException();
        }

        public override bool Equals(object o)
        {
            if (o == null || !(o is IPoint2D))
                return false;

            IPoint2D p = (IPoint2D)o;

            return this.m_X == p.X && this.m_Y == p.Y;
        }

        public override int GetHashCode()
        {
            return this.m_X ^ this.m_Y;
        }

        public static bool operator ==(Point2D l, Point2D r)
        {
            return l.m_X == r.m_X && l.m_Y == r.m_Y;
        }

        public static bool operator !=(Point2D l, Point2D r)
        {
            return l.m_X != r.m_X || l.m_Y != r.m_Y;
        }

        public static bool operator ==(Point2D l, IPoint2D r)
        {
            if (Object.ReferenceEquals(r, null))
                return false;

            return l.m_X == r.X && l.m_Y == r.Y;
        }

        public static bool operator !=(Point2D l, IPoint2D r)
        {
            if (Object.ReferenceEquals(r, null))
                return false;

            return l.m_X != r.X || l.m_Y != r.Y;
        }

        public static bool operator >(Point2D l, Point2D r)
        {
            return l.m_X > r.m_X && l.m_Y > r.m_Y;
        }

        public static bool operator >(Point2D l, Point3D r)
        {
            return l.m_X > r.m_X && l.m_Y > r.m_Y;
        }

        public static bool operator >(Point2D l, IPoint2D r)
        {
            if (Object.ReferenceEquals(r, null))
                return false;

            return l.m_X > r.X && l.m_Y > r.Y;
        }

        public static bool operator <(Point2D l, Point2D r)
        {
            return l.m_X < r.m_X && l.m_Y < r.m_Y;
        }

        public static bool operator <(Point2D l, Point3D r)
        {
            return l.m_X < r.m_X && l.m_Y < r.m_Y;
        }

        public static bool operator <(Point2D l, IPoint2D r)
        {
            if (Object.ReferenceEquals(r, null))
                return false;

            return l.m_X < r.X && l.m_Y < r.Y;
        }

        public static bool operator >=(Point2D l, Point2D r)
        {
            return l.m_X >= r.m_X && l.m_Y >= r.m_Y;
        }

        public static bool operator >=(Point2D l, Point3D r)
        {
            return l.m_X >= r.m_X && l.m_Y >= r.m_Y;
        }

        public static bool operator >=(Point2D l, IPoint2D r)
        {
            if (Object.ReferenceEquals(r, null))
                return false;

            return l.m_X >= r.X && l.m_Y >= r.Y;
        }

        public static bool operator <=(Point2D l, Point2D r)
        {
            return l.m_X <= r.m_X && l.m_Y <= r.m_Y;
        }

        public static bool operator <=(Point2D l, Point3D r)
        {
            return l.m_X <= r.m_X && l.m_Y <= r.m_Y;
        }

        public static bool operator <=(Point2D l, IPoint2D r)
        {
            if (Object.ReferenceEquals(r, null))
                return false;

            return l.m_X <= r.X && l.m_Y <= r.Y;
        }
    }

    [Parsable]
    public struct Point3D : IPoint3D, IComparable, IComparable<Point3D>
    {
        public static readonly Point3D Zero = new Point3D(0, 0, 0);
        internal int m_X;
        internal int m_Y;
        internal int m_Z;
        public Point3D(int x, int y, int z)
        {
            this.m_X = x;
            this.m_Y = y;
            this.m_Z = z;
        }

        public Point3D(IPoint3D p)
            : this(p.X, p.Y, p.Z)
        {
        }

        public Point3D(IPoint2D p, int z)
            : this(p.X, p.Y, z)
        {
        }

        [CommandProperty(AccessLevel.Counselor)]
        public int X
        {
            get
            {
                return this.m_X;
            }
            set
            {
                this.m_X = value;
            }
        }
        [CommandProperty(AccessLevel.Counselor)]
        public int Y
        {
            get
            {
                return this.m_Y;
            }
            set
            {
                this.m_Y = value;
            }
        }
        [CommandProperty(AccessLevel.Counselor)]
        public int Z
        {
            get
            {
                return this.m_Z;
            }
            set
            {
                this.m_Z = value;
            }
        }
        public static Point3D Parse(string value)
        {
            int start = value.IndexOf('(');
            int end = value.IndexOf(',', start + 1);

            string param1 = value.Substring(start + 1, end - (start + 1)).Trim();

            start = end;
            end = value.IndexOf(',', start + 1);

            string param2 = value.Substring(start + 1, end - (start + 1)).Trim();

            start = end;
            end = value.IndexOf(')', start + 1);

            string param3 = value.Substring(start + 1, end - (start + 1)).Trim();

            return new Point3D(Convert.ToInt32(param1), Convert.ToInt32(param2), Convert.ToInt32(param3));
        }

        public override string ToString()
        {
            return String.Format("({0}, {1}, {2})", this.m_X, this.m_Y, this.m_Z);
        }

        public override bool Equals(object o)
        {
            if (o == null || !(o is IPoint3D))
                return false;

            IPoint3D p = (IPoint3D)o;

            return this.m_X == p.X && this.m_Y == p.Y && this.m_Z == p.Z;
        }

        public override int GetHashCode()
        {
            return this.m_X ^ this.m_Y ^ this.m_Z;
        }

        public int CompareTo(Point3D other)
        {
            int v = (this.m_X.CompareTo(other.m_X));

            if (v == 0)
            {
                v = (this.m_Y.CompareTo(other.m_Y));

                if (v == 0)
                    v = (this.m_Z.CompareTo(other.m_Z));
            }

            return v;
        }

        public int CompareTo(object other)
        {
            if (other is Point3D)
                return this.CompareTo((Point3D)other);
            else if (other == null)
                return -1;

            throw new ArgumentException();
        }

        public static bool operator ==(Point3D l, Point3D r)
        {
            return l.m_X == r.m_X && l.m_Y == r.m_Y && l.m_Z == r.m_Z;
        }

        public static bool operator !=(Point3D l, Point3D r)
        {
            return l.m_X != r.m_X || l.m_Y != r.m_Y || l.m_Z != r.m_Z;
        }

        public static bool operator ==(Point3D l, IPoint3D r)
        {
            if (Object.ReferenceEquals(r, null))
                return false;

            return l.m_X == r.X && l.m_Y == r.Y && l.m_Z == r.Z;
        }

        public static bool operator !=(Point3D l, IPoint3D r)
        {
            if (Object.ReferenceEquals(r, null))
                return false;

            return l.m_X != r.X || l.m_Y != r.Y || l.m_Z != r.Z;
        }
    }

    [NoSort]
    [Parsable]
    [PropertyObject]
    public struct Rectangle2D
    {
        private Point2D m_Start;
        private Point2D m_End;
        public Rectangle2D(IPoint2D start, IPoint2D end)
        {
            this.m_Start = new Point2D(start);
            this.m_End = new Point2D(end);
        }

        public Rectangle2D(int x, int y, int width, int height)
        {
            this.m_Start = new Point2D(x, y);
            this.m_End = new Point2D(x + width, y + height);
        }

        [CommandProperty(AccessLevel.Counselor)]
        public Point2D Start
        {
            get
            {
                return this.m_Start;
            }
            set
            {
                this.m_Start = value;
            }
        }
        [CommandProperty(AccessLevel.Counselor)]
        public Point2D End
        {
            get
            {
                return this.m_End;
            }
            set
            {
                this.m_End = value;
            }
        }
        [CommandProperty(AccessLevel.Counselor)]
        public int X
        {
            get
            {
                return this.m_Start.m_X;
            }
            set
            {
                this.m_Start.m_X = value;
            }
        }
        [CommandProperty(AccessLevel.Counselor)]
        public int Y
        {
            get
            {
                return this.m_Start.m_Y;
            }
            set
            {
                this.m_Start.m_Y = value;
            }
        }
        [CommandProperty(AccessLevel.Counselor)]
        public int Width
        {
            get
            {
                return this.m_End.m_X - this.m_Start.m_X;
            }
            set
            {
                this.m_End.m_X = this.m_Start.m_X + value;
            }
        }
        [CommandProperty(AccessLevel.Counselor)]
        public int Height
        {
            get
            {
                return this.m_End.m_Y - this.m_Start.m_Y;
            }
            set
            {
                this.m_End.m_Y = this.m_Start.m_Y + value;
            }
        }
        public static Rectangle2D Parse(string value)
        {
            int start = value.IndexOf('(');
            int end = value.IndexOf(',', start + 1);

            string param1 = value.Substring(start + 1, end - (start + 1)).Trim();

            start = end;
            end = value.IndexOf(',', start + 1);

            string param2 = value.Substring(start + 1, end - (start + 1)).Trim();

            start = end;
            end = value.IndexOf(',', start + 1);

            string param3 = value.Substring(start + 1, end - (start + 1)).Trim();

            start = end;
            end = value.IndexOf(')', start + 1);

            string param4 = value.Substring(start + 1, end - (start + 1)).Trim();

            return new Rectangle2D(Convert.ToInt32(param1), Convert.ToInt32(param2), Convert.ToInt32(param3), Convert.ToInt32(param4));
        }

        public void Set(int x, int y, int width, int height)
        {
            this.m_Start = new Point2D(x, y);
            this.m_End = new Point2D(x + width, y + height);
        }

        public void MakeHold(Rectangle2D r)
        {
            if (r.m_Start.m_X < this.m_Start.m_X)
                this.m_Start.m_X = r.m_Start.m_X;

            if (r.m_Start.m_Y < this.m_Start.m_Y)
                this.m_Start.m_Y = r.m_Start.m_Y;

            if (r.m_End.m_X > this.m_End.m_X)
                this.m_End.m_X = r.m_End.m_X;

            if (r.m_End.m_Y > this.m_End.m_Y)
                this.m_End.m_Y = r.m_End.m_Y;
        }

        public bool Contains(Point3D p)
        {
            return (this.m_Start.m_X <= p.m_X && this.m_Start.m_Y <= p.m_Y && this.m_End.m_X > p.m_X && this.m_End.m_Y > p.m_Y);
            //return ( m_Start <= p && m_End > p );
        }

        public bool Contains(Point2D p)
        {
            return (this.m_Start.m_X <= p.m_X && this.m_Start.m_Y <= p.m_Y && this.m_End.m_X > p.m_X && this.m_End.m_Y > p.m_Y);
            //return ( m_Start <= p && m_End > p );
        }

        public bool Contains(IPoint2D p)
        {
            return (this.m_Start <= p && this.m_End > p);
        }

        public override string ToString()
        {
            return String.Format("({0}, {1})+({2}, {3})", this.X, this.Y, this.Width, this.Height);
        }
    }

    [NoSort]
    [PropertyObject]
    public struct Rectangle3D
    {
        private Point3D m_Start;
        private Point3D m_End;
        public Rectangle3D(Point3D start, Point3D end)
        {
            this.m_Start = start;
            this.m_End = end;
        }

        public Rectangle3D(int x, int y, int z, int width, int height, int depth)
        {
            this.m_Start = new Point3D(x, y, z);
            this.m_End = new Point3D(x + width, y + height, z + depth);
        }

        [CommandProperty(AccessLevel.Counselor)]
        public Point3D Start
        {
            get
            {
                return this.m_Start;
            }
            set
            {
                this.m_Start = value;
            }
        }
        [CommandProperty(AccessLevel.Counselor)]
        public Point3D End
        {
            get
            {
                return this.m_End;
            }
            set
            {
                this.m_End = value;
            }
        }
        [CommandProperty(AccessLevel.Counselor)]
        public int Width
        {
            get
            {
                return this.m_End.X - this.m_Start.X;
            }
        }
        [CommandProperty(AccessLevel.Counselor)]
        public int Height
        {
            get
            {
                return this.m_End.Y - this.m_Start.Y;
            }
        }
        [CommandProperty(AccessLevel.Counselor)]
        public int Depth
        {
            get
            {
                return this.m_End.Z - this.m_Start.Z;
            }
        }
        public bool Contains(Point3D p)
        {
            return (p.m_X >= this.m_Start.m_X) &&
                   (p.m_X < this.m_End.m_X) &&
                   (p.m_Y >= this.m_Start.m_Y) &&
                   (p.m_Y < this.m_End.m_Y) &&
                   (p.m_Z >= this.m_Start.m_Z) &&
                   (p.m_Z < this.m_End.m_Z);
        }

        public bool Contains(IPoint3D p)
        {
            return (p.X >= this.m_Start.m_X) &&
                   (p.X < this.m_End.m_X) &&
                   (p.Y >= this.m_Start.m_Y) &&
                   (p.Y < this.m_End.m_Y) &&
                   (p.Z >= this.m_Start.m_Z) &&
                   (p.Z < this.m_End.m_Z);
        }
    }
}