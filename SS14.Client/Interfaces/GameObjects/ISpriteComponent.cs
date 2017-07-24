using SFML.Graphics;
using SS14.Shared.Interfaces.GameObjects;
using System.Collections.Generic;

namespace SS14.Client.Interfaces.GameObjects
{
    public interface ISpriteComponent : IComponent
    {
        FloatRect AABB { get; }
        Sprite GetCurrentSprite();
        Sprite GetSprite(string spriteKey);
        List<Sprite> GetAllSprites();
        void SetSpriteByKey(string spriteKey);
        void AddSprite(string spriteKey);
        void AddSprite(string key, Sprite spritetoadd);
    }
}
