using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GorgonLibrary.Graphics;
using GorgonLibrary;
using ClientResourceManager;
using ClientWindow;
using System.Drawing;
using SS3D_shared.GO;

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

        public override void OnAdd(Entity owner)
        {
            base.OnAdd(owner);
            //Send a spritechanged message so everything knows whassup.
            Owner.SendMessage(this, ComponentMessageType.SpriteChanged, null);
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

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message)
        {
            switch ((ComponentMessageType)message.messageParameters[0])
            {
                case ComponentMessageType.SetSpriteByKey:
                    SetSpriteByKey((string)message.messageParameters[1]);
                    break;
            }
        }

        public override void RecieveMessage(object sender, ComponentMessageType type, List<ComponentReplyMessage> reply, params object[] list)
        {
            base.RecieveMessage(sender, type, reply, list);
            if (sender == this) return;

            switch (type)
            {
                case ComponentMessageType.CheckSpriteClick:
                    reply.Add(new ComponentReplyMessage(ComponentMessageType.SpriteWasClicked, WasClicked((PointF)list[0]), DrawDepth));
                    break;
                case ComponentMessageType.GetAABB:
                    reply.Add(new ComponentReplyMessage(ComponentMessageType.CurrentAABB, AABB));
                    break;
                case ComponentMessageType.GetSprite:
                    reply.Add(new ComponentReplyMessage(ComponentMessageType.CurrentSprite, GetBaseSprite()));
                    break;
                case ComponentMessageType.SetSpriteByKey:
                    SetSpriteByKey((string)list[0]);
                    break;
                case ComponentMessageType.SetDrawDepth:
                    SetDrawDepth((int)list[0]);
                    break;
            }
        }

        protected virtual Sprite GetBaseSprite()
        {
            return currentSprite;
        }

        protected void SetDrawDepth(int p)
        {
            DrawDepth = p;
        }

        private bool WasClicked(PointF worldPos)
        {
            if (currentSprite == null) return false;
            // // // Almost straight copy & paste.
            System.Drawing.RectangleF AABB = new System.Drawing.RectangleF(Owner.Position.X - (currentSprite.Width / 2), Owner.Position.Y - (currentSprite.Height / 2), currentSprite.Width, currentSprite.Height);
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
                    Owner.SendMessage(this, ComponentMessageType.SpriteChanged, null);
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

            //If there's only one sprite, and the current sprite is explicitly not set, then lets go ahead and set our sprite.
            if (sprites.Count == 1)
                SetSpriteByKey(sprites.Keys.First());
        }

        public void AddSprite(string key, Sprite spritetoadd)
        {
            if (spritetoadd != null && key != "")
                sprites.Add(key, spritetoadd);
        }

        public override void SetParameter(ComponentParameter parameter)
        {
            base.SetParameter(parameter);
            switch(parameter.MemberName)
            {
                case "addsprite":
                    AddSprite((string)parameter.Parameter);
                    break;
            }
        }

        public override void Render()
        {
            Vector2D RenderPos = ClientWindowData.Singleton.WorldToScreen(Owner.Position);
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
