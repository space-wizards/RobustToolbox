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
        private delegate object CopySerializerDelegate(
            object source,
            ref object target,
            bool skipHook,
            ISerializationContext? context = null);

        private readonly Dictionary<Type, object> _customTypeSerializers = new();

        private readonly ConcurrentDictionary<(Type common, Type source, Type target, Type serializer), CopySerializerDelegate>
            _copySerializerDelegates = new();

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

                var returnLabel = Expression.Label(typeof(object));
                var returnExpression = Expression.Label(returnLabel, targetParam);

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
                    Expression.Assign(targetCastVariable, call),
                    Expression.Assign(
                        targetParam,
                        Expression.Convert(targetCastVariable, typeof(object))),
                    Expression.Return(returnLabel, targetParam),
                    returnExpression);

                return Expression.Lambda<CopySerializerDelegate>(
                    block,
                    sourceParam,
                    targetParam,
                    skipHookParam,
                    contextParam).Compile();
            }, (common, source, target, serializer));
        }

        private DeserializationResult ReadWithSerializer<T, TNode, TSerializer>(
            TNode node,
            ISerializationContext? context = null,
            bool skipHook = false)
            where TSerializer : ITypeReader<T, TNode>
            where T : notnull
            where TNode : DataNode
        {
            var serializer = (ITypeReader<T, TNode>)GetTypeSerializer(typeof(TSerializer));
            return serializer.Read(this, node, DependencyCollection, skipHook, context);
        }

        private DataNode WriteWithSerializer<T, TSerializer>(
            T value,
            ISerializationContext? context = null,
            bool alwaysWrite = false)
            where TSerializer : ITypeWriter<T>
            where T : notnull
        {
            var serializer = (ITypeWriter<T>)GetTypeSerializer(typeof(TSerializer));
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
            where TCommon : notnull
            where TSerializer : ITypeCopier<TCommon>
        {
            var serializer = (ITypeCopier<TCommon>) GetTypeSerializer(typeof(TSerializer));
            return serializer.Copy(this, source, target, skipHook, context);
        }

        private ValidationNode ValidateWithSerializer<T, TNode, TSerializer>(
            TNode node,
            ISerializationContext? context)
            where T : notnull
            where TNode : DataNode
            where TSerializer : ITypeValidator<T, TNode>
        {
            var serializer = (ITypeValidator<T, TNode>) GetTypeSerializer(typeof(TSerializer));
            return serializer.Validate(this, node, DependencyCollection, context);
        }
    }
}
