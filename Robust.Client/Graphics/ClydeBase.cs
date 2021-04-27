using Robust.Shared.Maths;

namespace Robust.Client.Graphics
{
    public enum WindowMode : byte
    {
        Windowed = 0,
        Fullscreen = 1,
        // Maybe add borderless? Not sure how good Godot's default fullscreen is with alt tabbing.
    }

    // Remember when this was called DisplayManager?
    internal static class ClydeBase
    {
        internal static Vector2i ClampSubRegion(Vector2i size, UIBox2i? subRegionSpecified)
        {
            return subRegionSpecified == null
                ? size
                : UIBox2i.FromDimensions(Vector2i.Zero, size).Intersection(subRegionSpecified.Value)?.Size ?? default;
        }
    }
}
