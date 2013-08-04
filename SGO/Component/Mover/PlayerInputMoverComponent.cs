using GameObject;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;
using SS13_Shared.GO.Component.Mover;

namespace SGO
{
    //Moves the entity based on input from a Clientside KeyBindingMoverComponent.
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
                Translate((float) message.MessageParameters[0],
                          (float) message.MessageParameters[1]);
            else SendPositionUpdate(true); //Tried to move even though they cant. Lets pin that fucker down.
        }

        public void Translate(float x, float y)
        {
            Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position = new Vector2(x, y);
            SendPositionUpdate();
        }

        public void SendPositionUpdate(bool forced = false)
        {
        }

        public void SendPositionUpdate(NetConnection client, bool forced = false)
        {
        }

        public override void HandleInstantiationMessage(NetConnection netConnection)
        {
            SendPositionUpdate(netConnection, true);
        }

        public override ComponentState GetComponentState()
        {
            return new MoverComponentState(Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.X,
                                           Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.Y,
                                           Owner.GetComponent<VelocityComponent>(ComponentFamily.Velocity).Velocity.X,
                                           Owner.GetComponent<VelocityComponent>(ComponentFamily.Velocity).Velocity.Y);
        }
    }
}