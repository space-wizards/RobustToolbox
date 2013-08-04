using System;
using System.Drawing;
using System.Linq;
using CGO;
using ClientInterfaces.Player;
using ClientInterfaces.Resource;
using ClientInterfaces.UserInterface;
using ClientServices.Helpers;
using GameObject;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using Lidgren.Network;
using SS13.IoC;
using SS13_Shared;
using SS13_Shared.GO;

namespace ClientServices.UserInterface.Components
{
    public struct UiHandInfo
    {
        public Entity Entity;
        public Hand Hand;
        public Sprite HeldSprite;
    }

    public class HandsGui : GuiComponent
    {
        private readonly Color _inactiveColor = Color.FromArgb(255, 90, 90, 90);

        private readonly IPlayerManager _playerManager = IoCManager.Resolve<IPlayerManager>();
        private readonly IUserInterfaceManager _userInterfaceManager = IoCManager.Resolve<IUserInterfaceManager>();
        private readonly Sprite handSlot;
        private readonly int spacing = 1;

        public UiHandInfo LeftHand;
        public UiHandInfo RightHand;
        private Rectangle handL;
        private Rectangle handR;

        public HandsGui()
        {
            var _resMgr = IoCManager.Resolve<IResourceManager>();
            ComponentClass = GuiComponentType.HandsUi;
            handSlot = _resMgr.GetSprite("hand");
            ZDepth = 5;
        }

        public override void ComponentUpdate(params object[] args)
        {
            base.ComponentUpdate(args);
            UpdateHandIcons();
        }

        public override void Update(float frameTime)
        {
            handL = new Rectangle(Position.X, Position.Y, (int) handSlot.Width, (int) handSlot.Height);
            handR = new Rectangle(Position.X + (int) handSlot.Width + spacing, Position.Y, (int) handSlot.Width,
                                  (int) handSlot.Height);
            ClientArea = new Rectangle(Position.X, Position.Y, (int) ((handSlot.Width*2) + spacing),
                                       (int) handSlot.Height);
        }

        public override void Render()
        {
            if (_playerManager == null || _playerManager.ControlledEntity == null)
                return;

            Entity entity = _playerManager.ControlledEntity;
            var hands = (HumanHandsComponent) entity.GetComponent(ComponentFamily.Hands);

            if (hands.CurrentHand == Hand.Left)
            {
                handSlot.Color = Color.White;
                handSlot.Draw(handL);

                handSlot.Color = _inactiveColor;
                handSlot.Draw(handR);
            }
            else
            {
                handSlot.Color = Color.White;
                handSlot.Draw(handR);

                handSlot.Color = _inactiveColor;
                handSlot.Draw(handL);
            }

            if (LeftHand.Entity != null && LeftHand.HeldSprite != null)
                LeftHand.HeldSprite.Draw(
                    new Rectangle(handL.X + (int) (handL.Width/2f - LeftHand.HeldSprite.AABB.Width/2f),
                                  handL.Y + (int) (handL.Height/2f - LeftHand.HeldSprite.AABB.Height/2f),
                                  (int) LeftHand.HeldSprite.AABB.Width, (int) LeftHand.HeldSprite.AABB.Height));

            if (RightHand.Entity != null && RightHand.HeldSprite != null)
                RightHand.HeldSprite.Draw(
                    new Rectangle(handR.X + (int) (handR.Width/2f - RightHand.HeldSprite.AABB.Width/2f),
                                  handR.Y + (int) (handR.Height/2f - RightHand.HeldSprite.AABB.Height/2f),
                                  (int) RightHand.HeldSprite.AABB.Width, (int) RightHand.HeldSprite.AABB.Height));
        }

        public override void Resize()
        {
        }

        public override void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public override void HandleNetworkMessage(NetIncomingMessage message)
        {
        }

        public void UpdateHandIcons()
        {
            if (_playerManager.ControlledEntity == null)
                return;

            Entity entity = _playerManager.ControlledEntity;
            var hands = (HumanHandsComponent) entity.GetComponent(ComponentFamily.Hands);

            if (hands == null) return;

            if (hands.HandSlots.Keys.Contains(Hand.Left))
            {
                if (LeftHand.Entity == null || LeftHand.Entity.Uid != hands.HandSlots[Hand.Left].Uid)
                {
                    Entity entityL = hands.HandSlots[Hand.Left];
                    LeftHand.Entity = entityL;
                    LeftHand.HeldSprite = Utilities.GetSpriteComponentSprite(entityL);
                }
            }
            else
            {
                LeftHand.Entity = null;
                LeftHand.HeldSprite = null;
            }

            if (hands.HandSlots.Keys.Contains(Hand.Right))
            {
                if (RightHand.Entity == null || RightHand.Entity.Uid != hands.HandSlots[Hand.Right].Uid)
                {
                    Entity entityR = hands.HandSlots[Hand.Right];
                    RightHand.Entity = entityR;
                    RightHand.HeldSprite = Utilities.GetSpriteComponentSprite(entityR);
                }
            }
            else
            {
                RightHand.Entity = null;
                RightHand.HeldSprite = null;
            }
        }

        private void SendSwitchHandTo(Hand hand)
        {
            var _playerManager = IoCManager.Resolve<IPlayerManager>();

            Entity playerEntity = _playerManager.ControlledEntity;
            var equipComponent = (HumanHandsComponent) playerEntity.GetComponent(ComponentFamily.Hands);
            equipComponent.SendSwitchHands(hand);
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            switch (e.Buttons)
            {
                case MouseButtons.Right:
                    if (handL.Contains(new Point((int) e.Position.X, (int) e.Position.Y)))
                    {
                        SendSwitchHandTo(Hand.Left);
                        return true;
                    }
                    if (handR.Contains(new Point((int) e.Position.X, (int) e.Position.Y)))
                    {
                        SendSwitchHandTo(Hand.Right);
                        return true;
                    }
                    break;
            }
            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            if (ClientArea.Contains(new Point((int) e.Position.X, (int) e.Position.Y)))
            {
                if (_playerManager.ControlledEntity == null)
                    return false;

                Entity entity = _playerManager.ControlledEntity;

                var equipment = (EquipmentComponent) entity.GetComponent(ComponentFamily.Equipment);
                var hands = (HumanHandsComponent) entity.GetComponent(ComponentFamily.Hands);

                if (hands == null || entity == null) return false;

                if (_userInterfaceManager.DragInfo.IsEntity && _userInterfaceManager.DragInfo.IsActive)
                {
                    if (handL.Contains(new Point((int) e.Position.X, (int) e.Position.Y)))
                    {
                        if (!hands.HandSlots.ContainsKey(Hand.Left))
                        {
                            if (hands.HandSlots.ContainsValue(_userInterfaceManager.DragInfo.DragEntity))
                            {
                                if (
                                    hands.HandSlots.First(x => x.Value == _userInterfaceManager.DragInfo.DragEntity).Key ==
                                    Hand.Left) //From me to me, dropped back on same hand.
                                    return false;

                                hands.SendDropEntity(_userInterfaceManager.DragInfo.DragEntity); //Other hand to me.
                            }
                            equipment.DispatchUnEquipItemToSpecifiedHand(_userInterfaceManager.DragInfo.DragEntity.Uid,
                                                                         Hand.Left);
                        }
                        _userInterfaceManager.DragInfo.Reset();
                        return true;
                    }

                    else if (handR.Contains(new Point((int) e.Position.X, (int) e.Position.Y)))
                    {
                        if (!hands.HandSlots.ContainsKey(Hand.Right))
                        {
                            if (hands.HandSlots.ContainsValue(_userInterfaceManager.DragInfo.DragEntity))
                            {
                                if (
                                    hands.HandSlots.First(x => x.Value == _userInterfaceManager.DragInfo.DragEntity).Key ==
                                    Hand.Right) //From me to me, dropped back on same hand
                                    return false;

                                hands.SendDropEntity(_userInterfaceManager.DragInfo.DragEntity); //Other hand to me.
                            }
                            equipment.DispatchUnEquipItemToSpecifiedHand(_userInterfaceManager.DragInfo.DragEntity.Uid,
                                                                         Hand.Right);
                        }
                        _userInterfaceManager.DragInfo.Reset();
                        return true;
                    }
                }
                else
                {
                    if (handL.Contains(new Point((int) e.Position.X, (int) e.Position.Y)) &&
                        hands.HandSlots.ContainsKey(Hand.Left))
                    {
                        hands.HandSlots[Hand.Left].SendMessage(this, ComponentMessageType.ClickedInHand,
                                                               _playerManager.ControlledEntity.Uid);
                    }
                    else if (handR.Contains(new Point((int) e.Position.X, (int) e.Position.Y)) &&
                             hands.HandSlots.ContainsKey(Hand.Right))
                    {
                        hands.HandSlots[Hand.Right].SendMessage(this, ComponentMessageType.ClickedInHand,
                                                                _playerManager.ControlledEntity.Uid);
                    }
                }
            }
            return false;
        }

        public override void MouseMove(MouseInputEventArgs e)
        {
            if (ClientArea.Contains(new Point((int) e.Position.X, (int) e.Position.Y)))
            {
                Entity entity = _playerManager.ControlledEntity;
                var hands = (HumanHandsComponent) entity.GetComponent(ComponentFamily.Hands);
                switch (e.Buttons)
                {
                    case MouseButtons.Left:
                        if (handL.Contains(new Point((int) e.Position.X, (int) e.Position.Y)))
                        {
                            if (hands.HandSlots.Keys.Contains(Hand.Left))
                            {
                                Entity entityL = hands.HandSlots[Hand.Left];
                                _userInterfaceManager.DragInfo.StartDrag(entityL);
                            }
                        }
                        if (handR.Contains(new Point((int) e.Position.X, (int) e.Position.Y)))
                        {
                            if (hands.HandSlots.Keys.Contains(Hand.Right))
                            {
                                Entity entityR = hands.HandSlots[Hand.Right];
                                _userInterfaceManager.DragInfo.StartDrag(entityR);
                            }
                        }
                        break;
                }
            }
        }

        public override bool MouseWheelMove(MouseInputEventArgs e)
        {
            return false;
        }

        public override bool KeyDown(KeyboardInputEventArgs e)
        {
            return false;
        }
    }
}