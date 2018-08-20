using SS14.Server.Interfaces.Player;
using SS14.Shared.GameObjects;
using SS14.Shared.Network;

namespace SS14.Server.Player
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
