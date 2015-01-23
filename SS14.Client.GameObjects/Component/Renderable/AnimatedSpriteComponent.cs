using GorgonLibrary;
using GorgonLibrary.Graphics;
using SS14.Client.ClientWindow;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.GOC;
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
using Image = GorgonLibrary.Graphics.Image;

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
            get { return sprite.AverageAABB; }
        }

        #region ISpriteComponent Members

        public RectangleF AABB
        {
            get
            {
                return new RectangleF(0, 0, sprite.AABB.Width,
                                      sprite.AABB.Height);
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

            foreach(AnimatedSpriteComponent s in slaves)
            {
                s.SetAnimationState(state, loop);
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
                                                      WasClicked((PointF)list[0]), DrawDepth);
                    break;
                case ComponentMessageType.GetAABB:
                    reply = new ComponentReplyMessage(ComponentMessageType.CurrentAABB, AABB);
                    break;
                case ComponentMessageType.GetSprite:
                    reply = new ComponentReplyMessage(ComponentMessageType.CurrentSprite, sprite.GetCurrentSprite());
                    break;
                case ComponentMessageType.SetDrawDepth:
                    SetDrawDepth((DrawDepth)list[0]);
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

            Sprite spriteToCheck = sprite.GetCurrentSprite();

            var AABB =
                new RectangleF(
                    Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.X -
                    (spriteToCheck.Width / 2),
                    Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.Y -
                    (spriteToCheck.Height / 2), spriteToCheck.Width, spriteToCheck.Height);
            if (!AABB.Contains(worldPos)) return false;

            var spritePosition = new Point((int)(worldPos.X - AABB.X + spriteToCheck.ImageOffset.X),
                                           (int)(worldPos.Y - AABB.Y + spriteToCheck.ImageOffset.Y));

            Image.ImageLockBox imgData = spriteToCheck.Image.GetImageData();
            imgData.Lock(false);
            Color pixColour = Color.FromArgb((int)(imgData[spritePosition.X, spritePosition.Y]));

            imgData.Dispose();
            imgData.Unlock();

            if (pixColour.A == 0) return false;

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

        public virtual void Render(Vector2D topLeft, Vector2D bottomRight)
        {
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

            Vector2D renderPos =
                ClientWindowData.WorldToScreen(
                    Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position);
            SetSpriteCenter(renderPos);

            if (Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.X + sprite.AABB.Right <
                topLeft.X
                || Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.X > bottomRight.X
                ||
                Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.Y +
                sprite.AABB.Bottom < topLeft.Y
                || Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.Y > bottomRight.Y)
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
                _speechBubble.Draw(Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position,
                                   ClientWindowData.Singleton.ScreenOrigin, aabb);

        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            if(sprite != null)
            {
                sprite.Update(frameTime);
            }
        }

        public void SetSpriteCenter(Vector2D center)
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