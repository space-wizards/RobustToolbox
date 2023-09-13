using System;
using Robust.Client.Graphics;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Maths;
using static Robust.Client.GameObjects.SpriteComponent;
using Direction = Robust.Shared.Maths.Direction;

namespace Robust.Client.Utility
{
    public static class DirExt
    {
        public static Direction Convert(this RsiDirection dir)
        {
            switch (dir)
            {
                case RsiDirection.South:
                    return Direction.South;

                case RsiDirection.North:
                    return Direction.North;

                case RsiDirection.East:
                    return Direction.East;

                case RsiDirection.West:
                    return Direction.West;

                case RsiDirection.SouthEast:
                    return Direction.SouthEast;

                case RsiDirection.SouthWest:
                    return Direction.SouthWest;

                case RsiDirection.NorthEast:
                    return Direction.NorthEast;

                case RsiDirection.NorthWest:
                    return Direction.NorthWest;
            }

            throw new ArgumentException($"Unknown RSI dir: {dir}.", nameof(dir));
        }

        /// <summary>
        /// 'Rounds' a diagonal direction down to a cardinal direction
        /// </summary>
        /// <param name="dir">The direction to round</param>
        /// <returns><paramref name="dir"/> if it's a cardinal direction, otherwise either north or
        /// south.</returns>
        public static RsiDirection RoundToCardinal(this RsiDirection dir)
        {
            switch (dir)
            {
                case RsiDirection.NorthEast:
                case RsiDirection.NorthWest:
                    return RsiDirection.North;

                case RsiDirection.SouthEast:
                case RsiDirection.SouthWest:
                    return RsiDirection.South;

                default:
                    return dir;
            }
        }

        public static RsiDirection Convert(this Direction dir, RsiDirectionType type)
        {
            if (type == RsiDirectionType.Dir1)
                return RsiDirection.South;

            switch (dir)
            {
                case Direction.North:
                    return RsiDirection.North;

                case Direction.South:
                    return RsiDirection.South;

                case Direction.East:
                    return RsiDirection.East;

                case Direction.West:
                    return RsiDirection.West;
            }

            var rsiDir = dir switch
            {
                Direction.SouthEast => RsiDirection.SouthEast,
                Direction.SouthWest => RsiDirection.SouthWest,
                Direction.NorthEast => RsiDirection.NorthEast,
                Direction.NorthWest => RsiDirection.NorthWest,
                _ => throw new ArgumentException($"Unknown dir: {dir}.", nameof(dir))
            };

            // Round down to a four-way direction if appropriate.
            if (type == RsiDirectionType.Dir4)
            {
                return RoundToCardinal(rsiDir);
            }

            return rsiDir;
        }

        public static Direction TurnCw(this Direction dir)
        {
            return (Direction)(((int)dir + 7) % 8);
        }

        public static Direction TurnCcw(this Direction dir)
        {
            return (Direction)(((int)dir + 1) % 8);
        }

        public static RsiDirection ToRsiDirection(this Angle angle, RsiDirectionType type)
        {
            return type switch
            {
                RsiDirectionType.Dir1 => RsiDirection.South,
                RsiDirectionType.Dir4 => angle.GetCardinalDir().Convert(type),
                RsiDirectionType.Dir8 => angle.GetDir().Convert(type),
                _ => throw new ArgumentOutOfRangeException($"Unknown rsi direction type: {type}.", nameof(type))
            };
        }

        public static RsiDirection OffsetRsiDir(this RsiDirection dir, DirectionOffset offset)
        {
            // There is probably a better way to do this.
            // Eh.
            //
            // Maybe convert RSI direction to a direction and use the much more elegant solution in `TurnCw()` functions?
            // but that conversion would probably be slower than this, even though its a mess to read.
            switch (offset)
            {
                case DirectionOffset.None:
                    return dir;
                case DirectionOffset.Clockwise:
                    return dir switch
                    {
                        RsiDirection.North => RsiDirection.East,
                        RsiDirection.East => RsiDirection.South,
                        RsiDirection.South => RsiDirection.West,
                        RsiDirection.West => RsiDirection.North,
                        _ => throw new NotImplementedException()
                    };
                case DirectionOffset.CounterClockwise:
                    return dir switch
                    {
                        RsiDirection.North => RsiDirection.West,
                        RsiDirection.East => RsiDirection.North,
                        RsiDirection.South => RsiDirection.East,
                        RsiDirection.West => RsiDirection.South,
                        _ => throw new NotImplementedException()
                    };
                case DirectionOffset.Flip:
                    switch (dir)
                    {
                        case RsiDirection.North:
                            return RsiDirection.South;
                        case RsiDirection.East:
                            return RsiDirection.West;
                        case RsiDirection.South:
                            return RsiDirection.North;
                        case RsiDirection.West:
                            return RsiDirection.East;
                        default:
                            throw new NotImplementedException();
                    }
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
