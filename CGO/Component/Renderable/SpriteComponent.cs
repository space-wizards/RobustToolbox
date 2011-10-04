using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GorgonLibrary.Graphics;
using GorgonLibrary;
using ClientResourceManager;
using ClientWindow;
using System.Drawing;

namespace CGO
{
    public class SpriteComponent : RenderableComponent, ISpriteComponent
    {
        protected Sprite currentSprite;
        protected bool flip;
        protected Dictionary<string, Sprite> sprites;
        public RectangleF AABB
        {
            get
            {
                return new RectangleF(0,0,currentSprite.AABB.Width, currentSprite.AABB.Height);
            }
        }

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

        public void ClearSprites()
        {
            sprites.Clear();
        }

        public override void RecieveMessage(object sender, CGO.MessageType type, List<ComponentReplyMessage> reply, params object[] list)
        {
            base.RecieveMessage(sender, type, reply, list);
            if (sender == this) return;

            switch (type)
            {
                case MessageType.Clicked:
                    object[] replyParams = new object[1];
                    replyParams[0] = WasClicked(list);
                    reply.Add(new ComponentReplyMessage(MessageType.Clicked, replyParams));
                    break;
                case MessageType.GetAABB:
                    reply.Add(new ComponentReplyMessage(MessageType.CurrentAABB, AABB));
                    break;
            }
        }

        private bool WasClicked(params object[] list)
        {
            if (currentSprite == null) return false;
            PointF worldPos = (PointF)list[0];
            // // // Almost straight copy & paste.
            System.Drawing.RectangleF AABB = new System.Drawing.RectangleF(Owner.position.X - (currentSprite.Width / 2), Owner.position.Y - (currentSprite.Height / 2), currentSprite.Width, currentSprite.Height);
            if (!AABB.Contains(worldPos)) return false;
            System.Drawing.Point spritePosition = new System.Drawing.Point((int)(worldPos.X - AABB.X + currentSprite.ImageOffset.X), (int)(worldPos.Y - AABB.Y + currentSprite.ImageOffset.Y));
            GorgonLibrary.Graphics.Image.ImageLockBox imgData = currentSprite.Image.GetImageData();
            imgData.Lock(false);
            System.Drawing.Color pixColour = System.Drawing.Color.FromArgb((int)(imgData[spritePosition.X, spritePosition.Y]));
            imgData.Dispose();
            imgData.Unlock();
            if (pixColour.A == 0) return false;
            // // //
            return true;
        }

        public void SetSpriteByKey(string spriteKey)
        {
            if (sprites.ContainsKey(spriteKey))
            {
                currentSprite = sprites[spriteKey];
                if(Owner != null)
                    Owner.SendMessage(this, MessageType.SpriteChanged, null);
            }
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
                if (flip)
                    currentSprite.HorizontalFlip = true;
                currentSprite.Draw();
                currentSprite.HorizontalFlip = false;
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
