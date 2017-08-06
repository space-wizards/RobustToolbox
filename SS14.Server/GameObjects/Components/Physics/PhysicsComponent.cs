using SS14.Shared.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Utility;
using System.Collections.Generic;
using YamlDotNet.RepresentationModel;

namespace SS14.Server.GameObjects
{
    public class PhysicsComponent : Component
    {
        public override string Name => "Physics";
        public override uint? NetID => NetIDs.PHYSICS;
        public float Mass { get; set; }

        public override void Update(float frameTime)
        {
            /*if (Owner.GetComponent<SlaveMoverComponent>(ComponentFamily.Mover) != null)
                // If we are being moved by something else right now (like being carried) dont be affected by physics
                return;

            GasEffect();*/
        }

        //private void GasEffect()
        //{
        //    ITile t = IoCManager.Resolve<IMapManager>().GetFloorAt(Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position);
        //    if (t == null)
        //        return;
        //    Vector2 gasVel = t.GasCell.GasVelocity;
        //    if (gasVel.Abs() > Mass) // Stop tiny wobbles
        //    {
        //        Owner.SendMessage(this, ComponentMessageType.PhysicsMove,
        //                          Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.X +
        //                          gasVel.X,
        //                          Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.Y +
        //                          gasVel.Y);
        //    }
        //}

        public override void LoadParameters(YamlMappingNode mapping)
        {
            YamlNode node;
            if (mapping.TryGetNode("mass", out node))
            {
                Mass = node.AsFloat();
            }
        }

        public override ComponentState GetComponentState()
        {
            return new PhysicsComponentState(Mass);
        }
    }
}
