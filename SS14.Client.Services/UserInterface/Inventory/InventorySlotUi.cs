using SS14.Client.Interfaces.Resource;
using SS14.Client.Services.Helpers;
using SS14.Client.Services.UserInterface.Components;
using SS14.Client.Graphics.CluwneLib.Sprite;
using SS14.Shared.GameObjects;
using SFML.Window;
using System;
using System.Drawing;

namespace SS14.Client.Services.UserInterface.Inventory
{
    internal class InventorySlotUi : GuiComponent
    {
        #region Delegates

        public delegate void InventoryClickHandler(InventorySlotUi sender);

        #endregion

		private readonly CluwneSprite _entitySprite;
        private readonly IResourceManager _resourceManager;
		private readonly CluwneSprite _slotSprite;

        public Entity ContainingEntity;
        private Color _currentColor;

        public InventorySlotUi(Entity containingEnt, IResourceManager resourceManager)
        {
            _currentColor = Color.White;
            _resourceManager = resourceManager;
            ContainingEntity = containingEnt;
            if (ContainingEntity != null) _entitySprite = Utilities.GetIconSprite(ContainingEntity);
            _slotSprite = _resourceManager.GetSprite("slot");
        }

        public event InventoryClickHandler Clicked;

        public override void Update(float frameTime)
        {
            ClientArea = new Rectangle(Position, new Size((int) _slotSprite.AABB.Width, (int) _slotSprite.AABB.Height));
        }

        public override void Render()
        {
            _slotSprite.Color = _currentColor;
            _slotSprite.Draw(new Rectangle(Position,
                                           new Size((int) _slotSprite.AABB.Width, (int) _slotSprite.AABB.Height)));
            if (_entitySprite != null)
                _entitySprite.Draw(
                    new Rectangle((int) (Position.X + _slotSprite.AABB.Width/2f - _entitySprite.AABB.Width/2f),
                                  (int) (Position.Y + _slotSprite.AABB.Height/2f - _entitySprite.AABB.Height/2f),
                                  (int) _entitySprite.Width, (int) _entitySprite.Height));
            _slotSprite.Color = Color.White;
        }

        public override void Dispose()
        {
            base.Dispose();
            GC.SuppressFinalize(this);
        }

		public override bool MouseDown(MouseButtonEventArgs e)
        {
            if (ClientArea.Contains(new Point((int) e.Position.X, (int) e.Position.Y)))
            {
                if (Clicked != null) Clicked(this);
                return true;
            }
            return false;
        }

		public override bool MouseUp(MouseButtonEventArgs e)
        {
            if (ClientArea.Contains(new Point((int) e.Position.X, (int) e.Position.Y)))
            {
                return true;
            }
            return false;
        }

		public override void MouseMove(MouseMoveEventArgs e)
        {
            _currentColor = ClientArea.Contains(new Point((int) e.Position.X, (int) e.Position.Y))
                                ? Color.LightSteelBlue
                                : Color.White;
        }
    }
}