using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

public abstract partial class SharedTransformSystem
{
    [IoC.Dependency] private readonly IGameTiming _gameTiming = default!;

    #region Anchoring

    internal void ReAnchor(TransformComponent xform,
        MapGridComponent oldGrid,
        MapGridComponent newGrid,
        Vector2i tilePos,
        TransformComponent oldGridXform,
        TransformComponent newGridXform,
        EntityQuery<TransformComponent> xformQuery)
    {
        // Bypass some of the expensive stuff in unanchoring / anchoring.
        oldGrid.Grid.RemoveFromSnapGridCell(tilePos, xform.Owner);
        newGrid.Grid.AddToSnapGridCell(tilePos, xform.Owner);
        // TODO: Could do this re-parent way better.
        // Unfortunately we don't want any anchoring events to go out hence... this.
        xform._anchored = false;
        oldGridXform._children.Remove(xform.Owner);
        newGridXform._children.Add(xform.Owner);
        xform._parent = newGrid.Owner;
        xform._anchored = true;

        SetGridId(xform, newGrid.Owner, xformQuery);
        var reParent = new EntParentChangedMessage(xform.Owner, oldGrid.Owner, xform.MapID, xform);
        RaiseLocalEvent(xform.Owner, ref reParent, true);
        // TODO: Ideally shouldn't need to call the moveevent
        var movEevee = new MoveEvent(xform.Owner,
            new EntityCoordinates(oldGrid.Owner, xform._localPosition),
            new EntityCoordinates(newGrid.Owner, xform._localPosition),
            xform.LocalRotation,
            xform.LocalRotation,
            xform,
            _gameTiming.ApplyingState);
        RaiseLocalEvent(xform.Owner, ref movEevee, true);

        DebugTools.Assert(xformQuery.GetComponent(oldGrid.Owner).MapID == xformQuery.GetComponent(newGrid.Owner).MapID);
        DebugTools.Assert(xform._anchored);

        Dirty(xform);
        var ev = new ReAnchorEvent(xform.Owner, oldGrid.Owner, newGrid.Owner, tilePos);
        RaiseLocalEvent(xform.Owner, ref ev);
    }

    public bool AnchorEntity(TransformComponent xform, IMapGrid grid, Vector2i tileIndices)
    {
        var result = grid.AddToSnapGridCell(tileIndices, xform.Owner);

        if (result)
        {
            // Mark as static first to avoid the velocity change on parent change.
            if (TryComp<PhysicsComponent>(xform.Owner, out var physicsComponent))
                physicsComponent.BodyType = BodyType.Static;

            // anchor snapping
            // Internally it will do the parent update; doing it separately just triggers a redundant move.
            xform.Coordinates = new EntityCoordinates(grid.GridEntityId, grid.GridTileToLocal(tileIndices).Position);
            xform.SetAnchored(result);
        }

        return result;
    }

    public bool AnchorEntity(TransformComponent xform, IMapGridComponent component)
    {
        return AnchorEntity(xform, component.Grid);
    }

    public bool AnchorEntity(TransformComponent xform, IMapGrid grid)
    {
        var tileIndices = grid.TileIndicesFor(xform.Coordinates);
        return AnchorEntity(xform, grid, tileIndices);
    }

    public bool AnchorEntity(TransformComponent xform)
    {
        if (!_mapManager.TryGetGrid(xform.GridUid, out var grid))
        {
            return false;
        }

        var tileIndices = grid.TileIndicesFor(xform.Coordinates);
        return AnchorEntity(xform, grid, tileIndices);
    }

    public void Unanchor(TransformComponent xform)
    {
        //HACK: Client grid pivot causes this.
        //TODO: make grid components the actual grid
        if(xform.GridUid == null)
            return;

        UnanchorEntity(xform, Comp<IMapGridComponent>(xform.GridUid.Value));
    }

    public void UnanchorEntity(TransformComponent xform, IMapGridComponent grid)
    {
        var tileIndices = grid.Grid.TileIndicesFor(xform.Coordinates);
        grid.Grid.RemoveFromSnapGridCell(tileIndices, xform.Owner);
        if (TryComp<PhysicsComponent>(xform.Owner, out var physicsComponent))
        {
            physicsComponent.BodyType = BodyType.Dynamic;
        }

        xform.SetAnchored(false);
    }

    #endregion

    #region Contains

    /// <summary>
    ///     Returns whether the given entity is a child of this transform or one of its descendants.
    /// </summary>
    public bool ContainsEntity(TransformComponent xform, EntityUid entity)
    {
        return ContainsEntity(xform, entity, GetEntityQuery<TransformComponent>());
    }

    /// <inheritdoc cref="ContainsEntity(Robust.Shared.GameObjects.TransformComponent,Robust.Shared.GameObjects.EntityUid)"/>
    public bool ContainsEntity(TransformComponent xform, EntityUid entity, EntityQuery<TransformComponent> xformQuery)
    {
        return ContainsEntity(xform, xformQuery.GetComponent(entity), xformQuery);
    }

    /// <inheritdoc cref="ContainsEntity(Robust.Shared.GameObjects.TransformComponent,Robust.Shared.GameObjects.EntityUid)"/>
    public bool ContainsEntity(TransformComponent xform, TransformComponent entityTransform)
    {
        return ContainsEntity(xform, entityTransform, GetEntityQuery<TransformComponent>());
    }

    /// <inheritdoc cref="ContainsEntity(Robust.Shared.GameObjects.TransformComponent,Robust.Shared.GameObjects.EntityUid)"/>
    public bool ContainsEntity(TransformComponent xform, TransformComponent entityTransform, EntityQuery<TransformComponent> xformQuery)
    {
        // Is the entity the scene root
        if (!entityTransform.ParentUid.IsValid())
            return false;

        // Is this the direct parent of the entity
        if (xform.Owner == entityTransform.ParentUid)
            return true;

        // Recursively search up the parents for this object
        var parentXform = xformQuery.GetComponent(entityTransform.ParentUid);
        return ContainsEntity(xform, parentXform, xformQuery);
    }

    #endregion

    #region Component Lifetime

    private void OnCompInit(EntityUid uid, TransformComponent component, ComponentInit args)
    {
        // Children MAY be initialized here before their parents are.
        // We do this whole dance to handle this recursively,
        // setting _mapIdInitialized along the way to avoid going to the IMapComponent every iteration.
        static MapId FindMapIdAndSet(TransformComponent xform, IEntityManager entMan, EntityQuery<TransformComponent> xformQuery)
        {
            if (xform._mapIdInitialized)
                return xform.MapID;

            MapId value;

            if (xform.ParentUid.IsValid())
            {
                value = FindMapIdAndSet(xformQuery.GetComponent(xform.ParentUid), entMan, xformQuery);
            }
            else
            {
                // second level node, terminates recursion up the branch of the tree
                if (entMan.TryGetComponent(xform.Owner, out IMapComponent? mapComp))
                {
                    value = mapComp.WorldMap;
                }
                else
                {
                    // We allow entities to be spawned directly into null-space.
                    value = MapId.Nullspace;
                }
            }

            xform.MapID = value;
            xform._mapIdInitialized = true;
            return value;
        }

        var xformQuery = GetEntityQuery<TransformComponent>();

        if (!component._mapIdInitialized)
        {
            FindMapIdAndSet(component, EntityManager, xformQuery);
            component._mapIdInitialized = true;
        }

        // Has to be done if _parent is set from ExposeData.
        if (component.ParentUid.IsValid())
        {
            // Note that _children is a SortedSet<EntityUid>,
            // so duplicate additions (which will happen) don't matter.

            var parentXform = xformQuery.GetComponent(component.ParentUid);
            if (parentXform.LifeStage > ComponentLifeStage.Running || LifeStage(parentXform.Owner) > EntityLifeStage.MapInitialized)
            {
                var msg = $"Attempted to re-parent to a terminating object. Entity: {ToPrettyString(parentXform.Owner)}, new parent: {ToPrettyString(uid)}";
#if EXCEPTION_TOLERANCE
                Logger.Error(msg);
                Del(uid);
#else
                throw new InvalidOperationException(msg);
#endif
            }

            parentXform._children.Add(uid);
        }

        if (component.GridUid == null)
            SetGridId(component, component.FindGridEntityId(xformQuery));
        component.MatricesDirty = true;
    }

    private void OnCompStartup(EntityUid uid, TransformComponent component, ComponentStartup args)
    {
        // Re-Anchor the entity if needed.
        if (component._anchored && _mapManager.TryFindGridAt(component.MapPosition, out var grid))
        {
            if (!grid.IsAnchored(component.Coordinates, uid))
            {
                AnchorEntity(component, grid);
            }
        }
        else
            component._anchored = false;

        // Keep the cached matrices in sync with the fields.
        Dirty(component);

        var ev = new TransformStartupEvent(component);
        RaiseLocalEvent(uid, ref ev, true);
    }

    #endregion

    #region GridId

    /// <summary>
    /// Sets the <see cref="GridId"/> for the transformcomponent. Does not Dirty it.
    /// </summary>
    public void SetGridId(TransformComponent xform, EntityUid? gridId, EntityQuery<TransformComponent>? xformQuery = null)
    {
        if (xform._gridUid == gridId) return;

        DebugTools.Assert(gridId == null || HasComp<MapGridComponent>(gridId));

        xformQuery ??= GetEntityQuery<TransformComponent>();
        SetGridIdRecursive(xform, gridId, xformQuery.Value);
    }

    private static void SetGridIdRecursive(TransformComponent xform, EntityUid? gridId, EntityQuery<TransformComponent> xformQuery)
    {
        xform._gridUid = gridId;
        var childEnumerator = xform.ChildEnumerator;

        while (childEnumerator.MoveNext(out var child))
        {
            SetGridIdRecursive(xformQuery.GetComponent(child.Value), gridId, xformQuery);
        }
    }

    #endregion

    #region Local Position

    public void SetLocalPosition(EntityUid uid, Vector2 value, TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref xform)) return;
        SetLocalPosition(xform, value);
    }

    public virtual void SetLocalPosition(TransformComponent xform, Vector2 value)
    {
        xform.LocalPosition = value;
    }

    public void SetLocalPositionNoLerp(EntityUid uid, Vector2 value, TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref xform)) return;
        SetLocalPositionNoLerp(xform, value);
    }

    public virtual void SetLocalPositionNoLerp(TransformComponent xform, Vector2 value)
    {
        xform.LocalPosition = value;
    }

    #endregion

    #region Local Rotation

    public void SetLocalRotation(EntityUid uid, Angle value, TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref xform)) return;
        SetLocalRotation(xform, value);
    }

    public virtual void SetLocalRotation(TransformComponent xform, Angle value)
    {
        xform.LocalRotation = value;
    }

    #endregion

    #region Parent

    public TransformComponent? GetParent(EntityUid uid)
    {
        return GetParent(uid, GetEntityQuery<TransformComponent>());
    }

    public TransformComponent? GetParent(EntityUid uid, EntityQuery<TransformComponent> xformQuery)
    {
        return GetParent(xformQuery.GetComponent(uid), xformQuery);
    }

    public TransformComponent? GetParent(TransformComponent xform)
    {
        return GetParent(xform, GetEntityQuery<TransformComponent>());
    }

    public TransformComponent? GetParent(TransformComponent xform, EntityQuery<TransformComponent> xformQuery)
    {
        if (!xform.ParentUid.IsValid()) return null;
        return xformQuery.GetComponent(xform.ParentUid);
    }

    /* TODO: Need to peel out relevant bits of AttachParent e.g. children updates.
    public void SetParent(TransformComponent xform, EntityUid parent, bool move = true)
    {
        if (xform.ParentUid == parent) return;

        if (!parent.IsValid())
        {
            xform.AttachToGridOrMap();
            return;
        }

        if (xform.Anchored)
        {
            xform.Anchored = false;
        }

        if (move)
            xform.AttachParent(parent);
        else
            xform._parent = parent;

        Dirty(xform);
    }
    */

    #endregion

    #region States

    protected void ActivateLerp(TransformComponent xform)
    {
        if (xform.ActivelyLerping)
            return;

        xform.ActivelyLerping = true;
        RaiseLocalEvent(xform.Owner, new TransformStartLerpMessage(xform), true);
    }

    internal void OnGetState(EntityUid uid, TransformComponent component, ref ComponentGetState args)
    {
        args.State = new TransformComponentState(
            component.LocalPosition,
            component.LocalRotation,
            component.ParentUid,
            component.NoLocalRotation,
            component.Anchored);
    }

    internal void OnHandleState(EntityUid uid, TransformComponent component, ref ComponentHandleState args)
    {
        if (args.Current is TransformComponentState newState)
        {
            var newParentId = newState.ParentID;
            var rebuildMatrices = false;

            // Update to new parent.
            // if the new one isn't valid (e.g. PVS) then we'll just return early after yeeting them to nullspace.
            if (component.ParentUid != newParentId)
            {
                if (!newParentId.IsValid())
                {
                    DetachParentToNull(component);
                }
                else
                {
                    if (!Exists(newParentId))
                    {
#if !EXCEPTION_TOLERANCE
                        throw new InvalidOperationException($"Unable to find new parent {newParentId}! This probably means the server never sent it.");
#else
                        _logger.Error($"Unable to find new parent {newParentId}! Deleting {ToPrettyString(uid)}");
                        QueueDel(uid);
                        return;
#endif
                    }

                    component.AttachParent(Transform(newParentId));
                }

                rebuildMatrices = true;
            }

            if (!component.LocalPosition.EqualsApprox(newState.LocalPosition) || !component.LocalRotation.EqualsApprox(newState.Rotation))
            {
                var oldPos = component.Coordinates;
                component._localPosition = newState.LocalPosition;
                var oldRot = component.LocalRotation;
                component._localRotation = newState.Rotation;

                var ev = new MoveEvent(uid, oldPos, component.Coordinates, oldRot, component.LocalRotation, component, _gameTiming.ApplyingState);
                DeferMoveEvent(ref ev);

                rebuildMatrices = true;
            }

            component._prevPosition = newState.LocalPosition;
            component._prevRotation = newState.Rotation;

            // Anchored currently does a TryFindGridAt internally which may fail in particularly... violent situations.
            if (newState.Anchored && !component.Anchored)
            {
                DebugTools.Assert(component.GridUid != null);
                var iGrid = Comp<MapGridComponent>(component.GridUid!.Value);
                AnchorEntity(component, iGrid);
                DebugTools.Assert(component.Anchored);
            }
            else
            {
                component.Anchored = newState.Anchored;
            }

            component._noLocalRotation = newState.NoLocalRotation;

            // This is not possible, because client entities don't exist on the server, so the parent HAS to be a shared entity.
            // If this assert fails, the code above that sets the parent is broken.
            DebugTools.Assert(!component.ParentUid.IsClientSide(), "Transform received a state, but is still parented to a client entity.");

            // Whatever happened on the client, these should still be correct
            DebugTools.Assert(component.ParentUid == newState.ParentID);
            DebugTools.Assert(component.Anchored == newState.Anchored);

            if (rebuildMatrices)
            {
                component.MatricesDirty = true;
            }

            Dirty(component);
        }

        if (args.Next is TransformComponentState nextTransform)
        {
            component._nextPosition = nextTransform.LocalPosition;
            component._nextRotation = nextTransform.Rotation;
            component.LerpParent = nextTransform.ParentID;
            ActivateLerp(component);
        }
        else
        {
            DeactivateLerp(component);
        }
    }

    private void DeactivateLerp(TransformComponent component)
    {
        // this should cause the lerp to do nothing
        component._nextPosition = null;
        component._nextRotation = null;
        component.LerpParent = EntityUid.Invalid;
    }

    #endregion

    #region World Matrix

    [Pure]
    public Matrix3 GetWorldMatrix(EntityUid uid)
    {
        return Transform(uid).WorldMatrix;
    }

    // Temporary until it's moved here
    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix3 GetWorldMatrix(TransformComponent component)
    {
        return component.WorldMatrix;
    }

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix3 GetWorldMatrix(EntityUid uid, EntityQuery<TransformComponent> xformQuery)
    {
        return GetWorldMatrix(xformQuery.GetComponent(uid));
    }


    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix3 GetWorldMatrix(TransformComponent component, EntityQuery<TransformComponent> xformQuery)
    {
        return component.WorldMatrix;
    }

    #endregion

    #region World Position

    [Pure]
    public Vector2 GetWorldPosition(EntityUid uid)
    {
        return Transform(uid).WorldPosition;
    }

    // Temporary until it's moved here
    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 GetWorldPosition(TransformComponent component)
    {
        return component.WorldPosition;
    }

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 GetWorldPosition(EntityUid uid, EntityQuery<TransformComponent> xformQuery)
    {
        return GetWorldPosition(xformQuery.GetComponent(uid));
    }

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 GetWorldPosition(TransformComponent component, EntityQuery<TransformComponent> xformQuery)
    {
        return component.WorldPosition;
    }

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (Vector2 WorldPosition, Angle WorldRotation) GetWorldPositionRotation(TransformComponent component, EntityQuery<TransformComponent> xformQuery)
    {
        return component.GetWorldPositionRotation(xformQuery);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetWorldPosition(EntityUid uid, Vector2 worldPos)
    {
        var xform = Transform(uid);
        SetWorldPosition(xform, worldPos);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetWorldPosition(EntityUid uid, Vector2 worldPos, EntityQuery<TransformComponent> xformQuery)
    {
        var component = xformQuery.GetComponent(uid);
        SetWorldPosition(component, worldPos, xformQuery);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetWorldPosition(TransformComponent component, Vector2 worldPos)
    {
        SetWorldPosition(component, worldPos, GetEntityQuery<TransformComponent>());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetWorldPosition(TransformComponent component, Vector2 worldPos, EntityQuery<TransformComponent> xformQuery)
    {
        if (!component._parent.IsValid())
        {
            DebugTools.Assert("Parent is invalid while attempting to set WorldPosition - did you try to move root node?");
            return;
        }

        // TODO look at SetWorldPositionRotation and how it sets world position. I ASSUME that is faster than matrix products + transform, but not actually sure.

        // world coords to parent coords
        var newPos = GetInvWorldMatrix(component._parent, xformQuery).Transform(worldPos);
        SetLocalPosition(component, newPos);
    }

    #endregion

    #region World Rotation

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Angle GetWorldRotation(EntityUid uid)
    {
        return Transform(uid).WorldRotation;
    }

    // Temporary until it's moved here
    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Angle GetWorldRotation(TransformComponent component)
    {
        return component.WorldRotation;
    }

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Angle GetWorldRotation(EntityUid uid, EntityQuery<TransformComponent> xformQuery)
    {
        return GetWorldRotation(xformQuery.GetComponent(uid));
    }

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Angle GetWorldRotation(TransformComponent component, EntityQuery<TransformComponent> xformQuery)
    {
        return component.WorldRotation;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetWorldRotation(EntityUid uid, Angle angle)
    {
        var component = Transform(uid);
        SetWorldRotation(component, angle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetWorldRotation(TransformComponent component, Angle angle)
    {
        var current = GetWorldRotation(component);
        var diff = angle - current;
        SetLocalRotation(component, component.LocalRotation + diff);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetWorldRotation(EntityUid uid, Angle angle, EntityQuery<TransformComponent> xformQuery)
    {
        SetWorldRotation(xformQuery.GetComponent(uid), angle, xformQuery);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetWorldRotation(TransformComponent component, Angle angle, EntityQuery<TransformComponent> xformQuery)
    {
        var current = GetWorldRotation(component, xformQuery);
        var diff = angle - current;
        SetLocalRotation(component, component.LocalRotation + diff);
    }

    #endregion

    #region Set Position+Rotation

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetWorldPositionRotation(EntityUid uid, Vector2 worldPos, Angle worldRot, EntityQuery<TransformComponent> xformQuery)
    {
        var component = xformQuery.GetComponent(uid);
        SetWorldPositionRotation(component, worldPos, worldRot, xformQuery);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetWorldPositionRotation(TransformComponent component, Vector2 worldPos, Angle worldRot)
    {
        SetWorldPositionRotation(component, worldPos, worldRot, GetEntityQuery<TransformComponent>());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetWorldPositionRotation(TransformComponent component, Vector2 worldPos, Angle worldRot, EntityQuery<TransformComponent> xformQuery)
    {
        if (!component._parent.IsValid())
        {
            DebugTools.Assert("Parent is invalid while attempting to set WorldPosition - did you try to move root node?");
            return;
        }

        var (curWorldPos, curWorldRot) = GetWorldPositionRotation(component, xformQuery);

        var negativeParentWorldRot = component.LocalRotation - curWorldRot;

        var newLocalPos = component.LocalPosition + negativeParentWorldRot.RotateVec(worldPos - curWorldPos);
        var newLocalRot = component.LocalRotation + worldRot - curWorldRot;

        SetLocalPositionRotation(component, newLocalPos, newLocalRot);
    }

    /// <summary>
    ///     Simultaneously set the position and rotation. This is better than setting individually, as it reduces the number of move events and matrix rebuilding operations.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void SetLocalPositionRotation(TransformComponent xform, Vector2 pos, Angle rot)
    {
        if (!xform._parent.IsValid())
        {
            DebugTools.Assert("Parent is invalid while attempting to set WorldPosition - did you try to move root node?");
            return;
        }

        if (xform._localPosition.EqualsApprox(pos) && xform.LocalRotation.EqualsApprox(rot))
            return;

        var oldPosition = xform.Coordinates;
        var oldRotation = xform.LocalRotation;

        if (!xform.Anchored)
            xform._localPosition = pos;

        if (!xform.NoLocalRotation)
            xform.LocalRotation = rot;

        Dirty(xform);

        if (!xform.DeferUpdates)
        {
            xform.MatricesDirty = true;
            var moveEvent = new MoveEvent(xform.Owner, oldPosition, xform.Coordinates, oldRotation, rot, xform, _gameTiming.ApplyingState);
            RaiseLocalEvent(xform.Owner, ref moveEvent, true);
        }
        else
        {
            xform._oldCoords ??= oldPosition;
            xform._oldLocalRotation ??= oldRotation;
        }
    }

    #endregion

    #region Inverse World Matrix

    [Pure]
    public Matrix3 GetInvWorldMatrix(EntityUid uid)
    {
        return Comp<TransformComponent>(uid).InvWorldMatrix;
    }

    // Temporary until it's moved here
    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix3 GetInvWorldMatrix(TransformComponent component)
    {
        return component.InvWorldMatrix;
    }

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix3 GetInvWorldMatrix(EntityUid uid, EntityQuery<TransformComponent> xformQuery)
    {
        return GetInvWorldMatrix(xformQuery.GetComponent(uid));
    }

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix3 GetInvWorldMatrix(TransformComponent component, EntityQuery<TransformComponent> xformQuery)
    {
        return component.InvWorldMatrix;
    }

    #endregion

    #region State Handling
    private void ChangeMapId(TransformComponent xform, MapId newMapId, EntityQuery<TransformComponent> xformQuery, EntityQuery<MetaDataComponent> metaQuery)
    {
        if (newMapId == xform.MapID)
            return;

        //Set Paused state
        var mapPaused = _mapManager.IsMapPaused(newMapId);
        var meta = metaQuery.GetComponent(xform.Owner);
        _metaSys.SetEntityPaused(xform.Owner, mapPaused, meta);

        xform.MapID = newMapId;
        xform.UpdateChildMapIdsRecursive(xform.MapID, mapPaused, xformQuery, metaQuery, _metaSys);
    }

    public void DetachParentToNull(TransformComponent xform)
    {
        if (xform._parent.IsValid())
            DetachParentToNull(xform, GetEntityQuery<TransformComponent>(), GetEntityQuery<MetaDataComponent>());
        else
            DebugTools.Assert(!xform.Anchored);
    }

    public void DetachParentToNull(TransformComponent xform, EntityQuery<TransformComponent> xformQuery, EntityQuery<MetaDataComponent> metaQuery, TransformComponent? oldConcrete = null)
    {
        var oldParent = xform._parent;

        // Even though they may already be in nullspace we may want to deparent them anyway
        if (!oldParent.IsValid())
        {
            DebugTools.Assert(!xform.Anchored);
            return;
        }

        // Stop any active lerps
        xform._nextPosition = null;
        xform._nextRotation = null;
        xform.LerpParent = EntityUid.Invalid;

        if (xform.Anchored && metaQuery.TryGetComponent(xform.GridUid, out var meta) && meta.EntityLifeStage <= EntityLifeStage.MapInitialized)
        {
            var grid = Comp<IMapGridComponent>(xform.GridUid.Value);
            var tileIndices = grid.Grid.TileIndicesFor(xform.Coordinates);
            grid.Grid.RemoveFromSnapGridCell(tileIndices, xform.Owner);

            // intentionally not updating physics body type to non-static, there is no need to add it to the current map.

            xform._anchored = false;
            var anchorStateChangedEvent = new AnchorStateChangedEvent(xform, true);
            RaiseLocalEvent(xform.Owner, ref anchorStateChangedEvent, true);
        }

        // TODO replace this with just setting the xform's entity coordinates.

        oldConcrete ??= xformQuery.GetComponent(oldParent);
        oldConcrete._children.Remove(xform.Owner);

        var oldPos = xform.Coordinates;
        var oldRot = xform.LocalRotation;
        var oldMap = xform.MapID;
        xform._parent = EntityUid.Invalid;

        // aaaaaaaaaaaaaaaa
        ChangeMapId(xform, MapId.Nullspace, xformQuery, metaQuery);

        if (xform.GridUid != null)
            SetGridId(xform, null, xformQuery);

        var entParentChangedMessage = new EntParentChangedMessage(xform.Owner, oldParent, oldMap, xform);
        RaiseLocalEvent(xform.Owner, ref entParentChangedMessage, true);

        var ev = new MoveEvent(xform.Owner, oldPos, default, oldRot, default, xform, _gameTiming.ApplyingState);
        RaiseLocalEvent(xform.Owner, ref ev, true);
        Dirty(xform);
    }
    #endregion
}
