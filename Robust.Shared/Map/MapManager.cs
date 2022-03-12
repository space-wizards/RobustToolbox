using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Shared.Map;

/// <inheritdoc cref="IMapManager" />
[Virtual]
internal partial class MapManager : IMapManagerInternal, IEntityEventSubscriber
{
    [field: Dependency] public IGameTiming GameTiming { get; } = default!;
    [field: Dependency] public IEntityManager EntityManager { get; } = default!;

    [Dependency] private readonly IConsoleHost _conhost = default!;

    /// <inheritdoc />
    public void Initialize()
    {
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
#if DEBUG
        DebugTools.Assert(_dbgGuardInit);
        _dbgGuardRunning = true;
#endif

        Logger.DebugS("map", "Starting...");

        StartupGridTrees();
        EnsureNullspaceExistsAndClear();

        DebugTools.Assert(_grids.Count == 0);
        DebugTools.Assert(!GridExists(GridId.Invalid));
    }

    /// <inheritdoc />
    public void Shutdown()
    {
#if DEBUG
        DebugTools.Assert(_dbgGuardInit);
#endif
        Logger.DebugS("map", "Stopping...");

        foreach (var mapComp in EntityManager.EntityQuery<IMapComponent>())
        {
            EntityManager.DeleteEntity(mapComp.Owner);
        }
        ShutdownGridTrees();

#if DEBUG
        DebugTools.Assert(_grids.Count == 0);
        DebugTools.Assert(!GridExists(GridId.Invalid));
        _dbgGuardRunning = false;
#endif
    }

    /// <inheritdoc />
    public void Restart()
    {
        Logger.DebugS("map", "Restarting...");

        // Don't just call Shutdown / Startup because we don't want to touch the subscriptions on gridtrees
        // Restart can be called any time during a game, whereas shutdown / startup are typically called upon connection.
        DeleteAllMaps();
        EnsureNullspaceExistsAndClear();
    }

#if DEBUG
    private bool _dbgGuardInit;
    private bool _dbgGuardRunning;
#endif
}
