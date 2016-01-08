using Lidgren.Network;
using SS14.Client.Interfaces.Network;
using SS14.Client.Interfaces.Resource;
using SS14.Shared;
using System.Drawing;
using SFML.Window;
using SS14.Client.Graphics;
using SFML.System;
using SS14.Shared.Maths;

namespace SS14.Client.Services.UserInterface.Components
{
    internal class AdminPlayerPanel : Window
    {
        private readonly INetworkManager _networkManager;
        private readonly IResourceManager _resourceManager;

        public AdminPlayerPanel(Vector2i size, INetworkManager networkManager, IResourceManager resourceManager,
                                NetIncomingMessage message)
            : base("Admin Player Panel", size, resourceManager)
        {
            _networkManager = networkManager;
            _resourceManager = resourceManager;

            BuildList(message);
            var closeButton = new Button("Close", _resourceManager) {Position = new Vector2i(5, 5)};
            closeButton.Clicked += CloseButtonClicked;
            components.Add(closeButton);

            var unbanButton = new Button("Unban", _resourceManager)
                                  {Position = new Vector2i(closeButton.ClientArea.Right() + 10, 5)};
            unbanButton.Clicked += UnbanButtonClicked;
            components.Add(unbanButton);

            Position = new Vector2i((int) (CluwneLib.CurrentRenderTarget.Size.X/2f) - (int) (ClientArea.Width/2f),
                                 (int) (CluwneLib.CurrentRenderTarget.Size.Y/2f) - (int) (ClientArea.Height/2f));
        }

        private void UnbanButtonClicked(Button sender)
        {
            NetOutgoingMessage msg = _networkManager.CreateMessage();
            msg.Write((byte) NetMessage.RequestBanList);
            _networkManager.SendMessage(msg, NetDeliveryMethod.ReliableUnordered);
            Dispose();
        }

        private void CloseButtonClicked(Button sender)
        {
            Dispose();
        }

        private void BuildList(NetIncomingMessage message)
        {
            byte playerCount = message.ReadByte();
            int yOffset = 40;
            for (int i = 0; i < playerCount; i++)
            {
                string name = message.ReadString();
                var status = (SessionStatus) message.ReadByte();
                string job = message.ReadString();
                string ip = message.ReadString();
                bool isAdmin = message.ReadBoolean();

                var line = new Label("Name: " + name + "    Status: " + status + "    Job: " + job + "    IP: " + ip,
                                     "CALIBRI", _resourceManager)
                               {
                                   Position = new Vector2i(5, yOffset + 5),
                                   Text = {Color = isAdmin ? new SFML.Graphics.Color(0, 139, 139) : SFML.Graphics.Color.Black}
                               };

                components.Add(line);

                var kickButton = new Button("Kick", _resourceManager)
                                     {
                                         Position =
                                             new Vector2i(line.ClientArea.Right() + 10,
                                                       yOffset + (int) (line.ClientArea.Height/3f))
                                     };
                components.Add(kickButton);
                kickButton.UserData = ip;
                kickButton.Clicked += KickButtonClicked;
                kickButton.Update(0);

                var banButt = new Button("Ban", _resourceManager)
                                  {
                                      Position =
                                          new Vector2i(kickButton.ClientArea.Right() + 5,
                                                    yOffset + (int) (line.ClientArea.Height/3f))
                                  };
                components.Add(banButt);
                banButt.UserData = ip;
                banButt.Clicked += BanButtonClicked;

                yOffset += 35;
            }
        }

        private void BanButtonClicked(Button sender)
        {
            NetOutgoingMessage msg = _networkManager.CreateMessage();
            msg.Write((byte) NetMessage.RequestAdminBan);
            msg.Write((string) sender.UserData); //ip
            _networkManager.SendMessage(msg, NetDeliveryMethod.ReliableUnordered);
        }

        private void KickButtonClicked(Button sender)
        {
            NetOutgoingMessage msg = _networkManager.CreateMessage();
            msg.Write((byte) NetMessage.RequestAdminKick);
            msg.Write((string) sender.UserData); //ip
            _networkManager.SendMessage(msg, NetDeliveryMethod.ReliableUnordered);
        }

        public override void Update(float frameTime)
        {
            if (disposing || !IsVisible()) return;
            base.Update(frameTime);
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

        public override bool MouseDown(MouseButtonEventArgs e)
        {
            if (disposing || !IsVisible()) return false;
            if (base.MouseDown(e)) return true;
            return false;
        }

        public override bool MouseUp(MouseButtonEventArgs e)
        {
            if (disposing || !IsVisible()) return false;
            if (base.MouseUp(e)) return true;
            return false;
        }

        public override void MouseMove(MouseMoveEventArgs e)
        {
            if (disposing || !IsVisible()) return;
            base.MouseMove(e);
        }

        public override bool MouseWheelMove(MouseWheelEventArgs e)
        {
            if (base.MouseWheelMove(e)) return true;
            return false;
        }

        public override bool KeyDown(KeyEventArgs e)
        {
            if (base.KeyDown(e)) return true;
            return false;
        }
    }
}