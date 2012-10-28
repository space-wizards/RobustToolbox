using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using SFML.Graphics.Design;

namespace SFML.Graphics
{
    /// <summary>Defines a point in 2D space.</summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [TypeConverter(typeof(PointConverter))]
    public struct Point : IEquatable<Point>
    {
        /// <summary>Specifies the x-coordinate of the Point.</summary>
        public readonly int X;

        /// <summary>Specifies the y-coordinate of the Point.</summary>
        public readonly int Y;

        static readonly Point _zero;

        /// <summary>Returns the point (0,0).</summary>
        public static Point Zero
        {
            get { return _zero; }
        }

        /// <summary>Initializes a new instance of Point.</summary>
        /// <param name="x">The x-coordinate of the Point.</param>
        /// <param name="y">The y-coordinate of the Point.</param>
        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>Determines whether two Point instances are equal.</summary>
        /// <param name="other">The Point to compare this instance to.</param>
        public bool Equals(Point other)
        {
            return ((X == other.X) && (Y == other.Y));
        }

        /// <summary>Determines whether two Point instances are equal.</summary>
        /// <param name="obj">The object to compare this instance to.</param>
        public override bool Equals(object obj)
        {
            var flag = false;
            if (obj is Point)
                flag = Equals((Point)obj);
            return flag;
        }

        /// <summary>Gets the hash code for this object.</summary>
        public override int GetHashCode()
        {
            return (X.GetHashCode() + Y.GetHashCode());
        }

        /// <summary>Returns a String that represents the current Point.</summary>
        public override string ToString()
        {
            var currentCulture = CultureInfo.CurrentCulture;
            return string.Format(currentCulture, "{{X:{0} Y:{1}}}",
                new object[] { X.ToString(currentCulture), Y.ToString(currentCulture) });
        }

        /// <summary>Determines whether two Point instances are equal.</summary>
        /// <param name="a">Point on the left side of the equal sign.</param>
        /// <param name="b">Point on the right side of the equal sign.</param>
        public static bool operator ==(Point a, Point b)
        {
            return a.Equals(b);
        }

        /// <summary>Determines whether two Point instances are not equal.</summary>
        /// <param name="a">The Point on the left side of the equal sign.</param>
        /// <param name="b">The Point on the right side of the equal sign.</param>
        public static bool operator !=(Point a, Point b)
        {
            if (a.X == b.X)
                return (a.Y != b.Y);
            return true;
        }

        static Point()
        {
            _zero = new Point();
        }
    }
}