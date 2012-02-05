using System;
using System.Drawing;
using ClientServices;
using ClientServices.Resources;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;

namespace SS13.UserInterface
{
    class SimpleImageButton : GuiComponent
    {
        public delegate void SimpleImageButtonPressHandler(SimpleImageButton sender);
        public event SimpleImageButtonPressHandler Clicked;

        public Color Color = Color.White;

        private Sprite buttonSprite;

        public SimpleImageButton(string spriteName)
            : base()
        {
            buttonSprite = ServiceManager.Singleton.GetService<ResourceManager>().GetSprite(spriteName);
            Update();
        }

        public override void Update()
        {
            buttonSprite.Position = Position;
            clientArea = new Rectangle(Position, new Size((int)buttonSprite.AABB.Width, (int)buttonSprite.AABB.Height));
        }

        public override void Render()
        {
            buttonSprite.Color = Color;
            buttonSprite.Position = Position;
            buttonSprite.Draw();
            buttonSprite.Color = Color.White;
        }

        public override void Dispose()
        {
            buttonSprite = null;
            Clicked = null;
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            if (clientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y)))
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
