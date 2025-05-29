using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.UnitTesting.Constraints;

namespace Robust.UnitTesting
{
    public sealed class Is : NUnit.Framework.Is
    {
        /// <summary>
        /// Returns a constraint that tests for equality within a set tolerance.
        /// </summary>
        public static ApproxEqualityConstraint Approximately(object expected, double? tolerance = null)
            => new(expected, tolerance);

        /// <summary>
        /// Returns a constraint that tests if an entity has been deleted.
        /// </summary>
        public static EntityDeletedConstraint Deleted(IEntityManager entMan)
            => new(entMan);

        /// <summary>
        /// Returns a constraint that tests if an entity is on a specified map.
        /// </summary>
        public static EntityOnMapConstraint OnMap(MapId mapId, IEntityManager entMan)
            => new(mapId, entMan);

        /// <summary>
        /// Returns a constraint that tests if an entity is in nullspace.
        /// </summary>
        public static EntityOnMapConstraint InNullspace(IEntityManager entMan)
            => new(MapId.Nullspace, entMan);

        /// <summary>
        /// Returns a constraint that tests if an entity is within a specified range of another entity.
        /// </summary>
        public static EntityInRangeOfConstraint InRangeOf(EntityUid other, float range, SharedTransformSystem xformSystem)
            => new(other, range, xformSystem);

        /// <inheritdoc cref="InRangeOf(EntityUid, float, SharedTransformSystem)"/>
        public static EntityInRangeOfConstraint InRangeOf(EntityUid other, float range, IEntityManager entMan)
            => new(other, range, entMan.System<SharedTransformSystem>());
    }
}
