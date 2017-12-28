using SS14.Client.Graphics;
using SS14.Shared.Maths;

namespace SS14.Client.Interfaces.GameObjects
{
    public interface ISpriteRenderableComponent : IRenderableComponent
    {
        TextureSource CurrentSprite { get; }
        Color Color { get; set; }
    }
}
