using SFML.Graphics;

namespace SS14.Client.Interfaces.GameObjects
{
    public interface ISpriteRenderableComponent : IRenderableComponent
    {
        Sprite GetCurrentSprite();
    }
}
