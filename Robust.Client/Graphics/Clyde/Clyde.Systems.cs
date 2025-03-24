using Robust.Client.ComponentTrees;
using Robust.Client.GameObjects;

namespace Robust.Client.Graphics.Clyde;

internal sealed partial class Clyde
{
    // Caches entity systems required by Clyde.

    private MapSystem _mapSystem = default!;
    private LightTreeSystem _lightTreeSystem = default!;
    private TransformSystem _transformSystem = default!;
    private SpriteTreeSystem _spriteTreeSystem = default!;
    private ClientOccluderSystem _occluderSystem = default!;

    private void InitSystems()
    {
        _entityManager.AfterStartup += EntityManagerOnAfterStartup;
        _entityManager.AfterShutdown += EntityManagerOnAfterShutdown;
    }

    private void EntityManagerOnAfterStartup()
    {
        _mapSystem = _entitySystemManager.GetEntitySystem<MapSystem>();
        _lightTreeSystem = _entitySystemManager.GetEntitySystem<LightTreeSystem>();
        _transformSystem = _entitySystemManager.GetEntitySystem<TransformSystem>();
        _spriteTreeSystem = _entitySystemManager.GetEntitySystem<SpriteTreeSystem>();
        _occluderSystem = _entitySystemManager.GetEntitySystem<ClientOccluderSystem>();
    }

    private void EntityManagerOnAfterShutdown()
    {
        _mapSystem = null!;
        _lightTreeSystem = null!;
        _transformSystem = null!;
        _spriteTreeSystem = null!;
        _occluderSystem = null!;
    }
}
