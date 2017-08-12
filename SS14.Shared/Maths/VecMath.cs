using System;
using OpenTK;

namespace SS14.Shared.Maths
{
    /// <summary>
    /// Vector helper functions for the OpenTK math library.
    /// </summary>
    public static class VecMath
    {
        /// <summary>
        /// Converts an angle in radians to a unit direction vector.
        /// </summary>
        /// <param name="rads">angle in radians</param>
        /// <returns>Unit Direction Vector</returns>
        public static Vector2 FromAngle(double rads)
        {
            var x = Math.Cos(rads);
            var y = Math.Sin(rads);
            return new Vector2((float)x, (float)y);
        }
    }
}
