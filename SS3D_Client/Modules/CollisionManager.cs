using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using ClientInterfaces;
using SS3D_shared;

namespace SS3D.Modules
{
    //Its the bucket list!
    /// <summary>
    /// Here's what is happening here. Each collidable AABB added to this manager gets tossed into 
    /// a "bucket". The buckets are subdivisions of the world space in 256-unit blocks. 
    /// </summary>
    public class CollisionManager : ICollisionManager, IService
    {
        private Dictionary<Point, int> bucketIndex; //Indexed in 256-pixel blocks - 0 = 0, 1 = 256, 2 = 512 etc
        private Dictionary<int, CollidableBucket> buckets; // each bucket represents a 256x256 block of pixelspace
        private List<CollidableAABB> aabbs;
        private ClientServiceType serviceType = ClientServiceType.CollisionManager;

        private int lastIndex = 0;

        private const int bucketSize = 256;

        /// <summary>
        /// Constructor
        /// </summary>
        public CollisionManager()
        {
            bucketIndex = new Dictionary<Point, int>();
            buckets = new Dictionary<int, CollidableBucket>();
            aabbs = new List<CollidableAABB>();
        }

        #region ICollisionManager members
        /// <summary>
        /// returns true if collider intersects a collidable under management.
        /// </summary>
        /// <param name="collider">Rectangle to check for collision</param>
        /// <param name="bump">If true, don't run the Bump() func on the objects that are hit</param>
        /// <returns></returns>
        public bool IsColliding(RectangleF collider, bool suppressBump = false)
        {
            PointF[] points = {   new PointF(collider.Left, collider.Top),
                                  new PointF(collider.Right, collider.Top),
                                  new PointF(collider.Right, collider.Bottom),
                                  new PointF(collider.Left, collider.Bottom)
                              };
            List<CollidableBucket> buckets = new List<CollidableBucket>();

            //Get the buckets that correspond to the collider's points.
            foreach (PointF point in points)
            {
                buckets.Add(GetBucket(point));
            }
            buckets = buckets.Distinct().ToList();

            //Get all of the points
            List<CollidablePoint> cpoints = new List<CollidablePoint>();
            foreach (CollidableBucket bucket in buckets)
            {
                cpoints.AddRange(bucket.GetPoints());
            }

            //Expand points to distinct AABBs
            List<CollidableAABB> AABBs = (from cp in cpoints
                                          select cp.parentAABB).Distinct().ToList();

            //try all of the AABBs against the target rect.
            bool collided = false;
            foreach (CollidableAABB AABB in AABBs)
            {
                if (AABB.collidable.AABB.IntersectsWith(collider))
                {
                    if (AABB.IsHardCollider) //If the collider is supposed to prevent movement
                    {
                        collided = true;
                    }

                    if (!suppressBump)
                        AABB.collidable.Bump();
                }
            }
            return collided;
        }

        /// <summary>
        /// Adds a collidable to the manager.
        /// </summary>
        /// <param name="collidable"></param>
        public void AddCollidable(ICollidable collidable)
        {
            CollidableAABB c = new CollidableAABB(collidable);
            foreach (CollidablePoint p in c.points)
            {
                AddPoint(p);
            }
            aabbs.Add(c);
        }

        /// <summary>
        /// Removes a collidable from the manager
        /// </summary>
        /// <param name="collidable"></param>
        public void RemoveCollidable(ICollidable collidable)
        {
            var aabb = from a in aabbs
                       where a.collidable == collidable
                       select a;

            if (aabb.Count() == 0)
                return;
            CollidableAABB ourAABB = aabb.First();
            foreach (CollidablePoint p in ourAABB.points)
            {
                RemovePoint(p);
            }
            aabbs.Remove(ourAABB);
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

        #region IService Members
        public ClientServiceType ServiceType
        {
            get { return serviceType; }
        }
        #endregion

        /// <summary>
        /// Adds an AABB point to a buckets
        /// </summary>
        /// <param name="point"></param>
        private void AddPoint(CollidablePoint point)
        {
            CollidableBucket b = GetBucket(point.coordinates);
            b.AddPoint(point);
        }

        /// <summary>
        /// Removes an AABB point from a bucket
        /// </summary>
        /// <param name="point"></param>
        private void RemovePoint(CollidablePoint point)
        {
            CollidableBucket b = GetBucket(point.coordinates);
            b.RemovePoint(point);
        }

        /// <summary>
        /// Gets a bucket given a point coordinate
        /// </summary>
        /// <param name="coordinate"></param>
        /// <returns></returns>
        private CollidableBucket GetBucket(Point coordinate)
        {
            if (bucketIndex.ContainsKey(GetBucketCoordinate(coordinate)))
                return buckets[bucketIndex[GetBucketCoordinate(coordinate)]];
            else
                return CreateBucket(GetBucketCoordinate(coordinate));
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

        private Point GetBucketCoordinate(PointF coordinate)
        {
            int x = (int)Math.Floor(coordinate.X / bucketSize);
            int y = (int)Math.Floor(coordinate.Y / bucketSize);
            return new Point(x, y);
        }

        private Point GetBucketCoordinate(Point coordinate)
        {
            int x = (int)Math.Floor((decimal)coordinate.X / bucketSize);
            int y = (int)Math.Floor((decimal)coordinate.Y / bucketSize);
            return new Point(x, y);
        }

        private CollidableBucket CreateBucket(Point coordinate)
        {
            if (bucketIndex.ContainsKey(coordinate))
                return buckets[bucketIndex[GetBucketCoordinate(coordinate)]];
            else
            {
                CollidableBucket b = new CollidableBucket(lastIndex, coordinate);
                buckets.Add(lastIndex, b);
                bucketIndex.Add(coordinate, lastIndex);
                lastIndex++;
                return b;
            }
        }


    }

    /// <summary>
    /// This class holds points of collision AABBs. It represents a square in the world.
    /// </summary>
    internal class CollidableBucket
    {
        private int index;
        private Point coordinates;
        private List<CollidablePoint> points;

        public CollidableBucket(int _index, Point _coordinates)
        {
            index = _index;
            coordinates = _coordinates;
            points = new List<CollidablePoint>();
        }

        /// <summary>
        /// Adds a CollidablePoint to this bucket
        /// </summary>
        /// <param name="point"></param>
        public void AddPoint(CollidablePoint point)
        {
            points.Add(point);
        }

        /// <summary>
        /// Removes a CollidablePoint from this bucket, if it exists.
        /// </summary>
        /// <param name="point"></param>
        public void RemovePoint(CollidablePoint point)
        {
            if(points.Contains(point))
                points.Remove(point);
        }

        public IEnumerable<CollidablePoint> GetPoints()
        {
            return points;
        }
    }

    /// <summary>
    /// This represents a point of a collision AABB
    /// </summary>
    internal struct CollidablePoint
    {
        public CollidablePointIndex index;
        public PointF coordinates;
        public CollidableAABB parentAABB;
        public CollidablePoint(CollidablePointIndex _index, PointF _coordinates, CollidableAABB _parentAABB)
        {
            index = _index;
            coordinates = _coordinates;
            parentAABB = _parentAABB;
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
        public ICollidable collidable;
        public CollidablePoint[] points;
        public bool IsHardCollider;

        public CollidableAABB(ICollidable _collidable)
        {
            collidable = _collidable;
            IsHardCollider = collidable.IsHardCollidable;
            points = new CollidablePoint[4];
            float Top = collidable.AABB.Top;
            float Bottom = collidable.AABB.Bottom;
            float Left = collidable.AABB.Left;
            float Right = collidable.AABB.Right;
            points[0] = new CollidablePoint(CollidablePointIndex.TopLeft, new PointF(Left, Top), this);
            points[1] = new CollidablePoint(CollidablePointIndex.TopRight, new PointF(Right, Top), this);
            points[2] = new CollidablePoint(CollidablePointIndex.BottomRight, new PointF(Right, Bottom), this);
            points[3] = new CollidablePoint(CollidablePointIndex.BottomLeft, new PointF(Left, Bottom), this);
        }
    }
}
