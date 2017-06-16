using SFML.System;
using SS14.Client.Interfaces.Configuration;
using SS14.Client.Interfaces.GameTimer;
using SS14.Client.Interfaces.Player;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.System;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using System;

namespace SS14.Client.GameObjects.EntitySystems
{
    internal class TransformSystem : EntitySystem
    {
        public TransformSystem()
        {
            EntityQuery = new EntityQuery();
            EntityQuery.AllSet.Add(typeof(TransformComponent));
            EntityQuery.ExclusionSet.Add(typeof(SlaveMoverComponent));
        }

        private Vector2f? calculateNewPosition(IEntity entity, Vector2f newPosition, TransformComponent transform)
        {
            //Check for collision
            var collider = entity.GetComponent<ColliderComponent>(ComponentFamily.Collider);
            bool collided = collider.TryCollision(newPosition - transform.Position, true);
            if (!collided)
            {
                return newPosition;
            }

            // When modifying the movement diagonally we need to scale it down
            // as the magnitude is too high for cardinal movements
            float diagonalMovementScale = 0.75f;

            Vector2f newPositionX = newPosition;
            newPositionX.X = transform.Position.X;
            bool collidedX = collider.TryCollision(newPositionX - transform.Position, true);
            if (!collidedX)
            {
                // Add back the lost speed from colliding with the wall
                // but because we was moving diagonally we need to scale it down
                newPositionX.Y += (newPositionX.Y - transform.Position.Y) * diagonalMovementScale;
                return newPositionX;
            }

            Vector2f newPositionY = newPosition;
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
            var interpolation = IoCManager.Resolve<IPlayerConfigurationManager>().GetInterpolation();
            Vector2f newPosition;
            foreach (var entity in entities)
            {
                //Get transform component
                var transform = entity.GetComponent<TransformComponent>(ComponentFamily.Transform);
                //Check if the entity has a keyboard input mover component
                bool isLocallyControlled = entity.GetComponent<PlayerInputMoverComponent>(ComponentFamily.Mover) != null
                    && IoCManager.Resolve<IPlayerManager>().ControlledEntity == entity;

                //Pretend that the current point in time is actually 100 or more milliseconds in the past depending on the interp constant
                var currentTime = IoCManager.Resolve<IGameTimer>().CurrentTime - interpolation;

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
                    newPosition = new Vector2f(transform.lerpStateTo.X, transform.lerpStateTo.Y);
                }
                else //OTHERWISE
                {
                    //Interpolate

                    var p1 = new Vector2f(transform.lerpStateFrom.X, transform.lerpStateTo.Y);
                    var p2 = new Vector2f(transform.lerpStateTo.X, transform.lerpStateTo.Y);
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
                    var velocityComponent = entity.GetComponent<VelocityComponent>(ComponentFamily.Velocity);
                    if (velocityComponent != null)
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
                        if (entity.GetComponent<ColliderComponent>(ComponentFamily.Collider) != null)
                        {
                            Vector2f? _newPosition = calculateNewPosition(entity, newPosition, transform);
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
                        transform.TranslateTo(newPosition);
                        if (isLocallyControlled)
                            entity.GetComponent<PlayerInputMoverComponent>(ComponentFamily.Mover).SendPositionUpdate(newPosition);
                    }
                }
            }
        }

        private Vector2f EaseExponential(float time, Vector2f v1, Vector2f v2, float duration)
        {
            var dx = (v2.X - v1.X);
            var x = EaseExponential(time, v1.X, dx, duration);

            var dy = (v2.Y - v1.Y);
            var y = EaseExponential(time, v1.Y, dy, duration);
            return new Vector2f(x, y);
        }

        private float EaseExponential(float t, float b, float c, float d)
        {
            return c * ((float)-Math.Pow(2, -10 * t / d) + 1) + b;
        }

        private Vector2f Interpolate(Vector2f v1, Vector2f v2, float control, bool allowExtrapolation)
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
                        new Vector2f
                            (
                            v1.X * (1 - control) + v2.X * control,
                            v1.Y * (1 - control) + v2.Y * control
                            )
                    );
            }
        }
    }
}
