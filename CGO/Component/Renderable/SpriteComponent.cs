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
using SS13_Shared.GO.Component.Renderable;

namespace CGO
{
    public class SpriteComponent : RenderableComponent, ISpriteComponent
    {
        protected Sprite currentBaseSprite;
        protected bool flip;
        protected Dictionary<string, Sprite> sprites;
        protected Dictionary<string, Sprite> dirSprites;
        protected bool visible = true;
        protected SpriteComponent master;
        protected List<SpriteComponent> slaves;
        
        public override Type StateType
        {
            get
            {
                return typeof(SpriteComponentState);
            }
        }

        public RectangleF AABB
        {
            get
            {
                return new RectangleF(0, 0, GetActiveDirectionalSprite().AABB.Width, GetActiveDirectionalSprite().AABB.Height);
            }
        }

        public override float Bottom
        {
            get { return Owner.Position.Y + (GetActiveDirectionalSprite().AABB.Height / 2); }
        }

        private void BuildDirectionalSprites()
        {
            dirSprites.Clear();
            IResourceManager resMgr = IoCManager.Resolve<IResourceManager>();

            foreach(KeyValuePair<string, Sprite> curr in sprites)
            {
                foreach (var dir in Enum.GetNames(typeof(Direction)))
                {
                    string name = (curr.Key + "_" + dir).ToLowerInvariant();
                    if (resMgr.SpriteExists(name))
                        dirSprites.Add(name, resMgr.GetSprite(name));
                }
            }
        }

        public SpriteComponent()
        {
            sprites = new Dictionary<string, Sprite>();
            dirSprites = new Dictionary<string, Sprite>();
            slaves = new List<SpriteComponent>();
        }

        public override void OnAdd(IEntity owner)
        {
            base.OnAdd(owner);
            //Send a spritechanged message so everything knows whassup.
            Owner.SendMessage(this, ComponentMessageType.SpriteChanged);
        }

        public Sprite GetCurrentSprite()
        {
            return GetActiveDirectionalSprite();
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
                case ComponentMessageType.SlaveAttach:
                    SetMaster(EntityManager.Singleton.GetEntity((int) list[0]));
                    break;
                case ComponentMessageType.ItemUnEquipped:
                case ComponentMessageType.Dropped:
                    UnsetMaster();
                    break;
            }

            return reply;
        }

        protected virtual Sprite GetBaseSprite()
        {
            return currentBaseSprite;
        }

        protected void SetDrawDepth(DrawDepth p)
        {
            DrawDepth = p;
        }

        private Sprite GetActiveDirectionalSprite()
        {
            if (currentBaseSprite == null) return null;

            Sprite sprite = currentBaseSprite;

            string dirName = (currentBaseSprite.Name + "_" + Owner.Direction.ToString()).ToLowerInvariant();

            if (dirSprites.ContainsKey(dirName))
                sprite = dirSprites[dirName];

            return sprite;
        }

        protected virtual bool WasClicked(PointF worldPos)
        {
            if (currentBaseSprite == null || !visible) return false;

            Sprite spriteToCheck = GetActiveDirectionalSprite();

            System.Drawing.RectangleF AABB = new System.Drawing.RectangleF(Owner.Position.X - (spriteToCheck.Width / 2), Owner.Position.Y - (spriteToCheck.Height / 2), spriteToCheck.Width, spriteToCheck.Height);
            if (!AABB.Contains(worldPos)) return false;

            System.Drawing.Point spritePosition = new System.Drawing.Point((int)(worldPos.X - AABB.X + spriteToCheck.ImageOffset.X), (int)(worldPos.Y - AABB.Y + spriteToCheck.ImageOffset.Y));

            GorgonLibrary.Graphics.Image.ImageLockBox imgData = spriteToCheck.Image.GetImageData();
            imgData.Lock(false);
            System.Drawing.Color pixColour = System.Drawing.Color.FromArgb((int)(imgData[spritePosition.X, spritePosition.Y]));

            imgData.Dispose();
            imgData.Unlock();

            if (pixColour.A == 0) return false;

            return true;
        }

        public void SetSpriteByKey(string spriteKey)
        {
            if (sprites.ContainsKey(spriteKey))
            {
                currentBaseSprite = sprites[spriteKey];
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

            BuildDirectionalSprites();
        }

        public void AddSprite(string key, Sprite spritetoadd)
        {
            if (spritetoadd != null && key != "")
                sprites.Add(key, spritetoadd);
            BuildDirectionalSprites();
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
                case "drawdepth":
                    SetDrawDepth((DrawDepth)Enum.Parse(typeof(DrawDepth), parameter.GetValue<string>(), true));
                    break;
                case "addsprite":
                    AddSprite(parameter.GetValue<string>());
                    break;
            }
        }

        public override void Render(Vector2D topLeft, Vector2D bottomRight)
        {
            //Render slaves beneath
            var renderablesBeneath = from SpriteComponent c in slaves //FIXTHIS
                    orderby c.DrawDepth ascending
                    where c.DrawDepth < DrawDepth
                    select c;

            foreach (var component in renderablesBeneath.ToList())
            {
                component.Render(topLeft, bottomRight);
            }

            //Render this sprite
            if (!visible) return;
            if (currentBaseSprite == null) return;

            Sprite spriteToRender = GetActiveDirectionalSprite();

            var renderPos = ClientWindowData.WorldToScreen(Owner.Position);
            SetSpriteCenter(spriteToRender, renderPos);

            if (Owner.Position.X + spriteToRender.AABB.Right < topLeft.X
                || Owner.Position.X > bottomRight.X
                || Owner.Position.Y + spriteToRender.AABB.Bottom < topLeft.Y
                || Owner.Position.Y > bottomRight.Y)
                return;

            spriteToRender.HorizontalFlip = flip;
            spriteToRender.Draw();
            spriteToRender.HorizontalFlip = false;

            //Render slaves above
            var renderablesAbove = from SpriteComponent c in slaves //FIXTHIS
                              orderby c.DrawDepth ascending
                              where c.DrawDepth >= DrawDepth
                              select c;

            foreach (var component in renderablesAbove.ToList())
            {
                component.Render(topLeft, bottomRight);
            }
        }

        public void SetSpriteCenter(string sprite, Vector2D center)
        {
            SetSpriteCenter(sprites[sprite], center);
        }
        public void SetSpriteCenter(Sprite sprite, Vector2D center)
        {
            sprite.SetPosition(center.X - (GetActiveDirectionalSprite().AABB.Width / 2), center.Y - (GetActiveDirectionalSprite().AABB.Height / 2));
        }

        public bool IsSlaved() { return master != null; }

        public void SetMaster(IEntity m) 
        { 
            if(!m.HasComponent(ComponentFamily.Renderable))
                return;
            var mastercompo = m.GetComponent<SpriteComponent>(ComponentFamily.Renderable);
            //If there's no sprite component, then FUCK IT
            if (mastercompo == null)
                return;

            // lets get gay together and do some shit like in that stupid book 50 shades of gay
            // “His pointer finger circled my puckered love cave. “Are you ready for this?” he mewled, smirking at me like a mother hamster about to eat her three-legged young.”
            mastercompo.AddSlave(this);
            master = mastercompo;
        }

        public void UnsetMaster()
        {
            if (master == null)
                return;
            master.RemoveSlave(this);
            master = null;
        }

        public void AddSlave(SpriteComponent slavecompo)
        {
            slaves.Add(slavecompo);
        }

        public void RemoveSlave(SpriteComponent slavecompo)
        {
            if (slaves.Contains(slavecompo))
                slaves.Remove(slavecompo);
        }

        public override void HandleComponentState(dynamic state)
        {
            base.HandleComponentState((SpriteComponentState)state);
            if (state.SpriteKey != null && sprites.ContainsKey(state.SpriteKey) && currentBaseSprite != sprites[state.SpriteKey])
            {   
                SetSpriteByKey(state.SpriteKey);
            }

            visible = state.Visible;
        }
    }
}
