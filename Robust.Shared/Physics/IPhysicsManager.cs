using Robust.Shared.Map;

namespace Robust.Shared.Physics
{
    /// <summary>
    ///     This service provides access into the physics system.
    /// </summary>
    public interface IPhysicsManager
    {
        /// <summary>
        ///     Checks whether a certain grid position is weightless or not
        /// </summary>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        bool IsWeightless(EntityCoordinates coordinates);

        /// <summary>
        ///     Calculates the penetration depth of the axis-of-least-penetration for a
        /// </summary>
        /// <param name="target"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        float CalculatePenetration(IPhysBody target, IPhysBody source);
    }
}
