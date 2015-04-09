using Lidgren.Network;
using SFML.Window;
using SS14.Client.GameObjects;
using SS14.Client.Graphics.CluwneLib.Sprite;
using SS14.Client.Interfaces.Player;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.Services.Helpers;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using SS14.Shared.IoC;
using System;
using System.Drawing;
using System.Linq;
using Color = System.Drawing.Color;

namespace SS14.Client.Services.UserInterface.Components
{
    public struct UiHandInfo
    {
        public Entity Entity;
        public InventoryLocation Hand;
		public CluwneSprite HeldSprite;
    }

    public class HandsGui : GuiComponent
    {
        private readonly Color _inactiveColor = Color.FromArgb(255, 90, 90, 90);

        private readonly IPlayerManager _playerManager = IoCManager.Resolve<IPlayerManager>();
        private readonly IUserInterfaceManager _userInterfaceManager = IoCManager.Resolve<IUserInterfaceManager>();
		private readonly CluwneSprite handSlot;
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

            if (hands.CurrentHand == InventoryLocation.HandLeft)
            {
                handSlot.Color = new SFML.Graphics.Color(Color.White.R, Color.White.G, Color.White.B, Color.White.A);
                handSlot.Draw(handL);

                handSlot.Color = new SFML.Graphics.Color(_inactiveColor.R, _inactiveColor.G, _inactiveColor.B, _inactiveColor.A);
                handSlot.Draw(handR);
            }
            else
            {
                handSlot.Color = new SFML.Graphics.Color(Color.White.R, Color.White.G, Color.White.B, Color.White.A); ;
                handSlot.Draw(handR);

                handSlot.Color = new SFML.Graphics.Color(_inactiveColor.R, _inactiveColor.G, _inactiveColor.B, _inactiveColor.A);
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
            base.Dispose();
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

            if (hands.HandSlots.Keys.Contains(InventoryLocation.HandLeft) && hands.HandSlots[InventoryLocation.HandLeft] != null)
            {
                if (LeftHand.Entity == null || LeftHand.Entity.Uid != hands.HandSlots[InventoryLocation.HandLeft].Uid)
                {
                    Entity entityL = hands.HandSlots[InventoryLocation.HandLeft];
                    LeftHand.Entity = entityL;
                    LeftHand.HeldSprite = Utilities.GetIconSprite(entityL);
                }
            }
            else
            {
                LeftHand.Entity = null;
                LeftHand.HeldSprite = null;
            }

            if (hands.HandSlots.Keys.Contains(InventoryLocation.HandRight) && hands.HandSlots[InventoryLocation.HandRight] != null)
            {
                if (RightHand.Entity == null || RightHand.Entity.Uid != hands.HandSlots[InventoryLocation.HandRight].Uid)
                {
                    Entity entityR = hands.HandSlots[InventoryLocation.HandRight];
                    RightHand.Entity = entityR;
                    RightHand.HeldSprite = Utilities.GetIconSprite(entityR);
                }
            }
            else
            {
                RightHand.Entity = null;
                RightHand.HeldSprite = null;
            }
        }

        private void SendSwitchHandTo(InventoryLocation hand)
        {
            var _playerManager = IoCManager.Resolve<IPlayerManager>();

            Entity playerEntity = _playerManager.ControlledEntity;
            var equipComponent = (HumanHandsComponent) playerEntity.GetComponent(ComponentFamily.Hands);
            equipComponent.SendSwitchHands(hand);
        }

		public override bool MouseDown(MouseButtonEventArgs e)
        {
            switch (e.Button)
            {
                case Mouse.Button.Right:
                    if (handL.Contains(new Point((int) e.X, (int) e.Y)))
                    {
                        SendSwitchHandTo(InventoryLocation.HandLeft);
                        return true;
                    }
                    if (handR.Contains(new Point((int) e.X, (int) e.Y)))
                    {
                        SendSwitchHandTo(InventoryLocation.HandRight);
                        return true;
                    }
                    break;
            }
            return false;
        }

		public override bool MouseUp(MouseButtonEventArgs e)
        {
            if (ClientArea.Contains(new Point((int) e.X, (int) e.Y)))
            {
                if (_playerManager.ControlledEntity == null)
                    return false;

                Entity entity = _playerManager.ControlledEntity;

                var equipment = (EquipmentComponent) entity.GetComponent(ComponentFamily.Equipment);
                var hands = (HumanHandsComponent) entity.GetComponent(ComponentFamily.Hands);

                if (hands == null || entity == null) return false;

                if (_userInterfaceManager.DragInfo.IsEntity && _userInterfaceManager.DragInfo.IsActive)
                {
                    if (handL.Contains(new Point((int) e.X, (int) e.Y)))
                    {
                        if (hands.HandSlots.ContainsKey(InventoryLocation.HandLeft) && hands.HandSlots[InventoryLocation.HandLeft] == null)
                        {
                            if (hands.HandSlots.ContainsValue(_userInterfaceManager.DragInfo.DragEntity))
                            {
                                if (
                                    hands.HandSlots.First(x => x.Value == _userInterfaceManager.DragInfo.DragEntity).Key ==
                                    InventoryLocation.HandLeft) //From me to me, dropped back on same hand.
                                    return false;

                                hands.SendDropEntity(_userInterfaceManager.DragInfo.DragEntity); //Other hand to me.
                            }
                            equipment.DispatchUnEquipItemToSpecifiedHand(_userInterfaceManager.DragInfo.DragEntity.Uid,
                                                                         InventoryLocation.HandLeft);
                        }
                        _userInterfaceManager.DragInfo.Reset();
                        return true;
                    }

                    else if (handR.Contains(new Point((int) e.X, (int) e.Y)))
                    {
                        if (hands.HandSlots.ContainsKey(InventoryLocation.HandRight) && hands.HandSlots[InventoryLocation.HandRight] == null)
                        {
                            if (hands.HandSlots.ContainsValue(_userInterfaceManager.DragInfo.DragEntity))
                            {
                                if (
                                    hands.HandSlots.First(x => x.Value == _userInterfaceManager.DragInfo.DragEntity).Key ==
                                    InventoryLocation.HandRight) //From me to me, dropped back on same hand
                                    return false;

                                hands.SendDropEntity(_userInterfaceManager.DragInfo.DragEntity); //Other hand to me.
                            }
                            equipment.DispatchUnEquipItemToSpecifiedHand(_userInterfaceManager.DragInfo.DragEntity.Uid,
                                                                         InventoryLocation.HandRight);
                        }
                        _userInterfaceManager.DragInfo.Reset();
                        return true;
                    }
                }
                else
                {
                    if (handL.Contains(new Point((int) e.X, (int) e.Y)) &&
                        hands.HandSlots.ContainsKey(InventoryLocation.HandLeft) && hands.HandSlots[InventoryLocation.HandRight] != null)
                    {
                        hands.HandSlots[InventoryLocation.HandLeft].SendMessage(this, ComponentMessageType.ClickedInHand,
                                                               _playerManager.ControlledEntity.Uid);
                    }
                    else if (handR.Contains(new Point((int) e.X, (int) e.Y)) &&
                             hands.HandSlots.ContainsKey(InventoryLocation.HandRight) && hands.HandSlots[InventoryLocation.HandRight] != null)
                    {
                        hands.HandSlots[InventoryLocation.HandRight].SendMessage(this, ComponentMessageType.ClickedInHand,
                                                                _playerManager.ControlledEntity.Uid);
                    }
                }
            }
            return false;
        }

        public void MouseMove(MouseButtonEventArgs e)
        {
            if (ClientArea.Contains(new Point((int) e.X, (int) e.Y)))
            {
                Entity entity = _playerManager.ControlledEntity;
                var hands = (HumanHandsComponent) entity.GetComponent(ComponentFamily.Hands);
                switch (e.Button)
                {
                    case Mouse.Button.Left:
                        if (handL.Contains(new Point((int) e.X, (int) e.Y)))
                        {
                            if (hands.HandSlots.Keys.Contains(InventoryLocation.HandLeft) && hands.HandSlots[InventoryLocation.HandLeft] != null)
                            {
                                Entity entityL = hands.HandSlots[InventoryLocation.HandLeft];
                                _userInterfaceManager.DragInfo.StartDrag(entityL);
                            }
                        }
                        if (handR.Contains(new Point((int) e.X, (int) e.Y)))
                        {
                            if (hands.HandSlots.Keys.Contains(InventoryLocation.HandRight) && hands.HandSlots[InventoryLocation.HandRight] != null)
                            {
                                Entity entityR = hands.HandSlots[InventoryLocation.HandRight];
                                _userInterfaceManager.DragInfo.StartDrag(entityR);
                            }
                        }
                        break;
                }
            }
        }

		public override bool MouseWheelMove(MouseWheelEventArgs e)
        {
            return false;
        }

		public override bool KeyDown(KeyEventArgs e)
        {
            return false;
        }
    }
}