using SS14.Client.Graphics.CluwneLib.Sprite;
using System.Collections.Generic;
using System.Drawing;

namespace SS14.Client.Interfaces.GOC
{
    public interface ISpriteComponent
    {
        RectangleF AABB { get; }
        CluwneSprite GetCurrentSprite();
        CluwneSprite GetSprite(string spriteKey);
        List<CluwneSprite> GetAllSprites();
        void SetSpriteByKey(string spriteKey);
        void AddSprite(string spriteKey);
        void AddSprite(string key, CluwneSprite spritetoadd);
    }
}