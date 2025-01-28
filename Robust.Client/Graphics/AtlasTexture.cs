using JetBrains.Annotations;
using Robust.Shared.Graphics;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.Graphics
{
    /// <summary>
    ///     Represents a sub region of another texture.
    ///     This can be a useful optimization in many cases.
    /// </summary>
    [PublicAPI]
    public sealed class AtlasTexture : Texture
    {
        public AtlasTexture(Texture texture, UIBox2 subRegion, int? width = null) : base((Vector2i) subRegion.Size)
        {
            DebugTools.Assert(SubRegion.Right < texture.Width);
            DebugTools.Assert(SubRegion.Bottom < texture.Height);
            DebugTools.Assert(SubRegion.Left >= 0);
            DebugTools.Assert(SubRegion.Top >= 0);

            SubRegion = subRegion;
            RegionWidth = width ?? (int)subRegion.Width;
            SourceTexture = texture;
        }

        /// <summary>
        ///     The texture this texture is a sub region of.
        /// </summary>
        public Texture SourceTexture { get; }

        /// <summary>
        ///     Our sub region within our source, in pixel coordinates.
        /// </summary>
        public UIBox2 SubRegion { get; }

        /// <summary>
        ///     The width of the texture we came from, used for offsetting to get normals
        /// </summary>
        public int RegionWidth { get; }

        public override Color GetPixel(int x, int y)
        {
            DebugTools.Assert(x < SubRegion.Right);
            DebugTools.Assert(y < SubRegion.Top);
            int xTranslated = x + (int) SubRegion.Left;
            int yTranslated = y + (int) SubRegion.Top;
            return this.SourceTexture[xTranslated, yTranslated];
        }
    }
}
