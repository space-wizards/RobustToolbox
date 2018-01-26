using SS14.Shared.Interfaces.Physics;
using SS14.Shared.Maths;

namespace SS14.Shared.Physics
{
    /// <summary>
    /// This is our representation of an AABB.
    /// </summary>
    internal struct CollidableAABB
    {
        public ICollidable Collidable;
        public CollidablePoint[] Points;

        public CollidableAABB(ICollidable collidable)
        {
            Collidable = collidable;
            Points = new CollidablePoint[4];
            float top = Collidable.AABB.Top;
            float bottom = Collidable.AABB.Bottom;
            float left = Collidable.AABB.Left;
            float right = Collidable.AABB.Right;
            Points[0] = new CollidablePoint(CollidablePointIndex.TopLeft, new Vector2(left, top), this);
            Points[1] = new CollidablePoint(CollidablePointIndex.TopRight, new Vector2(right, top), this);
            Points[2] = new CollidablePoint(CollidablePointIndex.BottomRight, new Vector2(right, bottom), this);
            Points[3] = new CollidablePoint(CollidablePointIndex.BottomLeft, new Vector2(left, bottom), this);
        }
    }
}
