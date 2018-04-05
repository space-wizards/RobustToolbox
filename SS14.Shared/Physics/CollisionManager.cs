using System;
using System.Collections.Generic;
using System.Linq;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Physics;
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
        /// <returns></returns>
        public bool IsColliding(Box2 collider)
        {
            Vector2[] points =
            {
                new Vector2(collider.Left, collider.Top),
                new Vector2(collider.Right, collider.Top),
                new Vector2(collider.Right, collider.Bottom),
                new Vector2(collider.Left, collider.Bottom)
            };

            //Get the buckets that correspond to the collider's points.
            var buckets = points.Select(GetBucket).Distinct().ToList();

            //Get all of the points
            var cPoints = new List<CollidablePoint>();
            foreach (var bucket in buckets)
                cPoints.AddRange(bucket.GetPoints());

            //Expand points to distinct AABBs
            var aabBs = cPoints.Select(cp => cp.ParentAABB).Distinct().ToList();

            //try all of the AABBs against the target rect.
            var collided = false;
            foreach (var aabb in aabBs.Where(aabb => aabb.Collidable.AABB.Intersects(collider)))
                if (aabb.Collidable.IsHardCollidable) //If the collider is supposed to prevent movement
                    collided = true;

            //TODO: This needs multi-grid support.
            return collided;
        }

        /// <summary>
        ///     returns true if collider intersects a collidable under management and calls Bump.
        /// </summary>
        /// <param name="collider">Rectangle to check for collision</param>
        /// <returns></returns>
        public bool TryCollide(IEntity entity)
        {
            return TryCollide(entity, new Vector2());
        }

        /// <summary>
        ///     returns true if collider intersects a collidable under management and calls Bump.
        /// </summary>
        /// <param name="collider">Rectangle to check for collision</param>
        /// <returns></returns>
        public bool TryCollide(IEntity entity, Vector2 offset, bool bump = true)
        {
            var collider = entity.GetComponent<ICollidableComponent>();
            if (collider == null) return false;

            var colliderAABB = collider.WorldAABB;
            if (offset.LengthSquared > 0)
            {
                colliderAABB = colliderAABB.Translated(offset);
            }

            Vector2[] points =
            {
                new Vector2(colliderAABB.Left, colliderAABB.Top),
                new Vector2(colliderAABB.Right, colliderAABB.Top),
                new Vector2(colliderAABB.Right, colliderAABB.Bottom),
                new Vector2(colliderAABB.Left, colliderAABB.Bottom)
            };

            var bounds = points
                    .Select(GetBucket) // Get the buckets that correspond to the collider's points.
                    .Distinct()
                    .SelectMany(b => b.GetPoints()) // Get all of the points
                    .Select(p => p.ParentAABB) // Expand points to distinct AABBs
                    .Distinct()
                    .Where(aabb => aabb.Collidable != collider &&
                           aabb.Collidable.WorldAABB.Intersects(colliderAABB) &&
                           aabb.Collidable.MapID == collider.MapID); //try all of the AABBs against the target rect.

            //try all of the AABBs against the target rect.
            var collided = false;
            foreach (var aabb in bounds)
            {
                if (aabb.Collidable.IsHardCollidable) //If the collider is supposed to prevent movement
                    collided = true;

                if (bump) aabb.Collidable.Bump(entity);
            }

            //TODO: This needs multi-grid support.
            return collided;
        }

        /// <summary>
        ///     Adds a collidable to the manager.
        /// </summary>
        /// <param name="collidable"></param>
        public void AddCollidable(ICollidable collidable)
        {
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
        public void UpdateCollidable(ICollidable collidable)
        {
            RemoveCollidable(collidable);
            AddCollidable(collidable);
        }

        /// <summary>
        ///     Adds an AABB point to a buckets
        /// </summary>
        /// <param name="point"></param>
        private void AddPoint(CollidablePoint point)
        {
            var b = GetBucket(point.Coordinates);
            b.AddPoint(point);
        }

        /// <summary>
        ///     Removes an AABB point from a bucket
        /// </summary>
        /// <param name="point"></param>
        private void RemovePoint(CollidablePoint point)
        {
            var b = GetBucket(point.Coordinates);
            b.RemovePoint(point);
        }

        public RayCastResults IntersectRay(Ray ray, float maxLength = 50, IEntity entityignore = null)
        {
            var closestResults = new RayCastResults(float.PositiveInfinity, Vector2.Zero, null);
            var minDist = float.PositiveInfinity;
            var localBounds = new Box2(0, BucketSize, BucketSize, 0);

            // for each bucket index
            foreach (var kvIndices in _bucketIndex)
            {
                var worldBounds = localBounds.Translated(kvIndices.Key * BucketSize);

                // check if ray intersects the bucket AABB
                if (ray.Intersects(worldBounds, out var dist, out _))
                {
                    // bucket is too far away
                    if(dist > maxLength)
                        continue;

                    // get the object it intersected in the bucket
                    var bucket = _buckets[kvIndices.Value];
                    if (TryGetClosestIntersect(ray, bucket, out var results, entityignore))
                    {
                        if (results.Distance < minDist)
                        {
                            minDist = results.Distance;
                            closestResults = results;
                        }
                    }
                }
            }
            
            return closestResults;
        }

        /// <summary>
        ///     Return the closest object, inside a bucket, to the ray origin that was intersected (if any).
        /// </summary>
        private static bool TryGetClosestIntersect(Ray ray, CollidableBucket bucket, out RayCastResults results, IEntity entityignore = null)
        {
            IEntity entity = null;
            var hitPosition = Vector2.Zero;
            var minDist = float.PositiveInfinity;

            foreach (var collidablePoint in bucket.GetPoints()) // *goes to kitchen to freshen up his drink...*
            {
                var worldAABB = collidablePoint.ParentAABB.Collidable.WorldAABB;

                if (ray.Intersects(worldAABB, out var dist, out var hitPos) && !(dist > minDist))
                {
                    if (entityignore != null && entityignore == collidablePoint.ParentAABB.Collidable.Owner)
                    {
                        continue;
                    }

                    entity = collidablePoint.ParentAABB.Collidable.Owner;
                    minDist = dist;
                    hitPosition = hitPos;
                }
            }

            if (minDist < float.PositiveInfinity)
            {
                results = new RayCastResults(minDist, hitPosition, entity);
                return true;
            }

            results = default(RayCastResults);
            return false;
        }

        /// <summary>
        ///     Gets a bucket given a point coordinate
        /// </summary>
        /// <param name="coordinate"></param>
        /// <returns></returns>
        private CollidableBucket GetBucket(Vector2 coordinate)
        {
            var key = GetBucketCoordinate(coordinate);
            return _bucketIndex.ContainsKey(key)
                ? _buckets[_bucketIndex[key]]
                : CreateBucket(key);
        }

        private static Vector2i GetBucketCoordinate(Vector2 coordinate)
        {
            var x = (int)Math.Floor(coordinate.X / BucketSize);
            var y = (int)Math.Floor(coordinate.Y / BucketSize);
            return new Vector2i(x, y);
        }

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
