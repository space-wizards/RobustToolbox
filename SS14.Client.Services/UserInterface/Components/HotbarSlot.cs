using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.UserInterface;
using SS14.Shared.IoC;
using System;
using System.Drawing;

namespace SS14.Client.Services.UserInterface.Components
{
    internal class HotbarSlot : GuiComponent
    {
        #region Delegates

        public delegate void HotbarSlotDropHandler(HotbarSlot sender);

        public delegate void HotbarSlotPressHandler(HotbarSlot sender);

        #endregion

        private readonly IResourceManager _resourceManager;
        private Sprite _buttonSprite;

        public HotbarSlot(IResourceManager resourceManager)
        {
            _resourceManager = resourceManager;
            _buttonSprite = _resourceManager.GetSprite("hotbar_slot");
            Color = Color.White;
            Update(0);
        }

        public Color Color { get; set; }

        public event HotbarSlotPressHandler Clicked;

        public event HotbarSlotDropHandler Dropped;

        public override sealed void Update(float frameTime)
        {
            _buttonSprite.Position = Position;
            ClientArea = new Rectangle(Position,
                                       new Size((int) _buttonSprite.AABB.Width, (int) _buttonSprite.AABB.Height));
        }

        public override void Render()
        {
            _buttonSprite.Color = Color;
            _buttonSprite.Position = Position;
            _buttonSprite.Draw();
            _buttonSprite.Color = Color.White;
        }

        public override void Dispose()
        {
            _buttonSprite = null;
            Clicked = null;
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            if (ClientArea.Contains(new Point((int) e.Position.X, (int) e.Position.Y)))
            {
                if (Clicked != null) Clicked(this);
                return true;
            }
            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            if (ClientArea.Contains(new Point((int) e.Position.X, (int) e.Position.Y)) &&
                IoCManager.Resolve<IUserInterfaceManager>().DragInfo.IsActive)
            {
                if (Dropped != null) Dropped(this);
                return true;
            }
            return false;
        }
    }
}