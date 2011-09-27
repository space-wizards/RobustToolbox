using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GorgonLibrary.Graphics;
using GorgonLibrary;
using ClientResourceManager;

namespace CGO.Component.Renderable
{
    public class SpriteComponent : RenderableComponent, ISpriteComponent
    {
        Sprite currentSprite;
        Dictionary<string, Sprite> sprites;

        public SpriteComponent()
            : base()
        {

        }
        
        public Sprite GetCurrentSprite()
        {
            return currentSprite;
        }

        public Sprite GetSprite(string spriteKey)
        {
            if (sprites.ContainsKey(spriteKey))
                return sprites[spriteKey];
            else
                return null;
        }

        public List<Sprite> GetAllSprites()
        {
            return sprites.Values.ToList();
        }

        public void SetSpriteByKey(string spriteKey)
        {
            if (sprites.ContainsKey(spriteKey))
                currentSprite = sprites[spriteKey];
            else
                throw new Exception("Whoops. That sprite isn't in the dictionary.");
        }

        public void AddSprite(string spriteKey)
        {
            if (sprites.ContainsKey(spriteKey))
                throw new Exception("That sprite is already added.");
            if (ResMgr.Singleton.SpriteExists(spriteKey))
                AddSprite(spriteKey, ResMgr.Singleton.GetSprite(spriteKey));
        }

        public void AddSprite(string key, Sprite spritetoadd)
        {
            if (spritetoadd != null && key != "")
                sprites.Add(key, spritetoadd);
        }
    }
}
