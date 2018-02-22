using System.Collections.Generic;
using SS14.Client.Graphics;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Maths;

namespace SS14.Client.Interfaces.GameObjects
{
    public interface ISpriteComponent : IComponent
    {
        Box2 LocalAABB { get; }
        Texture CurrentSprite { get; }
        Texture GetSprite(string spriteKey);
        List<Texture> GetAllSprites();
        void SetSpriteByKey(string spriteKey);
        void AddSprite(string spriteKey);
        void AddSprite(string key, Texture spritetoadd);
    }
}
