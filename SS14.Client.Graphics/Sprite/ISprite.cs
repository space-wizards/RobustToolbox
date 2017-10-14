using SS14.Client.Graphics.Textures;
using SS14.Shared.Maths;

namespace SS14.Client.Graphics.Sprites
{
    public interface ISprite
    {
        ITexture Texture { get; }
        Vector2i Size { get; }
        Box2i TexturRect { get; }
    }
}
