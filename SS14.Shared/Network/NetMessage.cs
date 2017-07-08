using Lidgren.Network;
using SS14.Shared.Interfaces.Network;

namespace SS14.Shared.Network
{
    /// <summary>
    /// The group the message belongs to, used for statistics.
    /// </summary>
    public enum MsgGroups
    {
        /// <summary>
        /// Error state, the message needs to set a different one.
        /// </summary>
        ERROR = 0,

        /// <summary>
        /// A core message, like connect, disconnect, and tick.
        /// </summary>
        CORE,

        /// <summary>
        /// Entity message, for keeping entities in sync.
        /// </summary>
        ENTITY,

        /// <summary>
        /// A string message, for chat.
        /// </summary>
        STRING,

        /// <summary>
        /// A command message from client -> server.
        /// </summary>
        COMMAND,
    }

    public abstract class NetMessage
    {
        public string MsgName { get; }
        public MsgGroups MsgGroup { get; }
        public NetMessages MsgId { get; }

        public INetChannel MsgChannel { get; set; }

        internal NetMessage(string name, MsgGroups group, NetMessages id)
        {
            MsgName = name;
            MsgGroup = group;
            MsgId = id;
        }

        /// <summary>
        /// Deserializes the NetIncomingMessage into this NetMessage class.
        /// </summary>
        /// <param name="buffer">The buffer of the raw incoming packet.</param>
        public abstract void ReadFromBuffer(NetIncomingMessage buffer);

        /// <summary>
        /// Serializes this NetMessage into a new NetOutgoingMessage.
        /// </summary>
        /// <param name="buffer">The buffer of the new packet being serialized.</param>
        public abstract void WriteToBuffer(NetOutgoingMessage buffer);
    }
}
