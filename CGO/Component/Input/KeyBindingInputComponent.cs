using System.Collections.Generic;
using System.Linq;
using ClientInterfaces.Input;
using SS13.IoC;
using SS13_Shared.GO;
using SS13_Shared;

namespace CGO
{
    public class KeyBindingInputComponent : GameObjectComponent
    {
        private readonly Dictionary<BoundKeyFunctions, bool> _keyStates;
        private readonly Dictionary<BoundKeyFunctions, KeyEvent> _keyHandlers;
        public delegate void KeyEvent(bool state);
        private bool _enabled = true;

        public override ComponentFamily Family
        {
            get { return ComponentFamily.Input; }
        }

        public KeyBindingInputComponent()
        {
            //Bind to the key binding manager
            var keyBindingManager = IoCManager.Resolve<IKeyBindingManager>();
            keyBindingManager.BoundKeyDown += KeyDown;
            keyBindingManager.BoundKeyUp += KeyUp;
            _keyStates = new Dictionary<BoundKeyFunctions, bool>();
            _keyHandlers = new Dictionary<BoundKeyFunctions, KeyEvent>();
            //Set up keystates
        }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type, params object[] list)
        {
            var reply = base.RecieveMessage(sender, type, list);

            if (sender == this)
                return ComponentReplyMessage.Empty;

            switch (type)
            {
                case ComponentMessageType.Die:
                    Disable();
                    break;
                case ComponentMessageType.Live:
                    Enable();
                    break;
            }

            return reply;
        }

        public override void Shutdown()
        {
            base.Shutdown();
            var keyBindingManager = IoCManager.Resolve<IKeyBindingManager>();
            keyBindingManager.BoundKeyDown -= KeyDown;
            keyBindingManager.BoundKeyUp -= KeyUp;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            if(_enabled)
                UpdateKeys(frameTime);
        }

        private void Enable()
        {
            _enabled = true;
        }

        private void Disable()
        {
            _enabled = false;

            //Remove all active key states and send keyup messages for them.
            foreach (var state in _keyStates.ToList())
            {
                Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, state.Key, BoundKeyState.Up);
                Owner.SendMessage(this, ComponentMessageType.BoundKeyChange, state.Key, BoundKeyState.Up);
                _keyStates.Remove(state.Key);
            }
        }

        public virtual void KeyDown(object sender, BoundKeyEventArgs e)
        {
            if (!_enabled || GetKeyState(e.Function))
                return; //Don't repeat keys that are already down.
            Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, e.Function, e.FunctionState);
            SetKeyState(e.Function, true);
            Owner.SendMessage(this, ComponentMessageType.BoundKeyChange, e.Function, e.FunctionState);
        }

        public virtual void KeyUp(object sender, BoundKeyEventArgs e)
        {
            if (!_enabled)
                return;
            Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, e.Function, e.FunctionState);
            SetKeyState(e.Function, false);
            Owner.SendMessage(this, ComponentMessageType.BoundKeyChange, e.Function, e.FunctionState);
        }

        protected void SetKeyState(BoundKeyFunctions k, bool state)
        {
            // Check to see if we have a keyhandler for the key that's been pressed. Discard invalid keys.
            _keyStates[k] = state;

        }

        public bool GetKeyState(BoundKeyFunctions k)
        {
            if (_keyStates.Keys.Contains(k))
                return _keyStates[k];
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
                from keyState in _keyStates
                join handler in _keyHandlers on keyState.Key equals handler.Key
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
            foreach (var state in _keyStates.ToList())
            {
                if (state.Value == false)
                    _keyStates.Remove(state.Key);
                else
                    Owner.SendMessage(this, ComponentMessageType.BoundKeyRepeat, state.Key, BoundKeyState.Repeat);
            }
            //lastKeyUpdate = entityManager.now;
        }
    }
}
