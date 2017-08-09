using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.System;

namespace SS14.Server.GameObjects.EntitySystems
{
    internal class PhysicsSystem : EntitySystem
    {
        public PhysicsSystem()
        {
            EntityQuery = new EntityQuery();
            EntityQuery.AllSet.Add(typeof(PhysicsComponent));
        }

        public override void Update(float frametime)
        {
            var entities = EntityManager.GetEntities(EntityQuery);
            foreach (var entity in entities)
            {
                var transform = entity.GetComponent<TransformComponent>();
                var bounds = entity.GetComponent<BoundingBoxComponent>();
                var physics = entity.GetComponent<PhysicsComponent>();

                //TODO: All physics happens in here.

                if (physics.Velocity.LengthSquared < 0.00001f)
                    continue;

                //Decelerate
                physics.Velocity -= physics.Velocity * (frametime * 0.01f);

                var movement = physics.Velocity * frametime;

                //Apply velocity
                transform.Position += movement;
            }
        }
    }
}
