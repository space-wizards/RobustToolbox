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

        public NetConnection attachedClient = null;

        public Atom()
        {
            position = new Vector3(160, 0, 160);
            rotW = 1;
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
                    // TODO: route these messages through the player controller instead of directly to the atom. That way we can prevent client x from controlling client y's mob.
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
            SendInterpolationPacket(true); // Forcibly update the position of the node.
        }

        public void SendInterpolationPacket(bool force)
        {
            NetOutgoingMessage message = atomManager.netServer.netServer.CreateMessage();
            message.Write((byte)NetMessage.AtomManagerMessage);
            message.Write((byte)AtomManagerMessage.Passthrough);
            message.Write(uid);
            message.Write((byte)AtomMessage.InterpolationPacket);

            InterpolationPacket i = new InterpolationPacket((float)position.X, (float)position.Y, (float)position.Z, rotW, rotY, 0); // Fuckugly
            i.WriteMessage(message);

            /* VVVV This is the force flag. If this flag is set, the client will run the interpolation 
             * packet even if it is that client's player mob. Use this in case the client has ended up somewhere bad.
             */
            message.Write(force); 
            atomManager.netServer.SendMessageToAll(message);
        }

        public virtual void HandlePositionUpdate(NetIncomingMessage message)
        {
            /* This will be largely unused by the server, as the client shouldn't 
             * be able to move shit besides its associated player mob there may be 
             * cases when the client will need to move stuff around in a non-laggy 
             * way, but For now the only case I can think of is the player mob.*/
            // Hack to accept position updates from clients
            if (attachedClient != null && message.SenderConnection == attachedClient)
            {
                position.X = (double)message.ReadFloat();
                position.Y = (double)message.ReadFloat();
                position.Z = (double)message.ReadFloat();
                rotW = message.ReadFloat();
                rotY = message.ReadFloat();

                SendInterpolationPacket(false); // Send position updates to everyone. The client that is controlling this atom should discard this packet.
            }
            // Discard the rest.
        }
    }
}
