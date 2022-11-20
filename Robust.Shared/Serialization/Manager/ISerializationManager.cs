using System;
using JetBrains.Annotations;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Markdown;
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
        ///     Validates that a node has all the properties required by a certain type with its serializer.
        /// </summary>
        /// <param name="type">The type to check for.</param>
        /// <param name="node">The node to check.</param>
        /// <param name="context">The context to use, if any.</param>
        /// <returns>
        ///     A node with whether or not <see cref="node"/> is valid and which of its fields
        ///     are invalid, if any.
        /// </returns>
        ValidationNode ValidateNode(Type type, DataNode node, ISerializationContext? context = null);

        /// <summary>
        ///     Validates that a node has all the properties required by a certain type with its serializer.
        /// </summary>
        /// <param name="node">The node to check.</param>
        /// <param name="context">The context to use, if any.</param>
        /// <returns>
        ///     A node with whether or not <see cref="node"/> is valid and which of its fields
        ///     are invalid, if any.
        /// </returns>
        ValidationNode ValidateNode<T>(DataNode node, ISerializationContext? context = null);

        //todo paul docs
        ValidationNode ValidateNode<T, TNode>(ITypeValidator<T, TNode> typeValidator, TNode node,
            ISerializationContext? context = null) where TNode : DataNode;

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
        /// <returns>The deserialized object or null.</returns>
        public object? Read(Type type, DataNode node, ISerializationContext? context = null, bool skipHook = false);

        /// <summary>
        ///     Deserializes a node into a populated object of the given generic type <see cref="T"/>
        /// </summary>
        /// <param name="node">The node to deserialize.</param>
        /// <param name="context">The context to use, if any.</param>
        /// <param name="skipHook">Whether or not to skip running <see cref="ISerializationHooks"/></param>
        /// <param name="instanceProvider">The valueProvider which can provide a value to read into. If none is supplied, a new object will be created.</param>
        /// <typeparam name="T">The type of object to create and populate.</typeparam>
        /// <returns>The deserialized object, or null.</returns>
        T Read<T>(DataNode node, ISerializationContext? context = null, bool skipHook = false, InstantiationDelegate<T>? instanceProvider = null);

        /// <summary>
        /// todo paul docs
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="node"></param>
        /// <param name="context"></param>
        /// <param name="skipHook"></param>
        /// <param name="instanceProvider"></param>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TNode"></typeparam>
        /// <returns></returns>
        T Read<T, TNode>(ITypeReader<T, TNode> reader, TNode node, ISerializationContext? context = null,
            bool skipHook = false, InstantiationDelegate<T>? instanceProvider = null) where TNode : DataNode;

        T Read<T, TNode, TReader>(TNode node, ISerializationContext? context = null,
            bool skipHook = false, InstantiationDelegate<T>? instanceProvider = null) where TNode : DataNode where TReader : ITypeReader<T, TNode>;

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
        /// <typeparam name="T">The type to serialize.</typeparam>
        /// <returns>A serialized datanode created from the given <see cref="value"/>.</returns>
        DataNode WriteValue<T>(T value, bool alwaysWrite = false, ISerializationContext? context = null);

        /// <summary>
        /// todo paul docs
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="alwaysWrite"></param>
        /// <param name="context"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        DataNode WriteValue<T>(ITypeWriter<T> writer, T value, bool alwaysWrite = false, ISerializationContext? context = null);

        DataNode WriteValue<T, TWriter>(T value, bool alwaysWrite = false, ISerializationContext? context = null) where TWriter : ITypeWriter<T>;

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
        DataNode WriteValue(Type type, object? value, bool alwaysWrite = false, ISerializationContext? context = null);

        /// <summary>
        ///     Serializes a value into a node.
        /// </summary>
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
        DataNode WriteValue(object? value, bool alwaysWrite = false, ISerializationContext? context = null);

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
        void CopyTo(object source, ref object? target, ISerializationContext? context = null, bool skipHook = false);

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
        void CopyTo<T>(T source, ref T? target, ISerializationContext? context = null, bool skipHook = false);

        /// <summary>
        /// todo paul docs
        /// </summary>
        /// <param name="copier"></param>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="context"></param>
        /// <param name="skipHook"></param>
        /// <typeparam name="T"></typeparam>
        void CopyTo<T>(ITypeCopier<T> copier, T source, ref T? target, ISerializationContext? context = null, bool skipHook = false);

        void CopyTo<T, TCopier>(T source, ref T? target, ISerializationContext? context = null, bool skipHook = false) where TCopier : ITypeCopier<T>;

        /// <summary>
        ///     Creates a copy of the given object.
        /// </summary>
        /// <param name="source">The object to copy.</param>
        /// <param name="context">The context to use, if any.</param>
        /// <param name="skipHook">Whether or not to skip running <see cref="ISerializationHooks"/></param>
        /// <returns>A copy of the given object.</returns>
        [MustUseReturnValue]
        object? CreateCopy(object? source, ISerializationContext? context = null, bool skipHook = false);

        /// <summary>
        ///     Creates a copy of the given object.
        /// </summary>
        /// <param name="source">The object to copy.</param>
        /// <param name="context">The context to use, if any.</param>
        /// <param name="skipHook">Whether or not to skip running <see cref="ISerializationHooks"/></param>
        /// <typeparam name="T">The type of the object to copy.</typeparam>
        /// <returns>A copy of the given object.</returns>
        [MustUseReturnValue]
        T CreateCopy<T>(T source, ISerializationContext? context = null, bool skipHook = false);

        /// <summary>
        /// todo paul docs
        /// </summary>
        /// <param name="copyCreator"></param>
        /// <param name="source"></param>
        /// <param name="context"></param>
        /// <param name="skipHook"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MustUseReturnValue]
        T CreateCopy<T>(ITypeCopyCreator<T> copyCreator, T source, ISerializationContext? context = null, bool skipHook = false);

        [MustUseReturnValue]
        T CreateCopy<T, TCopyCreator>(T source, ISerializationContext? context = null, bool skipHook = false) where TCopyCreator : ITypeCopyCreator<T>;

        #endregion

        #region Flags And Constants

        Type GetFlagTypeFromTag(Type tagType);

        int GetFlagHighestBit(Type tagType);

        Type GetConstantTypeFromTag(Type tagType);

        #endregion

        #region Composition

        DataNode PushComposition(Type type, DataNode[] parents, DataNode child, ISerializationContext? context = null);

        public TNode PushComposition<TType, TNode>(TNode[] parents, TNode child, ISerializationContext? context = null) where TNode : DataNode
        {
            // ReSharper disable once CoVariantArrayConversion
            return (TNode)PushComposition(typeof(TType), parents, child, context);
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

        #endregion
    }
}
