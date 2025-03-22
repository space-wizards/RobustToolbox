using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.UnitTesting.Constraints;

namespace Robust.UnitTesting
{
    public sealed class Is : NUnit.Framework.Is
    {
        public static ApproxEqualityConstraint Approximately(object expected, double? tolerance = null)
        {
            return new(expected, tolerance);
        }

        public static EntityDeletedConstraint Deleted(IEntityManager entMan) => new(entMan);

        public static EntityOnMapConstraint OnMap(MapId mapId, IEntityManager entMan) => new(mapId, entMan);

        public static EntityOnMapConstraint InNullspace(IEntityManager entMan) => new(MapId.Nullspace, entMan);
    }
}
