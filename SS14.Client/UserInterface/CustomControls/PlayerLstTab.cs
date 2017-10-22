using Lidgren.Network;
using SS14.Client.Graphics.Input;
using SS14.Client.Interfaces.Resource;
using SS14.Client.UserInterface.Components;
using SS14.Shared;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using Vector2i = SS14.Shared.Maths.Vector2i;

namespace SS14.Client.UserInterface.CustomControls
{
    internal class PlayerListTab : TabContainer
    {
        public ScrollableContainer _scPlayerList;

        public PlayerListTab(string uniqueName, Vector2i size, IResourceCache resourceCache)
            : base(uniqueName, size, resourceCache)
        {
            DrawBorder = false;

            _scPlayerList = new ScrollableContainer("scplayerlist", new Vector2i(784, 346));
            _scPlayerList.Position = new Vector2i(5, 10);
            Components.Add(_scPlayerList);
        }

        public override void Activated() //Called when tab is selected.
        {
            getPlayerList();
        }

        public void getPlayerList()
        {
            var netManager = IoCManager.Resolve<IClientNetManager>();
            NetOutgoingMessage playerListMsg = netManager.CreateMessage();
            playerListMsg.Write((byte)NetMessages.PlayerListReq); //Request Playerlist.
            netManager.ClientSendMessage(playerListMsg, NetDeliveryMethod.ReliableOrdered);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
        }

        public override void Draw()
        {
            base.Draw();
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
