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
        public bool updateRequired = false;

        // Position data
        public Vector3 position;
        public float rotW;
        public float rotY;

        public List<InterpolationPacket> interpolationPacket;

        public Atom()
        {
            position = new Vector3(160, 0, 160);
            rotW = 0;
            rotY = 0;
        }

        public void SetUp(ushort _uid, AtomManager _atomManager)
        {
            uid = _uid;
            atomManager = _atomManager;
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
                case AtomMessage.PositionUpdate:
                    // We'll accept position packets from the client so that movement doesn't lag. There may be other special cases like this.
                    HandlePositionUpdate(message);
                    break;
                default:
                    break;
            }
            return;
        }

        public virtual void Push()
        {
            // Do nothing, this is a default atom and nothing needs to be pushed.
            SendInterpolationPacket();
        }

        public void SendInterpolationPacket()
        {
            NetOutgoingMessage message = atomManager.netServer.netServer.CreateMessage();
            message.Write((byte)NetMessage.AtomManagerMessage);
            message.Write((byte)AtomManagerMessage.Passthrough);
            message.Write(uid);
            message.Write((byte)AtomMessage.InterpolationPacket);

            InterpolationPacket i = new InterpolationPacket((float)position.X, (float)position.Y, (float)position.Z, rotW, rotY, 0); // Fuckugly
            i.WriteMessage(message);
            atomManager.netServer.SendMessageToAll(message);
        }

        public virtual void HandlePositionUpdate(NetIncomingMessage message)
        {
            //This will be largely ignored by the server, as the client shouldn't be able to move shit besides its associated player mob
            //There may be cases when the client will need to move stuff around in a non-laggy way, but For now the only case I can think of is the player mob.
        }
    }
}
