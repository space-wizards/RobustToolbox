using SS14.Client.Interfaces.Resource;
using SS14.Client.Services.Helpers;
using SS14.Client.Services.UserInterface.Components;
using SS14.Client.Graphics.Sprite;
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
            ClientArea = new Rectangle(Position, new Size((int) _slotSprite.Width, (int) _slotSprite.Height));
        }

        public override void Render()
        {
            _slotSprite.Color = new SFML.Graphics.Color(_currentColor.R, _currentColor.G, _currentColor.B, _currentColor.A); ;
            _slotSprite.Draw(new Rectangle(Position,
                                           new Size((int) _slotSprite.Width, (int) _slotSprite.Height)));
            if (_entitySprite != null)
                _entitySprite.Draw(
                    new Rectangle((int) (Position.X + _slotSprite.Width/2f - _entitySprite.Width/2f),
                                  (int) (Position.Y + _slotSprite.Height/2f - _entitySprite.Height/2f),
                                  (int) _entitySprite.Width, (int) _entitySprite.Height));
            _slotSprite.Color = new SFML.Graphics.Color(Color.White.R, Color.White.G, Color.White.B, Color.White.A);
        }

        public override void Dispose()
        {
            base.Dispose();
            GC.SuppressFinalize(this);
        }

		public override bool MouseDown(MouseButtonEventArgs e)
        {
            if (ClientArea.Contains(new Point((int) e.X, (int) e.Y)))
            {
                if (Clicked != null) Clicked(this);
                return true;
            }
            return false;
        }

		public override bool MouseUp(MouseButtonEventArgs e)
        {
            if (ClientArea.Contains(new Point((int) e.X, (int) e.Y)))
            {
                return true;
            }
            return false;
        }

		public override void MouseMove(MouseMoveEventArgs e)
        {
            _currentColor = ClientArea.Contains(new Point((int) e.X, (int) e.Y))
                                ? Color.LightSteelBlue
                                : Color.White;
        }
    }
}