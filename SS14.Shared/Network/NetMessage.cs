using Lidgren.Network;
using SS14.Shared.Interfaces.Network;

namespace SS14.Shared.Network
{
    /// <summary>
    /// The group the message belongs to, used for statistics and packet channels.
    /// </summary>
    public enum MsgGroups
    {
        /// <summary>
        /// Error state, the message needs to set a different one.
        /// </summary>
        Error = 0,

        /// <summary>
        /// A core message, like connect, disconnect, and tick.
        /// </summary>
        Core,

        /// <summary>
        /// Entity message, for keeping entities in sync.
        /// </summary>
        Entity,

        /// <summary>
        /// A string message, for chat.
        /// </summary>
        String,

        /// <summary>
        /// A command message from client -> server.
        /// </summary>
        Command,
    }

    /// <summary>
    /// A packet message that the NetManager sends/receives.
    /// </summary>
    public abstract class NetMessage
    {
        /// <summary>
        /// String identifier of the message type.
        /// </summary>
        public string MsgName { get; }

        /// <summary>
        /// The group this message type belongs to.
        /// </summary>
        public MsgGroups MsgGroup { get; }
        
        /// <summary>
        /// The channel that this message came in on.
        /// </summary>
        public INetChannel MsgChannel { get; set; }

        /// <summary>
        /// Constructs an instance of the NetMessage.
        /// </summary>
        /// <param name="name">String identifier of the message type.</param>
        /// <param name="group">The group this message type belongs to.</param>
        internal NetMessage(string name, MsgGroups group)
        {
            MsgName = name;
            MsgGroup = group;
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
