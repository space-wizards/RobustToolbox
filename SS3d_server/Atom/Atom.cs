using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Lidgren.Network;
using SS3D_Server.Atom.Extension;
using SS3D_Server.HelperClasses;
using SS3D_Server.Modules;
using SS3D_shared;
using SS3D_shared.HelperClasses;
using SGO;

namespace SS3D_Server.Atom
{
    [Serializable()]
    public class Atom : Entity, ISerializable  // SERVER SIDE
    {
       
        #region variables
        // wat
        public AtomManager atomManager;
        public bool updateRequired = false;
        public int drawDepth = 0;
        public int spritestate = 0; //This is the sprite state so the client knows which of the atom's defined sprites to display.
        public bool damageable = false;
        public bool collidable = false;

        public Atom[] linkedAtoms = new Atom[4]; //0 = North, 1 = East, 2 = South, 3 = West

        public Tiles.Tile spawnTile; //The tile this atom spawned on. Used for wall mounted items etc.

        // Extensions
        public List<Extension.Extension> extensions;

        // Position data

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
            rotation = 0;
            name = this.GetType().ToString();

            AddComponent(SS3D_shared.GO.ComponentFamily.Click, ComponentFactory.Singleton.GetComponent("ClickableComponent"));
            AddComponent(SS3D_shared.GO.ComponentFamily.Renderable, ComponentFactory.Singleton.GetComponent("SpriteComponent"));
            AddComponent(SS3D_shared.GO.ComponentFamily.Interactable, ComponentFactory.Singleton.GetComponent("BasicInteractableComponent"));
            AddComponent(SS3D_shared.GO.ComponentFamily.Mover, ComponentFactory.Singleton.GetComponent("BasicMoverComponent"));
            extensions = new List<Extension.Extension>();
        }

        public void SetUp(int _uid, AtomManager _atomManager)
        {
            Uid = _uid;
            atomManager = _atomManager;
            updateRequired = true;
        }

        public virtual void PostSpawnActions()
        {
            //Called after atom has been spawned and set up.
        }

        public virtual void SerializedInit()
        {
            // When things are created with reflection using serialization their default constructor
            // isn't called. Put things in here which need to be done when it's created.
        }

        /// <summary>
        /// Used to cleanly destroy an atom.
        /// </summary>
        public virtual void Destruct()
        {

        }
        #endregion

        /// <summary>
        ///  <para>Returns linked atom for given direction if any or null if none.</para>
        ///  <para>See GlobalConstants.cs in Shared for direction info.</para>
        /// </summary>
        public Atom hasLinkedAtom(byte direction)
        {
            switch (direction)
            {
                case (Constants.NORTH):
                    break;
                case (Constants.EAST):
                    break;
                case (Constants.SOUTH):
                    break;
                case (Constants.WEST):
                    break;
            }
            return null;
        }

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
                    Push(message.SenderConnection);
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
            Mob.Mob clicker = (Mob.Mob)SS3DServer.Singleton.playerManager.GetSessionByConnection(message.SenderConnection).attachedAtom;
            if (clicker == null || clicker.IsDead()) //HAHA U CANT KILL ME WHEN UR DEAD NEMORE
                return;

            Clicked(clicker);
        }

        public override void HandleClick(int clickerID)
        {
            Mob.Mob clicker = (Mob.Mob)atomManager.GetAtom(clickerID);
            if (clicker == null || clicker.IsDead())
                return;
            Clicked(clicker);
        }

        public virtual void Push()
        {
            SendInterpolationPacket(true); // Forcibly update the position of the node.
        }

        public virtual void Push(NetConnection sender)
        {
            SendInterpolationPacket(true, sender);
        }

        public void SendInterpolationPacket(bool force)
        {
            NetOutgoingMessage message = CreateAtomMessage();
            message.Write((byte)AtomMessage.InterpolationPacket);

            InterpolationPacket i = new InterpolationPacket((float)position.X, (float)position.Y, rotation, 0); // Fuckugly
            i.WriteMessage(message);

            /* VVVV This is the force flag. If this flag is set, the client will run the interpolation 
             * packet even if it is that client's player mob. Use this in case the client has ended up somewhere bad.
             */
            message.Write(force);
            SS3DServer.Singleton.SendMessageToAll(message, NetDeliveryMethod.ReliableUnordered);
        }

        public void SendInterpolationPacket(bool force, NetConnection sender)
        {
            NetOutgoingMessage message = CreateAtomMessage();
            message.Write((byte)AtomMessage.InterpolationPacket);

            InterpolationPacket i = new InterpolationPacket((float)position.X, (float)position.Y, rotation, 0); // Fuckugly
            i.WriteMessage(message);

            /* VVVV This is the force flag. If this flag is set, the client will run the interpolation 
             * packet even if it is that client's player mob. Use this in case the client has ended up somewhere bad.
             */
            message.Write(force);
            SS3DServer.Singleton.SendMessageTo(message, sender, NetDeliveryMethod.ReliableUnordered);
        }

        public NetOutgoingMessage CreateAtomMessage()
        {
            NetOutgoingMessage message = SS3DNetServer.Singleton.CreateMessage();
            message.Write((byte)NetMessage.AtomManagerMessage);
            message.Write((byte)AtomManagerMessage.Passthrough);
            message.Write(Uid);
            return message;
        }

        protected void SendMessageToAll(NetOutgoingMessage message, NetDeliveryMethod method = NetDeliveryMethod.ReliableOrdered)
        {
            SS3DServer.Singleton.SendMessageToAll(message, method);
        }

        protected void SendMessageTo(NetOutgoingMessage message, NetConnection client , NetDeliveryMethod method = NetDeliveryMethod.ReliableOrdered)
        {
            SS3DServer.Singleton.SendMessageTo(message, client, method);
        }

        public virtual void SendState()
        {
            SendSpriteState();
            SendCollidable();
        }

        public virtual void SendState(NetConnection client)
        {
            SendSpriteState(client);
            SendCollidable(client);
        }

        public virtual void SetSpriteState(int index)
        {
            spritestate = index;
            SendSpriteState();
        }

        public virtual void SendSpriteState()
        {
            NetOutgoingMessage message = CreateAtomMessage();
            message.Write((byte)AtomMessage.SpriteState);
            message.Write(spritestate);
            SS3DServer.Singleton.SendMessageToAll(message);
        }

        public virtual void SendSpriteState(NetConnection client)
        {
            NetOutgoingMessage message = CreateAtomMessage();
            message.Write((byte)AtomMessage.SpriteState);
            message.Write(spritestate);
            SS3DServer.Singleton.SendMessageTo(message, client);
        }

        public virtual void SendCollidable()
        {
            NetOutgoingMessage  message = CreateAtomMessage();
            message.Write((byte)AtomMessage.SetCollidable);
            message.Write(collidable);
            SS3DServer.Singleton.SendMessageToAll(message);
        }

        public virtual void SendCollidable(NetConnection client)
        {
            NetOutgoingMessage message = CreateAtomMessage();
            message.Write((byte)AtomMessage.SetCollidable);
            message.Write(collidable);
            SS3DServer.Singleton.SendMessageTo(message, client);
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
                rotation = message.ReadFloat();

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
        public virtual void Translate(float _rotation)
        {
            rotation = _rotation;

            SendInterpolationPacket(false);
        }

        // For both movement and rotation
        public virtual void Translate(Vector2 newPosition, float newRotation)
        {
            position.X = newPosition.X;
            position.Y = newPosition.Y;

            rotation = newRotation;

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

            //SS3DServer.Singleton.chatManager.SendChatMessage(0, clicker.name + " touched the " + name + ".", "", uid);
            LogManager.Log(clicker.name + "(" + clicker.Uid.ToString() + ")" + " clicked " + name + "(" + Uid.ToString() + ").", LogLevel.Debug);
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
            if(m != null && m.selectedAppendage.heldItem != null)
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

        public PlayerSession GetSession()
        {
            if (attachedClient != null)
            {
                var session = SS3DServer.Singleton.playerManager.GetSessionByConnection(attachedClient);
                if(session != null)
                    return session;
            }
            return null;
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
        public virtual void Damage(int amount, int damager)
        {
            //Lots of room to get more complicated here
            currentHealth -= amount;

            string message = atomManager.GetAtom((ushort)damager).name + " hit " + name + "! " + name + " looks ";
            float healthpct = ((float)currentHealth / (float)maxHealth) * 100;
            Debug.WriteLine("Health percentage: " + healthpct.ToString() + "%");
            if (healthpct > 80)
                message += "kinda dinged up.";
            else if (healthpct > 50)
                message += "unwell.";
            else if (healthpct > 20)
                message += "really bad!";
            else if (healthpct > 0)
                message += "near death.";
            else if (healthpct <= 0)
                message += "kinda... dead.";
            SS3DServer.Singleton.chatManager.SendChatMessage(ChatChannel.Damage, message, "", Uid);
        #if DEBUG
            string healthmsg = name + "(" + Uid.ToString() + ") " + "took " + amount.ToString() + " points of damage.";
            healthmsg += " Current health: " + currentHealth.ToString() + "/" + maxHealth.ToString();//TODO SEND DAMAGE MESSAGES
            LogManager.Log(healthmsg, LogLevel.Debug);
            //SS3DServer.Singleton.chatManager.SendChatMessage(ChatChannel.Damage, healthmsg, name, uid);
        #endif

            var session = GetSession();
            if (session != null)
            {
                int healthpercent = Convert.ToInt32(100 * ((decimal)currentHealth / (decimal)maxHealth));

                var healthupdatemessage = session.CreateGuiMessage(SS3D_shared.GuiComponentType.StatPanelComponent);
                healthupdatemessage.Write((byte)SS3D_shared.GuiComponentType.HealthComponent);
                healthupdatemessage.Write((byte)SS3D_shared.HealthComponentMessage.CurrentHealth);
                healthupdatemessage.Write(Convert.ToInt32(healthpercent));
                SendMessageTo(healthupdatemessage, attachedClient);
            }

        }

        public bool IsDead()
        {
            if (currentHealth <= 0)
                isDead = true;
            
            return isDead;
        }

        public virtual void Die()
        {
            currentHealth = 0;
            isDead = true;
        }

        public Tiles.Tile GetNearestTile()
        {
            Point pos = SS3DServer.Singleton.map.GetTileArrayPositionFromWorldPosition(position);
            return SS3DServer.Singleton.map.GetTileAt(pos.x, pos.y);
        }
        #endregion

        #region Serialization
        
        public Atom(SerializationInfo info, StreamingContext ctxt)
        {
            SerializeBasicInfo(info, ctxt);
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            base.GetObjectData(info, ctxt);
        }

        #endregion
    }
}
