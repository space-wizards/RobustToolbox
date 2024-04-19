using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Robust.Shared.Map;

/// <inheritdoc cref="IMapManager" />
[Virtual]
internal partial class MapManager : IMapManagerInternal, IEntityEventSubscriber
{
    [field: Dependency] public IGameTiming GameTiming { get; } = default!;
    [field: Dependency] public IEntityManager EntityManager { get; } = default!;
    [Dependency] private readonly IManifoldManager _manifolds = default!;

    [Dependency] private readonly IConsoleHost _conhost = default!;

    private ISawmill _sawmill => _mapSystem.Log;

    private SharedMapSystem _mapSystem = default!;
    private SharedPhysicsSystem _physics = default!;
    private SharedTransformSystem _transformSystem = default!;

    private EntityQuery<GridTreeComponent> _gridTreeQuery;
    private EntityQuery<MapGridComponent> _gridQuery;

    /// <inheritdoc />
    public void Initialize()
    {
        _gridTreeQuery = EntityManager.GetEntityQuery<GridTreeComponent>();
        _gridQuery = EntityManager.GetEntityQuery<MapGridComponent>();
        InitializeMapPausing();
    }

    /// <inheritdoc />
    public void Startup()
    {
        _physics = EntityManager.System<SharedPhysicsSystem>();
        _transformSystem = EntityManager.System<SharedTransformSystem>();
        _mapSystem = EntityManager.System<SharedMapSystem>();

        _sawmill.Debug("Starting...");
    }

    /// <inheritdoc />
    public void Shutdown()
    {
        _sawmill.Debug("Stopping...");

        // TODO: AllEntityQuery instead???
        var query = EntityManager.EntityQueryEnumerator<MapComponent>();

        while (query.MoveNext(out var uid, out _))
        {
            EntityManager.DeleteEntity(uid);
        }
    }

    /// <inheritdoc />
    public void Restart()
    {
        _sawmill.Debug("Restarting...");

        // Don't just call Shutdown / Startup because we don't want to touch the subscriptions on gridtrees
        // Restart can be called any time during a game, whereas shutdown / startup are typically called upon connection.
        var query = EntityManager.EntityQueryEnumerator<MapComponent>();

        while (query.MoveNext(out var uid, out _))
        {
            EntityManager.DeleteEntity(uid);
        }
    }
}
