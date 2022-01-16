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

        void ChunkRemoved(MapChunk chunk);

        /// <summary>
        ///     Raises the OnTileChanged event.
        /// </summary>
        /// <param name="tileRef">A reference to the new tile.</param>
        /// <param name="oldTile">The old tile that got replaced.</param>
        void RaiseOnTileChanged(TileRef tileRef, Tile oldTile);

        IMapGridInternal CreateBoundGrid(MapId mapId, MapGridComponent gridComponent);
        bool TryGetGridComp(GridId id, [MaybeNullWhen(false)]out IMapGridComponent comp);
        bool TryGetGridEuid(GridId id, [MaybeNullWhen(false)]out EntityUid euid);
    }
}
