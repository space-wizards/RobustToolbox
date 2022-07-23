using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Robust.Shared.Console.Commands;

internal sealed class DumpNetComponentsCommand : IConsoleCommand
{
    public string Command => "dump_net_comps";
    public string Description => Loc.GetString("cmd-dump_net_comps-desc");
    public string Help => Loc.GetString("cmd-dump_net_comps-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var mgr = IoCManager.Resolve<IComponentFactory>();

        if (mgr.NetworkedComponents is not { } comps)
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
