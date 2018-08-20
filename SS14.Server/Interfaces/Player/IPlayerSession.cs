using System;
using SS14.Server.Player;
using SS14.Shared.Enums;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Players;

namespace SS14.Server.Interfaces.Player
{
    public interface IPlayerSession : ICommonSession
    {
        IEntity AttachedEntity { get; }
        EntityUid? AttachedEntityUid { get; }
        INetChannel ConnectedClient { get; }
        DateTime ConnectedTime { get; }

        event EventHandler<SessionStatusEventArgs> PlayerStatusChanged;

        void JoinLobby();
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
        void AddPostProcessingEffect(PostProcessingEffectType type, float duration);

        /// <summary>
        ///     Persistant data for this player.
        /// </summary>
        IPlayerData Data { get; }
    }
}
