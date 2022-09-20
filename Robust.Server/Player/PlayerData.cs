using Robust.Shared.Network;
using Robust.Shared.ViewVariables;

namespace Robust.Server.Player
{
    sealed class PlayerData : IPlayerData
    {
        public PlayerData(NetUserId userId, string userName)
        {
            UserId = userId;
            UserName = userName;
        }

        [ViewVariables]
        public NetUserId UserId { get; }

        [ViewVariables]
        public string UserName { get; }

        [ViewVariables]
        public object? ContentDataUncast { get; set; }
    }
}
