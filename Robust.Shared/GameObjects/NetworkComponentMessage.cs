using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Players;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects
{
#nullable enable

    /// <summary>
    /// A container that holds a component message and network info.
    /// </summary>
    public readonly struct NetworkComponentMessage
    {
        /// <summary>
        /// Network channel this message came from.
        /// </summary>
        public readonly INetChannel Channel;

        /// <summary>
        /// Entity Uid this message is associated with.
        /// </summary>
        public readonly EntityUid EntityUid;

        /// <summary>
        /// If the Message is Directed, Component net Uid this message is being sent to.
        /// </summary>
        public readonly uint NetId;

        /// <summary>
        /// The message payload.
        /// </summary>
#pragma warning disable 618
        public readonly ComponentMessage Message;
#pragma warning restore 618

        /// <summary>
        /// If the message is from the client, the client's session.
        /// </summary>
        public readonly ICommonSession? Session;

        /// <summary>
        /// Constructs a new instance of <see cref="NetworkComponentMessage"/>.
        /// </summary>
        /// <param name="netMsg">Raw network message containing the component message.</param>
        public NetworkComponentMessage(MsgEntity netMsg, ICommonSession? session = null)
        {
            DebugTools.Assert(netMsg.Type == EntityMessageType.ComponentMessage);

            Channel = netMsg.MsgChannel;
            EntityUid = netMsg.EntityUid;
            NetId = netMsg.NetId;
            Message = netMsg.ComponentMessage;
            Session = session;
        }
    }

#nullable restore
}
