using System;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;

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
        /// <summary>
        /// A list of all cardinal and diagonal <see cref="Direction"/>s.
        /// </summary>
        public static readonly ImmutableArray<Direction> AllDirections =
        [
            Direction.South,
            Direction.SouthEast,
            Direction.East,
            Direction.NorthEast,
            Direction.North,
            Direction.NorthWest,
            Direction.West,
            Direction.SouthWest
        ];

        private const double Segment = 2 * Math.PI / 8.0; // Cut the circle into 8 pieces

        // 1f / MathF.Sqrt(2) except we can't const that.
        private const float DiagonalComponent = 0.7071067811865476f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Direction AsDir(this DirectionFlag directionFlag)
        {
            return directionFlag switch
            {
                DirectionFlag.South => Direction.South,
                DirectionFlag.SouthEast => Direction.SouthEast,
                DirectionFlag.East => Direction.East,
                DirectionFlag.NorthEast => Direction.NorthEast,
                DirectionFlag.North => Direction.North,
                DirectionFlag.NorthWest => Direction.NorthWest,
                DirectionFlag.West => Direction.West,
                DirectionFlag.SouthWest => Direction.SouthWest,
                _ => throw new ArgumentOutOfRangeException(nameof(directionFlag), directionFlag, null)
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DirectionFlag AsFlag(this Direction direction)
        {
            return direction switch
            {
                Direction.South => DirectionFlag.South,
                Direction.SouthEast => DirectionFlag.SouthEast,
                Direction.East => DirectionFlag.East,
                Direction.NorthEast => DirectionFlag.NorthEast,
                Direction.North => DirectionFlag.North,
                Direction.NorthWest => DirectionFlag.NorthWest,
                Direction.West => DirectionFlag.West,
                Direction.SouthWest => DirectionFlag.SouthWest,
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DirectionFlag AsDirectionFlag(this Vector2i indices)
        {
            return (indices.X, indices.Y) switch
            {
                (-1, -1) => DirectionFlag.SouthWest,
                (-1, 0) => DirectionFlag.West,
                (-1, 1) => DirectionFlag.NorthWest,
                (0, -1) => DirectionFlag.South,
                (0, 1) => DirectionFlag.North,
                (1, -1) => DirectionFlag.SouthEast,
                (1, 0) => DirectionFlag.East,
                (1, 1) => DirectionFlag.NorthEast,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(indices),
                    indices,
                    "Tried to use a non-supported Vector2i for conversion to direction flag")
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Direction AsDirection(this Vector2i indices)
        {
            return (indices.X, indices.Y) switch
            {
                (-1, -1) => Direction.SouthWest,
                (-1, 0) => Direction.West,
                (-1, 1) => Direction.NorthWest,
                (0, -1) => Direction.South,
                (0, 1) => Direction.North,
                (1, -1) => Direction.SouthEast,
                (1, 0) => Direction.East,
                (1, 1) => Direction.NorthEast,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(indices),
                    indices,
                    "Tried to use a non-supported Vector2i for conversion to direction")
            };
        }


        /// <summary>
        /// Converts a direction vector to the closest Direction enum.
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Direction GetDir(this Vector2 vec)
        {
            return Angle.FromWorldVec(vec).GetDir();
        }

        /// <summary>
        /// Converts a direction vector to the closest Direction enum.
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Direction GetDir(this Vector2i vec)
        {
            return new Angle(vec).GetDir();
        }

        /// <summary>
        /// Converts a direction vector to the closest cardinal Direction enum.
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Direction GetCardinalDir(this Vector2i vec)
        {
            return new Angle(vec).GetCardinalDir();
        }

        /// <param name="direction"></param>
        extension(Direction direction)
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Direction GetOpposite()
            {
                return (Direction) (((int) direction + 4) & 7);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Direction GetClockwise90Degrees()
            {
                return (Direction) (((int) direction + 6) & 7);
            }

            /// <summary>
            /// Converts a direction to an angle, where angle is -PI to +PI.
            /// </summary>
            /// <returns></returns>
            [Pure]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Angle ToAngle()
            {
                var ang = Segment * (int) direction;

                if (ang > Math.PI) // convert 0 > 2PI to -PI > +PI
                    ang -= 2 * Math.PI;

                return ang;
            }

            /// <summary>
            /// Converts a Direction to a normalized Direction vector.
            /// </summary>
            /// <returns>a normalized 2D Vector</returns>
            /// <exception cref="ArgumentOutOfRangeException">if invalid Direction is used</exception>
            /// <seealso cref="Vector2"/>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector2 ToVec()
            {
                return direction switch
                {
                    Direction.South => new Vector2(0, -1),
                    Direction.SouthEast => new Vector2(DiagonalComponent, -DiagonalComponent),
                    Direction.East => new Vector2(1, 0),
                    Direction.NorthEast => new Vector2(DiagonalComponent, DiagonalComponent),
                    Direction.North => new Vector2(0, 1),
                    Direction.NorthWest => new Vector2(-DiagonalComponent, DiagonalComponent),
                    Direction.West => new Vector2(-1, 0),
                    Direction.SouthWest => new Vector2(-DiagonalComponent, -DiagonalComponent),
                    _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
                };
            }

            /// <summary>
            /// Converts a Direction to a Vector2i. Useful for getting adjacent tiles.
            /// </summary>
            /// <returns>an 2D int Vector</returns>
            /// <exception cref="ArgumentOutOfRangeException">if invalid Direction is used</exception>
            /// <seealso cref="Vector2i"/>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector2i ToIntVec()
            {
                return direction switch
                {
                    Direction.South => new Vector2i(0, -1),
                    Direction.SouthEast => new Vector2i(1, -1),
                    Direction.East => new Vector2i(1, 0),
                    Direction.NorthEast => new Vector2i(1, 1),
                    Direction.North => new Vector2i(0, 1),
                    Direction.NorthWest => new Vector2i(-1, 1),
                    Direction.West => new Vector2i(-1, 0),
                    Direction.SouthWest => new Vector2i(-1, -1),
                    _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
                };
            }
        }

        /// <summary>
        ///     Offset 2D integer vector by a given direction.
        ///     Convenience for adding <see cref="ToIntVec"/> to <see cref="Vector2i"/>
        /// </summary>
        /// <param name="vec">2D integer vector</param>
        /// <param name="dir">Direction by which we offset</param>
        /// <returns>a newly vector offset by the <param name="dir">dir</param> or exception if the direction is invalid</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2i Offset(this Vector2i vec, Direction dir)
        {
            return vec + dir.ToIntVec();
        }

        /// <summary>
        /// Converts a direction vector to an angle, where angle is -PI to +PI.
        /// </summary>
        /// <param name="vec">Vector to get the angle from.</param>
        /// <returns>Angle of the vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Angle ToAngle(this Vector2 vec)
        {
            return new(vec);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Angle ToWorldAngle(this Vector2 vec)
        {
            return Angle.FromWorldVec(vec);
        }
    }
}
