using System;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Dynamics.Shapes;
using Robust.Shared.Serialization;

namespace Robust.Shared.Physics
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
    public interface IPhysShape : IExposeData, IEquatable<IPhysShape>
    {
        /// <summary>
        ///     Get the number of child primitives. Only relevant for chain shape.
        /// </summary>
        int ChildCount { get; }

        float Radius { get; set; }

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
