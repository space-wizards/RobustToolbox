using SFML.Graphics;
using SFML.System;
using SS14.Client.GameObjects;
using SS14.Client.Interfaces.Collision;
using SS14.Client.Interfaces.Map;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SS14.Client.Collision
{
    //Its the bucket list!
    /// <summary>
    /// Here's what is happening here. Each collidable AABB added to this manager gets tossed into
    /// a "bucket". The buckets are subdivisions of the world space in 256-unit blocks.
    /// </summary>
    [IoCTarget]
    public class CollisionManager : ICollisionManager
    {
        private const int BucketSize = 256;
        private readonly Dictionary<CollidableAABB, Entity> _aabbs;

        private readonly Dictionary<Vector2i, int> _bucketIndex;
        //Indexed in 256-pixel blocks - 0 = 0, 1 = 256, 2 = 512 etc

        private readonly Dictionary<int, CollidableBucket> _buckets;
        // each bucket represents a 256x256 block of pixelspace

        private int _lastIndex;

        /// <summary>
        /// Constructor
        /// </summary>
        public CollisionManager()
        {
            _bucketIndex = new Dictionary<Vector2i, int>();
            _buckets = new Dictionary<int, CollidableBucket>();
            _aabbs = new Dictionary<CollidableAABB, Entity>();
        }

        #region ICollisionManager members

        /// <summary>
        /// returns true if collider intersects a collidable under management. Does not trigger Bump.
        /// </summary>
        /// <param name="collider">Rectangle to check for collision</param>
        /// <returns></returns>
        public bool IsColliding(FloatRect collider)
        {
            Vector2f[] points =
                {
                    new Vector2f(collider.Left, collider.Top),
                    new Vector2f(collider.Right(), collider.Top),
                    new Vector2f(collider.Right(), collider.Bottom()),
                    new Vector2f(collider.Left, collider.Bottom())
                };

            //Get the buckets that correspond to the collider's points.
            List<CollidableBucket> buckets = points.Select(GetBucket).Distinct().ToList();

            //Get all of the points
            var cpoints = new List<CollidablePoint>();
            foreach (CollidableBucket bucket in buckets)
            {
                cpoints.AddRange(bucket.GetPoints());
            }

            //Expand points to distinct AABBs
            List<CollidableAABB> aabBs = (cpoints.Select(cp => cp.ParentAABB)).Distinct().ToList();

            //try all of the AABBs against the target rect.
            bool collided = false;
            foreach (CollidableAABB aabb in aabBs.Where(aabb => aabb.Collidable.AABB.Intersects(collider)))
            {
                if (aabb.IsHardCollider) //If the collider is supposed to prevent movement
                {
                    collided = true;
                }
            }

            return collided || IoCManager.Resolve<IMapManager>().GetTilesIntersecting(collider, true).Any(t => t.Tile.TileDef.IsCollidable);
        }

        /// <summary>
        /// returns true if collider intersects a collidable under management and calls Bump.
        /// </summary>
        /// <param name="collider">Rectangle to check for collision</param>
        /// <returns></returns>
        public bool TryCollide(Entity entity)
        {
            return TryCollide(entity, new Vector2f());
        }

        /// <summary>
        /// returns true if collider intersects a collidable under management and calls Bump.
        /// </summary>
        /// <param name="collider">Rectangle to check for collision</param>
        /// <returns></returns>
        public bool TryCollide(Entity entity, Vector2f offset, bool bump = true)
        {
            var collider = (ColliderComponent)entity.GetComponent(ComponentFamily.Collider);
            if (collider == null) return false;

            var ColliderAABB = collider.WorldAABB;
            if (offset.LengthSquared() > 0)
            {
                ColliderAABB.Left += offset.X;
                ColliderAABB.Top += offset.Y;
            }

            Vector2f[] points =
                {
                    new Vector2f(ColliderAABB.Left, ColliderAABB.Top),
                    new Vector2f(ColliderAABB.Right(), ColliderAABB.Top),
                    new Vector2f(ColliderAABB.Right(), ColliderAABB.Bottom()),
                    new Vector2f(ColliderAABB.Left, ColliderAABB.Bottom())
                };

            var aabbs =
                points
                .Select(GetBucket) // Get the buckets that correspond to the collider's points.
                .Distinct()
                .SelectMany(b => b.GetPoints()) // Get all of the points
                .Select(p => p.ParentAABB) // Expand points to distinct AABBs
                .Distinct()
                .Where(aabb => aabb.Collidable.AABB.Intersects(ColliderAABB)); //try all of the AABBs against the target rect.

            //try all of the AABBs against the target rect.
            bool collided = false;
            foreach (var aabb in aabbs)
            {
                if (aabb.IsHardCollider) //If the collider is supposed to prevent movement
                {
                    collided = true;
                }

                if(bump) aabb.Collidable.Bump(entity);
            }
            return collided || IoCManager.Resolve<IMapManager>().GetTilesIntersecting(ColliderAABB, true).Any(t => t.Tile.TileDef.IsCollidable);
        }


        /// <summary>
        /// Adds a collidable to the manager.
        /// </summary>
        /// <param name="collidable"></param>
        public void AddCollidable(ICollidable collidable)
        {
            var c = new CollidableAABB(collidable);
            foreach (CollidablePoint p in c.Points)
            {
                AddPoint(p);
            }
            if (collidable is IComponent)
            {
                var baseComp = collidable as Component;
                _aabbs.Add(c, baseComp.Owner);
            }
            else
                _aabbs.Add(c, null);
        }

        /// <summary>
        /// Removes a collidable from the manager
        /// </summary>
        /// <param name="collidable"></param>
        public void RemoveCollidable(ICollidable collidable)
        {
            KeyValuePair<CollidableAABB, Entity> ourAABB = _aabbs.FirstOrDefault(a => a.Key.Collidable == collidable);

            if (ourAABB.Key.Collidable == null)
                return;

            foreach (CollidablePoint p in ourAABB.Key.Points)
            {
                RemovePoint(p);
            }
            _aabbs.Remove(ourAABB.Key);
        }

        /// <summary>
        /// Updates the collidable in the manager.
        /// </summary>
        /// <param name="collidable"></param>
        public void UpdateCollidable(ICollidable collidable)
        {
            RemoveCollidable(collidable);
            AddCollidable(collidable);
        }

        #endregion

        /// <summary>
        /// Adds an AABB point to a buckets
        /// </summary>
        /// <param name="point"></param>
        private void AddPoint(CollidablePoint point)
        {
            CollidableBucket b = GetBucket(point.Coordinates);
            b.AddPoint(point);
        }

        /// <summary>
        /// Removes an AABB point from a bucket
        /// </summary>
        /// <param name="point"></param>
        private void RemovePoint(CollidablePoint point)
        {
            CollidableBucket b = GetBucket(point.Coordinates);
            b.RemovePoint(point);
        }

        /// <summary>
        /// Gets a bucket given a point coordinate
        /// </summary>
        /// <param name="coordinate"></param>
        /// <returns></returns>
        private CollidableBucket GetBucket(Vector2f coordinate)
        {
            var key = GetBucketCoordinate(coordinate);
            return _bucketIndex.ContainsKey(key)
                       ? _buckets[_bucketIndex[key]]
                       : CreateBucket(key);
        }

        private static Vector2i GetBucketCoordinate(Vector2f coordinate)
        {
            var x = (int) Math.Floor(coordinate.X/BucketSize);
            var y = (int) Math.Floor(coordinate.Y/BucketSize);
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

    /// <summary>
    /// This class holds points of collision AABBs. It represents a square in the world.
    /// </summary>
    internal class CollidableBucket
    {
        private readonly List<CollidablePoint> _points;
        private Vector2i _coordinates;
        private int _index;

        public CollidableBucket(int index, Vector2i coordinates)
        {
            _index = index;
            _coordinates = coordinates;
            _points = new List<CollidablePoint>();
        }

        /// <summary>
        /// Adds a CollidablePoint to this bucket
        /// </summary>
        /// <param name="point"></param>
        public void AddPoint(CollidablePoint point)
        {
            _points.Add(point);
        }

        /// <summary>
        /// Removes a CollidablePoint from this bucket, if it exists.
        /// </summary>
        /// <param name="point"></param>
        public void RemovePoint(CollidablePoint point)
        {
            if (_points.Contains(point))
                _points.Remove(point);
        }

        public IEnumerable<CollidablePoint> GetPoints()
        {
            return _points;
        }
    }

    /// <summary>
    /// This represents a point of a collision AABB
    /// </summary>
    internal struct CollidablePoint
    {
        public Vector2f Coordinates;
        public CollidablePointIndex Index;
        public CollidableAABB ParentAABB;

        public CollidablePoint(CollidablePointIndex index, Vector2f coordinates, CollidableAABB parentAABB)
        {
            Index = index;
            Coordinates = coordinates;
            ParentAABB = parentAABB;
        }
    }

    internal enum CollidablePointIndex
    {
        TopLeft,
        TopRight,
        BottomRight,
        BottomLeft
    }

    /// <summary>
    /// This is our representation of an AABB.
    /// </summary>
    internal struct CollidableAABB
    {
        public ICollidable Collidable;
        public bool IsHardCollider;
        public CollidablePoint[] Points;

        public CollidableAABB(ICollidable collidable)
        {
            Collidable = collidable;
            IsHardCollider = Collidable.IsHardCollidable;
            Points = new CollidablePoint[4];
            float top = Collidable.AABB.Top;
            float bottom = Collidable.AABB.Bottom();
            float left = Collidable.AABB.Left;
            float right = Collidable.AABB.Right();
            Points[0] = new CollidablePoint(CollidablePointIndex.TopLeft, new Vector2f(left, top), this);
            Points[1] = new CollidablePoint(CollidablePointIndex.TopRight, new Vector2f(right, top), this);
            Points[2] = new CollidablePoint(CollidablePointIndex.BottomRight, new Vector2f(right, bottom), this);
            Points[3] = new CollidablePoint(CollidablePointIndex.BottomLeft, new Vector2f(left, bottom), this);
        }
    }
}
