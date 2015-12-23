using SS14.Client.GameObjects;
using SS14.Client.Interfaces.Player;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.Services.Helpers;
using SS14.Client.Services.UserInterface.Components;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using System;
using System.Drawing;
using SS14.Client.Graphics.Sprite;
using SFML.Window;
using SS14.Shared.Maths;

namespace SS14.Client.Services.UserInterface.Inventory
{
    internal class EquipmentSlotUi : GuiComponent
    {
        #region Delegates

        public delegate void InventorySlotUiDropHandler(EquipmentSlotUi sender, Entity dropped);

        #endregion

        private readonly IPlayerManager _playerManager;
        private readonly IResourceManager _resourceManager;
        private readonly TextSprite _textSprite;
        private readonly IUserInterfaceManager _userInterfaceManager;
		private CluwneSprite _buttonSprite;
        private Color _color;
		private CluwneSprite _currentEntSprite;

        public EquipmentSlotUi(EquipmentSlot slot, IPlayerManager playerManager, IResourceManager resourceManager,
                               IUserInterfaceManager userInterfaceManager)
        {
            _playerManager = playerManager;
            _resourceManager = resourceManager;
            _userInterfaceManager = userInterfaceManager;

            _color = Color.White;

            AssignedSlot = slot;
            _buttonSprite = _resourceManager.GetSprite("slot");
            _textSprite = new TextSprite(slot + "UIElementSlot", slot.ToString(),
                                         _resourceManager.GetFont("CALIBRI"))
                              {
                                  ShadowColor = Color.Black,
                                  ShadowOffset = new Vector2(1, 1),
                                  Shadowed = true,
                                  Color = Color.White
                              };

            Update(0);
        }

        public EquipmentSlot AssignedSlot { get; private set; }
        public Entity CurrentEntity { get; private set; }
        public event InventorySlotUiDropHandler Dropped;

        public override sealed void Update(float frameTime)
        {
            _buttonSprite.Position = new Vector2(Position.X,Position.Y);
            ClientArea = new Rectangle(Position,
                                       new Size((int) _buttonSprite.Width, (int) _buttonSprite.Height));

            _textSprite.Position = Position;

            if (_playerManager.ControlledEntity == null)
                return;

            Entity entity = _playerManager.ControlledEntity;
            var equipment = (EquipmentComponent) entity.GetComponent(ComponentFamily.Equipment);

            if (equipment.EquippedEntities.ContainsKey(AssignedSlot))
            {
                if ((CurrentEntity == null || CurrentEntity.Uid != equipment.EquippedEntities[AssignedSlot].Uid))
                {
                    CurrentEntity = equipment.EquippedEntities[AssignedSlot];
                    _currentEntSprite = Utilities.GetIconSprite(CurrentEntity);
                }
            }
            else
            {
                CurrentEntity = null;
                _currentEntSprite = null;
            }
        }

        public override void Render()
        {
            _buttonSprite.Color = _color;
            _buttonSprite.Position = new Vector2(Position.X,Position.Y);
            _buttonSprite.Draw();
            _buttonSprite.Color = Color.White;

            if (_currentEntSprite != null && CurrentEntity != null)
                _currentEntSprite.Draw(
                    new Rectangle((int) (Position.X + _buttonSprite.Width/2f - _currentEntSprite.Width/2f),
                                  (int) (Position.Y + _buttonSprite.Height/2f - _currentEntSprite.Height/2f),
                                  (int) _currentEntSprite.Width, (int) _currentEntSprite.Height));

            _textSprite.Draw();
        }

        public override void Dispose()
        {
            _buttonSprite = null;
            Dropped = null;
            base.Dispose();
            GC.SuppressFinalize(this);
        }

		public override bool MouseDown(MouseButtonEventArgs e)
        {
            if (ClientArea.Contains(new Point((int) e.X, (int) e.Y)))
            {
                if (_playerManager.ControlledEntity == null)
                    return false;

                Entity entity = _playerManager.ControlledEntity;
                var equipment = (EquipmentComponent) entity.GetComponent(ComponentFamily.Equipment);

                if (equipment.EquippedEntities.ContainsKey(AssignedSlot))
                    _userInterfaceManager.DragInfo.StartDrag(equipment.EquippedEntities[AssignedSlot]);

                return true;
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

                if (CurrentEntity != null && CurrentEntity == _userInterfaceManager.DragInfo.DragEntity &&
                    hands.IsHandEmpty(hands.CurrentHand)) //Dropped from us to us. (Try to) unequip it to active hand.
                {
                    _userInterfaceManager.DragInfo.Reset();
                    equipment.DispatchUnEquipToHand(CurrentEntity.Uid);
                    return true;
                }

                if (CurrentEntity == null && _userInterfaceManager.DragInfo.IsEntity &&
                    _userInterfaceManager.DragInfo.IsActive)
                {
                    if (Dropped != null) Dropped(this, _userInterfaceManager.DragInfo.DragEntity);
                    return true;
                }
            }

            return false;
        }

		public override void MouseMove(MouseMoveEventArgs e)
        {
            _color = ClientArea.Contains(new Point((int) e.X, (int) e.Y))
                         ? Color.LightSteelBlue
                         : Color.White;
        }

        private bool IsEmpty()
        {
            if (_playerManager.ControlledEntity == null)
                return false;

            Entity entity = _playerManager.ControlledEntity;
            var equipment = (EquipmentComponent) entity.GetComponent(ComponentFamily.Equipment);

            return equipment.IsEmpty(AssignedSlot);
        }
    }
}