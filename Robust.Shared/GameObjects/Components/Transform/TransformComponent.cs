using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Animations;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    ///     Stores the position and orientation of the entity.
    /// </summary>
    [NetworkedComponent]
    public sealed class TransformComponent : Component, IComponentDebug
    {
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        [DataField("parent")] internal EntityUid _parent;
        [DataField("pos")] internal Vector2 _localPosition = Vector2.Zero; // holds offset from grid, or offset from parent
        [DataField("rot")] internal Angle _localRotation; // local rotation
        [DataField("noRot")] internal bool _noLocalRotation;
        [DataField("anchored")]
        internal bool _anchored;

        private Matrix3 _localMatrix = Matrix3.Identity;
        private Matrix3 _invLocalMatrix = Matrix3.Identity;

        // used for lerping

        internal Vector2? _nextPosition;
        internal Angle? _nextRotation;

        internal Vector2 _prevPosition;
        internal Angle _prevRotation;

        // Cache changes so we can distribute them after physics is done (better cache)
        private EntityCoordinates? _oldCoords;
        private Angle? _oldLocalRotation;

        /// <summary>
        ///     While updating did we actually defer anything?
        /// </summary>
        public bool UpdatesDeferred => _oldCoords != null || _oldLocalRotation != null;

        [ViewVariables(VVAccess.ReadWrite)]
        internal bool ActivelyLerping { get; set; }

        [ViewVariables] internal readonly HashSet<EntityUid> _children = new();

        [Dependency] private readonly IMapManager _mapManager = default!;

        /// <summary>
        ///     Returns the index of the map which this object is on
        /// </summary>
        [ViewVariables]
        public MapId MapID { get; internal set; }

        internal bool _mapIdInitialized;

        // TODO: Cache this.
        /// <summary>
        ///     The EntityUid of the map which this object is on, if any.
        /// </summary>
        public EntityUid? MapUid => _mapManager.MapExists(MapID) ? _mapManager.GetMapEntityId(MapID) : null;

        /// <summary>
        ///     Defer updates to the EntityTree and MoveEvent calls if toggled.
        /// </summary>
        public bool DeferUpdates { get; set; }

        /// <summary>
        ///     The EntityUid of the grid which this object is on, if any.
        /// </summary>
        [ViewVariables]
        public EntityUid? GridUid => _gridUid;

        [Access(typeof(SharedTransformSystem))]
        internal EntityUid? _gridUid = null;

        [Obsolete("Use GridUid")]
        public GridId GridID
        {
            get => _entMan.TryGetComponent(GridUid, out MapGridComponent? grid)
                ? grid.GridIndex
                : GridId.Invalid;
        }

        /// <summary>
        ///     Disables or enables to ability to locally rotate the entity. When set it removes any local rotation.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool NoLocalRotation
        {
            get => _noLocalRotation;
            set
            {
                if (value)
                    LocalRotation = Angle.Zero;

                _noLocalRotation = value;
                Dirty(_entMan);
            }
        }

        /// <summary>
        ///     Current rotation offset of the entity.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [Animatable]
        public Angle LocalRotation
        {
            get => _localRotation;
            set
            {
                if(_noLocalRotation)
                    return;

                if (_localRotation.EqualsApprox(value))
                    return;

                var oldRotation = _localRotation;
                _localRotation = value;
                Dirty(_entMan);

                if (!DeferUpdates)
                {
                    RebuildMatrices();
                    var rotateEvent = new RotateEvent(Owner, oldRotation, _localRotation, this);
                    _entMan.EventBus.RaiseLocalEvent(Owner, ref rotateEvent, true);
                }
                else
                {
                    _oldLocalRotation ??= oldRotation;
                }
            }
        }

        /// <summary>
        ///     Current world rotation of the entity.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public Angle WorldRotation
        {
            get
            {
                var parent = _parent;
                var xformQuery = _entMan.GetEntityQuery<TransformComponent>();
                var rotation = _localRotation;

                while (parent.IsValid())
                {
                    var parentXform = xformQuery.GetComponent(parent);
                    rotation += parentXform._localRotation;
                    parent = parentXform.ParentUid;
                }

                return rotation;
            }
            set
            {
                var current = WorldRotation;
                var diff = value - current;
                LocalRotation += diff;
            }
        }

        /// <summary>
        ///     Reference to the transform of the container of this object if it exists, can be nested several times.
        /// </summary>
        [ViewVariables]
        public TransformComponent? Parent
        {
            get => !_parent.IsValid() ? null : _entMan.GetComponent<TransformComponent>(_parent);
            internal set
            {
                if (value == null)
                {
                    AttachToGridOrMap();
                    return;
                }

                if (_anchored && (value).Owner != _parent)
                {
                    Anchored = false;
                }

                AttachParent(value);
            }
        }

        /// <summary>
        /// The UID of the parent entity that this entity is attached to.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public EntityUid ParentUid
        {
            get => _parent;
            set
            {
                if (value == _parent) return;
                Parent = _entMan.GetComponent<TransformComponent>(value);
            }
        }

        /// <summary>
        ///     Matrix for transforming points from local to world space.
        /// </summary>
        public Matrix3 WorldMatrix
        {
            get
            {
                var xformQuery = _entMan.GetEntityQuery<TransformComponent>();
                var parent = _parent;
                var myMatrix = _localMatrix;

                while (parent.IsValid())
                {
                    var parentXform = xformQuery.GetComponent(parent);
                    var parentMatrix = parentXform._localMatrix;
                    parent = parentXform.ParentUid;

                    Matrix3.Multiply(in myMatrix, in parentMatrix, out var result);
                    myMatrix = result;
                }

                return myMatrix;
            }
        }

        /// <summary>
        ///     Matrix for transforming points from world to local space.
        /// </summary>
        public Matrix3 InvWorldMatrix
        {
            get
            {
                var xformQuery = _entMan.GetEntityQuery<TransformComponent>();
                var parent = _parent;
                var myMatrix = _invLocalMatrix;

                while (parent.IsValid())
                {
                    var parentXform = xformQuery.GetComponent(parent);
                    var parentMatrix = parentXform._invLocalMatrix;
                    parent = parentXform.ParentUid;

                    Matrix3.Multiply(in parentMatrix, in myMatrix, out var result);
                    myMatrix = result;
                }

                return myMatrix;
            }
        }

        /// <summary>
        ///     Current position offset of the entity relative to the world.
        ///     Can de-parent from its parent if the parent is a grid.
        /// </summary>
        [Animatable]
        [ViewVariables(VVAccess.ReadWrite)]
        public Vector2 WorldPosition
        {
            get
            {
                if (_parent.IsValid())
                {
                    // parent coords to world coords
                    return Parent!.WorldMatrix.Transform(_localPosition);
                }
                else
                {
                    return Vector2.Zero;
                }
            }
            set
            {
                if (!_parent.IsValid())
                {
                    DebugTools.Assert("Parent is invalid while attempting to set WorldPosition - did you try to move root node?");
                    return;
                }

                // world coords to parent coords
                var newPos = Parent!.InvWorldMatrix.Transform(value);

                LocalPosition = newPos;
            }
        }

        /// <summary>
        ///     Position offset of this entity relative to its parent.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public EntityCoordinates Coordinates
        {
            get
            {
                var valid = _parent.IsValid();
                return new EntityCoordinates(valid ? _parent : Owner, valid ? LocalPosition : Vector2.Zero);
            }
            // NOTE: This setter must be callable from before initialize (inheriting from AttachParent's note)
            set
            {
                // unless the parent is changing, nothing to do here
                if(value.EntityId == _parent && _anchored)
                    return;

                var sameParent = value.EntityId == _parent;

                if (!sameParent)
                {
                    // Need to set anchored before we update position so that we can clear snapgrid cells correctly.
                    if(_parent != EntityUid.Invalid) // Allow setting Transform.Parent in Prototypes
                        Anchored = false; // changing the parent un-anchors the entity
                }

                var oldPosition = Coordinates;
                _localPosition = value.Position;
                var changedParent = false;

                if (!sameParent)
                {
                    var xformQuery = _entMan.GetEntityQuery<TransformComponent>();
                    changedParent = true;
                    var newParent = xformQuery.GetComponent(value.EntityId);

                    DebugTools.Assert(newParent != this,
                        $"Can't parent a {nameof(TransformComponent)} to itself.");

                    if (newParent.LifeStage > ComponentLifeStage.Running ||
                        _entMan.GetComponent<MetaDataComponent>(newParent.Owner).EntityLifeStage > EntityLifeStage.MapInitialized)
                    {
                        var msg = $"Attempted to re-parent to a terminating object. Entity: {_entMan.ToPrettyString(Owner)}, new parent: {_entMan.ToPrettyString(value.EntityId)}";
#if EXCEPTION_TOLERANCE
                        Logger.Error(msg);
                        _entMan.DeleteEntity(Owner);
#else
                        throw new InvalidOperationException(msg);
#endif
                    }

                    // That's already our parent, don't bother attaching again.

                    var oldParent = _parent.IsValid() ? xformQuery.GetComponent(_parent) : null;
                    var uid = Owner;
                    oldParent?._children.Remove(uid);
                    newParent._children.Add(uid);

                    // offset position from world to parent
                    _parent = value.EntityId;
                    var oldMapId = MapID;
                    ChangeMapId(newParent.MapID, xformQuery);

                    // Cache new GridID before raising the event.
                    _entMan.EntitySysManager.GetEntitySystem<SharedTransformSystem>().SetGridId(this, FindGridEntityId(xformQuery), xformQuery);

                    // preserve world rotation
                    if (LifeStage == ComponentLifeStage.Running)
                        LocalRotation += (oldParent?.WorldRotation ?? Angle.Zero) - newParent.WorldRotation;

                    var entParentChangedMessage = new EntParentChangedMessage(Owner, oldParent?.Owner, oldMapId, this);
                    _entMan.EventBus.RaiseLocalEvent(Owner, ref entParentChangedMessage, true);
                }

                // These conditions roughly emulate the effects of the code before I changed things,
                //  in regards to when to rebuild matrices.
                // This may not in fact be the right thing.
                if (changedParent || !DeferUpdates)
                    RebuildMatrices();

                Dirty(_entMan);

                if (!DeferUpdates)
                {
                    //TODO: This is a hack, look into WHY we can't call GridPosition before the comp is Running
                    if (Running)
                    {
                        if (!oldPosition.Equals(Coordinates))
                        {
                            var moveEvent = new MoveEvent(Owner, oldPosition, Coordinates, this, _gameTiming.ApplyingState);
                            _entMan.EventBus.RaiseLocalEvent(Owner, ref moveEvent, true);
                        }
                    }
                }
                else
                {
                    _oldCoords ??= oldPosition;
                }
            }
        }

        /// <summary>
        ///     Current position offset of the entity relative to the world.
        ///     This is effectively a more complete version of <see cref="WorldPosition"/>
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public MapCoordinates MapPosition => new(WorldPosition, MapID);

        /// <summary>
        ///     Local offset of this entity relative to its parent
        ///     (<see cref="Parent"/> if it's not null, to <see cref="GridID"/> otherwise).
        /// </summary>
        [Animatable]
        [ViewVariables(VVAccess.ReadWrite)]
        public Vector2 LocalPosition
        {
            get => _localPosition;
            set
            {
                if(Anchored)
                    return;

                if (_localPosition.EqualsApprox(value))
                    return;

                var oldGridPos = Coordinates;
                _localPosition = value;
                Dirty(_entMan);

                if (!DeferUpdates)
                {
                    RebuildMatrices();
                    var moveEvent = new MoveEvent(Owner, oldGridPos, Coordinates, this, _gameTiming.ApplyingState);
                    _entMan.EventBus.RaiseLocalEvent(Owner, ref moveEvent, true);
                }
                else
                {
                    _oldCoords ??= oldGridPos;
                }
            }
        }

        /// <summary>
        /// Is this transform anchored to a grid tile?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Anchored
        {
            get => _anchored;
            set
            {
                // This will be set again when the transform starts, actually anchoring it.
                if (LifeStage < ComponentLifeStage.Starting)
                {
                    _anchored = value;
                }
                else if (value && !_anchored && _mapManager.TryFindGridAt(MapPosition, out var grid))
                {
                    _anchored = _entMan.EntitySysManager.GetEntitySystem<SharedTransformSystem>().AnchorEntity(this, grid);
                }
                else if (!value && _anchored)
                {
                    // An anchored entity is always parented to the grid.
                    // If Transform.Anchored is true in the prototype but the entity was not spawned with a grid as the parent,
                    // then this will be false.
                    _entMan.EntitySysManager.GetEntitySystem<SharedTransformSystem>().Unanchor(this);
                }
            }
        }

        [ViewVariables]
        public IEnumerable<TransformComponent> Children
        {
            get
            {
                if (_children.Count == 0) yield break;

                var xforms = _entMan.GetEntityQuery<TransformComponent>();
                var children = ChildEnumerator;

                while (children.MoveNext(out var child))
                {
                    yield return xforms.GetComponent(child.Value);
                }
            }
        }

        [ViewVariables] public IEnumerable<EntityUid> ChildEntities => _children;

        public TransformChildrenEnumerator ChildEnumerator => new(_children.GetEnumerator());

        [ViewVariables] public int ChildCount => _children.Count;

        [ViewVariables]
        public Vector2? LerpDestination
        {
            get => _nextPosition;
            internal set
            {
                _nextPosition = value;
                ActivelyLerping = true;
            }
        }

        [ViewVariables]
        internal Angle? LerpAngle
        {
            get => _nextRotation;
            set
            {
                _nextRotation = value;
                ActivelyLerping = true;
            }
        }

        [ViewVariables] internal Vector2 LerpSource => _prevPosition;
        [ViewVariables] internal Angle LerpSourceAngle => _prevRotation;

        [ViewVariables] internal EntityUid LerpParent { get; set; }

        internal EntityUid? FindGridEntityId(EntityQuery<TransformComponent> xformQuery)
        {
            if (_entMan.HasComponent<IMapComponent>(Owner))
            {
                return null;
            }

            if (_entMan.TryGetComponent(Owner, out IMapGridComponent? gridComponent))
            {
                return Owner;
            }

            if (_parent.IsValid())
            {
                return xformQuery.GetComponent(_parent).GridUid;
            }

            return _mapManager.TryFindGridAt(MapID, WorldPosition, out var mapgrid) ? mapgrid.GridEntityId : null;
        }

        /// <summary>
        ///     Run MoveEvent, RotateEvent, and UpdateEntityTree updates.
        /// </summary>
        public void RunDeferred()
        {
            // if we resolved to (close enough) to the OG position then no update.
            if ((_oldCoords == null || _oldCoords.Equals(Coordinates)) &&
                (_oldLocalRotation == null || _oldLocalRotation.Equals(_localRotation)))
            {
                return;
            }

            RebuildMatrices();

            if (_oldCoords != null)
            {
                var moveEvent = new MoveEvent(Owner, _oldCoords.Value, Coordinates, this, _gameTiming.ApplyingState);
                _entMan.EventBus.RaiseLocalEvent(Owner, ref moveEvent, true);
                _oldCoords = null;
            }

            if (_oldLocalRotation != null)
            {
                var rotateEvent = new RotateEvent(Owner, _oldLocalRotation.Value, _localRotation, this);
                _entMan.EventBus.RaiseLocalEvent(Owner, ref rotateEvent, true);
                _oldLocalRotation = null;
            }
        }

        /// <summary>
        /// Detaches this entity from its parent.
        /// </summary>
        public void AttachToGridOrMap()
        {
            bool TerminatingOrDeleted(EntityUid uid)
            {
                return !_entMan.TryGetComponent(uid, out MetaDataComponent? meta)
                       || meta.EntityLifeStage >= EntityLifeStage.Terminating;
            }

            // nothing to do
            if (!_parent.IsValid())
                return;

            var mapPos = MapPosition;

            EntityUid newMapEntity;
            if (_mapManager.TryFindGridAt(mapPos, out var mapGrid) && !TerminatingOrDeleted(mapGrid.GridEntityId))
            {
                newMapEntity = mapGrid.GridEntityId;
            }
            else if (_mapManager.HasMapEntity(mapPos.MapId)
                     && _mapManager.GetMapEntityIdOrThrow(mapPos.MapId) is var mapEnt
                     && !TerminatingOrDeleted(mapEnt))
            {
                newMapEntity = _mapManager.GetMapEntityIdOrThrow(mapPos.MapId);
            }
            else
            {
                if (!_mapManager.IsMap(Owner))
                    Logger.Warning($"Detached a non-map entity ({_entMan.ToPrettyString(Owner)}) to null-space. Unless this entity is being deleted, this should not happen.");

                DetachParentToNull();
                return;
            }

            // this would be a no-op
            if (newMapEntity == _parent)
            {
                return;
            }

            AttachParent(newMapEntity);

            // Technically we're not moving, just changing parent.
            DeferUpdates = true;
            WorldPosition = mapPos.Position;
            DeferUpdates = false;

            Dirty(_entMan);
        }

        [Obsolete("Use transform system")]
        public void DetachParentToNull()
        {
            _entMan.EntitySysManager.GetEntitySystem<SharedTransformSystem>().DetachParentToNull(this);
        }

        /// <summary>
        /// Sets another entity as the parent entity, maintaining world position.
        /// </summary>
        /// <param name="newParent"></param>
        public void AttachParent(TransformComponent newParent)
        {
            //NOTE: This function must be callable from before initialize

            // don't attach to something we're already attached to
            if (ParentUid == newParent.Owner)
                return;

            DebugTools.Assert(newParent != this,
                $"Can't parent a {nameof(TransformComponent)} to itself.");

            // offset position from world to parent, and set
            Coordinates = new EntityCoordinates(newParent.Owner, newParent.InvWorldMatrix.Transform(WorldPosition));
        }

        internal void ChangeMapId(MapId newMapId, EntityQuery<TransformComponent> xformQuery)
        {
            if (newMapId == MapID)
                return;

            //Set Paused state
            var mapPaused = _mapManager.IsMapPaused(newMapId);
            var metaEnts = _entMan.GetEntityQuery<MetaDataComponent>();
            var metaData = metaEnts.GetComponent(Owner);
            var metaSystem = _entMan.EntitySysManager.GetEntitySystem<MetaDataSystem>();
            metaSystem.SetEntityPaused(Owner, mapPaused, metaData);

            MapID = newMapId;
            UpdateChildMapIdsRecursive(MapID, mapPaused, xformQuery, metaEnts, metaSystem);
        }

        internal void UpdateChildMapIdsRecursive(
            MapId newMapId,
            bool mapPaused,
            EntityQuery<TransformComponent> xformQuery,
            EntityQuery<MetaDataComponent> metaQuery,
            MetaDataSystem system)
        {
            var childEnumerator = ChildEnumerator;

            while (childEnumerator.MoveNext(out var child))
            {
                //Set Paused state
                var metaData = metaQuery.GetComponent(child.Value);
                system.SetEntityPaused(child.Value, mapPaused, metaData);

                var concrete = xformQuery.GetComponent(child.Value);

                concrete.MapID = newMapId;

                if (concrete.ChildCount != 0)
                {
                    concrete.UpdateChildMapIdsRecursive(newMapId, mapPaused, xformQuery, metaQuery, system);
                }
            }
        }

        public void AttachParent(EntityUid parent)
        {
            var transform = _entMan.GetComponent<TransformComponent>(parent);
            AttachParent(transform);
        }

        /// <summary>
        /// Get the WorldPosition and WorldRotation of this entity faster than each individually.
        /// </summary>
        public (Vector2 WorldPosition, Angle WorldRotation) GetWorldPositionRotation()
        {
            // Worldmatrix needs calculating anyway for worldpos so we'll just drop it.
            var (worldPos, worldRot, _) = GetWorldPositionRotationMatrix();
            return (worldPos, worldRot);
        }

        /// <see cref="GetWorldPositionRotation()"/>
        public (Vector2 WorldPosition, Angle WorldRotation) GetWorldPositionRotation(EntityQuery<TransformComponent> xforms)
        {
            var (worldPos, worldRot, _) = GetWorldPositionRotationMatrix(xforms);
            return (worldPos, worldRot);
        }

        /// <summary>
        /// Get the WorldPosition, WorldRotation, and WorldMatrix of this entity faster than each individually.
        /// </summary>
        public (Vector2 WorldPosition, Angle WorldRotation, Matrix3 WorldMatrix) GetWorldPositionRotationMatrix(EntityQuery<TransformComponent> xforms)
        {
            var parent = _parent;
            var worldRot = _localRotation;
            var worldMatrix = _localMatrix;

            // By doing these all at once we can elide multiple IsValid + GetComponent calls
            while (parent.IsValid())
            {
                var xform = xforms.GetComponent(parent);
                worldRot += xform.LocalRotation;
                var parentMatrix = xform._localMatrix;
                Matrix3.Multiply(in worldMatrix, in parentMatrix, out var result);
                worldMatrix = result;
                parent = xform.ParentUid;
            }

            var worldPosition = new Vector2(worldMatrix.R0C2, worldMatrix.R1C2);

            return (worldPosition, worldRot, worldMatrix);
        }

        /// <summary>
        /// Get the WorldPosition, WorldRotation, and WorldMatrix of this entity faster than each individually.
        /// </summary>
        public (Vector2 WorldPosition, Angle WorldRotation, Matrix3 WorldMatrix) GetWorldPositionRotationMatrix()
        {
            var xforms = _entMan.GetEntityQuery<TransformComponent>();
            return GetWorldPositionRotationMatrix(xforms);
        }

        /// <summary>
        /// Get the WorldPosition, WorldRotation, and InvWorldMatrix of this entity faster than each individually.
        /// </summary>
        public (Vector2 WorldPosition, Angle WorldRotation, Matrix3 InvWorldMatrix) GetWorldPositionRotationInvMatrix()
        {
            var xformQuery = _entMan.GetEntityQuery<TransformComponent>();
            return GetWorldPositionRotationInvMatrix(xformQuery);
        }

        /// <summary>
        /// Get the WorldPosition, WorldRotation, and InvWorldMatrix of this entity faster than each individually.
        /// </summary>
        public (Vector2 WorldPosition, Angle WorldRotation, Matrix3 InvWorldMatrix) GetWorldPositionRotationInvMatrix(EntityQuery<TransformComponent> xformQuery)
        {
            var (worldPos, worldRot, _, invWorldMatrix) = GetWorldPositionRotationMatrixWithInv(xformQuery);
            return (worldPos, worldRot, invWorldMatrix);
        }

        /// <summary>
        /// Get the WorldPosition, WorldRotation, WorldMatrix, and InvWorldMatrix of this entity faster than each individually.
        /// </summary>
        public (Vector2 WorldPosition, Angle WorldRotation, Matrix3 WorldMatrix, Matrix3 InvWorldMatrix) GetWorldPositionRotationMatrixWithInv()
        {
            var xformQuery = _entMan.GetEntityQuery<TransformComponent>();
            return GetWorldPositionRotationMatrixWithInv(xformQuery);
        }

        /// <summary>
        /// Get the WorldPosition, WorldRotation, WorldMatrix, and InvWorldMatrix of this entity faster than each individually.
        /// </summary>
        public (Vector2 WorldPosition, Angle WorldRotation, Matrix3 WorldMatrix, Matrix3 InvWorldMatrix) GetWorldPositionRotationMatrixWithInv(EntityQuery<TransformComponent> xformQuery)
        {
            var parent = _parent;
            var worldRot = _localRotation;
            var invMatrix = _invLocalMatrix;
            var worldMatrix = _localMatrix;

            // By doing these all at once we can elide multiple IsValid + GetComponent calls
            while (parent.IsValid())
            {
                var xform = xformQuery.GetComponent(parent);
                worldRot += xform.LocalRotation;

                var parentMatrix = xform._localMatrix;
                Matrix3.Multiply(in worldMatrix, in parentMatrix, out var result);
                worldMatrix = result;

                var parentInvMatrix = xform._invLocalMatrix;
                Matrix3.Multiply(in parentInvMatrix, in invMatrix, out var invResult);
                invMatrix = invResult;

                parent = xform.ParentUid;
            }

            var worldPosition = new Vector2(worldMatrix.R0C2, worldMatrix.R1C2);

            return (worldPosition, worldRot, worldMatrix, invMatrix);
        }

        internal void RebuildMatrices()
        {
            var pos = _localPosition;

            if (!_parent.IsValid()) // Root Node
                pos = Vector2.Zero;

            var rot = (float)_localRotation.Theta;

            _localMatrix = Matrix3.CreateTransform(pos.X, pos.Y, rot);
            _invLocalMatrix = Matrix3.CreateInverseTransform(pos.X, pos.Y, rot);
        }

        public string GetDebugString()
        {
            return $"pos/rot/wpos/wrot: {Coordinates}/{LocalRotation}/{WorldPosition}/{WorldRotation}";
        }

        internal void SetAnchored(bool value, bool issueEvent = true)
        {
            _anchored = value;
            Dirty(_entMan);

            if (issueEvent)
            {
                var anchorStateChangedEvent = new AnchorStateChangedEvent(this, false);
                _entMan.EventBus.RaiseLocalEvent(Owner, ref anchorStateChangedEvent, true);
            }
        }
    }

    /// <summary>
    ///     Raised whenever an entity moves.
    ///     There is no guarantee it will be raised if they move in worldspace, only when moved relative to their parent.
    /// </summary>
    [ByRefEvent]
    public readonly struct MoveEvent
    {
        public MoveEvent(EntityUid sender, EntityCoordinates oldPos, EntityCoordinates newPos, TransformComponent component, bool stateHandling)
        {
            Sender = sender;
            OldPosition = oldPos;
            NewPosition = newPos;
            Component = component;
            FromStateHandling = stateHandling;
        }

        public readonly EntityUid Sender;
        public readonly EntityCoordinates OldPosition;
        public readonly EntityCoordinates NewPosition;
        public readonly TransformComponent Component;

        /// <summary>
        ///     If true, this event was generated during component state handling. This means it can be ignored in some instances.
        /// </summary>
        public readonly bool FromStateHandling;
    }

    /// <summary>
    ///     Raised whenever this entity rotates in relation to their parent.
    /// </summary>
    [ByRefEvent]
    public readonly struct RotateEvent
    {
        public RotateEvent(EntityUid sender, Angle oldRotation, Angle newRotation, TransformComponent xform)
        {
            Sender = sender;
            OldRotation = oldRotation;
            NewRotation = newRotation;
            Component = xform;
        }

        public readonly EntityUid Sender;
        public readonly Angle OldRotation;
        public readonly Angle NewRotation;
        public readonly TransformComponent Component;
    }

    public struct TransformChildrenEnumerator : IDisposable
    {
        private HashSet<EntityUid>.Enumerator _children;

        public TransformChildrenEnumerator(HashSet<EntityUid>.Enumerator children)
        {
            _children = children;
        }

        public bool MoveNext([NotNullWhen(true)] out EntityUid? child)
        {
            if (!_children.MoveNext())
            {
                child = null;
                return false;
            }

            child = _children.Current;
            return true;
        }

        public void Dispose()
        {
            _children.Dispose();
        }
    }

    /// <summary>
    /// Raised when the Anchor state of the transform is changed.
    /// </summary>
    [ByRefEvent]
    public readonly struct AnchorStateChangedEvent
    {
        public readonly TransformComponent Transform;
        public EntityUid Entity => Transform.Owner;
        public bool Anchored => Transform.Anchored;

        /// <summary>
        ///     If true, the entity is being detached to null-space
        /// </summary>
        public readonly bool Detaching;

        public AnchorStateChangedEvent(TransformComponent transform, bool detaching)
        {
            Detaching = detaching;
            Transform = transform;
        }
    }

    /// <summary>
    /// Raised when an entity is re-anchored to another grid.
    /// </summary>
    [ByRefEvent]
    public readonly struct ReAnchorEvent
    {
        public readonly EntityUid Entity;
        public readonly EntityUid OldGrid;
        public readonly EntityUid Grid;

        /// <summary>
        /// Tile on both the old and new grid being re-anchored.
        /// </summary>
        public readonly Vector2i TilePos;

        public ReAnchorEvent(EntityUid uid, EntityUid oldGrid, EntityUid grid, Vector2i tilePos)
        {
            Entity = uid;
            OldGrid = oldGrid;
            Grid = grid;
            TilePos = tilePos;
        }
    }
}
