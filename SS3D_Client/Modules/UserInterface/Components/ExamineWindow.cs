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
using SS3D.UserInterface;
using CGO;

namespace SS3D.UserInterface
{
    class ExamineWindow : Window
    {
        private Sprite entSprite;
        private Label entDesc;

        public ExamineWindow(Size _size, Entity entity)
            : base(entity.name, _size)
        {

            entDesc = new Label(entity.GetDescriptionString());

            this.components.Add(entDesc);

            List<ComponentReplyMessage> replies = new List<ComponentReplyMessage>();
            entity.SendMessage(entity, SS3D_shared.GO.ComponentMessageType.GetSprite, replies);

            this.SetVisible(true);

            if (replies.Any())
            {
                entSprite = (Sprite)replies.First(x => x.messageType == SS3D_shared.GO.ComponentMessageType.CurrentSprite).paramsList[0];
                entDesc.Position = new Point(10, (int)entSprite.Height + entDesc.ClientArea.Height + 10);
            }
            else
                entDesc.Position = new Point(10, 10);

            //position = new Point((int)(Gorgon.Screen.Width / 2f) - (int)(this.ClientArea.Width / 2f), (int)(Gorgon.Screen.Height / 2f) - (int)(this.ClientArea.Height / 2f));
        }

        public override void Update()
        {
            base.Update();
        }

        public override void Render()
        {
            base.Render();
            if (entSprite != null)
            {
                Rectangle spriteRect = new Rectangle((int)(clientArea.Width / 2f - entSprite.Width / 2f) + clientArea.X, 10 + clientArea.Y, (int)entSprite.Width, (int)entSprite.Height);
                entSprite.Draw(spriteRect);
            }
        }

        public override void Dispose()
        {
            entSprite = null;
            base.Dispose();
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            if (base.MouseDown(e)) return true;
            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            if (base.MouseUp(e)) return true;
            return false;
        }

        public override void MouseMove(MouseInputEventArgs e)
        {
            base.MouseMove(e);
            return;
        }

        public override bool MouseWheelMove(MouseInputEventArgs e)
        {
            if (base.MouseWheelMove(e)) return true;
            return false;
        }

        public override bool KeyDown(KeyboardInputEventArgs e)
        {
            if (base.KeyDown(e)) return true;
            return false;
        }
    }
}
