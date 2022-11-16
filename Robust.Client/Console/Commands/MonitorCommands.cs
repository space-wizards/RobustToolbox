using System.Linq;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Shared.Console;
using Robust.Shared.IoC;

namespace Robust.Client.Console.Commands
{
    [UsedImplicitly]
    public sealed class LsMonitorCommand : LocalizedCommands
    {
        [Dependency] private readonly IClyde _clyde = default!;

        public override string Command => "lsmonitor";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            foreach (var monitor in _clyde.EnumerateMonitors())
            {
                shell.WriteLine(
                    $"[{monitor.Id}] {monitor.Name}: {monitor.Size.X}x{monitor.Size.Y}@{monitor.RefreshRate}Hz");
            }
        }
    }

    [UsedImplicitly]
    public sealed class MonitorInfoCommand : LocalizedCommands
    {
        [Dependency] private readonly IClyde _clyde = default!;

        public override string Command => "monitorinfo";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 1)
            {
                shell.WriteError("Expected one argument.");
                return;
            }

            var monitor = _clyde.EnumerateMonitors().Single(c => c.Id == int.Parse(args[0]));

            shell.WriteLine($"{monitor.Id}: {monitor.Name}");
            shell.WriteLine($"Video modes:");

            foreach (var mode in monitor.VideoModes)
            {
                shell.WriteLine($"  {mode.Width}x{mode.Height} {mode.RefreshRate} Hz {mode.RedBits}/{mode.GreenBits}/{mode.BlueBits}");
            }
        }
    }

    [UsedImplicitly]
    public sealed class SetMonitorCommand : LocalizedCommands
    {
        [Dependency] private readonly IClyde _clyde = default!;

        public override string Command => "setmonitor";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var id = int.Parse(args[0]);
            var monitor = _clyde.EnumerateMonitors().Single(m => m.Id == id);
            _clyde.SetWindowMonitor(monitor);
        }
    }
}
