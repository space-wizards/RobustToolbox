using System;
using Robust.Shared;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Players;

namespace Robust.Server.Player
{
    public interface IPlayerSession : ICommonSession
    {
        EntityUid? AttachedEntityUid { get; }
        INetChannel ConnectedClient { get; }
        DateTime ConnectedTime { get; }

        /// <summary>
        ///     The visibility mask for this player.
        ///     The player will be able to get updates for entities whose layers match the mask.
        /// </summary>
        int VisibilityMask { get; set; }

        event EventHandler<SessionStatusEventArgs> PlayerStatusChanged;

        void JoinGame();

        LoginType AuthType { get; }

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
