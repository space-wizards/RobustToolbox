using System;

namespace Robust.Shared.Maths
{
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

    [Flags]
    public enum DirectionFlag : sbyte
    {
        None = 0,
        South = 1 << 0,
        East = 1 << 1,
        North = 1 << 2,
        West = 1 << 3,

        SouthEast = South | East,
        NorthEast = North | East,
        NorthWest = North | West,
        SouthWest = South | West,
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

        private static readonly Vector2[] DirectionVectors = {
            new (0, -1),
            new Vector2(1, -1).Normalized,
            new (1, 0),
            new Vector2(1, 1).Normalized,
            new (0, 1),
            new Vector2(-1, 1).Normalized,
            new (-1, 0),
            new Vector2(-1, -1).Normalized
        };

        private static readonly Vector2i[] IntDirectionVectors = {
            new (0, -1),
            new (1, -1),
            new (1, 0),
            new (1, 1),
            new (0, 1),
            new (-1, 1),
            new (-1, 0),
            new (-1, -1)
        };

        /// <summary>
        /// Converts a Direction to a normalized Direction vector.
        /// </summary>
        /// <param name="dir"></param>
        /// <returns>a normalized 2D Vector</returns>
        /// <exception cref="IndexOutOfRangeException">if invalid Direction is used</exception>
        /// <seealso cref="Vector2"/>
        public static Vector2 ToVec(this Direction dir)
        {
            return DirectionVectors[(int) dir];
        }

        /// <summary>
        /// Converts a Direction to a Vector2i. Useful for getting adjacent tiles.
        /// </summary>
        /// <param name="dir">Direction</param>
        /// <returns>an 2D int Vector</returns>
        /// <exception cref="IndexOutOfRangeException">if invalid Direction is used</exception>
        /// <seealso cref="Vector2i"/>
        public static Vector2i ToIntVec(this Direction dir)
        {
            return IntDirectionVectors[(int) dir];
        }

        /// <summary>
        ///     Offset 2D integer vector by a given direction.
        ///     Convenience for adding <see cref="ToIntVec"/> to <see cref="Vector2i"/>
        /// </summary>
        /// <param name="vec">2D integer vector</param>
        /// <param name="dir">Direction by which we offset</param>
        /// <returns>a newly vector offset by the <param name="dir">dir</param> or exception if the direction is invalid</returns>
        public static Vector2i Offset(this Vector2i vec, Direction dir)
        {
            return vec + dir.ToIntVec();
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
