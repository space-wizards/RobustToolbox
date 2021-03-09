using Robust.Shared.GameObjects;
using Robust.Shared.Players;

namespace Robust.Server.GameObjects
{
    internal sealed class ChunkSubscribeMessage : EntitySystemMessage
    {
        public ICommonSession Session { get; }

        public ChunkSubscribeMessage(ICommonSession session)
        {
            Session = session;
        }
    }

    internal sealed class ChunkUnsubscribeMessage : EntitySystemMessage
    {
        public ICommonSession Session { get; }

        public ChunkUnsubscribeMessage(ICommonSession session)
        {
            Session = session;
        }
    }
}
