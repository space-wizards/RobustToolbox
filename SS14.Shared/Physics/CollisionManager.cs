using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Interfaces.Physics;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using SS14.Shared.Utility;
using Vector2i = SS14.Shared.Maths.Vector2i;

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
        private readonly Dictionary<CollidableAABB, IEntity> _aabbs;

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
            _aabbs = new Dictionary<CollidableAABB, IEntity>();
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
                if (aabb.IsHardCollider) //If the collider is supposed to prevent movement
                    collided = true;

            //TODO: This needs multi-grid support.
            return collided || IoCManager.Resolve<IMapManager>().GetDefaultGrid().GetTilesIntersecting(collider).Any(t => t.TileDef.IsCollidable);
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
                colliderAABB.Left += offset.X;
                colliderAABB.Top += offset.Y;
            }

            Vector2[] points =
            {
                new Vector2(colliderAABB.Left, colliderAABB.Top),
                new Vector2(colliderAABB.Right, colliderAABB.Top),
                new Vector2(colliderAABB.Right, colliderAABB.Bottom),
                new Vector2(colliderAABB.Left, colliderAABB.Bottom)
            };

            var bounds = 
                points
                    .Select(GetBucket) // Get the buckets that correspond to the collider's points.
                    .Distinct()
                    .SelectMany(b => b.GetPoints()) // Get all of the points
                    .Select(p => p.ParentAABB) // Expand points to distinct AABBs
                    .Distinct()
                    .Where(aabb => aabb.Collidable.WorldAABB != collider.WorldAABB && aabb.Collidable.WorldAABB.Intersects(colliderAABB)); //try all of the AABBs against the target rect.

            //try all of the AABBs against the target rect.
            var collided = false;
            foreach (var aabb in bounds)
            {
                if (aabb.IsHardCollider) //If the collider is supposed to prevent movement
                    collided = true;

                if (bump) aabb.Collidable.Bump(entity);
            }

            //TODO: This needs multi-grid support.
            return collided || IoCManager.Resolve<IMapManager>().GetDefaultGrid().GetTilesIntersecting(colliderAABB).Any(t => t.TileDef.IsCollidable);
        }

        /// <summary>
        ///     Adds a collidable to the manager.
        /// </summary>
        /// <param name="collidable"></param>
        public void AddCollidable(ICollidable collidable)
        {
            var c = new CollidableAABB(collidable);
            foreach (var p in c.Points)
                AddPoint(p);
            if (collidable is IComponent comp)
                _aabbs.Add(c, comp.Owner);
            else
                _aabbs.Add(c, null);
        }

        /// <summary>
        ///     Removes a collidable from the manager
        /// </summary>
        /// <param name="collidable"></param>
        public void RemoveCollidable(ICollidable collidable)
        {
            var ourAABB = _aabbs.FirstOrDefault(a => a.Key.Collidable == collidable);

            if (ourAABB.Key.Collidable == null)
                return;

            foreach (var p in ourAABB.Key.Points)
                RemovePoint(p);
            _aabbs.Remove(ourAABB.Key);
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
            var x = (int) Math.Floor(coordinate.X / BucketSize);
            var y = (int) Math.Floor(coordinate.Y / BucketSize);
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
