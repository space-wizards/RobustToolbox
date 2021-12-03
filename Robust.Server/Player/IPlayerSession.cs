using System;
using System.Collections.Generic;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Players;
using Robust.Shared.IoC;

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
        void AttachToEntity(EntityUid? entity);

        /// <summary>
        ///     Attaches this player to an entity.
        ///     NOTE: The content pack almost certainly has an alternative for this.
        ///     Do not call this directly for most content code.
        /// </summary>
        /// <param name="uid">The entity to attach to.</param>
        void AttachToEntity(EntityUid uid);

        /// <summary>
        ///     Detaches this player from an entity.
        ///     NOTE: The content pack almost certainly has an alternative for this.
        ///     Do not call this directly for most content code.
        /// </summary>
        void DetachFromEntity();
        void OnConnect();
        void OnDisconnect();

        IReadOnlySet<EntityUid> ViewSubscriptions { get; }

        /// <summary>
        ///     Persistent data for this player.
        /// </summary>
        IPlayerData Data { get; }

        /// <summary>
        ///     Internal method to set <see cref="ICommonSession.AttachedEntity"/> and update the player's status.
        ///     Do NOT use this unless you know what you're doing, you probably want <see cref="AttachToEntity"/>
        ///     and <see cref="DetachFromEntity"/> instead.
        /// </summary>
        internal void SetAttachedEntity(EntityUid? entity);

        /// <summary>
        ///     Internal method to add an entity Uid to <see cref="ViewSubscriptions"/>.
        ///     Do NOT use this outside of <see cref="ViewSubscriberSystem"/>.
        /// </summary>
        internal void AddViewSubscription(EntityUid eye);

        /// <summary>
        ///     Internal method to remove an entity Uid from <see cref="ViewSubscriptions"/>.
        ///     Do NOT use this outside of <see cref="ViewSubscriberSystem"/>.
        /// </summary>
        internal void RemoveViewSubscription(EntityUid eye);

        /// <summary>
        ///     Porting convenience
        /// </summary>
        EntityUid AttachedEntityUidOrInvalid { get => AttachedEntityUid ?? EntityUid.Invalid; }

        /// <summary>
        ///     Porting convenience for admin commands which use such logic as "at the player's feet", etc: the transform component of the attached entity.
        /// </summary>
        TransformComponent? AttachedEntityTransform { get => IoCManager.Resolve<IEntityManager>().GetComponentOrNull<TransformComponent>(AttachedEntityUidOrInvalid); }
    }
}
