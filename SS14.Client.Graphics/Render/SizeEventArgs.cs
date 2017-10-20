using SS14.Shared.Maths;
using System;
using SSizeEventArgs = SFML.Window.SizeEventArgs;

namespace SS14.Client.Graphics.Render
{
    public class SizeEventArgs : EventArgs
    {
        public Vector2u Size { get; }
        public uint Width => Size.X;
        public uint Height => Size.Y;

        internal SizeEventArgs(SSizeEventArgs args)
        {
            Size = new Vector2u(args.Width, args.Height);
        }

        public SizeEventArgs(uint width, uint height)
        {
            Size = new Vector2u(width, height);
        }
    }
}
