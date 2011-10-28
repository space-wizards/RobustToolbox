using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using GorgonLibrary.GUI;
using SS3D.UserInterface;
using Lidgren.Network;
using SS3D_shared;
using ClientResourceManager;

namespace SS3D.UserInterface
{
    class SimpleImageButton : GuiComponent
    {
        public delegate void SimpleImageButtonPressHandler(SimpleImageButton sender);
        public event SimpleImageButtonPressHandler Clicked;

        private Sprite buttonSprite;

        public SimpleImageButton(string spriteName)
            : base()
        {
            buttonSprite = ResMgr.Singleton.GetSprite(spriteName);
            Update();
        }

        public override void Update()
        {
            buttonSprite.Position = Position;
            clientArea = new Rectangle(Position, new Size((int)buttonSprite.AABB.Width, (int)buttonSprite.AABB.Height));
        }

        public override void Render()
        {
            buttonSprite.Draw();
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
