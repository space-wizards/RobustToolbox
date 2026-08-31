using System;
using System.Collections.Generic;
using System.Numerics;
using JetBrains.Annotations;
using Robust.Shared.Animations;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    ///     Stores the relative and global position and orientation of the entity.<br/>
    ///     This also tracks the overall transform hierarchy, which allows entities to be children of other entities
    ///     and move when their parent moves cheaply.
    /// </summary>
    /// <seealso cref="SharedTransformSystem"/>
    [RegisterComponent, NetworkedComponent]
    public sealed partial class TransformComponent : Component, IComponentDebug, IComponentDelta
    {
        [Dependency] private IEntityManager _entMan = default!;

        // Currently this field just exists for VV. In future, it might become a real field
        [ViewVariables, PublicAPI]
        private NetEntity NetParent => _entMan.GetNetEntity(_parent);

        [DataField("parent")] internal EntityUid _parent;

        [DataField("pos")] internal Vector3 _localPosition = Vector3.Zero; // holds offset from parent

        [DataField("rot")] internal Quaternion _localRotation = Quaternion.Identity; // local orientation

        [DataField("scale")] internal Vector3 _localScale = Vector3.One;

        [DataField("noRot")] internal bool _noLocalRotation;

        [DataField("anchored")]
        internal bool _anchored;

        /// <inheritdoc />
        public GameTick LastUnclassifiedDirty { get; set; }

        /// <inheritdoc />
        public GameTick[] LastModifiedFields { get; set; } = default!;

        /// <summary>
        /// Indicates this entity can traverse grids.
        /// </summary>
        [DataField]
        public bool GridTraversal = true;

        /// <summary>
        ///     The broadphase that this entity is currently stored on, if any.
        /// </summary>
        /// <remarks>
        ///     Maybe this should be moved to its own component eventually, but at least currently comps are not structs
        ///     and this data is required whenever any entity moves, so this will just save a component lookup.
        /// </remarks>
        [ViewVariables]
        internal BroadphaseData? Broadphase;

        internal bool MatricesDirty = true;
        private Matrix4x4 _localMatrix = Matrix4x4.Identity;
        private Matrix4x4 _invLocalMatrix = Matrix4x4.Identity;

        // these should just be system methods, but existing component functions like InvWorldMatrix still rely on
        // getting these so those have to be fully ECS-ed first.
        public Matrix4x4 LocalMatrix
        {
            get
            {
                if (MatricesDirty)
                    RebuildMatrices();
                return _localMatrix;
            }
        }
        public Matrix4x4 InvLocalMatrix
        {
            get
            {
                if (MatricesDirty)
                    RebuildMatrices();
                return _invLocalMatrix;
            }
        }

        // used for lerping

        [ViewVariables]
        public Vector3? NextPosition { get; internal set; }

        [ViewVariables]
        public Quaternion? NextRotation { get; internal set; }

        [ViewVariables]
        public Vector3 PrevPosition { get; internal set; }

        [ViewVariables]
        public Quaternion PrevRotation { get; internal set; } = Quaternion.Identity;

        [ViewVariables]
        public Vector3? NextScale { get; internal set; }

        [ViewVariables]
        public Vector3 PrevScale { get; internal set; } = Vector3.One;

        [ViewVariables] public bool ActivelyLerping;

        [ViewVariables] public GameTick LastLerp = GameTick.Zero;

        [ViewVariables] internal readonly HashSet<EntityUid> _children = new();

        /// <summary>
        ///     Returns the index of the map which this object is on
        /// </summary>
        [ViewVariables]
        public MapId MapID { get; internal set; }

        internal bool _mapIdInitialized;
        internal bool _gridInitialized;

        /// <summary>
        ///     The EntityUid of the map which this object is on, if any.
        /// </summary>
        public EntityUid? MapUid { get; internal set; }

        /// <summary>
        ///     The EntityUid of the grid which this object is on, if any.
        /// </summary>
        [ViewVariables]
        public EntityUid? GridUid => _gridUid;

        [Access(typeof(SharedTransformSystem))]
        internal EntityUid? _gridUid = null;

        /// <summary>
        ///     Disables or enables to ability to locally rotate the entity. When set it removes any local rotation.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool NoLocalRotation
        {
            get => _noLocalRotation;
            [Obsolete("Use SharedTransformSystem.SetNoLocalRotation() instead")]
            set
            {
                if (value)
                    LocalRotation = Quaternion.Identity;

                _noLocalRotation = value;
                _entMan.DirtyField(Owner, this, nameof(NoLocalRotation));
            }
        }

        /// <summary>
        ///     Current rotation offset of the entity.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [Animatable]
        public Quaternion LocalRotation
        {
            get => _localRotation;
            [Obsolete("Use SharedTransformSystem.SetLocalRotation() instead")]
            set
            {
                if(_noLocalRotation)
                    return;

                value = Quaternion.Normalize(value);

                if (MathF.Abs(Quaternion.Dot(_localRotation, value)) > 0.999999f)
                    return;

                var oldRotation = _localRotation;
                _localRotation = value;
                var meta = _entMan.GetComponent<MetaDataComponent>(Owner);
                _entMan.DirtyField(Owner, this, nameof(LocalRotation), meta);
                MatricesDirty = true;

                if (!Initialized)
                    return;

                _entMan.System<SharedTransformSystem>().RaiseMoveEvent((Owner, this, meta), _parent, _localPosition, oldRotation, MapUid, checkTraversal: false);
            }
        }

        /// <summary>
        ///     Scale of the entity relative to its parent on the X, Y and Z axes.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [Animatable]
        public Vector3 LocalScale
        {
            get => _localScale;
            [Obsolete("Use SharedTransformSystem.SetLocalScale() instead")]
            set
            {
                if (!float.IsFinite(value.X) || !float.IsFinite(value.Y) || !float.IsFinite(value.Z) ||
                    value.X == 0f || value.Y == 0f || value.Z == 0f)
                    throw new ArgumentOutOfRangeException(nameof(value), "Transform scale must be finite and non-zero on every axis.");

                if (_localScale.EqualsApprox(value))
                    return;

                var oldScale = _localScale;
                _localScale = value;
                MatricesDirty = true;
                _entMan.Dirty(Owner, this);

                if (Initialized)
                {
                    var ev = new TransformScaleChangedEvent(Owner, oldScale, value);
                    _entMan.EventBus.RaiseLocalEvent(Owner, ref ev);
                }
            }
        }

        /// <summary>
        ///     Current world rotation of the entity.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [Obsolete("Use SharedTransformSystem.Get/SetWorldRotation() instead")]
        public Quaternion WorldRotation
        {
            get
            {
                var parent = _parent;
                var xformQuery = _entMan.GetEntityQuery<TransformComponent>();
                var rotation = _localRotation;

                while (parent.IsValid())
                {
                    var parentXform = xformQuery.GetComponent(parent);
                    rotation = Quaternion.Normalize(Quaternion.Concatenate(rotation, parentXform._localRotation));
                    parent = parentXform.ParentUid;
                }

                return rotation;
            }
            set
            {
                if (NoLocalRotation)
                    return;

                var parentRotation = _parent.IsValid()
                    ? _entMan.GetComponent<TransformComponent>(_parent).WorldRotation
                    : Quaternion.Identity;
                LocalRotation = Quaternion.Normalize(Quaternion.Concatenate(value, Quaternion.Inverse(parentRotation)));
            }
        }

        // lazy VV convenience variable.
        [ViewVariables]
        private TransformComponent? _parentXform => !_parent.IsValid() ? null : _entMan.GetComponent<TransformComponent>(_parent);

        /// <summary>
        /// The UID of the parent entity that this entity is attached to.
        /// </summary>
        public EntityUid ParentUid  => _parent;

        /// <summary>
        ///     Matrix for transforming points from local to world space.
        /// </summary>
        [Obsolete("Use SharedTransformSystem.GetWorldMatrix() instead")]
        public Matrix4x4 WorldMatrix
        {
            get
            {
                var xformQuery = _entMan.GetEntityQuery<TransformComponent>();
                var parent = _parent;
                var myMatrix = LocalMatrix;

                while (parent.IsValid())
                {
                    var parentXform = xformQuery.GetComponent(parent);
                    var parentMatrix = parentXform.LocalMatrix;
                    parent = parentXform.ParentUid;

                    var result = Matrix4x4.Multiply(myMatrix, parentMatrix);
                    myMatrix = result;
                }

                return myMatrix;
            }
        }

        /// <summary>
        ///     Matrix for transforming points from world to local space.
        /// </summary>
        [Obsolete("Use SharedTransformSystem.GetInvWorldMatrix() instead")]
        public Matrix4x4 InvWorldMatrix
        {
            get
            {
                var xformQuery = _entMan.GetEntityQuery<TransformComponent>();
                var parent = _parent;
                var myMatrix = InvLocalMatrix;

                while (parent.IsValid())
                {
                    var parentXform = xformQuery.GetComponent(parent);
                    var parentMatrix = parentXform.InvLocalMatrix;
                    parent = parentXform.ParentUid;

                    var result = Matrix4x4.Multiply(parentMatrix, myMatrix);
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
        [Obsolete("Use SharedTransformSystem.Get/SetWorldPosition() instead")]
        public Vector3 WorldPosition
        {
            get
            {
                if (_parent.IsValid())
                {
                    // parent coords to world coords
                    return Vector3.Transform(_localPosition, _entMan.GetComponent<TransformComponent>(ParentUid).WorldMatrix);
                }
                else
                {
                    return Vector3.Zero;
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
                var newPos = Vector3.Transform(value, _entMan.GetComponent<TransformComponent>(ParentUid).InvWorldMatrix);

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
                return new EntityCoordinates(valid ? _parent : Owner, valid ? LocalPosition : Vector3.Zero);
            }
            [Obsolete("Use SharedTransformSystem.SetCoordinates() instead")]
            set => _entMan.EntitySysManager.GetEntitySystem<SharedTransformSystem>().SetCoordinates(Owner, this, value);
        }

        /// <summary>
        ///     Current position offset of the entity relative to the world.
        ///     This is effectively a more complete version of <see cref="WorldPosition"/>
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [Obsolete("Use SharedTransformSystem.GetMapCoordinates() instead")]
        public MapCoordinates MapPosition => new(WorldPosition, MapID);

        /// <summary>
        ///     Local offset of this entity relative to its parent
        ///     (<see cref="Parent"/> if it's not null, to <see cref="GridUid"/> otherwise).
        /// </summary>
        [Animatable]
        [ViewVariables(VVAccess.ReadWrite)]
        public Vector3 LocalPosition
        {
            get => _localPosition;
            [Obsolete("Use SharedTransformSystem.SetLocalPosition() instead")]
            set
            {
                if(Anchored)
                    return;

                if (_localPosition.EqualsApprox(value))
                    return;

                var oldParent = _parent;
                var oldPos = _localPosition;

                _localPosition = value;
                var meta = _entMan.GetComponent<MetaDataComponent>(Owner);
                _entMan.DirtyField(Owner, this, nameof(LocalPosition), meta);
                MatricesDirty = true;

                if (!Initialized)
                    return;

                _entMan.System<SharedTransformSystem>().RaiseMoveEvent((Owner, this, meta), oldParent, oldPos, _localRotation, MapUid);
            }
        }

        /// <summary>
        /// Is this transform anchored to a grid tile?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Anchored
        {
            get => _anchored;
            [Obsolete("Use the SharedTransformSystem.AnchorEntity/Unanchor methods instead.")]
            set
            {
                // This will be set again when the transform initializes, actually anchoring it.
                if (!Initialized)
                {
                    _anchored = value;
                }
                else if (value && !_anchored && _entMan.EntitySysManager.GetEntitySystem<SharedMapSystem>().TryFindGridAt(MapPosition, out _, out var grid))
                {
                    _anchored = _entMan.EntitySysManager.GetEntitySystem<SharedTransformSystem>().AnchorEntity(Owner, this, grid);
                }
                else if (!value && _anchored)
                {
                    // An anchored entity is always parented to the grid.
                    // If Transform.Anchored is true in the prototype but the entity was not spawned with a grid as the parent,
                    // then this will be false.
                    _entMan.EntitySysManager.GetEntitySystem<SharedTransformSystem>().Unanchor(Owner, this);
                }
            }
        }

        public TransformChildrenEnumerator ChildEnumerator => new(_children.GetEnumerator());

        [ViewVariables] public int ChildCount => _children.Count;

        [ViewVariables] public EntityUid LerpParent;
        public bool PredictedLerp;

        /// <summary>
        /// Detaches this entity from its parent.
        /// </summary>
        [Obsolete("Use SharedTransformSystem.AttachToGridOrMap() instead")]
        public void AttachToGridOrMap()
        {
            _entMan.EntitySysManager.GetEntitySystem<SharedTransformSystem>().AttachToGridOrMap(Owner, this);
        }

        [Obsolete("Use TransformSystem.SetParent() instead")]
        public void AttachParent(EntityUid parent)
        {
            _entMan.EntitySysManager.GetEntitySystem<SharedTransformSystem>().SetParent(Owner, this, parent, _entMan.GetEntityQuery<TransformComponent>());
        }

        /// <summary>
        /// Get the WorldPosition and WorldRotation of this entity faster than each individually.
        /// </summary>
        [Obsolete("Use SharedTransformSystem.GetWorldPositionRotation() instead")]
        public (Vector3 WorldPosition, Quaternion WorldRotation) GetWorldPositionRotation()
        {
            // Worldmatrix needs calculating anyway for worldpos so we'll just drop it.
            var (worldPos, worldRot, _) = GetWorldPositionRotationMatrix();
            return (worldPos, worldRot);
        }

        /// <summary>
        /// Get the WorldPosition, WorldRotation, and WorldMatrix of this entity faster than each individually.
        /// </summary>
        [Obsolete("Use SharedTransformSystem.GetWorldPositionRotationMatrix() instead")]
        public (Vector3 WorldPosition, Quaternion WorldRotation, Matrix4x4 WorldMatrix) GetWorldPositionRotationMatrix(EntityQuery<TransformComponent> xforms)
        {
            var parent = _parent;
            var worldRot = _localRotation;
            var worldMatrix = LocalMatrix;

            // By doing these all at once we can elide multiple IsValid + GetComponent calls
            while (parent.IsValid())
            {
                var xform = xforms.GetComponent(parent);
                worldRot = Quaternion.Normalize(Quaternion.Concatenate(worldRot, xform.LocalRotation));
                var parentMatrix = xform.LocalMatrix;
                var result = Matrix4x4.Multiply(worldMatrix, parentMatrix);
                worldMatrix = result;
                parent = xform.ParentUid;
            }

            var worldPosition = worldMatrix.Translation;

            return (worldPosition, worldRot, worldMatrix);
        }

        /// <summary>
        /// Get the WorldPosition, WorldRotation, and WorldMatrix of this entity faster than each individually.
        /// </summary>
        [Obsolete("Use SharedTransformSystem.GetWorldPositionRotationMatrix() instead")]
        public (Vector3 WorldPosition, Quaternion WorldRotation, Matrix4x4 WorldMatrix) GetWorldPositionRotationMatrix()
        {
            var xforms = _entMan.GetEntityQuery<TransformComponent>();
            return GetWorldPositionRotationMatrix(xforms);
        }

        /// <summary>
        /// Get the WorldPosition, WorldRotation, and InvWorldMatrix of this entity faster than each individually.
        /// </summary>
        [Obsolete("Use SharedTransformSystem.WorldPositionRotationInvMatrix() instead")]
        public (Vector3 WorldPosition, Quaternion WorldRotation, Matrix4x4 InvWorldMatrix) GetWorldPositionRotationInvMatrix(EntityQuery<TransformComponent> xformQuery)
        {
            var (worldPos, worldRot, _, invWorldMatrix) = GetWorldPositionRotationMatrixWithInv(xformQuery);
            return (worldPos, worldRot, invWorldMatrix);
        }

        /// <summary>
        /// Get the WorldPosition, WorldRotation, WorldMatrix, and InvWorldMatrix of this entity faster than each individually.
        /// </summary>
        [Obsolete("Use SharedTransformSystem.GetWorldPositionRotationMatrixWithInv() instead")]
        public (Vector3 WorldPosition, Quaternion WorldRotation, Matrix4x4 WorldMatrix, Matrix4x4 InvWorldMatrix) GetWorldPositionRotationMatrixWithInv()
        {
            var xformQuery = _entMan.GetEntityQuery<TransformComponent>();
            return GetWorldPositionRotationMatrixWithInv(xformQuery);
        }

        /// <summary>
        /// Get the WorldPosition, WorldRotation, WorldMatrix, and InvWorldMatrix of this entity faster than each individually.
        /// </summary>
        [Obsolete("Use SharedTransformSystem.GetWorldPositionRotationMatrixWithInv() instead")]
        public (Vector3 WorldPosition, Quaternion WorldRotation, Matrix4x4 WorldMatrix, Matrix4x4 InvWorldMatrix) GetWorldPositionRotationMatrixWithInv(EntityQuery<TransformComponent> xformQuery)
        {
            var parent = _parent;
            var worldRot = _localRotation;
            var invMatrix = InvLocalMatrix;
            var worldMatrix = LocalMatrix;

            // By doing these all at once we can avoid multiple IsValid + GetComponent calls
            while (parent.IsValid())
            {
                var xform = xformQuery.GetComponent(parent);
                worldRot = Quaternion.Normalize(Quaternion.Concatenate(worldRot, xform.LocalRotation));

                var parentMatrix = xform.LocalMatrix;
                var result = Matrix4x4.Multiply(worldMatrix, parentMatrix);
                worldMatrix = result;

                var parentInvMatrix = xform.InvLocalMatrix;
                var invResult = Matrix4x4.Multiply(parentInvMatrix, invMatrix);
                invMatrix = invResult;

                parent = xform.ParentUid;
            }

            var worldPosition = worldMatrix.Translation;

            return (worldPosition, worldRot, worldMatrix, invMatrix);
        }

        public void RebuildMatrices()
        {
            MatricesDirty = false;

            if (!_parent.IsValid()) // Root Node
            {
                _localMatrix = Matrix4x4.Identity;
                _invLocalMatrix = Matrix4x4.Identity;
            }

            _localMatrix = SpatialMath.CreateTransform(_localPosition, _localRotation, _localScale);
            _invLocalMatrix = SpatialMath.CreateInverseTransform(_localPosition, _localRotation, _localScale);
        }

        [Obsolete("Use SharedTransformSystem.GetDebugString() instead")]
        public string GetDebugString()
        {
            return $"pos/rot/wpos/wrot: {Coordinates}/{LocalRotation}/{WorldPosition}/{WorldRotation}";
        }
    }

    [ByRefEvent]
    public readonly record struct TransformScaleChangedEvent(EntityUid Entity, Vector3 OldScale, Vector3 NewScale);

    /// <summary>
    /// Raised directed at an entity whenever is position or rotation changes relative to their parent, or if their
    /// parent changed. Note that this event does not get broadcast. If you need to receive information about ALL
    /// move events, subscribe to the <see cref="SharedTransformSystem.OnGlobalMoveEvent"/>.
    /// </summary>
    [ByRefEvent]
    public readonly struct MoveEvent(
        Entity<TransformComponent, MetaDataComponent> entity,
        EntityCoordinates oldPos,
        EntityCoordinates newPos,
        Quaternion oldRotation,
        Quaternion newRotation)
    {
        public readonly Entity<TransformComponent, MetaDataComponent> Entity = entity;
        public readonly EntityCoordinates OldPosition = oldPos;
        public readonly EntityCoordinates NewPosition = newPos;
        public readonly Quaternion OldRotation = oldRotation;
        public readonly Quaternion NewRotation = newRotation;

        public EntityUid Sender => Entity.Owner;
        public TransformComponent Component => Entity.Comp1;

        public bool ParentChanged => NewPosition.EntityId != OldPosition.EntityId;
        public bool OnlyRotation => OldPosition.Equals(NewPosition);
    }

    public struct TransformChildrenEnumerator : IDisposable
    {
        private HashSet<EntityUid>.Enumerator _children;

        public TransformChildrenEnumerator(HashSet<EntityUid>.Enumerator children)
        {
            _children = children;
        }

        public bool MoveNext(out EntityUid child)
        {
            if (!_children.MoveNext())
            {
                child = default;
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
    public readonly struct AnchorStateChangedEvent(
        EntityUid entity,
        TransformComponent transform,
        bool detaching = false)
    {
        public readonly TransformComponent Transform = transform;
        public EntityUid Entity { get; } = entity;
        public bool Anchored => Transform.Anchored;

        /// <summary>
        ///     If true, the entity is being detached to null-space
        /// </summary>
        public readonly bool Detaching = detaching;
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
        public readonly TransformComponent Xform;

        /// <summary>
        /// Tile on both the old and new grid being re-anchored.
        /// </summary>
        public readonly Vector2i TilePos;

        public ReAnchorEvent(EntityUid uid, EntityUid oldGrid, EntityUid grid, Vector2i tilePos, TransformComponent xform)
        {
            Entity = uid;
            OldGrid = oldGrid;
            Grid = grid;
            TilePos = tilePos;
            Xform = xform;
        }
    }

    /// <summary>
    ///     Data used to store information about the broad-phase that any given entity is currently on.
    /// </summary>
    /// <remarks>
    ///     A null value means that this entity is simply not on a broadphase (e.g., in null-space or in a container).
    ///     An invalid entity UID indicates that this entity has intentionally been removed from broadphases and should
    ///     not automatically be re-added by movement events.
    /// </remarks>
    internal record struct BroadphaseData(EntityUid Uid, bool CanCollide, bool Static)
    {
        public bool IsValid() => Uid.IsValid();
        public bool Valid => IsValid();
        public static readonly BroadphaseData Invalid = default;

        // TODO include MapId if ever grids are allowed to enter null-space (leave PVS).
    }
}
