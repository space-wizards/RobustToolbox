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
using SS3D.UserInterface;

namespace SS3D.UserInterface
{
    class AdminPlayerPanel : Window
    {
        NetworkManager netMgr;

        public AdminPlayerPanel (Size _size, NetworkManager _netMgr, NetIncomingMessage _msgBody)
            : base("Admin Player Panel", _size)
        {
            netMgr = _netMgr;
            BuildList(_msgBody);
            Button closeButton = new Button("Close");
            closeButton.Position = new Point(5, 5);
            closeButton.Clicked += new Button.ButtonPressHandler(closeButton_Clicked);
            components.Add(closeButton);

            Button unbanButton = new Button("Unban");
            unbanButton.Position = new Point(closeButton.ClientArea.Right + 10, 5);
            unbanButton.Clicked += new Button.ButtonPressHandler(unbanButton_Clicked);
            components.Add(unbanButton);

            position = new Point((int)(Gorgon.Screen.Width / 2f) - (int)(this.ClientArea.Width / 2f), (int)(Gorgon.Screen.Height / 2f) - (int)(this.ClientArea.Height / 2f));
        }

        void unbanButton_Clicked(Button sender)
        {
            NetOutgoingMessage msg = netMgr.netClient.CreateMessage();
            msg.Write((byte)NetMessage.RequestBanList);
            netMgr.SendMessage(msg, NetDeliveryMethod.ReliableUnordered);
            this.Dispose();
        }

        void closeButton_Clicked(Button sender)
        {
            this.Dispose();
        }

        private void BuildList(NetIncomingMessage _msgBody)
        {
            byte playerCount = _msgBody.ReadByte();
            int y_offset = 40;
            for (int i = 0; i < playerCount; i++)
            {
                string name = _msgBody.ReadString();
                SessionStatus status = (SessionStatus)_msgBody.ReadByte();
                string job = _msgBody.ReadString();
                string ip = _msgBody.ReadString();
                Boolean isAdmin = _msgBody.ReadBoolean();

                Label line = new Label("Name: " + name + "    Status: " + status + "    Job: " + job + "    IP: " + ip);
                line.Position = new Point(5, y_offset + 5);
                line.Text.Color = isAdmin ? Color.DarkCyan: Color.Black;
                components.Add(line);

                Button kickButt = new Button("Kick"); //And chew bubblegum. And im all out of gum. Get it? kickButt? HAHA. Shut up, it's funny.
                kickButt.Position = new Point(line.ClientArea.Right + 10, y_offset);
                components.Add(kickButt);
                kickButt.UserData = ip;
                kickButt.Clicked += new Button.ButtonPressHandler(kickButt_Clicked);
                kickButt.Update(); //Needed so the clientarea update properly. Used for the next buttons placement.

                Button banButt = new Button("Ban");
                banButt.Position = new Point(kickButt.ClientArea.Right + 5, y_offset);
                components.Add(banButt);
                banButt.UserData = ip;
                banButt.Clicked += new Button.ButtonPressHandler(banButt_Clicked);

                y_offset += 35;
            }
        }

        void banButt_Clicked(Button sender)
        {
            NetOutgoingMessage msg = netMgr.netClient.CreateMessage();
            msg.Write((byte)NetMessage.RequestAdminBan);
            msg.Write((string)sender.UserData); //ip
            netMgr.SendMessage(msg, NetDeliveryMethod.ReliableUnordered);
        }

        void kickButt_Clicked(Button sender)
        {
            NetOutgoingMessage msg = netMgr.netClient.CreateMessage();
            msg.Write((byte)NetMessage.RequestAdminKick);
            msg.Write((string)sender.UserData); //ip
            netMgr.SendMessage(msg, NetDeliveryMethod.ReliableUnordered);
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