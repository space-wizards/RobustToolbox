using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Robust.Shared.ViewVariables
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
    [Virtual]
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
    public sealed class ViewVariablesComponentSelector : ViewVariablesEntitySelector
    {
        public ViewVariablesComponentSelector(EntityUid uid, string componentType) : base(uid)
        {
            ComponentType = componentType;
        }

        public string ComponentType { get; }
    }

    /// <inheritdoc />
    /// <summary>
    ///     Specifies a specific property of an object currently opened in a remote VV session.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class ViewVariablesSessionRelativeSelector : ViewVariablesObjectSelector
    {
        public ViewVariablesSessionRelativeSelector(uint sessionId, object[] propertyIndex)
        {
            SessionId = sessionId;
            PropertyIndex = propertyIndex;
        }

        /// <summary>
        ///     The session to which this selection is relative.
        /// </summary>
        public uint SessionId { get; }

        /// <summary>
        ///     A list of objects that can be "resolved" in some way to figure out which object is being talked about,
        ///     relative to this session.
        /// </summary>
        /// <remarks>
        ///     The reason it's an array is that we might, in the future, want the ability to display tuples inline or whatever, and then perhaps not open a new remote session.
        ///     Using an array would allow you to go "property name -> index -> index -> index" for as long as that madman is nesting tuples.
        ///     This is not used yet.
        ///     The reason it's <see cref="object"/> is to avoid confusion about which trait gets to handle it.
        ///     <see cref="ViewVariablesMemberSelector"/> and <see cref="ViewVariablesEnumerableIndexSelector"/> are used here.
        /// </remarks>
        public object[] PropertyIndex { get; }
    }

    [Serializable, NetSerializable]
    public sealed class ViewVariablesIoCSelector : ViewVariablesObjectSelector
    {
        public ViewVariablesIoCSelector(string typeName)
        {
            TypeName = typeName;
        }

        public string TypeName { get; }
    }

    [Serializable, NetSerializable]
    public sealed class ViewVariablesEntitySystemSelector : ViewVariablesObjectSelector
    {
        public ViewVariablesEntitySystemSelector(string typeName)
        {
            TypeName = typeName;
        }

        public string TypeName { get; }
    }
}
