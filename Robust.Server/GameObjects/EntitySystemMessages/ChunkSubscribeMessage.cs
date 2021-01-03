using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;

namespace Robust.Server.GameObjects.EntitySystemMessages
{
    internal sealed class ChunkSubscribeMessage : EntitySystemMessage
    {
        public IPlayerSession Session { get; }

        public ChunkSubscribeMessage(IPlayerSession session)
        {
            Session = session;
        }
    }

    internal sealed class ChunkUnsubscribeMessage : EntitySystemMessage
    {
        public IPlayerSession Session { get; }

        public ChunkUnsubscribeMessage(IPlayerSession session)
        {
            Session = session;
        }
    }
}
