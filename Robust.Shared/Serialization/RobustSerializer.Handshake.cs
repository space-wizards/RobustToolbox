using System;
using System.Threading.Tasks;
using Robust.Shared.Network;

namespace Robust.Shared.Serialization
{

    internal abstract partial class RobustSerializer
    {
        /// <summary>
        /// Initiates any sequence of handshake extensions that
        /// need to occur before the serializer is initialized
        /// for a given client.
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        public Task Handshake(INetChannel channel)
            => MappedStringSerializer.Handshake(channel);

        /// <summary>
        /// An event that occurs once all handshake extensions have
        /// completed for the client.
        ///
        /// Note: This should not occur on the server.
        /// </summary>
        public event Action ClientHandshakeComplete
        {
            add => MappedStringSerializer.ClientHandshakeComplete += value;
            remove => MappedStringSerializer.ClientHandshakeComplete -= value;
        }

    }

}
