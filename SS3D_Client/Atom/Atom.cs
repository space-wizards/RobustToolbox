using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using SS3D_shared.HelperClasses;
using SS3D.HelperClasses;
using Lidgren.Network;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using SS3D.Modules;

namespace SS3D.Atom
{
    public abstract class Atom // CLIENT SIDE
    {
        #region variables
        // GRAPHICS
        public bool updateRequired = false;
        public bool drawn = false;
        public Light light;

        //SPRITE
        public Sprite sprite;
        public string spritename = "noSprite";

        public string name;
        public ushort uid;
        public AtomManager atomManager;

        // Position data
        public Vector2D position;
        public Vector2D offset = Vector2D.Zero; // For odd models
        public float rotation;
        public bool positionChanged = false;
        public List<InterpolationPacket> interpolationPackets;
        public float speed = 2.0f;
        public bool clipping = true;
        public bool collidable = false;
        private DateTime lastPositionUpdate;
        private int positionUpdateRateLimit = 30; //Packets per second
        private int keyUpdateRateLimit = 160; // 120 key updates per second;
        private DateTime lastKeyUpdate;

        public bool visible = true;
        public bool attached;
        public bool snapTogrid = false; // Is this locked to the grid, eg a door / window

        //Input
        public Dictionary<KeyboardKeys, bool> keyStates;
        public Dictionary<KeyboardKeys, KeyEvent> keyHandlers;

        public delegate void KeyEvent(bool state);

        //Misc
        public SpeechBubble speechBubble;

        #endregion

        #region constructors and init
        public Atom()
        {
            keyStates = new Dictionary<KeyboardKeys, bool>();
            keyHandlers = new Dictionary<KeyboardKeys, KeyEvent>();

            position = new Vector2D(160, 160);
            rotation = 0;

            interpolationPackets = new List<InterpolationPacket>();
        }

        public Atom(ushort _uid, AtomManager _atomManager)
        {
            keyStates = new Dictionary<KeyboardKeys, bool>();
            keyHandlers = new Dictionary<KeyboardKeys, KeyEvent>();

            position = new Vector2D(160, 160);
            rotation = 0;

            interpolationPackets = new List<InterpolationPacket>();

            SetUp(_uid, _atomManager);
        }

        public virtual void SetUp(ushort _uid, AtomManager _atomManager)
        {
            uid = _uid;
            atomManager = _atomManager;

            Draw();
        }

        public virtual void Draw()
        {
            //Draw the atom into the scene. This should be called after instantiation.
            sprite = ResMgr.Singleton.GetSprite(spritename);
            sprite.Position = new Vector2D(position.X, position.Y);
            sprite.SetAxis(sprite.Width / 2, sprite.Height / 2);
            drawn = true;
        }
        #endregion

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

        public virtual void HandleInterpolationPacket(NetIncomingMessage message)
        {
            SS3D.HelperClasses.InterpolationPacket intPacket = new SS3D.HelperClasses.InterpolationPacket(message);

            // This makes the client discard interpolation packets for the atom the local player is controlling, 
            // unless the force flag is set. If the force flag is set, the server is trying to correct an issue.
            bool forceUpdate = message.ReadBoolean();
            if (attached && forceUpdate == false)
                return;

            //Add an interpolation packet to the end of the list. If the list is more than 5 long, delete a packet.
            //TODO: For the Player class, override this function to do some sort of intelligent checking on the interpolation packets 
            // recieved to make sure they don't greatly disagree with the client's own data.
            interpolationPackets.Add(intPacket);

            if (interpolationPackets.Count > 2)
            {
                interpolationPackets.RemoveAt(0);
            }

            // Need an update.
            updateRequired = true;
        }

        // Sends a message to the server to request the atom's data.
        public void SendPullMessage()
        {
            NetOutgoingMessage message = CreateAtomMessage(); 
            message.Write((byte)AtomMessage.Pull);
            atomManager.networkManager.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
        }

        public virtual void HandlePush(NetIncomingMessage message)
        {
            // Do nothing. This should be overridden by the child.
        }

        public void SendPositionUpdate()
        {
            //Rate limit
            TimeSpan timeSinceLastUpdate = DateTime.Now - lastPositionUpdate;
            if (timeSinceLastUpdate.TotalMilliseconds < 1000 / positionUpdateRateLimit)
                return;

            // This is only useful if the fucking shit is actually controlled by a player
            NetOutgoingMessage message = CreateAtomMessage();
            message.Write((byte)AtomMessage.PositionUpdate);
            message.Write(position.X);
            message.Write(position.Y);
            message.Write(rotation);
            atomManager.networkManager.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
            lastPositionUpdate = DateTime.Now;
        }

        protected NetOutgoingMessage CreateAtomMessage()
        {
            NetOutgoingMessage message = atomManager.networkManager.netClient.CreateMessage();
            message.Write((byte)NetMessage.AtomManagerMessage);
            message.Write((byte)AtomManagerMessage.Passthrough);
            message.Write(uid);
            return message;
        }

        protected void SendMessage(NetOutgoingMessage message)
        {
            // Send messages unreliably by default
            SendMessage(message, NetDeliveryMethod.ReliableUnordered);
        }

        protected void SendMessage(NetOutgoingMessage message, NetDeliveryMethod method = NetDeliveryMethod.ReliableUnordered)
        {
            atomManager.networkManager.SendMessage(message, method);
        }
        #endregion

        #region updating
        public virtual void Update(double time)
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
            //Rate limit
            TimeSpan timeSinceLastUpdate = atomManager.now - lastKeyUpdate;
            if (timeSinceLastUpdate.TotalMilliseconds < 1000 / keyUpdateRateLimit)
                return;

            // So basically we check for active keys with handlers and execute them. This is a linq query.
            // Get all of the active keys' handlers
            var activeKeyHandlers =
                from keyState in keyStates
                join handler in keyHandlers on keyState.Key equals handler.Key
                select new { evt = handler.Value, state = keyState.Value};

            //Execute the bastards!
            foreach (var keyHandler in activeKeyHandlers)
            {
                //If there's even one active, we set updateRequired so that this gets hit again next update
                updateRequired = true; // QUICKNDIRTY
                KeyEvent k = keyHandler.evt;
                k(keyHandler.state);
            }

            //Delete false states from the dictionary so they don't get reprocessed and fuck up other stuff. 
            foreach (var state in keyStates.ToList())
            {
                if (state.Value == false)
                    keyStates.Remove(state.Key);
            }
            lastKeyUpdate = atomManager.now;
        }

        // Mobs may need to override this for animation, or they could use this.
        public virtual void UpdatePosition()
        {
            Vector2D difference;
            Vector2D fulldifference;
            float rot;

            if (interpolationPackets.Count == 0)
            {
                updateRequired = false;
                return;
            }
            InterpolationPacket i = interpolationPackets[0];

            if (i.startposition.X == 1234 && i.startposition.Y == 1234) //This is silly, but vectors are non-nullable, so I can't do what I'd rather.
                i.startposition = position;

            difference = i.position - position;
            fulldifference = i.position - i.startposition;

            // Set rotation. The packet may be rotation only.
            rotation = i.rotation;
            //Node.SetOrientation(rotW, 0, rotY, 0);

            //Check interpolation packet to see if we're close enough to the interpolation packet on the top of the stack.
            if (difference.Length < 0.1)
            {
                interpolationPackets.RemoveAt(0);
                UpdatePosition(); // RECURSION :D - this discards interpolationpackets we don't need anymore.
            }
            else
            {
                //Distance between interpolation packet and current position is big, so we will move the node towards it.

                //This constant should be time interval based.
                //TODO: Make this better if it isn't good enough.
                //difference /= 10; //Position updates were lagging. This would probably be faster on a better system.
                //difference = fulldifference / 3;
                position += difference/2;
                //Node.Position = position + offset;
                updateRequired = true; // This interpolation packet and probably the ones after it are still useful, so we'll update again on the next cycle.
            }

            sprite.Position = new Vector2D(position.X, position.Y);

        }

        #endregion

        public virtual void Render(float xTopLeft, float yTopLeft)//, List<Light> lights)
        {
            System.Drawing.Point tilePos = atomManager.gameState.map.GetTileArrayPositionFromWorldPosition(position);
            System.Drawing.Point topLeft = atomManager.gameState.map.GetTileArrayPositionFromWorldPosition(position - sprite.Size / 2);
            System.Drawing.Point bottomRight = atomManager.gameState.map.GetTileArrayPositionFromWorldPosition(position + sprite.Size / 2);
            sprite.SetPosition(position.X - xTopLeft, position.Y - yTopLeft);
            sprite.Rotation = rotation;
            bool draw = false;
            if ((tilePos.X > 0 && atomManager.gameState.map.tileArray[tilePos.X, tilePos.Y].Visible) ||
                (topLeft.X > 0 &&atomManager.gameState.map.tileArray[topLeft.X, topLeft.Y].Visible) ||
                (bottomRight.X > 0 && atomManager.gameState.map.tileArray[bottomRight.X, bottomRight.Y].Visible))
            {
                draw = true;
            }
            if (tilePos.X >= 0 && tilePos.Y >= 0)
            {
                if (draw && visible)
                {
                    LightManager.Singleton.ApplyLightsToSprite(atomManager.gameState.map.tileArray[tilePos.X, tilePos.Y].tileLights, sprite, new Vector2D(xTopLeft, yTopLeft));
                    sprite.Draw();
                }
            }
        }
        
        #region positioning
        public virtual bool IsColliding()
        {
            ///CHECK TURF COLLISIONS
            //Lets just check each corner of our sprite to see if it is in a wall tile for now.
            System.Drawing.RectangleF myAABB = new System.Drawing.RectangleF(position.X - ((sprite.Width * sprite.UniformScale) / 2), 
                position.Y - ((sprite.Height * sprite.UniformScale) / 2), 
                (sprite.Width * sprite.UniformScale), 
                (sprite.Height * sprite.UniformScale));

            if (atomManager.gameState.map.GetTileTypeFromWorldPosition(myAABB.Left+1, myAABB.Top+(myAABB.Height / 2)) == TileType.Wall) // Top left
            {
                return true;
            }
            else if (atomManager.gameState.map.GetTileTypeFromWorldPosition(myAABB.Left+1, myAABB.Bottom-1) == TileType.Wall) // Bottom left
            {
                return true;
            }
            else if (atomManager.gameState.map.GetTileTypeFromWorldPosition(myAABB.Right - 1, myAABB.Top + (myAABB.Height / 2)) == TileType.Wall) // Top right
            {
                return true;
            }
            else if (atomManager.gameState.map.GetTileTypeFromWorldPosition(myAABB.Right-1, myAABB.Bottom-1) == TileType.Wall) // Bottom left
            {
                return true;
            }
            
            ///CHECK ATOM COLLISIONS
            IEnumerable<Atom> atoms = from a in atomManager.atomDictionary.Values
                                      where
                                      a.collidable == true &&
                                      System.Math.Sqrt((position.X - a.position.X) * (position.X - a.position.X)) < (sprite.Width * sprite.UniformScale) &&
                                      System.Math.Sqrt((position.Y - a.position.Y) * (position.Y - a.position.Y)) < (sprite.Height * sprite.UniformScale) &&
                                      a.uid != uid
                                      select a;

            foreach (Atom a in atoms)
            {
                System.Drawing.RectangleF box = new System.Drawing.RectangleF(a.position.X - ((a.sprite.Width * a.sprite.UniformScale) / 2), 
                    a.position.Y - ((a.sprite.Height * a.sprite.UniformScale) / 2), 
                    (a.sprite.Width * a.sprite.UniformScale), 
                    (a.sprite.Height * a.sprite.UniformScale));

                if(a.GetType() == Type.GetType("SS3D.Atom.Object.Door.Door")) //DOOR SPECIAL CASE LOL
                    box = new System.Drawing.RectangleF(a.position.X - ((a.sprite.Width * a.sprite.UniformScale) / 2),
                    a.position.Y + ((a.sprite.Height * a.sprite.UniformScale) / 2) - 32,
                    (a.sprite.Width * a.sprite.UniformScale),
                    32);

                if (box.IntersectsWith(myAABB))
                {
                    return true;
                }
            }

            return false;

        }

        public virtual void TranslateLocal(Vector2D toPosition) 
        {
            Vector2D oldPosition = position;
            position += toPosition; // We move the sprite here rather than the position, as we can then use its updated AABB values.

            if (clipping && IsColliding())
            {
                position -= toPosition;
            }
        }

        //TODO: Unfuck this. 
        /* Shouldn't really be translating the node and then backfilling the atom objects
         * position and rotation from it. Ostaf? */

        /* These are solely for user input, not for updating position from server. */
        public virtual void MoveForward() 
        {
            TranslateLocal(new Vector2D(0, -1 * speed));
            SendPositionUpdate();
        }

        public virtual void MoveBack()
        {
            TranslateLocal(new Vector2D(0,speed));
            SendPositionUpdate();
        }

        public virtual void MoveLeft()
        {
            TranslateLocal(new Vector2D(-1 * speed, 0));
            SendPositionUpdate();
        }

        public virtual void MoveRight()
        {
            TranslateLocal(new Vector2D(speed, 0));
            SendPositionUpdate();
        }

        public virtual void MoveUpLeft()
        {
            TranslateLocal(new Vector2D(-1 * speed, -1 * speed));
            SendPositionUpdate();
        }

        public virtual void MoveDownLeft()
        {
            TranslateLocal(new Vector2D(-1 * speed, speed));
            SendPositionUpdate();
        }

        public virtual void MoveUpRight()
        {
            TranslateLocal(new Vector2D(speed, -1 * speed));
            SendPositionUpdate();
        }

        public virtual void MoveDownRight()
        {
            TranslateLocal(new Vector2D(speed, speed));
            SendPositionUpdate();
        }

        public virtual void TurnLeft()
        {
            /*Node.Rotate(Mogre.Vector3.UNIT_Y, Mogre.Math.DegreesToRadians(2));
            rotW = Node.Orientation.w;
            rotY = Node.Orientation.y;
            SendPositionUpdate();*/
        }

        public virtual void TurnRight()
        {
           /* Node.Rotate(Mogre.Vector3.UNIT_Y, Mogre.Math.DegreesToRadians(-2));
            rotW = Node.Orientation.w;
            rotY = Node.Orientation.y;
            SendPositionUpdate();*/
        }

        #endregion

        #region input handling
        /* You might be wondering why input handling is in the base atom code. Well, It's simple.
         * This way I can make any item on the station player controllable. If I want to, I can spawn
         * a watermelon and make the player I hate with the fire of a million burning suns become that
         * melon. Awesome.
         */
        public virtual void initKeys()
        {
            /* Set up key handlers (we don't need to do this unless a playercontroller attaches.)
             * Example: keyHandlers.Add(MOIS.KeyCode.KC_Whatever, new KeyEvent(HandleKC_whatever));
             * To override a keyhandler, delete it and make a new one OR override the handler function 
             * BEFORE calling initKeys(). */
            keyHandlers.Add(KeyboardKeys.W, new KeyEvent(HandleKC_W));
            keyHandlers.Add(KeyboardKeys.A, new KeyEvent(HandleKC_A));
            keyHandlers.Add(KeyboardKeys.S, new KeyEvent(HandleKC_S));
            keyHandlers.Add(KeyboardKeys.D, new KeyEvent(HandleKC_D));
        }
        
        public void HandleKeyPressed(KeyboardKeys k)
        {
            SetKeyState(k, true);
        }

        public void HandleKeyReleased(KeyboardKeys k)
        {
            SetKeyState(k, false);
        }

        protected void SetKeyState(KeyboardKeys k, bool state)
        {
            // Check to see if we have a keyhandler for the key that's been pressed. Discard invalid keys.
            if (keyHandlers.ContainsKey(k))
            {
                keyStates[k] = state;
            }
            updateRequired = true;
        }

        protected bool GetKeyState(KeyboardKeys k)
        {
            if (keyStates.ContainsKey(k) && keyStates[k])
                return true;
            return false;
        }

        #region mouse handling
        public bool WasClicked(Vector2D worldPosition)
        {
            System.Drawing.RectangleF AABB = new System.Drawing.RectangleF(position.X - (sprite.Width / 2), position.Y - (sprite.Height / 2), sprite.Width, sprite.Height);
            if (!AABB.Contains(worldPosition))
                return false;
            System.Drawing.Point spritePosition = new System.Drawing.Point((int)(worldPosition.X - AABB.X), (int)(worldPosition.Y - AABB.Y));
            Image.ImageLockBox imgData = sprite.Image.GetImageData();
            imgData.Lock(false);
            System.Drawing.Color pixColour = System.Drawing.Color.FromArgb((int)(imgData[spritePosition.X, spritePosition.Y]));
            imgData.Dispose();
            imgData.Unlock();
            if (pixColour.A == 0)
                return false;
            return true;
        }

        public virtual void HandleClick()
        {
            SendClick();
        }

        public void SendClick()
        {
            NetOutgoingMessage message = CreateAtomMessage();
            message.Write((byte)AtomMessage.Click);
            SendMessage(message);
        }
        #endregion

        #region key handlers
        public virtual void HandleKC_W(bool state)
        {
            if (state && GetKeyState(KeyboardKeys.A) && !GetKeyState(KeyboardKeys.D))
                MoveUpLeft();
            else if (state && GetKeyState(KeyboardKeys.D) && !GetKeyState(KeyboardKeys.A))
                MoveUpRight();
            else if (state)
                MoveForward();
        }
        public virtual void HandleKC_A(bool state)
        {
            if (state && !GetKeyState(KeyboardKeys.W) && !GetKeyState(KeyboardKeys.S))
                MoveLeft();
        }
        public virtual void HandleKC_S(bool state)
        {
            if (state && GetKeyState(KeyboardKeys.A) && !GetKeyState(KeyboardKeys.D))
                MoveDownLeft();
            else if (state && GetKeyState(KeyboardKeys.D) && !GetKeyState(KeyboardKeys.A))
                MoveDownRight();
            else if (state)
                MoveBack();
        }
        public virtual void HandleKC_D(bool state)
        {
            if (state && !GetKeyState(KeyboardKeys.W) && !GetKeyState(KeyboardKeys.S))
                MoveRight();
        }
        #endregion

 
        #endregion
    }
}
