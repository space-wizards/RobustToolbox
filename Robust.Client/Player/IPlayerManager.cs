using System;
using System.Collections.Generic;
using Robust.Shared.GameStates;
using Robust.Shared.Network;

namespace Robust.Client.Player
{
    public interface IPlayerManager
    {
        IEnumerable<IPlayerSession> Sessions { get; }
        IReadOnlyDictionary<NetUserId, IPlayerSession> SessionsDict { get; }

        LocalPlayer? LocalPlayer { get; }

        int PlayerCount { get; }
        int MaxPlayers { get; }
        event EventHandler PlayerListUpdated;

        void Initialize();
        void Startup(INetChannel channel);
        void Update(float frameTime);
        void Shutdown();

        void ApplyPlayerStates(IEnumerable<PlayerState>? list);
    }
}
