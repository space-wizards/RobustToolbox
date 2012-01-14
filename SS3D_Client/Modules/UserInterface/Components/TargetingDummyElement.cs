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
    class TargetingDummyElement : GuiComponent
    {
        public delegate void TargetingDummyElementPressHandler(TargetingDummyElement sender);
        public event TargetingDummyElementPressHandler Clicked;

        private Sprite elementSprite;

        private Boolean selected = false;
        private Point click_pos;

        public TargetingDummyElement(string spriteName, BodyPart part)
            : base()
        {
            elementSprite = ResMgr.Singleton.GetSprite(spriteName);
            Update();
        }

        public void ClearSelected()
        {
            selected = false;
        }

        public override void Update()
        {
            elementSprite.Position = Position;
            clientArea = new Rectangle(Position, new Size((int)elementSprite.AABB.Width, (int)elementSprite.AABB.Height));
        }

        public override void Render()
        {
            elementSprite.Color = selected ? Color.DarkRed : Color.White;
            elementSprite.Position = Position;
            elementSprite.Draw();
            elementSprite.Color = Color.White;
            if (selected)
            {
                Gorgon.Screen.Circle(Position.X + click_pos.X, Position.Y + click_pos.Y, 5, Color.Black);
                Gorgon.Screen.Circle(Position.X + click_pos.X, Position.Y + click_pos.Y, 4, Color.DarkRed);
                Gorgon.Screen.Circle(Position.X + click_pos.X, Position.Y + click_pos.Y, 3, Color.Black);
            }
        }

        public override void Dispose()
        {
            elementSprite = null;
            Clicked = null;
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            if (!clientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y))) return false;

            Point spritePosition = new Point((int)e.Position.X - this.Position.X + (int)elementSprite.ImageOffset.X, (int)e.Position.Y - this.Position.Y + (int)elementSprite.ImageOffset.Y);

            GorgonLibrary.Graphics.Image.ImageLockBox imgData = elementSprite.Image.GetImageData();
            imgData.Lock(false);
            Color pixColour = System.Drawing.Color.FromArgb((int)(imgData[spritePosition.X, spritePosition.Y]));
            imgData.Dispose();
            imgData.Unlock();

            if (pixColour.A != 0)
            {
                if (Clicked != null) Clicked(this);
                click_pos = new Point((int)e.Position.X - this.Position.X, (int)e.Position.Y - this.Position.Y);
                selected = true;
                return true;
            }
            else
            {
                selected = false;
                return false;
            }
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            return false;
        }
    }
}
