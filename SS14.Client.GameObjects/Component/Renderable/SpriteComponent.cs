using Lidgren.Network;
using SFML.Graphics;
using SFML.System;
using SS14.Client.Graphics;
using SS14.Client.Graphics.texture;
using SS14.Client.Interfaces.GOC;
using SS14.Client.Interfaces.Resource;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using SS14.Shared.GO.Component.Renderable;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SS14.Client.GameObjects
{
    public class SpriteComponent : Component, IRenderableComponent, ISpriteComponent
    {
        protected Sprite currentBaseSprite;
        protected string currentBaseSpriteKey;
        protected Dictionary<string, Sprite> dirSprites;
        protected bool HorizontalFlip { get; set; }
        protected IRenderableComponent master;
        protected List<IRenderableComponent> slaves;
        protected Dictionary<string, Sprite> sprites;
        protected bool visible = true;
        public DrawDepth DrawDepth { get; set; }

        public SpriteComponent()
        {
            Family = ComponentFamily.Renderable;
            sprites = new Dictionary<string, Sprite>();
            dirSprites = new Dictionary<string, Sprite>();
            slaves = new List<IRenderableComponent>();
        }

        public override Type StateType
        {
            get { return typeof (SpriteComponentState); }
        }

        public float Bottom
        {
            get
            {
                return Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.Y +
                       (GetActiveDirectionalSprite().GetLocalBounds().Height/2);
            }
        }

        #region ISpriteComponent Members
        
        public FloatRect AverageAABB
        {
            get { return AABB; }
        }
        
        public FloatRect AABB
        {
            get
            {
                var bounds = GetActiveDirectionalSprite().GetLocalBounds();
                return new FloatRect(0, 0, bounds.Width, bounds.Height);
            }
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

        public void SetSpriteByKey(string spriteKey)
        {
            if (sprites.ContainsKey(spriteKey))
            {
                currentBaseSprite = sprites[spriteKey];
                currentBaseSpriteKey = spriteKey;
                if (Owner != null)
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

        public bool HasSprite(string key)
        {
            return sprites.ContainsKey(key);
        }

        #endregion

        private void BuildDirectionalSprites()
        {
            dirSprites.Clear();
            var resMgr = IoCManager.Resolve<IResourceManager>();

            foreach (var curr in sprites)
            {
                foreach (string dir in Enum.GetNames(typeof (Direction)))
                {
                    string name = (curr.Key + "_" + dir).ToLowerInvariant();
                    if (resMgr.SpriteExists(name))
                        dirSprites.Add(name, resMgr.GetSprite(name));
                }
            }
        }

        public override void OnAdd(Entity owner)
        {
            base.OnAdd(owner);
            //Send a spritechanged message so everything knows whassup.
            Owner.SendMessage(this, ComponentMessageType.SpriteChanged);
        }

        public void ClearSprites()
        {
            sprites.Clear();
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection sender)
        {
            switch ((ComponentMessageType) message.MessageParameters[0])
            {
                case ComponentMessageType.SetVisible:
                    visible = (bool) message.MessageParameters[1];
                    break;
                case ComponentMessageType.SetSpriteByKey:
                    SetSpriteByKey((string) message.MessageParameters[1]);
                    break;
            }
        }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            ComponentReplyMessage reply = base.RecieveMessage(sender, type, list);

            if (sender == this) //Don't listen to our own messages!
                return ComponentReplyMessage.Empty;

            switch (type)
            {
                case ComponentMessageType.CheckSpriteClick:
                    reply = new ComponentReplyMessage(ComponentMessageType.SpriteWasClicked,
                                                      WasClicked((Vector2f) list[0]), DrawDepth);
                    break;
                case ComponentMessageType.GetAABB:
                    reply = new ComponentReplyMessage(ComponentMessageType.CurrentAABB, AABB);
                    break;
                case ComponentMessageType.GetSprite:
                    reply = new ComponentReplyMessage(ComponentMessageType.CurrentSprite, GetBaseSprite());
                    break;
                case ComponentMessageType.SetSpriteByKey:
                    SetSpriteByKey((string) list[0]);
                    break;
                case ComponentMessageType.SlaveAttach:
                    SetMaster(Owner.EntityManager.GetEntity((int) list[0]));
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

            string dirName =
                (currentBaseSpriteKey + "_" +
                 Owner.GetComponent<DirectionComponent>(ComponentFamily.Direction).Direction.ToString()).
                    ToLowerInvariant();

            if (dirSprites.ContainsKey(dirName))
                sprite = dirSprites[dirName];

            return sprite;
        }

        protected virtual bool WasClicked(Vector2f worldPos)
        {
            if (currentBaseSprite == null || !visible) return false;

            Sprite spriteToCheck = GetActiveDirectionalSprite();
            var bounds = spriteToCheck.GetLocalBounds();

            var AABB =
                new FloatRect(
                    Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.X - (bounds.Width/2),
                    Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.Y - (bounds.Height/2), bounds.Width, bounds.Height);
            if (!AABB.Contains(worldPos.X, worldPos.Y)) return false;
            
            // Get the sprite's position within the texture
            var texRect = spriteToCheck.TextureRect;
            
            // Get the clicked position relative to the texture
            var spritePosition = new Vector2i((int) (worldPos.X - AABB.Left + texRect.Left),
                                              (int) (worldPos.Y - AABB.Top + texRect.Top));

            if (spritePosition.X < 0 || spritePosition.Y < 0)
                return false;

            IResourceManager _resManager = IoCManager.Resolve<IResourceManager>();
            Dictionary<Texture, string> tmp = _resManager.textureToKey;
            if(!tmp.ContainsKey(spriteToCheck.Texture)) { return false; } //if it doesn't exist, something's fucked
            string textureKey = tmp[spriteToCheck.Texture];
            bool[,] opacityMap = TextureCache.Textures[textureKey].Item2; //get our clickthrough 'map'
            if(!opacityMap[spritePosition.X, spritePosition.Y]) // Check if the clicked pixel is opaque
            {
                return false;
            }

            return true;
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
            switch (parameter.MemberName)
            {
                case "drawdepth":
                    SetDrawDepth((DrawDepth) Enum.Parse(typeof (DrawDepth), parameter.GetValue<string>(), true));
                    break;
                case "addsprite":
                    AddSprite(parameter.GetValue<string>());
                    break;
            }
        }

        public virtual void Render(Vector2f topLeft, Vector2f bottomRight)
        {
            //Render slaves beneath
            IEnumerable<SpriteComponent> renderablesBeneath = from SpriteComponent c in slaves
                                                              //FIXTHIS
                                                              orderby c.DrawDepth ascending
                                                              where c.DrawDepth < DrawDepth
                                                              select c;

            foreach (SpriteComponent component in renderablesBeneath.ToList())
            {
                component.Render(topLeft, bottomRight);
            }

            //Render this sprite
            if (!visible) return;
            if (currentBaseSprite == null) return;

            Sprite spriteToRender = GetActiveDirectionalSprite();

            Vector2f renderPos = CluwneLib.WorldToScreen( Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position);
            var bounds = spriteToRender.GetLocalBounds();
            SetSpriteCenter(spriteToRender, renderPos);

            if (Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.X + bounds.Left + bounds.Width < topLeft.X
                || Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.X > bottomRight.X
                || Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.Y + bounds.Top + bounds.Height < topLeft.Y
                || Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.Y > bottomRight.Y)
                return;

            spriteToRender.Scale = new Vector2f(HorizontalFlip ? -1 : 1, 1);
            spriteToRender.Draw();
          

            //Render slaves above
            IEnumerable<SpriteComponent> renderablesAbove = from SpriteComponent c in slaves
                                                            //FIXTHIS
                                                            orderby c.DrawDepth ascending
                                                            where c.DrawDepth >= DrawDepth
                                                            select c;

            foreach (SpriteComponent component in renderablesAbove.ToList())
            {
                component.Render(topLeft, bottomRight);
            }


            //Draw AABB
            var aabb = AABB;
            if(CluwneLib.Debug.DebugColliders)
            CluwneLib.drawRectangle((int)(renderPos.X - aabb.Width / 2), (int)(renderPos.Y - aabb.Height / 2), aabb.Width, aabb.Height, new SFML.Graphics.Color(0, 255, 0));
        }

        public void SetSpriteCenter(string sprite, Vector2f center)
        {
            SetSpriteCenter(sprites[sprite], center);
        }

        public void SetSpriteCenter(Sprite sprite, Vector2f center)
        {
            var bounds = GetActiveDirectionalSprite().GetLocalBounds();
            sprite.Position = new SFML.System.Vector2f(center.X - (bounds.Width / 2),
                                                       center.Y - (bounds.Height / 2));
        }

        public bool IsSlaved()
        {
            return master != null;
        }

        public void SetMaster(Entity m)
        {
            if (!m.HasComponent(ComponentFamily.Renderable))
                return;
            var mastercompo = m.GetComponent<SpriteComponent>(ComponentFamily.Renderable);
            //If there's no sprite component, then FUCK IT
            if (mastercompo == null)
                return;

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

        public void AddSlave(IRenderableComponent slavecompo)
        {
            slaves.Add(slavecompo);
        }

        public void RemoveSlave(IRenderableComponent slavecompo)
        {
            if (slaves.Contains(slavecompo))
                slaves.Remove(slavecompo);
        }

        public override void HandleComponentState(dynamic state)
        {
            DrawDepth = state.DrawDepth;
            if (state.SpriteKey != null && sprites.ContainsKey(state.SpriteKey) &&
                currentBaseSprite != sprites[state.SpriteKey])
            {
                SetSpriteByKey(state.SpriteKey);
            }

            visible = state.Visible;
        }
    }
}