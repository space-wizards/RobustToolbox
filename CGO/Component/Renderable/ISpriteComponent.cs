using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using System.Drawing;

namespace CGO
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
