using Lidgren.Network;
using SS14.Shared.IoC;

namespace SS14.Shared.Interfaces.Map
{
    /// <summary>
    /// A shared interface for the client/server map network managers.
    /// </summary>
    public interface IMapNetworkManager : IIoCInterface
    {
        /// <summary>
        /// Handles and processes a incoming network message.
        /// </summary>
        /// <param name="mapManager">The mapManager to apply to message to.</param>
        /// <param name="message">The message to handle.</param>
        void HandleNetworkMessage(IMapManager mapManager, NetIncomingMessage message);

        /// <summary>
        /// Serializes the MapManager and TileDefMgr into an outgoing message to send to a client.
        /// </summary>
        /// <param name="mapManager">The mapManager to apply to message to.</param>
        /// <param name="connection">The connection to make the message on.</param>
        void SendMap(IMapManager mapManager, NetConnection connection);
    }
}
