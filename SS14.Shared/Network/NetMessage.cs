using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;

namespace SS14.Shared.Network
{
    /// <summary>
    /// The group the message belongs to, used for statistics.
    /// </summary>
    public enum MsgGroups
    {
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
        public MsgGroups Group { get; }
        public string Name { get; }
        public NetChannel Channel { get; set; }
        public NetMessages Id { get; set; }

        protected NetMessage(NetChannel channel, string name, MsgGroups group, NetMessages id)
        {
            Name = name;
            Group = group;
            Channel = channel;
            Id = id;
        }

        public abstract void ReadFromBuffer(NetIncomingMessage buffer);
        public abstract void WriteToBuffer(NetOutgoingMessage buffer);

        public delegate void ProcessMessage(NetMessage message);
        public abstract ProcessMessage Callback { get; set; }
    }
}
