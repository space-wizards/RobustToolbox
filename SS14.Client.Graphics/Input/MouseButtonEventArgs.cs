using System;
using SS14.Shared.Maths;

namespace SS14.Client.Graphics.Input
{
    public class MouseButtonEventArgs : EventArgs
    {
        public Mouse.Button Button { get; }
        public Vector2i Position { get; }
        public int X => Position.X;
        public int Y => Position.Y;

        public MouseButtonEventArgs(Mouse.Button button, Vector2i position)
        {
            Button = button;
            Position = position;
        }

        public static explicit operator MouseButtonEventArgs(SFML.Window.MouseButtonEventArgs args)
        {
            return new MouseButtonEventArgs(args.Button.Convert(), new Vector2i(args.X, args.Y));
        }
    }
}
