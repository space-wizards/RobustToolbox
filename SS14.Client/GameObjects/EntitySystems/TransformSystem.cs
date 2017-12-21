using SFML.System;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Client.Interfaces.Player;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.System;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Timing;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using System;
using System.Collections.Generic;

namespace SS14.Client.GameObjects.EntitySystems
{
    internal class TransformSystem : EntitySystem
    {
        public TransformSystem()
        {
            EntityQuery = new ComponentEntityQuery()
            {
                AllSet = new List<Type>()
                {
                    typeof(ITransformComponent),
                },
                ExclusionSet = new List<Type>()
                {
                    typeof(SlaveMoverComponent),
                },
            };
        }

        private Vector2? calculateNewPosition(IEntity entity, Vector2 newPosition, ITransformComponent transform)
        {
            //Check for collision
            var collider = entity.GetComponent<ColliderComponent>();
            bool collided = collider.TryCollision(newPosition - transform.Position, true);
            if (!collided)
            {
                return newPosition;
            }

            // When modifying the movement diagonally we need to scale it down
            // as the magnitude is too high for cardinal movements
            float diagonalMovementScale = 0.75f;

            Vector2 newPositionX = newPosition;
            newPositionX.X = transform.Position.X;
            bool collidedX = collider.TryCollision(newPositionX - transform.Position, true);
            if (!collidedX)
            {
                // Add back the lost speed from colliding with the wall
                // but because we was moving diagonally we need to scale it down
                newPositionX.Y += (newPositionX.Y - transform.Position.Y) * diagonalMovementScale;
                return newPositionX;
            }

            Vector2 newPositionY = newPosition;
            newPositionY.Y = transform.Position.Y;
            bool collidedY = collider.TryCollision(newPositionY - transform.Position, true);
            if (!collidedY)
            {
                // Add back the lost speed from colliding with the wall
                // but because we was moving diagonally we need to scale it down
                newPositionY.X += (newPositionY.X - transform.Position.X) * diagonalMovementScale;
                return newPositionY;
            }

            return null;
        }

        public override void Update(float frametime)
        {
            var entities = EntityManager.GetEntities(EntityQuery);
            //Interp constant -- determines how far back in time to interpolate from
            var interpolation = IoCManager.Resolve<IConfigurationManager>().GetCVar<float>("net.interpolation");
            Vector2 newPosition;
            foreach (var entity in entities)
            {
                //Get transform component
                var transform = entity.GetComponent<IClientTransformComponent>();
                //Check if the entity has a keyboard input mover component
                bool isLocallyControlled = entity.HasComponent<PlayerInputMoverComponent>()
                    && IoCManager.Resolve<IPlayerManager>().ControlledEntity == entity;

                //Pretend that the current point in time is actually 100 or more milliseconds in the past depending on the interp constant
                var currentTime = (float)IoCManager.Resolve<IGameTiming>().CurTime.TotalSeconds - interpolation;

                //Limit to how far a human can move
                var humanMoveLimit = 6 * interpolation * PlayerInputMoverComponent.FastMoveSpeed;

                // If the "to" interp position is equal to the "from" interp position,
                // OR we're actually trying to interpolate past the "to" state
                // OR we're trying to interpolate a point older than the oldest state in memory
                if (transform.lerpStateTo == transform.lerpStateFrom ||
                    currentTime > transform.lerpStateTo.ReceivedTime ||
                    currentTime < transform.lerpStateFrom.ReceivedTime)
                {
                    // Fall back to setting the position to the "To" state
                    newPosition = transform.lerpStateTo.Position;
                }
                else //OTHERWISE
                {
                    //Interpolate

                    var p1 = new Vector2(transform.lerpStateFrom.Position.X, transform.lerpStateTo.Position.Y);
                    var p2 = new Vector2(transform.lerpStateTo.Position.X, transform.lerpStateTo.Position.Y);
                    var t1 = transform.lerpStateFrom.ReceivedTime;
                    var t2 = transform.lerpStateTo.ReceivedTime;

                    // linear interpolation from the state immediately prior to the "current time"
                    // to the state immediately after the "current time"
                    var lerp = (currentTime - t1) / (t2 - t1);
                    //lerp is a constant 0..1 value that says what position along the line from p1 to p2 we're at
                    newPosition = Interpolate(p1, p2, lerp, false);
                    if (isLocallyControlled)
                    {
                        newPosition = EaseExponential(currentTime - t1, transform.Position, newPosition, t2 - t1);
                    }
                }

                //Handle player movement
                if (isLocallyControlled)
                {
                    //var playerPosition = transform.Position +
                    if (entity.TryGetComponent<IVelocityComponent>(out var velocityComponent))
                    {
                        var movement = velocityComponent.Velocity * frametime;
                        var playerPosition = movement + transform.Position;
                        var difference = playerPosition - newPosition;
                        if (difference.LengthSquared() <= humanMoveLimit * humanMoveLimit)
                            //TODO do this by reducing the length of the difference vector to the acceptable amount and applying it
                            //Instead of just snapping back to the server's position
                            newPosition = playerPosition;
                    }
                    // Reduce rubber banding by easing to the position we're supposed to be at
                }

                if ((newPosition - transform.Position).LengthSquared() > 0.0000001f)// &&
                                                                                    //(!haskbMover || (newPosition - transform.Position).Length > humanMoveLimit))
                {
                    var doTranslate = false;
                    if (!isLocallyControlled)
                        doTranslate = true;
                    else
                    {
                        //Only for components with a keyboard input mover component, and a collider component
                        // Check for collision so we don't get shit stuck in objects
                        if (entity.HasComponent<ColliderComponent>())
                        {
                            Vector2? _newPosition = calculateNewPosition(entity, newPosition, transform);
                            if (_newPosition != null)
                            {
                                newPosition = _newPosition.Value;
                                doTranslate = true;
                            }
                        }
                        else
                        {
                            doTranslate = true;
                        }
                    }
                    if (doTranslate)
                    {
                        transform.Position = newPosition;
                        if (isLocallyControlled)
                            entity.GetComponent<PlayerInputMoverComponent>().SendPositionUpdate(newPosition);
                    }
                }
            }
        }

        private Vector2 EaseExponential(float time, Vector2 v1, Vector2 v2, float duration)
        {
            var dx = (v2.X - v1.X);
            var x = EaseExponential(time, v1.X, dx, duration);

            var dy = (v2.Y - v1.Y);
            var y = EaseExponential(time, v1.Y, dy, duration);
            return new Vector2(x, y);
        }

        private float EaseExponential(float t, float b, float c, float d)
        {
            return c * ((float)-Math.Pow(2, -10 * t / d) + 1) + b;
        }

        private Vector2 Interpolate(Vector2 v1, Vector2 v2, float control, bool allowExtrapolation)
        {
            if (!allowExtrapolation && (control > 1 || control < 0))
            {
                // Error message includes information about the actual value of the argument
                throw new ArgumentOutOfRangeException
                    (
                    "control",
                    control,
                    "Control parameter must be a value between 0 & 1\nThe argument provided has a value of " + control
                    );
            }
            else
            {
                return
                    (
                        new Vector2
                            (
                            v1.X * (1 - control) + v2.X * control,
                            v1.Y * (1 - control) + v2.Y * control
                            )
                    );
            }
        }
    }
}
