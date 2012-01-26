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
using SS3D.Modules;

namespace SS3D.UserInterface
{
    class TargetingDummyElement : GuiComponent
    {
        public delegate void TargetingDummyElementPressHandler(TargetingDummyElement sender);
        public event TargetingDummyElementPressHandler Clicked;

        public float maxHealth = 0;
        public float currHealth = 0;
        public BodyPart myPart;

        private Sprite elementSprite;

        private Boolean selected = false;
        private Point click_pos;


        public TargetingDummyElement(string spriteName, BodyPart part, PlayerController controller)
            : base(controller)
        {
            myPart = part;
            elementSprite = ResMgr.Singleton.GetSprite(spriteName);
            Update();
        }

        public void Select()
        {
            selected = true;
        }

        public bool isSelected()
        {
            return selected;
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
            //elementSprite.Color = selected ? Color.DarkRed : Color.White;
            float healthPct = currHealth / maxHealth;
            Color healthCol = Color.WhiteSmoke;

            if (healthPct > 0.75) healthCol = Color.DarkGreen;
            else if (healthPct > 0.50) healthCol = Color.Yellow;
            else if (healthPct > 0.25) healthCol = Color.DarkOrange;
            else if (healthPct > 0) healthCol = Color.Red;
            else healthCol = Color.Black;

            elementSprite.Color = healthCol;

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
