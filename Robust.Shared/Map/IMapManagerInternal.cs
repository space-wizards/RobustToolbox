using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Robust.Shared.Map
{
    /// <inheritdoc />
    internal interface IMapManagerInternal : IMapManager
    {
        IGameTiming GameTiming { get; }

        /// <summary>
        ///     Raises the OnTileChanged event.
        /// </summary>
        /// <param name="tileRef">A reference to the new tile.</param>
        /// <param name="oldTile">The old tile that got replaced.</param>
        void RaiseOnTileChanged(TileRef tileRef, Tile oldTile, Vector2i chunk);

        MapId CreateMap(MapId? mapId, EntityUid euid);

        void RemoveMapId(MapId mapId);
    }
}
