using System;
using System.Threading.Tasks;
using Lidgren.Network;
using Robust.Shared.Interfaces.Network;

namespace Robust.Shared.Serialization
{

    public partial class RobustSerializer
    {
        public Task Handshake(INetChannel channel)
            => MappedStringSerializer.Handshake(channel);

        public event Action ClientHandshakeComplete
        {
            add => MappedStringSerializer.ClientHandshakeComplete += value;
            remove => MappedStringSerializer.ClientHandshakeComplete -= value;
        }

    }

}
