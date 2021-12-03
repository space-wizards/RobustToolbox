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
        ///     Game entity that the local player is controlling. If this is null, the player is not attached to any
        ///     entity at all.
        /// </summary>
        [ViewVariables] public IEntity? ControlledEntity { get; private set; }

        [ViewVariables] public EntityUid? ControlledEntityUid => ControlledEntity;


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
        public void AttachEntity(IEntity entity)
        {
            // Detach and cleanup first
            DetachEntity();

            ControlledEntity = entity;
            InternalSession.AttachedEntity = entity;

            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent<EyeComponent?>(ControlledEntity, out var eye))
            {
                eye = IoCManager.Resolve<IEntityManager>().AddComponent<EyeComponent>(ControlledEntity);
            }
            eye.Current = true;

            EntityAttached?.Invoke(new EntityAttachedEventArgs(entity));

            // notify ECS Systems
            IoCManager.Resolve<IEntityManager>().EventBus.RaiseEvent(EventSource.Local, new PlayerAttachSysMessage(ControlledEntity));
            IoCManager.Resolve<IEntityManager>().EventBus.RaiseLocalEvent(ControlledEntity, new PlayerAttachedEvent(ControlledEntity));
        }

        /// <summary>
        ///     Detaches the client from an entity.
        /// </summary>
        public void DetachEntity()
        {
            var previous = ControlledEntity;
            if (previous is {((!IoCManager.Resolve<IEntityManager>().EntityExists(Uid) ? EntityLifeStage.Deleted : IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(Uid).EntityLifeStage) >= EntityLifeStage.Initialized): true, ((!IoCManager.Resolve<IEntityManager>().EntityExists(Uid) ? EntityLifeStage.Deleted : IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(Uid).EntityLifeStage) >= EntityLifeStage.Deleted): false})
            {
                IoCManager.Resolve<IEntityManager>().GetComponent<EyeComponent>(previous).Current = false;

                // notify ECS Systems
                IoCManager.Resolve<IEntityManager>().EventBus.RaiseEvent(EventSource.Local, new PlayerAttachSysMessage(null));
                IoCManager.Resolve<IEntityManager>().EventBus.RaiseLocalEvent(previous, new PlayerDetachedEvent(previous));
            }

            ControlledEntity = null;

            if (previous != null)
            {
                EntityDetached?.Invoke(new EntityDetachedEventArgs(previous));
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
        public EntityDetachedEventArgs(IEntity oldEntity)
        {
            OldEntity = oldEntity;
        }

        public IEntity OldEntity { get; }
    }

    public class EntityAttachedEventArgs : EventArgs
    {
        public EntityAttachedEventArgs(IEntity newEntity)
        {
            NewEntity = newEntity;
        }

        public IEntity NewEntity { get; }
    }
}
