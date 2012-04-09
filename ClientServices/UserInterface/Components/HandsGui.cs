using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using ClientInterfaces;
using ClientInterfaces.GOC;
using ClientInterfaces.Network;
using ClientInterfaces.Player;
using ClientInterfaces.Resource;
using ClientInterfaces.UserInterface;
using ClientServices.Helpers;
using ClientServices.UserInterface.Components;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using Lidgren.Network;
using CGO;
using SS13_Shared.GO;
using SS13_Shared;
using SS13.IoC;
using System;

namespace ClientServices.UserInterface.Components
{
    public struct UiHandInfo
    {
        public Hand Hand;
        public IEntity Entity;
        public Sprite HeldSprite;
    }

    public class HandsGui : GuiComponent
    {
        Sprite handSlot;
        private readonly int spacing = 1;

        Rectangle handL = new Rectangle();
        Rectangle handR = new Rectangle();

        public UiHandInfo LeftHand;
        public UiHandInfo RightHand;

        private readonly Color _inactiveColor = Color.FromArgb(255, 90, 90, 90);

        IPlayerManager _playerManager = IoCManager.Resolve<IPlayerManager>();
        IUserInterfaceManager _userInterfaceManager = IoCManager.Resolve<IUserInterfaceManager>();

        public HandsGui() : base ()
        {
            IResourceManager _resMgr = IoCManager.Resolve<IResourceManager>();
            ComponentClass = GuiComponentType.HandsUi;
            handSlot = _resMgr.GetSprite("hand");
        }

        public override void ComponentUpdate(params object[] args)
        {
            base.ComponentUpdate(args);
            UpdateHandIcons();
        }

        public override void Update()
        {
            handL = new Rectangle(Position.X, Position.Y, (int)handSlot.Width, (int)handSlot.Height);
            handR = new Rectangle(Position.X + (int)handSlot.Width + spacing, Position.Y, (int)handSlot.Width, (int)handSlot.Height);
            ClientArea = new Rectangle(Position.X, Position.Y, (int)((handSlot.Width * 2) + spacing), (int)handSlot.Height);

        }

        public override void Render()
        {
            if (_playerManager == null || _playerManager.ControlledEntity == null)
                return;

            var entity = _playerManager.ControlledEntity;
            var hands = (HumanHandsComponent)entity.GetComponent(ComponentFamily.Hands);

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
                LeftHand.HeldSprite.Draw(new Rectangle((int)handL.X + (int)(handL.Width / 2f - LeftHand.HeldSprite.AABB.Width / 2f), (int)handL.Y + (int)(handL.Height / 2f - LeftHand.HeldSprite.AABB.Height / 2f), (int)LeftHand.HeldSprite.AABB.Width, (int)LeftHand.HeldSprite.AABB.Height));

            if (RightHand.Entity != null && RightHand.HeldSprite != null)
                RightHand.HeldSprite.Draw(new Rectangle((int)handR.X + (int)(handR.Width / 2f- RightHand.HeldSprite.AABB.Width / 2f), (int)handR.Y + (int)(handR.Height / 2f - RightHand.HeldSprite.AABB.Height / 2f), (int)RightHand.HeldSprite.AABB.Width, (int)RightHand.HeldSprite.AABB.Height));
        }

        public override void Resize()
        {
        }

        public override void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public override void HandleNetworkMessage(Lidgren.Network.NetIncomingMessage message)
        {
        }

        public void UpdateHandIcons()
        {
            if (_playerManager.ControlledEntity == null)
                return;

            var entity = _playerManager.ControlledEntity;
            var hands = (HumanHandsComponent)entity.GetComponent(ComponentFamily.Hands);

            if (hands == null) return;

            if (hands.HandSlots.Keys.Contains(Hand.Left))
            {
                if (LeftHand.Entity == null || LeftHand.Entity.Uid != hands.HandSlots[Hand.Left].Uid)
                {
                    var entityL = hands.HandSlots[Hand.Left];
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
                    var entityR = hands.HandSlots[Hand.Right];
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
            IPlayerManager _playerManager = IoCManager.Resolve<IPlayerManager>();

            var playerEntity = _playerManager.ControlledEntity;
            var equipComponent = (HumanHandsComponent)playerEntity.GetComponent(ComponentFamily.Hands);
            equipComponent.SendSwitchHands(hand);
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            var entity = _playerManager.ControlledEntity;
            var hands = (HumanHandsComponent)entity.GetComponent(ComponentFamily.Hands);
            switch (e.Buttons)
            {
                case MouseButtons.Right:
                    if (handL.Contains(new Point((int)e.Position.X, (int)e.Position.Y)))
                    {
                        SendSwitchHandTo(Hand.Left);
                        return true;
                    }
                    if (handR.Contains(new Point((int)e.Position.X, (int)e.Position.Y)))
                    {
                        SendSwitchHandTo(Hand.Right);
                        return true;
                    }
                    break;

                case MouseButtons.Left:
                    if (handL.Contains(new Point((int)e.Position.X, (int)e.Position.Y)))
                    {
                        if (hands.HandSlots.Keys.Contains(Hand.Left))
                        {
                            var entityL = hands.HandSlots[Hand.Left];
                            _userInterfaceManager.DragInfo.StartDrag(entityL);
                        }
                        return true;
                    }
                    if (handR.Contains(new Point((int)e.Position.X, (int)e.Position.Y)))
                    {
                        if (hands.HandSlots.Keys.Contains(Hand.Right))
                        {
                            var entityR = hands.HandSlots[Hand.Right];
                            _userInterfaceManager.DragInfo.StartDrag(entityR);
                        }
                        return true;
                    }
                    break;
            }
            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            if (ClientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y)))
            {
                if (_userInterfaceManager.DragInfo.IsEntity && _userInterfaceManager.DragInfo.IsActive)
                {
                    if (_playerManager.ControlledEntity == null)
                        return false;

                    var entity = _playerManager.ControlledEntity;

                    var equipment = (EquipmentComponent)entity.GetComponent(ComponentFamily.Equipment);
                    var hands = (HumanHandsComponent)entity.GetComponent(ComponentFamily.Hands);

                    if (hands == null || entity == null) return false;

                    if (handL.Contains(new Point((int)e.Position.X, (int)e.Position.Y)))
                    {
                        if (!hands.HandSlots.ContainsKey(Hand.Left))
                        {
                            if (hands.HandSlots.ContainsValue(_userInterfaceManager.DragInfo.DragEntity))
                            {
                                if (hands.HandSlots.First(x => x.Value == _userInterfaceManager.DragInfo.DragEntity).Key == Hand.Left) //From me to me, ignore.
                                    return false;

                                hands.SendDropEntity(_userInterfaceManager.DragInfo.DragEntity); //Other hand to me.

                            }
                            equipment.DispatchUnEquipItemToSpecifiedHand(_userInterfaceManager.DragInfo.DragEntity.Uid, Hand.Left);
                            _userInterfaceManager.DragInfo.Reset();
                            return true;
                        }
                    }
                    else if (handR.Contains(new Point((int)e.Position.X, (int)e.Position.Y)))
                    {
                        if (!hands.HandSlots.ContainsKey(Hand.Right))
                        {
                            if (hands.HandSlots.ContainsValue(_userInterfaceManager.DragInfo.DragEntity))
                            {
                                if (hands.HandSlots.First(x => x.Value == _userInterfaceManager.DragInfo.DragEntity).Key == Hand.Right) //From me to me, ignore.
                                    return false;

                                hands.SendDropEntity(_userInterfaceManager.DragInfo.DragEntity); //Other hand to me.
                            }
                            equipment.DispatchUnEquipItemToSpecifiedHand(_userInterfaceManager.DragInfo.DragEntity.Uid, Hand.Right);
                            _userInterfaceManager.DragInfo.Reset();
                            return true;
                        }
                    } 
                }
            }
            return false;
        }

        public override void MouseMove(MouseInputEventArgs e)
        {
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
