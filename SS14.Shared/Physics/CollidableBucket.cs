using System.Collections.Generic;
using SS14.Shared.Maths;

namespace SS14.Shared.Physics
{
    /// <summary>
    /// This class holds points of collision AABBs. It represents a square in the world.
    /// </summary>
    internal class CollidableBucket
    {
        private readonly List<CollidablePoint> _points;
        private readonly Vector2i _coordinates;
        private readonly int _index;

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
}
