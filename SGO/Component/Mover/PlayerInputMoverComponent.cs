using System;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;
using SS13_Shared.GO.Component.Mover;

namespace SGO
{
    //Moves the entity based on input from a Clientside KeyBindingMoverComponent.
    public class PlayerInputMoverComponent : GameObjectComponent
    {
        public PlayerInputMoverComponent()
        {
            family = ComponentFamily.Mover;
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
            Vector2 oldPosition = Owner.Position;
            Owner.Position = new Vector2(x, y);
            Owner.Moved(oldPosition);
            SendPositionUpdate();
        }

        public void SendPositionUpdate(bool forced = false)
        {
            Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, null, Owner.Position.X,
                                              Owner.Position.Y, forced);
        }

        public void SendPositionUpdate(NetConnection client, bool forced = false)
        {
            Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, client, Owner.Position.X,
                                              Owner.Position.Y, forced);
        }

        public override void HandleInstantiationMessage(NetConnection netConnection)
        {
            SendPositionUpdate(netConnection, true);
        }
        
        public override ComponentState GetComponentState()
        {
            return new MoverComponentState(Owner.Position.X, Owner.Position.Y, Owner.Velocity.X, Owner.Velocity.Y);
        }
    }
}