using SS14.Shared.Interfaces.Network;
using SS14.Shared.Network.Messages;

namespace SS14.Shared.Interfaces.Map
{
    /// <summary>
    /// A shared interface for the client/server map network managers.
    /// </summary>
    public interface IMapNetworkManager
    {
        /// <summary>
        /// Handles and processes a incoming network message.
        /// </summary>
        /// <param name="message">The message to handle.</param>
        void HandleNetworkMessage(MsgMap message);

        /// <summary>
        /// Serializes the MapManager and TileDefMgr into an outgoing message to send to a client.
        /// </summary>
        /// <param name="channel">The channel to send the message on.</param>
        void SendMap(INetChannel channel);
    }
}
