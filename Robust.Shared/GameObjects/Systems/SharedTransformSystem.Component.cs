using JetBrains.Annotations;
using Robust.Shared.GameStates;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System;
using System.Runtime.CompilerServices;
using Robust.Shared.Map.Components;

namespace Robust.Shared.GameObjects;

public abstract partial class SharedTransformSystem
{
    [IoC.Dependency] private readonly IGameTiming _gameTiming = default!;
    [IoC.Dependency] private readonly EntityLookupSystem _lookup = default!;
    [IoC.Dependency] private readonly SharedPhysicsSystem _physics = default!;

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
        oldGrid.RemoveFromSnapGridCell(tilePos, xform.Owner);
        newGrid.AddToSnapGridCell(tilePos, xform.Owner);
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

    public bool AnchorEntity(TransformComponent xform, MapGridComponent grid, Vector2i tileIndices)
    {
        if (!grid.AddToSnapGridCell(tileIndices, xform.Owner))
            return false;

        var wasAnchored = xform._anchored;
        xform._anchored = true;

        // Mark as static before doing position changes, to avoid the velocity change on parent change.
        _physics.TrySetBodyType(xform.Owner, BodyType.Static);

        if (!wasAnchored && xform.Running)
        {
            var ev = new AnchorStateChangedEvent(xform);
            RaiseLocalEvent(xform.Owner, ref ev, true);
        }

        // Anchor snapping. Note that set coordiantes will dirty the component for us.
        var pos = new EntityCoordinates(grid.GridEntityId, grid.GridTileToLocal(tileIndices).Position);
        SetCoordinates(xform, pos, unanchor: false);

        return true;
    }

    public bool AnchorEntity(TransformComponent xform, MapGridComponent grid)
    {
        var tileIndices = grid.TileIndicesFor(xform.Coordinates);
        return AnchorEntity(xform, grid, tileIndices);
    }

    public bool AnchorEntity(TransformComponent xform)
    {
        return _mapManager.TryGetGrid(xform.GridUid, out var grid)
            && AnchorEntity(xform, grid, grid.TileIndicesFor(xform.Coordinates));
    }

    public void Unanchor(TransformComponent xform, bool setPhysics = true)
    {
        if (!xform._anchored)
            return;

        Dirty(xform);
        xform._anchored = false;

        if (setPhysics)
            _physics.TrySetBodyType(xform.Owner, BodyType.Dynamic);

        if (xform.LifeStage < ComponentLifeStage.Initialized)
            return;

        if (TryComp(xform.GridUid, out MapGridComponent? grid))
        {
            var tileIndices = grid.TileIndicesFor(xform.Coordinates);
            grid.RemoveFromSnapGridCell(tileIndices, xform.Owner);
        }
        else if (xform.Initialized)
        {
            //HACK: Client grid pivot causes this.
            //TODO: make grid components the actual grid

            // I have NFI what the comment above is on about, but this doesn't seem good, so lets log an error if it happens.
            Logger.Error($"Missing grid while unanchoring {ToPrettyString(xform.Owner)}");
        }

        if (!xform.Running)
            return;

        var ev = new AnchorStateChangedEvent(xform);
        RaiseLocalEvent(xform.Owner, ref ev, true);
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
        // setting _mapIdInitialized along the way to avoid going to the MapComponent every iteration.
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
                if (entMan.TryGetComponent(xform.Owner, out MapComponent? mapComp))
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

        if (!component._anchored)
            return;

        MapGridComponent? grid;

        // First try find grid via parent:
        if (component.GridUid == component.ParentUid && TryComp(component.ParentUid, out MapGridComponent? gridComp))
        {
            grid = gridComp;
        }
        else
        {
            // Entity may not be directly parented to the grid (e.g., spawned using some relative entity coordiantes)
            // in that case, we attempt to attach to a grid.
            var pos = new MapCoordinates(GetWorldPosition(component), component.MapID);
            _mapManager.TryFindGridAt(pos, out grid);
        }

        if (grid == null)
        {
            Unanchor(component);
            return;
        }

        if (!AnchorEntity(component, grid))
            component._anchored = false;
    }

    private void OnCompStartup(EntityUid uid, TransformComponent xform, ComponentStartup args)
    {
        // TODO PERFORMANCE remove AnchorStateChangedEvent and EntParentChangedMessage events here.

        // I hate this. Apparently some entities rely on this to perform their initialization logic (e.g., power
        // receivers or lights?). Those components should just do their own init logic, instead of wasting time raising
        // this event on every entity that gets created.
        if (xform.Anchored)
        {
            DebugTools.Assert(xform.ParentUid == xform.GridUid && xform.ParentUid.IsValid());
            var anchorEv = new AnchorStateChangedEvent(xform);
            RaiseLocalEvent(uid, ref anchorEv, true);
        }

        // I hate this too. Once again, required for shit like containers because they CBF doing their own init logic
        // and rely on parent changed messages instead. Might also be used by broadphase stuff?
        var parentEv = new EntParentChangedMessage(uid, null, MapId.Nullspace, xform);
        RaiseLocalEvent(uid, ref parentEv, true);

        // there should be no deferred events before startup has finished.
        DebugTools.Assert(xform._oldCoords == null && xform._oldLocalRotation == null);

        var ev = new TransformStartupEvent(xform);
        RaiseLocalEvent(uid, ref ev, true);
    }

    #endregion

    #region GridId

    /// <summary>
    /// Sets the <see cref="GridId"/> for the transformcomponent. Does not Dirty it.
    /// </summary>
    public void SetGridId(TransformComponent xform, EntityUid? gridId, EntityQuery<TransformComponent>? xformQuery = null)
    {
        if (xform._gridUid == gridId)
            return;

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

    #region Coordinates

    public void SetCoordinates(EntityUid uid, EntityCoordinates value)
    {
        SetCoordinates(Transform(uid), value);
    }

    /// <summary>
    ///     This sets the local position and parent of an entity.
    /// </summary>
    /// <param name="rotation">Final local rotation. If not specified, this will attempt to preserve world
    /// rotation.</param>
    /// <param name="unanchor">Whether or not to unanchor the entity before moving. Note that this will still move the
    /// entity even when false. If you set this to false, you need to manually manage the grid lookup changes and ensure
    /// the final position is valid</param>
    public void SetCoordinates(TransformComponent xform, EntityCoordinates value, Angle? rotation = null, bool unanchor = true, TransformComponent? newParent = null, TransformComponent? oldParent = null)
    {
        // NOTE: This setter must be callable from before initialize.

        if (xform.ParentUid == value.EntityId
            && xform._localPosition.EqualsApprox(value.Position)
            && (rotation == null || MathHelper.CloseTo(rotation.Value.Theta, xform._localRotation.Theta)))
        {
            return;
        }

        var oldPosition = xform._parent.IsValid() ? new EntityCoordinates(xform._parent, xform._localPosition) : default;
        var oldRotation = xform._localRotation;

        if (xform.Anchored && unanchor)
            Unanchor(xform);

        // Set new values
        Dirty(xform);
        xform.MatricesDirty = true;
        xform._localPosition = value.Position;

        if (rotation != null)
            xform._localRotation = rotation.Value;

        // Perform parent change logic
        if (value.EntityId != xform._parent)
        {
            var xformQuery = GetEntityQuery<TransformComponent>();

            if (value.EntityId == xform.Owner)
            {
                QueueDel(xform.Owner);
                throw new InvalidOperationException($"Attempted to parent an entity to itself: {ToPrettyString(xform.Owner)}");
            }

            if (value.EntityId.IsValid())
            {
                if (!xformQuery.Resolve(value.EntityId, ref newParent, false))
                {
                    QueueDel(xform.Owner);
                    throw new InvalidOperationException($"Attempted to parent entity {ToPrettyString(xform.Owner)} to non-existent entity {value.EntityId}");
                }

                if (newParent.LifeStage > ComponentLifeStage.Running || LifeStage(value.EntityId) > EntityLifeStage.MapInitialized)
                {
                    QueueDel(xform.Owner);
                    throw new InvalidOperationException($"Attempted to re-parent to a terminating object. Entity: {ToPrettyString(xform.Owner)}, new parent: {ToPrettyString(value.EntityId)}");
                }
            }

            if (xform._parent.IsValid())
                xformQuery.Resolve(xform._parent, ref oldParent);

            oldParent?._children.Remove(xform.Owner);
            newParent?._children.Add(xform.Owner);

            xform._parent = value.EntityId;
            var oldMapId = xform.MapID;

            if (newParent != null)
            {
                xform.ChangeMapId(newParent.MapID, xformQuery);
                if (xform.GridUid != xform.Owner)
                    SetGridId(xform, xform.FindGridEntityId(xformQuery), xformQuery);
            }
            else
            {
                xform.ChangeMapId(MapId.Nullspace, xformQuery);
                if (xform.GridUid != xform.Owner)
                    SetGridId(xform, null, xformQuery);
            }

            if (xform.Initialized)
            {
                // preserve world rotation
                if (rotation == null && oldParent != null && newParent != null)
                    xform._localRotation += GetWorldRotation(oldParent, xformQuery) - GetWorldRotation(newParent, xformQuery);

                var entParentChangedMessage = new EntParentChangedMessage(xform.Owner, oldParent?.Owner, oldMapId, xform);
                RaiseLocalEvent(xform.Owner, ref entParentChangedMessage, true);
            }
        }

        DebugTools.Assert(!xform.DeferUpdates); // breaks anchoring lookup logic if deferred. If this changes, also need to relocate the `xform.MatricesDirty = true`

        if (!xform.Initialized)
            return;

        var newPosition = xform._parent.IsValid() ? new EntityCoordinates(xform._parent, xform._localPosition) : default;
        var moveEvent = new MoveEvent(xform.Owner, oldPosition, newPosition, oldRotation, xform._localRotation, xform, _gameTiming.ApplyingState);
        RaiseLocalEvent(xform.Owner, ref moveEvent, true);
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

    public void SetParent(EntityUid uid, EntityUid parent)
    {
        var query = GetEntityQuery<TransformComponent>();
        SetParent(query.GetComponent(uid), parent, query);
    }

    public void SetParent(TransformComponent xform, EntityUid parent, TransformComponent? parentXform = null)
    {
        SetParent(xform, parent, GetEntityQuery<TransformComponent>(), parentXform);
    }

    public void SetParent(TransformComponent xform, EntityUid parent, EntityQuery<TransformComponent> xformQuery, TransformComponent? parentXform = null)
    {
        if (xform.ParentUid == parent)
            return;

        if (!parent.IsValid())
        {
            DetachParentToNull(xform, xformQuery, GetEntityQuery<MetaDataComponent>());
            return;
        }

        if (!xformQuery.Resolve(parent, ref parentXform))
            return;

        var (_, parRot, parInvMatrix) = parentXform.GetWorldPositionRotationInvMatrix(xformQuery);
        var (pos, rot) = GetWorldPositionRotation(xform, xformQuery);
        var newPos = parInvMatrix.Transform(pos);
        var newRot = rot - parRot;

        SetCoordinates(xform, new EntityCoordinates(parent, newPos), newRot, newParent: parentXform);
    }

    #endregion

    #region States
    public virtual void ActivateLerp(TransformComponent xform) { }

    public virtual void DeactivateLerp(TransformComponent xform) { }

    internal void OnGetState(EntityUid uid, TransformComponent component, ref ComponentGetState args)
    {
        DebugTools.Assert(!component.ParentUid.IsValid() || (!Deleted(component.ParentUid) && !EntityManager.IsQueuedForDeletion(component.ParentUid)));
        args.State = new TransformComponentState(
            component.LocalPosition,
            component.LocalRotation,
            component.ParentUid,
            component.NoLocalRotation,
            component.Anchored);
    }

    internal void OnHandleState(EntityUid uid, TransformComponent xform, ref ComponentHandleState args)
    {
        if (args.Current is TransformComponentState newState)
        {
            var newParentId = newState.ParentID;
            var oldAnchored = xform.Anchored;

            // update actual position data, if required
            if (!xform.LocalPosition.EqualsApprox(newState.LocalPosition)
                || !xform.LocalRotation.EqualsApprox(newState.Rotation)
                || xform.ParentUid != newParentId)
            {
                // remove from any old grid lookups
                if (xform.Anchored && TryComp(xform.ParentUid, out MapGridComponent? grid))
                {
                    var tileIndices = grid.TileIndicesFor(xform.Coordinates);
                    grid.RemoveFromSnapGridCell(tileIndices, xform.Owner);
                }

                // Set anchor state true during the move event unless the entity wasn't and isn't being anchored. This avoids unnecessary entity lookup changes.
                xform._anchored |= newState.Anchored;

                // Update the action position, rotation, and parent (and hence also map, grid, etc).
                SetCoordinates(xform, new EntityCoordinates(newParentId, newState.LocalPosition), newState.Rotation, unanchor: false);

                xform._anchored = newState.Anchored;

                // Add to any new grid lookups. Normal entity lookups will either have been handled by the move event,
                // or by the following AnchorStateChangedEvent
                if (xform._anchored && xform.Initialized)
                {
                    if (xform.ParentUid == xform.GridUid && TryComp(xform.GridUid, out MapGridComponent? newGrid))
                    {
                        var tileIndices = newGrid.TileIndicesFor(xform.Coordinates);
                        newGrid.AddToSnapGridCell(tileIndices, xform.Owner);
                    }
                    else
                    {
                        DebugTools.Assert("New transform state coordinates are incompatible with anchoring.");
                        xform._anchored = false;
                    }
                }
            }
            else
            {
                xform.Anchored = newState.Anchored;
            }

            if (oldAnchored != newState.Anchored && xform.Initialized)
            {
                var ev = new AnchorStateChangedEvent(xform);
                RaiseLocalEvent(xform.Owner, ref ev, true);
            }

            xform.PrevPosition = newState.LocalPosition;
            xform.PrevRotation = newState.Rotation;
            xform._noLocalRotation = newState.NoLocalRotation;

            DebugTools.Assert(xform.ParentUid == newState.ParentID, "Transform state failed to set parent");
            DebugTools.Assert(xform.Anchored == newState.Anchored, "Transform state failed to set anchored");
        }

        if (args.Next is TransformComponentState nextTransform)
        {
            xform.NextPosition = nextTransform.LocalPosition;
            xform.NextRotation = nextTransform.Rotation;
            xform.LerpParent = nextTransform.ParentID;
            ActivateLerp(xform);
        }
        else
        {
            DeactivateLerp(xform);
        }
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
            xform._localRotation = rot;

        Dirty(xform);

        if (!xform.DeferUpdates)
        {
            xform.MatricesDirty = true;
            if (!xform.Initialized)
                return;

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

        // Map entities retain their map Uids
        if (xform.Owner != xform.MapUid)
            xform.MapID = newMapId;

        xform.UpdateChildMapIdsRecursive(newMapId, mapPaused, xformQuery, metaQuery, _metaSys);
    }

    public void DetachParentToNull(TransformComponent xform)
    {
        if (xform._parent.IsValid())
            DetachParentToNull(xform, GetEntityQuery<TransformComponent>(), GetEntityQuery<MetaDataComponent>());
        else
            DebugTools.Assert(!xform.Anchored);
    }

    public void DetachParentToNull(TransformComponent xform, EntityQuery<TransformComponent> xformQuery, EntityQuery<MetaDataComponent> metaQuery, TransformComponent? oldXform = null)
    {
        var oldParent = xform._parent;
        if (!oldParent.IsValid())
        {
            DebugTools.Assert(!xform.Anchored);
            DebugTools.Assert((MetaData(xform.Owner).Flags & MetaDataFlags.InContainer) == 0x0);
            return;
        }

        // Before making any changes to physics or transforms, remove from the current broadphase
        _lookup.RemoveFromEntityTree(xform.Owner, xform, xformQuery);

        // Stop any active lerps
        xform.NextPosition = null;
        xform.NextRotation = null;
        xform.LerpParent = EntityUid.Invalid;

        if (xform.Anchored && metaQuery.TryGetComponent(xform.GridUid, out var meta) && meta.EntityLifeStage <= EntityLifeStage.MapInitialized)
        {
            var grid = Comp<MapGridComponent>(xform.GridUid.Value);
            var tileIndices = grid.TileIndicesFor(xform.Coordinates);
            grid.RemoveFromSnapGridCell(tileIndices, xform.Owner);
            xform._anchored = false;
            var anchorStateChangedEvent = new AnchorStateChangedEvent(xform, true);
            RaiseLocalEvent(xform.Owner, ref anchorStateChangedEvent, true);
        }

        SetCoordinates(xform, default, Angle.Zero, oldParent: oldXform);
        DebugTools.Assert((MetaData(xform.Owner).Flags & MetaDataFlags.InContainer) == 0x0);
    }
    #endregion
}
