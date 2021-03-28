using System;
using System.Collections.Generic;
using Robust.Shared.GameStates;
using Robust.Shared.Network;

namespace Robust.Client.Player
{
    public interface IPlayerManager : Shared.Players.ISharedPlayerManager
    {
        new IEnumerable<IPlayerSession> Sessions { get; }
        IReadOnlyDictionary<NetUserId, IPlayerSession> SessionsDict { get; }

        LocalPlayer? LocalPlayer { get; }

        /// <summary>
        /// Invoked after LocalPlayer is changed
        /// </summary>
        event Action<LocalPlayerChangedEventArgs>? LocalPlayerChanged;

        event EventHandler PlayerListUpdated;

        void Initialize();
        void Startup(INetChannel channel);
        void Shutdown();

        void ApplyPlayerStates(IEnumerable<PlayerState>? list);
    }

    public class LocalPlayerChangedEventArgs : EventArgs
    {
        public readonly LocalPlayer? OldPlayer;
        public readonly LocalPlayer? NewPlayer;
        public LocalPlayerChangedEventArgs(LocalPlayer? oldPlayer, LocalPlayer? newPlayer)
        {
            OldPlayer = oldPlayer;
            NewPlayer = newPlayer;
        }
    }
}
