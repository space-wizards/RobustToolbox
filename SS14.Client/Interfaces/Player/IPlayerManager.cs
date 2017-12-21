using System;
using System.Collections.Generic;
using SS14.Client.Player;
using SS14.Shared.GameStates;
using SS14.Shared.Interfaces.Network;

namespace SS14.Client.Interfaces.Player
{
    public interface IPlayerManager
    {
        IEnumerable<PlayerSession> Sessions { get; }
        IReadOnlyDictionary<int, PlayerSession> SessionsDict { get; }

        LocalPlayer LocalPlayer { get; }

        int PlayerCount { get; }
        int MaxPlayers { get; }
        event EventHandler PlayerListUpdated;

        void Initialize();
        void Startup(INetChannel channel);
        void Update(float frameTime);
        void Shutdown();
        void Destroy();

        //TODO: Move to console system
        void SendVerb(string verb, int uid);

        //void ApplyEffects(RenderImage image);
        void ApplyPlayerStates(List<PlayerState> list);
    }
}
