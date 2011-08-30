using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Lidgren.Network;
using SS3D_Server.Atom;
using SS3D_Server;
using SS3D_shared;

namespace SS3D_Server.Modules
{
    public class PlayerSession
    {
        /* This class represents a connected player session */

        public NetConnection connectedClient;
        public Atom.Atom attachedAtom;
        public string name = "";
        public enum SessionStatus
        {
            Zombie,
            Connected,
            InLobby,
            InGame,
            Disconnected
        }
        public SessionStatus status;

        public PlayerSession(NetConnection client)
        {         
            if (client != null)
            {
                connectedClient = client;
                OnConnect();
            }
            else
                status = SessionStatus.Zombie;
        }

        public void AttachToAtom(Atom.Atom a)
        {
            DetachFromAtom();
            a.attachedClient = connectedClient;
            attachedAtom = a;
            SendAttachMessage();
        }

        public void DetachFromAtom()
        {
            if (attachedAtom != null)
            {
                attachedAtom.attachedClient = null;
                attachedAtom.Die();
                attachedAtom = null;
            }
        }

        private void SendAttachMessage()
        {
            NetOutgoingMessage m = SS3DNetServer.Singleton.CreateMessage();
            m.Write((byte)NetMessage.PlayerSessionMessage);
            m.Write((byte)PlayerSessionMessage.AttachToAtom);
            m.Write(attachedAtom.uid);
            SS3DServer.Singleton.SendMessageTo(m, connectedClient);
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
                default:
                    break;
            }
        }

        private void HandleVerb(NetIncomingMessage message)
        {
            DispatchVerb(message.ReadString(), message.ReadUInt16());
        }

        public void DispatchVerb(string verb, ushort uid)
        {
            //Handle global verbs
            Console.WriteLine(verb + " from " + uid);
            
            if (uid == 0)
            {
                Console.WriteLine(verb);
                switch (verb)
                {
                    case "joingame":
                        JoinGame();
                        break;
                    case "toxins":
                        //Need debugging function to add more gas
                    case "save":
                        SS3DServer.Singleton.atomManager.SaveAtoms();
                        SS3DServer.Singleton.map.SaveMap();
                        break;
                    default:
                        break;
                }
            }
            else
            {
                var targetAtom = SS3DServer.Singleton.atomManager.GetAtom(uid);
                targetAtom.HandleVerb(verb);
            }
        }



        public void SetName(string _name)
        {
            name = _name;
            if (attachedAtom != null)
            {
                attachedAtom.SetName(_name);
            }
        }

        public void JoinLobby()
        {
            NetOutgoingMessage m = SS3DNetServer.Singleton.CreateMessage();
            m.Write((byte)NetMessage.PlayerSessionMessage);
            m.Write((byte)PlayerSessionMessage.JoinLobby);
            SS3DServer.Singleton.SendMessageTo(m, connectedClient);
            status = SessionStatus.InLobby;
        }

        public void JoinGame()
        {
            if (connectedClient != null && status != SessionStatus.InGame && SS3DServer.Singleton.runlevel == SS3DServer.RunLevel.Game)
            {
                NetOutgoingMessage m = SS3DNetServer.Singleton.CreateMessage();
                m.Write((byte)NetMessage.JoinGame);
                SS3DServer.Singleton.SendMessageTo(m, connectedClient);

                status = SessionStatus.InGame;
            }
        }

        public void OnConnect()
        {
            status = SessionStatus.Connected;
            //Put player in lobby immediately.
            JoinLobby();
        }

        public void OnDisconnect()
        {
            status = SessionStatus.Disconnected;
            DetachFromAtom();
        }

        public NetOutgoingMessage CreateGuiMessage(GuiComponentType gui)
        {
            NetOutgoingMessage m = SS3DNetServer.Singleton.CreateMessage();
            m.Write((byte)NetMessage.PlayerSessionMessage);
            m.Write((byte)PlayerSessionMessage.UIComponentMessage);
            m.Write((byte)gui);
            return m;
        }
    }
}
