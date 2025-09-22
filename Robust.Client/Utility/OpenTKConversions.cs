using Robust.Shared.Maths;

namespace Robust.Client.Utility
{
    internal static class OpenTKConversions
    {
        public static OpenToolkit.Mathematics.Color4 ConvertOpenTK(this Color color)
        {
            return new(color.R, color.G, color.B, color.A);
        }
    }
}
