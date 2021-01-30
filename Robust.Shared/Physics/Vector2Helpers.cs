using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics
{
    internal static class Vector2Helpers
    {
        public static Vector2[] Inverse(this Vector2[] matrix)
        {
            DebugTools.Assert(matrix.Length == 2);
            float a = matrix[0].X, b = matrix[1].X, c = matrix[0].Y, d = matrix[1].Y;
            float det = a * d - b * c;
            if (det != 0.0f)
            {
                det = 1.0f / det;
            }

            Vector2[] result = new Vector2[2];
            result[0].X = det * d;
            result[0].Y = -det * c;

            result[1].X = -det * b;
            result[1].Y = det * a;

            return result;
        }
    }
}
