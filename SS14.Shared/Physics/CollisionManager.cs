using System;
using System.Collections.Generic;
using System.Linq;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Physics;
using SS14.Shared.Log;
using SS14.Shared.Map;
using SS14.Shared.Maths;

namespace SS14.Shared.Physics
{
    //Its the bucket list!
    /// <summary>
    ///     Here's what is happening here. Each collidable AABB added to this manager gets tossed into
    ///     a "bucket". The buckets are subdivisions of the world space in 256-unit blocks.
    /// </summary>
    public class CollisionManager : ICollisionManager
    {
        private const int BucketSize = 256;
        private readonly Dictionary<ICollidable, (CollidableAABB aabb, IEntity owner)> _aabbs;

        private readonly Dictionary<Vector2i, int> _bucketIndex;
        //Indexed in 256-pixel blocks - 0 = 0, 1 = 256, 2 = 512 etc

        private readonly Dictionary<int, CollidableBucket> _buckets;
        // each bucket represents a 256x256 block of pixelspace

        private int _lastIndex;

        private readonly Dictionary<ICollidable, PhysicsBody> _bodies = new Dictionary<ICollidable, PhysicsBody>();

        private readonly List<ICollidable> _results = new List<ICollidable>();

        /// <summary>
        ///     Constructor
        /// </summary>
        public CollisionManager()
        {
            _bucketIndex = new Dictionary<Vector2i, int>();
            _buckets = new Dictionary<int, CollidableBucket>();
            _aabbs = new Dictionary<ICollidable, (CollidableAABB aabb, IEntity owner)>();
        }

        /// <summary>
        ///     returns true if collider intersects a collidable under management. Does not trigger Bump.
        /// </summary>
        /// <param name="collider">Rectangle to check for collision</param>
        /// <param name="map">Map ID to filter</param>
        /// <returns></returns>
        public bool IsColliding(Box2 collider, MapId map)
        {
            foreach (var kvBody in _bodies)
            {
                var collidable = kvBody.Key;

                if (collidable.MapID == map &&
                    collidable.IsHardCollidable &&
                    collidable.WorldAABB.Intersects(collider))
                    return true;
            }

            return false;
        }

        /// <summary>
        ///     returns true if collider intersects a collidable under management and calls Bump.
        /// </summary>
        /// <param name="entity">Rectangle to check for collision</param>
        /// <param name="offset"></param>
        /// <param name="bump"></param>
        /// <returns></returns>
        public bool TryCollide(IEntity entity, Vector2 offset, bool bump = true)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var collidable = (ICollidable) entity.GetComponent<ICollidableComponent>();

            var colliderAABB = collidable.WorldAABB;
            if (offset.LengthSquared > 0)
            {
                colliderAABB = colliderAABB.Translated(offset);
            }

            // Test this collidable against every other one.
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
                //Provides component level overrides for collision behavior based on the entity we are trying to collide with
                var preventcollision = false;

                foreach (var mods in collisionmodifiers)
                    preventcollision |= mods.PreventCollide(otherCollidable);

                if (preventcollision) //We were prevented, bail
                    continue;

                if (!otherCollidable.IsHardCollidable)
                    continue;

                collided = true;

                if (!bump)
                    continue;

                otherCollidable.Bumped(entity);
                collidedwith.Add(otherCollidable.Owner);
            }

            return collided;
        }

        /// <summary>
        ///     Tests a collidable against every other registered collidable.
        /// </summary>
        /// <param name="collidable">Collidable being tested.</param>
        /// <param name="colliderAABB">The AABB of the collidable being tested. This can be ICollidable.WorldAABB, or a modified version of it.</param>
        /// <param name="results">An empty list that the function stores all colliding bodies inside of.</param>
        public void DoCollisionTest(ICollidable collidable, Box2 colliderAABB, List<ICollidable> results)
        {
            foreach (var kvBody in _bodies)
            {
                var other = kvBody.Key;

                if (collidable.MapID != other.MapID ||
                    collidable.IsHardCollidable == false ||
                    collidable == other ||
                    !colliderAABB.Intersects(other.WorldAABB))
                    continue;

                results.Add(kvBody.Key);
            }
        }

        /// <summary>
        ///     Adds a collidable to the manager.
        /// </summary>
        /// <param name="collidable"></param>
        public void AddCollidable(ICollidable collidable)
        {
            var body = new PhysicsBody(collidable);

            if (!_bodies.ContainsKey(collidable))
                _bodies.Add(collidable, body);
            else
                Logger.WarningS("phys", $"Collidable already registered! {collidable.Owner}");

            



            if (_aabbs.ContainsKey(collidable))
            {
                // TODO: throw an exception instead.
                // There's too much buggy code in the client that I can't be bothered to fix,
                // so it'd crash reliably.
                UpdateCollidable(collidable);
                return;
            }
            var c = new CollidableAABB(collidable);
            foreach (var p in c.Points)
            {
                AddPoint(p);
            }
            var comp = collidable as IComponent;
            _aabbs.Add(collidable, (aabb: c, owner: comp?.Owner));
        }

        /// <summary>
        ///     Removes a collidable from the manager
        /// </summary>
        /// <param name="collidable"></param>
        public void RemoveCollidable(ICollidable collidable)
        {
            if (_bodies.ContainsKey(collidable))
                _bodies.Remove(collidable);
            else
                Logger.WarningS("phys", $"Trying to remove unregistered collidable! {collidable.Owner}");




            var ourAABB = _aabbs[collidable].aabb;

            foreach (var p in ourAABB.Points)
            {
                RemovePoint(p);
            }
            _aabbs.Remove(collidable);
        }

        /// <summary>
        ///     Updates the collidable in the manager.
        /// </summary>
        /// <param name="collidable"></param>
        [Obsolete("What EventArgs is this?")]
        public void UpdateCollidable(ICollidable collidable)
        {
            RemoveCollidable(collidable);
            AddCollidable(collidable);
        }

        /// <summary>
        ///     Adds an AABB point to a buckets
        /// </summary>
        /// <param name="point"></param>
        [Obsolete("Point BS")]
        private void AddPoint(CollidablePoint point)
        {
            var b = GetBucket(point.Coordinates);
            b.AddPoint(point);
        }

        /// <summary>
        ///     Removes an AABB point from a bucket
        /// </summary>
        /// <param name="point"></param>
        [Obsolete("Point BS")]
        private void RemovePoint(CollidablePoint point)
        {
            var b = GetBucket(point.Coordinates);
            b.RemovePoint(point);
        }

        /// <inheritdoc />
        public RayCastResults IntersectRay(Ray ray, float maxLength = 50, IEntity ignoredEnt = null)
        {
            IEntity entity = null;
            var hitPosition = Vector2.Zero;
            var minDist = maxLength;

            foreach (var kvBody in _bodies)
            {
                var body = kvBody.Key;
                if (ray.Intersects(body.WorldAABB, out var dist, out var hitPos) && dist < minDist)
                {
                    if (ignoredEnt != null && ignoredEnt == body.Owner)
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

        /// <summary>
        ///     Gets a bucket given a point coordinate
        /// </summary>
        /// <param name="coordinate"></param>
        /// <returns></returns>
        [Obsolete("Bucket BS")]
        private CollidableBucket GetBucket(Vector2 coordinate)
        {
            var key = GetBucketCoordinate(coordinate);
            return _bucketIndex.ContainsKey(key)
                ? _buckets[_bucketIndex[key]]
                : CreateBucket(key);
        }

        [Obsolete("Bucket BS")]
        private static Vector2i GetBucketCoordinate(Vector2 coordinate)
        {
            var x = (int)Math.Floor(coordinate.X / BucketSize);
            var y = (int)Math.Floor(coordinate.Y / BucketSize);
            return new Vector2i(x, y);
        }

        [Obsolete("Bucket BS")]
        private CollidableBucket CreateBucket(Vector2i coordinate)
        {
            if (_bucketIndex.ContainsKey(coordinate))
                return _buckets[_bucketIndex[coordinate]];

            var b = new CollidableBucket(_lastIndex, coordinate);
            _buckets.Add(_lastIndex, b);
            _bucketIndex.Add(coordinate, _lastIndex);
            _lastIndex++;
            return b;
        }
    }
}
