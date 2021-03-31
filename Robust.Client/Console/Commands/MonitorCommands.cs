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
