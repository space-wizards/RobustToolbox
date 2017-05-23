using Lidgren.Network;
using SFML.System;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;

namespace SS14.Server.GameObjects
{
    //Moves the entity based on input from a Clientside PlayerInputMoverComponent.
    [IoCTarget]
    [Component("PlayerInputMover")]
    public class PlayerInputMoverComponent : Component
    {
        public PlayerInputMoverComponent()
        {
            Family = ComponentFamily.Mover;
        }

        /// <summary>
        /// Handles position messages. that should be it.
        /// </summary>
        /// <param name="message"></param>
        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection client)
        {
            bool shouldMove = true;

            if (shouldMove)
            {
                var velComp = Owner.GetComponent<VelocityComponent>(ComponentFamily.Velocity);
                var transform = Owner.GetComponent<TransformComponent>(ComponentFamily.Transform);

                velComp.Velocity = new Vector2f((float)message.MessageParameters[2], (float)message.MessageParameters[3]);
                transform.Position = new Vector2f((float)message.MessageParameters[0], (float)message.MessageParameters[1]);
            }
        }
    }
}
