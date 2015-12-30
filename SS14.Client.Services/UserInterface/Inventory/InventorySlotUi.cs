using SS14.Client.Interfaces.Resource;
using SS14.Client.Services.Helpers;
using SS14.Client.Services.UserInterface.Components;
using SS14.Client.Graphics.Sprite;
using SS14.Shared.GameObjects;
using SFML.Window;
using System;
using System.Drawing;
using SFML.Graphics;
using SFML.System;
using SS14.Client.Graphics;
using Color = SFML.Graphics.Color;

namespace SS14.Client.Services.UserInterface.Inventory
{
    internal class InventorySlotUi : GuiComponent
    {
        #region Delegates

        public delegate void InventoryClickHandler(InventorySlotUi sender);

        #endregion

        private readonly Sprite _entitySprite;
        private readonly IResourceManager _resourceManager;
        private readonly Sprite _slotSprite;

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
            var bounds = _slotSprite.GetLocalBounds();
            ClientArea = new Rectangle(Position, new Size((int)bounds.Width, (int)bounds.Height));
        }

        public override void Render()
        {
            _slotSprite.Color = _currentColor;
            _slotSprite.Position = new Vector2f(Position.X, Position.Y);
            _slotSprite.Draw();

            if (_entitySprite != null)
            {
                var slotBounds = _slotSprite.GetLocalBounds();
                var entBounds = _entitySprite.GetLocalBounds();
                _entitySprite.SetTransformToRect (
                    new Rectangle((int)(Position.X + slotBounds.Width / 2f - entBounds.Width / 2f),
                                  (int)(Position.Y + slotBounds.Height / 2f - entBounds.Height / 2f),
                                  (int)entBounds.Width, (int)entBounds.Height));
                _entitySprite.Draw();
            }
            _slotSprite.Color = Color.White;
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
                                ? new SFML.Graphics.Color(176, 196, 222)
                                : Color.White;
        }
    }
}