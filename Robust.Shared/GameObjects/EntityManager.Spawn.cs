using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Robust.Shared.Containers;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects;

public partial class EntityManager
{
    // This method will soon(TM) be marked as obsolete.
    public EntityUid SpawnEntity(string? protoName, EntityCoordinates coordinates, ComponentRegistry? overrides = null)
        => SpawnAttachedTo(protoName, coordinates, overrides);

    // This method will soon(TM) be marked as obsolete.
    public EntityUid SpawnEntity(string? protoName, MapCoordinates coordinates, ComponentRegistry? overrides = null)
        => Spawn(protoName, coordinates, overrides);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityUid[] SpawnEntitiesAttachedTo(EntityCoordinates coordinates, params string?[] protoNames)
    {
        var ents = new EntityUid[protoNames.Length];
        for (var i = 0; i < protoNames.Length; i++)
        {
            ents[i] = SpawnAttachedTo(protoNames[i], coordinates);
        }
        return ents;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityUid[] SpawnEntities(MapCoordinates coordinates, params string?[] protoNames)
    {
        var ents = new EntityUid[protoNames.Length];
        for (var i = 0; i < protoNames.Length; i++)
        {
            ents[i] = Spawn(protoNames[i], coordinates);
        }
        return ents;
    }

    public EntityUid[] SpawnEntities(MapCoordinates coordinates, string? prototype, int count)
    {
        var ents = new EntityUid[count];
        for (var i = 0; i < count; i++)
        {
            ents[i] = Spawn(prototype, coordinates);
        }
        return ents;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityUid[] SpawnEntitiesAttachedTo(EntityCoordinates coordinates, List<string?> protoNames)
    {
        var ents = new EntityUid[protoNames.Count];
        for (var i = 0; i < protoNames.Count; i++)
        {
            ents[i] = SpawnAttachedTo(protoNames[i], coordinates);
        }
        return ents;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityUid[] SpawnEntities(MapCoordinates coordinates, List<string?> protoNames)
    {
        var ents = new EntityUid[protoNames.Count];
        for (var i = 0; i < protoNames.Count; i++)
        {
            ents[i] = Spawn(protoNames[i], coordinates);
        }
        return ents;
    }

    public virtual EntityUid SpawnAttachedTo(string? protoName, EntityCoordinates coordinates, ComponentRegistry? overrides = null, Angle rotation = default)
    {
        if (!coordinates.IsValid(this))
            throw new InvalidOperationException($"Tried to spawn entity {protoName} on invalid coordinates {coordinates}.");

        var entity = CreateEntityUninitialized(protoName, coordinates, overrides, rotation);
        InitializeAndStartEntity(entity, _xforms.GetMapId(coordinates));
        return entity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityUid Spawn(string? protoName = null, ComponentRegistry? overrides = null, bool doMapInit = true)
    {
        var entity = CreateEntityUninitialized(protoName, MapCoordinates.Nullspace, overrides);
        InitializeAndStartEntity(entity, doMapInit);
        return entity;
    }

    public virtual EntityUid Spawn(string? protoName, MapCoordinates coordinates, ComponentRegistry? overrides = null, Angle rotation = default!)
    {
        var entity = CreateEntityUninitialized(protoName, coordinates, overrides, rotation);
        InitializeAndStartEntity(entity, coordinates.MapId);
        return entity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityUid SpawnAtPosition(string? protoName, EntityCoordinates coordinates, ComponentRegistry? overrides = null)
        => Spawn(protoName, _xforms.ToMapCoordinates(coordinates), overrides);

    public bool TrySpawnNextTo(
        string? protoName,
        EntityUid target,
        [NotNullWhen(true)] out EntityUid? uid,
        TransformComponent? xform = null,
        ComponentRegistry? overrides = null)
    {
        uid = null;
        if (!TransformQuery.Resolve(target, ref xform))
            return false;

        if (!xform.ParentUid.IsValid())
            return false;

        if (!_containers.TryGetContainingContainer(target, out var container))
        {
            uid = SpawnNextToOrDrop(protoName, target, xform, overrides);
            return true;
        }

        var doMapInit = _mapSystem.IsInitialized(xform.MapUid);
        uid = Spawn(protoName, overrides, doMapInit);
        if (_containers.Insert(uid.Value, container))
            return true;

        DeleteEntity(uid.Value);
        uid = null;
        return false;
    }

    public bool TrySpawnInContainer(
        string? protoName,
        EntityUid containerUid,
        string containerId,
        [NotNullWhen(true)] out EntityUid? uid,
        ContainerManagerComponent? containerComp = null,
        ComponentRegistry? overrides = null)
    {
        uid = null;
        if (containerComp == null && !TryGetComponent(containerUid, out containerComp))
            return false;

        if (!containerComp.Containers.TryGetValue(containerId, out var container))
            return false;

        var doMapInit = _mapSystem.IsInitialized(TransformQuery.GetComponent(containerUid).MapUid);
        uid = Spawn(protoName, overrides, doMapInit);

        if (_containers.Insert(uid.Value, container))
            return true;

        DeleteEntity(uid.Value);
        uid = null;
        return false;
    }

    public EntityUid SpawnNextToOrDrop(string? protoName, EntityUid target, TransformComponent? xform = null, ComponentRegistry? overrides = null)
    {
        xform ??= TransformQuery.GetComponent(target);
        if (!xform.ParentUid.IsValid())
            return Spawn(protoName);

        var doMapInit = _mapSystem.IsInitialized(xform.MapUid);
        var uid = Spawn(protoName, overrides, doMapInit);
        _xforms.DropNextTo(uid, target);
        return uid;
    }

    public EntityUid SpawnInContainerOrDrop(
        string? protoName,
        EntityUid containerUid,
        string containerId,
        TransformComponent? xform = null,
        ContainerManagerComponent? containerComp = null,
        ComponentRegistry? overrides = null)
    {
        return SpawnInContainerOrDrop(protoName, containerUid, containerId, out _, xform, containerComp, overrides);
    }

    public EntityUid SpawnInContainerOrDrop(
        string? protoName,
        EntityUid containerUid,
        string containerId,
        out bool inserted,
        TransformComponent? xform = null,
        ContainerManagerComponent? containerComp = null,
        ComponentRegistry? overrides = null)
    {
        inserted = true;
        xform ??= TransformQuery.GetComponent(containerUid);
        var doMapInit = _mapSystem.IsInitialized(xform.MapUid);
        var uid = Spawn(protoName, overrides, doMapInit);

        if ((containerComp == null && !TryGetComponent(containerUid, out containerComp))
             || !containerComp.Containers.TryGetValue(containerId, out var container)
             || !_containers.Insert(uid, container))
        {
            inserted = false;
            if (xform.ParentUid.IsValid())
                _xforms.DropNextTo(uid, (containerUid, xform));
        }

        return uid;
    }

    #region Prediction

    public virtual EntityUid PredictedSpawnAttachedTo(string? protoName, EntityCoordinates coordinates, ComponentRegistry? overrides = null, Angle rotation = default)
    {
        return SpawnAttachedTo(protoName, coordinates, overrides, rotation);
    }

    public virtual EntityUid PredictedSpawn(string? protoName = null, ComponentRegistry? overrides = null, bool doMapInit = true)
    {
        return Spawn(protoName, overrides, doMapInit);
    }

    public virtual EntityUid PredictedSpawn(string? protoName, MapCoordinates coordinates, ComponentRegistry? overrides = null, Angle rotation = default!)
    {
        return Spawn(protoName, coordinates, overrides, rotation);
    }

    public virtual EntityUid PredictedSpawnAtPosition(string? protoName, EntityCoordinates coordinates, ComponentRegistry? overrides = null)
    {
        return SpawnAtPosition(protoName, coordinates, overrides);
    }

    public virtual bool PredictedTrySpawnNextTo(
        string? protoName,
        EntityUid target,
        [NotNullWhen(true)] out EntityUid? uid,
        TransformComponent? xform = null,
        ComponentRegistry? overrides = null)
    {
        return TrySpawnNextTo(protoName, target, out uid, xform, overrides);
    }

    public virtual bool PredictedTrySpawnInContainer(
        string? protoName,
        EntityUid containerUid,
        string containerId,
        [NotNullWhen(true)] out EntityUid? uid,
        ContainerManagerComponent? containerComp = null,
        ComponentRegistry? overrides = null)
    {
        return TrySpawnInContainer(protoName, containerUid, containerId, out uid, containerComp, overrides);
    }

    public virtual EntityUid PredictedSpawnNextToOrDrop(string? protoName, EntityUid target, TransformComponent? xform = null, ComponentRegistry? overrides = null)
    {
        return SpawnNextToOrDrop(protoName, target, xform, overrides);
    }

    public virtual EntityUid PredictedSpawnInContainerOrDrop(
        string? protoName,
        EntityUid containerUid,
        string containerId,
        TransformComponent? xform = null,
        ContainerManagerComponent? containerComp = null,
        ComponentRegistry? overrides = null)
    {
        return SpawnInContainerOrDrop(protoName, containerUid, containerId, xform, containerComp, overrides);
    }

    public virtual EntityUid PredictedSpawnInContainerOrDrop(
        string? protoName,
        EntityUid containerUid,
        string containerId,
        out bool inserted,
        TransformComponent? xform = null,
        ContainerManagerComponent? containerComp = null,
        ComponentRegistry? overrides = null)
    {
        return SpawnInContainerOrDrop(protoName,
            containerUid,
            containerId,
            out inserted,
            xform,
            containerComp,
            overrides);
    }

    /// <summary>
    /// Flags an entity as being a predicted spawn and should be deleted when its corresponding tick comes in.
    /// </summary>
    public virtual void FlagPredicted(Entity<MetaDataComponent?> ent)
    {

    }

    #endregion
}
