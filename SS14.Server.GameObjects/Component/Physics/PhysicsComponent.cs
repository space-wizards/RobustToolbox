using SS14.Server.Interfaces.Map;
using SS14.Server.Interfaces.Tiles;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using SS14.Shared.GO.Component.Physics;
using SS14.Shared.IoC;
using System.Collections.Generic;
using SS14.Shared.Maths;

namespace SS14.Server.GameObjects
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

            ITile t = IoCManager.Resolve<IMapManager>().GetFloorAt(Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position);
            if (t == null)
                return;
            Vector2 gasVel = t.GasCell.GasVelocity;
            if (gasVel.Magnitude > Mass) // Stop tiny wobbles
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