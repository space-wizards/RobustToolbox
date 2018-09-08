using System;
using SS14.Shared.Enums;
using SS14.Shared.GameObjects;
using SS14.Shared.Maths;
using SS14.Shared.Map;

namespace SS14.Shared.Interfaces.GameObjects.Components
{
    /// <summary>
    ///     Stores the position and orientation of the entity.
    /// </summary>
    public interface ITransformComponent : IComponent
    {
        /// <summary>
        ///     Current position offset of the entity.
        /// </summary>
        GridLocalCoordinates LocalPosition { get; set; }

        /// <summary>
        ///     Current position offset of the entity.
        /// </summary>
        Vector2 WorldPosition { get; set; }

        event Action<Angle> OnRotate;

        /// <summary>
        ///     Current rotation offset of the entity.
        /// </summary>
        Angle LocalRotation { get; set; }

        /// <summary>
        ///     Current world rotation of the entity.
        /// </summary>
        Angle WorldRotation { get; }

        /// <summary>
        ///     Matrix for transforming points from local to world space.
        /// </summary>
        Matrix3 WorldMatrix { get; }

        /// <summary>
        ///     Matrix for transforming points from world to local space.
        /// </summary>
        Matrix3 InvWorldMatrix { get; }

        /// <summary>
        ///     Event that gets invoked every time the position gets modified through properties such as <see cref="LocalRotation" />.
        /// </summary>
        event EventHandler<MoveEventArgs> OnMove;

        /// <summary>
        ///     Invoked whenever the parent of this transform changes.
        /// </summary>
        event Action<ParentChangedEventArgs> OnParentChanged;

        /// <summary>
        ///     Reference to the transform of the container of this object if it exists, can be nested several times.
        /// </summary>
        ITransformComponent Parent { get; }

        /// <summary>
        /// Whether or not this entity is on the map, AKA it has no parent.
        /// </summary>
        bool IsMapTransform { get; }

        /// <summary>
        /// Whether or not this entity is visible while parented to another entity.
        /// </summary>
        bool VisibleWhileParented { get; set; }

        /// <summary>
        ///     Finds the transform located on the map or in nullspace
        /// </summary>
        ITransformComponent GetMapTransform();

        /// <summary>
        ///     Returns whether the entity of this transform contains the entity argument
        /// </summary>
        bool ContainsEntity(ITransformComponent entityTransform);

        /// <summary>
        ///     Returns the index of the map which this object is on
        /// </summary>
        MapId MapID { get; }

        /// <summary>
        ///     Returns the index of the grid which this object is on
        /// </summary>
        GridId GridID { get; }

        void DetachParent();
        void AttachParent(ITransformComponent parent);
        void AttachParent(IEntity parent);
    }

    public class ParentChangedEventArgs : EventArgs
    {
        /// <summary>
        ///     The entity that we were previously parented to. Can be null if none.
        /// </summary>
        public EntityUid Old { get; }

        /// <summary>
        ///     The entity that we are now parented to. Can be null if none.
        /// </summary>
        public EntityUid New { get; }

        public ParentChangedEventArgs(EntityUid old, EntityUid @new)
        {
            Old = old;
            New = @new;
        }
    }
}
