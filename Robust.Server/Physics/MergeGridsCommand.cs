using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed;

namespace Robust.Server.Physics;

public sealed class MergeGridsCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public override string Command => "merge_grids";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 4)
        {
            return;
        }

        if (!NetEntity.TryParse(args[0], out var gridANet) ||
            !NetEntity.TryParse(args[1], out var gridBNet))
        {
            return;
        }

        var gridAUid = _entManager.GetEntity(gridANet);
        var gridBUid = _entManager.GetEntity(gridBNet);

        if (!_entManager.TryGetComponent<MapGridComponent>(gridAUid, out var gridA) ||
            !_entManager.TryGetComponent<MapGridComponent>(gridBUid, out var gridB))
        {
            return;
        }

        if (!int.TryParse(args[2], out var x) ||
            !int.TryParse(args[3], out var y))
        {
            return;
        }

        Angle rotation = Angle.Zero;

        if (args.Length >= 5 && int.TryParse(args[4], out var rotationInt))
        {
            rotation = Angle.FromDegrees(rotationInt);
        }

        var offset = new Vector2i(x, y);
        var fixtureSystem = _entManager.System<GridFixtureSystem>();
        fixtureSystem.Merge(gridAUid, gridBUid, offset, rotation, gridA: gridA, gridB: gridB);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(CompletionHelper.Components<MapGridComponent>(args[0], _entManager), Loc.GetString("cmd-merge_grids-hintA")),
            2 => CompletionResult.FromHintOptions(CompletionHelper.Components<MapGridComponent>(args[1], _entManager), Loc.GetString("cmd-merge_grids-hintB")),
            3 => CompletionResult.FromHint(Loc.GetString("cmd-merge_grids-xOffset")),
            4 => CompletionResult.FromHint(Loc.GetString("cmd-merge_grids-yOffset")),
            5 => CompletionResult.FromHint(Loc.GetString("cmd-merge_grids-angle")),
            _ => CompletionResult.Empty
        };
    }
}
