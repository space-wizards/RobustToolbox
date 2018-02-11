using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using System;
using SS14.Shared.Enums;
using SS14.Shared.GameObjects.Serialization;

namespace SS14.Server.GameObjects
{
    /// <summary>
    ///     Stores the position and orientation of the entity.
    /// </summary>
    public class TransformComponent : Component, IServerTransformComponent
    {
        /// <summary>
        ///     Current parent entity of this entity.
        /// </summary>
        public IServerTransformComponent Parent
        {
            get => !_parent.IsValid() ? null : IoCManager.Resolve<IServerEntityManager>().GetEntity(_parent).GetComponent<IServerTransformComponent>();
            private set => _parent = value?.Owner.Uid ?? EntityUid.Invalid;
        }

        ITransformComponent ITransformComponent.Parent => Parent;

        private EntityUid _parent;
        private Vector2 _position;
        private Angle _rotation;
        private MapId _mapID;
        private GridId _gridID;

        /// <inheritdoc />
        public MapId MapID
        {
            get => _mapID;
            private set => _mapID = value;
        }

        /// <inheritdoc />
        public GridId GridID
        {
            get => _gridID;
            private set => _gridID = value;
        }

        /// <summary>
        ///     Current rotation offset of the entity.
        /// </summary>
        public Angle Rotation
        {
            get => _rotation;
            set
            {
                _rotation = value;
                OnRotate?.Invoke(value);
            }
        }

        /// <inheritdoc />
        public override string Name => "Transform";

        /// <inheritdoc />
        public override uint? NetID => NetIDs.TRANSFORM;

        /// <inheritdoc />
        public event EventHandler<MoveEventArgs> OnMove;

        /// <inheritdoc />
        public event Action<Angle> OnRotate;

        /// <inheritdoc />
        public LocalCoordinates LocalPosition
        {
            get => Parent != null ? GetMapTransform().LocalPosition : new LocalCoordinates(_position, GridID, MapID);
            set
            {
                _position = value.Position;

                MapID = value.MapID;
                GridID = value.GridID;

                OnMove?.Invoke(this, new MoveEventArgs(LocalPosition, value));
            }
        }

        /// <inheritdoc />
        public Vector2 WorldPosition
        {
            get => Parent != null ? GetMapTransform().WorldPosition : IoCManager.Resolve<IMapManager>().GetMap(MapID).GetGrid(GridID).ConvertToWorld(_position);
            set
            {
                _position = value;
                GridID = IoCManager.Resolve<IMapManager>().GetMap(MapID).FindGridAt(_position).Index;

                OnMove?.Invoke(this, new MoveEventArgs(LocalPosition, new LocalCoordinates(_position, GridID, MapID)));
            }
        }

        public override void ExposeData(EntitySerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _parent, "parent", new EntityUid());
            serializer.DataField(ref _mapID, "map", MapId.Nullspace);
            serializer.DataField(ref _gridID, "grid", GridId.DefaultGrid);
            serializer.DataField(ref _position, "pos", Vector2.Zero);
            serializer.DataField(ref _rotation, "rot", new Angle());
        }

        /// <inheritdoc />
        public override ComponentState GetComponentState()
        {
            return new TransformComponentState(LocalPosition, Rotation, Parent?.Owner?.Uid);
        }

        /// <summary>
        /// Detaches this entity from its parent.
        /// </summary>
        public void DetachParent()
        {
            // nothing to do
            if (Parent == null)
                return;

            Parent = null;
        }

        /// <summary>
        /// Sets another entity as the parent entity.
        /// </summary>
        /// <param name="parent"></param>
        public void AttachParent(IServerTransformComponent parent)
        {
            // nothing to attach to.
            if (parent == null)
                return;

            Parent = parent;
        }

        /// <summary>
        ///     Finds the transform of the entity located on the map itself
        /// </summary>
        public IServerTransformComponent GetMapTransform()
        {
            if (Parent != null) //If we are not the final transform, query up the chain of parents
            {
                return Parent.GetMapTransform();
            }
            return this;
        }

        ITransformComponent ITransformComponent.GetMapTransform() => GetMapTransform();

        public bool IsMapTransform => Parent == null;

        /// <summary>
        ///     Does this entity contain the entity in the argument
        /// </summary>
        public bool ContainsEntity(ITransformComponent transform)
        {
            if (transform.IsMapTransform) //Is the entity on the map
            {
                return false;
            }

            if (this == transform.Parent) //Is this the direct container of the entity
            {
                return true;
            }
            else
            {
                return ContainsEntity(transform.Parent); //Recursively search up the entitys containers for this object
            }
        }
    }
}
