using SS14.Shared.Maths;
using SS14.Client.Graphics.Sprites;

namespace SS14.Client.Interfaces.GameObjects
{
    public interface ISpriteRenderableComponent : IRenderableComponent
    {
        Sprite GetCurrentSprite();
        Color Color { get; set; }
    }
}
