using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using JetBrains.Annotations;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.Manager
{
    public partial class SerializationManager
    {
        //todo paul serv3 find a non-hardcoded way for this

        private delegate object? ReadSerializerDelegate(
            DataNode node,
            ISerializationContext? context = null,
            bool skipHook = false,
            object? value = null);

        private delegate DataNode WriteSerializerDelegate(
            object value,
            ISerializationContext? context = null,
            bool alwaysWrite = false);

        private delegate void CopyToSerializerDelegate(
            object source,
            ref object target,
            bool skipHook,
            ISerializationContext? context = null);

        private delegate object CreateCopySerializerDelegate(
            object source,
            bool skipHook,
            ISerializationContext? context = null);

        private delegate ValidationNode ValidateSerializerDelegate(
            DataNode node,
            ISerializationContext? context = null);

        private readonly ConcurrentDictionary<Type, object> _customTypeSerializers = new();

        private readonly ConcurrentDictionary<(Type value, Type node, Type serializer), ReadSerializerDelegate>
            _customReadSerializerDelegates = new();

        private readonly ConcurrentDictionary<(Type value, Type serializer), WriteSerializerDelegate>
            _customWriteSerializerDelegates = new();

        private readonly ConcurrentDictionary<(Type common, Type source, Type target, Type serializer), CopyToSerializerDelegate>
            _customCopyToSerializerDelegates = new();

        private readonly ConcurrentDictionary<(Type source, Type serializer), CreateCopySerializerDelegate>
            _customCreateCopySerializerDelegates = new();

        private readonly ConcurrentDictionary<(Type value, Type node, Type serializer), ValidateSerializerDelegate>
            _customValidateSerializerDelegates = new();

        internal object GetOrCreateCustomTypeSerializer(Type type)
        {
            return _customTypeSerializers.GetOrAdd(type, CreateSerializer);
        }

        private ReadSerializerDelegate GetOrCreateReadCustomSerializerDelegate(Type value, Type node, Type serializer)
        {
            return _customReadSerializerDelegates.GetOrAdd((value, node, serializer), static (tuple, instance) =>
            {
                var instanceParam = Expression.Constant(instance);
                var nodeParam = Expression.Parameter(typeof(DataNode), "node");
                var contextParam = Expression.Parameter(typeof(ISerializationContext), "context");
                var skipHookParam = Expression.Parameter(typeof(bool), "skipHook");
                var valueParam = Expression.Parameter(typeof(object), "value");

                var serializerInstance = instance.GetOrCreateCustomTypeSerializer(tuple.serializer);
                var serializerConstant = Expression.Constant(serializerInstance);

                var dependencyConst = Expression.Constant(instance.DependencyCollection);

                var call = Expression.Call(
                    serializerConstant,
                    typeof(ITypeReader<,>).MakeGenericType(tuple.value, tuple.node).GetMethod("Read")!,
                    instanceParam,
                    Expression.Convert(nodeParam, tuple.node),
                    dependencyConst,
                    skipHookParam,
                    contextParam,
                    !tuple.value.IsValueType
                        ? Expression.Convert(valueParam, tuple.value)
                        : Expression.Call(
                            instanceParam,
                            nameof(GetValueOrDefault),
                            new[] { tuple.value },
                            valueParam));

                return Expression.Lambda<ReadSerializerDelegate>(
                    Expression.Convert(call, typeof(object)),
                    nodeParam,
                    contextParam,
                    skipHookParam,
                    valueParam).Compile();
            }, this);
        }

        private WriteSerializerDelegate GetOrCreateWriteCustomSerializerDelegate(Type value, Type serializer)
        {
            return _customWriteSerializerDelegates.GetOrAdd((value, serializer), static (tuple, instance) =>
            {
                var instanceParam = Expression.Constant(instance);
                var valueParam = Expression.Parameter(typeof(object), "value");
                var contextParam = Expression.Parameter(typeof(ISerializationContext), "context");
                var alwaysWriteParam = Expression.Parameter(typeof(bool), "alwaysWrite");

                var serializerInstance = instance.GetOrCreateCustomTypeSerializer(tuple.serializer);
                var serializerConstant = Expression.Constant(serializerInstance);

                var dependencyConst = Expression.Constant(instance.DependencyCollection);

                var call = Expression.Call(
                    serializerConstant,
                    typeof(ITypeWriter<>).MakeGenericType(tuple.value).GetMethod("Write")!,
                    instanceParam,
                    Expression.Convert(valueParam, tuple.value),
                    dependencyConst,
                    alwaysWriteParam,
                    contextParam);

                return Expression.Lambda<WriteSerializerDelegate>(
                    call,
                    valueParam,
                    contextParam,
                    alwaysWriteParam).Compile();
            }, this);
        }

        private CopyToSerializerDelegate GetOrCreateCopyToCustomSerializerDelegate(Type common, Type source, Type target, Type serializer)
        {
            return _customCopyToSerializerDelegates.GetOrAdd((common, source, target, serializer), static (_, tuple) =>
            {
                var instanceParam = Expression.Constant(tuple.manager);
                var sourceParam = Expression.Parameter(typeof(object), "source");
                var targetParam = Expression.Parameter(typeof(object).MakeByRefType(), "target");
                var skipHookParam = Expression.Parameter(typeof(bool), "skipHook");
                var contextParam = Expression.Parameter(typeof(ISerializationContext), "context");

                var serializerInstance = tuple.manager.GetOrCreateCustomTypeSerializer(tuple.serializer);
                var serializerConstant = Expression.Constant(serializerInstance);


                var targetVar = Expression.Variable(tuple.common);
                var call = Expression.Block(
                    new[] { targetVar },
                    Expression.Assign(targetVar, Expression.Convert(targetParam, tuple.common)),
                    Expression.Call(
                        serializerConstant,
                        typeof(ITypeCopier<>).MakeGenericType(tuple.common).GetMethod("CopyTo")!,
                        instanceParam,
                        Expression.Convert(sourceParam, tuple.common),
                        targetVar,
                        skipHookParam,
                        contextParam),
                    Expression.Assign(targetParam, Expression.Convert(targetVar, typeof(object))));

                return Expression.Lambda<CopyToSerializerDelegate>(
                    call,
                    sourceParam,
                    targetParam,
                    skipHookParam,
                    contextParam).Compile();
            }, (common, source, target, serializer, manager: this));
        }

        private CreateCopySerializerDelegate GetOrCreateCreateCopyCustomSerializerDelegate(Type source, Type serializer)
        {
            return _customCreateCopySerializerDelegates.GetOrAdd((source, serializer), static (_, tuple) =>
            {
                var instanceParam = Expression.Constant(tuple.manager);
                var sourceParam = Expression.Parameter(typeof(object), "source");
                var skipHookParam = Expression.Parameter(typeof(bool), "skipHook");
                var contextParam = Expression.Parameter(typeof(ISerializationContext), "context");

                var serializerInstance = tuple.manager.GetOrCreateCustomTypeSerializer(tuple.serializer);
                var serializerConstant = Expression.Constant(serializerInstance);

                var call = Expression.Call(
                    serializerConstant,
                    typeof(ITypeCopyCreator<>).MakeGenericType(tuple.source).GetMethod("CreateCopy")!,
                    instanceParam,
                    Expression.Convert(sourceParam, tuple.source),
                    skipHookParam,
                    contextParam);

                return Expression.Lambda<CreateCopySerializerDelegate>(
                    Expression.Convert(call, typeof(object)),
                    sourceParam,
                    skipHookParam,
                    contextParam).Compile();
            }, (source, serializer, manager: this));
        }

        private ValidateSerializerDelegate GetOrCreateValidateCustomSerializerDelegate(Type type, Type nodeType, Type serializer)
        {
            return _customValidateSerializerDelegates.GetOrAdd((type, nodeType, serializer), static (types, manager) =>
            {
                var instanceParam = Expression.Constant(manager);
                var dependencyConst = Expression.Constant(manager.DependencyCollection);

                var nodeParam = Expression.Parameter(typeof(DataNode), "node");
                var contextParam = Expression.Parameter(typeof(ISerializationContext), "context");

                var serializerInstance = manager.GetOrCreateCustomTypeSerializer(types.serializer);
                var serializerConstant = Expression.Constant(serializerInstance);

                var call = Expression.Call(
                    serializerConstant,
                    typeof(ITypeValidator<,>).MakeGenericType(types.value, types.node).GetMethod("Validate")!,
                    instanceParam,
                    Expression.Convert(nodeParam, types.node),
                    dependencyConst,
                    contextParam);

                return Expression.Lambda<ValidateSerializerDelegate>(
                    call,
                    nodeParam,
                    contextParam).Compile();
            }, this);
        }

        public object? ReadWithCustomSerializer(
            Type type,
            Type serializer,
            DataNode node,
            ISerializationContext? context = null,
            bool skipHook = false,
            object? value = null)
        {
            return GetOrCreateReadCustomSerializerDelegate(type, node.GetType(), serializer)(node, context, skipHook, value);
        }

        public DataNode WriteWithCustomSerializer(
            Type type,
            Type serializer,
            object? value,
            ISerializationContext? context = null,
            bool alwaysWrite = false)
        {
            if (value == null) return NullNode();

            return GetOrCreateWriteCustomSerializerDelegate(type, serializer)(value, context, alwaysWrite);
        }

        public void CopyToWithCustomSerializer(
            Type serializer,
            object source,
            ref object target,
            bool skipHook = false,
            ISerializationContext? context = null)
        {
            var sourceType = source.GetType();
            var targetType = source.GetType();
            if (!TypeHelpers.TrySelectCommonType(sourceType, targetType, out var common))
                throw new ArgumentException();

            GetOrCreateCopyToCustomSerializerDelegate(common, sourceType, targetType, serializer)(source,
                ref target, skipHook, context);
        }

        [MustUseReturnValue]
        public object CreateCopyWithCustomSerializer(
            Type serializer,
            object source,
            bool skipHook = false,
            ISerializationContext? context = null)
        {
            return GetOrCreateCreateCopyCustomSerializerDelegate(source.GetType(), serializer)(source, skipHook, context);
        }

        public ValidationNode ValidateWithCustomSerializer(
            Type type,
            Type serializer,
            DataNode node,
            ISerializationContext? context = null)
        {
            return GetOrCreateValidateCustomSerializerDelegate(type, node.GetType(), serializer)(node, context);
        }
    }
}
