using SixLabors.ImageSharp.PixelFormats;
using SS14.Shared.Maths;

namespace SS14.Client.Utility
{
    public static class ImageSharpExt
    {
        public static Color ConvertImgSharp(this Rgba32 color)
        {
            return new Color(color.R, color.G, color.B, color.A);
        }

        public static Rgba32 ConvertImgSharp(this Color color)
        {
            return new Rgba32(color.R, color.G, color.B, color.A);
        }
    }
}
