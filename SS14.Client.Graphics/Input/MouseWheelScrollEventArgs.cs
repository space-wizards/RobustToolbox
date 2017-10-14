using SS14.Shared.Maths;
using System;

namespace SS14.Client.Graphics.Input
{
    public class MouseWheelScrollEventArgs : EventArgs
    {
        public Vector2i Position { get; }
        public int X => Position.X;
        public int Y => Position.Y;
        public Mouse.Wheel Wheel { get; }
        public float Delta { get; }

        public MouseWheelScrollEventArgs(Vector2i position, Mouse.Wheel wheel, float delta)
        {
            Position = position;
            Wheel = wheel;
            Delta = delta;
        }

        public static explicit operator MouseWheelScrollEventArgs(SFML.Window.MouseWheelScrollEventArgs args)
        {
            return new MouseWheelScrollEventArgs(new Vector2i(args.X, args.Y), args.Wheel.Convert(), args.Delta);
        }
    }
}
