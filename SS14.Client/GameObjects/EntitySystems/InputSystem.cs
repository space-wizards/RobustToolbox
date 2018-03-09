using System;
using System.Collections.Generic;
using System.Linq;
using SS14.Client.Input;
using SS14.Client.Interfaces.Input;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.System;
using SS14.Shared.Input;
using SS14.Shared.IoC;

namespace SS14.Client.GameObjects.EntitySystems
{
    public class InputSystem : EntitySystem
    {
        public delegate void KeyEvent(bool state);

        private readonly Dictionary<BoundKeyFunctions, KeyEvent> _keyHandlers = new Dictionary<BoundKeyFunctions, KeyEvent>();
        private readonly Dictionary<BoundKeyFunctions, bool> _keyStates = new Dictionary<BoundKeyFunctions, bool>();

        private bool _enabled = true;
        
        public override void Initialize()
        {
            base.Initialize();

            var keyBindingManager = IoCManager.Resolve<IKeyBindingManager>();
            keyBindingManager.BoundKeyDown += KeyDown;
            keyBindingManager.BoundKeyUp += KeyUp;
        }
        
        public override void Shutdown()
        {
            base.Shutdown();

            var keyBindingManager = IoCManager.Resolve<IKeyBindingManager>();
            keyBindingManager.BoundKeyDown -= KeyDown;
            keyBindingManager.BoundKeyUp -= KeyUp;
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
                var message = new BoundKeyChangedMessage(state.Key, BoundKeyState.Up);
                RaiseEvent(message);
                RaiseNetworkEvent(message);
                _keyStates.Remove(state.Key);
            }
        }

        public virtual void KeyDown(object sender, BoundKeyEventArgs e)
        {
            if (!_enabled || GetKeyState(e.Function))
                return; //Don't repeat keys that are already down.

            SetKeyState(e.Function, true);

            var message = new BoundKeyChangedMessage(e.Function, e.FunctionState);
            RaiseEvent(message);
            RaiseNetworkEvent(message);
        }

        public virtual void KeyUp(object sender, BoundKeyEventArgs e)
        {
            if (!_enabled)
                return;

            SetKeyState(e.Function, false);

            var message = new BoundKeyChangedMessage(e.Function, e.FunctionState);
            RaiseEvent(message);
            RaiseNetworkEvent(message);
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
            // So basically we check for active keys with handlers and execute them. This is a linq query.
            // Get all of the active keys' handlers
            var activeKeyHandlers =
                from keyState in _keyStates
                join handler in _keyHandlers on keyState.Key equals handler.Key
                select new {evt = handler.Value, state = keyState.Value};

            //Execute the bastards!
            foreach (var keyHandler in activeKeyHandlers)
            {
                //If there's even one active, we set updateRequired so that this gets hit again next update
                //updateRequired = true; // QUICKNDIRTY
                var k = keyHandler.evt;
                k(keyHandler.state);
            }

            //Delete false states from the dictionary so they don't get reprocessed and fuck up other stuff.
            foreach (var state in _keyStates.ToList())
            {
                if (!state.Value)
                    _keyStates.Remove(state.Key);
            }
        }

        public virtual void RaiseClick(ClickEventMessage message)
        {
            //TODO: Check modifiers here and modify the click type of the message?
            RaiseNetworkEvent(message);
        }
    }
}
