using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;
using SS13_Shared.GameStates;
using ServerServices;
using ServerInterfaces;
using ServerServices.Log;
using SS13.IoC;
using ServerInterfaces.Network;
using ServerInterfaces.GameObject;
using ServerInterfaces.Round;
using SS13_Shared.ServerEnums;
using ServerInterfaces.Player;

namespace ServerServices.Player
{
    public class PlayerSession : IPlayerSession
    {
        /* This class represents a connected player session */

        public NetConnection connectedClient { get; private set; }
        public NetConnection ConnectedClient { get { return connectedClient; } }
        private PlayerManager _playerManager;

        public IEntity attachedEntity { get; set; }
        public string name { get; set; }
        public SessionStatus status { get; set; }
        public AdminPermissions adminPermissions { get; set; }

        public BodyPart targetedArea = BodyPart.Torso;
        public BodyPart TargetedArea { get { return targetedArea; } }

        public JobDefinition assignedJob { get; set; }

        public PlayerState PlayerState;

        public PlayerSession(NetConnection client, PlayerManager playerManager)
        {
            _playerManager = playerManager;
            name = "";

            PlayerState = new PlayerState();
            PlayerState.UniqueIdentifier = client.RemoteUniqueIdentifier;

            if (client != null)
            {
                connectedClient = client;
                adminPermissions = new AdminPermissions();
                OnConnect();
            }
            else
                status = SessionStatus.Zombie;

            UpdatePlayerState();
        }

        public void AttachToEntity(IEntity a)
        {
            DetachFromEntity();
            //a.attachedClient = connectedClient;
            //Add input component.
            a.AddComponent(ComponentFamily.Input, _playerManager.server.EntityManager.ComponentFactory.GetComponent("KeyBindingInputComponent"));
            a.AddComponent(ComponentFamily.Mover, _playerManager.server.EntityManager.ComponentFactory.GetComponent("PlayerInputMoverComponent"));
            var actorComponent = _playerManager.server.EntityManager.ComponentFactory.GetComponent("BasicActorComponent");
            actorComponent.SetParameter(new ComponentParameter("playersession", this));
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

        private void SendAttachMessage()
        {
            NetOutgoingMessage m = IoCManager.Resolve<ISS13NetServer>().CreateMessage();
            m.Write((byte)NetMessage.PlayerSessionMessage);
            m.Write((byte)PlayerSessionMessage.AttachToEntity);
            m.Write(attachedEntity.Uid);
            IoCManager.Resolve<ISS13NetServer>().SendMessage(m, connectedClient);
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
                case PlayerSessionMessage.SetTargetArea:
                    var selected = (BodyPart)message.ReadByte();
                    targetedArea = selected;
                    break;
            }
        }

        private void HandleVerb(NetIncomingMessage message)
        {
            DispatchVerb(message.ReadString(), message.ReadInt32());
        }

        public void DispatchVerb(string verb, int uid)
        {
            //Handle global verbs
            LogManager.Log("Verb: " + verb + " from " + uid, LogLevel.Debug);
            
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



        public void SetName(string _name)
        {
            name = _name;
            LogManager.Log("Player set name: " + connectedClient.RemoteEndPoint.Address + " -> " + name);
            SetAttachedEntityName();
            UpdatePlayerState();
        }

        private void SetAttachedEntityName()
        {
            if(name != null && attachedEntity != null)
            {
                attachedEntity.Name = name;
            }
        }

        private void ResetAttachedEntityName()
        {
            attachedEntity.Name = attachedEntity.Template.Name;
        }
        
        public void JoinLobby()
        {
            /*var m = IoCManager.Resolve<ISS13NetServer>().CreateMessage();
            m.Write((byte)NetMessage.PlayerSessionMessage);
            m.Write((byte)PlayerSessionMessage.JoinLobby);
            IoCManager.Resolve<ISS13NetServer>().SendMessage(m, connectedClient);*/
            status = SessionStatus.InLobby;
            UpdatePlayerState();
        }

        public void JoinGame()
        {
            if (connectedClient != null && status != SessionStatus.InGame && _playerManager.server.Runlevel == RunLevel.Game)
            {
                var m = IoCManager.Resolve<ISS13NetServer>().CreateMessage();
                m.Write((byte)NetMessage.JoinGame);
                IoCManager.Resolve<ISS13NetServer>().SendMessage(m, connectedClient);

                status = SessionStatus.InGame;
                UpdatePlayerState();
            }
        }

        public void OnConnect()
        {
            status = SessionStatus.Connected;
            UpdatePlayerState();
            //Put player in lobby immediately.
            LogManager.Log("Player connected - " + connectedClient.RemoteEndPoint.Address);
            JoinLobby();
        }

        public void OnDisconnect()
        {
            status = SessionStatus.Disconnected;
            IoCManager.Resolve<IRoundManager>().CurrentGameMode.PlayerLeft(this);
            DetachFromEntity();
            UpdatePlayerState();

        }

        public NetOutgoingMessage CreateGuiMessage(GuiComponentType gui)
        {
            NetOutgoingMessage m = IoCManager.Resolve<ISS13NetServer>().CreateMessage();
            m.Write((byte)NetMessage.PlayerUiMessage);
            m.Write((byte)UiManagerMessage.ComponentMessage);
            m.Write((byte)gui);
            return m;
        }

        public void AddPostProcessingEffect(PostProcessingEffectType type, float duration)
        {
            var m = IoCManager.Resolve<ISS13NetServer>().CreateMessage();
            m.Write((byte)NetMessage.PlayerSessionMessage);
            m.Write((byte)PlayerSessionMessage.AddPostProcessingEffect);
            m.Write((int)type);
            m.Write(duration);
            IoCManager.Resolve<ISS13NetServer>().SendMessage(m, ConnectedClient, NetDeliveryMethod.ReliableUnordered);
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
