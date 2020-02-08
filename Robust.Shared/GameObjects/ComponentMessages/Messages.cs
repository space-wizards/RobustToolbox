using System;
using Robust.Shared.Console;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects
{
    [Serializable, NetSerializable]
    public class BumpedEntMsg : ComponentMessage
    {
        public IEntity Entity { get; }

        public BumpedEntMsg(IEntity entity)
        {
            Entity = entity;
        }
    }

    public class RelayMovementEntityMessage : ComponentMessage
    {
        public IEntity Entity { get; set; }

        public RelayMovementEntityMessage(IEntity entity)
        {
            Entity = entity;
        }
    }

    /// <summary>
    ///     The entity transform parent has been changed.
    /// </summary>
    public class ParentChangedMessage : ComponentMessage
    {
        /// <summary>
        ///     The new parent of the transform.
        /// </summary>
        public IEntity NewParent { get; }

        /// <summary>
        ///     The old parent of the transform.
        /// </summary>
        public IEntity OldParent { get; }

        /// <summary>
        ///     Constructs a new instance of <see cref="ParentChangedMessage"/>.
        /// </summary>
        /// <param name="newParent">The new parent of the transform.</param>
        /// <param name="oldParent">The old parent of the transform.</param>
        public ParentChangedMessage(IEntity newParent, IEntity oldParent)
        {
            NewParent = newParent;
            OldParent = oldParent;
        }
    }

    public class MoveMessage : ComponentMessage
    {
        public MoveMessage(GridCoordinates oldPos, GridCoordinates newPos)
        {
            OldPosition = oldPos;
            NewPosition = newPos;
        }

        public GridCoordinates OldPosition { get; }
        public GridCoordinates NewPosition { get; }
    }
}
