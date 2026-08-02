using JetBrains.Annotations;
using Robust.Shared.Graphics;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using ClydeTextureImpl = Robust.Client.Graphics.Clyde.Clyde.ClydeTexture;

namespace Robust.Client.Graphics
{
    /// <summary>
    ///     Represents a sub region of another texture.
    ///     This can be a useful optimization in many cases.
    /// </summary>
    [PublicAPI]
    public sealed class AtlasTexture : Texture
    {
        public AtlasTexture(Texture texture, UIBox2 subRegion) : base((Vector2i) subRegion.Size)
        {
            DebugTools.Assert(SubRegion.Right < texture.Width);
            DebugTools.Assert(SubRegion.Bottom < texture.Height);
            DebugTools.Assert(SubRegion.Left >= 0);
            DebugTools.Assert(SubRegion.Top >= 0);

            SubRegion = subRegion;
            ClydeTexture = (ClydeTextureImpl) texture;

            var (width, height) = texture.Size;
            NormalizedSubRegion = new Box2(
                subRegion.Left / width,
                (height - subRegion.Bottom) / height,
                subRegion.Right / width,
                (height - subRegion.Top) / height);
        }

        /// <summary>
        ///     The texture this texture is a sub region of.
        /// </summary>
        public Texture SourceTexture => ClydeTexture;

        /// <summary>
        ///     The Clyde texture backing this atlas texture.
        /// </summary>
        internal ClydeTextureImpl ClydeTexture { get; }

        /// <summary>
        ///     Our sub region within our source, in pixel coordinates.
        /// </summary>
        public UIBox2 SubRegion { get; }

        /// <summary>
        ///     Our sub region within the source texture, normalized for rendering.
        /// </summary>
        internal Box2 NormalizedSubRegion { get; }

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
