namespace SS14.Client.Graphics.View
{
    public class Viewport
    {
        public Viewport(int originX, int originY, uint width, uint height)
        {
            // TODO: Complete member initialization
            this.OriginX = originX;
            this.OriginY = originY;
            this.Width = (int)width;
            this.Height = (int)height;
        }
        public int Width { get; set; }
        public int Height { get; set; }
        public int OriginX { get; set; }
        public int OriginY { get; set; }

    }
}
