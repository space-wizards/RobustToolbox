using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using SS3d_server.HelperClasses;
using Lidgren.Network;
using SS3D_shared.HelperClasses;
using SS3d_server.Atom.Extension;

namespace SS3d_server.Atom
{
    [Serializable()]
    public class Atom : ISerializable  // SERVER SIDE
    {
        
        #region variables
        // wat
        public string name;
        public ushort uid;
        public AtomManager atomManager;
        public bool updateRequired = false;

        // Extensions
        public List<Extension.Extension> extensions;

        // Position data
        public Vector2 position;
        public Vector2 offset;
        public float rotW;
        public float rotY;

        public int maxHealth = 100;
        public int currentHealth = 100; // By default health is 100
        public bool isDead = false;
        

        public List<InterpolationPacket> interpolationPacket;

        public NetConnection attachedClient = null;
        #endregion

        #region constructors and init
        public Atom()
        {
            position = new Vector2(192, 192);
            offset = new Vector2(0, 0);
            rotW = 1;
            rotY = 0;
            name = this.GetType().ToString();

            extensions = new List<Extension.Extension>();
        }

        public void SetUp(ushort _uid, AtomManager _atomManager)
        {
            uid = _uid;
            atomManager = _atomManager;
            updateRequired = true;
        }


        public virtual void SerializedInit()
        {
            // When things are created with reflection using serialization their default constructor
            // isn't called. Put things in here which need to be done when it's created.
        }

        public virtual void SendState()
        {
            // Sends the state to all
        }

        public virtual void SendState(NetConnection client)
        {
            /// This is empty because so far the only things that need it are items.
        }

        /// <summary>
        /// Used to cleanly destroy an atom.
        /// </summary>
        public virtual void Destruct()
        {

        }
        #endregion

        #region updating
        public virtual void Update(float framePeriod)
        {
            //Updates the atom, item, whatever. This should be called from the atom manager's update queue.
            updateRequired = false;

            foreach (Extension.Extension e in extensions)
                e.Update(framePeriod);
        }
        #endregion

        #region networking
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
                case AtomMessage.Click:
                    HandleClick(message);
                    break;
                case AtomMessage.Extended:
                    HandleExtendedMessage(message); // This will punt unhandled messages to a virtual method so derived classes can handle them.
                    break;
                default:
                    break;
            }
            return;
        }

        protected virtual void HandleExtendedMessage(NetIncomingMessage message)
        {
            //Override this to handle custom messages.
        }

        protected virtual void HandleClick(NetIncomingMessage message)
        {
            //base.HandleClick(message);
            //Who clicked us?
            Mob.Mob clicker = (Mob.Mob)atomManager.netServer.playerManager.GetSessionByConnection(message.SenderConnection).attachedAtom;
            if (clicker == null)
                return;

            Clicked(clicker);
        }

        public virtual void Push()
        {
            SendInterpolationPacket(true); // Forcibly update the position of the node.
        }

        public void SendInterpolationPacket(bool force)
        {
            NetOutgoingMessage message = CreateAtomMessage();
            message.Write((byte)AtomMessage.InterpolationPacket);

            InterpolationPacket i = new InterpolationPacket((float)position.X, (float)position.Y, rotW, rotY, 0); // Fuckugly
            i.WriteMessage(message);

            /* VVVV This is the force flag. If this flag is set, the client will run the interpolation 
             * packet even if it is that client's player mob. Use this in case the client has ended up somewhere bad.
             */
            message.Write(force);
            atomManager.netServer.SendMessageToAll(message, NetDeliveryMethod.ReliableUnordered);
        }

        protected NetOutgoingMessage CreateAtomMessage()
        {
            NetOutgoingMessage message = atomManager.netServer.netServer.CreateMessage();
            message.Write((byte)NetMessage.AtomManagerMessage);
            message.Write((byte)AtomManagerMessage.Passthrough);
            message.Write(uid);
            return message;
        }

        protected void SendMessageToAll(NetOutgoingMessage message, NetDeliveryMethod method = NetDeliveryMethod.ReliableOrdered)
        {
            atomManager.netServer.SendMessageToAll(message, method);
        }

        protected void SendMessageTo(NetOutgoingMessage message, NetConnection client , NetDeliveryMethod method = NetDeliveryMethod.ReliableOrdered)
        {
            atomManager.netServer.SendMessageTo(message, client, method);
        }

        public virtual void HandlePositionUpdate(NetIncomingMessage message)
        {
            /* This will be largely unused by the server, as the client shouldn't 
             * be able to move shit besides its associated player mob there may be 
             * cases when the client will need to move stuff around in a non-laggy 
             * way, but For now the only case I can think of is the player mob.*/
            // Hack to accept position updates from clients
            if (attachedClient != null && message.SenderConnection == attachedClient && !IsDead())
            {
                position.X = (double)message.ReadFloat();
                position.Y = (double)message.ReadFloat();
                rotW = message.ReadFloat();
                rotY = message.ReadFloat();

                SendInterpolationPacket(false); // Send position updates to everyone. The client that is controlling this atom should discard this packet.
            }
            else
                SendInterpolationPacket(true); // If its dead, it should update everyone (prevents movement after death)
            // Discard the rest.
        }


        // For movement only
        public virtual void Translate(Vector2 newPosition)
        {
            position.X = newPosition.X;
            position.Y = newPosition.Y;

            SendInterpolationPacket(false);
        }

        // For rotation only
        public virtual void Translate(float W, float Y)
        {
            rotW = W;
            rotY = Y;

            SendInterpolationPacket(false);
        }

        // For both movement and rotation
        public virtual void Translate(Vector2 newPosition, float W, float Y)
        {
            position.X = newPosition.X;
            position.Y = newPosition.Y;

            rotW = W;
            rotY = Y;

            SendInterpolationPacket(false);
        }

        public virtual void HandleVerb(string verb)
        {

        }
        #endregion

        #region input handling
        protected virtual void Clicked(Mob.Mob clicker)
        {
            Vector2 dist = clicker.position - position;

            //If we're too far away
            if (dist.Magnitude > 96)
                return;


            /// TODO: add intent handling
            if (clicker.selectedAppendage.heldItem == null)
                ApplyAction(null, clicker);
            else
                ApplyAction(clicker.selectedAppendage.heldItem, clicker);

            atomManager.netServer.chatManager.SendChatMessage(0, clicker.name + "(" + clicker.uid.ToString() + ")" + " clicked " + name + "(" + uid.ToString() + ")", name, uid);
        }
        #endregion

        #region inter-atom functions
        /// <summary>
        /// Applies the specified atom to this atom. This should always have an originating mob m. 
        /// </summary>
        /// <param name="a">Atom that has been used on this one</param>
        /// <param name="m">Mob that used a on this one</param>
        protected virtual void ApplyAction(Atom a, Mob.Mob m)
        {
            m.selectedAppendage.heldItem.UsedOn(this); //Technically this is the same fucking thing as a, but i dont want to fuck with explicit casting it.

            //apply extension actions
            foreach (Extension.Extension e in extensions)
                e.ApplyAction(a, m);
        }

        /// <summary>
        /// This is a base method to allow any atom to be used on any other atom. 
        /// It is mainly useful for items, though it may be useful for other things
        /// down the road.
        /// </summary>
        /// <param name="target">Atom for this atom to be used on</param>
        protected virtual void UsedOn(Atom target)
        {
            //Apply extensions
            foreach (Extension.Extension e in extensions)
                e.UsedOn(target);
        }
        #endregion

        #region utility functions
        public void SetName(string _name)
        {
            name = _name;
        }

        /// <summary>
        /// Checks if the atom is a child / derived from the passed in type.
        /// </summary>
        /// <param name="type">Use typeof() on the type you want to check. For example, typeof(Item.Tool.Crowbar)</param>
        /// <returns>True if a child, false otherwise.</returns>
        public bool IsChildOfType(Type type)
        {
            if(this.GetType().IsSubclassOf(type))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if the atom is of the type passed in.
        /// </summary>
        /// <param name="type">Use typeof() on the type you want to check. For example, typeof(Item.Tool.Crowbar)</param>
        /// <returns>True if is the select type, false otherwise.</returns>
        public bool IsTypeOf(Type type)
        {
            if (this.GetType() == type)
            {
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Apply damage to the atom. All atoms have this, though not all atoms will react to their health being depleted.
        /// </summary>
        /// <param name="amount"></param>
        public virtual void Damage(int amount)
        {
            //Lots of room to get more complicated here
            currentHealth -= amount;
        #if DEBUG
            string healthmsg = name + "(" + uid.ToString() + ") " + "took " + amount.ToString() + " points of damage.";
            healthmsg += " Current health: " + currentHealth.ToString() + "/" + maxHealth.ToString();//TODO SEND DAMAGE MESSAGES
            atomManager.netServer.chatManager.SendChatMessage(0, healthmsg, name, uid);
        #endif
        }

        public bool IsDead()
        {
            if (currentHealth <= 0)
                isDead = true;
            
            return isDead;
        }

        public virtual void Die()
        {
            isDead = true;
        }
        #endregion

        #region Serialization

        public Atom(SerializationInfo info, StreamingContext ctxt)
        {
            name = (string)info.GetValue("name", typeof(string));
            position = (Vector2)info.GetValue("position", typeof(Vector2));
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            info.AddValue("name", name);
            info.AddValue("position", position);
        }

        #endregion
    }
}
