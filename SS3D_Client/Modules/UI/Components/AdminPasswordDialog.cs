using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using GorgonLibrary.GUI;

using Lidgren.Network;
using SS3D_shared;

namespace SS3D.Modules.UI.Components
{
    class AdminPasswordDialog : Window
    {

        Textbox textboxPassword;
        Button okayButton;
        Network.NetworkManager netMgr;

        public AdminPasswordDialog(Size _size, Network.NetworkManager _netMgr)
            : base("Admin Login", _size)
        {
            netMgr = _netMgr;
            textboxPassword = new Textbox((int)(_size.Width / 2f));
            okayButton = new Button("Submit");
            okayButton.Clicked += new Button.ButtonPressHandler(okayButton_Clicked);
            textboxPassword.OnSubmit += new Textbox.TextSubmitHandler(textboxPassword_OnSubmit);
            components.Add(textboxPassword);
            components.Add(okayButton);
            position = new Point((int)(Gorgon.Screen.Width / 2f) - (int)(this.ClientArea.Width / 2f), (int)(Gorgon.Screen.Height / 2f) - (int)(this.ClientArea.Height / 2f));
        }

        void textboxPassword_OnSubmit(string text)
        {
            if (text.Length > 1 && !string.IsNullOrWhiteSpace(text))
            {
                TryAdminLogin(text);
                textboxPassword.Text = string.Empty;
            }
        }

        void okayButton_Clicked(Button sender)
        {
            if (textboxPassword.Text.Length > 1 && !string.IsNullOrWhiteSpace(textboxPassword.Text))
            {
                TryAdminLogin(textboxPassword.Text);
                textboxPassword.Text = string.Empty;
            }
        }

        public override void Update()
        {
            if (disposing || !IsVisible()) return;
            base.Update();
            if (okayButton != null && textboxPassword != null)
            {
                okayButton.Position = new Point((int)(size.Width / 2f) - (int)(okayButton.ClientArea.Width / 2f), (size.Height - okayButton.ClientArea.Height - 3));
                textboxPassword.Position = new Point((int)(size.Width / 2f) - (int)(textboxPassword.ClientArea.Width / 2f),  5);
            }
        }

        private void TryAdminLogin(string password)
        {
            NetOutgoingMessage msg = netMgr.netClient.CreateMessage();
            msg.Write((byte)NetMessage.RequestAdminLogin);
            msg.Write(password);

            netMgr.SendMessage(msg, NetDeliveryMethod.ReliableUnordered);

            Dispose();
        }

        public override void Render()
        {
            if (disposing || !IsVisible()) return;
            base.Render();
        }

        public override void Dispose()
        {
            if (disposing) return;
            base.Dispose();
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            if (disposing || !IsVisible()) return false;
            if (base.MouseDown(e)) return true;
            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            if (disposing || !IsVisible()) return false;
            if (base.MouseUp(e)) return true;
            return false;
        }

        public override void MouseMove(MouseInputEventArgs e)
        {
            if (disposing || !IsVisible()) return;
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