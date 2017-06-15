using Lidgren.Network;
using SS14.Server.Interfaces.Network;
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

namespace SS14.Server.Player
{
    public class PlayerSession : IPlayerSession
    {
        /* This class represents a connected player session */

        private readonly PlayerManager _playerManager;
        public PlayerState PlayerState;
        public PlayerSession(NetConnection client, PlayerManager playerManager)
        {
            _playerManager = playerManager;
            name = "";

            PlayerState = new PlayerState()
            {
                UniqueIdentifier = client.RemoteUniqueIdentifier
            };
            if (client != null)
            {
                connectedClient = client;
                OnConnect();
            }
            else
                status = SessionStatus.Zombie;

            UpdatePlayerState();
        }

        #region IPlayerSession Members

        public NetConnection connectedClient { get; private set; }

        public NetConnection ConnectedClient
        {
            get { return connectedClient; }
        }

        public IEntity attachedEntity { get; set; }
        public int? AttachedEntityUid
        {
            get { return attachedEntity == null ? null : (int?)attachedEntity.Uid; }
        }
        public string name { get; set; }
        public SessionStatus status { get; set; }

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

        public void HandleNetworkMessage(NetIncomingMessage message)
        {
            var messageType = (PlayerSessionMessage)message.ReadByte();
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
            name = _name;
            Logger.Log("Player set name: " + connectedClient.RemoteEndPoint.Address + " -> " + name);
            SetAttachedEntityName();
            UpdatePlayerState();
        }

        public void OnConnect()
        {
            ConnectedTime = DateTime.Now;
            status = SessionStatus.Connected;
            UpdatePlayerState();
            //Put player in lobby immediately.
            Logger.Log("Player connected - " + connectedClient.RemoteEndPoint.Address);
            JoinLobby();
        }

        public void OnDisconnect()
        {
            status = SessionStatus.Disconnected;
            IoCManager.Resolve<IRoundManager>().CurrentGameMode.PlayerLeft(this);
            DetachFromEntity();
            UpdatePlayerState();
        }

        public void AddPostProcessingEffect(PostProcessingEffectType type, float duration)
        {
            NetOutgoingMessage m = IoCManager.Resolve<ISS14NetServer>().CreateMessage();
            m.Write((byte)NetMessage.PlayerSessionMessage);
            m.Write((byte)PlayerSessionMessage.AddPostProcessingEffect);
            m.Write((int)type);
            m.Write(duration);
            IoCManager.Resolve<ISS14NetServer>().SendMessage(m, ConnectedClient, NetDeliveryMethod.ReliableUnordered);
        }

        #endregion IPlayerSession Members

        private void SendAttachMessage()
        {
            if (attachedEntity == null) //TODO proper exception
                throw new Exception("Cannot attach player session to entity: No entity attached.");
            NetOutgoingMessage m = IoCManager.Resolve<ISS14NetServer>().CreateMessage();
            m.Write((byte)NetMessage.PlayerSessionMessage);
            m.Write((byte)PlayerSessionMessage.AttachToEntity);
            m.Write(attachedEntity.Uid);
            IoCManager.Resolve<ISS14NetServer>().SendMessage(m, connectedClient);
        }

        private void HandleVerb(NetIncomingMessage message)
        {
            DispatchVerb(message.ReadString(), message.ReadInt32());
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
                        _playerManager.server.SaveEntities();
                        _playerManager.server.SaveMap();
                        break;
                }
            }
        }

        private void SetAttachedEntityName()
        {
            if (name != null && attachedEntity != null)
            {
                attachedEntity.Name = name;
            }
        }

        private void ResetAttachedEntityName()
        {
            if (attachedEntity != null)
                attachedEntity.Name = attachedEntity.Prototype.ID;
        }

        public void JoinLobby()
        {
            /*var m = IoCManager.Resolve<ISS13NetServer>().CreateMessage();
            m.Write((byte)NetMessage.PlayerSessionMessage);
            m.Write((byte)PlayerSessionMessage.JoinLobby);
            IoCManager.Resolve<ISS13NetServer>().SendMessage(m, connectedClient);*/
            DetachFromEntity();
            status = SessionStatus.InLobby;
            UpdatePlayerState();
        }

        public void JoinGame()
        {
            if (connectedClient != null && status != SessionStatus.InGame &&
                _playerManager.server.Runlevel == RunLevel.Game)
            {
                NetOutgoingMessage m = IoCManager.Resolve<ISS14NetServer>().CreateMessage();
                m.Write((byte)NetMessage.JoinGame);
                IoCManager.Resolve<ISS14NetServer>().SendMessage(m, connectedClient);

                status = SessionStatus.InGame;
                UpdatePlayerState();
            }
        }

        public NetOutgoingMessage CreateGuiMessage(GuiComponentType gui)
        {
            NetOutgoingMessage m = IoCManager.Resolve<ISS14NetServer>().CreateMessage();
            m.Write((byte)NetMessage.PlayerUiMessage);
            m.Write((byte)UiManagerMessage.ComponentMessage);
            m.Write((byte)gui);
            return m;
        }

        private void UpdatePlayerState()
        {
            PlayerState.Status = status;
            PlayerState.Name = name;
            if (attachedEntity == null)
                PlayerState.ControlledEntity = null;
            else
                PlayerState.ControlledEntity = attachedEntity.Uid;
        }
    }
}
