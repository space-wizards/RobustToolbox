using OpenTK;
using SS14.Client.Graphics;
using SS14.Shared.Interfaces.GameObjects;
using System.Collections.Generic;

namespace SS14.Client.Interfaces.GameObjects
{
    public interface ISpriteComponent : IComponent
    {
        Box2 LocalAABB { get; }
        TextureSource CurrentSprite { get; }
        TextureSource GetSprite(string spriteKey);
        List<TextureSource> GetAllSprites();
        void SetSpriteByKey(string spriteKey);
        void AddSprite(string spriteKey);
        void AddSprite(string key, TextureSource spritetoadd);
    }
}
