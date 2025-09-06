#if TOOLS

using Robust.Client.Graphics;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Robust.Client.Console.Commands;

internal sealed class ViewportClearAllCachedCommand : IConsoleCommand
{
    [Dependency] private readonly IClydeInternal _clyde = default!;

    public string Command => "vp_clear_all_cached";
    public string Description => "Fires IClydeViewport.ClearCachedResources on all viewports";
    public string Help => "";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _clyde.ViewportsClearAllCached();
    }
}

internal sealed class ViewportTestFinalizeCommand : IConsoleCommand
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;

    public string Command => "vp_test_finalize";
    public string Description => "Creates a viewport, renders it once, then leaks it (finalizes it).";
    public string Help => "";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var vp = _clyde.CreateViewport(new Vector2i(1920, 1080), nameof(ViewportTestFinalizeCommand));
        vp.Eye = _eyeManager.CurrentEye;

        vp.Render();

        // Leak it.
    }
}

#endif // TOOLS
