using System.Collections.Generic;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics
{
    public static class Vertices
    {
        public static void ForceCounterClockwise(this List<Vector2> vertices)
        {
            if (vertices.Count < 3) return;

            if (!vertices.IsCounterClockwise()) vertices.Reverse();
        }

        public static bool IsCounterClockwise(this List<Vector2> vertices)
        {
            if (vertices.Count < 3) return false;
            return vertices.GetSignedArea() > 0.0f;
        }

        /// <summary>
        /// Gets the signed area.
        /// If the area is less than 0, it indicates that the polygon is clockwise winded.
        /// </summary>
        /// <returns>The signed area</returns>
        public static float GetSignedArea(this List<Vector2> vertices)
        {
            var count = vertices.Count;

            //The simplest polygon which can exist in the Euclidean plane has 3 sides.
            if (count < 3)
                return 0;

            int i;
            float area = 0;

            for (i = 0; i < count; i++)
            {
                int j = (i + 1) % count;

                Vector2 vi = vertices[i];
                Vector2 vj = vertices[j];

                area += vi.X * vj.Y;
                area -= vi.Y * vj.X;
            }
            area /= 2.0f;
            return area;
        }
    }
}
