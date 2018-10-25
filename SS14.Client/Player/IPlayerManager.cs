using System;
using System.Collections.Generic;
using SS14.Shared.GameStates;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Network;
using SS14.Shared.Players;

namespace SS14.Client.Player
{
    public interface IPlayerManager
    {
        IEnumerable<IPlayerSession> Sessions { get; }
        IReadOnlyDictionary<NetSessionId, IPlayerSession> SessionsDict { get; }

        LocalPlayer LocalPlayer { get; }

        int PlayerCount { get; }
        int MaxPlayers { get; }
        event EventHandler PlayerListUpdated;

        void Initialize();
        void Startup(INetChannel channel);
        void Update(float frameTime);
        void Shutdown();
        
        void ApplyPlayerStates(IEnumerable<PlayerState> list);
    }
}
