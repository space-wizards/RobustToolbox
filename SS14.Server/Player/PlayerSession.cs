using SS14.Server.Interfaces.GameObjects;
using SS14.Server.Interfaces.Player;
using SS14.Shared;
using SS14.Shared.GameStates;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Server.GameObjects;
using System;
using SS14.Server.Interfaces;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Network.Messages;
using SS14.Shared.Players;

namespace SS14.Server.Player
{
    /// <summary>
    /// This is the session of a connected client.
    /// </summary>
    public class PlayerSession : IPlayerSession
    {
        private readonly PlayerManager _playerManager;
        public readonly PlayerState PlayerState;

        public PlayerSession(PlayerManager playerManager, INetChannel client, PlayerIndex index)
        {
            _playerManager = playerManager;
            Index = index;

            PlayerState = new PlayerState
            {
                Uuid = client.ConnectionId,
                Index = index,
            };

            ConnectedClient = client;
            OnConnect();

            UpdatePlayerState();
        }

        public INetChannel ConnectedClient { get; }

        public IEntity attachedEntity { get; set; }
        public int? AttachedEntityUid => attachedEntity?.Uid;

        private string _name;
        private SessionStatus _status;

        /// <inheritdoc />
        public string Name
        {
            get => string.IsNullOrWhiteSpace(_name) ? "<TERU-SAMA>" : _name;
            set => _name = value;
        }

        /// <inheritdoc />
        public SessionStatus Status
        {
            get => _status;
            set => OnPlayerStatusChanged(_status, value);
        }

        /// <inheritdoc />
        public DateTime ConnectedTime { get; private set; }

        /// <inheritdoc />
        public PlayerIndex Index { get; }

        /// <inheritdoc />
        public event EventHandler<SessionStatusEventArgs> PlayerStatusChanged;

        private void OnPlayerStatusChanged(SessionStatus oldStatus, SessionStatus newStatus)
        {
            if(oldStatus == newStatus)
                return;

            _status = newStatus;
            PlayerStatusChanged?.Invoke(this, new SessionStatusEventArgs(this, oldStatus, newStatus));
        }

        /// <inheritdoc />
        public void AttachToEntity(IEntity a)
        {
            DetachFromEntity();

            //Add input component.
            var factory = IoCManager.Resolve<IComponentFactory>();
            a.AddComponent(factory.GetComponent<KeyBindingInputComponent>());
            if (a.HasComponent<IMoverComponent>())
            {
                a.RemoveComponent<IMoverComponent>();
            }
            a.AddComponent(factory.GetComponent<PlayerInputMoverComponent>());

            BasicActorComponent actorComponent = factory.GetComponent<BasicActorComponent>();
            actorComponent.playerSession = this;
            a.AddComponent(actorComponent);

            attachedEntity = a;
            SendAttachMessage();
            SetAttachedEntityName();
            UpdatePlayerState();
        }

        /// <inheritdoc />
        public void DetachFromEntity()
        {
            if (attachedEntity == null) return;

            attachedEntity.RemoveComponent<KeyBindingInputComponent>();
            attachedEntity.RemoveComponent<PlayerInputMoverComponent>();
            attachedEntity.RemoveComponent<BasicActorComponent>();
            attachedEntity = null;
            UpdatePlayerState();
        }
        
        /// <inheritdoc />
        public void SetName(string name)
        {
            Name = name;
            Logger.Log($"[SRV] {ConnectedClient.RemoteAddress}: Player set name: {Name}");
            SetAttachedEntityName();
            UpdatePlayerState();
        }

        /// <inheritdoc />
        public void OnConnect()
        {
            ConnectedTime = DateTime.Now;
            Status = SessionStatus.Connected;
            UpdatePlayerState();
        }

        /// <inheritdoc />
        public void OnDisconnect()
        {
            Status = SessionStatus.Disconnected;

            // TODO: PlayerLeaveServer event

            DetachFromEntity();
            UpdatePlayerState();
        }

        /// <inheritdoc />
        public void AddPostProcessingEffect(PostProcessingEffectType type, float duration)
        {
            var net = IoCManager.Resolve<IServerNetManager>();
            var message = net.CreateNetMessage<MsgSession>();

            message.MsgType = PlayerSessionMessage.AddPostProcessingEffect;
            message.PpType = type;
            message.PpDuration = duration;

            net.ServerSendMessage(message, ConnectedClient);
        }
        
        private void SendAttachMessage()
        {
            if (attachedEntity == null)
                throw new Exception("Cannot attach player session to entity: No entity attached.");

            var net = IoCManager.Resolve<IServerNetManager>();
            var message = net.CreateNetMessage<MsgSession>();

            message.MsgType = PlayerSessionMessage.AttachToEntity;
            message.Uid = attachedEntity.Uid;

            net.ServerSendMessage(message, ConnectedClient);
        }
        
        private void SetAttachedEntityName()
        {
            if (Name != null && attachedEntity != null)
            {
                attachedEntity.Name = Name;
            }
        }

        /// <inheritdoc />
        public void JoinLobby()
        {
            DetachFromEntity();
            Status = SessionStatus.InLobby;
            UpdatePlayerState();
        }

        /// <summary>
        ///     Causes the session to switch from the lobby to the game.
        /// </summary>
        public void JoinGame()
        {
            var baseServer = IoCManager.Resolve<IBaseServer>();

            if (ConnectedClient == null || Status == SessionStatus.InGame || baseServer.RunLevel != ServerRunLevel.Game)
                return;
            
            Status = SessionStatus.InGame;
            UpdatePlayerState();
        }

        public void CreateGuiMessage(GuiComponentType gui)
        {
            var net = IoCManager.Resolve<IServerNetManager>();
            var message = net.CreateNetMessage<MsgUi>();

            message.UiType = UiManagerMessage.ComponentMessage;
            message.CompType = gui;

            net.ServerSendMessage(message, ConnectedClient);
        }

        private void UpdatePlayerState()
        {
            PlayerState.Status = Status;
            PlayerState.Name = Name;
            if (attachedEntity == null)
                PlayerState.ControlledEntity = null;
            else
                PlayerState.ControlledEntity = attachedEntity.Uid;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"[{Index}]{Name}";
        }
    }
}
