using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Robust.Shared.Console.Commands;

internal sealed class DumpEventTablesCommand : LocalizedCommands
{
    [Dependency] private readonly EntityManager _entities = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

    public override string Command => "dump_event_tables";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1)
        {
            shell.WriteError(Loc.GetString("cmd-dump_event_tables-missing-arg-entity"));
            return;
        }

        if (!EntityUid.TryParse(args[0], out var entity) || !_entities.EntityExists(entity))
        {
            shell.WriteError(Loc.GetString("cmd-dump_event_tables-error-entity"));
            return;
        }

        var eventBus = (EntityEventBus)_entities.EventBus;

        var table = eventBus._entEventTables[entity];
        foreach (var (evType, comps) in table.EventIndices)
        {
            shell.WriteLine($"{evType}:");

            var idx = comps;
            while (idx != -1)
            {
                ref var entry = ref table.ComponentLists[idx];
                idx = entry.Next;

                var reg = _componentFactory.IdxToType(entry.Component);
                shell.WriteLine($"    {reg.Name}");
            }
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromHint(Loc.GetString("cmd-dump_event_tables-arg-entity"));

        return CompletionResult.Empty;
    }
}
