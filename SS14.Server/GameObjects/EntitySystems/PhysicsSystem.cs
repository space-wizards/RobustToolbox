using System;
using System.Linq;
using SS14.Server.Interfaces.Timing;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Systems;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Physics;
using SS14.Shared.IoC;
using SS14.Shared.Maths;

namespace SS14.Server.GameObjects.EntitySystems
{
    internal class PhysicsSystem : EntitySystem
    {
        private IPauseManager _pauseManager;
        private IPhysicsManager _physicsManager;
        private const float Epsilon = 1.0e-6f;
        private const float GlobalFriction = 0.01f;

        public PhysicsSystem()
        {
            EntityQuery = new TypeEntityQuery(typeof(PhysicsComponent));
        }

        public override void Initialize()
        {
            base.Initialize();
           
            _pauseManager = IoCManager.Resolve<IPauseManager>();
            _physicsManager = IoCManager.Resolve<IPhysicsManager>();
        }

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            var entities = EntityManager.GetEntities(EntityQuery);
            _physicsManager.BuildCollisionGrid();
            foreach (var entity in entities)
            {
                if (_pauseManager.IsEntityPaused(entity))
                {
                    continue;
                }
                HandleMovement(entity, frameTime);
            }
            foreach(var entity in entities)
            {
                DoMovement(entity, frameTime);
            }
        }

        private static void HandleMovement(IEntity entity, float frameTime)
        {
            var velocity = entity.GetComponent<PhysicsComponent>();
            if (velocity.DidMovementCalculations)
            {
                velocity.DidMovementCalculations = false;
                return;
            }

            if (velocity.AngularVelocity == 0 && velocity.LinearVelocity == Vector2.Zero)
            {
                return;
            }

            var velocityConsumers = velocity.GetVelocityConsumers();
            var initialMovement = velocity.LinearVelocity;

            int velocityConsumerCount;
            float totalMass;
            Vector2 lowestMovement;
            do
            {
                velocityConsumerCount = velocityConsumers.Count;
                totalMass = 0;
                lowestMovement = initialMovement;
                lowestMovement = velocityConsumers.Select(velocityConsumer =>
                {
                    totalMass += velocityConsumer.Mass;
                    var movement = lowestMovement * velocity.Mass / totalMass;
                    velocityConsumer.AngularVelocity = velocity.AngularVelocity;
                    velocityConsumer.LinearVelocity = movement;
                    return CalculateMovement(velocityConsumer, frameTime, velocityConsumer.Owner) / frameTime;
                }).Min();
                velocityConsumers = velocity.GetVelocityConsumers();
            }
            while (velocityConsumers.Count != velocityConsumerCount);
            velocity.ClearVelocityConsumers();

            velocityConsumers.ForEach(velocityConsumer =>
            {
                velocityConsumer.LinearVelocity = lowestMovement;
                velocityConsumer.DidMovementCalculations = true;
            });
            velocity.DidMovementCalculations = false;
        }

        private static void DoMovement(IEntity entity, float frameTime)
        {
            var velocity = entity.GetComponent<PhysicsComponent>();
            float angImpulse = 0;
            if (velocity.AngularVelocity > Epsilon)
            {
                angImpulse = velocity.AngularVelocity * frameTime;
            }
            var transform = entity.Transform;
            transform.LocalRotation += angImpulse;
            transform.WorldPosition += velocity.LinearVelocity * frameTime;
        }

        private static Vector2 CalculateMovement(PhysicsComponent velocity, float frameTime, IEntity entity)
        {
            var movement = velocity.LinearVelocity * frameTime;
            //"space friction"
            if (movement.LengthSquared > Epsilon)
            {
                movement -= movement * GlobalFriction;
            }
            else
            {
                return Vector2.Zero;
            }

            //Check for collision
            if (movement.LengthSquared > Epsilon && entity.TryGetComponent(out CollidableComponent collider))
            {
                var collided = collider.TryCollision(movement, true);

                if (collided)
                {
                    if (velocity.EdgeSlide)
                    {
                        //Slide along the blockage in the non-blocked direction
                        var xBlocked = collider.TryCollision(new Vector2(movement.X, 0));
                        var yBlocked = collider.TryCollision(new Vector2(0, movement.Y));

                        movement = new Vector2(xBlocked ? 0 : movement.X, yBlocked ? 0 : movement.Y);
                    }
                    else
                    {
                        //Stop movement entirely at first blockage
                        movement = new Vector2(0, 0);
                    }
                }

                if (movement != Vector2.Zero && collider.IsInteractingWithFloor && entity.TryGetComponent<ITransformComponent>(out var location))
                {
                    var grid = location.GridPosition.Grid;
                    var tile = grid.GetTile(location.GridPosition);
                    var tileDef = tile.TileDef;
                    if (tileDef.Friction != 0)
                    {
                        movement -= movement * tileDef.Friction;
                        if (movement.LengthSquared <= velocity.Mass * Epsilon / (1 - tileDef.Friction))
                        {
                            movement = Vector2.Zero;
                        }
                    }
                }
            }
            return movement;
        }
    }
}
