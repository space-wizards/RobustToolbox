using System;
using SS14.Shared.GameObjects;
using SS14.Shared.Serialization;

namespace SS14.Shared.ViewVariables
{
    /// <summary>
    ///     A type that allows to select a remote object.
    /// </summary>
    [Serializable, NetSerializable]
    public abstract class ViewVariablesObjectSelector
    {
    }

    /// <inheritdoc />
    /// <summary>
    ///     Specifies an entity with a certain entity UID.
    /// </summary>
    [Serializable, NetSerializable]
    public class ViewVariablesEntitySelector : ViewVariablesObjectSelector
    {
        public ViewVariablesEntitySelector(EntityUid entity)
        {
            Entity = entity;
        }

        public EntityUid Entity { get; }
    }

    /// <inheritdoc />
    /// <summary>
    ///     Specifies a component of a specified entity.
    /// </summary>
    [Serializable, NetSerializable]
    public class ViewVariablesComponentSelector : ViewVariablesEntitySelector
    {
        public ViewVariablesComponentSelector(EntityUid uid, string componentType) : base(uid)
        {
            ComponentType = componentType;
        }

        public string ComponentType { get;  }
    }

    /// <inheritdoc />
    /// <summary>
    ///     Specifies a specific property of an object currently opened in a remote VV session.
    /// </summary>
    [Serializable, NetSerializable]
    public class ViewVariablesSessionRelativeSelector : ViewVariablesObjectSelector
    {
        public ViewVariablesSessionRelativeSelector(uint sessionId, string propertyName)
        {
            SessionId = sessionId;
            PropertyName = propertyName;
        }

        public uint SessionId { get; }
        public string PropertyName { get; }
    }
}
