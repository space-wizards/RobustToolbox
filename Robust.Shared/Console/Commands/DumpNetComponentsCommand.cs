using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Robust.Shared.Console.Commands;

internal sealed class DumpNetComponentsCommand : LocalizedCommands
{
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

    public override string Command => "dump_net_comps";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (_componentFactory.NetworkedComponents is not { } comps)
        {
            shell.WriteError(Loc.GetString("cmd-dump_net_comps-error-writeable"));
            return;
        }

        shell.WriteLine(Loc.GetString("cmd-dump_net_comps-header"));

        for (var netId = 0; netId < comps.Count; netId++)
        {
            var registration = comps[netId];
            shell.WriteLine($"  [{netId,4}] {registration.Name,-16} {registration.Type.Name}");
        }
    }
}
