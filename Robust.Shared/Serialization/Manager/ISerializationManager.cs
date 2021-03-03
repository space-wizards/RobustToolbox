using System;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;

namespace Robust.Shared.Serialization.Manager
{
    public interface ISerializationManager
    {
        IComponentFactory ComponentFactory { get; }

        IEntityManager EntityManager { get; }

        #region Serialization

        /// <summary>
        ///     Initializes the serialization manager.
        /// </summary>
        void Initialize();

        /// <summary>
        ///     Checks if a type has a data definition defined for it.
        /// </summary>
        /// <param name="type">The type to check for.</param>
        /// <returns>True if it does, false otherwise.</returns>
        bool HasDataDefinition(Type type);

        /// <summary>
        ///     Validates that a node has all the properties required by a certain type with its serializer.
        /// </summary>
        /// <param name="type">The type to check for.</param>
        /// <param name="node">The node to check.</param>
        /// <param name="context">The context to use, if any.</param>
        /// <returns>
        ///     A node with whether or not <see cref="node"/> is valid and which of its fields
        ///     are invalid, if any.
        /// </returns>
        ValidatedNode ValidateNode(Type type, DataNode node, ISerializationContext? context = null);

        /// <summary>
        ///     Validates that a node has all the properties required by a certain type with its serializer.
        /// </summary>
        /// <param name="node">The node to check.</param>
        /// <param name="context">The context to use, if any.</param>
        /// <returns>
        ///     A node with whether or not <see cref="node"/> is valid and which of its fields
        ///     are invalid, if any.
        /// </returns>
        ValidatedNode ValidateNode<T>(DataNode node, ISerializationContext? context = null);

        /// <summary>
        ///     Creates a deserialization result from a generic type and its fields,
        ///     populating the object.
        /// </summary>
        /// <param name="fields">The fields to use for deserialization.</param>
        /// <param name="skipHook">Whether or not to skip running <see cref="ISerializationHooks"/></param>
        /// <typeparam name="T">The type to populate.</typeparam>
        /// <returns>A result with the populated type.</returns>
        DeserializationResult CreateDataDefinition<T>(DeserializedFieldEntry[] fields, bool skipHook = false) where T : notnull, new();

        /// <summary>
        ///     Creates a deserialization result from a generic type and its definition,
        ///     populating the object.
        /// </summary>
        /// <param name="obj">The object to populate.</param>
        /// <param name="definition">The data to use for deserialization.</param>
        /// <param name="skipHook">Whether or not to skip running <see cref="ISerializationHooks"/></param>
        /// <typeparam name="T">The type of <see cref="obj"/> to populate.</typeparam>
        /// <returns>A result with the populated object.</returns>
        DeserializationResult PopulateDataDefinition<T>(T obj, DeserializedDefinition<T> definition, bool skipHook = false) where T : notnull, new();

        /// <summary>
        ///     Creates a deserialization result from an object and its definition,
        ///     populating the object.
        /// </summary>
        /// <param name="obj">The object to populate.</param>
        /// <param name="definition">The data to use for deserialization.</param>
        /// <param name="skipHook">Whether or not to skip running <see cref="ISerializationHooks"/></param>
        /// <returns>A result with the populated object.</returns>
        DeserializationResult PopulateDataDefinition(object obj, IDeserializedDefinition definition, bool skipHook = false);

        /// <summary>
        ///     Deserializes a node into an object, populating it.
        /// </summary>
        /// <param name="type">The type of object to populate.</param>
        /// <param name="node">The node to deserialize.</param>
        /// <param name="context">The context to use, if any.</param>
        /// <param name="skipHook">Whether or not to skip running <see cref="ISerializationHooks"/></param>
        /// <returns>A result with the deserialized object.</returns>
        DeserializationResult Read(Type type, DataNode node, ISerializationContext? context = null, bool skipHook = false);

        /// <summary>
        ///     Deserializes a node into an object, populating it.
        /// </summary>
        /// <param name="type">The type of object to deserialize into.</param>
        /// <param name="node">The node to deserialize.</param>
        /// <param name="context">The context to use, if any.</param>
        /// <param name="skipHook">Whether or not to skip running <see cref="ISerializationHooks"/></param>
        /// <returns>The deserialized object or null.</returns>
        public object? ReadValue(Type type, DataNode node, ISerializationContext? context = null, bool skipHook = false);

        /// <summary>
        ///     Deserializes a node into an object of the given <see cref="type"/>,
        ///     directly casting it to the given generic type <see cref="T"/>.
        /// </summary>
        /// <param name="type">The type of object to deserialize into.</param>
        /// <param name="node">The node to deserialize.</param>
        /// <param name="context">The context to use, if any.</param>
        /// <param name="skipHook">Whether or not to skip running <see cref="ISerializationHooks"/></param>
        /// <typeparam name="T">The generic type to cast the resulting object to.</typeparam>
        /// <returns>The deserialized casted object, or null.</returns>
        T? ReadValueCast<T>(Type type, DataNode node, ISerializationContext? context = null, bool skipHook = false);

        /// <summary>
        ///     Deserializes a node into a populated object of the given generic type <see cref="T"/>
        /// </summary>
        /// <param name="node">The node to deserialize.</param>
        /// <param name="context">The context to use, if any.</param>
        /// <param name="skipHook">Whether or not to skip running <see cref="ISerializationHooks"/></param>
        /// <typeparam name="T">The type of object to create and populate.</typeparam>
        /// <returns>The deserialized object, or null.</returns>
        T? ReadValue<T>(DataNode node, ISerializationContext? context = null, bool skipHook = false);

        /// <summary>
        ///     Serializes a value into a node.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <param name="alwaysWrite">
        ///     Whether or not to always write the given values into the resulting node,
        ///     even if they are the default.
        /// </param>
        /// <param name="context">The context to use, if any.</param>
        /// <typeparam name="T">The type to serialize.</typeparam>
        /// <returns>A serialized datanode created from the given <see cref="value"/>.</returns>
        DataNode WriteValue<T>(T value, bool alwaysWrite = false, ISerializationContext? context = null)
            where T : notnull;

        /// <summary>
        ///     Serializes a value into a node.
        /// </summary>
        /// <param name="type">The type of the <see cref="value"/> to serialize as.</param>
        /// <param name="value">The value to serialize.</param>
        /// <param name="alwaysWrite">
        ///     Whether or not to always write the given values into the resulting node,
        ///     even if they are the default.
        /// </param>
        /// <param name="context">The context to use, if any.</param>
        /// <returns>
        ///     A serialized datanode created from the given <see cref="value"/>
        ///     of type <see cref="type"/>.
        /// </returns>
        DataNode WriteValue(Type type, object value, bool alwaysWrite = false, ISerializationContext? context = null);

        /// <summary>
        ///     Copies the values of one object into another.
        ///     This does not guarantee that the object passed as <see cref="target"/>
        ///     is actually mutated.
        /// </summary>
        /// <param name="source">The object to copy values from.</param>
        /// <param name="target">The object to copy values into.</param>
        /// <param name="context">The context to use, if any.</param>
        /// <param name="skipHook">Whether or not to skip running <see cref="ISerializationHooks"/></param>
        /// <returns>
        ///     The object with the copied values.
        ///     This object is not necessarily the same instance as the one passed
        ///     as <see cref="target"/>.
        /// </returns>
        [MustUseReturnValue]
        object? Copy(object? source, object? target, ISerializationContext? context = null, bool skipHook = false);

        /// <summary>
        ///     Copies the values of one object into another.
        ///     This does not guarantee that the object passed as <see cref="target"/>
        ///     is actually mutated.
        /// </summary>
        /// <param name="source">The object to copy values from.</param>
        /// <param name="target">The object to copy values into.</param>
        /// <param name="context">The context to use, if any.</param>
        /// <param name="skipHook">Whether or not to skip running <see cref="ISerializationHooks"/></param>
        /// <typeparam name="T">The type of the objects to copy from and into.</typeparam>
        /// <returns>
        ///     The object with the copied values.
        ///     This object is not necessarily the same instance as the one passed
        ///     as <see cref="target"/>.
        /// </returns>
        [MustUseReturnValue]
        T? Copy<T>(T? source, T? target, ISerializationContext? context = null, bool skipHook = false);

        /// <summary>
        ///     Creates a copy of the given object.
        /// </summary>
        /// <param name="source">The object to copy.</param>
        /// <param name="context">The context to use, if any.</param>
        /// <param name="skipHook">Whether or not to skip running <see cref="ISerializationHooks"/></param>
        /// <returns>A copy of the given object.</returns>
        object? CreateCopy(object? source, ISerializationContext? context = null, bool skipHook = false);

        /// <summary>
        ///     Creates a copy of the given object.
        /// </summary>
        /// <param name="source">The object to copy.</param>
        /// <param name="context">The context to use, if any.</param>
        /// <param name="skipHook">Whether or not to skip running <see cref="ISerializationHooks"/></param>
        /// <typeparam name="T">The type of the object to copy.</typeparam>
        /// <returns>A copy of the given object.</returns>
        T? CreateCopy<T>(T? source, ISerializationContext? context = null, bool skipHook = false);

        #endregion

        #region Flags And Constants

        /// <summary>
        ///     Deserializes a node into an enum flag value of the given type.
        /// </summary>
        /// <param name="tagType">The type of enum to deserialize into.</param>
        /// <param name="node">The node to deserialize.</param>
        /// <returns>The deserialized enum flags.</returns>
        int ReadFlag(Type tagType, DataNode node);

        /// <summary>
        ///     Validates that a node can be deserialized into the specified flagtype.
        /// </summary>
        /// <param name="tagType">The tagtype for used to retrieve the flagtype.</param>
        /// <param name="node">The node to check.</param>
        /// <returns>
        ///     A node with whether or not <see cref="node"/> is valid and which of its fields
        ///     are invalid, if any.
        /// </returns>
        ValidatedNode ValidateFlag(Type tagType, DataNode node);

        /// <summary>
        ///     Deserializes a node into an enum value of the given type.
        /// </summary>
        /// <param name="tagType">The type of enum to deserialize into.</param>
        /// <param name="node">The node to deserialize.</param>
        /// <returns>The deserialized enum.</returns>
        int ReadConstant(Type tagType, DataNode node);

        /// <summary>
        ///     Validates that a node can be deserialized into the specified constanttype.
        /// </summary>
        /// <param name="tagType">The tagtype for used to retrieve the constanttype.</param>
        /// <param name="node">The node to check.</param>
        /// <returns>
        ///     A node with whether or not <see cref="node"/> is valid and which of its fields
        ///     are invalid, if any.
        /// </returns>
        ValidatedNode ValidateConstant(Type tagType, DataNode node);

        /// <summary>
        ///     Serializes an enum flag into a node.
        /// </summary>
        /// <param name="tagType">The type of enum to serialize.</param>
        /// <param name="flag">The enum flags value to serialize.</param>
        /// <returns>The serialized node.</returns>
        DataNode WriteFlag(Type tagType, int flag);

        /// <summary>
        ///     Serializes an enum into a node.
        /// </summary>
        /// <param name="tagType">The type of enum to serialize.</param>
        /// <param name="constant">The enum value to serialize.</param>
        /// <returns>The serialized node.</returns>
        DataNode WriteConstant(Type tagType, int constant);

        #endregion
    }
}
