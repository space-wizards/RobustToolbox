using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

namespace Robust.Shared.Map.Commands;

/// <summary>
/// Sets the ambient light for a particular map
/// </summary>
public sealed class AmbientLightCommand : IConsoleCommand
{
    [Dependency] private readonly IEntitySystemManager _systems = default!;

    public string Command => $"setambientlight";
    public string Description => Loc.GetString("cmd-set-ambient-light-desc");
    public string Help => Loc.GetString("cmd-set-ambient-light-help");
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 5)
        {
            shell.WriteError(Loc.GetString("cmd-invalid-arg-number-error"));
            return;
        }

        if (!int.TryParse(args[0], out var mapInt))
        {
            shell.WriteError(Loc.GetString("cmd-parse-failure-integer"));
            return;
        }

        var mapId = new MapId(mapInt);
        var mapSystem = _systems.GetEntitySystem<SharedMapSystem>();

        if (!mapSystem.MapExists(mapId))
        {
            shell.WriteError(Loc.GetString("cmd-parse-failure-mapid", ("arg", mapId.Value)));
            return;
        }

        if (!byte.TryParse(args[1], out var r) ||
            !byte.TryParse(args[2], out var g) ||
            !byte.TryParse(args[3], out var b) ||
            !byte.TryParse(args[4], out var a))
        {
            shell.WriteError(Loc.GetString("cmd-set-ambient-light-parse"));
            return;
        }

        var color = Color.FromSrgb(new Color(r, g, b, a));
        mapSystem.SetAmbientLight(mapId, color);
    }
}
