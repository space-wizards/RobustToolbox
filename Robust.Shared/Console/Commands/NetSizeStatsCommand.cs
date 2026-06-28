using System;
using Robust.Shared;
using Robust.Shared.Serialization;

namespace Robust.Shared.Console.Commands;

internal sealed partial class NetSizeStatsCommand : LocalizedCommands
{
    public override string Command => "net_size_stats";

    public override string Description => Loc.GetString("cmd-net-size-stats-desc");

    public override string Help => Loc.GetString("cmd-net-size-stats-help", ("command", Command));

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length > 0 && args[0].Equals("clear", StringComparison.OrdinalIgnoreCase))
        {
            NetSizeStats.Clear();
            shell.WriteLine(Loc.GetString("cmd-net-size-stats-cleared"));
            return;
        }

        var limit = 100;
        if (args.Length > 0 && (!int.TryParse(args[0], out limit) || limit < 1))
        {
            shell.WriteLine(Loc.GetString("cmd-net-size-stats-invalid-limit"));
            return;
        }

        shell.WriteLine(Loc.GetString(
            "cmd-net-size-stats-status",
            ("status", Loc.GetString(NetSizeStats.Enabled
                ? "cmd-net-size-stats-status-enabled"
                : "cmd-net-size-stats-status-disabled"))));
        shell.WriteLine(Loc.GetString("cmd-net-size-stats-enable", ("cvar", CVars.NetMessageSizeStats.Name)));

        var stats = NetSizeStats.Snapshot();
        if (stats.Length == 0)
        {
            shell.WriteLine(Loc.GetString("cmd-net-size-stats-empty"));
            return;
        }

        foreach (var stat in stats[..Math.Min(limit, stats.Length)])
        {
            var unit = stat.Kind == NetSizeStatKind.MemberCount
                ? Loc.GetString("cmd-net-size-stats-unit-count")
                : Loc.GetString("cmd-net-size-stats-unit-bytes");
            shell.WriteLine($"{stat.Kind,-11} {stat.Value,8} {unit,-5} {stat.Count,8}x {stat.Name}");
        }
    }
}
