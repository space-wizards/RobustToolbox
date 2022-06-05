using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Robust.Shared.Console.Commands;

internal sealed class DumpEventTablesCommand : IConsoleCommand
{
    public string Command => "dump_event_tables";
    public string Description => Loc.GetString("cmd-dump_event_tables-desc");
    public string Help => Loc.GetString("cmd-dump_event_tables-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var entMgr = IoCManager.Resolve<EntityManager>();
        var compFactory = IoCManager.Resolve<IComponentFactory>();

        if (args.Length < 1)
        {
            shell.WriteError(Loc.GetString("cmd-dump_event_tables-missing-arg-entity"));
            return;
        }

        if (!EntityUid.TryParse(args[0], out var entity) || !entMgr.EntityExists(entity))
        {
            shell.WriteError(Loc.GetString("cmd-dump_event_tables-error-entity"));
            return;
        }

        var eventBus = (EntityEventBus)entMgr.EventBus;

        var table = eventBus._entEventTables[entity];
        foreach (var (evType, comps) in table)
        {
            shell.WriteLine($"{evType}:");

            foreach (var comp in comps)
            {
                var reg = compFactory.IdxToType(comp);
                shell.WriteLine($"    {reg.Name}");
            }
        }
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromHint(Loc.GetString("cmd-dump_event_tables-arg-entity"));

        return CompletionResult.Empty;
    }
}
