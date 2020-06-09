using Robust.Server.Interfaces.Player;
using Robust.Shared.Network;
using Robust.Shared.ViewVariables;

namespace Robust.Server.Player
{
    class PlayerData : IPlayerData
    {
        public PlayerData(NetSessionId sessionId)
        {
            SessionId = sessionId;
        }

        [ViewVariables]
        public NetSessionId SessionId { get; }

        [ViewVariables]
        public object? ContentDataUncast { get; set; }
    }
}
