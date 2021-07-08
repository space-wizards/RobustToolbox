using System;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics.Collision.Shapes
{
    public enum ShapeType : sbyte
    {
        Unknown = -1,
        Circle = 0,
        Edge = 1,
        Polygon = 2,
        Chain = 3,
        Aabb = 4,
        Rectangle = 5, // Look you might be able to replace this with polys but for now I have done the thing
        TypeCount = 6, // Obviously increment this if you add something
    }

    /// <summary>
    /// A primitive physical shape that is used by a <see cref="IPhysBody"/>.
    /// </summary>
    public interface IPhysShape : IEquatable<IPhysShape>
    {
        /// <summary>
        ///     Get the number of child primitives. Only relevant for chain shape.
        /// </summary>
        int ChildCount { get; }

        /// <summary>
        /// Radius of the Shape
        /// Changing the radius causes a recalculation of shape properties.
        /// </summary>
        float Radius { get; set; }

        // Sloth: I removed density because mass is way easier to work with.
        // If you really want it back then code it yaself (and also probably put it on the fixture).

        ShapeType ShapeType { get; }

        /// <summary>
        /// Calculates the AABB of the shape.
        /// </summary>
        /// <param name="rotation"></param>
        /// <returns></returns>
        Box2 CalculateLocalBounds(Angle rotation);

        void ApplyState();

        void DebugDraw(DebugDrawingHandle handle, in Matrix3 modelMatrix, in Box2 worldViewport, float sleepPercent);
    }
}
