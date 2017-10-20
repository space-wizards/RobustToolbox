using SS14.Shared.Maths;
using System;

namespace SS14.Client.Graphics.Input
{
    public class MouseMoveEventArgs : EventArgs
    {
        public Vector2i NewPosition { get; }
        public int X => NewPosition.X;
        public int Y => NewPosition.Y;

        public MouseMoveEventArgs(Vector2i newPosition)
        {
            NewPosition = newPosition;
        }

        public static explicit operator MouseMoveEventArgs(SFML.Window.MouseMoveEventArgs args)
        {
            return new MouseMoveEventArgs(new Vector2i(args.X, args.Y));
        }
    }
}
