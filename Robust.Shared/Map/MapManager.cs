using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Shared.Map;

/// <inheritdoc cref="IMapManager" />
[Virtual]
internal partial class MapManager : IMapManagerInternal, IEntityEventSubscriber
{
    [field: Dependency] public IGameTiming GameTiming { get; } = default!;
    [field: Dependency] public IEntityManager EntityManager { get; } = default!;
    [Dependency] private readonly IManifoldManager _manifolds = default!;

    [Dependency] private readonly IConsoleHost _conhost = default!;

    private ISawmill _sawmill = default!;

    private FixtureSystem _fixtureSystem = default!;
    private SharedMapSystem _mapSystem = default!;
    private SharedPhysicsSystem _physics = default!;
    private SharedTransformSystem _transformSystem = default!;

    private EntityQuery<FixturesComponent> _fixturesQuery;
    private EntityQuery<GridTreeComponent> _gridTreeQuery;
    private EntityQuery<MapGridComponent> _gridQuery;
    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<TransformComponent> _xformQuery;

    /// <inheritdoc />
    public void Initialize()
    {
        _fixturesQuery = EntityManager.GetEntityQuery<FixturesComponent>();
        _gridTreeQuery = EntityManager.GetEntityQuery<GridTreeComponent>();
        _gridQuery = EntityManager.GetEntityQuery<MapGridComponent>();
        _physicsQuery = EntityManager.GetEntityQuery<PhysicsComponent>();
        _xformQuery = EntityManager.GetEntityQuery<TransformComponent>();

        _sawmill = Logger.GetSawmill("map");

#if DEBUG
        DebugTools.Assert(!_dbgGuardInit);
        DebugTools.Assert(!_dbgGuardRunning);
        _dbgGuardInit = true;
#endif
        InitializeMapPausing();
    }

    /// <inheritdoc />
    public void Startup()
    {
        _fixtureSystem = EntityManager.System<FixtureSystem>();
        _physics = EntityManager.System<SharedPhysicsSystem>();
        _transformSystem = EntityManager.System<SharedTransformSystem>();
        _mapSystem = EntityManager.System<SharedMapSystem>();

#if DEBUG
        DebugTools.Assert(_dbgGuardInit);
        _dbgGuardRunning = true;
#endif

        _sawmill.Debug("Starting...");
    }

    /// <inheritdoc />
    public void Shutdown()
    {
#if DEBUG
        DebugTools.Assert(_dbgGuardInit);
#endif
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

#if DEBUG
    private bool _dbgGuardInit;
    private bool _dbgGuardRunning;
#endif
}
