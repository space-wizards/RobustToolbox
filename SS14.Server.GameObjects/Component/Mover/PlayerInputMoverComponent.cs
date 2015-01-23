using Lidgren.Network;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;

namespace SS14.Server.GameObjects
{
    //Moves the entity based on input from a Clientside PlayerInputMoverComponent.
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
            if (Owner.HasComponent(ComponentFamily.StatusEffects))
            {
                var statComp = (StatusEffectComp) Owner.GetComponent(ComponentFamily.StatusEffects);
                if (statComp.HasFamily(StatusEffectFamily.Root) || statComp.HasFamily(StatusEffectFamily.Stun))
                    shouldMove = false;
            }

            if (shouldMove)
            {
                var velComp = Owner.GetComponent<VelocityComponent>(ComponentFamily.Velocity);
                var transform = Owner.GetComponent<TransformComponent>(ComponentFamily.Transform);

                velComp.Velocity = new Vector2((float)message.MessageParameters[2], (float)message.MessageParameters[3]);
                transform.Position = new Vector2((float)message.MessageParameters[0], (float)message.MessageParameters[1]);
            }
        }
    }
}