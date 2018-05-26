using System;
using SS14.Client.Graphics;
using SS14.Shared.Maths;
using RSIDirection = SS14.Client.Graphics.RSI.State.Direction;

namespace SS14.Client.Utility
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
            }

            throw new ArgumentException($"Unknown RSI dir: {dir}.", nameof(dir));
        }

        public static RSIDirection Convert(this Direction dir)
        {
            switch (dir)
            {
                case Direction.North:
                case Direction.NorthEast:
                case Direction.NorthWest:
                    return RSIDirection.North;

                case Direction.South:
                case Direction.SouthEast:
                case Direction.SouthWest:
                    return RSIDirection.South;

                case Direction.East:
                    return RSIDirection.East;

                case Direction.West:
                    return RSIDirection.West;
            }

            throw new ArgumentException($"Unknown dir: {dir}.", nameof(dir));
        }
    }
}
