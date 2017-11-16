using System.Collections.Generic;

namespace SS14.Shared.Interfaces.Players
{
    public interface IPlayerManager
    {
        int MaxPlayerCount { get; }
        int PlayerCount { get; }

        IEnumerable<IPlayerSession> Sessions { get; }
    }
}
