using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Mogre;
using SS3D_shared.HelperClasses;
using Lidgren.Network;

namespace SS3D.Atom
{
    public class Atom // CLIENT SIDE
    {
        // GRAPHICS
        public SceneNode Node;
        public Entity Entity;
        public string meshName = "ogrehead.mesh"; // Ogrehead is a nice default mesh. This prevents any atom from inadvertently spawning without a mesh.
        public bool updateRequired = false;

        public string name;
        public ushort uid;
        public AtomManager atomManager;

        // Position data
        public SS3D_shared.HelperClasses.Vector3 position;
        public float rotW;
        public float rotY;

        public List<InterpolationPacket> interpolationPacket;

        public Atom()
        {
        }

        public void HandleNetworkMessage(NetIncomingMessage message)
        {
            //Pass on a push message.
            AtomMessage messageType = (AtomMessage)message.ReadByte();
            switch (messageType)
            {
                case AtomMessage.Push:
                    // Pass a message to the atom in question
                    HandlePush(message);
                    break;
                default:
                    break;
            }
            return;
        }

        public void Update()
        { 
        
        }

        // Sends a message to the server to request the atom's data.
        public void SendPullMessage()
        {
            NetOutgoingMessage message = atomManager.networkManager.netClient.CreateMessage();
            message.Write((byte)NetMessage.AtomManagerMessage);
            message.Write((byte)AtomManagerMessage.Passthrough);
            message.Write((ushort)uid);
            message.Write((byte)AtomMessage.Pull);
            atomManager.networkManager.SendMessage(message, NetDeliveryMethod.Unreliable);
        }

        public virtual void HandlePush(NetIncomingMessage message)
        {
            // Do nothing. This should be overridden by the child.
        }
    }
}
