using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;
using SS13_Shared.GO.Component.Mover;
using SS13.IoC;
using ServerInterfaces.Map;
using System.Collections.Generic;
using ServerServices.Log;

namespace SGO
{
    internal class Physics : GameObjectComponent
    {

        private float mass = 0.0f;

        public Physics()
        {
            family = ComponentFamily.Physics;
        }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            ComponentReplyMessage reply = base.RecieveMessage(sender, type, list);

            if (sender == this)
                return ComponentReplyMessage.Empty;

            return reply;
        }

        public override void HandleInstantiationMessage(NetConnection netConnection)
        {

        }

        public override void Update(float frameTime)
        {
            if (Owner.GetComponent<SlaveMoverComponent>(ComponentFamily.Mover) != null)  // If we are being moved by something else right now (like being carried) dont be affected by physics
                return;

            GasEffect();
        }

        private void GasEffect()
        {
            Vector2 gasVel = IoCManager.Resolve<IMapManager>().GetGasVelocity(IoCManager.Resolve<IMapManager>().GetTileArrayPositionFromWorldPosition(Owner.Position));
            if (gasVel.Abs() > mass) // Stop tiny wobbles
            {
                Owner.SendMessage(this, ComponentMessageType.PhysicsMove, Owner.Position.X + gasVel.X, Owner.Position.Y + gasVel.Y);
            }
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
                case "Mass":
                    mass = parameter.GetValue<float>();
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
