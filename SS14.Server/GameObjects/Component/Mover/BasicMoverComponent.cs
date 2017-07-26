using SFML.System;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components;
using SS14.Shared.IoC;

namespace SS14.Server.GameObjects
{
    internal class BasicMoverComponent : Component, IMoverComponent
    {
        public override string Name => "BasicMover";
        public override uint? NetID => NetIDs.BASIC_MOVER;
        public override bool NetworkSynchronizeExistence => true;

        public override ComponentReplyMessage ReceiveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            ComponentReplyMessage reply = base.ReceiveMessage(sender, type, list);

            if (sender == this)
                return ComponentReplyMessage.Empty;

            switch (type)
            {
                case ComponentMessageType.PhysicsMove:
                    Translate((float) list[0], (float) list[1]);
                    break;
            }
            return reply;
        }

        public void Translate(float x, float y)
        {
            Owner.GetComponent<TransformComponent>().Position = new Vector2f(x, y);
        }

    }
}
