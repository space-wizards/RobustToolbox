using System;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Robust.Shared.Physics
{
    /// <summary>
    /// A primitive physical shape that is used by a <see cref="IPhysBody"/>.
    /// </summary>
    public interface IPhysShape : IExposeData
    {
        /// <summary>
        /// Calculates the AABB of the shape.
        /// </summary>
        /// <param name="rotation"></param>
        /// <returns></returns>
        Box2 CalculateLocalBounds(Angle rotation);

        void ApplyState();

        void DebugDraw(DebugDrawingHandle handle, in Matrix3 modelMatrix, in Box2 worldViewport, float sleepPercent);
    }

    /// <summary>
    /// Tag type for defining the representation of the collision layer bitmask
    /// in terms of readable names in the content. To understand more about the
    /// point of this type, see the <see cref="FlagsForAttribute"/>.
    /// </summary>
    public sealed class CollisionLayer {}

    /// <summary>
    /// Tag type for defining the representation of the collision mask bitmask
    /// in terms of readable names in the content. To understand more about the
    /// point of this type, see the <see cref="FlagsForAttribute"/>.
    /// </summary>
    public sealed class CollisionMask {}
}
