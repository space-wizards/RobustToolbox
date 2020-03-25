using System;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Players;

namespace Robust.Server.Interfaces.Player
{
    public interface IPlayerSession : ICommonSession
    {
        EntityUid? AttachedEntityUid { get; }
        INetChannel ConnectedClient { get; }
        DateTime ConnectedTime { get; }

        event EventHandler<SessionStatusEventArgs> PlayerStatusChanged;

        void JoinGame();

        /// <summary>
        ///     Attaches this player to an entity.
        ///     NOTE: The content pack almost certainly has an alternative for this.
        ///     Do not call this directly for most content code.
        /// </summary>
        /// <param name="a">The entity to attach to.</param>
        void AttachToEntity(IEntity a);

        /// <summary>
        ///     Detaches this player from an entity.
        ///     NOTE: The content pack almost certainly has an alternative for this.
        ///     Do not call this directly for most content code.
        /// </summary>
        void DetachFromEntity();
        void OnConnect();
        void OnDisconnect();

        /// <summary>
        ///     Persistent data for this player.
        /// </summary>
        IPlayerData Data { get; }
    }
}
