using System;
using OpenTK;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Maths;

namespace SS14.Server.GameObjects
{
    /// <summary>
    ///     Stores the position and orientation of the entity.
    /// </summary>
    public class TransformComponent : Component, ITransformComponent
    {
        /// <summary>
        ///     Current parent entity of this entity.
        /// </summary>
        public ITransformComponent Parent { get; private set; }

        private Vector2 _position;

        /// <summary>
        ///     Current rotation offset of the entity.
        /// </summary>
        public Angle Rotation { get; set; }

        /// <inheritdoc />
        public override string Name => "Transform";

        /// <inheritdoc />
        public override uint? NetID => NetIDs.TRANSFORM;

        /// <inheritdoc />
        public event EventHandler<VectorEventArgs> OnMove;

        /// <inheritdoc />
        public Vector2 Position
        {
            get
            {
                if(Parent != null)
                {
                    return GetMapTransform().Position; //Search up the tree for the true map position
                }
                else
                {
                    return _position;
                }
            }
            set
            {
                var oldPosition = _position;
                _position = value;

                OnMove?.Invoke(this, new VectorEventArgs(oldPosition, _position));
            }
        }

        /// <inheritdoc />
        public override ComponentState GetComponentState()
        {
            return new TransformComponentState(Position, Rotation, Parent);
        }

        /// <summary>
        ///     Finds the transform of the entity located on the map itself
        /// </summary>
        public ITransformComponent GetMapTransform()
        {
            if(Parent != null) //If we are not the final transform, query up the chain of parents
            {
                return Parent.GetMapTransform();
            }
            return this;
        }

        /// <summary>
        ///     Does this entity contain the entity in the argument
        /// </summary>
        public bool ContainsEntity(ITransformComponent entity)
        {
            if (entity.Parent != null) //Is the entity on the map
                if (this == entity.Parent) //Is this the direct container of the entity
                {
                    return true;
                }
                else
                {
                    return ContainsEntity(entity.Parent); //Recursively search up the entitys containers for this object
                }
            return false;
        }
}
