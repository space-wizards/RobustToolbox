using System.Linq;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Shared.Console;
using Robust.Shared.IoC;

namespace Robust.Client.Console.Commands
{
    [UsedImplicitly]
    public sealed class LsMonitorCommand : IConsoleCommand
    {
        public string Command => "lsmonitor";
        public string Description => "";
        public string Help => "";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var clyde = IoCManager.Resolve<IClyde>();

            foreach (var monitor in clyde.EnumerateMonitors())
            {
                shell.WriteLine(
                    $"[{monitor.Id}] {monitor.Name}: {monitor.Size.X}x{monitor.Size.Y}@{monitor.RefreshRate}Hz");
            }
        }
    }

    [UsedImplicitly]
    public sealed class MonitorInfoCommand : IConsoleCommand
    {
        public string Command => "monitorinfo";
        public string Description => "";
        public string Help => "Usage: monitorinfo <id>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 1)
            {
                shell.WriteError("Expected one argument.");
                return;
            }

            var clyde = IoCManager.Resolve<IClyde>();
            var monitor = clyde.EnumerateMonitors().Single(c => c.Id == int.Parse(args[0]));

            shell.WriteLine($"{monitor.Id}: {monitor.Name}");
            shell.WriteLine($"Video modes:");

            foreach (var mode in monitor.VideoModes)
            {
                shell.WriteLine($"  {mode.Width}x{mode.Height} {mode.RefreshRate} Hz {mode.RedBits}/{mode.GreenBits}/{mode.BlueBits}");
            }
        }
    }

    [UsedImplicitly]
    public sealed class SetMonitorCommand : IConsoleCommand
    {
        public string Command => "setmonitor";
        public string Description => "";
        public string Help => "Usage: setmonitor <id>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var clyde = IoCManager.Resolve<IClyde>();

            var id = int.Parse(args[0]);
            var monitor = clyde.EnumerateMonitors().Single(m => m.Id == id);
            clyde.SetWindowMonitor(monitor);
        }
    }
}
