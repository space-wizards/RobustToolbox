#if TOOLS
using System;
using JetBrains.Profiler.Api;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Robust.Server.Console.Commands;

public sealed class ProfileEntitySpawningCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entities = default!;

    public string Command => "profileEntitySpawning";
    public string Description => "Profiles entity spawning with n entities";
    public string Help => $"Usage: {Command} | {Command} <amount> <prototype>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var amount = 1000;
        string? prototype = null;

        switch (args.Length)
        {
            case 0:
                break;
            case 2:
                if (!int.TryParse(args[0], out amount))
                {
                    shell.WriteError($"First argument is not an integer: {args[0]}");
                    return;
                }

                prototype = args[1];

                break;
            default:
                shell.WriteError(Help);
                return;
        }

        GC.Collect();

        Span<EntityUid> ents = stackalloc EntityUid[amount];
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        MeasureProfiler.StartCollectingData();

        for (var i = 0; i < amount; i++)
        {
            ents[i] = _entities.SpawnEntity(prototype, MapCoordinates.Nullspace);
        }

        MeasureProfiler.SaveData();

        shell.WriteLine($"Server: Profiled spawning {amount} entities in {stopwatch.Elapsed.TotalMilliseconds:N3} ms");

        foreach (var ent in ents)
        {
            _entities.DeleteEntity(ent);
        }
    }
}
#endif
