using System.Collections.Generic;
using GameObject;
using Lidgren.Network;
using SS13.IoC;
using SS13_Shared;
using SS13_Shared.GO;
using SS13_Shared.GO.Component.Mover;
using SS13_Shared.GO.Component.Physics;
using ServerInterfaces.Map;
using ServerInterfaces.Tiles;

namespace SGO
{
    internal class PhysicsComponent : Component
    {
        public float Mass { get; set; }

        public PhysicsComponent()
        {
            Family = ComponentFamily.Physics;
        }

        public override void Update(float frameTime)
        {
            /*if (Owner.GetComponent<SlaveMoverComponent>(ComponentFamily.Mover) != null)
                // If we are being moved by something else right now (like being carried) dont be affected by physics
                return;

            GasEffect();*/
        }

        private void GasEffect()
        {
            ITile t =
                IoCManager.Resolve<IMapManager>().GetTileFromWorldPosition(
                    Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position);
            if (t == null)
                return;
            Vector2 gasVel = t.GasCell.GasVelocity;
            if (gasVel.Abs() > Mass) // Stop tiny wobbles
            {
                Owner.SendMessage(this, ComponentMessageType.PhysicsMove,
                                  Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.X +
                                  gasVel.X,
                                  Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.Y +
                                  gasVel.Y);
            }
        }

        public override void SetParameter(ComponentParameter parameter)
        {
            base.SetParameter(parameter);

            switch (parameter.MemberName)
            {
                case "Mass":
                    Mass = parameter.GetValue<float>();
                    break;
            }
        }

        public override List<ComponentParameter> GetParameters()
        {
            List<ComponentParameter> cparams = base.GetParameters();
            return cparams;
        }

        public override ComponentState GetComponentState()
        {
            return new PhysicsComponentState(Mass);
        }
    }
}