using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Animations;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Log; //Needed for release build, do not remove
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
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

        [DataField("parent")]
        private EntityUid _parent;
        [DataField("pos")]
        private Vector2 _localPosition = Vector2.Zero; // holds offset from grid, or offset from parent
        [DataField("rot")]
        private Angle _localRotation; // local rotation
        [DataField("noRot")]
        private bool _noLocalRotation;
        [DataField("anchored")]
        private bool _anchored;

        private Matrix3 _localMatrix = Matrix3.Identity;
        private Matrix3 _invLocalMatrix = Matrix3.Identity;

        private Vector2? _nextPosition;
        private Angle? _nextRotation;

        private Vector2 _prevPosition;
        private Angle _prevRotation;

        // Cache changes so we can distribute them after physics is done (better cache)
        private EntityCoordinates? _oldCoords;
        private Angle? _oldLocalRotation;

        /// <summary>
        ///     While updating did we actually defer anything?
        /// </summary>
        public bool UpdatesDeferred => _oldCoords != null || _oldLocalRotation != null;

        [ViewVariables(VVAccess.ReadWrite)]
        internal bool ActivelyLerping { get; set; }

        [ViewVariables] private readonly SortedSet<EntityUid> _children = new();

        [Dependency] private readonly IMapManager _mapManager = default!;

        public override string Name => "Transform";

        /// <summary>
        ///     Returns the index of the map which this object is on
        /// </summary>
        [ViewVariables]
        public MapId MapID { get; private set; }

        private bool _mapIdInitialized;

        /// <summary>
        ///     Defer updates to the EntityTree and MoveEvent calls if toggled.
        /// </summary>
        public bool DeferUpdates { get; set; }

        /// <summary>
        ///     Returns the index of the grid which this object is on
        /// </summary>
        [ViewVariables]
        public GridId GridID
        {
            get => _gridId;
            private set
            {
                if (_gridId.Equals(value)) return;

                _gridId = value;
                foreach (var transformComponent in Children)
                {
                    var child = transformComponent;
                    child.GridID = value;
                }
            }
        }

        private GridId _gridId = GridId.Invalid;

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
                Dirty();
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

                // Set _nextRotation to null to break any active lerps if this is a client side prediction.
                _nextRotation = null;
                _localRotation = value;
                Dirty();

                if (!DeferUpdates)
                {
                    RebuildMatrices();
                    var rotateEvent = new RotateEvent(Owner, oldRotation, _localRotation);
                    _entMan.EventBus.RaiseLocalEvent(Owner, ref rotateEvent);
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
                if (_parent.IsValid())
                {
                    return Parent!.WorldRotation + _localRotation;
                }

                return _localRotation;
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
            set => Parent = _entMan.GetComponent<TransformComponent>(value);
        }

        /// <summary>
        ///     Matrix for transforming points from local to world space.
        /// </summary>
        public Matrix3 WorldMatrix
        {
            get
            {
                if (_parent.IsValid())
                {
                    var parentMatrix = Parent!.WorldMatrix;
                    var myMatrix = GetLocalMatrix();
                    Matrix3.Multiply(ref myMatrix, ref parentMatrix, out var result);
                    return result;
                }

                return GetLocalMatrix();
            }
        }

        /// <summary>
        ///     Matrix for transforming points from world to local space.
        /// </summary>
        public Matrix3 InvWorldMatrix
        {
            get
            {
                if (_parent.IsValid())
                {
                    var matP = Parent!.InvWorldMatrix;
                    var myMatrix = GetLocalMatrixInv();
                    Matrix3.Multiply(ref matP, ref myMatrix, out var result);
                    return result;
                }

                return GetLocalMatrixInv();
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

                // float rounding error guard, if the offset is less than 1mm ignore it
                //if ((newPos - GetLocalPosition()).LengthSquared < 1.0E-3)
                //    return;

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
                    changedParent = true;
                    var newParent = _entMan.GetComponent<TransformComponent>(value.EntityId);

                    DebugTools.Assert(newParent != this,
                        $"Can't parent a {nameof(TransformComponent)} to itself.");

                    // That's already our parent, don't bother attaching again.

                    var oldParent = Parent;
                    var uid = Owner;
                    oldParent?._children.Remove(uid);
                    newParent._children.Add(uid);

                    // offset position from world to parent
                    _parent = value.EntityId;
                    ChangeMapId(newParent.MapID);

                    // Cache new GridID before raising the event.
                    GridID = GetGridIndex();

                    var entParentChangedMessage = new EntParentChangedMessage(Owner, oldParent?.Owner);
                    _entMan.EventBus.RaiseLocalEvent(Owner, ref entParentChangedMessage);
                }

                // These conditions roughly emulate the effects of the code before I changed things,
                //  in regards to when to rebuild matrices.
                // This may not in fact be the right thing.
                if (changedParent || !DeferUpdates)
                    RebuildMatrices();
                Dirty();

                if (!DeferUpdates)
                {
                    //TODO: This is a hack, look into WHY we can't call GridPosition before the comp is Running
                    if (Running)
                    {
                        if(!oldPosition.Position.Equals(Coordinates.Position))
                        {
                            var moveEvent = new MoveEvent(Owner, oldPosition, Coordinates, this);
                            _entMan.EventBus.RaiseLocalEvent(Owner, ref moveEvent);
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

                // Set _nextPosition to null to break any on-going lerps if this is done in a client side prediction.
                _nextPosition = null;

                var oldGridPos = Coordinates;
                _localPosition = value;
                Dirty();

                if (!DeferUpdates)
                {
                    RebuildMatrices();
                    var moveEvent = new MoveEvent(Owner, oldGridPos, Coordinates, this);
                    _entMan.EventBus.RaiseLocalEvent(Owner, ref moveEvent);
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
                else if (LifeStage == ComponentLifeStage.Starting)
                {
                    if (value && _mapManager.TryFindGridAt(MapPosition, out var grid))
                    {
                        _anchored = _entMan.GetComponent<IMapGridComponent>(grid.GridEntityId).AnchorEntity(this);
                    }
                    // If no grid found then unanchor it.
                    else
                    {
                        _anchored = false;
                    }
                }
                else if (value && !_anchored && _mapManager.TryFindGridAt(MapPosition, out var grid))
                {
                    _anchored = _entMan.GetComponent<IMapGridComponent>(grid.GridEntityId).AnchorEntity(this);
                }
                else if (!value && _anchored)
                {
                    // An anchored entity is always parented to the grid.
                    // If Transform.Anchored is true in the prototype but the entity was not spawned with a grid as the parent,
                    // then this will be false.
                    if (_entMan.TryGetComponent<IMapGridComponent>(ParentUid, out var gridComp))
                        gridComp.UnanchorEntity(this);
                    else
                        SetAnchored(false);
                }
            }
        }

        [ViewVariables]
        public IEnumerable<TransformComponent> Children =>
            _children.Select(u =>
            {
                return _entMan.GetComponent<TransformComponent>(u);
            });

        [ViewVariables] public IEnumerable<EntityUid> ChildEntities => _children;

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

        [ViewVariables] internal EntityUid LerpParent { get; private set; }

        protected override void Initialize()
        {
            base.Initialize();

            // Children MAY be initialized here before their parents are.
            // We do this whole dance to handle this recursively,
            // setting _mapIdInitialized along the way to avoid going to the IMapComponent every iteration.
            static MapId FindMapIdAndSet(TransformComponent p, IEntityManager entMan)
            {
                if (p._mapIdInitialized)
                {
                    return p.MapID;
                }

                MapId value;
                if (p._parent.IsValid())
                {
                    value = FindMapIdAndSet((TransformComponent) p.Parent!, entMan);
                }
                else
                {
                    // second level node, terminates recursion up the branch of the tree
                    if (entMan.TryGetComponent(p.Owner, out IMapComponent? mapComp))
                    {
                        value = mapComp.WorldMap;
                    }
                    else
                    {
                        throw new InvalidOperationException("Transform node does not exist inside scene tree!");
                    }
                }

                p.MapID = value;
                p._mapIdInitialized = true;
                return value;
            }

            if (!_mapIdInitialized)
            {
                FindMapIdAndSet(this, _entMan);

                _mapIdInitialized = true;
            }

            // Has to be done if _parent is set from ExposeData.
            if (_parent.IsValid())
            {
                // Note that _children is a SortedSet<EntityUid>,
                // so duplicate additions (which will happen) don't matter.
                ((TransformComponent) Parent!)._children.Add(Owner);
            }

            GridID = GetGridIndex();
            RebuildMatrices();
        }

        private GridId GetGridIndex()
        {
            if (_entMan.HasComponent<IMapComponent>(Owner))
            {
                return GridId.Invalid;
            }

            if (_entMan.TryGetComponent(Owner, out IMapGridComponent? gridComponent))
            {
                return gridComponent.GridIndex;
            }

            if (_parent.IsValid())
            {
                return Parent!.GridID;
            }

            return _mapManager.TryFindGridAt(MapID, WorldPosition, out var mapgrid) ? mapgrid.Index : GridId.Invalid;
        }

        protected override void Startup()
        {
            // Re-Anchor the entity if needed.
            if (_anchored)
                Anchored = true;

            base.Startup();

            // Keep the cached matrices in sync with the fields.
            Dirty();
        }

        /// <summary>
        ///     Run MoveEvent, RotateEvent, and UpdateEntityTree updates.
        /// </summary>
        public void RunDeferred(Box2 worldAABB)
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
                var moveEvent = new MoveEvent(Owner, _oldCoords.Value, Coordinates, this, worldAABB);
                _entMan.EventBus.RaiseLocalEvent(Owner, ref moveEvent);
                _oldCoords = null;
            }

            if (_oldLocalRotation != null)
            {
                var rotateEvent = new RotateEvent(Owner, _oldLocalRotation.Value, _localRotation, worldAABB);
                _entMan.EventBus.RaiseLocalEvent(Owner, ref rotateEvent);
                _oldLocalRotation = null;
            }
        }

        /// <summary>
        /// Detaches this entity from its parent.
        /// </summary>
        public void AttachToGridOrMap()
        {
            // nothing to do
            var oldParent = Parent;
            if (oldParent == null)
            {
                return;
            }

            var mapPos = MapPosition;

            EntityUid newMapEntity;
            if (_mapManager.TryFindGridAt(mapPos, out var mapGrid))
            {
                newMapEntity = mapGrid.GridEntityId;
            }
            else if (_mapManager.HasMapEntity(mapPos.MapId))
            {
                newMapEntity = _mapManager.GetMapEntityIdOrThrow(mapPos.MapId);
            }
            else
            {
                DetachParentToNull();
                return;
            }

            // this would be a no-op
            var oldParentEnt = oldParent.Owner;
            if (newMapEntity == oldParentEnt)
            {
                return;
            }

            AttachParent(newMapEntity);

            // Technically we're not moving, just changing parent.
            DeferUpdates = true;
            WorldPosition = mapPos.Position;
            DeferUpdates = false;

            Dirty();
        }

        public void DetachParentToNull()
        {
            var oldParent = Parent;
            if (oldParent == null)
            {
                return;
            }

            Anchored = false;

            var oldConcrete = (TransformComponent) oldParent;
            var uid = Owner;
            oldConcrete._children.Remove(uid);

            _parent = EntityUid.Invalid;
            var oldMapId = MapID;
            MapID = MapId.Nullspace;
            var entParentChangedMessage = new EntParentChangedMessage(Owner, oldParent?.Owner);
            _entMan.EventBus.RaiseLocalEvent(Owner, ref entParentChangedMessage);

            // Does it even make sense to call these since this is called purely from OnRemove right now?
            RebuildMatrices();
            MapIdChanged(oldMapId);
            Dirty();
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

        internal void ChangeMapId(MapId newMapId)
        {
            if (newMapId == MapID)
                return;

            var oldMapId = MapID;

            MapID = newMapId;
            MapIdChanged(oldMapId);
            UpdateChildMapIdsRecursive(MapID, _entMan);
        }

        private void UpdateChildMapIdsRecursive(MapId newMapId, IEntityManager entMan)
        {
            foreach (var child in _children)
            {
                var concrete = entMan.GetComponent<TransformComponent>(child);
                var old = concrete.MapID;

                concrete.MapID = newMapId;
                concrete.MapIdChanged(old);

                if (concrete.ChildCount != 0)
                {
                    concrete.UpdateChildMapIdsRecursive(newMapId, entMan);
                }
            }
        }

        private void MapIdChanged(MapId oldId)
        {
            _entMan.EventBus.RaiseLocalEvent(Owner, new EntMapIdChangedMessage(Owner, oldId));
        }

        public void AttachParent(EntityUid parent)
        {
            var transform = _entMan.GetComponent<TransformComponent>(parent);
            AttachParent(transform);
        }

        /// <summary>
        ///     Finds the transform of the entity located on the map itself
        /// </summary>
        public TransformComponent GetMapTransform()
        {
            if (Parent != null) //If we are not the final transform, query up the chain of parents
            {
                return Parent.GetMapTransform();
            }

            return this;
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

        /// <summary>
        /// Get the WorldPosition, WorldRotation, and WorldMatrix of this entity faster than each individually.
        /// </summary>
        public (Vector2 WorldPosition, Angle WorldRotation, Matrix3 WorldMatrix) GetWorldPositionRotationMatrix()
        {
            var parent = _parent;
            var worldRot = _localRotation;
            var worldMatrix = GetLocalMatrix();

            // By doing these all at once we can elide multiple IsValid + GetComponent calls
            while (parent.IsValid())
            {
                var xform = _entMan.GetComponent<TransformComponent>(parent);
                worldRot += xform.LocalRotation;
                var parentMatrix = xform.GetLocalMatrix();
                Matrix3.Multiply(ref worldMatrix, ref parentMatrix, out var result);
                worldMatrix = result;
                parent = xform.ParentUid;
            }

            var worldPosition = new Vector2(worldMatrix.R0C2, worldMatrix.R1C2);

            return (worldPosition, worldRot, worldMatrix);
        }

        /// <summary>
        /// Get the WorldPosition, WorldRotation, and InvWorldMatrix of this entity faster than each individually.
        /// </summary>
        public (Vector2 WorldPosition, Angle WorldRotation, Matrix3 InvWorldMatrix) GetWorldPositionRotationInvMatrix()
        {
            var parent = _parent;
            var worldRot = _localRotation;
            var invMatrix = GetLocalMatrixInv();
            var worldMatrix = GetLocalMatrix();

            // By doing these all at once we can elide multiple IsValid + GetComponent calls
            while (parent.IsValid())
            {
                var xform = _entMan.GetComponent<TransformComponent>(parent);
                worldRot += xform.LocalRotation;
                var parentMatrix = xform.GetLocalMatrix();
                Matrix3.Multiply(ref worldMatrix, ref parentMatrix, out var result);
                worldMatrix = result;

                var parentInvMatrix = xform.GetLocalMatrixInv();
                Matrix3.Multiply(ref invMatrix, ref parentInvMatrix, out var invResult);
                invMatrix = invResult;

                parent = xform.ParentUid;
            }

            var worldPosition = new Vector2(worldMatrix.R0C2, worldMatrix.R1C2);

            return (worldPosition, worldRot, invMatrix);
        }

        /// <summary>
        ///     Returns whether the entity of this transform contains the entity argument
        /// </summary>
        public bool ContainsEntity(TransformComponent entityTransform)
        {
            if (entityTransform.Parent == null) //Is the entity the scene root
            {
                return false;
            }

            if (this == entityTransform.Parent) //Is this the direct container of the entity
            {
                return true;
            }
            else
            {
                return
                    ContainsEntity(entityTransform
                        .Parent); //Recursively search up the entities containers for this object
            }
        }

        public override ComponentState GetComponentState()
        {
            return new TransformComponentState(_localPosition, LocalRotation, _parent, _noLocalRotation, _anchored);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (curState != null)
            {
                var newState = (TransformComponentState) curState;

                var newParentId = newState.ParentID;
                var rebuildMatrices = false;
                if (Parent?.Owner != newParentId)
                {
                    if (newParentId != _parent)
                    {
                        if (!newParentId.IsValid())
                        {
                            DetachParentToNull();
                        }
                        else
                        {
                            var entManager = _entMan;
                            if (!entManager.EntityExists(newParentId))
                            {
#if !EXCEPTION_TOLERANCE
                                throw new InvalidOperationException($"Unable to find new parent {newParentId}! This probably means the server never sent it.");
#else
                                Logger.ErrorS("transform", $"Unable to find new parent {newParentId}! Deleting {Owner}");
                                entManager.QueueDeleteEntity(Owner);
                                return;
#endif
                            }

                            AttachParent(entManager.GetComponent<TransformComponent>(newParentId));
                        }
                    }

                    rebuildMatrices = true;
                }

                if (LocalRotation != newState.Rotation)
                {
                    _localRotation = newState.Rotation;
                    rebuildMatrices = true;
                }

                if (!_localPosition.EqualsApprox(newState.LocalPosition))
                {
                    var oldPos = Coordinates;
                    _localPosition = newState.LocalPosition;

                    var ev = new MoveEvent(Owner, oldPos, Coordinates, this);
                    EntitySystem.Get<SharedTransformSystem>().DeferMoveEvent(ref ev);

                    rebuildMatrices = true;
                }

                _prevPosition = newState.LocalPosition;
                _prevRotation = newState.Rotation;

                Anchored = newState.Anchored;
                _noLocalRotation = newState.NoLocalRotation;

                // This is not possible, because client entities don't exist on the server, so the parent HAS to be a shared entity.
                // If this assert fails, the code above that sets the parent is broken.
                DebugTools.Assert(!_parent.IsClientSide(), "Transform received a state, but is still parented to a client entity.");

                // Whatever happened on the client, these should still be correct
                DebugTools.Assert(ParentUid == newState.ParentID);
                DebugTools.Assert(Anchored == newState.Anchored);

                if (rebuildMatrices)
                {
                    RebuildMatrices();
                }

                Dirty();
            }

            if (nextState is TransformComponentState nextTransform)
            {
                _nextPosition = nextTransform.LocalPosition;
                _nextRotation = nextTransform.Rotation;
                LerpParent = nextTransform.ParentID;
                ActivateLerp();
            }
            else
            {
                // this should cause the lerp to do nothing
                _nextPosition = null;
                _nextRotation = null;
                LerpParent = EntityUid.Invalid;
            }
        }

        public Matrix3 GetLocalMatrix()
        {
            return _localMatrix;
        }

        public Matrix3 GetLocalMatrixInv()
        {
            return _invLocalMatrix;
        }

        private void RebuildMatrices()
        {
            var pos = _localPosition;

            if (!_parent.IsValid()) // Root Node
                pos = Vector2.Zero;

            var rot = _localRotation.Theta;

            var posMat = Matrix3.CreateTranslation(pos);
            var rotMat = Matrix3.CreateRotation((float) rot);

            Matrix3.Multiply(ref rotMat, ref posMat, out var transMat);

            _localMatrix = transMat;

            var posImat = Matrix3.Invert(posMat);
            var rotImap = Matrix3.Invert(rotMat);

            Matrix3.Multiply(ref posImat, ref rotImap, out var itransMat);

            _invLocalMatrix = itransMat;
        }

        public string GetDebugString()
        {
            return $"pos/rot/wpos/wrot: {Coordinates}/{LocalRotation}/{WorldPosition}/{WorldRotation}";
        }

        private void ActivateLerp()
        {
            if (ActivelyLerping)
            {
                return;
            }

            ActivelyLerping = true;
            _entMan.EventBus.RaiseLocalEvent(Owner, new TransformStartLerpMessage(this));
        }

        /// <summary>
        ///     Serialized state of a TransformComponent.
        /// </summary>
        [Serializable, NetSerializable]
        internal class TransformComponentState : ComponentState
        {
            /// <summary>
            ///     Current parent entity of this entity.
            /// </summary>
            public readonly EntityUid ParentID;

            /// <summary>
            ///     Current position offset of the entity.
            /// </summary>
            public readonly Vector2 LocalPosition;

            /// <summary>
            ///     Current rotation offset of the entity.
            /// </summary>
            public readonly Angle Rotation;

            /// <summary>
            /// Is the transform able to be locally rotated?
            /// </summary>
            public readonly bool NoLocalRotation;

            /// <summary>
            /// True if the transform is anchored to a tile.
            /// </summary>
            public readonly bool Anchored;

            /// <summary>
            ///     Constructs a new state snapshot of a TransformComponent.
            /// </summary>
            /// <param name="localPosition">Current position offset of this entity.</param>
            /// <param name="rotation">Current direction offset of this entity.</param>
            /// <param name="parentId">Current parent transform of this entity.</param>
            /// <param name="noLocalRotation"></param>
            public TransformComponentState(Vector2 localPosition, Angle rotation, EntityUid parentId, bool noLocalRotation, bool anchored)
            {
                LocalPosition = localPosition;
                Rotation = rotation;
                ParentID = parentId;
                NoLocalRotation = noLocalRotation;
                Anchored = anchored;
            }
        }

        internal void SetAnchored(bool value)
        {
            _anchored = value;
            Dirty();

            var anchorStateChangedEvent = new AnchorStateChangedEvent(Owner, value);
            _entMan.EventBus.RaiseLocalEvent(Owner, ref anchorStateChangedEvent);
        }
    }

    /// <summary>
    ///     Raised whenever an entity moves.
    ///     There is no guarantee it will be raised if they move in worldspace, only when moved relative to their parent.
    /// </summary>
    public readonly struct MoveEvent
    {
        public MoveEvent(EntityUid sender, EntityCoordinates oldPos, EntityCoordinates newPos, TransformComponent component, Box2? worldAABB = null)
        {
            Sender = sender;
            OldPosition = oldPos;
            NewPosition = newPos;
            Component = component;
            WorldAABB = worldAABB;
        }

        public readonly EntityUid Sender;
        public readonly EntityCoordinates OldPosition;
        public readonly EntityCoordinates NewPosition;
        public readonly TransformComponent Component;

        /// <summary>
        ///     New AABB of the entity.
        /// </summary>
        public readonly Box2? WorldAABB;
    }

    /// <summary>
    ///     Raised whenever this entity rotates in relation to their parent.
    /// </summary>
    public readonly struct RotateEvent
    {
        public RotateEvent(EntityUid sender, Angle oldRotation, Angle newRotation, Box2? worldAABB = null)
        {
            Sender = sender;
            OldRotation = oldRotation;
            NewRotation = newRotation;
            WorldAABB = worldAABB;
        }

        public readonly EntityUid Sender;
        public readonly Angle OldRotation;
        public readonly Angle NewRotation;

        /// <summary>
        ///     New AABB of the entity.
        /// </summary>
        public readonly Box2? WorldAABB;
    }

    /// <summary>
    /// Raised when the Anchor state of the transform is changed.
    /// </summary>
    public readonly struct AnchorStateChangedEvent
    {
        public readonly EntityUid Entity;

        public readonly bool Anchored;

        public AnchorStateChangedEvent(EntityUid entity, bool anchored)
        {
            Entity = entity;
            Anchored = anchored;
        }
    }
}
