using System.Collections.Generic;
using SS14.Shared.GameObjects;
using SS14.Shared.Log;
using SS14.Shared.Enums;
using SS14.Shared.Input;

namespace SS14.Server.GameObjects
{
    /// <summary>
    /// This class receives keypresses from the attached client and forwards them to other components.
    /// </summary>
    public class KeyBindingInputComponent : Component
    {
        public override string Name => "KeyBindingInput";
        public override uint? NetID => NetIDs.KEY_BINDING_INPUT;

        public override void HandleMessage(object owner, ComponentMessage message)
        {
            base.HandleMessage(owner, message);

            switch (message)
            {
                case BoundKeyChangedMsg msg:
                    if (msg.Remote) break;
                    var keyFunction = msg.Function;
                    var keyState = msg.State;

                    var boolState = keyState == BoundKeyState.Down;
                    SetKeyState(keyFunction, boolState);
                    break;
            }
        }

        private readonly Dictionary<BoundKeyFunctions, bool> _keyStates = new Dictionary<BoundKeyFunctions, bool>();

        protected void SetKeyState(BoundKeyFunctions k, bool state)
        {
            // Check to see if we have a keyhandler for the key that's been pressed. Discard invalid keys.
            _keyStates[k] = state;
        }

        public bool GetKeyState(BoundKeyFunctions k)
        {
            if (_keyStates.ContainsKey(k))
                return _keyStates[k];
            return false;
        }
    }
}
