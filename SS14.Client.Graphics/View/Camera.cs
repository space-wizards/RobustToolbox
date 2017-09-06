using SFML.Graphics;
using SS14.Shared.Maths;

namespace SS14.Client.Graphics.View
{
    public class Camera
    {
        private Viewport _view;
        private readonly RenderWindow _viewport;

        public Camera(Viewport viewport) { }

        public Camera(RenderWindow viewport)
        {
            _viewport = viewport;
        }

        public int PixelsPerMeter { get; } = 32;
        public Vector2 Position { get; set; }

        public void SetView(SFML.Graphics.View view)
        {
            _viewport.SetView(view);
        }
    }
}
