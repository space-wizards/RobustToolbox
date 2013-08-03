using System;
using System.Drawing;
using ClientInterfaces;
using ClientInterfaces.GOC;
using ClientInterfaces.Resource;
using ClientServices.Helpers;
using ClientServices.UserInterface.Components;
using GameObject;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;

namespace ClientServices.UserInterface.Inventory
{
    class InventorySlotUi : GuiComponent
    {
        private readonly IResourceManager _resourceManager;
        private readonly Sprite _entitySprite;
        private readonly Sprite _slotSprite;
        private Color _currentColor;

        public Entity ContainingEntity;
        public delegate void InventoryClickHandler(InventorySlotUi sender);
        public event InventoryClickHandler Clicked;

        public InventorySlotUi(Entity containingEnt, IResourceManager resourceManager)
        {
            _currentColor = Color.White;
            _resourceManager = resourceManager;
            ContainingEntity = containingEnt;
            if (ContainingEntity != null) _entitySprite = Utilities.GetSpriteComponentSprite(ContainingEntity);
            _slotSprite = _resourceManager.GetSprite("slot");
        }

        public override void Update(float frameTime)
        {
            ClientArea = new Rectangle(Position, new Size((int)_slotSprite.AABB.Width, (int)_slotSprite.AABB.Height));
        }

        public override void Render()
        {
            _slotSprite.Color = _currentColor;
            _slotSprite.Draw(new Rectangle(Position, new Size((int)_slotSprite.AABB.Width, (int)_slotSprite.AABB.Height)));
            if (_entitySprite != null) 
                _entitySprite.Draw(new Rectangle((int)(Position.X + _slotSprite.AABB.Width / 2f - _entitySprite.AABB.Width / 2f), (int)(Position.Y + _slotSprite.AABB.Height / 2f - _entitySprite.AABB.Height / 2f), (int)_entitySprite.Width, (int)_entitySprite.Height));
            _slotSprite.Color = Color.White;
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
                if (Clicked != null) Clicked(this);
                return true;
            }
            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            if (ClientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y)))
            {
                return true;
            }
            return false;
        }

        public override void MouseMove(MouseInputEventArgs e)
        {
            _currentColor = ClientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y)) ? Color.LightSteelBlue : Color.White;
        }
    }
}
