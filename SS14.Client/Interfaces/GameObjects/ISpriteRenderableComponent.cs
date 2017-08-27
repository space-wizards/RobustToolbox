using OpenTK.Graphics;
using SFML.Graphics;

namespace SS14.Client.Interfaces.GameObjects
{
    public interface ISpriteRenderableComponent : IRenderableComponent
    {
        Sprite GetCurrentSprite();
        Color4 Color { get; set; }
    }
}
