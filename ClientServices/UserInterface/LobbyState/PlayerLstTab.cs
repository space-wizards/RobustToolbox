using System.Drawing;
using ClientInterfaces.Network;
using ClientInterfaces.Resource;
using ClientServices.Network;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using Lidgren.Network;
using SS13.IoC;
using SS13_Shared;

namespace ClientServices.UserInterface.Components
{
    internal class PlayerListTab : TabContainer
    {
        public ScrollableContainer _scPlayerList;

        public PlayerListTab(string uniqueName, Size size, IResourceManager resourceManager)
            : base(uniqueName, size, resourceManager)
        {
            DrawBorder = false;

            _scPlayerList = new ScrollableContainer("scplayerlist", new Size(784, 346), resourceManager);
            _scPlayerList.Position = new Point(5,10);
            components.Add(_scPlayerList);
        }

        public override void Activated() //Called when tab is selected.
        {
            getPlayerList();
        }

        public void getPlayerList()
        {
            var netManager = IoCManager.Resolve<INetworkManager>();
            NetOutgoingMessage playerListMsg = netManager.CreateMessage();
            playerListMsg.Write((byte)NetMessage.PlayerList); //Request Playerlist.
            netManager.SendMessage(playerListMsg, NetDeliveryMethod.ReliableOrdered);          
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
        }

        public override void Render()
        {
            base.Render();
        }

        public override void Dispose()
        {
            base.Dispose();
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            return base.MouseDown(e);
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            return base.MouseUp(e);
        }

        public override void MouseMove(MouseInputEventArgs e)
        {
            base.MouseMove(e);
        }
    }
}