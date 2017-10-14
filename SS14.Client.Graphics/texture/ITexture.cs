using SS14.Client.Graphics.Sprites;
using SS14.Shared.Maths;

namespace SS14.Client.Graphics.Textures
{
    public interface ITexture
    {
        Vector2u Size { get; }
        ISprite MakeSprite();
    }
}
