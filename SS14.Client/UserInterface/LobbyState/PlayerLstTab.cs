using Lidgren.Network;
using SFML.System;
using SFML.Window;
using SS14.Client.Interfaces.Network;
using SS14.Client.Interfaces.Resource;
using SS14.Shared;
using SS14.Shared.IoC;

namespace SS14.Client.UserInterface.Components
{
    internal class PlayerListTab : TabContainer
    {
        public ScrollableContainer _scPlayerList;

        public PlayerListTab(string uniqueName, Vector2i size, IResourceManager resourceManager)
            : base(uniqueName, size, resourceManager)
        {
            DrawBorder = false;

            _scPlayerList = new ScrollableContainer("scplayerlist", new Vector2i(784, 346), resourceManager);
            _scPlayerList.Position = new Vector2i(5,10);
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
            playerListMsg.Write((byte)NetMessages.PlayerListReq); //Request Playerlist.
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

        public override bool MouseDown(MouseButtonEventArgs e)
        {
            return base.MouseDown(e);
        }

        public override bool MouseUp(MouseButtonEventArgs e)
        {
            return base.MouseUp(e);
        }

        public override void MouseMove(MouseMoveEventArgs e)
        {
            base.MouseMove(e);
        }
    }
}