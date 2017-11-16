using System.Collections.Generic;
using SS14.Shared.Interfaces.Players;

namespace SS14.Shared.Players
{
    public class PlayerManager : IClientPlayerManager, IServerPlayerManager
    {
        private const int DefaultNumPlayers = 64;

        private readonly Dictionary<int, PlayerSession> _sessions;

        public int MaxPlayerCount { get; }
        public int PlayerCount { get; }
        public IEnumerable<IPlayerSession> Sessions => _sessions.Values;

        public PlayerManager()
        {
            _sessions = new Dictionary<int, PlayerSession>(DefaultNumPlayers);
        }
    }

    public struct PlayerId
    {
        
    }
}
