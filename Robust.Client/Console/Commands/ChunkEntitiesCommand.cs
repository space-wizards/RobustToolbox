using System.Globalization;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Robust.Client.Console.Commands;

/// <summary>
/// Dumps the relevant chunk entities near the client.
/// </summary>
internal sealed partial class ChunkEntitiesCommand : LocalizedCommands
{
    [Dependency] private EntityManager _entities = default!;
    [Dependency] private IEyeManager _eye = default!;

    public override string Command => "chunkentities";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        switch (args.Length)
        {
            case 0:
                ExecuteViewport(shell);
                return;
            case 4:
                ExecuteRange(shell, args);
                return;
            default:
                shell.WriteLine(Help);
                return;
        }
    }

    private void ExecuteRange(IConsoleShell shell, string[] args)
    {
        if (!NetEntity.TryParse(args[0], out var rootNet) ||
            !_entities.TryGetEntity(rootNet, out var root) ||
            !_entities.EntityExists(root))
        {
            shell.WriteError(Loc.GetString("cmd-chunkentities-error-invalid-root", ("root", args[0])));
            return;
        }

        if (!float.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ||
            !float.TryParse(args[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var y) ||
            !float.TryParse(args[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var range))
        {
            shell.WriteError(Loc.GetString("cmd-chunkentities-error-parse"));
            return;
        }

        var chunkEntity = _entities.System<ChunkEntitySystem>();
        var enumerator = chunkEntity.GetChunksInRange(root.Value, new Vector2(x, y), range);

        shell.WriteLine(Loc.GetString(
            "cmd-chunkentities-range-header",
            ("root", root.Value),
            ("x", x.ToString(CultureInfo.InvariantCulture)),
            ("y", y.ToString(CultureInfo.InvariantCulture)),
            ("range", range.ToString(CultureInfo.InvariantCulture))));
        WriteChunks(shell, enumerator);
    }

    private void ExecuteViewport(IConsoleShell shell)
    {
        var mapId = _eye.CurrentEye.Position.MapId;

        if (mapId == MapId.Nullspace)
        {
            shell.WriteError(Loc.GetString("cmd-chunkentities-error-nullspace"));
            return;
        }

        var mapSystem = _entities.System<SharedMapSystem>();
        if (!mapSystem.TryGetMap(mapId, out var mapUid))
        {
            shell.WriteError(Loc.GetString("cmd-chunkentities-error-no-map", ("map", mapId)));
            return;
        }

        var viewport = _eye.GetWorldViewport();
        var chunkEntity = _entities.System<ChunkEntitySystem>();
        var total = 0;

        shell.WriteLine(Loc.GetString("cmd-chunkentities-viewport-header", ("map", mapId), ("viewport", viewport)));
        total += WriteRootChunks(shell, mapUid.Value, viewport, chunkEntity);

        var transform = _entities.System<SharedTransformSystem>();
        var query = _entities.AllEntityQueryEnumerator<MapGridComponent, TransformComponent>();

        while (query.MoveNext(out var gridUid, out var grid, out var gridXform))
        {
            if (gridXform.MapID != mapId)
                continue;

            var localAabb = transform.GetInvWorldMatrix(gridUid).TransformBox(viewport);
            if (!grid.LocalAABB.Intersects(localAabb))
                continue;

            total += WriteRootChunks(shell, gridUid, localAabb, chunkEntity);
        }

        shell.WriteLine(Loc.GetString("cmd-chunkentities-total", ("count", total)));
    }

    private int WriteRootChunks(IConsoleShell shell, EntityUid root, Box2 localAabb, ChunkEntitySystem chunkEntity)
    {
        var enumerator = chunkEntity.GetChunksIntersecting(root, localAabb);
        var count = WriteChunks(shell, enumerator, printTotal: false);

        if (count != 0)
            shell.WriteLine(Loc.GetString("cmd-chunkentities-root-count", ("root", root), ("count", count)));

        return count;
    }

    private int WriteChunks(
        IConsoleShell shell,
        ChunkEntitySystem.ChunkEntityEnumerator enumerator,
        bool printTotal = true)
    {
        var metaQuery = _entities.GetEntityQuery<MetaDataComponent>();
        var count = 0;

        while (enumerator.MoveNext(out var chunk))
        {
            count++;
            var netEntity = _entities.GetNetEntity(chunk.Value.Owner);
            var name = metaQuery.TryComp(chunk.Value.Owner, out var meta) ? meta.EntityName : string.Empty;
            var componentCount = _entities.ComponentCount(chunk.Value.Owner);

            shell.WriteLine(
                Loc.GetString(
                    "cmd-chunkentities-entry",
                    ("netEntity", netEntity),
                    ("uid", chunk.Value.Owner),
                    ("root", chunk.Value.Comp.Root),
                    ("chunk", chunk.Value.Comp.Chunk),
                    ("componentCount", componentCount),
                    ("name", name)));
        }

        if (printTotal)
            shell.WriteLine(Loc.GetString("cmd-chunkentities-total", ("count", count)));

        return count;
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(
                CompletionHelper.NetEntities(args[0], _entities),
                Loc.GetString("cmd-chunkentities-arg-root")),
            2 => CompletionResult.FromHint(Loc.GetString("cmd-chunkentities-arg-x")),
            3 => CompletionResult.FromHint(Loc.GetString("cmd-chunkentities-arg-y")),
            4 => CompletionResult.FromHint(Loc.GetString("cmd-chunkentities-arg-range")),
            _ => CompletionResult.Empty
        };
    }
}
