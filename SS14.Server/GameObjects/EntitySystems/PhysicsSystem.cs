using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.System;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using System;
using System.Collections.Generic;

namespace SS14.Server.GameObjects.EntitySystems
{
    internal class PhysicsSystem : EntitySystem
    {
        public PhysicsSystem()
        {
            EntityQuery = new ComponentEntityQuery()
            {
                AllSet = new List<Type>()
                {
                    typeof(PhysicsComponent),
                    typeof(ITransformComponent),
                    typeof(IVelocityComponent),
                },
                ExclusionSet = new List<Type>()
                {
                    typeof(SlaveMoverComponent),
                    typeof(PlayerInputMoverComponent),
                },
            };
        }

        public override void Update(float frametime)
        {
            var entities = EntityManager.GetEntities(EntityQuery);
            foreach (var entity in entities)
            {
                //GasEffect(entity, frametime);

                var transform = entity.GetComponent<ITransformComponent>();
                var velocity = entity.GetComponent<IVelocityComponent>();

                if (velocity.Velocity.LengthSquared() < 0.00001f)
                    continue;
                //Decelerate
                velocity.Velocity -= (velocity.Velocity * (frametime * 0.01f));

                var movement = velocity.Velocity * frametime;
                //Apply velocity
                transform.Position += movement;
            }
        }

        //private void GasEffect(Entity entity, float frameTime)
        //{
        //    var transform = entity.GetComponent<TransformComponent>(ComponentFamily.Transform);
        //    var physics = entity.GetComponent<PhysicsComponent>(ComponentFamily.Physics);
        //    ITile t =
        //        IoCManager.Resolve<IMapManager>().GetFloorAt(transform.Position);
        //    if (t == null)
        //        return;
        //    var gasVel = t.GasCell.GasVelocity;
        //    if (gasVel.Abs() > physics.Mass) // Stop tiny wobbles
        //    {
        //        transform.Position = new Vector2(transform.X + (gasVel.X * frameTime), transform.Y + (gasVel.Y * frameTime));
        //    }
        //}
    }
}
