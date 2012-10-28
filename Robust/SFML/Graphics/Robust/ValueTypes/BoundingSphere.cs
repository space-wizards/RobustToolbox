using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using SFML.Graphics.Design;

namespace SFML.Graphics
{
    /// <summary>Defines a sphere.</summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [TypeConverter(typeof(BoundingSphereConverter))]
    public struct BoundingSphere : IEquatable<BoundingSphere>
    {
        /// <summary>The center point of the sphere.</summary>
        public Vector3 Center;

        /// <summary>The radius of the sphere.</summary>
        public float Radius;

        /// <summary>Creates a new instance of BoundingSphere.</summary>
        /// <param name="center">Center point of the sphere.</param>
        /// <param name="radius">Radius of the sphere.</param>
        public BoundingSphere(Vector3 center, float radius)
        {
            if (radius < 0f)
                throw new ArgumentException(FrameworkMessages.NegativeRadius);
            Center = center;
            Radius = radius;
        }

        /// <summary>Determines whether the specified BoundingSphere is equal to the current BoundingSphere.</summary>
        /// <param name="other">The BoundingSphere to compare with the current BoundingSphere.</param>
        public bool Equals(BoundingSphere other)
        {
            return ((Center == other.Center) && (Radius == other.Radius));
        }

        /// <summary>Determines whether the specified Object is equal to the BoundingSphere.</summary>
        /// <param name="obj">The Object to compare with the current BoundingSphere.</param>
        public override bool Equals(object obj)
        {
            var flag = false;
            if (obj is BoundingSphere)
                flag = Equals((BoundingSphere)obj);
            return flag;
        }

        /// <summary>Gets the hash code for this instance.</summary>
        public override int GetHashCode()
        {
            return (Center.GetHashCode() + Radius.GetHashCode());
        }

        /// <summary>Returns a String that represents the current BoundingSphere.</summary>
        public override string ToString()
        {
            var currentCulture = CultureInfo.CurrentCulture;
            return string.Format(currentCulture, "{{Center:{0} Radius:{1}}}",
                new object[] { Center.ToString(), Radius.ToString(currentCulture) });
        }

        /// <summary>Creates a BoundingSphere that contains the two specified BoundingSphere instances.</summary>
        /// <param name="original">BoundingSphere to be merged.</param>
        /// <param name="additional">BoundingSphere to be merged.</param>
        public static BoundingSphere CreateMerged(BoundingSphere original, BoundingSphere additional)
        {
            BoundingSphere sphere;
            Vector3 vector2;
            Vector3.Subtract(ref additional.Center, ref original.Center, out vector2);
            var num = vector2.Length();
            var radius = original.Radius;
            var num2 = additional.Radius;
            if ((radius + num2) >= num)
            {
                if ((radius - num2) >= num)
                    return original;
                if ((num2 - radius) >= num)
                    return additional;
            }
            var vector = (vector2 * (1f / num));
            var num5 = MathHelper.Min(-radius, num - num2);
            var num4 = (MathHelper.Max(radius, num + num2) - num5) * 0.5f;
            sphere.Center = original.Center + ((vector * (num4 + num5)));
            sphere.Radius = num4;
            return sphere;
        }

        /// <summary>Creates a BoundingSphere that contains the two specified BoundingSphere instances.</summary>
        /// <param name="original">BoundingSphere to be merged.</param>
        /// <param name="additional">BoundingSphere to be merged.</param>
        /// <param name="result">[OutAttribute] The created BoundingSphere.</param>
        public static void CreateMerged(ref BoundingSphere original, ref BoundingSphere additional, out BoundingSphere result)
        {
            Vector3 vector2;
            Vector3.Subtract(ref additional.Center, ref original.Center, out vector2);
            var num = vector2.Length();
            var radius = original.Radius;
            var num2 = additional.Radius;
            if ((radius + num2) >= num)
            {
                if ((radius - num2) >= num)
                {
                    result = original;
                    return;
                }
                if ((num2 - radius) >= num)
                {
                    result = additional;
                    return;
                }
            }
            var vector = (vector2 * (1f / num));
            var num5 = MathHelper.Min(-radius, num - num2);
            var num4 = (MathHelper.Max(radius, num + num2) - num5) * 0.5f;
            result.Center = original.Center + ((vector * (num4 + num5)));
            result.Radius = num4;
        }

        /// <summary>Creates the smallest BoundingSphere that can contain a specified BoundingBox.</summary>
        /// <param name="box">The BoundingBox to create the BoundingSphere from.</param>
        public static BoundingSphere CreateFromBoundingBox(BoundingBox box)
        {
            float num;
            BoundingSphere sphere;
            Vector3.Lerp(ref box.Min, ref box.Max, 0.5f, out sphere.Center);
            Vector3.Distance(ref box.Min, ref box.Max, out num);
            sphere.Radius = num * 0.5f;
            return sphere;
        }

        /// <summary>Creates the smallest BoundingSphere that can contain a specified BoundingBox.</summary>
        /// <param name="box">The BoundingBox to create the BoundingSphere from.</param>
        /// <param name="result">[OutAttribute] The created BoundingSphere.</param>
        public static void CreateFromBoundingBox(ref BoundingBox box, out BoundingSphere result)
        {
            float num;
            Vector3.Lerp(ref box.Min, ref box.Max, 0.5f, out result.Center);
            Vector3.Distance(ref box.Min, ref box.Max, out num);
            result.Radius = num * 0.5f;
        }

        /// <summary>Creates a BoundingSphere that can contain a specified list of points. Reference page contains links to related code samples.</summary>
        /// <param name="points">List of points the BoundingSphere must contain.</param>
        public static BoundingSphere CreateFromPoints(IEnumerable<Vector3> points)
        {
            float num;
            float num2;
            Vector3 vector2;
            float num4;
            float num5;
            BoundingSphere sphere;
            Vector3 vector5;
            Vector3 vector6;
            Vector3 vector7;
            Vector3 vector8;
            Vector3 vector9;
            if (points == null)
                throw new ArgumentNullException("points");
            var enumerator = points.GetEnumerator();
            if (!enumerator.MoveNext())
                throw new ArgumentException(FrameworkMessages.BoundingSphereZeroPoints);
            var vector4 = vector5 = vector6 = vector7 = vector8 = vector9 = enumerator.Current;
            foreach (var vector in points)
            {
                if (vector.X < vector4.X)
                    vector4 = vector;
                if (vector.X > vector5.X)
                    vector5 = vector;
                if (vector.Y < vector6.Y)
                    vector6 = vector;
                if (vector.Y > vector7.Y)
                    vector7 = vector;
                if (vector.Z < vector8.Z)
                    vector8 = vector;
                if (vector.Z > vector9.Z)
                    vector9 = vector;
            }
            Vector3.Distance(ref vector5, ref vector4, out num5);
            Vector3.Distance(ref vector7, ref vector6, out num4);
            Vector3.Distance(ref vector9, ref vector8, out num2);
            if (num5 > num4)
            {
                if (num5 > num2)
                {
                    Vector3.Lerp(ref vector5, ref vector4, 0.5f, out vector2);
                    num = num5 * 0.5f;
                }
                else
                {
                    Vector3.Lerp(ref vector9, ref vector8, 0.5f, out vector2);
                    num = num2 * 0.5f;
                }
            }
            else if (num4 > num2)
            {
                Vector3.Lerp(ref vector7, ref vector6, 0.5f, out vector2);
                num = num4 * 0.5f;
            }
            else
            {
                Vector3.Lerp(ref vector9, ref vector8, 0.5f, out vector2);
                num = num2 * 0.5f;
            }
            foreach (var vector10 in points)
            {
                Vector3 vector3;
                vector3.X = vector10.X - vector2.X;
                vector3.Y = vector10.Y - vector2.Y;
                vector3.Z = vector10.Z - vector2.Z;
                var num3 = vector3.Length();
                if (num3 > num)
                {
                    num = (num + num3) * 0.5f;
                    vector2 += ((1f - (num / num3)) * vector3);
                }
            }
            sphere.Center = vector2;
            sphere.Radius = num;
            return sphere;
        }

        /// <summary>Creates the smallest BoundingSphere that can contain a specified BoundingFrustum.</summary>
        /// <param name="frustum">The BoundingFrustum to create the BoundingSphere with.</param>
        public static BoundingSphere CreateFromFrustum(BoundingFrustum frustum)
        {
            if (frustum == null)
                throw new ArgumentNullException("frustum");
            return CreateFromPoints(frustum.cornerArray);
        }

        /// <summary>Checks whether the current BoundingSphere intersects with a specified BoundingBox.</summary>
        /// <param name="box">The BoundingBox to check for intersection with the current BoundingSphere.</param>
        public bool Intersects(BoundingBox box)
        {
            float num;
            Vector3 vector;
            Vector3.Clamp(ref Center, ref box.Min, ref box.Max, out vector);
            Vector3.DistanceSquared(ref Center, ref vector, out num);
            return (num <= (Radius * Radius));
        }

        /// <summary>Checks whether the current BoundingSphere intersects a BoundingBox.</summary>
        /// <param name="box">The BoundingBox to check for intersection with.</param>
        /// <param name="result">[OutAttribute] true if the BoundingSphere and BoundingBox intersect; false otherwise.</param>
        public void Intersects(ref BoundingBox box, out bool result)
        {
            float num;
            Vector3 vector;
            Vector3.Clamp(ref Center, ref box.Min, ref box.Max, out vector);
            Vector3.DistanceSquared(ref Center, ref vector, out num);
            result = num <= (Radius * Radius);
        }

        /// <summary>Checks whether the current BoundingSphere intersects with a specified BoundingFrustum.</summary>
        /// <param name="frustum">The BoundingFrustum to check for intersection with the current BoundingSphere.</param>
        public bool Intersects(BoundingFrustum frustum)
        {
            bool flag;
            if (null == frustum)
                throw new ArgumentNullException("frustum", FrameworkMessages.NullNotAllowed);
            frustum.Intersects(ref this, out flag);
            return flag;
        }

        /// <summary>Checks whether the current BoundingSphere intersects with a specified Plane.</summary>
        /// <param name="plane">The Plane to check for intersection with the current BoundingSphere.</param>
        public PlaneIntersectionType Intersects(Plane plane)
        {
            return plane.Intersects(this);
        }

        /// <summary>Checks whether the current BoundingSphere intersects a Plane.</summary>
        /// <param name="plane">The Plane to check for intersection with.</param>
        /// <param name="result">[OutAttribute] An enumeration indicating whether the BoundingSphere intersects the Plane.</param>
        public void Intersects(ref Plane plane, out PlaneIntersectionType result)
        {
            plane.Intersects(ref this, out result);
        }

        /// <summary>Checks whether the current BoundingSphere intersects with a specified Ray.</summary>
        /// <param name="ray">The Ray to check for intersection with the current BoundingSphere.</param>
        public float? Intersects(Ray ray)
        {
            return ray.Intersects(this);
        }

        /// <summary>Checks whether the current BoundingSphere intersects a Ray.</summary>
        /// <param name="ray">The Ray to check for intersection with.</param>
        /// <param name="result">[OutAttribute] Distance at which the ray intersects the BoundingSphere or null if there is no intersection.</param>
        public void Intersects(ref Ray ray, out float? result)
        {
            ray.Intersects(ref this, out result);
        }

        /// <summary>Checks whether the current BoundingSphere intersects with a specified BoundingSphere.</summary>
        /// <param name="sphere">The BoundingSphere to check for intersection with the current BoundingSphere.</param>
        public bool Intersects(BoundingSphere sphere)
        {
            float num3;
            Vector3.DistanceSquared(ref Center, ref sphere.Center, out num3);
            var radius = Radius;
            var num = sphere.Radius;
            if ((((radius * radius) + ((2f * radius) * num)) + (num * num)) <= num3)
                return false;
            return true;
        }

        /// <summary>Checks whether the current BoundingSphere intersects another BoundingSphere.</summary>
        /// <param name="sphere">The BoundingSphere to check for intersection with.</param>
        /// <param name="result">[OutAttribute] true if the BoundingSphere instances intersect; false otherwise.</param>
        public void Intersects(ref BoundingSphere sphere, out bool result)
        {
            float num3;
            Vector3.DistanceSquared(ref Center, ref sphere.Center, out num3);
            var radius = Radius;
            var num = sphere.Radius;
            result = (((radius * radius) + ((2f * radius) * num)) + (num * num)) > num3;
        }

        /// <summary>Checks whether the current BoundingSphere contains the specified BoundingBox.</summary>
        /// <param name="box">The BoundingBox to check against the current BoundingSphere.</param>
        public ContainmentType Contains(BoundingBox box)
        {
            Vector3 vector;
            if (!box.Intersects(this))
                return ContainmentType.Disjoint;
            var num = Radius * Radius;
            vector.X = Center.X - box.Min.X;
            vector.Y = Center.Y - box.Max.Y;
            vector.Z = Center.Z - box.Max.Z;
            if (vector.LengthSquared() > num)
                return ContainmentType.Intersects;
            vector.X = Center.X - box.Max.X;
            vector.Y = Center.Y - box.Max.Y;
            vector.Z = Center.Z - box.Max.Z;
            if (vector.LengthSquared() > num)
                return ContainmentType.Intersects;
            vector.X = Center.X - box.Max.X;
            vector.Y = Center.Y - box.Min.Y;
            vector.Z = Center.Z - box.Max.Z;
            if (vector.LengthSquared() > num)
                return ContainmentType.Intersects;
            vector.X = Center.X - box.Min.X;
            vector.Y = Center.Y - box.Min.Y;
            vector.Z = Center.Z - box.Max.Z;
            if (vector.LengthSquared() > num)
                return ContainmentType.Intersects;
            vector.X = Center.X - box.Min.X;
            vector.Y = Center.Y - box.Max.Y;
            vector.Z = Center.Z - box.Min.Z;
            if (vector.LengthSquared() > num)
                return ContainmentType.Intersects;
            vector.X = Center.X - box.Max.X;
            vector.Y = Center.Y - box.Max.Y;
            vector.Z = Center.Z - box.Min.Z;
            if (vector.LengthSquared() > num)
                return ContainmentType.Intersects;
            vector.X = Center.X - box.Max.X;
            vector.Y = Center.Y - box.Min.Y;
            vector.Z = Center.Z - box.Min.Z;
            if (vector.LengthSquared() > num)
                return ContainmentType.Intersects;
            vector.X = Center.X - box.Min.X;
            vector.Y = Center.Y - box.Min.Y;
            vector.Z = Center.Z - box.Min.Z;
            if (vector.LengthSquared() > num)
                return ContainmentType.Intersects;
            return ContainmentType.Contains;
        }

        /// <summary>Checks whether the current BoundingSphere contains the specified BoundingBox.</summary>
        /// <param name="box">The BoundingBox to test for overlap.</param>
        /// <param name="result">[OutAttribute] Enumeration indicating the extent of overlap.</param>
        public void Contains(ref BoundingBox box, out ContainmentType result)
        {
            bool flag;
            box.Intersects(ref this, out flag);
            if (!flag)
                result = ContainmentType.Disjoint;
            else
            {
                Vector3 vector;
                var num = Radius * Radius;
                result = ContainmentType.Intersects;
                vector.X = Center.X - box.Min.X;
                vector.Y = Center.Y - box.Max.Y;
                vector.Z = Center.Z - box.Max.Z;
                if (vector.LengthSquared() <= num)
                {
                    vector.X = Center.X - box.Max.X;
                    vector.Y = Center.Y - box.Max.Y;
                    vector.Z = Center.Z - box.Max.Z;
                    if (vector.LengthSquared() <= num)
                    {
                        vector.X = Center.X - box.Max.X;
                        vector.Y = Center.Y - box.Min.Y;
                        vector.Z = Center.Z - box.Max.Z;
                        if (vector.LengthSquared() <= num)
                        {
                            vector.X = Center.X - box.Min.X;
                            vector.Y = Center.Y - box.Min.Y;
                            vector.Z = Center.Z - box.Max.Z;
                            if (vector.LengthSquared() <= num)
                            {
                                vector.X = Center.X - box.Min.X;
                                vector.Y = Center.Y - box.Max.Y;
                                vector.Z = Center.Z - box.Min.Z;
                                if (vector.LengthSquared() <= num)
                                {
                                    vector.X = Center.X - box.Max.X;
                                    vector.Y = Center.Y - box.Max.Y;
                                    vector.Z = Center.Z - box.Min.Z;
                                    if (vector.LengthSquared() <= num)
                                    {
                                        vector.X = Center.X - box.Max.X;
                                        vector.Y = Center.Y - box.Min.Y;
                                        vector.Z = Center.Z - box.Min.Z;
                                        if (vector.LengthSquared() <= num)
                                        {
                                            vector.X = Center.X - box.Min.X;
                                            vector.Y = Center.Y - box.Min.Y;
                                            vector.Z = Center.Z - box.Min.Z;
                                            if (vector.LengthSquared() <= num)
                                                result = ContainmentType.Contains;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>Checks whether the current BoundingSphere contains the specified BoundingFrustum.</summary>
        /// <param name="frustum">The BoundingFrustum to check against the current BoundingSphere.</param>
        public ContainmentType Contains(BoundingFrustum frustum)
        {
            if (null == frustum)
                throw new ArgumentNullException("frustum", FrameworkMessages.NullNotAllowed);
            if (!frustum.Intersects(this))
                return ContainmentType.Disjoint;
            var num2 = Radius * Radius;
            foreach (var vector2 in frustum.cornerArray)
            {
                Vector3 vector;
                vector.X = vector2.X - Center.X;
                vector.Y = vector2.Y - Center.Y;
                vector.Z = vector2.Z - Center.Z;
                if (vector.LengthSquared() > num2)
                    return ContainmentType.Intersects;
            }
            return ContainmentType.Contains;
        }

        /// <summary>Checks whether the current BoundingSphere contains the specified point.</summary>
        /// <param name="point">The point to check against the current BoundingSphere.</param>
        public ContainmentType Contains(Vector3 point)
        {
            if (Vector3.DistanceSquared(point, Center) >= (Radius * Radius))
                return ContainmentType.Disjoint;
            return ContainmentType.Contains;
        }

        /// <summary>Checks whether the current BoundingSphere contains the specified point.</summary>
        /// <param name="point">The point to test for overlap.</param>
        /// <param name="result">[OutAttribute] Enumeration indicating the extent of overlap.</param>
        public void Contains(ref Vector3 point, out ContainmentType result)
        {
            float num;
            Vector3.DistanceSquared(ref point, ref Center, out num);
            result = (num < (Radius * Radius)) ? ContainmentType.Contains : ContainmentType.Disjoint;
        }

        /// <summary>Checks whether the current BoundingSphere contains the specified BoundingSphere.</summary>
        /// <param name="sphere">The BoundingSphere to check against the current BoundingSphere.</param>
        public ContainmentType Contains(BoundingSphere sphere)
        {
            float num3;
            Vector3.Distance(ref Center, ref sphere.Center, out num3);
            var radius = Radius;
            var num = sphere.Radius;
            if ((radius + num) < num3)
                return ContainmentType.Disjoint;
            if ((radius - num) < num3)
                return ContainmentType.Intersects;
            return ContainmentType.Contains;
        }

        /// <summary>Checks whether the current BoundingSphere contains the specified BoundingSphere.</summary>
        /// <param name="sphere">The BoundingSphere to test for overlap.</param>
        /// <param name="result">[OutAttribute] Enumeration indicating the extent of overlap.</param>
        public void Contains(ref BoundingSphere sphere, out ContainmentType result)
        {
            float num3;
            Vector3.Distance(ref Center, ref sphere.Center, out num3);
            var radius = Radius;
            var num = sphere.Radius;
            result = ((radius + num) >= num3)
                         ? (((radius - num) >= num3) ? ContainmentType.Contains : ContainmentType.Intersects)
                         : ContainmentType.Disjoint;
        }

        internal void SupportMapping(ref Vector3 v, out Vector3 result)
        {
            var num2 = v.Length();
            var num = Radius / num2;
            result.X = Center.X + (v.X * num);
            result.Y = Center.Y + (v.Y * num);
            result.Z = Center.Z + (v.Z * num);
        }

        /// <summary>Translates and scales the BoundingSphere using a given Matrix.</summary>
        /// <param name="matrix">A transformation matrix that might include translation, rotation, or uniform scaling. Note that BoundingSphere.Transform will not return correct results if there are non-uniform scaling, shears, or other unusual transforms in this transformation matrix. This is because there is no way to shear or non-uniformly scale a sphere. Such an operation would cause the sphere to lose its shape as a sphere.</param>
        public BoundingSphere Transform(Matrix matrix)
        {
            var sphere = new BoundingSphere { Center = Vector3.Transform(Center, matrix) };
            var num4 = ((matrix.M11 * matrix.M11) + (matrix.M12 * matrix.M12)) + (matrix.M13 * matrix.M13);
            var num3 = ((matrix.M21 * matrix.M21) + (matrix.M22 * matrix.M22)) + (matrix.M23 * matrix.M23);
            var num2 = ((matrix.M31 * matrix.M31) + (matrix.M32 * matrix.M32)) + (matrix.M33 * matrix.M33);
            var num = Math.Max(num4, Math.Max(num3, num2));
            sphere.Radius = Radius * ((float)Math.Sqrt(num));
            return sphere;
        }

        /// <summary>Translates and scales the BoundingSphere using a given Matrix.</summary>
        /// <param name="matrix">A transformation matrix that might include translation, rotation, or uniform scaling. Note that BoundingSphere.Transform will not return correct results if there are non-uniform scaling, shears, or other unusual transforms in this transformation matrix. This is because there is no way to shear or non-uniformly scale a sphere. Such an operation would cause the sphere to lose its shape as a sphere.</param>
        /// <param name="result">[OutAttribute] The transformed BoundingSphere.</param>
        public void Transform(ref Matrix matrix, out BoundingSphere result)
        {
            result.Center = Vector3.Transform(Center, matrix);
            var num4 = ((matrix.M11 * matrix.M11) + (matrix.M12 * matrix.M12)) + (matrix.M13 * matrix.M13);
            var num3 = ((matrix.M21 * matrix.M21) + (matrix.M22 * matrix.M22)) + (matrix.M23 * matrix.M23);
            var num2 = ((matrix.M31 * matrix.M31) + (matrix.M32 * matrix.M32)) + (matrix.M33 * matrix.M33);
            var num = Math.Max(num4, Math.Max(num3, num2));
            result.Radius = Radius * ((float)Math.Sqrt(num));
        }

        /// <summary>Determines whether two instances of BoundingSphere are equal.</summary>
        /// <param name="a">The object to the left of the equality operator.</param>
        /// <param name="b">The object to the right of the equality operator.</param>
        public static bool operator ==(BoundingSphere a, BoundingSphere b)
        {
            return a.Equals(b);
        }

        /// <summary>Determines whether two instances of BoundingSphere are not equal.</summary>
        /// <param name="a">The BoundingSphere to the left of the inequality operator.</param>
        /// <param name="b">The BoundingSphere to the right of the inequality operator.</param>
        public static bool operator !=(BoundingSphere a, BoundingSphere b)
        {
            if (!(a.Center != b.Center))
                return (a.Radius != b.Radius);
            return true;
        }
    }
}