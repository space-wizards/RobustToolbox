using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClientInput;
using SS3D_shared.GO;
using SS3D_shared;

namespace CGO
{
    public class KeyBindingInputComponent : GameObjectComponent
    {
        private Dictionary<BoundKeyFunctions, bool> keyStates;
        private Dictionary<BoundKeyFunctions, KeyEvent> keyHandlers;
        public delegate void KeyEvent(bool state);

        public KeyBindingInputComponent()
        {
            family = ComponentFamily.Input;
            //Bind to the key binding manager
            KeyBindingManager.Singleton.BoundKeyDown += new KeyBindingManager.BoundKeyEventHandler(KeyDown);
            KeyBindingManager.Singleton.BoundKeyUp += new KeyBindingManager.BoundKeyEventHandler(KeyUp);
            keyStates = new Dictionary<BoundKeyFunctions, bool>();
            keyHandlers = new Dictionary<BoundKeyFunctions, KeyEvent>();
            //Set up keystates
            keyHandlers.Add(BoundKeyFunctions.MoveUp, new KeyEvent(HandleMoveUp));
            keyHandlers.Add(BoundKeyFunctions.MoveDown, new KeyEvent(HandleMoveDown));
            keyHandlers.Add(BoundKeyFunctions.MoveLeft, new KeyEvent(HandleMoveLeft));
            keyHandlers.Add(BoundKeyFunctions.MoveRight, new KeyEvent(HandleMoveRight));
        }

        public override void Shutdown()
        {
            base.Shutdown();
            KeyBindingManager.Singleton.BoundKeyDown -= new KeyBindingManager.BoundKeyEventHandler(KeyDown);
            KeyBindingManager.Singleton.BoundKeyUp -= new KeyBindingManager.BoundKeyEventHandler(KeyUp);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            UpdateKeys(frameTime);
        }

        public virtual void KeyDown(object sender, BoundKeyEventArgs e)
        {
            Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, e.Function, e.FunctionState);
            SetKeyState(e.Function, true);
            Owner.SendMessage(this, MessageType.KeyDown, e.Function);
        }

        public virtual void KeyUp(object sender, BoundKeyEventArgs e)
        {
            Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, e.Function, e.FunctionState);
            SetKeyState(e.Function, false);
            Owner.SendMessage(this, MessageType.KeyUp, e.Function);
        }

        protected void SetKeyState(BoundKeyFunctions k, bool state)
        {
            // Check to see if we have a keyhandler for the key that's been pressed. Discard invalid keys.
            if (keyHandlers.ContainsKey(k))
            {
                keyStates[k] = state;
            }
        }

        public bool GetKeyState(BoundKeyFunctions k)
        {
            if (keyStates.Keys.Contains(k))
                return keyStates[k];
            return false;
        }

        public virtual void UpdateKeys(float frameTime)
        {
            //Rate limit
            /*TimeSpan timeSinceLastUpdate = atomManager.now - lastKeyUpdate;
            if (timeSinceLastUpdate.TotalMilliseconds < 1000 / keyUpdateRateLimit)
                return;*/

            // So basically we check for active keys with handlers and execute them. This is a linq query.
            // Get all of the active keys' handlers
            var activeKeyHandlers =
                from keyState in keyStates
                join handler in keyHandlers on keyState.Key equals handler.Key
                select new { evt = handler.Value, state = keyState.Value };

            //Execute the bastards!
            foreach (var keyHandler in activeKeyHandlers)
            {
                //If there's even one active, we set updateRequired so that this gets hit again next update
                //updateRequired = true; // QUICKNDIRTY
                KeyEvent k = keyHandler.evt;
                k(keyHandler.state);
            }

            //Delete false states from the dictionary so they don't get reprocessed and fuck up other stuff. 
            foreach (var state in keyStates.ToList())
            {
                if (state.Value == false)
                    keyStates.Remove(state.Key);
            }
            //lastKeyUpdate = atomManager.now;
        }

        public virtual void HandleMoveUp(bool state)
        {
            if (state && GetKeyState(BoundKeyFunctions.MoveLeft) && !GetKeyState(BoundKeyFunctions.MoveRight))
                Owner.MoveUpLeft();
            else if (state && GetKeyState(BoundKeyFunctions.MoveRight))
                Owner.MoveUpRight();
            else if (state)
                Owner.MoveUp();
        }
        public virtual void HandleMoveDown(bool state)
        {
            if (state && GetKeyState(BoundKeyFunctions.MoveLeft) && !GetKeyState(BoundKeyFunctions.MoveRight))
                Owner.MoveDownLeft();
            else if (state && GetKeyState(BoundKeyFunctions.MoveRight))
                Owner.MoveDownRight();
            else if (state)
                Owner.MoveDown();
        }
        public virtual void HandleMoveLeft(bool state)
        {
            if (state && !GetKeyState(BoundKeyFunctions.MoveUp) && !GetKeyState(BoundKeyFunctions.MoveDown))
                Owner.MoveLeft();
        }
        public virtual void HandleMoveRight(bool state)
        {
            if (state && !GetKeyState(BoundKeyFunctions.MoveUp) && !GetKeyState(BoundKeyFunctions.MoveDown))
                Owner.MoveRight();
        }

    }
}
