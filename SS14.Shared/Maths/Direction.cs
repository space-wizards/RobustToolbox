using System;

namespace SS14.Shared.Maths
{
    public enum Direction
    {
        East = 0,
        NorthEast = 1,
        North = 2,
        NorthWest = 3,
        West = 4,
        SouthWest = 5,
        South = 6,
        SouthEast = 7
    }

    /// <summary>
    /// Extension methods for Direction enum.
    /// </summary>
    public static class DirectionExtensions
    {
        private const double Segment = 2 * Math.PI / 8.0; // Cut the circle into 8 pieces
        private const double Offset = Segment / 2.0; // offset the pieces by 1/2 their size

        /// <summary>
        /// Converts a direction vector to the closest Direction enum.
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        public static Direction GetDir(this Vector2 vec)
        {
            return vec.ToAngle().GetDir();
        }

        /// <summary>
        /// Converts a direction to an angle, where angle is -PI to +PI.
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static Angle ToAngle(this Direction dir)
        {
            var ang = Segment * (int)dir;

            if (ang > Math.PI) // convert 0 > 2PI to -PI > +PI
                ang -= 2 * Math.PI;

            return ang;
        }

        /// <summary>
        /// Converts a Direction to a normalized Direction vector.
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static Vector2 ToVec(this Direction dir)
        {
            return dir.ToAngle().ToVec();
        }

        /// <summary>
        /// Converts a direction vector to an angle, where angle is -PI to +PI.
        /// </summary>
        /// <param name="vec">Vector to get the angle from.</param>
        /// <returns>Angle of the vector.</returns>
        public static Angle ToAngle(this Vector2 vec)
        {
            return Math.Atan2(vec.Y, vec.X);
        }
    }
}
