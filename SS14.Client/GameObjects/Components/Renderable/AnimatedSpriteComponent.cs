/*
using OpenTK;
using OpenTK.Graphics;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Sprites;
using SS14.Client.Graphics.TexHelpers;
using SS14.Client.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Map;
using SS14.Client.Interfaces.Resource;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.RepresentationModel;
using Vector2i = SS14.Shared.Maths.Vector2i;
using SS14.Shared.Map;
using Vector2 = SS14.Shared.Maths.Vector2;
using SS14.Client.Graphics.Utility;

namespace SS14.Client.GameObjects
{
    public class AnimatedSpriteComponent : Component, ISpriteRenderableComponent, IClickTargetComponent
    {
        public override string Name => "AnimatedSprite";
        public override uint? NetID => NetIDs.ANIMATED_SPRITE;
        protected string baseSprite;
        protected string currentSprite;
        protected AnimatedSprite sprite;
        protected IRenderableComponent master;
        protected List<IRenderableComponent> slaves = new List<IRenderableComponent>();
        protected bool visible = true;
        public DrawDepth DrawDepth { get; set; }
        private SpeechBubble _speechBubble;
        public Color Color { get; set; } = Color.White;
        public int MapID { get; private set; }

        public override Type StateType => typeof(AnimatedSpriteComponentState);

        /// <summary>
        ///     Center of the Y axis of the sprite bounds in world coords.
        /// </summary>
        public float Bottom => Owner.GetComponent<ITransformComponent>().WorldPosition.Y + sprite.LocalAABB.Height / 2;

        public Box2 AverageAABB
        {
            get
            {
                var aaabb = sprite.AverageAABB;
                return Box2.FromDimensions(
                    aaabb.Left, aaabb.Top,
                    aaabb.Width, aaabb.Height
                    );
            }
        }

        #region ISpriteComponent Members

        public virtual Box2 LocalAABB => sprite.LocalAABB;

        public bool HorizontalFlip { get; set; }

        #endregion ISpriteComponent Members

        public override void OnAdd(IEntity owner)
        {
            base.OnAdd(owner);
            //Send a spritechanged message so everything knows whassup.
            Owner.SendMessage(this, ComponentMessageType.SpriteChanged);
        }

        public override void Initialize()
        {
            base.Initialize();
            var transform = Owner.GetComponent<ITransformComponent>();
            transform.OnMove += OnMove;
            MapID = transform.MapID;
        }

        public override void Shutdown()
        {
            var transform = Owner.GetComponent<ITransformComponent>();
            transform.OnMove -= OnMove;
            base.Shutdown();
        }

        public void OnMove(object sender, MoveEventArgs args)
        {
            MapID = args.NewPosition.MapID;
        }

        public void SetSprite()
        {
            if (baseSprite != null)
            {
                SetSprite(baseSprite);
            }
        }

        public void SetSprite(string name)
        {
            currentSprite = name;
            sprite = IoCManager.Resolve<IResourceCache>().GetAnimatedSprite(name);
        }

        public void SetAnimationState(string state, bool loop = true)
        {
            sprite.SetAnimationState(state);
            sprite.SetLoop(loop);
        }

        public override ComponentReplyMessage ReceiveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            ComponentReplyMessage reply = base.ReceiveMessage(sender, type, list);

            if (sender == this) //Don't listen to our own messages!
                return ComponentReplyMessage.Empty;

            switch (type)
            {
                case ComponentMessageType.SlaveAttach:
                    SetMaster(Owner.EntityManager.GetEntity((int)list[0]));
                    break;
                case ComponentMessageType.ItemUnEquipped:
                case ComponentMessageType.Dropped:
                    UnsetMaster();
                    break;
                case ComponentMessageType.EntitySaidSomething:
                    ChatChannel channel;
                    if (Enum.TryParse(list[0].ToString(), true, out channel))
                    {
                        string text = list[1].ToString();

                        if (channel == ChatChannel.Ingame || channel == ChatChannel.Player ||
                            channel == ChatChannel.Radio)
                        {
                            (_speechBubble ?? (_speechBubble = new SpeechBubble(Owner.Name + Owner.Uid))).SetText(text);
                        }
                    }
                    break;
            }

            return reply;
        }

        protected void SetDrawDepth(DrawDepth p)
        {
            DrawDepth = p;
        }

        public virtual Sprite GetCurrentSprite()
        {
            return sprite.GetCurrentSprite();
        }

        /// <summary>
        ///     Check if the world position is inside of the sprite texture. This checks both sprite bounds and transparency.
        /// </summary>
        /// <param name="worldPos">World position to check.</param>
        /// <returns>Is the world position inside of the sprite?</returns>
        public virtual bool WasClicked(LocalCoordinates worldPos)
        {
            if (sprite == null || !visible) return false;

            var spriteToCheck = GetCurrentSprite();

            var screenScale = CluwneLib.Camera.PixelsPerMeter;

            // local screen bounds
            var localBounds = spriteToCheck.LocalBounds;

            // local world bounds
            var worldBounds = localBounds.Scale(1.0f / screenScale);

            // move the origin from bottom right to center
            worldBounds = worldBounds.Translated(new Vector2(-worldBounds.Width / 2, -worldBounds.Height / 2));

            // absolute world bounds
            worldBounds = worldBounds.Translated(Owner.GetComponent<ITransformComponent>().WorldPosition);

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

            if (mapping.TryGetNode("sprite", out node))
            {
                baseSprite = node.AsString();
                SetSprite(baseSprite);
            }
        }

        public virtual void Render(Vector2 topLeft, Vector2 bottomRight)
        {
            UpdateSlaves();

            //Render slaves beneath
            IEnumerable<IRenderableComponent> renderablesBeneath = from IRenderableComponent c in slaves
                                                                       //FIXTHIS
                                                                   orderby c.DrawDepth ascending
                                                                   where c.DrawDepth < DrawDepth
                                                                   select c;

            foreach (IRenderableComponent component in renderablesBeneath.ToList())
            {
                component.Render(topLeft, bottomRight);
            }

            //Render this sprite
            if (!visible) return;
            if (sprite == null) return;

            var ownerPos = Owner.GetComponent<ITransformComponent>().WorldPosition;

            SetSpriteCenter(ownerPos * CluwneLib.Camera.PixelsPerMeter);
            var bounds = sprite.TextureRect;

            if (ownerPos.X + bounds.Left + bounds.Width < topLeft.X
                || ownerPos.X > bottomRight.X
                || ownerPos.Y + bounds.Top + bounds.Height < topLeft.Y
                || ownerPos.Y > bottomRight.Y)
                return;

            sprite.HorizontalFlip = HorizontalFlip;
            sprite.Draw(Color);

            //Render slaves above
            IEnumerable<IRenderableComponent> renderablesAbove = from IRenderableComponent c in slaves
                                                                     //FIXTHIS
                                                                 orderby c.DrawDepth ascending
                                                                 where c.DrawDepth >= DrawDepth
                                                                 select c;

            foreach (IRenderableComponent component in renderablesAbove.ToList())
            {
                component.Render(topLeft, bottomRight);
            }
        }

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (sprite == null || IsSlaved())
                return;

            var worldRot = Owner.GetComponent<TransformComponent>().Rotation.ToVec();

            // world2screen
            worldRot = new Vector2(worldRot.X, worldRot.Y * -1);

            //If the sprite is idle, it won't try to update Direction, meaning you stay facing the way you move
            if (sprite.CurrentAnimationStateKey.Equals("idle"))
                sprite.Update(frameTime);
            else
            {
                sprite.Direction = worldRot.GetDir();
                sprite.Update(frameTime);
            }
        }

        public virtual void UpdateSlaves()
        {
            if (slaves.Any())
            {
                foreach (var slave in slaves.OfType<AnimatedSpriteComponent>())
                {
                    slave.CopyAnimationInfoFrom(this);
                }
            }
        }

        protected void CopyAnimationInfoFrom(AnimatedSpriteComponent c)
        {
            if (sprite != null && c != null && c.sprite != null)
            {
                sprite.CopyStateFrom(c.sprite);
            }
        }

        public void SetSpriteCenter(Vector2 center)
        {
            sprite.SetPosition(center.X - (sprite.TextureRect.Width / 2),
                               center.Y - (sprite.TextureRect.Height / 2));
        }

        public bool IsSlaved()
        {
            return master != null;
        }

        public void SetMaster(IEntity m)
        {
            if (m == null)
            {
                UnsetMaster();
                return;
            }
            if (!m.HasComponent<ISpriteRenderableComponent>())
                return;
            var mastercompo = m.GetComponent<ISpriteRenderableComponent>();
            //If there's no sprite component, then FUCK IT
            if (mastercompo == null)
                return;

            mastercompo.AddSlave(this);
            master = mastercompo;
        }

        public void SetMaster(int? mUid)
        {
            if (master != null)
            {
                if (mUid == null)
                {
                    UnsetMaster();
                }
                else if (mUid != master.Owner.Uid)
                {
                    UnsetMaster();
                    SetMaster(Owner.EntityManager.GetEntity((int)mUid));
                }
            }
            else if (mUid != null)
            {
                SetMaster(Owner.EntityManager.GetEntity((int)mUid));
            }
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
            var newState = (AnimatedSpriteComponentState)state;
            DrawDepth = newState.DrawDepth;
            visible = newState.Visible;
            if (sprite.Name != newState.Name)
                SetSprite(newState.Name);

            if (sprite.CurrentAnimationStateKey != newState.CurrentAnimation)
                sprite.SetAnimationState(newState.CurrentAnimation ?? "idle");

            SetMaster(newState.MasterUid);

            sprite.SetLoop(newState.Loop);
        }
    }
}
*/
