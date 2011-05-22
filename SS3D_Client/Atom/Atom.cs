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
        public bool drawn = false;

        public string name;
        public ushort uid;
        public AtomManager atomManager;

        // Position data
        public Mogre.Vector3 position;
        public float rotW;
        public float rotY;
        public bool positionChanged = false;

        public List<InterpolationPacket> interpolationPackets;

        public Atom()
        {
            position = new Mogre.Vector3(0, 0, 0);
            rotW = 0;
            rotY = 0;

            interpolationPackets = new List<InterpolationPacket>();
        }

        public Atom(ushort _uid, AtomManager _atomManager)
        {
            uid = _uid;
            atomManager = _atomManager;

            position = new Mogre.Vector3(0, 0, 0);
            rotW = 0;
            rotY = 0;

            interpolationPackets = new List<InterpolationPacket>();

            Draw();
        }

        public void SetUp(ushort _uid, AtomManager _atomManager)
        {
            uid = _uid;
            atomManager = _atomManager;

            Draw();
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
                case AtomMessage.InterpolationPacket:
                    HandleInterpolationPacket(message);
                    break;
                default:
                    break;
            }
            return;
        }

        public virtual void HandleInterpolationPacket(NetIncomingMessage message)
        {
            SS3D_shared.HelperClasses.InterpolationPacket intPacket = new SS3D_shared.HelperClasses.InterpolationPacket(message);
            
            //Add an interpolation packet to the end of the list. If the list is more than 10 long, delete a packet.
            //TODO: For the Player class, override this function to do some sort of intelligent checking on the interpolation packets 
            // recieved to make sure they don't greatly disagree with the client's own data.
            interpolationPackets.Add(intPacket);

            if (interpolationPackets.Count > 10)
            {
                interpolationPackets.RemoveAt(0);
            }

            // Need an update.
            updateRequired = true;
        }

        public virtual void Update()
        {
            //If the node hasn't even been drawn into the scene, there's no point updating the fucker, is there?
            if (!drawn)
                return;
            //This lets the atom only update when it needs to. If it needs to update subsequent to this, the functions below will set that flag.
            updateRequired = false;

            if (interpolationPackets.Count > 0)
            {
                UpdatePosition();
            }
            
        }

        // Mobs may need to override this for animation, or they could use this.
        public virtual void UpdatePosition()
        {
            Mogre.Vector3 difference;
            float rotW, rotY;
            
            difference = interpolationPackets[0].position - Node.Position;
            
            // Set rotation. The packet may be rotation only.
            rotW = interpolationPackets[0].rotW;
            rotY = interpolationPackets[0].rotY;
            Node.SetOrientation(rotW, 0, rotY, 0);

            //Check interpolation packet to see if we're close enough to the interpolation packet on the top of the stack.
            if (difference.Length < 1)
            {
                interpolationPackets.RemoveAt(0);
                UpdatePosition(); // RECURSION :D - this discards interpolationpackets we don't need anymore.
            }
            else
            {
                //Distance between interpolation packet and current position is big, so we will move the node towards it.

                //This constant should be time interval based.
                //TODO: Make this better if it isn't good enough.
                difference /= 5;
                Node.Position += difference;
                updateRequired = true; // This interpolation packet and probably the ones after it are still useful, so we'll update again on the next cycle.
            }
        }

        public void Draw()
        {
            // Draw the atom into the scene. This should be called after instantiation.
            name = "Atom" + uid;
            SceneManager sceneManager = atomManager.mEngine.SceneMgr;

            string entityName = name;
            if (sceneManager.HasEntity(entityName))
            {
                sceneManager.DestroyEntity(entityName);
            }
            if (sceneManager.HasSceneNode(entityName))
            {
                sceneManager.DestroySceneNode(entityName);
            }
            Node = sceneManager.RootSceneNode.CreateChildSceneNode(entityName);
            Entity = sceneManager.CreateEntity(entityName, "male.mesh");
            Entity.UserObject = this;
            Node.Position = position;
            Node.AttachObject(Entity);
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
