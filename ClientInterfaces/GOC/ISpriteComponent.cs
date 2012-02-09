using System.Collections.Generic;
using GorgonLibrary.Graphics;
using System.Drawing;

namespace ClientInterfaces.GOC
{
    public interface ISpriteComponent
    {
        Sprite GetCurrentSprite();
        Sprite GetSprite(string spriteKey);
        List<Sprite> GetAllSprites();
        void SetSpriteByKey(string spriteKey);
        void AddSprite(string spriteKey);
        void AddSprite(string key, Sprite spritetoadd);
        RectangleF AABB { get; }
    }
}
