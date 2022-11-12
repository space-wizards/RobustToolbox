using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Components;

namespace Robust.Shared.Map
{
    /// <inheritdoc />
    internal interface IMapManagerInternal : IMapManager
    {
        /// <summary>
        /// Specific version of TryFindGridAt that allows re-usable data structures to be passed in for optimisation reasons.
        /// </summary>
        bool TryFindGridAt(MapId mapId,
            Vector2 worldPos,
            List<MapGridComponent> grids,
            EntityQuery<TransformComponent> xformQuery,
            EntityQuery<PhysicsComponent> bodyQuery,
            [MaybeNullWhen(false)] out MapGridComponent grid);

        /// <summary>
        /// Specific version of FindGridsIntersecting that allows re-usable data structures to be passed in for optimisation reasons.
        /// </summary>
        IEnumerable<MapGridComponent> FindGridsIntersecting(MapId mapId,
            Box2 worldAabb,
            List<MapGridComponent> grids,
            EntityQuery<TransformComponent> xformQuery,
            EntityQuery<PhysicsComponent> physicsQuery,
            bool approx = false);

        void TrueDeleteMap(MapId mapId);
        void OnGridBoundsChange(EntityUid uid, MapGridComponent grid);
    }
}
