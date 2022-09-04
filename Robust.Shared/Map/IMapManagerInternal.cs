using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Robust.Shared.Map
{
    /// <inheritdoc />
    internal interface IMapManagerInternal : IMapManager
    {
        void OnComponentRemoved(MapGridComponent comp);

        void ChunkRemoved(EntityUid gridId, MapChunk chunk);

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

        /// <summary>
        ///     Raises the OnTileChanged event.
        /// </summary>
        /// <param name="tileRef">A reference to the new tile.</param>
        /// <param name="oldTile">The old tile that got replaced.</param>
        void RaiseOnTileChanged(TileRef tileRef, Tile oldTile);

        void TrueDeleteMap(MapId mapId);
        GridId GenerateGridId(GridId? forcedGridId);
        void OnGridBoundsChange(EntityUid uid, MapGridComponent grid);
    }
}
