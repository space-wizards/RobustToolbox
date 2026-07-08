using Robust.Shared.Console;

namespace Robust.Client.Graphics.FontManagement;

internal sealed class SystemFontDebugCommand : IConsoleCommand
{
    public string Command => "system_font_debug";
    public string Description => "";
    public string Help => "";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        new SystemFontDebugWindow().OpenCentered();
    }
}
