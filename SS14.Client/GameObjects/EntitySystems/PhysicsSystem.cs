using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.System;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;

namespace SS14.Client.GameObjects.EntitySystems
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
                },
            };
        }

        /// <summary>
        /// Update
        ///
        /// This system is currently slightly dumb -- it only does player movement right now because
        /// all other movement is done via straight interpolation through coordinates sent from the server.
        /// </summary>
        /// <param name="frametime"></param>
        public override void Update(float frametime)
        {
            /*var entities = EntityManager.GetEntities(EntityQuery);
            return;
            foreach(var entity in entities)
            {
                var transform = entity.GetComponent<TransformComponent>(ComponentFamily.Transform);
                var velocity = entity.GetComponent<VelocityComponent>(ComponentFamily.Velocity);

                //Decelerate
                velocity.Velocity -= (velocity.Velocity * (frametime * 0.01f));

                var movement = velocity.Velocity*frametime;

                var mover = entity.GetComponent<PlayerInputMoverComponent>(ComponentFamily.Mover);
                if(mover != null && movement.Length > 0)
                {
                    //Check for collision
                    var collider = entity.GetComponent<ColliderComponent>(ComponentFamily.Collider);
                    if(collider != null)
                    {
                        bool collided = collider.TryCollision(movement);
                        bool collidedx, collidedy;
                        if(collided)
                        {
                            collidedx = collider.TryCollision(new Vector2(movement.X, 0));
                            if(collidedx)
                                velocity.X = 0;
                            collidedy = collider.TryCollision(new Vector2(0, movement.Y));
                            if (collidedy)
                                velocity.Y = 0;
                            movement = velocity.Velocity*frametime;
                        }
                    }
                    if (movement.Length > 0.001f)
                    {
                        transform.TranslateByOffset(movement);
                        mover.ShouldSendPositionUpdate = true;
                    }
                }
                if (mover != null && mover.ShouldSendPositionUpdate)
                {
                    mover.SendPositionUpdate(transform.Position);
                    mover.ShouldSendPositionUpdate = false;
                }
                /*else
                {
                    //Apply velocity
                    transform.Position += movement;
                }#1#
            }*/
        }
    }
}
