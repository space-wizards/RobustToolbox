using SS14.Shared.Maths;

namespace SS14.Shared.Physics
{
    internal enum CollidablePointIndex
    {
        TopLeft,
        TopRight,
        BottomRight,
        BottomLeft
    }

    /// <summary>
    ///     This represents a point of a collision AABB
    /// </summary>
    internal struct CollidablePoint
    {
        public Vector2 Coordinates;
        public CollidablePointIndex Index;
        public CollidableAABB ParentAABB;

        public CollidablePoint(CollidablePointIndex index, Vector2 coordinates, CollidableAABB parentAABB)
        {
            Index = index;
            Coordinates = coordinates;
            ParentAABB = parentAABB;
        }
    }
}
