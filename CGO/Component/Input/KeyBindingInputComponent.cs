using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClientServices.Input;
using SS13_Shared.GO;
using SS13_Shared;

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
            if (GetKeyState(e.Function))
                return; //Don't repeat keys that are already down.
            Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, e.Function, e.FunctionState);
            SetKeyState(e.Function, true);
            Owner.SendMessage(this, ComponentMessageType.BoundKeyChange, null, e.Function, e.FunctionState);
        }

        public virtual void KeyUp(object sender, BoundKeyEventArgs e)
        {
            Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, e.Function, e.FunctionState);
            SetKeyState(e.Function, false);
            Owner.SendMessage(this, ComponentMessageType.BoundKeyChange, null, e.Function, e.FunctionState);
        }

        protected void SetKeyState(BoundKeyFunctions k, bool state)
        {
            // Check to see if we have a keyhandler for the key that's been pressed. Discard invalid keys.
            keyStates[k] = state;

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
            /*TimeSpan timeSinceLastUpdate = entityManager.now - lastKeyUpdate;
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
                else
                    Owner.SendMessage(this, ComponentMessageType.BoundKeyRepeat, null, state.Key, BoundKeyState.Repeat);
            }
            //lastKeyUpdate = entityManager.now;
        }
    }
}
