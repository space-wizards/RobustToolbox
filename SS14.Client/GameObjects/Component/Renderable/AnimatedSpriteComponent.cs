using SFML.Graphics;
using SFML.System;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Sprite;
using SS14.Client.Graphics.TexHelpers;
using SS14.Client.Interfaces.GameObjects;
using SS14.Client.Interfaces.Map;
using SS14.Client.Interfaces.Resource;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components.Renderable;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace SS14.Client.GameObjects
{
    [IoCTarget]
    public class AnimatedSpriteComponent : ClientComponent, IRenderableComponent
    {
        public override string Name => "AnimatedSprite";
        protected string baseSprite;
        protected string currentSprite;
        protected AnimatedSprite sprite;
        protected IRenderableComponent master;
        protected List<IRenderableComponent> slaves;
        protected bool visible = true;
        public DrawDepth DrawDepth { get; set; }
        private SpeechBubble _speechBubble;

        public AnimatedSpriteComponent()
        {
            Family = ComponentFamily.Renderable;
            slaves = new List<IRenderableComponent>();
        }

        public override Type StateType
        {
            get { return typeof(AnimatedSpriteComponentState); }
        }

        public float Bottom
        {
            get
            {
                return Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.Y +
                       (sprite.AABB.Height / 2);
            }
        }

        public FloatRect AverageAABB
        {
            get
            {
                var tileSize = IoCManager.Resolve<IMapManager>().TileSize;
                var aaabb = sprite.AverageAABB;
                return new FloatRect(
                    aaabb.Left / tileSize, aaabb.Top / tileSize,
                    aaabb.Width / tileSize, aaabb.Height / tileSize
                    );
            }
        }

        #region ISpriteComponent Members

        public FloatRect AABB
        {
            get
            {
                var tileSize = IoCManager.Resolve<IMapManager>().TileSize;

                return new FloatRect(0, 0, sprite.AABB.Width / tileSize,
                                      sprite.AABB.Height / tileSize);
            }
        }

        public bool HorizontalFlip { get; set; }

        #endregion ISpriteComponent Members

        public override void OnAdd(IEntity owner)
        {
            base.OnAdd(owner);
            //Send a spritechanged message so everything knows whassup.
            Owner.SendMessage(this, ComponentMessageType.SpriteChanged);
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
            sprite = (AnimatedSprite)IoCManager.Resolve<IResourceManager>().GetAnimatedSprite(name);
        }

        public void SetAnimationState(string state, bool loop = true)
        {
            sprite.SetAnimationState(state);
            sprite.SetLoop(loop);

            /*foreach(AnimatedSpriteComponent s in slaves)
            {
                s.SetAnimationState(state, loop);
            }*/
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
                                                      WasClicked((Vector2f)list[0]), DrawDepth);
                    break;
                case ComponentMessageType.GetAABB:
                    reply = new ComponentReplyMessage(ComponentMessageType.CurrentAABB, AABB);
                    break;
                case ComponentMessageType.GetSprite:
                    reply = new ComponentReplyMessage(ComponentMessageType.CurrentSprite, sprite.GetCurrentSprite());
                    break;
                case ComponentMessageType.SlaveAttach:
                    SetMaster(Owner.EntityManager.GetEntity((int)list[0]));
                    break;
                case ComponentMessageType.ItemUnEquipped:
                case ComponentMessageType.Dropped:
                    UnsetMaster();
                    break;
                case ComponentMessageType.MoveDirection:
                    switch ((Direction)list[0])
                    {
                        case Direction.North:
                            sprite.Direction = Direction.North;
                            break;
                        case Direction.South:
                            sprite.Direction = Direction.South;
                            break;
                        case Direction.East:
                            sprite.Direction = Direction.East;
                            break;
                        case Direction.West:
                            sprite.Direction = Direction.West;
                            break;
                        case Direction.NorthEast:
                            sprite.Direction = Direction.NorthEast;
                            break;
                        case Direction.NorthWest:
                            sprite.Direction = Direction.NorthWest;
                            break;
                        case Direction.SouthEast:
                            sprite.Direction = Direction.SouthEast;
                            break;
                        case Direction.SouthWest:
                            sprite.Direction = Direction.SouthWest;
                            break;
                    }
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

        protected virtual bool WasClicked(Vector2f worldPos)
        {
            if (sprite == null || !visible) return false;

            Sprite spriteToCheck = sprite.GetCurrentSprite();
            var bounds = spriteToCheck.GetLocalBounds();

            var AABB =
                new FloatRect(
                    Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.X -
                    (bounds.Width / 2),
                    Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.Y -
                    (bounds.Height / 2), bounds.Width, bounds.Height);
            if (!AABB.Contains(worldPos.X, worldPos.Y)) return false;

            // Get the sprite's position within the texture
            var texRect = spriteToCheck.TextureRect;

            // Get the clicked position relative to the texture
            var spritePosition = new Vector2i((int)(worldPos.X - AABB.Left + texRect.Left),
                                              (int)(worldPos.Y - AABB.Top + texRect.Top));

            if (spritePosition.X < 0 || spritePosition.Y < 0)
                return false;

            IResourceManager _resManager = IoCManager.Resolve<IResourceManager>();
            Dictionary<Texture, string> tmp = _resManager.TextureToKey;
            if (!tmp.ContainsKey(spriteToCheck.Texture)) { return false; } //if it doesn't exist, something's fucked
            string textureKey = tmp[spriteToCheck.Texture];
            bool[,] opacityMap = TextureCache.Textures[textureKey].Opacity; //get our clickthrough 'map'
            if (!opacityMap[spritePosition.X, spritePosition.Y]) // Check if the clicked pixel is opaque
            {
                return false;
            }

            return true;
        }

        public override void LoadParameters(Dictionary<string, YamlNode> mapping)
        {
            YamlNode node;
            if (mapping.TryGetValue("drawdepth", out node))
            {
                SetDrawDepth(node.AsEnum<DrawDepth>());
            }

            if (mapping.TryGetValue("sprite", out node))
            {
                baseSprite = node.AsString();
                SetSprite(baseSprite);
            }
        }

        public virtual void Render(Vector2f topLeft, Vector2f bottomRight)
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

            var ownerPos = Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position;

            Vector2f renderPos = CluwneLib.WorldToScreen(ownerPos);
            SetSpriteCenter(renderPos);
            var bounds = sprite.AABB;

            if (ownerPos.X + bounds.Left + bounds.Width < topLeft.X
                || ownerPos.X > bottomRight.X
                || ownerPos.Y + bounds.Top + bounds.Height < topLeft.Y
                || ownerPos.Y > bottomRight.Y)
                return;

            sprite.HorizontalFlip = HorizontalFlip;
            sprite.Draw();

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

            //Draw AABB
            var aabb = AABB;
            //CluwneLib.CurrentRenderTarget.Rectangle(renderPos.X - aabb.Width/2, renderPos.Y - aabb.Height / 2, aabb.Width, aabb.Height, Color.Lime);

            if (_speechBubble != null)
                _speechBubble.Draw(CluwneLib.WorldToScreen(Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position),
                                   new Vector2f(), aabb);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            if (sprite != null && !IsSlaved())
            {
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

        public void SetSpriteCenter(Vector2f center)
        {
            sprite.SetPosition(center.X - (sprite.AABB.Width / 2),
                               center.Y - (sprite.AABB.Height / 2));
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
            if (!m.HasComponent(ComponentFamily.Renderable))
                return;
            var mastercompo = m.GetComponent<IRenderableComponent>(ComponentFamily.Renderable);
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

        public override void HandleComponentState(dynamic state)
        {
            DrawDepth = state.DrawDepth;
            visible = state.Visible;
            if (sprite.Name != state.Name)
                SetSprite(state.Name);
            if (sprite.CurrentAnimationStateKey != state.CurrentAnimation)
            {
                if (state.CurrentAnimation == null)
                    sprite.SetAnimationState("idle");
                else
                    sprite.SetAnimationState(state.CurrentAnimation);
            }
            SetMaster((int?)state.MasterUid);

            sprite.SetLoop(state.Loop);
        }
    }
}
