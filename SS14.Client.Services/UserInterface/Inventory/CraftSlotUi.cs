using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.Services.Helpers;
using SS14.Client.Services.UserInterface.Components;
using SS14.Client.Graphics.Sprite;
using SS14.Shared.GameObjects;
using System;
using System.Drawing;
using SFML.Window;
using SFML.Graphics;
using SS14.Client.Graphics;
using Color = SFML.Graphics.Color;

namespace SS14.Client.Services.UserInterface.Inventory
{
    internal class CraftSlotUi : GuiComponent
    {
        #region Delegates

        public delegate void CraftSlotClickHandler(CraftSlotUi sender);

        #endregion

        private readonly IResourceManager _resourceManager;
        private readonly Sprite _sprite;
        private readonly IUserInterfaceManager _userInterfaceManager;
        private Color _color;
        private Sprite _entSprite;

        public CraftSlotUi(IResourceManager resourceManager, IUserInterfaceManager userInterfaceManager)
        {
            _resourceManager = resourceManager;
            _userInterfaceManager = userInterfaceManager;
            _sprite = _resourceManager.GetSprite("slot");
            _color = Color.White;
        }

        public Entity ContainingEntity { get; private set; }

        public void SetEntity(Entity entity)
        {
            ContainingEntity = entity;
            if (ContainingEntity != null) _entSprite = Utilities.GetIconSprite(ContainingEntity);
        }

        public void ResetEntity()
        {
            ContainingEntity = null;
            _entSprite = null;
        }

        public override void Update(float frameTime)
        {
            var bounds = _sprite.GetLocalBounds();
            ClientArea = new Rectangle(Position, new Size((int)bounds.Width, (int)bounds.Height));
        }

        public override void Render()
        {
            _sprite.Color = _color;
            var spriteBounds = _sprite.GetLocalBounds();
            _sprite.SetTransformToRect(new Rectangle(Position, new Size((int)spriteBounds.Width, (int)spriteBounds.Height)));
            _sprite.Draw();
            if (_entSprite != null)
            {
                var entBounds = _entSprite.GetLocalBounds();
                _entSprite.SetTransformToRect(new Rectangle((int)(Position.X + spriteBounds.Width / 2f - entBounds.Width / 2f),
                                              (int)(Position.Y + spriteBounds.Height / 2f - entBounds.Height / 2f),
                                              (int)entBounds.Width, (int)entBounds.Height));
                _entSprite.Draw();
            }
            _sprite.Color = Color.White;
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
                ResetEntity();
                return true;
            }
            return false;
        }

        public override bool MouseUp(MouseButtonEventArgs e)
        {
            if (ClientArea.Contains(new Point((int) e.X, (int) e.Y)))
            {
                if (_userInterfaceManager.DragInfo.IsEntity && _userInterfaceManager.DragInfo.IsActive)
                {
                    SetEntity(_userInterfaceManager.DragInfo.DragEntity);
                    _userInterfaceManager.DragInfo.Reset();
                    return true;
                }
            }
            return false;
        }

        public override void MouseMove(MouseMoveEventArgs e)
        {
            if (ClientArea.Contains(new Point((int) e.X, (int) e.Y)))
                _color = new Color(176, 222, 196);
            else
                _color = Color.White;
        }
    }
}