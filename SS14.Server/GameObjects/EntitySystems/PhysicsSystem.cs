using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.System;
using SS14.Shared.Interfaces.GameObjects.Components;
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
            //EntityQuery.ExclusionSet.Add(typeof(PlayerInputMoverComponent));
        }

        public override void Update(float frametime)
        {
            var entities = EntityManager.GetEntities(EntityQuery);
            foreach (var entity in entities)
            {
                var transform = entity.GetComponent<ITransformComponent>();
                var physics = entity.GetComponent<PhysicsComponent>();

                if (physics.Velocity.LengthSquared < 0.00001f)
                    continue;

                //Decelerate
                physics.Velocity -= physics.Velocity * (frametime * 0.01f);

                var movement = physics.Velocity * frametime;
                //Apply velocity
                transform.Position += movement.Convert();
            }
        }
    }
}
