using Lidgren.Network;
using OpenTK;
using OpenTK.Graphics;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Sprites;
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
using Vector2 = SS14.Shared.Maths.Vector2;
using SS14.Client.Graphics.Render;

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
        public Color Color { get; set; } = Color.White;
        public MapId MapID { get; private set; }

        public override Type StateType => typeof(SpriteComponentState);

        private ITransformComponent transform;

        public float Bottom
        {
            get
            {
                if(Owner.Initialized)
                    return transform.WorldPosition.Y + (GetActiveDirectionalSprite().LocalBounds.Height / 2);
                return 0;
            }
        }

        #region ISpriteComponent Members

        public Box2 AverageAABB => LocalAABB;

        public Box2 LocalAABB
        {
            get
            {
                var bounds = GetActiveDirectionalSprite().LocalBounds;
                return Box2.FromDimensions(0, 0, bounds.Width, bounds.Height);
            }
        }

        /// <summary>
        ///     Offsets the sprite from the entity origin by this many meters.
        /// </summary>
        public Vector2 Offset { get; set; }

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
            {
                throw new InvalidOperationException("That sprite is already added.");
            }

            var manager = IoCManager.Resolve<IResourceCache>();
            if (manager.SpriteExists(spriteKey))
            {
                AddSprite(spriteKey, manager.GetSprite(spriteKey));
            }

            //If there's only one sprite, and the current sprite is explicitly not set, then lets go ahead and set our sprite.
            if (sprites.Count == 1)
            {
                SetSpriteByKey(sprites.Keys.First());
            }

            BuildDirectionalSprites();
        }

        public void AddSprite(string key, Sprite spritetoadd)
        {
            if (spritetoadd != null && !String.IsNullOrEmpty(key))
            {
                sprites.Add(key, spritetoadd);
            }
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

        public override void Initialize()
        {
            base.Initialize();
            transform = Owner.GetComponent<ITransformComponent>();
            transform.OnMove += OnMove;
            MapID = transform.MapID;
        }

        public override void Shutdown()
        {
            transform.OnMove -= OnMove;
            transform = null;
            base.Shutdown();
        }

        public void OnMove(object sender, MoveEventArgs args)
        {
            MapID = args.NewPosition.MapID;
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
                 transform.Rotation.GetDir().ToString()).
                    ToLowerInvariant();

            if (dirSprites.ContainsKey(dirName))
                sprite = dirSprites[dirName];

            return sprite;
        }

        /// <summary>
        ///     Check if the world position is inside of the sprite texture. This checks both sprite bounds and transparency.
        /// </summary>
        /// <param name="worldPos">World position to check.</param>
        /// <returns>Is the world position inside of the sprite?</returns>
        public virtual bool WasClicked(LocalCoordinates worldPos)
        {
            var spriteToCheck = GetActiveDirectionalSprite();

            if (spriteToCheck == null || !visible) return false;

            var screenScale = CluwneLib.Camera.PixelsPerMeter;

            // local screen bounds
            var localBounds = spriteToCheck.LocalBounds;

            // local world bounds
            var worldBounds = localBounds.Scale(1.0f / screenScale);

            // move the origin from bottom right to center
            worldBounds = worldBounds.Translated(new Vector2(-worldBounds.Width / 2, -worldBounds.Height / 2));

            // absolute world bounds
            worldBounds = worldBounds.Translated(transform.WorldPosition);

            // check if clicked inside of the rectangle
            if (!worldBounds.Contains(worldPos.ToWorld().Position))
                return false;

            // Get the sprite's position within the texture
            var texRect = spriteToCheck.TextureRect;

            // Get the clicked position relative to the texture (World to Texture)
            var pixelPos = new Vector2i((int)((worldPos.X - worldBounds.Left) * screenScale), (int)((worldPos.Y - worldBounds.Top) * screenScale));

            // offset pos by texture sub-rectangle
            pixelPos = pixelPos + new Vector2i(texRect.Left, texRect.Top);

            // make sure the position is actually inside the texture
            if (!texRect.Contains(pixelPos.X, pixelPos.Y))
                throw new InvalidOperationException("The click was inside the sprite bounds, but not inside the texture bounds? Check yo math.");

            // fetch texture key of the sprite
            var resCache = IoCManager.Resolve<IResourceCache>();
            if (!resCache.TextureToKey.TryGetValue(spriteToCheck.Texture, out string textureKey))
                throw new InvalidOperationException("Trying to look up a texture that does not exist in the ResourceCache.");

            // use the texture key to fetch the Image of the sprite
            if (!TextureCache.Textures.TryGetValue(textureKey, out TextureInfo texInfo))
                throw new InvalidOperationException("The texture exists in the ResourceCache, but not in the CluwneLib TextureCache?");

            // Check if the clicked pixel is transparent enough in the Image
            return texInfo.Image[(uint)pixelPos.X, (uint)pixelPos.Y].AByte >= Limits.ClickthroughLimit;
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

            var renderPos = (transform.WorldPosition + Offset) * CluwneLib.Camera.PixelsPerMeter;
            var bounds = spriteToRender.LocalBounds;
            SetSpriteCenter(spriteToRender, renderPos);

            if (transform.WorldPosition.X + bounds.Left + bounds.Width < topLeft.X
                || transform.WorldPosition.X > bottomRight.X
                || transform.WorldPosition.Y + bounds.Top + bounds.Height < topLeft.Y
                || transform.WorldPosition.Y > bottomRight.Y)
                return;

            spriteToRender.Scale = new Vector2(HorizontalFlip ? -1 : 1, 1);
            spriteToRender.Color = this.Color;
            spriteToRender.Draw();
            spriteToRender.Color = Color.White;

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
        }

        public void SetSpriteCenter(string sprite, Vector2 center)
        {
            SetSpriteCenter(sprites[sprite], center);
        }

        public void SetSpriteCenter(Sprite sprite, Vector2 center)
        {
            var bounds = GetActiveDirectionalSprite().LocalBounds;
            sprite.Position = new Vector2(center.X - (bounds.Width / 2),
                                          center.Y - (bounds.Height / 2));
        }

        public void SetSpriteCenter(Sprite sprite, LocalCoordinates worldPos)
        {
            SetSpriteCenter(sprite, worldPos.Position);
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
            var newState = (SpriteComponentState)state;
            DrawDepth = newState.DrawDepth;
            Offset = newState.Offset;

            if (newState.SpriteKey != null && sprites.ContainsKey(newState.SpriteKey) &&
                currentBaseSprite != sprites[newState.SpriteKey])
                SetSpriteByKey(newState.SpriteKey);

            visible = newState.Visible;
        }
    }
}
