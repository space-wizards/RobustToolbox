using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using ClientInterfaces;
using ClientInterfaces.Collision;

namespace ClientServices.Collision
{
    //Its the bucket list!
    /// <summary>
    /// Here's what is happening here. Each collidable AABB added to this manager gets tossed into 
    /// a "bucket". The buckets are subdivisions of the world space in 256-unit blocks. 
    /// </summary>
    public class CollisionManager : ICollisionManager
    {
        private const int BucketSize = 256;

        private readonly Dictionary<Point, int> _bucketIndex; //Indexed in 256-pixel blocks - 0 = 0, 1 = 256, 2 = 512 etc
        private readonly Dictionary<int, CollidableBucket> _buckets; // each bucket represents a 256x256 block of pixelspace
        private readonly List<CollidableAABB> _aabbs;

        private int _lastIndex;

        /// <summary>
        /// Constructor
        /// </summary>
        public CollisionManager()
        {
            _bucketIndex = new Dictionary<Point, int>();
            _buckets = new Dictionary<int, CollidableBucket>();
            _aabbs = new List<CollidableAABB>();
        }

        #region ICollisionManager members
        /// <summary>
        /// returns true if collider intersects a collidable under management.
        /// </summary>
        /// <param name="collider">Rectangle to check for collision</param>
        /// <param name="suppressBump">If true, don't run the Bump() func on the objects that are hit</param>
        /// <returns></returns>
        public bool IsColliding(RectangleF collider, bool suppressBump = false)
        {
            PointF[] points = {   new PointF(collider.Left, collider.Top),
                                  new PointF(collider.Right, collider.Top),
                                  new PointF(collider.Right, collider.Bottom),
                                  new PointF(collider.Left, collider.Bottom)
                              };
            
            //Get the buckets that correspond to the collider's points.
            var buckets = points.Select(GetBucket).Distinct().ToList();

            //Get all of the points
            var cpoints = new List<CollidablePoint>();
            foreach (var bucket in buckets)
            {
                cpoints.AddRange(bucket.GetPoints());
            }

            //Expand points to distinct AABBs
            var aabBs = (cpoints.Select(cp => cp.ParentAABB)).Distinct().ToList();

            //try all of the AABBs against the target rect.
            var collided = false;
            foreach (var aabb in aabBs.Where(aabb => aabb.Collidable.AABB.IntersectsWith(collider)))
            {
                if (aabb.IsHardCollider) //If the collider is supposed to prevent movement
                {
                    collided = true;
                }

                if (!suppressBump)
                    aabb.Collidable.Bump();
            }
            return collided;
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
            _aabbs.Add(c);
        }

        /// <summary>
        /// Removes a collidable from the manager
        /// </summary>
        /// <param name="collidable"></param>
        public void RemoveCollidable(ICollidable collidable)
        {
            var ourAABB = _aabbs.FirstOrDefault(a => a.Collidable == collidable);

            foreach (var p in ourAABB.Points)
            {
                RemovePoint(p);
            }
            _aabbs.Remove(ourAABB);
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
            var b = GetBucket(point.Coordinates);
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
        private CollidableBucket GetBucket(Point coordinate)
        {
            return _bucketIndex.ContainsKey(GetBucketCoordinate(coordinate)) ? _buckets[_bucketIndex[GetBucketCoordinate(coordinate)]] : CreateBucket(GetBucketCoordinate(coordinate));
        }

        /// <summary>
        /// Gets a bucket given a pointF coordinate
        /// </summary>
        /// <param name="coordinate"></param>
        /// <returns></returns>
        private CollidableBucket GetBucket(PointF coordinate)
        {
            return GetBucket(GetBucketCoordinate(coordinate));
        }

        private static Point GetBucketCoordinate(PointF coordinate)
        {
            var x = (int)Math.Floor(coordinate.X / BucketSize);
            var y = (int)Math.Floor(coordinate.Y / BucketSize);
            return new Point(x, y);
        }

        private static Point GetBucketCoordinate(Point coordinate)
        {
            var x = (int)Math.Floor((decimal)coordinate.X / BucketSize);
            var y = (int)Math.Floor((decimal)coordinate.Y / BucketSize);
            return new Point(x, y);
        }

        private CollidableBucket CreateBucket(Point coordinate)
        {
            if (_bucketIndex.ContainsKey(coordinate))
                return _buckets[_bucketIndex[GetBucketCoordinate(coordinate)]];

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
        private int _index;
        private Point _coordinates;
        private readonly List<CollidablePoint> _points;

        public CollidableBucket(int index, Point coordinates)
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
            if(_points.Contains(point))
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
        public CollidablePointIndex Index;
        public PointF Coordinates;
        public CollidableAABB ParentAABB;
        public CollidablePoint(CollidablePointIndex index, PointF coordinates, CollidableAABB parentAABB)
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
        public CollidablePoint[] Points;
        public bool IsHardCollider;

        public CollidableAABB(ICollidable collidable)
        {
            Collidable = collidable;
            IsHardCollider = Collidable.IsHardCollidable;
            Points = new CollidablePoint[4];
            var top = Collidable.AABB.Top;
            var bottom = Collidable.AABB.Bottom;
            var left = Collidable.AABB.Left;
            var right = Collidable.AABB.Right;
            Points[0] = new CollidablePoint(CollidablePointIndex.TopLeft, new PointF(left, top), this);
            Points[1] = new CollidablePoint(CollidablePointIndex.TopRight, new PointF(right, top), this);
            Points[2] = new CollidablePoint(CollidablePointIndex.BottomRight, new PointF(right, bottom), this);
            Points[3] = new CollidablePoint(CollidablePointIndex.BottomLeft, new PointF(left, bottom), this);
        }
    }
}
