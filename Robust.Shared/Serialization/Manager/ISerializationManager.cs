using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.Manager
{
    public interface ISerializationManager
    {
        public delegate T InstantiationDelegate<out T>();

        /// <summary>
        ///     Initializes the serialization manager.
        /// </summary>
        void Initialize();

        /// <summary>
        ///     Shuts down the serialization manager.
        /// </summary>
        void Shutdown();

        IReflectionManager ReflectionManager { get; }

        #region Validation

        /// <summary>
        ///     Validates that a node has all the properties required by a certain type.
        /// </summary>
        /// <param name="type">The type to check for.</param>
        /// <param name="node">The node to check.</param>
        /// <param name="context">The context to use, if any.</param>
        /// <returns>
        ///     A node with whether or not <see cref="node"/> is valid and which of its fields
        ///     are invalid, if any.
        /// </returns>
        [PreferGenericVariant]
        ValidationNode ValidateNode(Type type, DataNode node, ISerializationContext? context = null);

        /// <summary>
        ///     Validates that a node has all the properties required by a certain type.
        /// </summary>
        /// <param name="node">The node to check.</param>
        /// <param name="context">The context to use, if any.</param>
        /// <typeparam name="T">The type this node should be able to be read into.</typeparam>
        /// <returns>
        ///     A node with whether or not <see cref="node"/> is valid and which of its fields
        ///     are invalid, if any.
        /// </returns>
        ValidationNode ValidateNode<T>(DataNode node, ISerializationContext? context = null);

        /// <summary>
        ///     Validates that a node has all the properties required by a certain type using a specified <see cref="ITypeValidator{TType,TNode}"/> instance.
        /// </summary>
        /// <param name="typeValidator">The <see cref="ITypeValidator{TType,TNode}"/> instance to use.</param>
        /// <param name="node">The node to check.</param>
        /// <param name="context">The context to use, if any.</param>
        /// <typeparam name="T">The type this node should be able to be read into.</typeparam>
        /// <typeparam name="TNode">The node type</typeparam>
        /// <returns></returns>
        ValidationNode ValidateNode<T, TNode>(ITypeValidator<T, TNode> typeValidator, TNode node,
            ISerializationContext? context = null) where TNode : DataNode;

        /// <summary>
        ///     Validates that a node has all the properties required by a certain type using a specified <see cref="ITypeValidator{TType,TNode}"/> type.
        /// </summary>
        /// <param name="node">The node to check.</param>
        /// <param name="context">The context to use, if any.</param>
        /// <typeparam name="T">The type this node should be able to be read into.</typeparam>
        /// <typeparam name="TNode">The node type</typeparam>
        /// <typeparam name="TValidator">The type of the <see cref="ITypeValidator{TType,TNode}"/>.</typeparam>
        /// <returns></returns>
        ValidationNode ValidateNode<T, TNode, TValidator>(TNode node,
            ISerializationContext? context = null) where TNode : DataNode where TValidator : ITypeValidator<T, TNode>;

        #endregion

        #region Read

        /// <summary>
        ///     Deserializes a node into an object, populating it.
        /// </summary>
        /// <param name="type">The type of object to deserialize into.</param>
        /// <param name="node">The node to deserialize.</param>
        /// <param name="context">The context to use, if any.</param>
        /// <param name="skipHook">Whether or not to skip running <see cref="ISerializationHooks"/></param>
        /// <param name="notNullableOverride">Set true if a reference Type should not allow null. Not necessary for value types.</param>
        /// <returns>The deserialized object or null.</returns>
        public object? Read(Type type, DataNode node, ISerializationContext? context = null, bool skipHook = false, bool notNullableOverride = false);
        public object? Read(
            Type type,
            DataNode node,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            bool notNullableOverride = false);

        /// <summary>
        ///     Deserializes a node into a populated object of the given generic type <see cref="T"/>
        /// </summary>
        /// <param name="node">The node to deserialize.</param>
        /// <param name="context">The context to use, if any.</param>
        /// <param name="skipHook">Whether or not to skip running <see cref="ISerializationHooks"/></param>
        /// <param name="instanceProvider">The valueProvider which can provide a value to read into. If none is supplied, a new object will be created.</param>
        /// <param name="notNullableOverride">Set true if a reference Type should not allow null. Not necessary for value types.</param>
        /// <typeparam name="T">The type of object to create and populate.</typeparam>
        /// <returns>The deserialized object, or null.</returns>
        T Read<T>(DataNode node, ISerializationContext? context = null, bool skipHook = false, InstantiationDelegate<T>? instanceProvider = null, [NotNullableFlag(nameof(T))] bool notNullableOverride = false);
        T Read<T>(
            DataNode node,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            InstantiationDelegate<T>? instanceProvider = null,
            [NotNullableFlag(nameof(T))] bool notNullableOverride = false);


        /// <summary>
        ///     Deserializes a node into a populated object of the given generic type <see cref="T"/> using the provided <see cref="ITypeReader{TType,TNode}"/> instance.
        /// </summary>
        /// <param name="reader">The <see cref="ITypeReader{TType,TNode}"/> instance to use.</param>
        /// <param name="node">The node to deserialize.</param>
        /// <param name="context">The context to use, if any.</param>
        /// <param name="skipHook">Whether or not to skip running <see cref="ISerializationHooks"/></param>
        /// <param name="instanceProvider">The valueProvider which can provide a value to read into. If none is supplied, a new object will be created.</param>
        /// <param name="notNullableOverride">Set true if a reference Type should not allow null. Not necessary for value types.</param>
        /// <typeparam name="T">The type of object to create and populate.</typeparam>
        /// <typeparam name="TNode">The node type that will be returned by the <see cref="ITypeReader{TType,TNode}"/></typeparam>
        /// <returns>The deserialized object, or null.</returns>
        T Read<T, TNode>(ITypeReader<T, TNode> reader, TNode node, ISerializationContext? context = null,
            bool skipHook = false, InstantiationDelegate<T>? instanceProvider = null, [NotNullableFlag(nameof(T))] bool notNullableOverride = false) where TNode : DataNode;

        T Read<T, TNode>(
            ITypeReader<T, TNode> reader,
            TNode node,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            InstantiationDelegate<T>? instanceProvider = null,
            bool notNullableOverride = false)
            where TNode : DataNode;

        /// <summary>
        ///     Deserializes a node into a populated object of the given generic type <see cref="T"/> using the provided <see cref="ITypeReader{TType,TNode}"/> type.
        /// </summary>
        /// <param name="node">The node to deserialize.</param>
        /// <param name="context">The context to use, if any.</param>
        /// <param name="skipHook">Whether or not to skip running <see cref="ISerializationHooks"/></param>
        /// <param name="instanceProvider">The valueProvider which can provide a value to read into. If none is supplied, a new object will be created.</param>
        /// <param name="notNullableOverride">Set true if a reference Type should not allow null. Not necessary for value types.</param>
        /// <typeparam name="T">The type of object to create and populate.</typeparam>
        /// <typeparam name="TNode">The node type that will be returned by the <see cref="ITypeReader{TType,TNode}"/></typeparam>
        /// <typeparam name="TReader">The type of the <see cref="ITypeReader{TType,TNode}"/>.</typeparam>
        /// <returns>The deserialized object, or null.</returns>
        T Read<T, TNode, TReader>(TNode node, ISerializationContext? context = null,
            bool skipHook = false, InstantiationDelegate<T>? instanceProvider = null, [NotNullableFlag(nameof(T))] bool notNullableOverride = false) where TNode : DataNode where TReader : ITypeReader<T, TNode>;

        T Read<T, TNode, TReader>(
            TNode node,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            InstantiationDelegate<T>? instanceProvider = null,
            bool notNullableOverride = false)
            where TNode : DataNode
            where TReader : ITypeReader<T, TNode>;

        #endregion

        #region Write

        /// <summary>
        ///     Serializes a value into a node.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <param name="alwaysWrite">
        ///     Whether or not to always write the given values into the resulting node,
        ///     even if they are the default.
        /// </param>
        /// <param name="context">The context to use, if any.</param>
        /// <param name="notNullableOverride">Set true if a reference Type should not allow null. Not necessary for value types.</param>
        /// <typeparam name="T">The type to serialize.</typeparam>
        /// <returns>A <see cref="DataNode"/> created from the given <see cref="value"/>.</returns>
        DataNode WriteValue<T>(T value, bool alwaysWrite = false, ISerializationContext? context = null, [NotNullableFlag(nameof(T))] bool notNullableOverride = false);

        /// <summary>
        ///     Serializes a value into a node using a <see cref="ITypeWriter{TType}"/> instance.
        /// </summary>
        /// <param name="writer">The <see cref="ITypeWriter{TType}"/> to use for serializing the value.</param>
        /// <param name="value">The value to serialize.</param>
        /// <param name="alwaysWrite">
        ///     Whether or not to always write the given values into the resulting node,
        ///     even if they are the default.
        /// </param>
        /// <param name="context">The context to use, if any.</param>
        /// <param name="notNullableOverride">Set true if a reference Type should not allow null. Not necessary for value types.</param>
        /// <typeparam name="T">The type to serialize.</typeparam>
        /// <returns>A serialized datanode created from the given <see cref="value"/> by using the typewriter.</returns>
        DataNode WriteValue<T>(ITypeWriter<T> writer, T value, bool alwaysWrite = false, ISerializationContext? context = null, [NotNullableFlag(nameof(T))] bool notNullableOverride = false);

        /// <summary>
        ///     Serializes a value into a node using a <see cref="ITypeWriter{TType}"/> type.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <param name="alwaysWrite">
        ///     Whether or not to always write the given values into the resulting node,
        ///     even if they are the default.
        /// </param>
        /// <param name="context">The context to use, if any.</param>
        /// <param name="notNullableOverride">Set true if a reference Type should not allow null. Not necessary for value types.</param>
        /// <typeparam name="T">The type to serialize.</typeparam>
        /// <typeparam name="TWriter">The type of the <see cref="ITypeWriter{TType}"/>.</typeparam>
        /// <returns>A serialized datanode created from the given <see cref="value"/> by using the typewriter.</returns>
        DataNode WriteValue<T, TWriter>(T value, bool alwaysWrite = false, ISerializationContext? context = null, [NotNullableFlag(nameof(T))] bool notNullableOverride = false) where TWriter : ITypeWriter<T>;

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
        /// <param name="notNullableOverride">Set true if a reference Type should not allow null. Not necessary for value types.</param>
        /// <returns>
        ///     A serialized datanode created from the given <see cref="value"/>
        ///     of type <see cref="type"/>.
        /// </returns>
        [PreferGenericVariant]
        DataNode WriteValue(Type type, object? value, bool alwaysWrite = false, ISerializationContext? context = null, bool notNullableOverride = false);

        #endregion

        #region Copy

        /// <summary>
        ///     Copies the values of one object into another.
        ///     This does not guarantee that the object passed as <see cref="target"/>
        ///     is actually mutated.
        /// </summary>
        /// <param name="source">The object to copy values from.</param>
        /// <param name="target">The object to copy values into.</param>
        /// <param name="context">The context to use, if any.</param>
        /// <param name="skipHook">Whether or not to skip running <see cref="ISerializationHooks"/></param>
        /// <param name="notNullableOverride">Set true if a reference Type should not allow null. Not necessary for value types.</param>
        void CopyTo(object source, ref object? target, ISerializationContext? context = null, bool skipHook = false, bool notNullableOverride = false);
        void CopyTo(
            object source,
            ref object? target,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            bool notNullableOverride = false);

        /// <summary>
        ///     Copies the values of one object into another.
        ///     This does not guarantee that the object passed as <see cref="target"/>
        ///     is actually mutated.
        /// </summary>
        /// <param name="source">The object to copy values from.</param>
        /// <param name="target">The object to copy values into.</param>
        /// <param name="context">The context to use, if any.</param>
        /// <param name="skipHook">Whether or not to skip running <see cref="ISerializationHooks"/></param>
        /// <param name="notNullableOverride">Set true if a reference Type should not allow null. Not necessary for value types.</param>
        /// <typeparam name="T">The type of the objects to copy from and into.</typeparam>
        void CopyTo<T>(T source, ref T target, ISerializationContext? context = null, bool skipHook = false, [NotNullableFlag(nameof(T))]  bool notNullableOverride = false);
        void CopyTo<T>(
            T source,
            ref T target,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            [NotNullableFlag(nameof(T))] bool notNullableOverride = false);

        /// <summary>
        ///     Copies the values of one object into another using a specified <see cref="ITypeCopier{TType}"/> instance.
        ///     This does not guarantee that the object passed as <see cref="target"/>
        ///     is actually mutated.
        /// </summary>
        /// <param name="copier">the <see cref="ITypeCopier{TType}"/> instance to use</param>
        /// <param name="source">The object to copy values from.</param>
        /// <param name="target">The object to copy values into.</param>
        /// <param name="context">The context to use, if any.</param>
        /// <param name="skipHook">Whether or not to skip running <see cref="ISerializationHooks"/></param>
        /// <param name="notNullableOverride">Set true if a reference Type should not allow null. Not necessary for value types.</param>
        /// <typeparam name="T">The type of the objects to copy from and into.</typeparam>
        void CopyTo<T>(ITypeCopier<T> copier, T source, ref T target, ISerializationContext? context = null, bool skipHook = false, [NotNullableFlag(nameof(T))]  bool notNullableOverride = false);
        void CopyTo<T>(
            ITypeCopier<T> copier,
            T source,
            ref T target,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            [NotNullableFlag(nameof(T))] bool notNullableOverride = false);

        /// <summary>
        ///     Copies the values of one object into another using a specified <see cref="ITypeCopier{TType}"/> type.
        ///     This does not guarantee that the object passed as <see cref="target"/>
        ///     is actually mutated.
        /// </summary>
        /// <param name="source">The object to copy values from.</param>
        /// <param name="target">The object to copy values into.</param>
        /// <param name="context">The context to use, if any.</param>
        /// <param name="skipHook">Whether or not to skip running <see cref="ISerializationHooks"/></param>
        /// <param name="notNullableOverride">Set true if a reference Type should not allow null. Not necessary for value types.</param>
        /// <typeparam name="T">The type of the objects to copy from and into.</typeparam>
        /// <typeparam name="TCopier">The type of the <see cref="ITypeCopier{TType}"/>.</typeparam>
        void CopyTo<T, TCopier>(T source, ref T target, ISerializationContext? context = null, bool skipHook = false, [NotNullableFlag(nameof(T))]  bool notNullableOverride = false) where TCopier : ITypeCopier<T>;
        void CopyTo<T, TCopier>(
            T source,
            ref T target,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            [NotNullableFlag(nameof(T))] bool notNullableOverride = false)
            where TCopier : ITypeCopier<T>;

        /// <summary>
        ///     Creates a copy of the given object.
        /// </summary>
        /// <param name="source">The object to copy.</param>
        /// <param name="context">The context to use, if any.</param>
        /// <param name="skipHook">Whether or not to skip running <see cref="ISerializationHooks"/></param>
        /// <param name="notNullableOverride">Set true if a reference Type should not allow null. Not necessary for value types.</param>
        /// <returns>A copy of the given object.</returns>
        [MustUseReturnValue]
        object? CreateCopy(object? source, ISerializationContext? context = null, bool skipHook = false, bool notNullableOverride = false);

        [MustUseReturnValue]
        object? CreateCopy(
            object? source,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            bool notNullableOverride = false);

        /// <summary>
        ///     Creates a copy of the given object.
        /// </summary>
        /// <param name="source">The object to copy.</param>
        /// <param name="context">The context to use, if any.</param>
        /// <param name="skipHook">Whether or not to skip running <see cref="ISerializationHooks"/></param>
        /// <param name="notNullableOverride">Set true if a reference Type should not allow null. Not necessary for value types.</param>
        /// <typeparam name="T">The type of the object to copy.</typeparam>
        /// <returns>A copy of the given object.</returns>
        [MustUseReturnValue]
        T CreateCopy<T>(T source, ISerializationContext? context = null, bool skipHook = false, [NotNullableFlag(nameof(T))] bool notNullableOverride = false);

        [MustUseReturnValue]
        T CreateCopy<T>(
            T source,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            bool notNullableOverride = false);

        /// <summary>
        ///     Creates a copy of the given object using a specified <see cref="ITypeCopyCreator{TType}"/> instance.
        /// </summary>
        /// <param name="copyCreator">The <see cref="ITypeCopyCreator{TType}"/> instance.</param>
        /// <param name="source">The object to copy.</param>
        /// <param name="context">The context to use, if any.</param>
        /// <param name="skipHook">Whether or not to skip running <see cref="ISerializationHooks"/></param>
        /// <param name="notNullableOverride">Set true if a reference Type should not allow null. Not necessary for value types.</param>
        /// <typeparam name="T">The type of the object to copy.</typeparam>
        /// <returns>A copy of the given object.</returns>
        [MustUseReturnValue]
        T CreateCopy<T>(ITypeCopyCreator<T> copyCreator, T source, ISerializationContext? context = null, bool skipHook = false, [NotNullableFlag(nameof(T))] bool notNullableOverride = false);

        [MustUseReturnValue]
        T CreateCopy<T>(
            ITypeCopyCreator<T> copyCreator,
            T source,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            bool notNullableOverride = false);

        /// <summary>
        ///     Creates a copy of the given object using a specified <see cref="ITypeCopyCreator{TType}"/> type.
        /// </summary>
        /// <param name="source">The object to copy.</param>
        /// <param name="context">The context to use, if any.</param>
        /// <param name="skipHook">Whether or not to skip running <see cref="ISerializationHooks"/></param>
        /// <param name="notNullableOverride">Set true if a reference Type should not allow null. Not necessary for value types.</param>
        /// <typeparam name="T">The type of the object to copy.</typeparam>
        /// <typeparam name="TCopyCreator">The type of the <see cref="ITypeCopier{TType}"/> to use.</typeparam>
        /// <returns>A copy of the given object.</returns>
        [MustUseReturnValue]
        T CreateCopy<T, TCopyCreator>(T source, ISerializationContext? context = null, bool skipHook = false, [NotNullableFlag(nameof(T))] bool notNullableOverride = false) where TCopyCreator : ITypeCopyCreator<T>;

        [MustUseReturnValue]
        T CreateCopy<T, TCopyCreator>(
            T source,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            bool notNullableOverride = false)
            where TCopyCreator : ITypeCopyCreator<T>;

        [Obsolete]
        bool TryGetCopierOrCreator<TType>(
            out ITypeCopier<TType>? copier,
            out ITypeCopyCreator<TType>? copyCreator,
            ISerializationContext? context = null);

        [Obsolete]
        bool TryCustomCopy<T>(
            T source,
            ref T target,
            SerializationHookContext hookCtx,
            bool hasHooks,
            ISerializationContext? context = null);

        #endregion

        #region Flags And Constants

        Type GetFlagTypeFromTag(Type tagType);

        int GetFlagHighestBit(Type tagType);

        Type GetConstantTypeFromTag(Type tagType);

        #endregion

        #region Composition

        DataNode PushComposition(Type type, DataNode[] parents, DataNode child, ISerializationContext? context = null);
        DataNode PushComposition(Type type, DataNode parent, DataNode child, ISerializationContext? context = null);

        public TNode PushComposition<TType, TNode>(TNode[] parents, TNode child, ISerializationContext? context = null) where TNode : DataNode
        {
            // ReSharper disable once CoVariantArrayConversion
            return (TNode)PushComposition(typeof(TType), parents, child, context);
        }

        public TNode PushComposition<TType, TNode>(TNode parent, TNode child, ISerializationContext? context = null)
            where TNode : DataNode
        {
            return (TNode) PushComposition(typeof(TType), parent, child, context);
        }

        TNode PushInheritance<TType, TNode>(ITypeInheritanceHandler<TType, TNode> inheritanceHandler, TNode parent, TNode child,
            ISerializationContext? context = null) where TNode : DataNode;

        TNode PushInheritance<TType, TNode, TInheritanceHandler>(TNode parent, TNode child,
            ISerializationContext? context = null) where TNode : DataNode
            where TInheritanceHandler : ITypeInheritanceHandler<TType, TNode>;

        public TNode PushCompositionWithGenericNode<TNode>(Type type, TNode[] parents, TNode child, ISerializationContext? context = null) where TNode : DataNode
        {
            // ReSharper disable once CoVariantArrayConversion
            return (TNode) PushComposition(type, parents, child, context);
        }

        public TNode PushCompositionWithGenericNode<TNode>(Type type, TNode parent, TNode child, ISerializationContext? context = null)
            where TNode : DataNode
        {
            return (TNode) PushComposition(type, parent, child, context);
        }

        /// <summary>
        /// Simple <see cref="MappingDataNode"/> inheritance pusher clones data and overrides a parent's values with
        /// the child's.
        /// </summary>
        MappingDataNode CombineMappings(MappingDataNode child, MappingDataNode parent);

        #endregion

        public bool TryGetVariableType(Type type, string variableName, [NotNullWhen(true)] out Type? variableType);
    }
}
