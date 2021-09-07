using System;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Collision.Shapes;

namespace Robust.Shared.Physics
{
    public interface IShapeManager
    {
        /// <summary>
        /// Returns whether a particular point intersects the specified shape.
        /// </summary>
        bool TestPoint(IPhysShape shape, Transform xform, Vector2 worldPoint);
    }

    public class ShapeManager : IShapeManager
    {
        /// <inheritdoc />
        public bool TestPoint(IPhysShape shape, Transform xform, Vector2 worldPoint)
        {
            switch (shape)
            {
                case EdgeShape:
                    return false;
                case PhysShapeAabb aabb:
                    // TODO: When we get actual AABBs it will be a stupid ez check,
                    var polygon = (PolygonShape) aabb;
                    return TestPoint(polygon, xform, worldPoint);
                case PhysShapeCircle circle:
                    var center = xform.Position + Transform.Mul(xform.Quaternion2D, circle.Position);
                    var distance = worldPoint - center;
                    return Vector2.Dot(distance, distance) <= circle.Radius * circle.Radius;
                case PolygonShape poly:
                    var pLocal = Transform.MulT(xform.Quaternion2D, worldPoint - xform.Position);

                    for (var i = 0; i < poly.Vertices.Length; i++)
                    {
                        var dot = Vector2.Dot(poly.Normals[i], pLocal - poly.Vertices[i]);
                        if (dot > 0f) return false;
                    }

                    return true;
                default:
                    throw new ArgumentOutOfRangeException($"No implemented TestPoint for {shape.GetType()}");
            }
        }
    }
}
