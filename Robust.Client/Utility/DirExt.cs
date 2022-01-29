using System;
using Robust.Client.Graphics;
using Robust.Shared.Maths;
using static Robust.Client.GameObjects.SpriteComponent;
using RSIDirection = Robust.Client.Graphics.RSI.State.Direction;

namespace Robust.Client.Utility
{
    public static class DirExt
    {
        public static Direction Convert(this RSIDirection dir)
        {
            switch (dir)
            {
                case RSIDirection.South:
                    return Direction.South;

                case RSIDirection.North:
                    return Direction.North;

                case RSIDirection.East:
                    return Direction.East;

                case RSIDirection.West:
                    return Direction.West;

                case RSIDirection.SouthEast:
                    return Direction.SouthEast;

                case RSIDirection.SouthWest:
                    return Direction.SouthWest;

                case RSIDirection.NorthEast:
                    return Direction.NorthEast;

                case RSIDirection.NorthWest:
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
        public static RSIDirection RoundToCardinal(this RSIDirection dir)
        {
            switch (dir)
            {
                case RSIDirection.NorthEast:
                case RSIDirection.NorthWest:
                    return RSIDirection.North;

                case RSIDirection.SouthEast:
                case RSIDirection.SouthWest:
                    return RSIDirection.South;

                default:
                    return dir;
            }
        }

        public static RSIDirection Convert(this Direction dir, RSI.State.DirectionType type)
        {
            // Round down to a four-way direction if appropriate
            if (type != RSI.State.DirectionType.Dir8)
            {
                return dir.Convert(RSI.State.DirectionType.Dir8).RoundToCardinal();
            }

            switch (dir)
            {
                case Direction.North:
                    return RSIDirection.North;

                case Direction.South:
                    return RSIDirection.South;

                case Direction.East:
                    return RSIDirection.East;

                case Direction.West:
                    return RSIDirection.West;

                case Direction.SouthEast:
                    return RSIDirection.SouthEast;

                case Direction.SouthWest:
                    return RSIDirection.SouthWest;

                case Direction.NorthEast:
                    return RSIDirection.NorthEast;

                case Direction.NorthWest:
                    return RSIDirection.NorthWest;
            }

            throw new ArgumentException($"Unknown dir: {dir}.", nameof(dir));
        }

        public static Direction TurnCw(this Direction dir)
        {
            return (Direction)(((int)dir - 1) % 8);
        }

        public static Direction TurnCcw(this Direction dir)
        {
            return (Direction)(((int)dir + 1) % 8);
        }

        public static RSIDirection ToRsiDirection(this Angle angle, RSI.State.DirectionType type)
        {
            return type switch
            {
                RSI.State.DirectionType.Dir1 => RSIDirection.South,
                RSI.State.DirectionType.Dir4 or RSI.State.DirectionType.Dir8 => angle.GetDir().Convert(type),
                _ => throw new ArgumentOutOfRangeException($"Unknown rsi direction type: {type}.", nameof(type))
            };
        }

        public static RSIDirection OffsetRsiDir(this RSIDirection dir, DirectionOffset offset)
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
                        RSIDirection.North => RSIDirection.East,
                        RSIDirection.East => RSIDirection.South,
                        RSIDirection.South => RSIDirection.West,
                        RSIDirection.West => RSIDirection.North,
                        _ => throw new NotImplementedException()
                    };
                case DirectionOffset.CounterClockwise:
                    return dir switch
                    {
                        RSIDirection.North => RSIDirection.West,
                        RSIDirection.East => RSIDirection.North,
                        RSIDirection.South => RSIDirection.East,
                        RSIDirection.West => RSIDirection.South,
                        _ => throw new NotImplementedException()
                    };
                case DirectionOffset.Flip:
                    switch (dir)
                    {
                        case RSIDirection.North:
                            return RSIDirection.South;
                        case RSIDirection.East:
                            return RSIDirection.West;
                        case RSIDirection.South:
                            return RSIDirection.North;
                        case RSIDirection.West:
                            return RSIDirection.East;
                        default:
                            throw new NotImplementedException();
                    }
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
