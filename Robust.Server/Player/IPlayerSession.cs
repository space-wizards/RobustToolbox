using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Players;

namespace Robust.Server.Player
{
    public interface IPlayerSession : ICommonSession
    {
        DateTime ConnectedTime { get; }

        event EventHandler<SessionStatusEventArgs> PlayerStatusChanged;

        void JoinGame();

        LoginType AuthType { get; }

        /// <summary>
        ///     Attaches this player to an entity.
        ///     NOTE: The content pack almost certainly has an alternative for this.
        ///     Do not call this directly for most content code.
        /// </summary>
        /// <param name="entity">The entity to attach to.</param>
        void AttachToEntity(IEntity? entity);

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
