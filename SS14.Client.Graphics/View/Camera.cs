using SFML.System;
using SFML.Graphics;
using SS14.Shared.Maths;
using SS14.Client.Graphics.Render;
using SS14.Client.Graphics.Utility;

namespace SS14.Client.Graphics.View
{
    public class Camera
    {
        public readonly CluwneWindow Window;

        public Camera(CluwneWindow window)
        {
            Window = window;
            window.Resized += WindowResized;
        }

        public int PixelsPerMeter { get; } = 32;
        public Vector2 Position { get; set; }

        private void WindowResized(object sender, SizeEventArgs args)
        {
            Window.SFMLTarget.SetView(new SFML.Graphics.View(
                new Vector2f(args.Width / 2, args.Height / 2),
                new Vector2f(args.Width, args.Height)
            ));
        }
    }
}
