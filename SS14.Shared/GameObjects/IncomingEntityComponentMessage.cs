using System.Collections.Generic;
using SS14.Shared.Interfaces.Network;

namespace SS14.Shared.GameObjects
{
    public struct IncomingEntityComponentMessage
    {
        public uint NetId { get; }
        public INetChannel NetChannel { get; }
        public List<object> MessageParameters { get; }

        public IncomingEntityComponentMessage(uint netId, INetChannel netChannel, List<object> messageParameters) : this()
        {
            NetId = netId;
            NetChannel = netChannel;
            MessageParameters = messageParameters;
        }
    }
}
