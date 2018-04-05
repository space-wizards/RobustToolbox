using System;
using SS14.Client.GameObjects;
using SS14.Client.Interfaces.GameObjects;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Shared;
using SS14.Shared.Enums;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Network.Messages;
using SS14.Shared.Players;

namespace SS14.Client.Player
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
        public event EventHandler EntityAttached;

        /// <summary>
        ///     An entity has been detached from the local player.
        /// </summary>
        public event EventHandler EntityDetached;

        /// <summary>
        ///     Game entity that the local player is controlling. If this is null, the player
        ///     is in free/spectator cam.
        /// </summary>
        public IEntity ControlledEntity { get; private set; }

        /// <summary>
        ///     Index of the client's player session.
        /// </summary>
        public PlayerIndex Index { get; set; }

        /// <summary>
        ///     Session of the local client.
        /// </summary>
        public PlayerSession Session { get; set; }

        /// <summary>
        ///     In game name of the local player.
        /// </summary>
        public string Name
        {
            get => _configManager.GetCVar<string>("player.name");
            set => SetName(value);
        }

        /// <summary>
        ///     The client's entity has moved. This only is raised when the player is attached to an entity.
        /// </summary>
        public event EventHandler<MoveEventArgs> EntityMoved;

        /// <summary>
        ///     The status of the client's session has changed.
        /// </summary>
        public event EventHandler<StatusEventArgs> StatusChanged;

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

            if (ControlledEntity.HasComponent<IMoverComponent>())
                ControlledEntity.RemoveComponent<IMoverComponent>();

            ControlledEntity.AddComponent<PlayerInputMoverComponent>();

            if (!ControlledEntity.HasComponent<ICollidableComponent>())
                ControlledEntity.AddComponent<GodotCollidableComponent>();

            if (!ControlledEntity.TryGetComponent<EyeComponent>(out var eye))
            {
                eye = ControlledEntity.AddComponent<EyeComponent>();
            }
            eye.Current = true;

            var transform = ControlledEntity.GetComponent<IGodotTransformComponent>();
            transform.OnMove += OnPlayerMoved;

            EntityAttached?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        ///     Detaches the client from an entity.
        /// </summary>
        public void DetachEntity()
        {
            if (ControlledEntity != null && ControlledEntity.Initialized)
            {
                ControlledEntity.RemoveComponent<PlayerInputMoverComponent>();
                ControlledEntity.RemoveComponent<ICollidableComponent>();
                ControlledEntity.GetComponent<EyeComponent>().Current = false;
                var transform = ControlledEntity.GetComponent<ITransformComponent>();
                if (transform != null)
                    transform.OnMove -= OnPlayerMoved;
            }
            ControlledEntity = null;

            EntityDetached?.Invoke(this, EventArgs.Empty);
        }

        private void OnPlayerMoved(object sender, MoveEventArgs args)
        {
            EntityMoved?.Invoke(sender, args);
        }

        /// <summary>
        ///     Changes the state of the session.
        /// </summary>
        public void SwitchState(SessionStatus newStatus)
        {
            var args = new StatusEventArgs(Session.Status, newStatus);
            Session.Status = newStatus;
            StatusChanged?.Invoke(this, args);
        }

        private void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return;
            var fixedName = name.Trim();
            if (fixedName.Length < 3)
                fixedName = $"Player {Session.Index}";

            _configManager.SetCVar("player.name", fixedName);

            if (!_networkManager.IsConnected)
                return;

            var msg = _networkManager.CreateNetMessage<MsgClGreet>();
            msg.PlyName = name;
            _networkManager.ClientSendMessage(msg);
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
}
