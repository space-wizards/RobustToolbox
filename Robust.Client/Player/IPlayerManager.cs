using System;
using System.Collections.Generic;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Players;

namespace Robust.Client.Player
{
    public interface IPlayerManager : Shared.Players.ISharedPlayerManager
    {
        new IEnumerable<ICommonSession> Sessions { get; }
        IReadOnlyDictionary<NetUserId, ICommonSession> SessionsDict { get; }

        LocalPlayer? LocalPlayer { get; }

        /// <summary>
        /// Invoked after LocalPlayer is changed
        /// </summary>
        event Action<LocalPlayerChangedEventArgs>? LocalPlayerChanged;

        event EventHandler PlayerListUpdated;

        void Initialize();
        void Startup();
        void Shutdown();

        void ApplyPlayerStates(PlayerState[] list);
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
