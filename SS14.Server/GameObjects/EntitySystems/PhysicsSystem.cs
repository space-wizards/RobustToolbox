using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.System;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using SS14.Shared.Utility;

namespace SS14.Server.GameObjects.EntitySystems
{
    internal class PhysicsSystem : EntitySystem
    {
        public PhysicsSystem()
        {
            EntityQuery = new EntityQuery();
            EntityQuery.AllSet.Add(typeof(PhysicsComponent));
            EntityQuery.AllSet.Add(typeof(ITransformComponent));
            EntityQuery.ExclusionSet.Add(typeof(SlaveMoverComponent));
            EntityQuery.ExclusionSet.Add(typeof(PlayerInputMoverComponent));
        }

        public override void Update(float frametime)
        {
            var entities = EntityManager.GetEntities(EntityQuery);
            foreach (var entity in entities)
            {
                //GasEffect(entity, frametime);

                var transform = entity.GetComponent<ITransformComponent>();
                var physics = entity.GetComponent<PhysicsComponent>();

                if (physics.Velocity.LengthSquared < 0.00001f)
                    continue;
                //Decelerate
                physics.Velocity -= (physics.Velocity * (frametime * 0.01f));

                var movement = physics.Velocity * frametime;
                //Apply velocity
                transform.Position += movement.Convert();
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
