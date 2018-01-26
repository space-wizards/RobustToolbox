using System;
using System.Collections.Generic;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.System;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Maths;

namespace SS14.Server.GameObjects.EntitySystems
{
    internal class PhysicsSystem : EntitySystem
    {
        private const float Epsilon = 1.0e-6f;
        private const float GlobalFriction = 0.01f;

        public PhysicsSystem()
        {
            EntityQuery = new ComponentEntityQuery
            {
                AllSet = new List<Type>
                {
                    typeof(PhysicsComponent),
                    typeof(CollidableComponent)
                }
            };
        }

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            var entities = EntityManager.GetEntities(EntityQuery);
            foreach (var entity in entities)
                DoMovement(entity, frameTime);
        }

        private static void DoMovement(IEntity entity, float frameTime)
        {
            var transform = entity.GetComponent<TransformComponent>();
            var velocity = entity.GetComponent<PhysicsComponent>();

            //"space friction"
            if (velocity.Velocity.LengthSquared > Epsilon)
                velocity.Velocity -= velocity.Velocity * (frameTime * GlobalFriction);
            else
                velocity.Velocity = Vector2.Zero;

            var movement = velocity.Velocity * frameTime;

            //Check for collision
            if (movement.LengthSquared > Epsilon && entity.TryGetComponent(out CollidableComponent collider))
            {
                var collided = collider.TryCollision(movement);
                if (collided)
                {
                    var xBlocked = collider.TryCollision(new Vector2(movement.X, 0), true);
                    var yBlocked = collider.TryCollision(new Vector2(0, movement.Y), true);
                    var v = velocity.Velocity;
                    velocity.Velocity = new Vector2(xBlocked ? 0 : v.X, yBlocked ? 0 : v.Y);
                    movement = velocity.Velocity * frameTime;
                }
            }

            //Apply velocity
            transform.WorldPosition += movement;
        }
    }
}
