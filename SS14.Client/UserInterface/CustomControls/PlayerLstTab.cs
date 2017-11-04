using Lidgren.Network;
using SS14.Client.Graphics.Input;
using SS14.Client.Interfaces.Resource;
using SS14.Client.UserInterface.Components;
using SS14.Client.UserInterface.Controls;
using SS14.Shared;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Network.Messages;
using Vector2i = SS14.Shared.Maths.Vector2i;

namespace SS14.Client.UserInterface.CustomControls
{
    internal class PlayerListTab : TabContainer
    {
        public ListPanel PlayerList { get; }

        public PlayerListTab(Vector2i size)
            : base(size)
        {
            //PlayerList = new ScrollableContainer(new Vector2i(784, 346));
            //PlayerList.Position = new Vector2i(5, 10);
            //Components.Add(PlayerList);

            PlayerList = new ListPanel();
            Container.AddControl(PlayerList);
        }

        /// <inheritdoc />
        public override void Activated()
        {
            /*TODO: race condition where this is sending a message before the StringTable can be set up.
             * TODO: Move all netcode to ClientBase class.

            var netManager = IoCManager.Resolve<IClientNetManager>();
            //NetOutgoingMessage playerListMsg = netManager.CreateMessage();
            //playerListMsg.Write((byte)NetMessages.PlayerListReq); //Request Playerlist.
            //netManager.ClientSendMessage(playerListMsg, NetDeliveryMethod.ReliableOrdered);

            var msg = netManager.CreateNetMessage<MsgPlayerListReq>();
            // msg is empty
            netManager.ClientSendMessage(msg, NetDeliveryMethod.ReliableOrdered);

            */
        }
    }
}
