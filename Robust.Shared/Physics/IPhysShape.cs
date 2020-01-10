using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Maths;

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

        /// <summary>
        /// Bitmask of the collision layers the component is a part of.
        /// </summary>
        int CollisionLayer { get; set; }

        /// <summary>
        ///  Bitmask of the layers this component collides with.
        /// </summary>
        int CollisionMask { get; set; }

        void ApplyState();
    }
}
