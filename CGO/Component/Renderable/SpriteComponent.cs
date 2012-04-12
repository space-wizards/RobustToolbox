using System;
using System.Collections.Generic;
using System.Linq;
using ClientInterfaces;
using ClientInterfaces.GOC;
using ClientInterfaces.Resource;
using GorgonLibrary.Graphics;
using GorgonLibrary;
using ClientWindow;
using System.Drawing;
using SS13.IoC;
using SS13_Shared;
using SS13_Shared.GO;

namespace CGO
{
    public class SpriteComponent : RenderableComponent, ISpriteComponent
    {
        protected Sprite currentSprite;
        protected bool flip;
        protected Dictionary<string, Sprite> sprites;
        protected bool visible = true;

        public RectangleF AABB
        {
            get
            {
                return new RectangleF(0,0,currentSprite.AABB.Width, currentSprite.AABB.Height);
            }
        }

        public SpriteComponent()
        {
            sprites = new Dictionary<string, Sprite>();
        }

        public override void OnAdd(IEntity owner)
        {
            base.OnAdd(owner);
            //Send a spritechanged message so everything knows whassup.
            Owner.SendMessage(this, ComponentMessageType.SpriteChanged);
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
            switch ((ComponentMessageType)message.MessageParameters[0])
            {
                case ComponentMessageType.SetVisible:
                    visible = (bool)message.MessageParameters[1];
                    break;
                case ComponentMessageType.SetSpriteByKey:
                    SetSpriteByKey((string)message.MessageParameters[1]);
                    break;
                case ComponentMessageType.SetDrawDepth:
                    SetDrawDepth((DrawDepth)message.MessageParameters[1]);
                    break;
            }
        }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type, params object[] list)
        {
            var reply = base.RecieveMessage(sender, type, list);

            if (sender == this) //Don't listen to our own messages!
                return ComponentReplyMessage.Empty;

            switch (type)
            {
                case ComponentMessageType.CheckSpriteClick:
                    reply = new ComponentReplyMessage(ComponentMessageType.SpriteWasClicked, WasClicked((PointF)list[0]), DrawDepth);
                    break;
                case ComponentMessageType.GetAABB:
                    reply = new ComponentReplyMessage(ComponentMessageType.CurrentAABB, AABB);
                    break;
                case ComponentMessageType.GetSprite:
                    reply = new ComponentReplyMessage(ComponentMessageType.CurrentSprite, GetBaseSprite());
                    break;
                case ComponentMessageType.SetSpriteByKey:
                    SetSpriteByKey((string)list[0]);
                    break;
                case ComponentMessageType.SetDrawDepth:
                    SetDrawDepth((DrawDepth)list[0]);
                    break;
            }

            return reply;
        }

        protected virtual Sprite GetBaseSprite()
        {
            return currentSprite;
        }

        protected void SetDrawDepth(DrawDepth p)
        {
            DrawDepth = p;
        }

        protected virtual bool WasClicked(PointF worldPos)
        {
            if (currentSprite == null || !visible) return false;
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
                    Owner.SendMessage(this, ComponentMessageType.SpriteChanged);
            }
            else
                throw new Exception("Whoops. That sprite isn't in the dictionary.");
        }

        public void AddSprite(string spriteKey)
        {
            if (sprites.ContainsKey(spriteKey))
                throw new Exception("That sprite is already added.");
            if (IoCManager.Resolve<IResourceManager>().SpriteExists(spriteKey))
                AddSprite(spriteKey, IoCManager.Resolve<IResourceManager>().GetSprite(spriteKey));

            //If there's only one sprite, and the current sprite is explicitly not set, then lets go ahead and set our sprite.
            if (sprites.Count == 1)
                SetSpriteByKey(sprites.Keys.First());
        }

        public void AddSprite(string key, Sprite spritetoadd)
        {
            if (spritetoadd != null && key != "")
                sprites.Add(key, spritetoadd);
        }

        public bool SpriteExists(string key)
        {
            if (sprites.ContainsKey(key))
                return true;
            return false;
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

        public override void Render(Vector2D topLeft, Vector2D bottomRight)
        {
            if (!visible) return;
            if (currentSprite == null) return;
            if (Owner.Position.X + currentSprite.AABB.Right < topLeft.X
                || Owner.Position.X > bottomRight.X
                || Owner.Position.Y + currentSprite.AABB.Bottom < topLeft.Y
                || Owner.Position.Y > bottomRight.Y)
                return;

            var renderPos = ClientWindowData.Singleton.WorldToScreen(Owner.Position);
            SetSpriteCenter(currentSprite, renderPos);

            currentSprite.HorizontalFlip = flip;
            currentSprite.Draw();
            currentSprite.HorizontalFlip = false;
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
