using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.UserInterface;
using SS14.Shared.IoC;
using SS14.Client.Graphics.Sprite;
using System;
using SFML.Window;
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
		private CluwneSprite _buttonSprite;

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
            _buttonSprite.Position = new SFML.System.Vector2f(Position.X, Position.Y);
            ClientArea = new Rectangle(Position,
                                       new Size((int) _buttonSprite.Width, (int) _buttonSprite.Height));
        }

        public override void Render()
        {
            _buttonSprite.Color = Color;
            _buttonSprite.Position = new SFML.System.Vector2f(Position.X, Position.Y);
            _buttonSprite.Draw();
            _buttonSprite.Color = Color;
        }

        public override void Dispose()
        {
            _buttonSprite = null;
            Clicked = null;
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
            if (ClientArea.Contains(new Point((int) e.X, (int) e.Y)) &&
                IoCManager.Resolve<IUserInterfaceManager>().DragInfo.IsActive)
            {
                if (Dropped != null) Dropped(this);
                return true;
            }
            return false;
        }
    }
}