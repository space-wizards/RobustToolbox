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

            Skin.Elements["Window.Border.Top.LeftCorner"].Draw(new System.Drawing.Rectangle(Position.X, Position.Y, ResMgr.Singleton.GetGUIInfo("Window.Border.Top.LeftCorner").Width, ResMgr.Singleton.GetGUIInfo("Window.Border.Top.LeftCorner").Height));
            Skin.Elements["Window.Border.Top.Horizontal"].Draw(new System.Drawing.Rectangle(Position.X + ResMgr.Singleton.GetGUIInfo("Window.Border.Top.LeftCorner").Width, Position.Y, width - ResMgr.Singleton.GetGUIInfo("Window.Border.Top.RightCorner").Width - ResMgr.Singleton.GetGUIInfo("Window.Border.Top.RightCorner").Width, ResMgr.Singleton.GetGUIInfo("Window.Border.Top.Horizontal").Height));
            Skin.Elements["Window.Border.Top.RightCorner"].Draw(new System.Drawing.Rectangle(Position.X + width - ResMgr.Singleton.GetGUIInfo("Window.Border.Top.RightCorner").Width, Position.Y, ResMgr.Singleton.GetGUIInfo("Window.Border.Top.RightCorner").Width, ResMgr.Singleton.GetGUIInfo("Window.Border.Top.RightCorner").Height));

            Skin.Elements["Window.Border.Vertical.Left"].Draw(new System.Drawing.Rectangle(Position.X, ResMgr.Singleton.GetGUIInfo("Window.Border.Top.LeftCorner").Height + Position.Y, ResMgr.Singleton.GetGUIInfo("Window.Border.Vertical.Left").Width, height - ResMgr.Singleton.GetGUIInfo("Window.Border.Top.LeftCorner").Height - ResMgr.Singleton.GetGUIInfo("Window.Border.Bottom.LeftCorner").Height));
            Skin.Elements["Window.Border.Vertical.Right"].Draw(new System.Drawing.Rectangle(Position.X + width - ResMgr.Singleton.GetGUIInfo("Window.Border.Vertical.Right").Width, ResMgr.Singleton.GetGUIInfo("Window.Border.Top.Horizontal").Height + Position.Y, ResMgr.Singleton.GetGUIInfo("Window.Border.Vertical.Right").Width, height - ResMgr.Singleton.GetGUIInfo("Window.Border.Top.RightCorner").Height - ResMgr.Singleton.GetGUIInfo("Window.Border.Bottom.RightCorner").Height));

            Skin.Elements["Window.Border.Bottom.LeftCorner"].Draw(new System.Drawing.Rectangle(Position.X, Position.Y + height - ResMgr.Singleton.GetGUIInfo("Window.Border.Bottom.LeftCorner").Height, ResMgr.Singleton.GetGUIInfo("Window.Border.Bottom.LeftCorner").Width, ResMgr.Singleton.GetGUIInfo("Window.Border.Bottom.LeftCorner").Height));
            Skin.Elements["Window.Border.Bottom.Horizontal"].Draw(new System.Drawing.Rectangle(Position.X + ResMgr.Singleton.GetGUIInfo("Window.Border.Bottom.LeftCorner").Width, Position.Y + height - ResMgr.Singleton.GetGUIInfo("Window.Border.Bottom.Horizontal").Height, width - ResMgr.Singleton.GetGUIInfo("Window.Border.Bottom.RightCorner").Width - ResMgr.Singleton.GetGUIInfo("Window.Border.Bottom.LeftCorner").Width, ResMgr.Singleton.GetGUIInfo("Window.Border.Bottom.Horizontal").Height));
            Skin.Elements["Window.Border.Bottom.RightCorner"].Draw(new System.Drawing.Rectangle(position.X + width - ResMgr.Singleton.GetGUIInfo("Window.Border.Bottom.RightCorner").Width, Position.Y + height - ResMgr.Singleton.GetGUIInfo("Window.Border.Bottom.RightCorner").Height, ResMgr.Singleton.GetGUIInfo("Window.Border.Bottom.RightCorner").Width, ResMgr.Singleton.GetGUIInfo("Window.Border.Bottom.RightCorner").Height));
        }
    }
}
