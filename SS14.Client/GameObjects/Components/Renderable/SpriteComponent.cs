using Lidgren.Network;
using OpenTK;
using OpenTK.Graphics;
using SFML.Graphics;
using SFML.System;
using SS14.Client.Graphics;
using SS14.Client.Graphics.TexHelpers;
using SS14.Client.Interfaces.GameObjects;
using SS14.Client.Interfaces.Resource;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using SS14.Shared.Maths;
using YamlDotNet.RepresentationModel;
using Vector2i = SS14.Shared.Maths.Vector2i;
using SS14.Client.Graphics.Utility;
using SS14.Shared.Map;

namespace SS14.Client.GameObjects
{
    public class SpriteComponent : Component, ISpriteRenderableComponent, ISpriteComponent, IClickTargetComponent
    {
        public override string Name => "Sprite";
        public override uint? NetID => NetIDs.SPRITE;

        protected Sprite currentBaseSprite
        {
            get;
            private set;
        }
        protected string currentBaseSpriteKey;
        protected Dictionary<string, Sprite> dirSprites = new Dictionary<string, Sprite>();
        protected bool HorizontalFlip { get; set; }
        protected IRenderableComponent master;
        protected List<IRenderableComponent> slaves = new List<IRenderableComponent>();
        protected Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        protected bool visible = true;
        public DrawDepth DrawDepth { get; set; }
        public Color4 Color { get; set; } = Color4.White;

        public override Type StateType => typeof(SpriteComponentState);

        public float Bottom
        {
            get
            {
                return Owner.GetComponent<ITransformComponent>().Position.Y +
                       (GetActiveDirectionalSprite().GetLocalBounds().Height / 2);
            }
        }

        #region ISpriteComponent Members

        public Box2 AverageAABB => AABB;

        public Box2 AABB
        {
            get
            {
                var bounds = GetActiveDirectionalSprite().GetLocalBounds();
                return Box2.FromDimensions(0, 0, bounds.Width, bounds.Height);
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
            if (IoCManager.Resolve<IResourceCache>().SpriteExists(spriteKey))
                AddSprite(spriteKey, IoCManager.Resolve<IResourceCache>().GetSprite(spriteKey));

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

        #endregion ISpriteComponent Members

        private void BuildDirectionalSprites()
        {
            dirSprites.Clear();
            var resMgr = IoCManager.Resolve<IResourceCache>();

            foreach (var curr in sprites)
            {
                foreach (string dir in Enum.GetNames(typeof(Direction)))
                {
                    string name = (curr.Key + "_" + dir).ToLowerInvariant();
                    if (resMgr.SpriteExists(name))
                        dirSprites.Add(name, resMgr.GetSprite(name));
                }
            }
        }

        public override void OnAdd(IEntity owner)
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
            switch ((ComponentMessageType)message.MessageParameters[0])
            {
                case ComponentMessageType.SetVisible:
                    visible = (bool)message.MessageParameters[1];
                    break;
                case ComponentMessageType.SetSpriteByKey:
                    SetSpriteByKey((string)message.MessageParameters[1]);
                    break;
            }
        }

        public override ComponentReplyMessage ReceiveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            ComponentReplyMessage reply = base.ReceiveMessage(sender, type, list);

            if (sender == this) //Don't listen to our own messages!
                return ComponentReplyMessage.Empty;

            switch (type)
            {
                case ComponentMessageType.GetSprite:
                    reply = new ComponentReplyMessage(ComponentMessageType.CurrentSprite, GetBaseSprite());
                    break;
                case ComponentMessageType.SetSpriteByKey:
                    SetSpriteByKey((string)list[0]);
                    break;
                case ComponentMessageType.SlaveAttach:
                    SetMaster(Owner.EntityManager.GetEntity((int)list[0]));
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
                 Owner.GetComponent<TransformComponent>().Rotation.GetDir().ToString()).
                    ToLowerInvariant();

            if (dirSprites.ContainsKey(dirName))
                sprite = dirSprites[dirName];

            return sprite;
        }

        public virtual bool WasClicked(WorldCoordinates worldPos)
        {
            if (currentBaseSprite == null || !visible) return false;

            Sprite spriteToCheck = GetActiveDirectionalSprite();
            var bounds = spriteToCheck.GetLocalBounds();

            var AABB =
                Box2.FromDimensions(
                    Owner.GetComponent<ITransformComponent>().Position.X - (bounds.Width / 2),
                    Owner.GetComponent<ITransformComponent>().Position.Y - (bounds.Height / 2), bounds.Width, bounds.Height);
            if (!AABB.Contains(new Vector2(worldPos.X, worldPos.Y))) return false;

            // Get the sprite's position within the texture
            var texRect = spriteToCheck.TextureRect;

            // Get the clicked position relative to the texture
            var spritePosition = new Vector2i((int)(worldPos.X - AABB.Left + texRect.Left),
                                              (int)(worldPos.Y - AABB.Top + texRect.Top));

            if (spritePosition.X < 0 || spritePosition.Y < 0)
                return false;

            IResourceCache resCache = IoCManager.Resolve<IResourceCache>();
            Dictionary<Texture, string> tmp = resCache.TextureToKey;
            if (!tmp.ContainsKey(spriteToCheck.Texture)) { return false; } //if it doesn't exist, something's fucked
            string textureKey = tmp[spriteToCheck.Texture];
            bool[,] opacityMap = TextureCache.Textures[textureKey].Opacity; //get our clickthrough 'map'
            if (!opacityMap[spritePosition.X, spritePosition.Y]) // Check if the clicked pixel is opaque
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

        public override void LoadParameters(YamlMappingNode mapping)
        {
            YamlNode node;
            if (mapping.TryGetNode("drawdepth", out node))
            {
                SetDrawDepth(node.AsEnum<DrawDepth>());
            }

            if (mapping.TryGetNode("color", out node))
            {
                try
                {
                    Color = System.Drawing.Color.FromName(node.ToString());
                }
                catch
                {
                    Color = node.AsHexColor();
                }
            }

            if (mapping.TryGetNode<YamlSequenceNode>("sprites", out var sequence))
            {
                foreach (YamlNode spriteNode in sequence)
                {
                    AddSprite(spriteNode.AsString());
                }
            }
        }

        public virtual void Render(Vector2 topLeft, Vector2 bottomRight)
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

            Vector2 renderPos = CluwneLib.WorldToScreen(Owner.GetComponent<ITransformComponent>().Position);
            var bounds = spriteToRender.GetLocalBounds();
            SetSpriteCenter(spriteToRender, renderPos);

            if (Owner.GetComponent<ITransformComponent>().Position.X + bounds.Left + bounds.Width < topLeft.X
                || Owner.GetComponent<ITransformComponent>().Position.X > bottomRight.X
                || Owner.GetComponent<ITransformComponent>().Position.Y + bounds.Top + bounds.Height < topLeft.Y
                || Owner.GetComponent<ITransformComponent>().Position.Y > bottomRight.Y)
                return;

            spriteToRender.Scale = new Vector2f(HorizontalFlip ? -1 : 1, 1);
            spriteToRender.Color = this.Color.Convert();
            spriteToRender.Draw();
            spriteToRender.Color = Color4.White.Convert();

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
            if (CluwneLib.Debug.DebugColliders)
                CluwneLib.drawRectangle((int)(renderPos.X - aabb.Width / 2), (int)(renderPos.Y - aabb.Height / 2), aabb.Width, aabb.Height, Color4.Blue);
        }

        public void SetSpriteCenter(string sprite, Vector2 center)
        {
            SetSpriteCenter(sprites[sprite], center);
        }

        public void SetSpriteCenter(Sprite sprite, Vector2 center)
        {
            var bounds = GetActiveDirectionalSprite().GetLocalBounds();
            sprite.Position = new SFML.System.Vector2f(center.X - (bounds.Width / 2),
                                                       center.Y - (bounds.Height / 2));
        }

        public bool IsSlaved()
        {
            return master != null;
        }

        public void SetMaster(IEntity m)
        {
            if (!m.HasComponent<ISpriteRenderableComponent>())
                return;
            var mastercompo = m.GetComponent<ISpriteRenderableComponent>();
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

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState state)
        {
            var newState = (SpriteComponentState) state;
            DrawDepth = newState.DrawDepth;

            if (newState.SpriteKey != null && sprites.ContainsKey(newState.SpriteKey) &&
                currentBaseSprite != sprites[newState.SpriteKey])
                SetSpriteByKey(newState.SpriteKey);

            visible = newState.Visible;
        }
    }
}
