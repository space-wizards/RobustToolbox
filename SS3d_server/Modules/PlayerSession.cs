using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Lidgren.Network;
using SS13_Server;
using SS13_Shared;
using SS13_Shared.GO;
using SGO;
using ServerServices;
using ServerInterfaces;

namespace SS13_Server.Modules
{
    public class PlayerSession : IPlayerSession
    {
        /* This class represents a connected player session */

        public NetConnection connectedClient;
        public NetConnection ConnectedClient { get { return connectedClient; } }

        public Entity attachedEntity;
        public string name = "";
        public SessionStatus status;
        public AdminPermissions adminPermissions;

        public BodyPart targetedArea = BodyPart.Torso;
        public BodyPart TargetedArea { get { return targetedArea; } }

        public JobDefinition assignedJob;

        public PlayerSession(NetConnection client)
        {         
            if (client != null)
            {
                connectedClient = client;
                adminPermissions = new AdminPermissions();
                OnConnect();
            }
            else
                status = SessionStatus.Zombie;
        }

        public void AttachToEntity(Entity a)
        {
            DetachFromEntity();
            //a.attachedClient = connectedClient;
            //Add input component.
            a.AddComponent(ComponentFamily.Input, SGO.ComponentFactory.Singleton.GetComponent("KeyBindingInputComponent"));
            a.AddComponent(ComponentFamily.Mover, ComponentFactory.Singleton.GetComponent("PlayerInputMoverComponent"));
            var actorComponent = (BasicActorComponent)ComponentFactory.Singleton.GetComponent("BasicActorComponent");
            actorComponent.SetParameter(new ComponentParameter("playersession", typeof(IPlayerSession), this));
            a.AddComponent(ComponentFamily.Actor, actorComponent);

            attachedEntity = a;
            SendAttachMessage();
        }

        public void DetachFromEntity()
        {
            if (attachedEntity == null) return;

            attachedEntity.RemoveComponent(ComponentFamily.Input);
            attachedEntity.RemoveComponent(ComponentFamily.Mover);
            attachedEntity.RemoveComponent(ComponentFamily.Actor);
            attachedEntity = null;
        }

        private void SendAttachMessage()
        {
            NetOutgoingMessage m = SS13NetServer.Singleton.CreateMessage();
            m.Write((byte)NetMessage.PlayerSessionMessage);
            m.Write((byte)PlayerSessionMessage.AttachToEntity);
            m.Write(attachedEntity.Uid);
            SS13NetServer.Singleton.SendMessage(m, connectedClient);
        }

        public void HandleNetworkMessage(NetIncomingMessage message)
        {
            PlayerSessionMessage messageType = (PlayerSessionMessage)message.ReadByte();
            switch (messageType)
            {
                case PlayerSessionMessage.Verb:
                    HandleVerb(message);
                    break;
                case PlayerSessionMessage.JoinLobby:
                    JoinLobby();
                    break;
                case PlayerSessionMessage.SetTargetArea:
                    BodyPart selected = (BodyPart)message.ReadByte();
                    targetedArea = selected;
                    break;
                default:
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
                        EntityManager.Singleton.SaveEntities();
                        SS13Server.Singleton.map.SaveMap();
                        break;
                    default:
                        break;
                }
            }
        }



        public void SetName(string _name)
        {
            name = _name;
            LogManager.Log("Player set name: " + connectedClient.RemoteEndpoint.Address.ToString() + " -> " + name);
            if (attachedEntity != null)
            {
                attachedEntity.name = _name;
            }
        }

        public void JoinLobby()
        {
            NetOutgoingMessage m = SS13NetServer.Singleton.CreateMessage();
            m.Write((byte)NetMessage.PlayerSessionMessage);
            m.Write((byte)PlayerSessionMessage.JoinLobby);
            SS13NetServer.Singleton.SendMessage(m, connectedClient);
            status = SessionStatus.InLobby;
        }

        public void JoinGame()
        {
            if (connectedClient != null && status != SessionStatus.InGame && SS13Server.Singleton.runlevel == SS13Server.RunLevel.Game)
            {
                NetOutgoingMessage m = SS13NetServer.Singleton.CreateMessage();
                m.Write((byte)NetMessage.JoinGame);
                SS13NetServer.Singleton.SendMessage(m, connectedClient);

                status = SessionStatus.InGame;
            }
        }

        public void OnConnect()
        {
            status = SessionStatus.Connected;
            //Put player in lobby immediately.
            LogManager.Log("Player connected - " + connectedClient.RemoteEndpoint.Address.ToString());
            JoinLobby();
        }

        public void OnDisconnect()
        {
            status = SessionStatus.Disconnected;
            DetachFromEntity();
        }

        public NetOutgoingMessage CreateGuiMessage(GuiComponentType gui)
        {
            NetOutgoingMessage m = SS13NetServer.Singleton.CreateMessage();
            m.Write((byte)NetMessage.PlayerUiMessage);
            m.Write((byte)UiManagerMessage.ComponentMessage);
            m.Write((byte)gui);
            return m;
        }
    }
}
