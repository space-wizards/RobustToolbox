using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.Manager
{
    public partial class SerializationManager
    {
        private delegate DeserializationResult ReadSerializerDelegate(
            DataNode node,
            ISerializationContext? context = null,
            bool skipHook = false);

        private delegate DataNode WriteSerializerDelegate(
            object value,
            ISerializationContext? context = null,
            bool alwaysWrite = false);

        private delegate object CopySerializerDelegate(
            object source,
            ref object target,
            bool skipHook,
            ISerializationContext? context = null);

        private readonly Dictionary<Type, object> _customTypeSerializers = new();

        private readonly ConcurrentDictionary<(Type value, Type node, Type serializer), ReadSerializerDelegate>
            _readSerializerDelegates = new();

        private readonly ConcurrentDictionary<(Type value, Type serializer), WriteSerializerDelegate>
            _writeSerializerDelegates = new();

        private readonly ConcurrentDictionary<(Type common, Type source, Type target, Type serializer), CopySerializerDelegate>
            _copySerializerDelegates = new();

        private ReadSerializerDelegate GetOrCreateReadSerializerDelegate(Type value, Type node, Type serializer)
        {
            return _readSerializerDelegates.GetOrAdd((value, node, serializer), static (tuple, instance) =>
            {
                var instanceParam = Expression.Constant(instance);
                var nodeParam = Expression.Parameter(typeof(DataNode), "node");
                var contextParam = Expression.Parameter(typeof(ISerializationContext), "context");
                var skipHookParam = Expression.Parameter(typeof(bool), "skipHook");

                var call = Expression.Call(
                    instanceParam,
                    nameof(ReadWithSerializer),
                    new[] {tuple.value, tuple.node, tuple.serializer},
                    Expression.Convert(nodeParam, tuple.node),
                    contextParam,
                    skipHookParam);

                return Expression.Lambda<ReadSerializerDelegate>(
                    call,
                    nodeParam,
                    contextParam,
                    skipHookParam).Compile();
            }, this);
        }

        private WriteSerializerDelegate GetOrCreateWriteSerializerDelegate(Type value, Type serializer)
        {
            return _writeSerializerDelegates.GetOrAdd((value, serializer), static (tuple, instance) =>
            {
                var instanceParam = Expression.Constant(instance);
                var valueParam = Expression.Parameter(typeof(object), "value");
                var contextParam = Expression.Parameter(typeof(ISerializationContext), "context");
                var alwaysWriteParam = Expression.Parameter(typeof(bool), "alwaysWrite");

                var call = Expression.Call(
                    instanceParam,
                    nameof(WriteWithSerializer),
                    new[] {tuple.value, tuple.serializer},
                    Expression.Convert(valueParam, tuple.value),
                    contextParam,
                    alwaysWriteParam);

                return Expression.Lambda<WriteSerializerDelegate>(
                    call,
                    valueParam,
                    contextParam,
                    alwaysWriteParam).Compile();
            }, this);
        }

        private CopySerializerDelegate GetOrCreateCopySerializerDelegate(Type common, Type source, Type target, Type serializer)
        {
            return _copySerializerDelegates.GetOrAdd((common, source, target, serializer), (_, tuple) =>
            {
                var instanceParam = Expression.Constant(this);
                var sourceParam = Expression.Parameter(typeof(object), "source");
                var targetParam = Expression.Parameter(typeof(object).MakeByRefType(), "target");
                var skipHookParam = Expression.Parameter(typeof(bool), "skipHook");
                var contextParam = Expression.Parameter(typeof(ISerializationContext), "context");

                var targetCastVariable = Expression.Variable(tuple.target, "targetCastVariable");

                var call = Expression.Call(
                    instanceParam,
                    nameof(CopyWithSerializer),
                    new[] {tuple.common, tuple.source, tuple.target, tuple.serializer},
                    Expression.Convert(sourceParam, tuple.source),
                    targetCastVariable,
                    skipHookParam,
                    contextParam);

                var block = Expression.Block(
                    new[] {targetCastVariable},
                    Expression.Assign(
                        targetCastVariable,
                        Expression.Convert(targetParam, tuple.target)),
                    Expression.Convert(call, typeof(object)));

                return Expression.Lambda<CopySerializerDelegate>(
                    block,
                    sourceParam,
                    targetParam,
                    skipHookParam,
                    contextParam).Compile();
            }, (common, source, target, serializer));
        }

        private DeserializationResult ReadWithSerializerRaw(
            Type value,
            DataNode node,
            Type serializer,
            ISerializationContext? context = null,
            bool skipHook = false)
        {
            return GetOrCreateReadSerializerDelegate(value, node.GetType(), serializer)(node, context, skipHook);
        }

        private DeserializationResult ReadWithSerializer<T, TNode, TSerializer>(
            TNode node,
            ISerializationContext? context = null,
            bool skipHook = false)
            where TSerializer : ITypeReader<T, TNode>
            where TNode : DataNode
        {
            var serializer = (ITypeReader<T, TNode>) GetTypeSerializer(typeof(TSerializer));
            return serializer.Read(this, node, DependencyCollection, skipHook, context, TODO);
        }

        private DataNode WriteWithSerializerRaw(
            Type type,
            Type serializer,
            object value,
            ISerializationContext? context = null,
            bool alwaysWrite = false)
        {
            return GetOrCreateWriteSerializerDelegate(type, serializer)(value, context, alwaysWrite);
        }

        private DataNode WriteWithSerializer<T, TSerializer>(
            T value,
            ISerializationContext? context = null,
            bool alwaysWrite = false)
            where TSerializer : ITypeWriter<T>
        {
            var serializer = (ITypeWriter<T>) GetTypeSerializer(typeof(TSerializer));
            return serializer.Write(this, value, alwaysWrite, context);
        }

        private object CopyWithSerializerRaw(Type serializer, object source, ref object target, bool skipHook, ISerializationContext? context = null)
        {
            var sourceType = source.GetType();
            var targetType = target.GetType();
            var commonType = TypeHelpers.SelectCommonType(sourceType, targetType) ??
                             throw new ArgumentException($"No common type found between {sourceType} and {targetType}");

            return GetOrCreateCopySerializerDelegate(commonType, sourceType, targetType, serializer)(source, ref target, skipHook, context);
        }

        private TCommon CopyWithSerializer<TCommon, TSource, TTarget, TSerializer>(
            TSource source,
            ref TTarget target,
            bool skipHook,
            ISerializationContext? context = null)
            where TSource : TCommon
            where TTarget : TCommon
            where TSerializer : ITypeCopier<TCommon>
        {
            var serializer = (ITypeCopier<TCommon>) GetTypeSerializer(typeof(TSerializer));
            return serializer.Copy(this, source, target, skipHook, context);
        }

        private ValidationNode ValidateWithSerializer<T, TNode, TSerializer>(
            TNode node,
            ISerializationContext? context)
            where TNode : DataNode
            where TSerializer : ITypeValidator<T, TNode>
        {
            var serializer = (ITypeValidator<T, TNode>) GetTypeSerializer(typeof(TSerializer));
            return serializer.Validate(this, node, DependencyCollection, context);
        }
    }
}
