using Lidgren.Network;
using SS14.Server.GameObjects.Events;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;
using System.Collections.Generic;
using SS14.Shared.Log;

namespace SS14.Server.GameObjects
{
    /// <summary>
    /// This class receives keypresses from the attached client and forwards them to other components.
    /// </summary>
    public class KeyBindingInputComponent : Component
    {
        public override string Name => "KeyBindingInput";
        public override uint? NetID => NetIDs.KEY_BINDING_INPUT;
        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection client)
        {
            Logger.Debug("Got a net message!");
            var keyFunction = (BoundKeyFunctions) message.MessageParameters[0];
            var keyState = (BoundKeyState) message.MessageParameters[1];

            var boolState = keyState == BoundKeyState.Down;
            SetKeyState(keyFunction, boolState);

            Owner.SendMessage(this, ComponentMessageType.BoundKeyChange, keyFunction, keyState);
            Owner.RaiseEvent(new BoundKeyChangeEventArgs{KeyFunction = keyFunction, KeyState = keyState, Actor = Owner});

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
