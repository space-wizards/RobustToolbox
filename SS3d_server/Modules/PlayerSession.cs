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

        public PlayerSession(NetConnection client, SS3DNetserver _netServer)
        {
            if(client != null)
                connectedClient = client;
            netServer = _netServer;
        }

        public void AttachToAtom(Atom.Atom a)
        {
            DetachFromAtom();
            a.attachedClient = connectedClient;
            attachedAtom = a;
            SendAttachMessage();
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
                default:
                    break;
            }
        }

        private void HandleVerb(NetIncomingMessage message)
        {
            DispatchVerb(message.ReadString());
        }

        public void DispatchVerb(string verb)
        {
            Atom.Mob.Mob m;
            //This will be replaced by a verb table
            //TODO build dynamic verb lookup table with delegate functions :D
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

        public void DetachFromAtom()
        {
            if (attachedAtom != null)
            {
                attachedAtom.attachedClient = null;
                attachedAtom = null;
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
    }
}
