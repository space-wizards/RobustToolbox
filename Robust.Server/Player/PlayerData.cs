using Robust.Shared.Network;
using Robust.Shared.ViewVariables;

namespace Robust.Server.Player
{
    class PlayerData : IPlayerData
    {
        public PlayerData(NetUserId userId)
        {
            UserId = userId;
        }

        [ViewVariables]
        public NetUserId UserId { get; }

        [ViewVariables]
        public object? ContentDataUncast { get; set; }
    }
}
