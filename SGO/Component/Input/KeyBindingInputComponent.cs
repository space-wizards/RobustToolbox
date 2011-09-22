using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared;

namespace SGO
{
    /// <summary>
    /// This class recieves keypresses from the attached client and forwards them to other components.
    /// </summary>
    public class KeyBindingInputComponent : GameObjectComponent
    {
        public override void HandleNetworkMessage(IncomingEntityComponentMessage message)
        {
            BoundKeyFunctions keyFunction = (BoundKeyFunctions)message.messageParameters[0];
            BoundKeyState keyState = (BoundKeyState)message.messageParameters[1];

            if (keyState == BoundKeyState.Down)
                Owner.SendMessage(this, MessageType.KeyDown, keyFunction);
            if (keyState == BoundKeyState.Up)
                Owner.SendMessage(this, MessageType.KeyUp, keyFunction);
        }
    }
}
