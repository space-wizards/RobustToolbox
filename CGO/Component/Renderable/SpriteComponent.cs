using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GorgonLibrary.Graphics;
using GorgonLibrary;
using ClientResourceManager;
using ClientWindow;

namespace CGO
{
    public class SpriteComponent : RenderableComponent, ISpriteComponent
    {
        Sprite currentSprite;
        Dictionary<string, Sprite> sprites;

        public SpriteComponent()
            : base()
        {
            sprites = new Dictionary<string, Sprite>();
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

        public override void Render()
        {
            Vector2D RenderPos = ClientWindowData.Singleton.WorldToScreen(Owner.position);
            if (currentSprite != null)
            {
                SetSpriteCenter(currentSprite, RenderPos);
                currentSprite.Draw();
            }
        }

        public void SetSpriteCenter(string sprite, Vector2D center)
        {
            SetSpriteCenter(sprites[sprite], center);
        }
        public void SetSpriteCenter(Sprite sprite, Vector2D center)
        {
            sprite.SetPosition(center.X - (currentSprite.AABB.Width / 2), center.Y - (currentSprite.AABB.Height / 2));
        }
    }
}
