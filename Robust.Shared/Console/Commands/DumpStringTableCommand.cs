using System.Linq;
using Robust.Shared.IoC;
using Robust.Shared.Network;

namespace Robust.Shared.Console.Commands;

internal sealed class DumpStringTableCommand : IConsoleCommand
{
    [Dependency] private readonly INetManager _netManager = default!;

    public string Command  => "net_dumpstringtable";
    public string Description  => "";
    public string Help => "";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var netMgr = (NetManager)_netManager;
        foreach (var (k, v) in netMgr.StringTable.Strings.OrderBy(x => x.Key))
        {
            shell.WriteLine($"{k}: {v}");
        }
    }
}
