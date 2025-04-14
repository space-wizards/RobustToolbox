using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Robust.Shared.Collections;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Robust.Shared.Placement;

public sealed class SharedPlacementSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedMapSystem _maps = default!;
    [Dependency] private SharedTransformSystem _xformSystem = default!;

    //TO-DO: Expand for multiple permission per mob?
    //       Add support for multi-use placeables (tiles etc.).
    public List<PlacementInformation> BuildPermissions { get; set; } = new();

    //Holds build permissions for all mobs. A list of mobs and the objects they're allowed to request and how. One permission per mob.

    public Func<EntitySessionEventArgs, bool>? AllowPlacementFunc { get; set; }

    public override void Initialize()
    {
        base.Initialize();
        SubscribeAllEvent<MsgPlacement>(HandleNetMessage);
    }

    /// <summary>
    ///  Handles placement related client messages.
    /// </summary>
    public void HandleNetMessage(MsgPlacement msg, EntitySessionEventArgs args)
    {
        // TODO: Command check
        if (AllowPlacementFunc != null && !AllowPlacementFunc(args))
        {
            return;
        }

        switch (msg.PlaceType)
        {
            case PlacementManagerMessage.StartPlacement:
                break;
            case PlacementManagerMessage.CancelPlacement:
                break;
            case PlacementManagerMessage.RequestPlacement:
                HandlePlacementRequest(msg, args);
                break;
            case PlacementManagerMessage.RequestEntRemove:
                HandleEntRemoveReq(msg, args);
                break;
            case PlacementManagerMessage.RequestRectRemove:
                HandleRectRemoveReq(msg, args);
                break;
        }
    }

    public void HandlePlacementRequest(MsgPlacement msg, EntitySessionEventArgs args)
    {
        var isTile = msg.IsTile;

        int tileType = 0;
        var entityTemplateName = "";

        if (isTile) tileType = msg.TileType;
        else entityTemplateName = msg.EntityTemplateName;

        var dirRcv = msg.DirRcv;

        var session = args.SenderSession;
        var plyEntity = CompOrNull<TransformComponent>(session.AttachedEntity);

        // Don't have an entity, don't get to place.
        if (plyEntity == null)
            return;

        //TODO: Distance check, so you can't place things off of screen.
        // I don't think that's this manager's biggest problem

        var netCoordinates = msg.NetCoordinates;
        var coordinates = GetCoordinates(netCoordinates);

        if (!coordinates.IsValid(EntityManager))
        {
            Log.Warning($"{session} tried to place {msg.ObjType} at invalid coordinate {coordinates}");
            return;
        }

        /* TODO: Redesign permission system, or document what this is supposed to be doing
        var permission = GetPermission(session.attachedEntity.Uid, alignRcv);
        if (permission == null)
            return;

        if (permission.Uses > 0)
        {
            permission.Uses--;
            if (permission.Uses <= 0)
            {
                BuildPermissions.Remove(permission);
                SendPlacementCancel(session.attachedEntity);
            }
        }
        else
        {
            BuildPermissions.Remove(permission);
            SendPlacementCancel(session.attachedEntity);
            return;
        }
        */
        if (!isTile)
        {
            // Replace existing entities if relevant.
            if (msg.Replacement && _prototype.Index<EntityPrototype>(entityTemplateName).Components.TryGetValue(
                    _factory.GetComponentName(typeof(PlacementReplacementComponent)), out var compRegistry))
            {
                var key = ((PlacementReplacementComponent)compRegistry.Component).Key;
                var gridUid = _xformSystem.GetGrid(coordinates);

                if (TryComp(gridUid, out MapGridComponent? grid))
                {
                    var replacementQuery = GetEntityQuery<PlacementReplacementComponent>();
                    var anc = _maps.GetAnchoredEntitiesEnumerator(gridUid.Value, grid, _maps.LocalToTile(gridUid.Value, grid, coordinates));
                    var toDelete = new ValueList<EntityUid>();

                    while (anc.MoveNext(out var ent))
                    {
                        if (!replacementQuery.TryGetComponent(ent, out var repl) ||
                            repl.Key != key)
                        {
                            continue;
                        }

                        toDelete.Add(ent.Value);
                    }

                    foreach (var ent in toDelete)
                    {
                        var placementEraseEvent = new PlacementEntityEvent(ent, coordinates, PlacementEventAction.Erase, args.SenderSession.UserId);
                        RaiseLocalEvent(placementEraseEvent);
                        PredictedDel(ent);
                    }
                }
            }

            var created = PredictedSpawnAttachedTo(entityTemplateName, coordinates, rotation: dirRcv.ToAngle());

            var placementCreateEvent = new PlacementEntityEvent(created, coordinates, PlacementEventAction.Create, args.SenderSession.UserId);
            RaiseLocalEvent(placementCreateEvent);
        }
        else
        {
            PlaceNewTile(tileType, coordinates, args.SenderSession.UserId);
        }
    }

    private void PlaceNewTile(int tileType, EntityCoordinates coordinates, NetUserId placingUserId)
    {
        if (!coordinates.IsValid(EntityManager))
            return;

        MapGridComponent? grid;

        EntityUid gridId = coordinates.EntityId;
        if (TryComp(coordinates.EntityId, out grid)
            || _mapManager.TryFindGridAt(_xformSystem.ToMapCoordinates(coordinates), out gridId, out grid))
        {
            _maps.SetTile(gridId, grid, coordinates, new Tile(tileType));

            var placementEraseEvent = new PlacementTileEvent(tileType, coordinates, placingUserId);
            RaiseLocalEvent(placementEraseEvent);
        }
        else if (tileType != 0) // create a new grid
        {
            var newGrid = _mapManager.CreateGridEntity(_xformSystem.GetMapId(coordinates));
            var newGridXform = new Entity<TransformComponent>(
                newGrid.Owner,
                Transform(newGrid));

            _xformSystem.SetWorldPosition(newGridXform, coordinates.Position - newGrid.Comp.TileSizeHalfVector); // assume bottom left tile origin
            var tilePos = _maps.WorldToTile(newGrid.Owner, newGrid.Comp, coordinates.Position);
            _maps.SetTile(newGrid.Owner, newGrid.Comp, tilePos, new Tile(tileType));

            var placementEraseEvent = new PlacementTileEvent(tileType, coordinates, placingUserId);
            RaiseLocalEvent(placementEraseEvent);
        }
    }

    private void HandleEntRemoveReq(MsgPlacement msg, EntitySessionEventArgs args)
    {
        //TODO: Some form of admin check
        var entity = GetEntity(msg.EntityUid);

        if (!Exists(entity))
            return;

        var placementEraseEvent = new PlacementEntityEvent(entity, Transform(entity).Coordinates, PlacementEventAction.Erase, args.SenderSession.UserId);
        RaiseLocalEvent(placementEraseEvent);
        PredictedDel(entity);
    }

    private void HandleRectRemoveReq(MsgPlacement msg, EntitySessionEventArgs args)
    {
        EntityCoordinates start = GetCoordinates(msg.NetCoordinates);
        Vector2 rectSize = msg.RectSize;

        foreach (var entity in _lookup.GetEntitiesIntersecting(_xformSystem.GetMapId(start),
            new Box2(start.Position, start.Position + rectSize)))
        {
            if (Deleted(entity) ||
                HasComp<MapGridComponent>(entity) ||
                HasComp<ActorComponent>(entity))
            {
                continue;
            }

            var placementEraseEvent = new PlacementEntityEvent(entity, Transform(entity).Coordinates, PlacementEventAction.Erase, args.SenderSession.UserId);
            RaiseLocalEvent(placementEraseEvent);
            PredictedDel(entity);
        }
    }

    /// <summary>
    ///  Places mob in entity placement mode with given settings.
    /// </summary>
    public void SendPlacementBegin(EntityUid mob, int range, string objectType, string alignOption)
    {
        if (!TryComp(mob, out ActorComponent? actor))
            return;

        var message = new MsgPlacement
        {
            PlaceType = PlacementManagerMessage.StartPlacement,
            Range = range,
            IsTile = false,
            ObjType = objectType,
            AlignOption = alignOption
        };

        RaiseNetworkEvent(message, actor.PlayerSession);
    }

    /// <summary>
    ///  Places mob in tile placement mode with given settings.
    /// </summary>
    public void SendPlacementBeginTile(EntityUid mob, int range, string tileType, string alignOption)
    {
        if (!TryComp(mob, out ActorComponent? actor))
            return;

        var message = new MsgPlacement
        {
            PlaceType = PlacementManagerMessage.StartPlacement,
            Range = range,
            IsTile = true,
            ObjType = tileType,
            AlignOption = alignOption
        };

        RaiseNetworkEvent(message, actor.PlayerSession);
    }

    /// <summary>
    ///  Cancels object placement mode for given mob.
    /// </summary>
    public void SendPlacementCancel(EntityUid mob)
    {
        if (!TryComp(mob, out ActorComponent? actor))
            return;

        var message = new MsgPlacement
        {
            PlaceType = PlacementManagerMessage.CancelPlacement
        };

        RaiseNetworkEvent(message, actor.PlayerSession);
    }

    /// <summary>
    ///  Gives Mob permission to place entity and places it in object placement mode.
    /// </summary>
    public void StartBuilding(EntityUid mob, int range, string objectType, string alignOption)
    {
        AssignBuildPermission(mob, range, objectType, alignOption);
        SendPlacementBegin(mob, range, objectType, alignOption);
    }

    /// <summary>
    ///  Gives Mob permission to place tile and places it in object placement mode.
    /// </summary>
    public void StartBuildingTile(EntityUid mob, int range, string tileType, string alignOption)
    {
        AssignBuildPermission(mob, range, tileType, alignOption);
        SendPlacementBeginTile(mob, range, tileType, alignOption);
    }

    /// <summary>
    ///  Revokes open placement Permission and cancels object placement mode.
    /// </summary>
    public void CancelBuilding(EntityUid mob)
    {
        RevokeAllBuildPermissions(mob);
        SendPlacementCancel(mob);
    }

    /// <summary>
    ///  Gives a mob a permission to place a given Entity.
    /// </summary>
    public void AssignBuildPermission(EntityUid mob, int range, string objectType, string alignOption)
    {
        var newPermission = new PlacementInformation
        {
            MobUid = mob,
            Range = range,
            IsTile = false,
            EntityType = objectType,
            PlacementOption = alignOption
        };

        IEnumerable<PlacementInformation> mobPermissions = from PlacementInformation permission in BuildPermissions
                                                           where permission.MobUid == mob
                                                           select permission;

        if (mobPermissions.Any()) //Already has one? Revoke the old one and add this one.
        {
            RevokeAllBuildPermissions(mob);
            BuildPermissions.Add(newPermission);
        }
        else
        {
            BuildPermissions.Add(newPermission);
        }
    }

    /// <summary>
    ///  Gives a mob a permission to place a given Tile.
    /// </summary>
    public void AssignBuildPermissionTile(EntityUid mob, int range, string tileType, string alignOption)
    {
        var newPermission = new PlacementInformation
        {
            MobUid = mob,
            Range = range,
            IsTile = true,
            TileType = _tileDefinitionManager[tileType].TileId,
            PlacementOption = alignOption
        };

        IEnumerable<PlacementInformation> mobPermissions = from PlacementInformation permission in BuildPermissions
                                                           where permission.MobUid == mob
                                                           select permission;

        if (mobPermissions.Any()) //Already has one? Revoke the old one and add this one.
        {
            RevokeAllBuildPermissions(mob);
            BuildPermissions.Add(newPermission);
        }
        else
        {
            BuildPermissions.Add(newPermission);
        }
    }

    /// <summary>
    ///  Removes all building Permissions for given mob.
    /// </summary>
    public void RevokeAllBuildPermissions(EntityUid mob)
    {
        var mobPermissions = BuildPermissions
            .Where(permission => permission.MobUid == mob)
            .ToList();

        if (mobPermissions.Count != 0)
            BuildPermissions.RemoveAll(x => mobPermissions.Contains(x));
    }

    private PlacementInformation? GetPermission(EntityUid uid, string alignOpt)
    {
        foreach (var buildPermission in BuildPermissions)
        {
            if (buildPermission.MobUid == uid && buildPermission.PlacementOption == alignOpt)
            {
                return buildPermission;
            }
        }

        return null;
    }
}
