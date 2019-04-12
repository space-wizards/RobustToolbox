using System;
using Robust.Client.Graphics.RSI;
using Robust.Client.Graphics;
using Robust.Shared.Maths;
using RSIDirection = Robust.Client.Graphics.RSI.RSI.State.Direction;

namespace Robust.Client.Utility
{
    public static class DirExt
    {
        public static Direction Convert(this RSI.State.Direction dir)
        {
            switch (dir)
            {
                case RSI.State.Direction.South:
                    return Direction.South;

                case RSI.State.Direction.North:
                    return Direction.North;

                case RSI.State.Direction.East:
                    return Direction.East;

                case RSI.State.Direction.West:
                    return Direction.West;
            }

            throw new ArgumentException($"Unknown RSI dir: {dir}.", nameof(dir));
        }

        public static RSI.State.Direction Convert(this Direction dir)
        {
            switch (dir)
            {
                case Direction.North:
                case Direction.NorthEast:
                case Direction.NorthWest:
                    return RSI.State.Direction.North;

                case Direction.South:
                case Direction.SouthEast:
                case Direction.SouthWest:
                    return RSI.State.Direction.South;

                case Direction.East:
                    return RSI.State.Direction.East;

                case Direction.West:
                    return RSI.State.Direction.West;
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
    }
}
