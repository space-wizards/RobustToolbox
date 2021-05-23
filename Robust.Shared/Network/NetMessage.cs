using System;
using Lidgren.Network;

#nullable disable

namespace Robust.Shared.Network
{
    /// <summary>
    /// The group the message belongs to, used for statistics and packet channels.
    /// </summary>
    public enum MsgGroups : byte
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

        /// <summary>
        /// ECS Events between the server and the client.
        /// </summary>
        EntityEvent,
    }

    /// <summary>
    /// A packet message that the NetManager sends/receives.
    /// </summary>
    public abstract class NetMessage
    {
        /// <summary>
        /// String identifier of the message type.
        /// </summary>
        public string MsgName { get; set; } = default!;

        /// <summary>
        /// The group this message type belongs to.
        /// </summary>
        public MsgGroups MsgGroup { get; set; } = default!;

        /// <summary>
        /// The channel that this message came in on.
        /// </summary>
        public INetChannel MsgChannel { get; set; } = default!;

        /// <summary>
        ///     The size of this packet in bytes.
        /// </summary>
        public int MsgSize { get; set; }

        /// <summary>
        /// Constructs an instance of the NetMessage.
        /// </summary>
        /// <param name="name">String identifier of the message type.</param>
        /// <param name="group">The group this message type belongs to.</param>
        [Obsolete("Use NetMessageAttribute and no constructor instead")]
        protected NetMessage(string name, MsgGroups group)
        {
            MsgName = name;
            MsgGroup = group;
        }

        protected NetMessage()
        {
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

        public virtual NetDeliveryMethod DeliveryMethod
        {
            get
            {
                switch (MsgGroup)
                {
                    case MsgGroups.Entity:
                        return NetDeliveryMethod.Unreliable;
                    case MsgGroups.Core:
                    case MsgGroups.Command:
                        return NetDeliveryMethod.ReliableUnordered;
                    case MsgGroups.String:
                    case MsgGroups.EntityEvent:
                        return NetDeliveryMethod.ReliableOrdered;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
