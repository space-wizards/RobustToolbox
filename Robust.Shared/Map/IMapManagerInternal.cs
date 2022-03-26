using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Robust.Shared.Map
{
    /// <inheritdoc />
    internal interface IMapManagerInternal : IMapManager
    {
        IGameTiming GameTiming { get; }
        IEntityManager EntityManager { get; }

        void OnComponentRemoved(MapGridComponent comp);

        void ChunkRemoved(GridId gridId, MapChunk chunk);

        /// <summary>
        /// Specific version of TryFindGridAt that allows re-usable data structures to be passed in for optimisation reasons.
        /// </summary>
        bool TryFindGridAt(
            MapId mapId,
            Vector2 worldPos,
            List<MapGrid> grids,
            EntityQuery<TransformComponent> xformQuery,
            EntityQuery<PhysicsComponent> bodyQuery,
            [NotNullWhen(true)] out IMapGrid? grid);

        /// <summary>
        /// Specific version of FindGridsIntersecting that allows re-usable data structures to be passed in for optimisation reasons.
        /// </summary>
        IEnumerable<IMapGrid> FindGridsIntersecting(
            MapId mapId,
            Box2 worldAabb,
            List<MapGrid> grids,
            EntityQuery<TransformComponent> xformQuery,
            EntityQuery<PhysicsComponent> physicsQuery,
            bool approx = false);

        /// <summary>
        ///     Raises the OnTileChanged event.
        /// </summary>
        /// <param name="tileRef">A reference to the new tile.</param>
        /// <param name="oldTile">The old tile that got replaced.</param>
        void RaiseOnTileChanged(TileRef tileRef, Tile oldTile);

        bool TryGetGridComp(GridId id, [MaybeNullWhen(false)]out IMapGridComponent comp);
        bool TryGetGridEuid(GridId id, [MaybeNullWhen(false)]out EntityUid euid);
        void TrueGridDelete(MapGrid grid);
        void TrueDeleteMap(MapId mapId);
        GridId GenerateGridId(GridId? forcedGridId);
        void OnGridAllocated(MapGridComponent gridComponent, MapGrid mapGrid);
    }
}
