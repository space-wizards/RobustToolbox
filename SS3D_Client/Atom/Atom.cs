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
using SS3D.UserInterface;
using CGO;
using ClientResourceManager;
using ClientServices.Lighting;

namespace SS3D.Atom
{
    public abstract class Atom : CGO.Entity // CLIENT SIDE
    {
        #region variables
        // GRAPHICS
        public bool updateRequired = false;
        public bool drawn = false;
        //public Light light;

        //SPRITE
        public Sprite sprite;
        public string spritename = "noSprite";
        public Dictionary<int, string> spriteNames;
        public int drawDepth = 0;
        private int index = 0;
        
        // Position data
        public List<InterpolationPacket> interpolationPackets;
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
        /*
        public Dictionary<KeyboardKeys, bool> keyStates;
        public Dictionary<KeyboardKeys, KeyEvent> keyHandlers;
        */
        public delegate void KeyEvent(bool state);

        //Misc
        public SpeechBubble speechBubble;

        #endregion

        #region constructors and init
        public Atom()
        {
        }

        public Atom(ushort _uid, AtomManager _atomManager)
        {
            SetUp(_uid, _atomManager);
        }

        public virtual void SetUp(int _uid, AtomManager _atomManager)
        {
            //Initialize();
            //Uid = _uid;
            //atomManager = _atomManager;

            //Draw();
        }

        public override void Initialize()
        {
            base.Initialize();
            /*
            keyStates = new Dictionary<KeyboardKeys, bool>();
            keyHandlers = new Dictionary<KeyboardKeys, KeyEvent>();
            */
            Position = new Vector2D(160, 160);
            rotation = 0;

            interpolationPackets = new List<InterpolationPacket>();
            spriteNames = new Dictionary<int, string>();
            spriteNames[0] = spritename;

            AddComponent(SS3D_shared.GO.ComponentFamily.Mover, ComponentFactory.Singleton.GetComponent("NetworkMoverComponent"));
            AddComponent(SS3D_shared.GO.ComponentFamily.Renderable, ComponentFactory.Singleton.GetComponent("SpriteComponent"));
            AddComponent(SS3D_shared.GO.ComponentFamily.Click, ComponentFactory.Singleton.GetComponent("ClickableComponent"));
        }

        public virtual void SetSpriteName(int index, string name)
        {
            if (spriteNames.Keys.Contains(index))
                spriteNames[index] = name;
            else
                spriteNames.Add(index, name);
        }

        public virtual void Draw()
        {
            //Draw the atom into the scene. This should be called after instantiation.
            //sprite = ResMgr.Singleton.GetSprite(spritename);
            //sprite.Position = new Vector2D(position.X, position.Y);
            //sprite.SetAxis(sprite.Width / 2, sprite.Height / 2);
            drawn = true;
        }

        /*public virtual void SetSpriteByIndex(int _index)
        {
            if (spriteNames.Keys.Contains(_index))
            {
                index = _index;
                spritename = spriteNames[_index];
                Draw();
            }
        }*/

        /*
        public int GetSpriteIndex()
        {
            return index;
        }*/
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
                case AtomMessage.SpriteState:
                    HandleSpriteState(message);
                    break;
                case AtomMessage.Extended:
                    HandleExtendedMessage(message); // This will punt unhandled messages to a virtual method so derived classes can handle them.
                    break;
                case AtomMessage.SetCollidable:
                    collidable = message.ReadBoolean();
                    break;
                default:
                    break;
            }
            return;
        }

        private void HandleSpriteState(NetIncomingMessage message)
        {
            int index = message.ReadInt32();
            //SetSpriteByIndex(index);
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
            //NetOutgoingMessage message = CreateAtomMessage(); 
            //message.Write((byte)AtomMessage.Pull);
            //atomManager.networkManager.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
        }

        public virtual void HandlePush(NetIncomingMessage message)
        {
            // Do nothing. This should be overridden by the child.
        }

        /*protected NetOutgoingMessage CreateAtomMessage()
        {
            NetOutgoingMessage message = atomManager.networkManager.netClient.CreateMessage();
            message.Write((byte)NetMessage.AtomManagerMessage);
            message.Write((byte)AtomManagerMessage.Passthrough);
            message.Write(Uid);
            return message;
        }*/

        protected void SendMessage(NetOutgoingMessage message)
        {
            // Send messages unreliably by default
            //SendMessage(message, NetDeliveryMethod.ReliableUnordered);
        }

        protected void SendMessage(NetOutgoingMessage message, NetDeliveryMethod method = NetDeliveryMethod.ReliableUnordered)
        {
            //atomManager.networkManager.SendMessage(message, method);
        }
        #endregion

        #region updating
        public override void Update(float time)
        {
            base.Update(time);
            //This is where all the good stuff happens. 

            //If the node hasn't even been drawn into the scene, there's no point updating the fucker, is there?
            if (!drawn)
                return;
            //This lets the atom only update when it needs to. If it needs to update subsequent to this, the functions below will set that flag.
            updateRequired = false;

            //UpdatePosition();
            //UpdateKeys();
        }

        /*public virtual void UpdateKeys()
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
             
        }*/

        // Mobs may need to override this for animation, or they could use this.
        /*public virtual void UpdatePosition()
        {
            
            Vector2D difference;
            Vector2D fulldifference;

            if (interpolationPackets.Count == 0)
            {
                updateRequired = false;
                return;
            }
            InterpolationPacket i = interpolationPackets[0];

            if (i.startposition.X == 1234 && i.startposition.Y == 1234) //This is silly, but vectors are non-nullable, so I can't do what I'd rather.
                i.startposition = Position;

            difference = i.position - Position;
            fulldifference = i.position - i.startposition;

            // Set rotation. The packet may be rotation only.
            rotation = i.rotation;


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
                Position += difference/2;
                //Node.Position = position + offset;
                updateRequired = true; // This interpolation packet and probably the ones after it are still useful, so we'll update again on the next cycle.
            }

            //sprite.Position = new Vector2D(position.X, position.Y);
            
        }*/

        #endregion

        #region Rendering
        public virtual void Render(float xTopLeft, float yTopLeft, int Opacity = 255)//, List<Light> lights)
        {
            //if (spritename == "noSprite")
                //return;
            //System.Drawing.Point tilePos = atomManager.gameState.map.GetTileArrayPositionFromWorldPosition(Position);
            //System.Drawing.Point topLeft = atomManager.gameState.map.GetTileArrayPositionFromWorldPosition(position - sprite.Size / 2);
            //System.Drawing.Point bottomRight = atomManager.gameState.map.GetTileArrayPositionFromWorldPosition(position + sprite.Size / 2);
            //sprite.SetPosition(position.X - xTopLeft, position.Y - yTopLeft);
            //sprite.Rotation = rotation;
            //bool draw = false;
            /*if ((tilePos.X > 0 && atomManager.gameState.map.tileArray[tilePos.X, tilePos.Y].Visible) ||
                (topLeft.X > 0 &&atomManager.gameState.map.tileArray[topLeft.X, topLeft.Y].Visible) ||
                (bottomRight.X > 0 && atomManager.gameState.map.tileArray[bottomRight.X, bottomRight.Y].Visible))
            {
                draw = true;
            }
            if (tilePos.X >= 0 && tilePos.Y >= 0)
            {
                if (draw && visible)
                {
                    //LightManager.Singleton.ApplyLightsToSprite(atomManager.gameState.map.tileArray[tilePos.X, tilePos.Y].tileLights, sprite, new Vector2D(xTopLeft, yTopLeft));
                    //sprite.Color = System.Drawing.Color.FromArgb(Opacity, sprite.Color);
                    //sprite.Draw();
                }
            }
            */
            //TODO INTEGRATE SPEECH BUBBLES WITH COMPONENT SYSTEM
            /*if (speechBubble != null && this.IsChildOfType(typeof(Mob.Mob)))
                speechBubble.Draw(position, xTopLeft, yTopLeft, sprite);*/
        }

        public virtual void Render(float xTopLeft, float yTopLeft)
        {
            Render(xTopLeft, yTopLeft, 255);
        }
        #endregion


        #region positioning
        public virtual System.Drawing.RectangleF GetAABB()
        {
            return new System.Drawing.RectangleF(Position.X - (sprite.AABB.Width / 2),
                Position.Y - (sprite.AABB.Height / 2),
                sprite.AABB.Width,
                sprite.AABB.Height);
        }
        #endregion

        #region input handling
        /* You might be wondering why input handling is in the base atom code. Well, It's simple.
         * This way I can make any item on the station player controllable. If I want to, I can spawn
         * a watermelon and make the player I hate with the fire of a million burning suns become that
         * melon. Awesome.
         */
        
        /*
        public void HandleKeyPressed(KeyboardKeys k)
        {
            //SetKeyState(k, true);
        }

        public void HandleKeyReleased(KeyboardKeys k)
        {
            //SetKeyState(k, false);
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
        }*/

        #region mouse handling
        /*public bool WasClicked(Vector2D worldPosition)
        {
            return false; //HACKED TO DISABLE ATOM CLICKING WITHOUT COMPONENTS
            System.Drawing.RectangleF AABB = new System.Drawing.RectangleF(position.X - (sprite.Width / 2), position.Y - (sprite.Height / 2), sprite.Width, sprite.Height);
            if (!AABB.Contains(worldPosition))
                return false;
            System.Drawing.Point spritePosition = new System.Drawing.Point((int)(worldPosition.X - AABB.X + sprite.ImageOffset.X), (int)(worldPosition.Y - AABB.Y + sprite.ImageOffset.Y));
            Image.ImageLockBox imgData = sprite.Image.GetImageData();
            imgData.Lock(false);
            System.Drawing.Color pixColour = System.Drawing.Color.FromArgb((int)(imgData[spritePosition.X, spritePosition.Y]));
            imgData.Dispose();
            imgData.Unlock();
            if (pixColour.A == 0)
                return false;
            return true;
        }*/

        #endregion

        #endregion

        #region utility
        /// <summary>
        /// Checks if the atom is a child / derived from the passed in type.
        /// </summary>
        /// <param name="type">Use typeof() on the type you want to check. For example, typeof(Item.Tool.Crowbar)</param>
        /// <returns>True if a child, false otherwise.</returns>
        public bool IsChildOfType(Type type)
        {
            if (this.GetType().IsSubclassOf(type))
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
        #endregion

    }
}
