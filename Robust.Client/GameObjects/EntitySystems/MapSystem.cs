using Robust.Client.Graphics;
using Robust.Client.Map;
using Robust.Client.Physics;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Dynamics;

namespace Robust.Client.GameObjects;

public sealed class MapSystem : SharedMapSystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IResourceCache _resource = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;

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

    protected override void OnMapAdd(EntityUid uid, MapComponent component, ComponentAdd args)
    {
        EnsureComp<PhysicsMapComponent>(uid);
    }
}
