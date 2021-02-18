using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.Animations;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    ///     Stores the position and orientation of the entity.
    /// </summary>
    [PublicAPI]
    public interface ITransformComponent : IComponent
    {
        /// <summary>
        /// Disables or enables to ability to locally rotate the entity. When set it removes any local rotation.
        /// </summary>
        bool NoLocalRotation { get; set; }

        /// <summary>
        ///     Local offset of this entity relative to its parent
        ///     (<see cref="Parent"/> if it's not null, to <see cref="GridID"/> otherwise).
        /// </summary>
        [Animatable]
        Vector2 LocalPosition { get; set; }

        /// <summary>
        ///     Position offset of this entity relative to its parent.
        /// </summary>
        EntityCoordinates Coordinates { get; set; }

        /// <summary>
        ///     Current position offset of the entity relative to the world.
        ///     Can de-parent from its parent if the parent is a grid.
        /// </summary>
        [Animatable]
        Vector2 WorldPosition { get; set; }

        /// <summary>
        ///     Current position offset of the entity relative to the world.
        ///     This is effectively a more complete version of <see cref="WorldPosition"/>
        /// </summary>
        MapCoordinates MapPosition { get; }

        /// <summary>
        ///     Current rotation offset of the entity.
        /// </summary>
        [Animatable]
        Angle LocalRotation { get; set; }

        /// <summary>
        ///     Current world rotation of the entity.
        /// </summary>
        Angle WorldRotation { get; set; }

        /// <summary>
        ///     Matrix for transforming points from local to world space.
        /// </summary>
        Matrix3 WorldMatrix { get; }

        /// <summary>
        ///     Matrix for transforming points from world to local space.
        /// </summary>
        Matrix3 InvWorldMatrix { get; }

        /// <summary>
        ///     Reference to the transform of the container of this object if it exists, can be nested several times.
        /// </summary>
        ITransformComponent? Parent { get; }

        /// <summary>
        /// The UID of the parent entity that this entity is attached to.
        /// </summary>
        public EntityUid ParentUid { get; set; }

        /// <summary>
        /// Whether or not this entity is on the map, AKA it has no parent.
        /// </summary>
        bool IsMapTransform { get; }

        /// <summary>
        ///
        /// </summary>
        Vector2? LerpDestination { get; }

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

        /// <summary>
        ///     Whether external system updates should run or not (e.g. EntityTree, Matrices, PhysicsTree).
        ///     These should be manually run later.
        /// </summary>
        bool DeferUpdates { get; set; }

        void AttachToGridOrMap();
        void AttachParent(ITransformComponent parent);
        void AttachParent(IEntity parent);

        /// <summary>
        ///     Run the updates marked as deferred (UpdateEntityTree and movement events).
        ///     Don't call this unless you REALLY need to.
        /// </summary>
        /// <remarks>
        ///    Physics optimisation so these aren't spammed during physics updates.
        /// </remarks>
        void RunPhysicsDeferred();

        IEnumerable<ITransformComponent> Children { get; }
        int ChildCount { get; }
        IEnumerable<EntityUid> ChildEntityUids { get; }
        Matrix3 GetLocalMatrix();
        Matrix3 GetLocalMatrixInv();
    }
}
