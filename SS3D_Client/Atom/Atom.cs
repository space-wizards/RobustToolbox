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
        public float speed = 1.0f;

        //Input
        public Dictionary<MOIS.KeyCode, bool> keyStates;
        public Dictionary<MOIS.KeyCode, KeyEvent> keyHandlers;

        public delegate void KeyEvent();

        public Atom()
        {
            keyStates = new Dictionary<MOIS.KeyCode, bool>();
            keyHandlers = new Dictionary<MOIS.KeyCode, KeyEvent>();

            position = new Mogre.Vector3(0, 0, 0);
            rotW = 1;
            rotY = 0;

            interpolationPackets = new List<InterpolationPacket>();
        }

        public Atom(ushort _uid, AtomManager _atomManager)
        {
            keyStates = new Dictionary<MOIS.KeyCode, bool>();
            keyHandlers = new Dictionary<MOIS.KeyCode, KeyEvent>();

            uid = _uid;
            atomManager = _atomManager;

            position = new Mogre.Vector3(0, 0, 0);
            rotW = 1;
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

        #region network stuff
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

        public void SendPositionUpdate()
        {
            // This is only useful if the fucking shit is actually controlled by a player
            NetOutgoingMessage message = atomManager.networkManager.netClient.CreateMessage();
            message.Write((byte)NetMessage.AtomManagerMessage);
            message.Write((byte)AtomManagerMessage.Passthrough);
            message.Write((ushort)uid);
            message.Write((byte)AtomMessage.PositionUpdate);
            message.Write(position.x);
            message.Write(position.y);
            message.Write(position.z);
            message.Write(rotW);
            message.Write(rotY);
            atomManager.networkManager.SendMessage(message, NetDeliveryMethod.Unreliable);
        }
        #endregion

        public virtual void Update()
        {
            //This is where all the good stuff happens. 

            //If the node hasn't even been drawn into the scene, there's no point updating the fucker, is there?
            if (!drawn)
                return;
            //This lets the atom only update when it needs to. If it needs to update subsequent to this, the functions below will set that flag.
            updateRequired = false;

            UpdatePosition();
            UpdateKeys();
        }

        public virtual void UpdateKeys()
        {
            // So basically we check for active keys with handlers and execute them. This is a linq query.
            // Get all of the active keys' handlers
            var activeKeyHandlers =
                from keyState in keyStates
                join handler in keyHandlers on keyState.Key equals handler.Key
                select handler.Value;

            //Execute the bastards!
            foreach (var keyHandler in activeKeyHandlers)
            {
                keyHandler();
            }
        }

        #region positioning
        // Mobs may need to override this for animation, or they could use this.
        public virtual void UpdatePosition()
        {
            Mogre.Vector3 difference;
            float rotW, rotY;

            if (interpolationPackets.Count == 0)
            {
                updateRequired = false;
                return;
            }
            difference = interpolationPackets[0].position - position;
            
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
                position += difference;
                Node.Position = position;
                updateRequired = true; // This interpolation packet and probably the ones after it are still useful, so we'll update again on the next cycle.
            }
        }

        public virtual void Translate() {

        }

        //TODO: Unfuck this. 
        /* Shouldn't really be translating the node and then backfilling the atom objects
         * position and rotation from it. Ostaf? */

        public virtual void MoveForward() 
        {
            Node.Translate(new Mogre.Vector3(0, 0, speed), Mogre.Node.TransformSpace.TS_LOCAL);
            position = Node.Position;
        }

        public virtual void MoveBack()
        {
            Node.Translate(new Mogre.Vector3(0,0,-1 * speed), Mogre.Node.TransformSpace.TS_LOCAL);
            position = Node.Position;
        }

        public virtual void TurnLeft()
        {
            Node.Rotate(Mogre.Vector3.UNIT_Y, Mogre.Math.DegreesToRadians(-2));
            rotW = Node.Orientation.w;
            rotY = Node.Orientation.y;
        }

        public virtual void TurnRight()
        {
            Node.Rotate(Mogre.Vector3.UNIT_Y, Mogre.Math.DegreesToRadians(2));
            rotW = Node.Orientation.w;
            rotY = Node.Orientation.y;
        }

        #endregion

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
            Entity = sceneManager.CreateEntity(entityName, meshName);
            Entity.UserObject = this;
            Node.Position = position;
            Node.AttachObject(Entity);

            var entities = sceneManager.ToString();
            drawn = true;
        }



        #region input handling
        /* You might be wondering why input handling is in the base atom code. Well, It's simple.
         * This way I can make any item on the station player controllable. If I want to, I can spawn
         * a watermelon and make the player I hate with the fire of a million burning suns become that
         * melon. Awesome.
         */
        public void initKeys()
        {
            /* Set up key handlers (we don't need to do this unless a playercontroller attaches.)
             * Example: keyHandlers.Add(MOIS.KeyCode.KC_Whatever, new KeyEvent(HandleKC_whatever));
             * To override a keyhandler, delete it and make a new one OR override the handler function 
             * BEFORE calling initKeys(). */
            keyHandlers.Add(MOIS.KeyCode.KC_W, new KeyEvent(HandleKC_W));
            keyHandlers.Add(MOIS.KeyCode.KC_A, new KeyEvent(HandleKC_A));
            keyHandlers.Add(MOIS.KeyCode.KC_S, new KeyEvent(HandleKC_S));
            keyHandlers.Add(MOIS.KeyCode.KC_D, new KeyEvent(HandleKC_D));
        }
        
        public void HandleKeyPressed(MOIS.KeyCode k)
        {
            SetKeyState(k, true);
        }

        public void HandleKeyReleased(MOIS.KeyCode k)
        {
            SetKeyState(k, false);
        }

        private void SetKeyState(MOIS.KeyCode k, bool state)
        {
            // Check to see if we have a keyhandler for the key that's been pressed. Discard invalid keys.
            if (keyHandlers.ContainsKey(k))
            {
                keyStates[k] = state;
            }
        }

        #region key handlers
        protected void HandleKC_W()
        {
            MoveForward();
        }
        protected void HandleKC_A()
        {
            //moveLeft(); // I want this to be strafe
            TurnLeft();
        }
        protected void HandleKC_S()
        {
            MoveBack();
        }
        protected void HandleKC_D()
        {
            //moveRight(); // I want this to be strafe
            TurnRight();
        }
        #endregion
        #endregion
    }
}
