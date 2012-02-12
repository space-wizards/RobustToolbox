using System;
using System.Drawing;
using ClientInterfaces;
using ClientInterfaces.Resource;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;

namespace ClientServices.UserInterface.Components
{
    class SimpleImageButton : GuiComponent
    {
        private readonly IResourceManager _resourceManager;
        private Sprite _buttonSprite;

        public delegate void SimpleImageButtonPressHandler(SimpleImageButton sender);
        public event SimpleImageButtonPressHandler Clicked;
        public Color Color { get; set; }

        public SimpleImageButton(string spriteName, IResourceManager resourceManager)
        {
            _resourceManager = resourceManager;
            _buttonSprite = _resourceManager.GetSprite(spriteName);
            Color = Color.White;
            Update();
        }

        public override sealed void Update()
        {
            _buttonSprite.Position = Position;
            ClientArea = new Rectangle(Position, new Size((int)_buttonSprite.AABB.Width, (int)_buttonSprite.AABB.Height));
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
            if (ClientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y)))
            {
                if (Clicked != null) Clicked(this);
                return true;
            }
            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            return false;
        }
    }
}
