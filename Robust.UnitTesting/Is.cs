using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.UnitTesting.Constraints;

namespace Robust.UnitTesting
{
    public sealed class Is : NUnit.Framework.Is
    {
        public static ApproxEqualityConstraint Approximately(object expected, double? tolerance = null)
            => new(expected, tolerance);

        public static EntityDeletedConstraint Deleted(IEntityManager entMan)
            => new(entMan);

        public static EntityOnMapConstraint OnMap(MapId mapId, IEntityManager entMan)
            => new(mapId, entMan);

        public static EntityOnMapConstraint InNullspace(IEntityManager entMan)
            => new(MapId.Nullspace, entMan);

        public static EntityInRangeOfConstraint InRangeOf(EntityUid other, float range, SharedTransformSystem xformSystem)
            => new(other, range, xformSystem);

        public static EntityInRangeOfConstraint InRangeOf(EntityUid other, float range, IEntityManager entMan)
            => new(other, range, entMan.System<SharedTransformSystem>());
    }
}
