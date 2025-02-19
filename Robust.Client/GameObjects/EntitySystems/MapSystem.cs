using Robust.Client.Graphics;
using Robust.Client.Map;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Robust.Client.GameObjects;

public sealed class MapSystem : SharedMapSystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IResourceCache _resource = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;

    protected override MapId GetNextMapId()
    {
        // Client-side map entities use negative map Ids to avoid conflict with server-side maps.
        var id = new MapId(--LastMapId);
        while (MapExists(id) || UsedIds.Contains(id))
        {
            id = new MapId(--LastMapId);
        }
        return id;
    }

    public override void Initialize()
    {
        base.Initialize();
        _overlayManager.AddOverlay(new TileEdgeOverlay(EntityManager, _resource, _tileDefinitionManager));
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlayManager.RemoveOverlay<TileEdgeOverlay>();
    }
}
