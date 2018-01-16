using SS14.Shared.Map;
using SS14.Shared.Maths;

namespace SS14.Client.Graphics.Views
{
    public class Camera
    {
        public int PixelsPerMeter { get; } = 32;
        public Vector2 Position { get; set; }
        public MapId CurrentMap { get; set; }
    }
}
