using System;
using System.Collections.Generic;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Players;
using Robust.Shared.ViewVariables;

namespace Robust.Client.Player
{
    public interface IPlayerManager : ISharedPlayerManager
    {
        new IEnumerable<ICommonSession> Sessions { get; }

        [ViewVariables]
        IReadOnlyDictionary<NetUserId, ICommonSession> SessionsDict { get; }

        [ViewVariables]
        LocalPlayer? LocalPlayer { get; }

        /// <summary>
        /// Invoked after LocalPlayer is changed
        /// </summary>
        event Action<LocalPlayerChangedEventArgs>? LocalPlayerChanged;

        event EventHandler PlayerListUpdated;

        void Initialize();
        void Startup();
        void Shutdown();

        void ApplyPlayerStates(IReadOnlyCollection<PlayerState> list);
    }

    public sealed class LocalPlayerChangedEventArgs : EventArgs
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
