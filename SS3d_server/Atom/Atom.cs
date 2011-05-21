using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.HelperClasses;
using Lidgren.Network;

namespace SS3d_server.Atom
{
    public class Atom // SERVER SIDE
    {
        public string name;
        public ushort uid;
        public AtomManager atomManager;

        // Position data
        public Vector3 position;
        public float rotW;
        public float rotY;

        public List<InterpolationPacket> interpolationPacket;

        public Atom()
        {
        }

        public virtual void Update()
        {
            //Updates the atom, item, whatever. This should be called from the atom manager's update queue.
        }

        public void HandleNetworkMessage(NetIncomingMessage message)
        {
            //Do nothing, this is a default atom. This should be overridden by the inheriting class.
            AtomMessage messageType = (AtomMessage)message.ReadByte();
            switch (messageType)
            {
                case AtomMessage.Pull:
                    // Pass a message to the atom in question
                    Push();
                    break;
                default:
                    break;
            }
            return;
        }

        public virtual void Push()
        {
            // Do nothing, this is a default atom and nothing needs to be pushed.
        }

    }
}
