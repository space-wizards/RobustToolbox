using System;

namespace Robust.Shared.Maths
{
    [Flags]
    public enum Direction : sbyte
    {
        Invalid = -1,
        South = 0,
        SouthEast = 1,
        East = 2,
        NorthEast = 3,
        North = 4,
        NorthWest = 5,
        West = 6,
        SouthWest = 7,
    }

    /// <summary>
    /// Extension methods for Direction enum.
    /// </summary>
    public static class DirectionExtensions
    {
        private const double Segment = 2 * Math.PI / 8.0; // Cut the circle into 8 pieces

        /// <summary>
        /// Converts a direction vector to the closest Direction enum.
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        public static Direction GetDir(this Vector2 vec)
        {
            return Angle.FromWorldVec(vec).GetDir();
        }

        /// <summary>
        /// Converts a direction vector to the closest Direction enum.
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        public static Direction GetDir(this Vector2i vec)
        {
            return new Angle(vec).GetDir();
        }

        /// <summary>
        /// Converts a direction vector to the closest cardinal Direction enum.
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        public static Direction GetCardinalDir(this Vector2i vec)
        {
            return new Angle(vec).GetCardinalDir();
        }

        public static Direction GetOpposite(this Direction direction)
        {
            return direction switch
            {
                Direction.East => Direction.West,
                Direction.West => Direction.East,
                Direction.North => Direction.South,
                Direction.South => Direction.North,
                Direction.NorthEast => Direction.SouthWest,
                Direction.SouthWest => Direction.NorthEast,
                Direction.NorthWest => Direction.SouthEast,
                Direction.SouthEast => Direction.NorthWest,
                _ => throw new ArgumentOutOfRangeException(nameof(direction))
            };
        }

        /// <summary>
        /// Converts a direction to an angle, where angle is -PI to +PI.
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static Angle ToAngle(this Direction dir)
        {
            var ang = Segment * (int) dir;

            if (ang > Math.PI) // convert 0 > 2PI to -PI > +PI
                ang -= 2 * Math.PI;

            return ang;
        }

        private static Vector2[] directionVectors = new[]
        {
            new Vector2(0, -1),
            new Vector2(1, -1).Normalized,
            new Vector2(1, 0),
            new Vector2(1, 1).Normalized,
            new Vector2(0, 1),
            new Vector2(-1, 1).Normalized,
            new Vector2(-1, 0),
            new Vector2(-1, -1).Normalized
        };
        /// <summary>
        /// Converts a Direction to a normalized Direction vector.
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static Vector2 ToVec(this Direction dir)
        {
            return directionVectors[(int) dir];
        }

        /// <summary>
        /// Converts a direction vector to an angle, where angle is -PI to +PI.
        /// </summary>
        /// <param name="vec">Vector to get the angle from.</param>
        /// <returns>Angle of the vector.</returns>
        public static Angle ToAngle(this Vector2 vec)
        {
            return new(vec);
        }

        public static Angle ToWorldAngle(this Vector2 vec)
        {
            return Angle.FromWorldVec(vec);
        }
    }
}
