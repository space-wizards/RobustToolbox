using System;
using Robust.Client.GameObjects;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.ViewVariables;

namespace Robust.Client.Player
{
    /// <summary>
    ///     Variables and functions that deal with the local client's session.
    /// </summary>
    public class LocalPlayer
    {
        private readonly IConfigurationManager _configManager;
        private readonly IClientNetManager _networkManager;

        /// <summary>
        ///     An entity has been attached to the local player.
        /// </summary>
        public event Action<EntityAttachedEventArgs>? EntityAttached;

        /// <summary>
        ///     An entity has been detached from the local player.
        /// </summary>
        public event Action<EntityDetachedEventArgs>? EntityDetached;

        /// <summary>
        ///     Game entity that the local player is controlling. If this is null, the player
        ///     is in free/spectator cam.
        /// </summary>
        [ViewVariables] public IEntity? ControlledEntity { get; private set; }


        [ViewVariables] public NetUserId UserId { get; set; }

        /// <summary>
        ///     Session of the local client.
        /// </summary>
        [ViewVariables]
        public IPlayerSession Session => InternalSession;

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
        ///     Constructs an instance of this object.
        /// </summary>
        /// <param name="netMan"></param>
        /// <param name="configMan"></param>
        public LocalPlayer(IClientNetManager netMan, IConfigurationManager configMan)
        {
            _networkManager = netMan;
            _configManager = configMan;
        }

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

            if (!ControlledEntity.TryGetComponent<EyeComponent>(out var eye))
            {
                eye = ControlledEntity.AddComponent<EyeComponent>();
            }
            eye.Current = true;

            EntityAttached?.Invoke(new EntityAttachedEventArgs(entity));
            entity.SendMessage(null, new PlayerAttachedMsg());

            // notify ECS Systems
            ControlledEntity.EntityManager.EventBus.RaiseEvent(EventSource.Local, new PlayerAttachSysMessage(ControlledEntity));
        }

        /// <summary>
        ///     Detaches the client from an entity.
        /// </summary>
        public void DetachEntity()
        {
            var previous = ControlledEntity;
            if (previous != null && previous.Initialized && !previous.Deleted)
            {
                previous.GetComponent<EyeComponent>().Current = false;
                previous.SendMessage(null, new PlayerDetachedMsg());

                // notify ECS Systems
                previous.EntityManager.EventBus.RaiseEvent(EventSource.Local, new PlayerAttachSysMessage(null));
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
