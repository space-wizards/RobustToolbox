using System.Globalization;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Shared.Map.Commands;

/// <summary>
/// Pauses a given map, halting all entity processing on it.
/// </summary>
public sealed partial class PauseMapCommand : LocalizedEntityCommands
{
    [Dependency] SharedMapSystem _mapSystem = default!;
    public override string Command => "pausemap";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError("Need to supply a valid MapId");
            return;
        }

        var mapId = new MapId(int.Parse(args[0], CultureInfo.InvariantCulture));

        if (!_mapSystem.MapExists(mapId))
        {
            shell.WriteError("That map does not exist.");
            return;
        }

        _mapSystem.SetPaused(mapId, true);
    }
}

/// <summary>
/// Unpauses a given map, resuming all entity processing on it.
/// </summary>
public sealed partial class UnpauseMapCommand : LocalizedEntityCommands
{
    [Dependency] SharedMapSystem _mapSystem = default!;
    public override string Command => "unpausemap";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError("Need to supply a valid MapId");
            return;
        }

        var mapId = new MapId(int.Parse(args[0], CultureInfo.InvariantCulture));

        if (!_mapSystem.MapExists(mapId))
        {
            shell.WriteError("That map does not exist.");
            return;
        }

        _mapSystem.SetPaused(mapId, false);
    }
}

/// <summary>
/// Checks whether a given map is currently paused.
/// </summary>
public sealed partial class QueryMapPausedCommand : LocalizedEntityCommands
{
    [Dependency] SharedMapSystem _mapSystem = default!;
    public override string Command => "querymappaused";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError("Need to supply a valid MapId");
            return;
        }

        var mapId = new MapId(int.Parse(args[0], CultureInfo.InvariantCulture));

        if (!_mapSystem.MapExists(mapId))
        {
            shell.WriteError("That map does not exist.");
            return;
        }

        shell.WriteLine(_mapSystem.IsPaused(mapId).ToString());
    }
}
