using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Robust.Shared.Console.Commands;

internal sealed class TeleportCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;

    public override string Command => "tp";
    public override bool RequireServerOrSingleplayer => true;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { AttachedEntity: { } entity })
            return;

        if (args.Length < 2 || !float.TryParse(args[0], out var posX) || !float.TryParse(args[1], out var posY))
        {
            shell.WriteError(Help);
            return;
        }

        var transform = _entityManager.GetComponent<TransformComponent>(entity);
        var position = new Vector2(posX, posY);

        _transform.AttachToGridOrMap(entity, transform);

        MapId mapId;
        if (args.Length == 3 && int.TryParse(args[2], out var intMapId))
            mapId = new MapId(intMapId);
        else
            mapId = transform.MapID;

        if (!_mapSystem.MapExists(mapId))
        {
            shell.WriteError($"Map {mapId} doesn't exist!");
            return;
        }

        if (_map.TryFindGridAt(mapId, position, out var gridUid, out var grid))
        {
            var gridPos = Vector2.Transform(position, _transform.GetInvWorldMatrix(gridUid));

            _transform.SetCoordinates(entity, transform, new EntityCoordinates(gridUid, gridPos));
        }
        else
        {
            if (_mapSystem.TryGetMap(mapId, out var mapEnt))
            {
                _transform.SetWorldPosition((entity, transform), position);
                _transform.SetParent(entity, transform, mapEnt.Value);
            }
        }

        shell.WriteLine($"Teleported {shell.Player} to {mapId}:{posX},{posY}.");
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHint("<x>"),
            2 => CompletionResult.FromHint("<y>"),
            3 => CompletionResult.FromHintOptions(CompletionHelper.MapIds(_entityManager), "[MapId]"),
            _ => CompletionResult.Empty
        };
    }
}

public sealed class TeleportToCommand : LocalizedEntityCommands
{
    [Dependency] private readonly ISharedPlayerManager _players = default!;
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override string Command => "tpto";
    public override bool RequireServerOrSingleplayer => true;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
            return;

        var target = args[0];

        if (!TryGetTransformFromUidOrUsername(target, shell, out var targetUid, out _))
            return;

        var targetCoords = new EntityCoordinates(targetUid.Value, Vector2.Zero);

        if (_entities.TryGetComponent(targetUid, out PhysicsComponent? targetPhysics))
        {
            targetCoords = targetCoords.Offset(targetPhysics.LocalCenter);
        }

        var victims = new List<(EntityUid Entity, TransformComponent Transform)>();

        if (args.Length == 1)
        {
            var ent = shell.Player?.AttachedEntity;

            if (!_entities.TryGetComponent(ent, out TransformComponent? playerTransform))
            {
                shell.WriteError(Loc.GetString("cmd-failure-no-attached-entity"));
                return;
            }

            victims.Add((ent.Value, playerTransform));
        }
        else
        {
            foreach (var victim in args)
            {
                if (victim == target)
                    continue;

                if (!TryGetTransformFromUidOrUsername(victim, shell, out var uid, out var victimTransform))
                    continue;

                victims.Add((uid.Value, victimTransform));
            }
        }

        foreach (var victim in victims)
        {
            _transform.SetCoordinates(victim.Entity, targetCoords);
            _transform.AttachToGridOrMap(victim.Entity, victim.Transform);
        }
    }

    private bool TryGetTransformFromUidOrUsername(
        string str,
        IConsoleShell shell,
        [NotNullWhen(true)] out EntityUid? victimUid,
        [NotNullWhen(true)] out TransformComponent? transform)
    {
        if (NetEntity.TryParse(str, out var uidNet)
            && _entities.TryGetEntity(uidNet, out var uid)
            && _entities.TryGetComponent(uid, out transform)
            && !_entities.HasComponent<MapComponent>(uid))
        {
            victimUid = uid;
            return true;
        }

        if (_players.TryGetSessionByUsername(str, out var session)
            && _entities.TryGetComponent(session.AttachedEntity, out transform))
        {
            victimUid = session.AttachedEntity;
            return true;
        }

        shell.WriteError(Loc.GetString("cmd-tpto-parse-error", ("str",str)));

        transform = null;
        victimUid = default;
        return false;
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 0)
            return CompletionResult.Empty;

        var last = args[^1];

        var users = _players.Sessions
            .Select(x => x.Name ?? string.Empty)
            .Where(x => !string.IsNullOrWhiteSpace(x) && x.StartsWith(last, StringComparison.CurrentCultureIgnoreCase));

        var hint = args.Length == 1 ? "cmd-tpto-destination-hint" : "cmd-tpto-victim-hint";
        hint = Loc.GetString(hint);
        return CompletionResult.FromHintOptions(users, hint);
    }
}

sealed class LocationCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IEntityManager _ent = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override string Command => "loc";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { AttachedEntity: { } entity })
            return;

        var pt = _ent.GetComponent<TransformComponent>(entity);
        var pos = pt.Coordinates;

        var mapId = _transform.GetMapId(pos);
        var gridUid = _transform.GetGrid(pos);

        shell.WriteLine($"MapID:{mapId} GridUid:{gridUid} X:{pos.X:N2} Y:{pos.Y:N2}");
    }
}

sealed class TpGridCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IEntityManager _ent = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;

    public override string Command => "tpgrid";
    public override bool RequireServerOrSingleplayer => true;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length is < 3 or > 4)
        {
            shell.WriteError(Loc.GetString("cmd-invalid-arg-number-error"));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var gridIdNet))
        {
            shell.WriteError(Loc.GetString("cmd-parse-failure-uid", ("arg", args[0])));
            return;
        }

        if (!_ent.TryGetEntity(gridIdNet, out var uid)
            || !_ent.HasComponent<MapGridComponent>(uid)
            || _ent.HasComponent<MapComponent>(uid))
        {
            shell.WriteError(Loc.GetString("cmd-parse-failure-grid", ("arg", args[0])));
            return;
        }

        var xPos = float.Parse(args[1], CultureInfo.InvariantCulture);
        var yPos = float.Parse(args[2], CultureInfo.InvariantCulture);

        var gridXform = _ent.GetComponent<TransformComponent>(uid.Value);

        var mapId = gridXform.MapID;

        if (args.Length > 3)
        {
            if (!int.TryParse(args[3], out var map))
            {
                shell.WriteError(Loc.GetString("cmd-parse-failure-mapid", ("arg", args[3])));
                return;
            }

            mapId = new MapId(map);
        }

        var id = _map.GetMap(mapId);
        if (id == EntityUid.Invalid)
        {
            shell.WriteError(Loc.GetString("cmd-parse-failure-mapid", ("arg", mapId.Value)));
            return;
        }

        var pos = new EntityCoordinates(id, new Vector2(xPos, yPos));
        _ent.System<SharedTransformSystem>().SetCoordinates(uid.Value, pos);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(CompletionHelper.Components<MapGridComponent>(args[^1], _ent), "<GridUid>"),
            2 => CompletionResult.FromHint("<x>"),
            3 => CompletionResult.FromHint("<y>"),
            4 => CompletionResult.FromHintOptions(CompletionHelper.MapIds(_ent), "[MapId]"),
            _ => CompletionResult.Empty
        };
    }
}
