using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using SFML.Graphics.Design;

namespace SFML.Graphics
{
    /// <summary>Defines a ray.</summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [TypeConverter(typeof(RayConverter))]
    public struct Ray : IEquatable<Ray>
    {
        /// <summary>Specifies the starting point of the Ray.</summary>
        public Vector3 Position;

        /// <summary>Unit vector specifying the direction the Ray is pointing.</summary>
        public Vector3 Direction;

        /// <summary>Creates a new instance of Ray.</summary>
        /// <param name="position">The starting point of the Ray.</param>
        /// <param name="direction">Unit vector describing the direction of the Ray.</param>
        public Ray(Vector3 position, Vector3 direction)
        {
            Position = position;
            Direction = direction;
        }

        /// <summary>Determines whether the specified Ray is equal to the current Ray.</summary>
        /// <param name="other">The Ray to compare with the current Ray.</param>
        public bool Equals(Ray other)
        {
            return (((((Position.X == other.Position.X) && (Position.Y == other.Position.Y)) &&
                      ((Position.Z == other.Position.Z) && (Direction.X == other.Direction.X))) &&
                     (Direction.Y == other.Direction.Y)) && (Direction.Z == other.Direction.Z));
        }

        /// <summary>Determines whether two instances of Ray are equal.</summary>
        /// <param name="obj">The Object to compare with the current Ray.</param>
        public override bool Equals(object obj)
        {
            var flag = false;
            if ((obj != null) && (obj is Ray))
                flag = Equals((Ray)obj);
            return flag;
        }

        /// <summary>Gets the hash code for this instance.</summary>
        public override int GetHashCode()
        {
            return (Position.GetHashCode() + Direction.GetHashCode());
        }

        /// <summary>Returns a String that represents the current Ray.</summary>
        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "{{Position:{0} Direction:{1}}}",
                new object[] { Position.ToString(), Direction.ToString() });
        }

        /// <summary>Checks whether the Ray intersects a specified BoundingBox.</summary>
        /// <param name="box">The BoundingBox to check for intersection with the Ray.</param>
        public float? Intersects(BoundingBox box)
        {
            return box.Intersects(this);
        }

        /// <summary>Checks whether the current Ray intersects a BoundingBox.</summary>
        /// <param name="box">The BoundingBox to check for intersection with.</param>
        /// <param name="result">[OutAttribute] Distance at which the ray intersects the BoundingBox or null if there is no intersection.</param>
        public void Intersects(ref BoundingBox box, out float? result)
        {
            box.Intersects(ref this, out result);
        }

        /// <summary>Checks whether the Ray intersects a specified BoundingFrustum.</summary>
        /// <param name="frustum">The BoundingFrustum to check for intersection with the Ray.</param>
        public float? Intersects(BoundingFrustum frustum)
        {
            if (frustum == null)
                throw new ArgumentNullException("frustum");
            return frustum.Intersects(this);
        }

        /// <summary>Determines whether this Ray intersects a specified Plane.</summary>
        /// <param name="plane">The Plane with which to calculate this Ray's intersection.</param>
        public float? Intersects(Plane plane)
        {
            var num2 = ((plane.Normal.X * Direction.X) + (plane.Normal.Y * Direction.Y)) + (plane.Normal.Z * Direction.Z);
            if (Math.Abs(num2) < 1E-05f)
                return null;
            var num3 = ((plane.Normal.X * Position.X) + (plane.Normal.Y * Position.Y)) + (plane.Normal.Z * Position.Z);
            var num = (-plane.D - num3) / num2;
            if (num < 0f)
            {
                if (num < -1E-05f)
                    return null;
                num = 0f;
            }
            return num;
        }

        /// <summary>Determines whether this Ray intersects a specified Plane.</summary>
        /// <param name="plane">The Plane with which to calculate this Ray's intersection.</param>
        /// <param name="result">[OutAttribute] The distance at which this Ray intersects the specified Plane, or null if there is no intersection.</param>
        public void Intersects(ref Plane plane, out float? result)
        {
            var num2 = ((plane.Normal.X * Direction.X) + (plane.Normal.Y * Direction.Y)) + (plane.Normal.Z * Direction.Z);
            if (Math.Abs(num2) < 1E-05f)
                result = 0;
            else
            {
                var num3 = ((plane.Normal.X * Position.X) + (plane.Normal.Y * Position.Y)) + (plane.Normal.Z * Position.Z);
                var num = (-plane.D - num3) / num2;
                if (num < 0f)
                {
                    if (num < -1E-05f)
                    {
                        result = 0;
                        return;
                    }
                }
                result = new float?(num);
            }
        }

        /// <summary>Checks whether the Ray intersects a specified BoundingSphere.</summary>
        /// <param name="sphere">The BoundingSphere to check for intersection with the Ray.</param>
        public float? Intersects(BoundingSphere sphere)
        {
            var num5 = sphere.Center.X - Position.X;
            var num4 = sphere.Center.Y - Position.Y;
            var num3 = sphere.Center.Z - Position.Z;
            var num7 = ((num5 * num5) + (num4 * num4)) + (num3 * num3);
            var num2 = sphere.Radius * sphere.Radius;
            if (num7 <= num2)
                return 0f;
            var num = ((num5 * Direction.X) + (num4 * Direction.Y)) + (num3 * Direction.Z);
            if (num < 0f)
                return null;
            var num6 = num7 - (num * num);
            if (num6 > num2)
                return null;
            var num8 = (float)Math.Sqrt((num2 - num6));
            return num - num8;
        }

        /// <summary>Checks whether the current Ray intersects a BoundingSphere.</summary>
        /// <param name="sphere">The BoundingSphere to check for intersection with.</param>
        /// <param name="result">[OutAttribute] Distance at which the ray intersects the BoundingSphere or null if there is no intersection.</param>
        public void Intersects(ref BoundingSphere sphere, out float? result)
        {
            var num5 = sphere.Center.X - Position.X;
            var num4 = sphere.Center.Y - Position.Y;
            var num3 = sphere.Center.Z - Position.Z;
            var num7 = ((num5 * num5) + (num4 * num4)) + (num3 * num3);
            var num2 = sphere.Radius * sphere.Radius;
            if (num7 <= num2)
                result = 0f;
            else
            {
                result = 0;
                var num = ((num5 * Direction.X) + (num4 * Direction.Y)) + (num3 * Direction.Z);
                if (num >= 0f)
                {
                    var num6 = num7 - (num * num);
                    if (num6 <= num2)
                    {
                        var num8 = (float)Math.Sqrt((num2 - num6));
                        result = new float?(num - num8);
                    }
                }
            }
        }

        /// <summary>Determines whether two instances of Ray are equal.</summary>
        /// <param name="a">The object to the left of the equality operator.</param>
        /// <param name="b">The object to the right of the equality operator.</param>
        public static bool operator ==(Ray a, Ray b)
        {
            return (((((a.Position.X == b.Position.X) && (a.Position.Y == b.Position.Y)) &&
                      ((a.Position.Z == b.Position.Z) && (a.Direction.X == b.Direction.X))) && (a.Direction.Y == b.Direction.Y)) &&
                    (a.Direction.Z == b.Direction.Z));
        }

        /// <summary>Determines whether two instances of Ray are not equal.</summary>
        /// <param name="a">The object to the left of the inequality operator.</param>
        /// <param name="b">The object to the right of the inequality operator.</param>
        public static bool operator !=(Ray a, Ray b)
        {
            if ((((a.Position.X == b.Position.X) && (a.Position.Y == b.Position.Y)) &&
                 ((a.Position.Z == b.Position.Z) && (a.Direction.X == b.Direction.X))) && (a.Direction.Y == b.Direction.Y))
                return (a.Direction.Z != b.Direction.Z);
            return true;
        }
    }
}