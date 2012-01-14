using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using GorgonLibrary.GUI;
using SS3D.Modules.Network;
using Lidgren.Network;
using SS3D_shared;

namespace SS3D.UserInterface
{
    class AdminUnbanPanel : Window
    {
        NetworkManager netMgr;

        public AdminUnbanPanel(Size _size, NetworkManager _netMgr, Banlist _banlist)
            : base("Admin UnBan Panel", _size)
        {
            netMgr = _netMgr;
            BuildList(_banlist);
            Button closeButton = new Button("Close");
            closeButton.Position = new Point(5, 5);
            closeButton.Clicked += new Button.ButtonPressHandler(closeButton_Clicked);
            components.Add(closeButton);

            position = new Point((int)(Gorgon.Screen.Width / 2f) - (int)(this.ClientArea.Width / 2f), (int)(Gorgon.Screen.Height / 2f) - (int)(this.ClientArea.Height / 2f));
        }

        void closeButton_Clicked(Button sender)
        {
            this.Dispose();
        }

        private void BuildList(Banlist list)
        {
            int y_offset = 40;
            for (int i = 0; i < list.List.Count; i++)
            {
                Label line = new Label("IP: " + list.List[i].ip + "    Reason: " + list.List[i].reason + "    Temporary: " + list.List[i].tempBan.ToString() + "    Expires: " + list.List[i].expiresAt.ToString("d/M/yyyy HH:mm:ss"));
                line.Position = new Point(5, y_offset + 5);
                components.Add(line);

                Button UnbanButt = new Button("Unban");
                UnbanButt.Position = new Point(line.ClientArea.Right + 10, y_offset);
                components.Add(UnbanButt);
                UnbanButt.UserData = list.List[i].ip;
                UnbanButt.Clicked += new Button.ButtonPressHandler(UnbanButt_Clicked);
                UnbanButt.Update();

                y_offset += 35;
            }
        }

        void UnbanButt_Clicked(Button sender)
        {
            NetOutgoingMessage msg = netMgr.netClient.CreateMessage();
            msg.Write((byte)NetMessage.RequestAdminUnBan);
            msg.Write((string)sender.UserData);
            netMgr.SendMessage(msg, NetDeliveryMethod.ReliableUnordered);
            Dispose();
        }

        public override void Update()
        {
            if (disposing || !IsVisible()) return;
            base.Update();
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