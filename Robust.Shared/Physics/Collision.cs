using System;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics.Shapes
{
    /// <summary>
    ///     Ray-cast output data.
    /// </summary>
    public struct RayCastOutput
    {
        /// <summary>
        /// The ray hits at p1 + fraction * (p2 - p1), where p1 and p2 come from RayCastInput.
        /// Contains the actual fraction of the ray where it has the intersection point.
        /// </summary>
        public float Fraction;

        /// <summary>
        ///     The normal of the face of the shape the ray has hit.
        /// </summary>
        public Vector2 Normal;
    }

    /// <summary>
    ///     Ray-cast input data.
    /// </summary>
    public struct RayCastInput
    {
        /// <summary>
        /// The ray extends from p1 to p1 + maxFraction * (p2 - p1).
        /// If you supply a max fraction of 1, the ray extends from p1 to p2.
        /// A max fraction of 0.5 makes the ray go from p1 and half way to p2.
        /// </summary>
        public float MaxFraction;

        /// <summary>
        /// The starting point of the ray.
        /// </summary>
        public Vector2 Point1;

        /// <summary>
        /// The ending point of the ray.
        /// </summary>
        public Vector2 Point2;
    }

    public struct AABB
    {
        // TODO: Use FieldOffsets

        /// <summary>
        /// The lower vertex
        /// </summary>
        public Vector2 LowerBound;

        /// <summary>
        /// The upper vertex
        /// </summary>
        public Vector2 UpperBound;

        public AABB(Vector2 min, Vector2 max)
            : this(ref min, ref max)
        {
        }

        public AABB(ref Vector2 min, ref Vector2 max)
        {
            LowerBound = min;
            UpperBound = max;
        }

        public AABB(Vector2 center, float width, float height)
        {
            LowerBound = center - new Vector2(width / 2, height / 2);
            UpperBound = center + new Vector2(width / 2, height / 2);
        }

        public float Width => UpperBound.X - LowerBound.X;

        public float Height => UpperBound.Y - LowerBound.Y;

        /// <summary>
        /// Get the center of the AABB.
        /// </summary>
        public Vector2 Center => (LowerBound + UpperBound) * 0.5f;

        /// <summary>
        /// Get the extents of the AABB (half-widths).
        /// </summary>
        public Vector2 Extents => (UpperBound - LowerBound) * 0.5f;

        /// <summary>
        /// Get the perimeter length
        /// </summary>
        public float Perimeter
        {
            get
            {
                float wx = UpperBound.X - LowerBound.X;
                float wy = UpperBound.Y - LowerBound.Y;
                return 2.0f * (wx + wy);
            }
        }

        /// <summary>
        /// Gets the vertices of the AABB.
        /// </summary>
        /// <value>The corners of the AABB</value>
        public Vertices Vertices
        {
            get
            {
                Vertices vertices = new Vertices(4)
                {
                    UpperBound,
                    new Vector2(UpperBound.X, LowerBound.Y),
                    LowerBound,
                    new Vector2(LowerBound.X, UpperBound.Y)
                };
                return vertices;
            }
        }

        /// <summary>
        ///     First quadrant
        /// </summary>
        public AABB Q1 => new AABB(Center, UpperBound);

        /// <summary>
        ///     Second quadrant
        /// </summary>
        public AABB Q2 => new AABB(new Vector2(LowerBound.X, Center.Y), new Vector2(Center.X, UpperBound.Y));

        /// <summary>
        ///     Third quadrant
        /// </summary>
        public AABB Q3 => new AABB(LowerBound, Center);

        /// <summary>
        ///     Forth quadrant
        /// </summary>
        public AABB Q4 => new AABB(new Vector2(Center.X, LowerBound.Y), new Vector2(UpperBound.X, Center.Y));

        /// <summary>
        ///     Verify that the bounds are sorted. And the bounds are valid numbers (not NaN).
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if this instance is valid; otherwise, <c>false</c>.
        /// </returns>
        public bool IsValid()
        {
            Vector2 d = UpperBound - LowerBound;
            bool valid = d.X >= 0.0f && d.Y >= 0.0f;
            // TODO: Check they're not NANs
            return valid;
        }

        /// <summary>
        /// Combine an AABB into this one.
        /// </summary>
        /// <param name="aabb">The aabb.</param>
        public void Combine(ref AABB aabb)
        {
            LowerBound = Vector2.ComponentMin(LowerBound, aabb.LowerBound);
            UpperBound = Vector2.ComponentMax(UpperBound, aabb.UpperBound);
        }

        /// <summary>
        /// Combine two AABBs into this one.
        /// </summary>
        /// <param name="aabb1">The aabb1.</param>
        /// <param name="aabb2">The aabb2.</param>
        public void Combine(ref AABB aabb1, ref AABB aabb2)
        {
            LowerBound = Vector2.ComponentMin(aabb1.LowerBound, aabb2.LowerBound);
            UpperBound = Vector2.ComponentMax(aabb1.UpperBound, aabb2.UpperBound);
        }

        /// <summary>
        /// Does this aabb contain the provided AABB.
        /// </summary>
        /// <param name="aabb">The aabb.</param>
        /// <returns>
        /// 	<c>true</c> if it contains the specified aabb; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(ref AABB aabb)
        {
            // TODO: This is probably slow as shit make a better one
            bool result = true;
            result = result && LowerBound.X <= aabb.LowerBound.X;
            result = result && LowerBound.Y <= aabb.LowerBound.Y;
            result = result && aabb.UpperBound.X <= UpperBound.X;
            result = result && aabb.UpperBound.Y <= UpperBound.Y;
            return result;
        }

        /// <summary>
        /// Determines whether the AABB contains the specified point.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>
        /// 	<c>true</c> if it contains the specified point; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(ref Vector2 point)
        {
            //using epsilon to try and guard against float rounding errors.
            return point.X > LowerBound.X + float.Epsilon &&
                   point.X < UpperBound.X - float.Epsilon &&
                   point.Y > LowerBound.Y + float.Epsilon &&
                   point.Y < UpperBound.Y - float.Epsilon;
        }

        /// <summary>
        /// Test if the two AABBs overlap.
        /// </summary>
        /// <param name="a">The first AABB.</param>
        /// <param name="b">The second AABB.</param>
        /// <returns>True if they are overlapping.</returns>
        public static bool TestOverlap(ref AABB a, ref AABB b)
        {
            if (b.LowerBound.X > a.UpperBound.X || b.LowerBound.Y > a.UpperBound.Y)
                return false;

            if (a.LowerBound.X > b.UpperBound.X || a.LowerBound.Y > b.UpperBound.Y)
                return false;

            return true;
        }

        /// <summary>
        /// Raycast against this AABB using the specified points and maxfraction (found in input)
        /// </summary>
        /// <param name="output">The results of the raycast.</param>
        /// <param name="input">The parameters for the raycast.</param>
        /// <returns>True if the ray intersects the AABB</returns>
        public bool RayCast(out RayCastOutput output, ref RayCastInput input, bool doInteriorCheck = true)
        {
            // From Real-time Collision Detection, p179.

            output = new RayCastOutput();

            float tmin = float.MinValue;
            float tmax = float.MaxValue;

            Vector2 p = input.Point1;
            Vector2 d = input.Point2 - input.Point1;
            Vector2 absD = Vector2.Abs(d);

            Vector2 normal = Vector2.Zero;

            for (int i = 0; i < 2; ++i)
            {
                float absD_i = i == 0 ? absD.X : absD.Y;
                float lowerBound_i = i == 0 ? LowerBound.X : LowerBound.Y;
                float upperBound_i = i == 0 ? UpperBound.X : UpperBound.Y;
                float p_i = i == 0 ? p.X : p.Y;

                if (absD_i < float.Epsilon)
                {
                    // Parallel.
                    if (p_i < lowerBound_i || upperBound_i < p_i)
                    {
                        return false;
                    }
                }
                else
                {
                    float d_i = i == 0 ? d.X : d.Y;

                    float inv_d = 1.0f / d_i;
                    float t1 = (lowerBound_i - p_i) * inv_d;
                    float t2 = (upperBound_i - p_i) * inv_d;

                    // Sign of the normal vector.
                    float s = -1.0f;

                    if (t1 > t2)
                    {
                        Swap(ref t1, ref t2);
                        s = 1.0f;
                    }

                    // Push the min up
                    if (t1 > tmin)
                    {
                        if (i == 0)
                        {
                            normal.X = s;
                        }
                        else
                        {
                            normal.Y = s;
                        }

                        tmin = t1;
                    }

                    // Pull the max down
                    tmax = Math.Min(tmax, t2);

                    if (tmin > tmax)
                    {
                        return false;
                    }
                }
            }

            // Does the ray start inside the box?
            // Does the ray intersect beyond the max fraction?
            if (doInteriorCheck && (tmin < 0.0f || input.MaxFraction < tmin))
            {
                return false;
            }

            // Intersection.
            output.Fraction = tmin;
            output.Normal = normal;
            return true;
        }

        // TODO: Something better
        public static void Swap<T>(ref T a, ref T b)
        {
            T tmp = a;
            a = b;
            b = tmp;
        }
    }
}
