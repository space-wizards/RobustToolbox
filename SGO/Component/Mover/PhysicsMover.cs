using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;
using SS13_Shared.GO.Component.Mover;
using SS13.IoC;
using ServerInterfaces.Map;
using System.Collections.Generic;

namespace SGO
{
    internal class PhysicsMover : GameObjectComponent
    {

        private float weight = 0.0f;

        public PhysicsMover()
        {
            family = ComponentFamily.Mover;
        }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            ComponentReplyMessage reply = base.RecieveMessage(sender, type, list);

            if (sender == this)
                return ComponentReplyMessage.Empty;

            switch (type)
            {
                case ComponentMessageType.SendPositionUpdate:
                    SendPositionUpdate(true);
                    break;
            }
            return reply;
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

        public override void Update(float frameTime)
        {
            
            Vector2 gasVel = IoCManager.Resolve<IMapManager>().GetGasVelocity(IoCManager.Resolve<IMapManager>().GetTileArrayPositionFromWorldPosition(Owner.Position));
            if (gasVel.Abs() > weight) // Stop tiny wobbles (hack fix until we add weight)
                Translate(Owner.Position.X + gasVel.X, Owner.Position.Y + gasVel.Y);
            
        }

        public override ComponentState GetComponentState()
        {
            return new MoverComponentState(Owner.Position.X, Owner.Position.Y, Owner.Velocity.X, Owner.Velocity.Y);
        }

        public override void SetParameter(ComponentParameter parameter)
        {
            base.SetParameter(parameter);

            switch (parameter.MemberName)
            {
                case "Weight":
                    weight = parameter.GetValue<float>();
                    break;

            }
        }

        public override List<ComponentParameter> GetParameters()
        {
            var cparams = base.GetParameters();
            return cparams;
        }
    }
}
