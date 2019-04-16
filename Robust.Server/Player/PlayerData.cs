using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;

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
