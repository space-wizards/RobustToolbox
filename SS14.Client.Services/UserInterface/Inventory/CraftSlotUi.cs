using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.Services.Helpers;
using SS14.Client.Services.UserInterface.Components;
using SS14.Client.Graphics.Sprite;
using SS14.Shared.GameObjects;
using System;
using System.Drawing;
using SFML.Window;

namespace SS14.Client.Services.UserInterface.Inventory
{
    internal class CraftSlotUi : GuiComponent
    {
        #region Delegates

        public delegate void CraftSlotClickHandler(CraftSlotUi sender);

        #endregion

        private readonly IResourceManager _resourceManager;
		private readonly CluwneSprite _sprite;
        private readonly IUserInterfaceManager _userInterfaceManager;
        private Color _color;
		private CluwneSprite _entSprite;

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
            ClientArea = new Rectangle(Position, new Size((int) _sprite.Width, (int) _sprite.Height));
        }

        public override void Render()
        {
            _sprite.Color = new SFML.Graphics.Color(_color.R, _color.G, _color.B, _color.A); ;
            _sprite.Draw(new Rectangle(Position, new Size((int) _sprite.Width, (int) _sprite.Height)));
            if (_entSprite != null)
                _entSprite.Draw(new Rectangle((int) (Position.X + _sprite.Width/2f - _entSprite.Width/2f),
                                              (int) (Position.Y + _sprite.Height/2f - _entSprite.Height/2f),
                                              (int) _entSprite.Width, (int) _entSprite.Height));
            _sprite.Color = new SFML.Graphics.Color(Color.White.R, Color.White.G, Color.White.B, Color.White.A);
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
                _color = Color.LightSteelBlue;
            else
                _color = Color.White;
        }
    }
}