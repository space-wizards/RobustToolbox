using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;

namespace SGO
{
    /// <summary>
    /// This class recieves keypresses from the attached client and forwards them to other components.
    /// </summary>
    public class KeyBindingInputComponent : GameObjectComponent
    {
        public KeyBindingInputComponent()
        {
            family = ComponentFamily.Input;
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection client)
        {
            var keyFunction = (BoundKeyFunctions) message.MessageParameters[0];
            var keyState = (BoundKeyState) message.MessageParameters[1];

            Owner.SendMessage(this, ComponentMessageType.BoundKeyChange, keyFunction, keyState);
        }
    }
}