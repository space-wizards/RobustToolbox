using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Robust.Shared.Map.Commands;

/// <summary>
/// Sets the ambient light for a particular map
/// </summary>
public sealed class AmbientLightCommand : IConsoleCommand
{
    public string Command => $"setambientlight";
    public string Description => $"cmd-set-ambient-light-desc";
    public string Help => $"cmd-set-ambient-light-help";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 5)
        {
            shell.WriteError("cmd-invalid-arg-number-error");
            return;
        }

        var mapManager = IoCManager.Resolve<IMapManager>();

        if (!int.TryParse(args[0], out var mapInt))
        {
            shell.WriteError($"cmd-parse-failure-integer");
            return;
        }

        var mapId = new MapId(mapInt);

        if (!mapManager.MapExists(mapId))
        {
            shell.WriteError($"cmd-parse-failure-mapid");
            return;
        }

        if (!byte.TryParse(args[1], out var r) ||
            !byte.TryParse(args[2], out var g) ||
            !byte.TryParse(args[3], out var b) ||
            !byte.TryParse(args[4], out var a))
        {
            shell.WriteError($"cmd-set-ambient-light-parse");
            return;
        }

        var color = Color.FromSrgb(new Color(r, g, b, a));
        var mapSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SharedMapSystem>();
        mapSystem.SetAmbientLight(mapId, color);
    }
}
