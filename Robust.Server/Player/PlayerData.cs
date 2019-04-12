using Robust.Server.Interfaces.Player;
using Robust.Shared.Network;
using Robust.Shared.GameObjects;

namespace Robust.Server.Player
{
    class PlayerData : IPlayerData
    {
        public PlayerData(NetSessionId sessionId)
        {
            SessionId = sessionId;
        }

        public NetSessionId SessionId { get; }

        public object ContentDataUncast { get; set; }
    }
}
