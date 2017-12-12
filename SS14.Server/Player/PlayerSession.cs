using SS14.Server.Interfaces.GameObjects;
using SS14.Server.Interfaces.Player;
using SS14.Server.Interfaces.Round;
using SS14.Shared;
using SS14.Shared.GameStates;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.ServerEnums;
using SS14.Server.GameObjects;
using System;
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

            PlayerState = new PlayerState()
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
        public string Name
        {
            get => string.IsNullOrWhiteSpace(_name) ? "Unknown" : _name;
            set => _name = value;
        }

        public SessionStatus Status { get; set; }

        public DateTime ConnectedTime { get; private set; }

        public PlayerIndex Index { get; }

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

        public void DetachFromEntity()
        {
            if (attachedEntity == null) return;

            attachedEntity.RemoveComponent<KeyBindingInputComponent>();
            attachedEntity.RemoveComponent<PlayerInputMoverComponent>();
            attachedEntity.RemoveComponent<BasicActorComponent>();
            attachedEntity = null;
            UpdatePlayerState();
        }

        public void HandleNetworkMessage(MsgSession message)
        {
            var messageType = message.MsgType;
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

        public void SetName(string name)
        {
            Name = name;
            Logger.Log($"[SRV] {ConnectedClient.RemoteAddress}: Player set name: {Name}");
            SetAttachedEntityName();
            UpdatePlayerState();
        }

        public void OnConnect()
        {
            ConnectedTime = DateTime.Now;
            Status = SessionStatus.Connected;
            UpdatePlayerState();
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

        private void HandleVerb(MsgSession message)
        {
            DispatchVerb(message.Verb, message.Uid);
        }

        private void DispatchVerb(string verb, int uid)
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

            var net = IoCManager.Resolve<IServerNetManager>();
            var message = net.CreateNetMessage<MsgJoinGame>();
            net.ServerSendMessage(message, ConnectedClient);

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
    }
}
