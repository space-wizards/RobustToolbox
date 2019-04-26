using Robust.Shared.Interfaces.Map;
using Robust.Shared.Timing;

namespace Robust.Shared.Map
{
    internal interface IMapGridInternal : IMapGrid
    {
        GameTick LastModifiedTick { get; }

        GameTick CurTick { get; }
        
        void NotifyTileChanged(in TileRef tileRef, in Tile oldTile);
    }
}
