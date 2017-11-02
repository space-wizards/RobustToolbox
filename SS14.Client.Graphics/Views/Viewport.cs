using SS14.Shared.Maths;

namespace SS14.Client.Graphics.Views
{
    public class Viewport
    {
        public Viewport(int originX, int originY, uint width, uint height)
        {
            // TODO: Complete member initialization
            this.OriginX = originX;
            this.OriginY = originY;
            this.Width = width;
            this.Height = height;
        }
        public uint Width { get; set; }
        public uint Height { get; set; }
        public int OriginX { get; set; }
        public int OriginY { get; set; }

        public Vector2u Size => new Vector2u(Width, Height);
    }
}
