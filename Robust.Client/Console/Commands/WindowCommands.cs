using Robust.Client.Graphics;
using Robust.Shared.Console;
using Robust.Shared.IoC;

namespace Robust.Client.Console.Commands;

internal sealed class WindowCommands : IConsoleCommand
{
    [Dependency] private readonly IClyde _clyde = null!;

    public string Command => "window_maximize";
    public string Description => "";
    public string Help => "";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _clyde.MainWindow.Maximize();
    }
}
