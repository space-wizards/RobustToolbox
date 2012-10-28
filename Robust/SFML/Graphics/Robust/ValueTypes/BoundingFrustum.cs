using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace SFML.Graphics
{
    /// <summary>Defines a frustum and helps determine whether forms intersect with it.</summary>
    [Serializable]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class BoundingFrustum : IEquatable<BoundingFrustum>
    {
        /// <summary>Specifies the total number of corners (8) in the BoundingFrustum.</summary>
        public const int CornerCount = 8;

        internal readonly Vector3[] cornerArray;
        readonly Plane[] planes;
        Gjk gjk;
        Matrix matrix;

        /// <summary>Creates a new instance of BoundingFrustum. Reference page contains links to related conceptual articles.</summary>
        /// <param name="value">Combined matrix that usually takes view × projection matrix.</param>
        public BoundingFrustum(Matrix value)
        {
            planes = new Plane[6];
            cornerArray = new Vector3[8];
            SetMatrix(ref value);
        }

        BoundingFrustum()
        {
            planes = new Plane[6];
            cornerArray = new Vector3[8];
        }

        /// <summary>Gets the bottom plane of the BoundingFrustum.</summary>
        public Plane Bottom
        {
            get { return planes[5]; }
        }

        /// <summary>Gets the far plane of the BoundingFrustum.</summary>
        public Plane Far
        {
            get { return planes[1]; }
        }

        /// <summary>Gets the left plane of the BoundingFrustum.</summary>
        public Plane Left
        {
            get { return planes[2]; }
        }

        /// <summary>Gets or sets the Matrix that describes this bounding frustum.</summary>
        public Matrix Matrix
        {
            get { return matrix; }
            set { SetMatrix(ref value); }
        }

        /// <summary>Gets the near plane of the BoundingFrustum.</summary>
        public Plane Near
        {
            get { return planes[0]; }
        }

        /// <summary>Gets the right plane of the BoundingFrustum.</summary>
        public Plane Right
        {
            get { return planes[3]; }
        }

        /// <summary>Gets the top plane of the BoundingFrustum.</summary>
        public Plane Top
        {
            get { return planes[4]; }
        }

        static Vector3 ComputeIntersection(ref Plane plane, ref Ray ray)
        {
            var num = (-plane.D - Vector3.Dot(plane.Normal, ray.Position)) / Vector3.Dot(plane.Normal, ray.Direction);
            return (ray.Position + ((ray.Direction * num)));
        }

        static Ray ComputeIntersectionLine(ref Plane p1, ref Plane p2)
        {
            var ray = new Ray { Direction = Vector3.Cross(p1.Normal, p2.Normal) };
            var num = ray.Direction.LengthSquared();
            ray.Position = (Vector3.Cross(((-p1.D * p2.Normal) + (p2.D * p1.Normal)), ray.Direction) / num);
            return ray;
        }

        /// <summary>Checks whether the current BoundingFrustum contains the specified BoundingBox.</summary>
        /// <param name="box">The BoundingBox to check against the current BoundingFrustum.</param>
        public ContainmentType Contains(BoundingBox box)
        {
            var flag = false;
            foreach (var plane in planes)
            {
                switch (box.Intersects(plane))
                {
                    case PlaneIntersectionType.Front:
                        return ContainmentType.Disjoint;

                    case PlaneIntersectionType.Intersecting:
                        flag = true;
                        break;
                }
            }
            if (!flag)
                return ContainmentType.Contains;
            return ContainmentType.Intersects;
        }

        /// <summary>Checks whether the current BoundingFrustum contains the specified BoundingFrustum.</summary>
        /// <param name="frustum">The BoundingFrustum to check against the current BoundingFrustum.</param>
        public ContainmentType Contains(BoundingFrustum frustum)
        {
            if (frustum == null)
                throw new ArgumentNullException("frustum");
            var disjoint = ContainmentType.Disjoint;
            if (Intersects(frustum))
            {
                disjoint = ContainmentType.Contains;
                for (var i = 0; i < cornerArray.Length; i++)
                {
                    if (Contains(frustum.cornerArray[i]) == ContainmentType.Disjoint)
                        return ContainmentType.Intersects;
                }
            }
            return disjoint;
        }

        /// <summary>Checks whether the current BoundingFrustum contains the specified BoundingSphere.</summary>
        /// <param name="sphere">The BoundingSphere to check against the current BoundingFrustum.</param>
        public ContainmentType Contains(BoundingSphere sphere)
        {
            var center = sphere.Center;
            var radius = sphere.Radius;
            var num2 = 0;
            foreach (var plane in planes)
            {
                var num5 = ((plane.Normal.X * center.X) + (plane.Normal.Y * center.Y)) + (plane.Normal.Z * center.Z);
                var num3 = num5 + plane.D;
                if (num3 > radius)
                    return ContainmentType.Disjoint;
                if (num3 < -radius)
                    num2++;
            }
            if (num2 != 6)
                return ContainmentType.Intersects;
            return ContainmentType.Contains;
        }

        /// <summary>Checks whether the current BoundingFrustum contains the specified point.</summary>
        /// <param name="point">The point to check against the current BoundingFrustum.</param>
        public ContainmentType Contains(Vector3 point)
        {
            foreach (var plane in planes)
            {
                var num2 = (((plane.Normal.X * point.X) + (plane.Normal.Y * point.Y)) + (plane.Normal.Z * point.Z)) + plane.D;
                if (num2 > 1E-05f)
                    return ContainmentType.Disjoint;
            }
            return ContainmentType.Contains;
        }

        /// <summary>Checks whether the current BoundingFrustum contains the specified BoundingBox.</summary>
        /// <param name="box">The BoundingBox to test for overlap.</param>
        /// <param name="result">[OutAttribute] Enumeration indicating the extent of overlap.</param>
        public void Contains(ref BoundingBox box, out ContainmentType result)
        {
            var flag = false;
            foreach (var plane in planes)
            {
                switch (box.Intersects(plane))
                {
                    case PlaneIntersectionType.Front:
                        result = ContainmentType.Disjoint;
                        return;

                    case PlaneIntersectionType.Intersecting:
                        flag = true;
                        break;
                }
            }
            result = flag ? ContainmentType.Intersects : ContainmentType.Contains;
        }

        /// <summary>Checks whether the current BoundingFrustum contains the specified BoundingSphere.</summary>
        /// <param name="sphere">The BoundingSphere to test for overlap.</param>
        /// <param name="result">[OutAttribute] Enumeration indicating the extent of overlap.</param>
        public void Contains(ref BoundingSphere sphere, out ContainmentType result)
        {
            var center = sphere.Center;
            var radius = sphere.Radius;
            var num2 = 0;
            foreach (var plane in planes)
            {
                var num5 = ((plane.Normal.X * center.X) + (plane.Normal.Y * center.Y)) + (plane.Normal.Z * center.Z);
                var num3 = num5 + plane.D;
                if (num3 > radius)
                {
                    result = ContainmentType.Disjoint;
                    return;
                }
                if (num3 < -radius)
                    num2++;
            }
            result = (num2 == 6) ? ContainmentType.Contains : ContainmentType.Intersects;
        }

        /// <summary>Checks whether the current BoundingFrustum contains the specified point.</summary>
        /// <param name="point">The point to test for overlap.</param>
        /// <param name="result">[OutAttribute] Enumeration indicating the extent of overlap.</param>
        public void Contains(ref Vector3 point, out ContainmentType result)
        {
            foreach (var plane in planes)
            {
                var num2 = (((plane.Normal.X * point.X) + (plane.Normal.Y * point.Y)) + (plane.Normal.Z * point.Z)) + plane.D;
                if (num2 > 1E-05f)
                {
                    result = ContainmentType.Disjoint;
                    return;
                }
            }
            result = ContainmentType.Contains;
        }

        /// <summary>Determines whether the specified Object is equal to the BoundingFrustum.</summary>
        /// <param name="obj">The Object to compare with the current BoundingFrustum.</param>
        public override bool Equals(object obj)
        {
            var flag = false;
            var frustum = obj as BoundingFrustum;
            if (frustum != null)
                flag = matrix == frustum.matrix;
            return flag;
        }

        /// <summary>Gets an array of points that make up the corners of the BoundingFrustum.</summary>
        public Vector3[] GetCorners()
        {
            return (Vector3[])cornerArray.Clone();
        }

        /// <summary>Gets an array of points that make up the corners of the BoundingFrustum.</summary>
        /// <param name="corners">An existing array of at least 8 Vector3 points where the corners of the BoundingFrustum are written.</param>
        public void GetCorners(Vector3[] corners)
        {
            if (corners == null)
                throw new ArgumentNullException("corners");
            if (corners.Length < 8)
                throw new ArgumentOutOfRangeException("corners", FrameworkMessages.NotEnoughCorners);
            cornerArray.CopyTo(corners, 0);
        }

        /// <summary>Gets the hash code for this instance.</summary>
        public override int GetHashCode()
        {
            return matrix.GetHashCode();
        }

        /// <summary>Checks whether the current BoundingFrustum intersects the specified BoundingBox.</summary>
        /// <param name="box">The BoundingBox to check for intersection.</param>
        public bool Intersects(BoundingBox box)
        {
            bool flag;
            Intersects(ref box, out flag);
            return flag;
        }

        /// <summary>Checks whether the current BoundingFrustum intersects the specified BoundingFrustum.</summary>
        /// <param name="frustum">The BoundingFrustum to check for intersection.</param>
        public bool Intersects(BoundingFrustum frustum)
        {
            Vector3 closestPoint;
            if (frustum == null)
                throw new ArgumentNullException("frustum");
            if (gjk == null)
                gjk = new Gjk();
            gjk.Reset();
            Vector3.Subtract(ref cornerArray[0], ref frustum.cornerArray[0], out closestPoint);
            if (closestPoint.LengthSquared() < 1E-05f)
                Vector3.Subtract(ref cornerArray[0], ref frustum.cornerArray[1], out closestPoint);
            var maxValue = float.MaxValue;
            float num3;
            do
            {
                Vector3 vector2;
                Vector3 vector3;
                Vector3 vector4;
                Vector3 vector5;
                vector5.X = -closestPoint.X;
                vector5.Y = -closestPoint.Y;
                vector5.Z = -closestPoint.Z;
                SupportMapping(ref vector5, out vector4);
                frustum.SupportMapping(ref closestPoint, out vector3);
                Vector3.Subtract(ref vector4, ref vector3, out vector2);
                var num4 = ((closestPoint.X * vector2.X) + (closestPoint.Y * vector2.Y)) + (closestPoint.Z * vector2.Z);
                if (num4 > 0f)
                    return false;
                gjk.AddSupportPoint(ref vector2);
                closestPoint = gjk.ClosestPoint;
                var num2 = maxValue;
                maxValue = closestPoint.LengthSquared();
                num3 = 4E-05f * gjk.MaxLengthSquared;
                if ((num2 - maxValue) <= (1E-05f * num2))
                    return false;
            }
            while (!gjk.FullSimplex && (maxValue >= num3));
            return true;
        }

        /// <summary>Checks whether the current BoundingFrustum intersects the specified BoundingSphere.</summary>
        /// <param name="sphere">The BoundingSphere to check for intersection.</param>
        public bool Intersects(BoundingSphere sphere)
        {
            bool flag;
            Intersects(ref sphere, out flag);
            return flag;
        }

        /// <summary>Checks whether the current BoundingFrustum intersects the specified Plane.</summary>
        /// <param name="plane">The Plane to check for intersection.</param>
        public PlaneIntersectionType Intersects(Plane plane)
        {
            var num = 0;
            for (var i = 0; i < 8; i++)
            {
                float num3;
                Vector3.Dot(ref cornerArray[i], ref plane.Normal, out num3);
                if ((num3 + plane.D) > 0f)
                    num |= 1;
                else
                    num |= 2;
                if (num == 3)
                    return PlaneIntersectionType.Intersecting;
            }
            if (num != 1)
                return PlaneIntersectionType.Back;
            return PlaneIntersectionType.Front;
        }

        /// <summary>Checks whether the current BoundingFrustum intersects the specified Ray.</summary>
        /// <param name="ray">The Ray to check for intersection.</param>
        public float? Intersects(Ray ray)
        {
            float? nullable;
            Intersects(ref ray, out nullable);
            return nullable;
        }

        /// <summary>Checks whether the current BoundingFrustum intersects a BoundingBox.</summary>
        /// <param name="box">The BoundingBox to check for intersection with.</param>
        /// <param name="result">[OutAttribute] true if the BoundingFrustum and BoundingBox intersect; false otherwise.</param>
        public void Intersects(ref BoundingBox box, out bool result)
        {
            Vector3 closestPoint;
            Vector3 vector2;
            Vector3 vector3;
            Vector3 vector4;
            Vector3 vector5;
            if (gjk == null)
                gjk = new Gjk();
            gjk.Reset();
            Vector3.Subtract(ref cornerArray[0], ref box.Min, out closestPoint);
            if (closestPoint.LengthSquared() < 1E-05f)
                Vector3.Subtract(ref cornerArray[0], ref box.Max, out closestPoint);
            var maxValue = float.MaxValue;
            result = false;
            Label_006D:
            vector5.X = -closestPoint.X;
            vector5.Y = -closestPoint.Y;
            vector5.Z = -closestPoint.Z;
            SupportMapping(ref vector5, out vector4);
            box.SupportMapping(ref closestPoint, out vector3);
            Vector3.Subtract(ref vector4, ref vector3, out vector2);
            var num4 = ((closestPoint.X * vector2.X) + (closestPoint.Y * vector2.Y)) + (closestPoint.Z * vector2.Z);
            if (num4 <= 0f)
            {
                gjk.AddSupportPoint(ref vector2);
                closestPoint = gjk.ClosestPoint;
                var num2 = maxValue;
                maxValue = closestPoint.LengthSquared();
                if ((num2 - maxValue) > (1E-05f * num2))
                {
                    var num3 = 4E-05f * gjk.MaxLengthSquared;
                    if (!gjk.FullSimplex && (maxValue >= num3))
                        goto Label_006D;
                    result = true;
                }
            }
        }

        /// <summary>Checks whether the current BoundingFrustum intersects a BoundingSphere.</summary>
        /// <param name="sphere">The BoundingSphere to check for intersection with.</param>
        /// <param name="result">[OutAttribute] true if the BoundingFrustum and BoundingSphere intersect; false otherwise.</param>
        public void Intersects(ref BoundingSphere sphere, out bool result)
        {
            Vector3 unitX;
            Vector3 vector2;
            Vector3 vector3;
            Vector3 vector4;
            Vector3 vector5;
            if (gjk == null)
                gjk = new Gjk();
            gjk.Reset();
            Vector3.Subtract(ref cornerArray[0], ref sphere.Center, out unitX);
            if (unitX.LengthSquared() < 1E-05f)
                unitX = Vector3.UnitX;
            var maxValue = float.MaxValue;
            result = false;
            Label_005A:
            vector5.X = -unitX.X;
            vector5.Y = -unitX.Y;
            vector5.Z = -unitX.Z;
            SupportMapping(ref vector5, out vector4);
            sphere.SupportMapping(ref unitX, out vector3);
            Vector3.Subtract(ref vector4, ref vector3, out vector2);
            var num4 = ((unitX.X * vector2.X) + (unitX.Y * vector2.Y)) + (unitX.Z * vector2.Z);
            if (num4 <= 0f)
            {
                gjk.AddSupportPoint(ref vector2);
                unitX = gjk.ClosestPoint;
                var num2 = maxValue;
                maxValue = unitX.LengthSquared();
                if ((num2 - maxValue) > (1E-05f * num2))
                {
                    var num3 = 4E-05f * gjk.MaxLengthSquared;
                    if (!gjk.FullSimplex && (maxValue >= num3))
                        goto Label_005A;
                    result = true;
                }
            }
        }

        /// <summary>Checks whether the current BoundingFrustum intersects a Plane.</summary>
        /// <param name="plane">The Plane to check for intersection with.</param>
        /// <param name="result">[OutAttribute] An enumeration indicating whether the BoundingFrustum intersects the Plane.</param>
        public void Intersects(ref Plane plane, out PlaneIntersectionType result)
        {
            var num = 0;
            for (var i = 0; i < 8; i++)
            {
                float num3;
                Vector3.Dot(ref cornerArray[i], ref plane.Normal, out num3);
                if ((num3 + plane.D) > 0f)
                    num |= 1;
                else
                    num |= 2;
                if (num == 3)
                {
                    result = PlaneIntersectionType.Intersecting;
                    return;
                }
            }
            result = (num == 1) ? PlaneIntersectionType.Front : PlaneIntersectionType.Back;
        }

        /// <summary>Checks whether the current BoundingFrustum intersects a Ray.</summary>
        /// <param name="ray">The Ray to check for intersection with.</param>
        /// <param name="result">[OutAttribute] Distance at which the ray intersects the BoundingFrustum or null if there is no intersection.</param>
        public void Intersects(ref Ray ray, out float? result)
        {
            ContainmentType type;
            Contains(ref ray.Position, out type);
            if (type == ContainmentType.Contains)
                result = 0f;
            else
            {
                var minValue = float.MinValue;
                var maxValue = float.MaxValue;
                result = 0;
                foreach (var plane in planes)
                {
                    float num3;
                    float num6;
                    var normal = plane.Normal;
                    Vector3.Dot(ref ray.Direction, ref normal, out num6);
                    Vector3.Dot(ref ray.Position, ref normal, out num3);
                    num3 += plane.D;
                    if (Math.Abs(num6) < 1E-05f)
                    {
                        if (num3 > 0f)
                            return;
                    }
                    else
                    {
                        var num = -num3 / num6;
                        if (num6 < 0f)
                        {
                            if (num > maxValue)
                                return;
                            if (num > minValue)
                                minValue = num;
                        }
                        else
                        {
                            if (num < minValue)
                                return;
                            if (num < maxValue)
                                maxValue = num;
                        }
                    }
                }
                var num7 = (minValue >= 0f) ? minValue : maxValue;
                if (num7 >= 0f)
                    result = new float?(num7);
            }
        }

        void SetMatrix(ref Matrix value)
        {
            matrix = value;
            planes[2].Normal.X = -value.M14 - value.M11;
            planes[2].Normal.Y = -value.M24 - value.M21;
            planes[2].Normal.Z = -value.M34 - value.M31;
            planes[2].D = -value.M44 - value.M41;
            planes[3].Normal.X = -value.M14 + value.M11;
            planes[3].Normal.Y = -value.M24 + value.M21;
            planes[3].Normal.Z = -value.M34 + value.M31;
            planes[3].D = -value.M44 + value.M41;
            planes[4].Normal.X = -value.M14 + value.M12;
            planes[4].Normal.Y = -value.M24 + value.M22;
            planes[4].Normal.Z = -value.M34 + value.M32;
            planes[4].D = -value.M44 + value.M42;
            planes[5].Normal.X = -value.M14 - value.M12;
            planes[5].Normal.Y = -value.M24 - value.M22;
            planes[5].Normal.Z = -value.M34 - value.M32;
            planes[5].D = -value.M44 - value.M42;
            planes[0].Normal.X = -value.M13;
            planes[0].Normal.Y = -value.M23;
            planes[0].Normal.Z = -value.M33;
            planes[0].D = -value.M43;
            planes[1].Normal.X = -value.M14 + value.M13;
            planes[1].Normal.Y = -value.M24 + value.M23;
            planes[1].Normal.Z = -value.M34 + value.M33;
            planes[1].D = -value.M44 + value.M43;
            for (var i = 0; i < 6; i++)
            {
                var num2 = planes[i].Normal.Length();
                planes[i].Normal = (planes[i].Normal / num2);
                planes[i].D /= num2;
            }
            var ray = ComputeIntersectionLine(ref planes[0], ref planes[2]);
            cornerArray[0] = ComputeIntersection(ref planes[4], ref ray);
            cornerArray[3] = ComputeIntersection(ref planes[5], ref ray);
            ray = ComputeIntersectionLine(ref planes[3], ref planes[0]);
            cornerArray[1] = ComputeIntersection(ref planes[4], ref ray);
            cornerArray[2] = ComputeIntersection(ref planes[5], ref ray);
            ray = ComputeIntersectionLine(ref planes[2], ref planes[1]);
            cornerArray[4] = ComputeIntersection(ref planes[4], ref ray);
            cornerArray[7] = ComputeIntersection(ref planes[5], ref ray);
            ray = ComputeIntersectionLine(ref planes[1], ref planes[3]);
            cornerArray[5] = ComputeIntersection(ref planes[4], ref ray);
            cornerArray[6] = ComputeIntersection(ref planes[5], ref ray);
        }

        internal void SupportMapping(ref Vector3 v, out Vector3 result)
        {
            float num3;
            var index = 0;
            Vector3.Dot(ref cornerArray[0], ref v, out num3);
            for (var i = 1; i < cornerArray.Length; i++)
            {
                float num2;
                Vector3.Dot(ref cornerArray[i], ref v, out num2);
                if (num2 > num3)
                {
                    index = i;
                    num3 = num2;
                }
            }
            result = cornerArray[index];
        }

        /// <summary>Returns a String that represents the current BoundingFrustum.</summary>
        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "{{Near:{0} Far:{1} Left:{2} Right:{3} Top:{4} Bottom:{5}}}",
                new object[]
                { Near.ToString(), Far.ToString(), Left.ToString(), Right.ToString(), Top.ToString(), Bottom.ToString() });
        }

        #region IEquatable<BoundingFrustum> Members

        /// <summary>Determines whether the specified BoundingFrustum is equal to the current BoundingFrustum.</summary>
        /// <param name="other">The BoundingFrustum to compare with the current BoundingFrustum.</param>
        public bool Equals(BoundingFrustum other)
        {
            if (other == null)
                return false;
            return (matrix == other.matrix);
        }

        #endregion

        /// <summary>Determines whether two instances of BoundingFrustum are equal.</summary>
        /// <param name="a">The BoundingFrustum to the left of the equality operator.</param>
        /// <param name="b">The BoundingFrustum to the right of the equality operator.</param>
        public static bool operator ==(BoundingFrustum a, BoundingFrustum b)
        {
            return Equals(a, b);
        }

        /// <summary>Determines whether two instances of BoundingFrustum are not equal.</summary>
        /// <param name="a">The BoundingFrustum to the left of the inequality operator.</param>
        /// <param name="b">The BoundingFrustum to the right of the inequality operator.</param>
        public static bool operator !=(BoundingFrustum a, BoundingFrustum b)
        {
            return !Equals(a, b);
        }
    }
}