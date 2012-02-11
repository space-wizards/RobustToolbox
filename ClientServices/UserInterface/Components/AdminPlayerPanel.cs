using System.Drawing;
using ClientInterfaces;
using ClientInterfaces.Network;
using GorgonLibrary;
using GorgonLibrary.InputDevices;
using Lidgren.Network;
using SS13_Shared;

namespace ClientServices.UserInterface.Components
{
    class AdminPlayerPanel : Window
    {
        private readonly INetworkManager _networkManager;
        private readonly IResourceManager _resourceManager;

        public AdminPlayerPanel (Size size, INetworkManager networkManager, IResourceManager resourceManager, NetIncomingMessage message)
            : base("Admin Player Panel", size, resourceManager)
        {
            _networkManager = networkManager;
            _resourceManager = resourceManager;

            BuildList(message);
            var closeButton = new Button("Close", _resourceManager) { Position = new Point(5, 5) };
            closeButton.Clicked += CloseButtonClicked;
            components.Add(closeButton);

            var unbanButton = new Button("Unban", _resourceManager) { Position = new Point(closeButton.ClientArea.Right + 10, 5) };
            unbanButton.Clicked += UnbanButtonClicked;
            components.Add(unbanButton);

            Position = new Point((int)(Gorgon.Screen.Width / 2f) - (int)(ClientArea.Width / 2f), (int)(Gorgon.Screen.Height / 2f) - (int)(ClientArea.Height / 2f));
        }

        void UnbanButtonClicked(Button sender)
        {
            var msg = _networkManager.CreateMessage();
            msg.Write((byte)NetMessage.RequestBanList);
            _networkManager.SendMessage(msg, NetDeliveryMethod.ReliableUnordered);
            Dispose();
        }

        void CloseButtonClicked(Button sender)
        {
            Dispose();
        }

        private void BuildList(NetIncomingMessage message)
        {
            var playerCount = message.ReadByte();
            var yOffset = 40;
            for (var i = 0; i < playerCount; i++)
            {
                var name = message.ReadString();
                var status = (SessionStatus)message.ReadByte();
                var job = message.ReadString();
                var ip = message.ReadString();
                var isAdmin = message.ReadBoolean();

                var line = new Label("Name: " + name + "    Status: " + status + "    Job: " + job + "    IP: " + ip, "CALIBRI", _resourceManager)
                               {
                                   Position = new Point(5, yOffset + 5),
                                   Text = {Color = isAdmin ? Color.DarkCyan : Color.Black}
                               };

                components.Add(line);

                var kickButton = new Button("Kick", _resourceManager) { Position = new Point(line.ClientArea.Right + 10, yOffset) };
                components.Add(kickButton);
                kickButton.UserData = ip;
                kickButton.Clicked += KickButtonClicked;
                kickButton.Update();

                var banButt = new Button("Ban", _resourceManager) { Position = new Point(kickButton.ClientArea.Right + 5, yOffset) };
                components.Add(banButt);
                banButt.UserData = ip;
                banButt.Clicked += BanButtonClicked;

                yOffset += 35;
            }
        }

        void BanButtonClicked(Button sender)
        {
            NetOutgoingMessage msg = _networkManager.CreateMessage();
            msg.Write((byte)NetMessage.RequestAdminBan);
            msg.Write((string)sender.UserData); //ip
            _networkManager.SendMessage(msg, NetDeliveryMethod.ReliableUnordered);
        }

        void KickButtonClicked(Button sender)
        {
            NetOutgoingMessage msg = _networkManager.CreateMessage();
            msg.Write((byte)NetMessage.RequestAdminKick);
            msg.Write((string)sender.UserData); //ip
            _networkManager.SendMessage(msg, NetDeliveryMethod.ReliableUnordered);
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