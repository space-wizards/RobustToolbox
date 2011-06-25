using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Lidgren.Network;
using SS3d_server.Atom;
using SS3d_server;

namespace SS3d_server.Modules
{
    public class PlayerSession
    {
        /* This class represents a connected player session */

        public NetConnection connectedClient;
        public Atom.Atom attachedAtom;
        private SS3DNetserver netServer;
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

        public PlayerSession(NetConnection client, SS3DNetserver _netServer)
        {
            netServer = _netServer;
         
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
            NetOutgoingMessage m = netServer.netServer.CreateMessage();
            m.Write((byte)NetMessage.PlayerSessionMessage);
            m.Write((byte)PlayerSessionMessage.AttachToAtom);
            m.Write(attachedAtom.uid);
            netServer.SendMessageTo(m, connectedClient);
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
            if (uid == 0)
            {
                Atom.Mob.Mob m;

                switch (verb)
                {
                    case "selectlefthand":
                        m = (Atom.Mob.Mob)attachedAtom;
                        m.selectedAppendage = m.appendages["LeftHand"];
                        break;
                    case "selectrighthand":
                        m = (Atom.Mob.Mob)attachedAtom;
                        m.selectedAppendage = m.appendages["RightHand"];
                        break;
                    default:
                        break;
                }
            }
            else
            {
                var targetAtom = netServer.atomManager.GetAtom(uid);
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
            NetOutgoingMessage m = netServer.netServer.CreateMessage();
            m.Write((byte)NetMessage.PlayerSessionMessage);
            m.Write((byte)PlayerSessionMessage.JoinLobby);
            netServer.SendMessageTo(m, connectedClient);
            status = SessionStatus.InLobby;
        }

        public void JoinGame()
        {
            if (connectedClient != null && status != SessionStatus.InGame)
            {
                NetOutgoingMessage m = netServer.netServer.CreateMessage();
                m.Write((byte)NetMessage.JoinGame);
                netServer.SendMessageTo(m, connectedClient);

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
    }
}
