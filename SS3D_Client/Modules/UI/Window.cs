using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

using GorgonLibrary;
using GorgonLibrary.Framework;
using GorgonLibrary.GUI;
using GorgonLibrary.Graphics;
using GorgonLibrary.Graphics.Utilities;
using GorgonLibrary.InputDevices;

namespace SS3D.Modules.UI
{
    // This is just an empty window we can use for GUI shit, in the proper style.
    public class WindowComponent : GuiComponent
    {
        public override Point Position
        {
            get
            {
                return base.Position;
            }
            set
            {
                base.Position = value;
            }
        }

        private Sprite backgroundSprite;
        private GorgonLibrary.GUI.GUISkin Skin;
        private int width = 1;
        private int height = 1;
        public bool visible = true;


        public WindowComponent(PlayerController _playerController, int x, int y, int _width, int _height)
            : base(_playerController)
        {
            Position = new Point(x, y);
            width = _width;
            height = _height;
            backgroundSprite = ResMgr.Singleton.GetSprite("1pxwhite");
            Skin = UIDesktop.Singleton.Skin;
        }

        public override void Render()
        {
            if (Skin == null || playerController.controlledAtom == null || !visible)
                return;

            backgroundSprite.Color = System.Drawing.Color.FromArgb(51, 56, 64);
            backgroundSprite.Opacity = 240;
            backgroundSprite.Position = Position;
            backgroundSprite.Size = new Vector2D(width, height);
            backgroundSprite.Draw();

            Skin.Elements["Window.Border.Top.LeftCorner"].Draw(new System.Drawing.Rectangle(Position.X, Position.Y, Skin.Elements["Window.Border.Top.LeftCorner"].Dimensions.Width, Skin.Elements["Window.Border.Top.LeftCorner"].Dimensions.Height));
            Skin.Elements["Window.Border.Top.Horizontal"].Draw(new System.Drawing.Rectangle(Position.X + Skin.Elements["Window.Border.Top.LeftCorner"].Dimensions.Width, Position.Y, width - Skin.Elements["Window.Border.Top.RightCorner"].Dimensions.Width - Skin.Elements["Window.Border.Top.RightCorner"].Dimensions.Width, Skin.Elements["Window.Border.Top.Horizontal"].Dimensions.Height));
            Skin.Elements["Window.Border.Top.RightCorner"].Draw(new System.Drawing.Rectangle(Position.X + width - Skin.Elements["Window.Border.Top.RightCorner"].Dimensions.Width, Position.Y, Skin.Elements["Window.Border.Top.RightCorner"].Dimensions.Width, Skin.Elements["Window.Border.Top.RightCorner"].Dimensions.Height));

            Skin.Elements["Window.Border.Vertical.Left"].Draw(new System.Drawing.Rectangle(Position.X, Skin.Elements["Window.Border.Top.LeftCorner"].Dimensions.Height + Position.Y, Skin.Elements["Window.Border.Vertical.Left"].Dimensions.Width, height - Skin.Elements["Window.Border.Top.LeftCorner"].Dimensions.Height - Skin.Elements["Window.Border.Bottom.LeftCorner"].Dimensions.Height));
            Skin.Elements["Window.Border.Vertical.Right"].Draw(new System.Drawing.Rectangle(Position.X + width - Skin.Elements["Window.Border.Vertical.Right"].Dimensions.Width, Skin.Elements["Window.Border.Top.Horizontal"].Dimensions.Height + Position.Y, Skin.Elements["Window.Border.Vertical.Right"].Dimensions.Width, height - Skin.Elements["Window.Border.Top.RightCorner"].Dimensions.Height - Skin.Elements["Window.Border.Bottom.RightCorner"].Dimensions.Height));

            Skin.Elements["Window.Border.Bottom.LeftCorner"].Draw(new System.Drawing.Rectangle(Position.X, Position.Y + height - Skin.Elements["Window.Border.Bottom.LeftCorner"].Dimensions.Height, Skin.Elements["Window.Border.Bottom.LeftCorner"].Dimensions.Width, Skin.Elements["Window.Border.Bottom.LeftCorner"].Dimensions.Height));
            Skin.Elements["Window.Border.Bottom.Horizontal"].Draw(new System.Drawing.Rectangle(Position.X + Skin.Elements["Window.Border.Bottom.LeftCorner"].Dimensions.Width, Position.Y + height - Skin.Elements["Window.Border.Bottom.Horizontal"].Dimensions.Height, width - Skin.Elements["Window.Border.Bottom.RightCorner"].Dimensions.Width - Skin.Elements["Window.Border.Bottom.LeftCorner"].Dimensions.Width, Skin.Elements["Window.Border.Bottom.Horizontal"].Dimensions.Height));
            Skin.Elements["Window.Border.Bottom.RightCorner"].Draw(new System.Drawing.Rectangle(position.X + width - Skin.Elements["Window.Border.Bottom.RightCorner"].Dimensions.Width, Position.Y + height - Skin.Elements["Window.Border.Bottom.RightCorner"].Dimensions.Height, Skin.Elements["Window.Border.Bottom.RightCorner"].Dimensions.Width, Skin.Elements["Window.Border.Bottom.RightCorner"].Dimensions.Height));
        }
    }
}
