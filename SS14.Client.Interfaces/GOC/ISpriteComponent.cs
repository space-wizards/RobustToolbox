using SFML.Graphics;
using System.Collections.Generic;
using System.Drawing;

namespace SS14.Client.Interfaces.GOC
{
    public interface ISpriteComponent
    {
        RectangleF AABB { get; }
        Sprite GetCurrentSprite();
        Sprite GetSprite(string spriteKey);
        List<Sprite> GetAllSprites();
        void SetSpriteByKey(string spriteKey);
        void AddSprite(string spriteKey);
        void AddSprite(string key, Sprite spritetoadd);
    }
}