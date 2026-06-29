using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map.Components;

namespace Robust.Shared.Map;

/// <inheritdoc cref="IMapManager" />
[Virtual]
internal partial class MapManager : IMapManagerInternal, IEntityEventSubscriber
{
    [Dependency] public IEntityManager EntityManager = default!;
    [Dependency] private ILogManager _logManager = default!;

    private ISawmill _sawmill = default!;

    protected SharedMapSystem MapSystem = default!;

    /// <inheritdoc />
    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill("system.map");
    }

    /// <inheritdoc />
    public void Startup()
    {
        MapSystem = EntityManager.System<SharedMapSystem>();

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
