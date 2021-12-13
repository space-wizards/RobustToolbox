using System;
using Robust.Client.GameObjects;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Players;
using Robust.Shared.ViewVariables;

namespace Robust.Client.Player
{
    /// <summary>
    ///     Variables and functions that deal with the local client's session.
    /// </summary>
    public class LocalPlayer
    {
        /// <summary>
        ///     An entity has been attached to the local player.
        /// </summary>
        public event Action<EntityAttachedEventArgs>? EntityAttached;

        /// <summary>
        ///     An entity has been detached from the local player.
        /// </summary>
        public event Action<EntityDetachedEventArgs>? EntityDetached;

        /// <summary>
        ///     Game entity that the local player is controlling. If this is default, the player is not attached to any
        ///     entity at all.
        /// </summary>
        [ViewVariables] public EntityUid? ControlledEntity { get; private set; }

        [ViewVariables] public NetUserId UserId { get; set; }

        /// <summary>
        ///     Session of the local client.
        /// </summary>
        [ViewVariables]
        public ICommonSession Session => InternalSession;

        internal PlayerSession InternalSession { get; set; } = default!;

        /// <summary>
        ///     OOC name of the local player.
        /// </summary>
        [ViewVariables]
        public string Name { get; set; } = default!;

        /// <summary>
        ///     The status of the client's session has changed.
        /// </summary>
        public event EventHandler<StatusEventArgs>? StatusChanged;

        /// <summary>
        ///     Attaches a client to an entity.
        /// </summary>
        /// <param name="entity">Entity to attach the client to.</param>
        public void AttachEntity(EntityUid entity)
        {
            // Detach and cleanup first
            DetachEntity();

            ControlledEntity = entity;
            InternalSession.AttachedEntity = entity;

            var entMan = IoCManager.Resolve<IEntityManager>();

            if (!entMan.TryGetComponent<EyeComponent?>(entity, out var eye))
            {
                eye = entMan.AddComponent<EyeComponent>(entity);
            }
            eye.Current = true;

            EntityAttached?.Invoke(new EntityAttachedEventArgs(entity));

            // notify ECS Systems
            var eventBus = entMan.EventBus;
            eventBus.RaiseEvent(EventSource.Local, new PlayerAttachSysMessage(entity));
            eventBus.RaiseLocalEvent(entity, new PlayerAttachedEvent(entity));
        }

        /// <summary>
        ///     Detaches the client from an entity.
        /// </summary>
        public void DetachEntity()
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            var previous = ControlledEntity;
            if (entMan.TryGetComponent(previous, out MetaDataComponent? metaData) &&
                metaData.EntityInitialized &&
                !metaData.EntityDeleted)
            {
                entMan.GetComponent<EyeComponent>(previous.Value).Current = false;

                // notify ECS Systems
                entMan.EventBus.RaiseEvent(EventSource.Local, new PlayerAttachSysMessage(default));
                entMan.EventBus.RaiseLocalEvent(previous.Value, new PlayerDetachedEvent(previous.Value));
            }

            ControlledEntity = default;

            if (previous != null)
            {
                EntityDetached?.Invoke(new EntityDetachedEventArgs(previous.Value));
            }
        }

        /// <summary>
        ///     Changes the state of the session.
        /// </summary>
        public void SwitchState(SessionStatus newStatus)
        {
            SwitchState(Session.Status, newStatus);
        }

        /// <summary>
        ///     Changes the state of the session. This overload allows you to spoof the oldStatus, use with caution.
        /// </summary>
        public void SwitchState(SessionStatus oldStatus, SessionStatus newStatus)
        {
            var args = new StatusEventArgs(oldStatus, newStatus);
            Session.Status = newStatus;
            StatusChanged?.Invoke(this, args);
        }
    }

    /// <summary>
    ///     Event arguments for when the status of a session changes.
    /// </summary>
    public class StatusEventArgs : EventArgs
    {
        /// <summary>
        ///     Status that the session switched from.
        /// </summary>
        public SessionStatus OldStatus { get; }

        /// <summary>
        ///     Status that the session switched to.
        /// </summary>
        public SessionStatus NewStatus { get; }

        /// <summary>
        ///     Constructs a new instance of the class.
        /// </summary>
        public StatusEventArgs(SessionStatus oldStatus, SessionStatus newStatus)
        {
            OldStatus = oldStatus;
            NewStatus = newStatus;
        }
    }

    public class EntityDetachedEventArgs : EventArgs
    {
        public EntityDetachedEventArgs(EntityUid oldEntity)
        {
            OldEntity = oldEntity;
        }

        public EntityUid OldEntity { get; }
    }

    public class EntityAttachedEventArgs : EventArgs
    {
        public EntityAttachedEventArgs(EntityUid newEntity)
        {
            NewEntity = newEntity;
        }

        public EntityUid NewEntity { get; }
    }
}
