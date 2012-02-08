using System;
using System.Drawing;
using ClientInterfaces;
using ClientInterfaces.GOC;
using ClientInterfaces.UserInterface;
using ClientServices.Helpers;
using ClientServices.UserInterface.Components;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;

namespace ClientServices.UserInterface.Inventory
{
    class CraftSlotUi : GuiComponent
    {
        private readonly IResourceManager _resourceManager;
        private readonly IUserInterfaceManager _userInterfaceManager;
        private readonly Sprite _sprite;
        private Sprite _entSprite;
        private Color _color;

        public IEntity ContainingEntity { get; private set; }

        public delegate void CraftSlotClickHandler(CraftSlotUi sender);

        public CraftSlotUi(IResourceManager resourceManager, IUserInterfaceManager userInterfaceManager)
        {
            _resourceManager = resourceManager;
            _userInterfaceManager = userInterfaceManager;
            _sprite = _resourceManager.GetSprite("slot");
            _color = Color.White;
        }

        public void SetEntity(IEntity entity)
        {
            ContainingEntity = entity;
            if (ContainingEntity != null) _entSprite = Utilities.GetSpriteComponentSprite(ContainingEntity);
        }

        public void ResetEntity()
        {
            ContainingEntity = null;
            _entSprite = null;
        }

        public override void Update()
        {
            ClientArea = new Rectangle(Position, new Size((int)_sprite.AABB.Width, (int)_sprite.AABB.Height));
        }

        public override void Render()
        {
            _sprite.Color = _color;
            _sprite.Draw(new Rectangle(Position, new Size((int)_sprite.AABB.Width, (int)_sprite.AABB.Height)));
            if (_entSprite != null) 
                _entSprite.Draw(new Rectangle((int)(Position.X + _sprite.AABB.Width / 2f - _entSprite.AABB.Width / 2f), (int)(Position.Y + _sprite.AABB.Height / 2f - _entSprite.AABB.Height / 2f), (int)_entSprite.Width, (int)_entSprite.Height));
            _sprite.Color = Color.White;
        }

        public override void Dispose()
        {
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            if (ClientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y)))
            {
                ResetEntity();
                return true;
            }
            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            if (ClientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y)))
            {
                if (_userInterfaceManager.DragInfo.IsEntity && _userInterfaceManager.DragInfo.DragEntity != null)
                {
                    SetEntity(_userInterfaceManager.DragInfo.DragEntity);
                    _userInterfaceManager.DragInfo.Reset();
                    return true;
                }
            }
            return false;
        }

        public override void MouseMove(MouseInputEventArgs e)
        {
            if (ClientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y)))
                _color = Color.LightSteelBlue;
            else
                _color = Color.White;
            
        }
    }
}
