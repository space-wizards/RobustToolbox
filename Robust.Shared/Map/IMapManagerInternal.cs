using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
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
        ///     Raises the OnTileChanged event.
        /// </summary>
        /// <param name="tileRef">A reference to the new tile.</param>
        /// <param name="oldTile">The old tile that got replaced.</param>
        void RaiseOnTileChanged(TileRef tileRef, Tile oldTile);

        bool TryGetGridComp(GridId id, [MaybeNullWhen(false)]out IMapGridComponent comp);
        bool TryGetGridEuid(GridId id, [MaybeNullWhen(false)]out EntityUid euid);
        void TrueGridDelete(MapGrid grid);
        MapGrid CreateUnboundGrid(GridId? forcedGridId);
        void BindGrid(MapGridComponent gridComponent, MapGrid mapGrid);
        void TrueDeleteMap(MapId mapId);
        GridId GenerateGridId(GridId? forcedGridId);
    }
}
