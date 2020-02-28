using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Timing;

namespace Robust.Shared.Map
{
    /// <inheritdoc />
    internal interface IMapManagerInternal : IMapManager
    {
        IGameTiming GameTiming { get; }
        IEntityManager EntityManager { get; }

        /// <summary>
        ///     Raises the OnTileChanged event.
        /// </summary>
        /// <param name="tileRef">A reference to the new tile.</param>
        /// <param name="oldTile">The old tile that got replaced.</param>
        void RaiseOnTileChanged(TileRef tileRef, Tile oldTile);

        IMapGridInternal CreateGridNoEntity(MapId currentMapID, GridId? gridID = null, ushort chunkSize = 16, float snapSize = 1);
    }
}
