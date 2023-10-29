using Robust.Client.Graphics;
using Robust.Client.Map;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Robust.Client.GameObjects
{
    public sealed class MapSystem : SharedMapSystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IOverlayManager _overlayManager = default!;
        [Dependency] private readonly IClientResourceCache _resource = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            _overlayManager.AddOverlay(new TileEdgeOverlay(EntityManager, _mapManager, _resource, _tileDefinitionManager));
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _overlayManager.RemoveOverlay<TileEdgeOverlay>();
        }
    }
}
