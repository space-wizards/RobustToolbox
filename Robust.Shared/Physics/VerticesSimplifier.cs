/* Original source Farseer Physics Engine:
 * Copyright (c) 2014 Ian Qvist, http://farseerphysics.codeplex.com
 * Microsoft Permissive License (Ms-PL) v1.1
 */

using System;
using System.Collections.Generic;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics
{
    /// <summary>
    /// Takes in a list of vertices and removes any that are redundant (within tolerance).
    /// </summary>
    public interface IVerticesSimplifier
    {
        List<Vector2> Simplify(List<Vector2> vertices, float tolerance);
    }

    /// <inheritdoc />
    public sealed class CollinearSimplifier : IVerticesSimplifier
    {
        /// <summary>
        /// Removes all collinear points on the polygon.
        /// </summary>
        public List<Vector2> Simplify(List<Vector2> vertices, float tolerance = 0)
        {
            if (vertices.Count <= 3)
                return vertices;

            var simplified = new List<Vector2>(vertices.Count);

            for (var i = 0; i < vertices.Count; i++)
            {
                // No wraparound for negative sooooo
                var prev = vertices[i == 0 ? vertices.Count - 1 : i - 1];
                var current = vertices[i];
                var next = vertices[(i + 1) % vertices.Count];

                // If they collinear, continue
                if (IsCollinear(in prev, in current, in next, tolerance))
                    continue;

                simplified.Add(current);
            }

            // Farseer didn't seem to handle straight lines and nuked all points
            if (simplified.Count == 0)
            {
                simplified.Add(vertices[0]);
                simplified.Add(vertices[^1]);
            }

            return simplified;
        }

        private bool IsCollinear(in Vector2 prev, in Vector2 current, in Vector2 next, float tolerance)
        {
            return FloatInRange(Area(in prev, in current, in next), -tolerance, tolerance);
        }

        private float Area(in Vector2 a, in Vector2 b, in Vector2 c)
        {
            return a.X * (b.Y - c.Y) + b.X * (c.Y - a.Y) + c.X * (a.Y - b.Y);
        }

        private bool FloatInRange(float value, float min, float max)
        {
            return (value >= min && value <= max);
        }
    }

    /// <inheritdoc />
    public sealed class RamerDouglasPeuckerSimplifier : IVerticesSimplifier
    {
        /// <summary>
        /// Ramer-Douglas-Peucker polygon simplification algorithm. This is the general recursive version that does not use the
        /// speed-up technique by using the Melkman convex hull.
        ///
        /// If you pass in 0, it will remove all collinear points.
        /// </summary>
        /// <returns>The simplified polygon</returns>
        public List<Vector2> Simplify(List<Vector2> vertices, float distanceTolerance)
        {
            if (vertices.Count <= 3)
                return vertices;

            Span<bool> usePoint = stackalloc bool[vertices.Count];

            for (var i = 0; i < vertices.Count; i++)
                usePoint[i] = true;

            SimplifySection(vertices, 0, vertices.Count - 1, usePoint, distanceTolerance);

            var simplified = new List<Vector2>(vertices.Count);

            for (var i = 0; i < vertices.Count; i++)
            {
                if (usePoint[i])
                    simplified.Add(vertices[i]);
            }

            return simplified;
        }

        private static void SimplifySection(List<Vector2> vertices, int i, int j, Span<bool> usePoint, float distanceTolerance)
        {
            if (i + 1 == j)
                return;

            var a = vertices[i];
            var b = vertices[j];

            double maxDistance = -1.0;
            int maxIndex = i;
            for (int k = i + 1; k < j; k++)
            {
                Vector2 point = vertices[k];

                double distance = DistanceBetweenPointAndLineSegment(in point, in a, in b);

                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    maxIndex = k;
                }
            }

            if (maxDistance <= distanceTolerance)
            {
                for (int k = i + 1; k < j; k++)
                {
                    usePoint[k] = false;
                }
            }
            else
            {
                SimplifySection(vertices, i, maxIndex, usePoint, distanceTolerance);
                SimplifySection(vertices, maxIndex, j, usePoint, distanceTolerance);
            }
        }

        public static float DistanceBetweenPointAndLineSegment(in Vector2 point, in Vector2 start, in Vector2 end)
        {
            if (start == end)
                return (point - start).Length;

            var v = end - start;
            var w = point - start;

            var c1 = Vector2.Dot(w, v);
            if (c1 <= 0) return (point - start).Length;

            var c2 = Vector2.Dot(v, v);
            if (c2 <= c1) return (point - end).Length;

            var b = c1 / c2;
            var pointOnLine = start + v * b;
            return (point - pointOnLine).Length;
        }
    }
}
