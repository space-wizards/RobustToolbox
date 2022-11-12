using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;

namespace Robust.Shared.Map;

internal interface INetworkedMapManager : IMapManagerInternal
{
    // Two methods here, so that new grids etc can be made BEFORE entities get states applied,
    // but old ones can be deleted after.
    void ApplyGameStatePre(ReadOnlySpan<EntityState> entityStates);
}

internal sealed class NetworkedMapManager : MapManager, INetworkedMapManager
{
    private readonly List<(MapId mapId, EntityUid euid)> _newMaps = new();

    public void ApplyGameStatePre(ReadOnlySpan<EntityState> entityStates)
    {
        // Setup new maps
        {
            // search for any newly created map components
            foreach (var entityState in entityStates)
            {
                foreach (var compChange in entityState.ComponentChanges.Span)
                {
                    if (compChange.State is MapComponentState mapCompState)
                    {
                        var mapEuid = entityState.Uid;
                        var mapId = mapCompState.MapId;

                        // map already exists from a previous state.
                        if (MapExists(mapId))
                            continue;

                        _newMaps.Add((mapId, mapEuid));
                    }
                }
            }

            // create all the new maps
            foreach (var (mapId, euid) in _newMaps)
            {
                CreateMap(mapId, euid);
            }
            _newMaps.Clear();
        }
    }
}
