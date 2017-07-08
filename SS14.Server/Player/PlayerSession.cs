using SS14.Server.Interfaces.Player;
using SS14.Server.Interfaces.Round;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GameStates;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.ServerEnums;
using SS14.Server.GameObjects;
using System;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Network.Messages;

namespace SS14.Server.Player
{
    /// <summary>
    /// This is the session of a connected client.
    /// </summary>
    public class PlayerSession : IPlayerSession
    {
        private readonly PlayerManager _playerManager;
        public readonly PlayerState PlayerState;
        public PlayerSession(INetChannel client, PlayerManager playerManager)
        {
            _playerManager = playerManager;

            PlayerState = new PlayerState()
            {
                UniqueIdentifier = client.ConnectionId

            };

            if (client != null)
            {
                ConnectedClient = client;
                OnConnect();
            }
            else
                Status = SessionStatus.Zombie;

            UpdatePlayerState();
        }

        #region IPlayerSession Members

        public INetChannel ConnectedClient { get; }

        public IEntity attachedEntity { get; set; }
        public int? AttachedEntityUid => attachedEntity?.Uid;

        private string _name;
        public string Name
        {
            get => String.IsNullOrWhiteSpace(_name) ? _name : "Unknown";
            set => _name = value;
        }
        

        public SessionStatus Status { get; set; }

        public DateTime ConnectedTime { get; private set; }

        public void AttachToEntity(IEntity a)
        {
            DetachFromEntity();
            //a.attachedClient = connectedClient;
            //Add input component.
            var factory = IoCManager.Resolve<IComponentFactory>();
            a.AddComponent(ComponentFamily.Input, factory.GetComponent<KeyBindingInputComponent>());
            a.AddComponent(ComponentFamily.Mover, factory.GetComponent<PlayerInputMoverComponent>());

            BasicActorComponent actorComponent = factory.GetComponent<BasicActorComponent>();
            actorComponent.playerSession = this;
            a.AddComponent(ComponentFamily.Actor, actorComponent);

            attachedEntity = a;
            SendAttachMessage();
            SetAttachedEntityName();
            UpdatePlayerState();
        }

        public void DetachFromEntity()
        {
            if (attachedEntity == null) return;

            attachedEntity.RemoveComponent(ComponentFamily.Input);
            attachedEntity.RemoveComponent(ComponentFamily.Mover);
            attachedEntity.RemoveComponent(ComponentFamily.Actor);
            attachedEntity = null;
            UpdatePlayerState();
        }

        public void HandleNetworkMessage(MsgSession message)
        {
            var messageType = message.msgType;
            switch (messageType)
            {
                case PlayerSessionMessage.Verb:
                    HandleVerb(message);
                    break;
                case PlayerSessionMessage.JoinLobby:
                    JoinLobby();
                    break;
            }
        }

        public void SetName(string _name)
        {
            Name = _name;
            Logger.Log("Player set name: " + ConnectedClient.RemoteAddress + " -> " + Name);
            SetAttachedEntityName();
            UpdatePlayerState();
        }

        public void OnConnect()
        {
            ConnectedTime = DateTime.Now;
            Status = SessionStatus.Connected;
            UpdatePlayerState();
            //Put player in lobby immediately.
            Logger.Log("Player connected - " + ConnectedClient.RemoteAddress);
            JoinLobby();
        }

        public void OnDisconnect()
        {
            Status = SessionStatus.Disconnected;
            IoCManager.Resolve<IRoundManager>().CurrentGameMode.PlayerLeft(this);
            DetachFromEntity();
            UpdatePlayerState();
        }

        public void AddPostProcessingEffect(PostProcessingEffectType type, float duration)
        {
            var net = IoCManager.Resolve<INetServerManager>();
            var message = net.CreateNetMessage<MsgSession>();

            message.msgType = PlayerSessionMessage.AddPostProcessingEffect;
            message.PpType = type;
            message.PpDuration = duration;

            net.ServerSendMessage(message, ConnectedClient);
        }

        #endregion IPlayerSession Members

        private void SendAttachMessage()
        {
            if (attachedEntity == null)
                throw new Exception("Cannot attach player session to entity: No entity attached.");

            var net = IoCManager.Resolve<INetServerManager>();
            var message = net.CreateNetMessage<MsgSession>();

            message.msgType = PlayerSessionMessage.AttachToEntity;
            message.uid = attachedEntity.Uid;

            net.ServerSendMessage(message, ConnectedClient);
        }

        private void HandleVerb(MsgSession message)
        {
            DispatchVerb(message.verb, message.uid);
        }

        public void DispatchVerb(string verb, int uid)
        {
            //Handle global verbs
            Logger.Log("Verb: " + verb + " from " + uid, LogLevel.Debug);

            if (uid == 0)
            {
                switch (verb)
                {
                    case "joingame":
                        JoinGame();
                        break;
                    case "toxins":
                    //Need debugging function to add more gas
                    case "save":
                        _playerManager.Server.SaveGame();
                        break;
                }
            }
        }

        private void SetAttachedEntityName()
        {
            if (Name != null && attachedEntity != null)
            {
                attachedEntity.Name = Name;
            }
        }

        private void ResetAttachedEntityName()
        {
            if (attachedEntity != null)
                attachedEntity.Name = attachedEntity.Prototype.ID;
        }

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
            if (ConnectedClient == null || Status == SessionStatus.InGame || _playerManager.RunLevel != RunLevel.Game)
                return;

            var net = IoCManager.Resolve<INetServerManager>();
            var message = net.CreateNetMessage<MsgJoinGame>();
            net.ServerSendMessage(message, ConnectedClient);

            Status = SessionStatus.InGame;
            UpdatePlayerState();
        }

        public void CreateGuiMessage(GuiComponentType gui)
        {
            var net = IoCManager.Resolve<INetServerManager>();
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
    }
}
