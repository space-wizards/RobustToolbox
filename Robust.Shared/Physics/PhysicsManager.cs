using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics
{
    /// <inheritdoc />
    public class PhysicsManager : IPhysicsManager
    {
#pragma warning disable 649
        [Dependency] private readonly IPhysicsManager _physicsManager;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager;
        [Dependency] private readonly IMapManager _mapManager;
#pragma warning restore 649

        private const float Epsilon = 1.0e-6f;

        private readonly List<IPhysBody> _bodies = new List<IPhysBody>();
        private readonly List<IPhysBody> _bodyEnumCache = new List<IPhysBody>();
        private readonly List<IPhysBody> _results = new List<IPhysBody>();

        /// <summary>
        ///     returns true if collider intersects a physBody under management. Does not trigger Bump.
        /// </summary>
        /// <param name="collider">Rectangle to check for collision</param>
        /// <param name="map">Map ID to filter</param>
        /// <returns></returns>
        public bool IsColliding(Box2 collider, MapId map)
        {
            foreach (var body in _bodies)
            {
                if (!body.CollisionEnabled || body.CollisionLayer == 0x0)
                    continue;

                if (body.MapID == map &&
                    body.IsHardCollidable &&
                    body.WorldAABB.Intersects(collider))
                    return true;
            }

            return false;
        }

        /// <summary>
        ///     returns true if collider intersects a physBody under management and calls Bump.
        /// </summary>
        /// <param name="entity">Rectangle to check for collision</param>
        /// <param name="offset"></param>
        /// <param name="bump"></param>
        /// <returns></returns>
        public bool TryCollide(IEntity entity, Vector2 offset, bool bump = true)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var collidable = (IPhysBody) entity.GetComponent<ICollidableComponent>();

            if (!collidable.CollisionEnabled || collidable.CollisionLayer == 0x0)
                return false;

            var colliderAABB = collidable.WorldAABB;
            if (offset.LengthSquared > 0)
            {
                colliderAABB = colliderAABB.Translated(offset);
            }

            // Test this physBody against every other one.
            _results.Clear();
            DoCollisionTest(collidable, colliderAABB, _results);

            //See if our collision will be overridden by a component
            var collisionmodifiers = entity.GetAllComponents<ICollideSpecial>().ToList();
            var collidedwith = new List<IEntity>();

            var collided = TestSpecialCollisionAndBump(entity, bump, collisionmodifiers, collidedwith);

            collidable.Bump(collidedwith);

            //TODO: This needs multi-grid support.
            return collided;
        }

        private bool TestSpecialCollisionAndBump(IEntity entity, bool bump, List<ICollideSpecial> collisionmodifiers, List<IEntity> collidedwith)
        {
            //try all of the AABBs against the target rect.
            var collided = false;
            foreach (var otherCollidable in _results)
            {
                if (!otherCollidable.IsHardCollidable)
                {
                    continue;
                }

                //Provides component level overrides for collision behavior based on the entity we are trying to collide with
                var preventcollision = false;
                foreach (var mods in collisionmodifiers)
                {
                    preventcollision |= mods.PreventCollide(otherCollidable);
                }
                if (preventcollision)
                {
                    continue;
                }

                collided = true;

                if (!bump)
                {
                    continue;
                }

                otherCollidable.Bumped(entity);
                collidedwith.Add(otherCollidable.Owner);
            }

            return collided;
        }

        /// <summary>
        ///     Tests a physBody against every other registered physBody.
        /// </summary>
        /// <param name="physBody">Body being tested.</param>
        /// <param name="colliderAABB">The AABB of the physBody being tested. This can be IPhysBody.WorldAABB, or a modified version of it.</param>
        /// <param name="results">An empty list that the function stores all colliding bodies inside of.</param>
        internal void DoCollisionTest(IPhysBody physBody, Box2 colliderAABB, List<IPhysBody> results)
        {
            foreach (var body in GetCollidablesForLocation(colliderAABB))
            {
                if (!body.CollisionEnabled)
                {
                    continue;
                }

                if ((physBody.CollisionMask & body.CollisionLayer) == 0x0)
                {
                    continue;
                }

                if (physBody.MapID != body.MapID ||
                    physBody == body ||
                    !colliderAABB.Intersects(body.WorldAABB))
                {
                    continue;
                }

                results.Add(body);
            }
        }

        /// <summary>
        ///     Adds a physBody to the manager.
        /// </summary>
        /// <param name="physBody"></param>
        public void AddBody(IPhysBody physBody)
        {
            if (!_bodies.Contains(physBody))
                _bodies.Add(physBody);
            else
                Logger.WarningS("phys", $"PhysicsBody already registered! {physBody.Owner}");
        }

        /// <summary>
        ///     Removes a physBody from the manager
        /// </summary>
        /// <param name="physBody"></param>
        public void RemoveBody(IPhysBody physBody)
        {
            if (_bodies.Contains(physBody))
                _bodies.Remove(physBody);
            else
                Logger.WarningS("phys", $"Trying to remove unregistered PhysicsBody! {physBody.Owner}");
        }

        /// <inheritdoc />
        public void UpdateSimulation(TimeSpan deltaTime)
        {
            BuildCollisionGrid();

            _bodyEnumCache.Clear();
            _bodyEnumCache.AddRange(_bodies);

            // Collision callback modify the _bodies collection.
            foreach (var body in _bodyEnumCache)
            {
                if(body.DynamicBody == null || body.Disabled)
                    continue;

                HandleMovement(body.Owner, (float) deltaTime.TotalSeconds, _mapManager, _tileDefinitionManager, body.DynamicBody);
            }

            foreach (var body in _bodies)
            {
                if (body.DynamicBody == null || body.Disabled)
                    continue;

                DoMovement((float)deltaTime.TotalSeconds, body.DynamicBody, body.DynamicBody.Transform);
            }
        }

        /// <inheritdoc />
        public RayCastResults IntersectRay(Ray ray, float maxLength = 50, IEntity ignoredEnt = null)
        {
            IEntity entity = null;
            var hitPosition = Vector2.Zero;
            var minDist = maxLength;

            foreach (var body in _bodies)
            {
                if ((ray.CollisionMask & body.CollisionLayer) == 0x0)
                {
                    continue;
                }
                if (ray.Intersects(body.WorldAABB, out var dist, out var hitPos) && dist < minDist)
                {
                    if (!body.IsHardCollidable || ignoredEnt != null && ignoredEnt == body.Owner)
                        continue;

                    entity = body.Owner;
                    minDist = dist;
                    hitPosition = hitPos;
                }
            }

            if (entity != null)
                return new RayCastResults(minDist, hitPosition, entity);

            return default;
        }

        private Dictionary<Vector2, List<IPhysBody>> _collisionGrid = new Dictionary<Vector2, List<IPhysBody>>();
        private float _collisionGridResolution = 5;

        public void BuildCollisionGrid()
        {
            _collisionGrid.Clear();
            foreach (var body in _bodies)
            {
                var snappedLocation = SnapLocationToGrid(body.WorldAABB);
                if (!_collisionGrid.ContainsKey(snappedLocation))
                {
                    _collisionGrid[snappedLocation] = new List<IPhysBody>();
                }
                _collisionGrid[snappedLocation].Add(body);
            }
        }

        private Vector2 SnapLocationToGrid(Box2 worldAAAB)
        {
            var result = worldAAAB.Center;
            result /= _collisionGridResolution;
            result = new Vector2((float)Math.Floor(result.X), (float)Math.Floor(result.Y));
            return result;
        }

        private List<IPhysBody> GetCollidablesForLocation(Box2 location)
        {
            var snappedLocation = SnapLocationToGrid(location);
            var result = new List<IPhysBody>();
            for(int xOffset = -1; xOffset <= 1; xOffset++)
            {
                for (int yOffset = -1; yOffset <= 1; yOffset++)
                {
                    var offsetLocation = snappedLocation + new Vector2(xOffset, yOffset);
                    result.AddRange(_collisionGrid.ContainsKey(offsetLocation) ? _collisionGrid[offsetLocation] : Enumerable.Empty<IPhysBody>());
                }
            }
            return result;
        }

        private static void HandleMovement(IEntity entity, float frameTime, IMapManager mapManager, ITileDefinitionManager tileDefinitionManager, IPhysDynamicBody physicsComponent)
        {
            if (physicsComponent.DidMovementCalculations)
            {
                physicsComponent.DidMovementCalculations = false;
                return;
            }

            if (physicsComponent.AngularVelocity == 0 && physicsComponent.LinearVelocity == Vector2.Zero)
            {
                return;
            }
            var transform = physicsComponent.Transform;
            if (transform.Parent != null)
            {
                transform.Parent.Owner.SendMessage(transform, new RelayMovementEntityMessage(entity));
                physicsComponent.LinearVelocity = Vector2.Zero;
                return;
            }

            var velocityConsumers = physicsComponent.GetVelocityConsumers();
            var initialMovement = physicsComponent.LinearVelocity;
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
                    var movement = lowestMovement * physicsComponent.Mass / totalMass;
                    velocityConsumer.AngularVelocity = physicsComponent.AngularVelocity;
                    velocityConsumer.LinearVelocity = movement;
                    return CalculateMovement(velocityConsumer, frameTime, mapManager, tileDefinitionManager, velocityConsumer.Transform, velocityConsumer.Collidable) / frameTime;
                }).OrderBy(x=>x.LengthSquared).First();
                velocityConsumers = physicsComponent.GetVelocityConsumers();
            }
            while (velocityConsumers.Count != velocityConsumerCount);
            physicsComponent.ClearVelocityConsumers();

            velocityConsumers.ForEach(velocityConsumer =>
            {
                velocityConsumer.LinearVelocity = lowestMovement;
                velocityConsumer.DidMovementCalculations = true;
            });
            physicsComponent.DidMovementCalculations = false;
        }

        private static Vector2 CalculateMovement(IPhysDynamicBody dynamicBody, float frameTime, IMapManager mapManager, ITileDefinitionManager tileDefinitionManager, ITransformComponent transform, ICollidableComponent collider)
        {
            var movement = dynamicBody.LinearVelocity * frameTime;
            if (movement.LengthSquared <= Epsilon)
            {
                return Vector2.Zero;
            }

            //Check for collision
            if (collider != null)
            {
                var collided = collider.TryCollision(movement, true);

                if (collided)
                {
                    if (dynamicBody.EdgeSlide)
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

                if (movement != Vector2.Zero && collider.IsScrapingFloor)
                {
                    var grid = mapManager.GetGrid(transform.GridPosition.GridID);
                    var tile = grid.GetTileRef(transform.GridPosition);
                    var tileDef = tileDefinitionManager[tile.Tile.TypeId];
                    if (tileDef.Friction != 0)
                    {
                        movement -= movement * tileDef.Friction;
                        if (movement.LengthSquared <= dynamicBody.Mass * Epsilon / (1 - tileDef.Friction))
                        {
                            movement = Vector2.Zero;
                        }
                    }
                }
            }
            return movement;
        }

        private static void DoMovement(float frameTime, IPhysDynamicBody dynamicBody, ITransformComponent transform)
        {
            if (dynamicBody.LinearVelocity.LengthSquared < Epsilon && dynamicBody.AngularVelocity < Epsilon)
                return;

            float angImpulse = 0;
            if (dynamicBody.AngularVelocity > Epsilon)
            {
                angImpulse = dynamicBody.AngularVelocity * frameTime;
            }

            transform.LocalRotation += angImpulse;
            transform.WorldPosition += dynamicBody.LinearVelocity * frameTime;
        }
    }
}
