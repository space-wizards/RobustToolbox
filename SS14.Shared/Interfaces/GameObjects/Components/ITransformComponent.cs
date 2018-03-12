using System;
using SS14.Shared.Enums;
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
        LocalCoordinates LocalPosition { get; }

        /// <summary>
        ///     Current position offset of the entity.
        /// </summary>
        Vector2 WorldPosition { get; }

        event Action<Angle> OnRotate;

        /// <summary>
        ///     Current rotation offset of the entity.
        /// </summary>
        Angle LocalRotation { get; }

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
        ///     Reference to the transform of the container of this object if it exists, can be nested several times.
        /// </summary>
        ITransformComponent Parent { get; }

        /// <summary>
        /// Whether or not this entity is on the map, AKA it has no parent.
        /// </summary>
        bool IsMapTransform { get; }

        /// <summary>
        ///     Finds the transform located on the map or in nullspace
        /// </summary>
        ITransformComponent GetMapTransform();

        /// <summary>
        ///     Returns whether the entity of this transform contains the entity argument
        /// </summary>
        bool ContainsEntity(ITransformComponent entity);

        /// <summary>
        ///     Returns the index of the map which this object is on
        /// </summary>
        MapId MapID { get; }

        /// <summary>
        ///     Returns the index of the grid which this object is on
        /// </summary>
        GridId GridID { get; }
    }
}
