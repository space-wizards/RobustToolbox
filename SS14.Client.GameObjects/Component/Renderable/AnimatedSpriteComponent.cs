using SS14.Client.ClientWindow;
using SS14.Client.Graphics;
using SS14.Client.Graphics.CluwneLib.Sprite;
using SS14.Client.Interfaces.GOC;
using SS14.Client.Interfaces.Map;
using SS14.Client.Interfaces.Resource;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using SS14.Shared.GO.Component.Renderable;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using SS14.Client.Graphics.CluwneLib;
using SS14.Shared.Maths;

namespace SS14.Client.GameObjects
{
    public class AnimatedSpriteComponent : Component, IRenderableComponent
    {
        protected string baseSprite;
        protected string currentSprite;
        protected AnimatedSprite sprite;
        protected bool flip;
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

        public RectangleF AverageAABB
        {
            get {
                var tileSize = IoCManager.Resolve<IMapManager>().TileSize;
                var aaabb = sprite.AverageAABB;
                return new RectangleF(
                    aaabb.X / tileSize, aaabb.Y / tileSize,
                    aaabb.Width / tileSize, aaabb.Height / tileSize
                    );
            }
        }

        #region ISpriteComponent Members

        public RectangleF AABB
        {
            get
            {
                var tileSize = IoCManager.Resolve<IMapManager>().TileSize;

                return new RectangleF(0, 0, sprite.AABB.Width / tileSize,
                                      sprite.AABB.Height / tileSize);
            }
        }
        
        #endregion

        public override void OnAdd(Entity owner)
        {
            base.OnAdd(owner);
            //Send a spritechanged message so everything knows whassup.
            Owner.SendMessage(this, ComponentMessageType.SpriteChanged);
        }

        public void SetSprite()
        {
            if(baseSprite != null)
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
                                                      WasClicked((PointF)list[0]), DrawDepth);
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

        protected virtual bool WasClicked(PointF worldPos)
        {
            if (sprite == null || !visible) return false;

            CluwneSprite spriteToCheck = sprite.GetCurrentSprite();

            var AABB =
                new RectangleF(
                    Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.X -
                    (spriteToCheck.Width / 2),
                    Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.Y -
                    (spriteToCheck.Height / 2), spriteToCheck.Width, spriteToCheck.Height);
            if (!AABB.Contains(worldPos)) return false;

            // Get the sprite's position within the texture
            var texRect = spriteToCheck.TextureRect;

            // Get the clicked position relative to the texture
            var spritePosition = new Point((int)(worldPos.X - AABB.X + texRect.Left),
                                           (int)(worldPos.Y - AABB.Y + texRect.Top));
            
            if (spritePosition.X < 0 || spritePosition.Y < 0)
                return false;

            // Copy the texture to image
            var img = spriteToCheck.Texture.CopyToImage();
            // Check if the clicked pixel is opaque
            if (img.GetPixel((uint)spritePosition.X, (uint)spritePosition.Y).A == 0)
                return false;

            return true;
        }

        public override void SetParameter(ComponentParameter parameter)
        {
            base.SetParameter(parameter);
            switch (parameter.MemberName)
            {
                case "drawdepth":
                    SetDrawDepth((DrawDepth)Enum.Parse(typeof(DrawDepth), parameter.GetValue<string>(), true));
                    break;

                case "sprite":
                    baseSprite = parameter.GetValue<string>();
                    SetSprite(parameter.GetValue<string>());
                    break;
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

            var ownerPos = Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position;
            
            Vector2 renderPos = ClientWindowData.Singleton.WorldToScreen(ownerPos);
            SetSpriteCenter(renderPos);

            if (ownerPos.X + sprite.AABB.Right < topLeft.X
                || ownerPos.X > bottomRight.X
                || ownerPos.Y + sprite.AABB.Bottom < topLeft.Y
                || ownerPos.Y > bottomRight.Y)
                return;

            sprite.HorizontalFlip = flip;
            sprite.Draw();
            sprite.HorizontalFlip = false;

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
            //Gorgon.CurrentRenderTarget.Rectangle(renderPos.X - aabb.Width/2, renderPos.Y - aabb.Height / 2, aabb.Width, aabb.Height, Color.Lime);

            if (_speechBubble != null)
                _speechBubble.Draw(ClientWindowData.Singleton.WorldToScreen(Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position),
                                   Vector2.Zero, aabb);

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

        public void SetSpriteCenter(Vector2 center)
        {
            sprite.SetPosition(center.X - (sprite.AABB.Width / 2),
                               center.Y - (sprite.AABB.Height / 2));
        }

        public bool IsSlaved()
        {
            return master != null;
        }

        public void SetMaster(Entity m)
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
            if(sprite.Name != state.Name)
                SetSprite(state.Name);
            if (sprite.CurrentAnimationStateKey != state.CurrentAnimation)
            {
                if(state.CurrentAnimation == null)
                    sprite.SetAnimationState("idle");
                else 
                    sprite.SetAnimationState(state.CurrentAnimation);
            }
            SetMaster((int?)state.MasterUid);

            sprite.SetLoop(state.Loop);
        }
    }
}