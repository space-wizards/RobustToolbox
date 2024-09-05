using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Globalization;
using System.Linq.Expressions;
using Robust.Shared.Serialization.Manager.Definition;
using Robust.Shared.Serialization.Manager.Exceptions;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.Manager;

public sealed partial class SerializationManager
{
    #region DelegateElements

    private delegate DataNode WriteBoxingDelegate(
        object value,
        bool alwaysWrite,
        ISerializationContext? context);

    private delegate DataNode WriteGenericDelegate<T>(
        T value,
        bool alwaysWrite,
        ISerializationContext? context);

    private readonly ConcurrentDictionary<(Type, bool), WriteBoxingDelegate> _writeBoxingDelegates = new();
    private readonly ConcurrentDictionary<(Type baseType, Type actualType, bool), object> _writeGenericBaseDelegates = new();
    private readonly ConcurrentDictionary<(Type, bool), object> _writeGenericDelegates = new();

    private WriteBoxingDelegate GetOrCreateWriteBoxingDelegate(Type type, bool notNullableOverride)
    {
        return _writeBoxingDelegates.GetOrAdd((type, notNullableOverride), static (tuple, manager) =>
        {
            var type = tuple.Item1;
            var managerConst = Expression.Constant(manager);

            var valueParam = Expression.Variable(typeof(object));
            var alwaysWrite = Expression.Variable(typeof(bool));
            var contextParam = Expression.Variable(typeof(ISerializationContext));

            var call = Expression.Call(
                managerConst,
                nameof(WriteValue),
                new[] { type },
                Expression.Convert(valueParam, type),
                alwaysWrite,
                contextParam,
                Expression.Constant(tuple.Item2));

            return Expression.Lambda<WriteBoxingDelegate>(
                call,
                valueParam,
                alwaysWrite,
                contextParam).Compile();
        }, this);
    }

    private WriteGenericDelegate<T> GetOrCreateWriteGenericDelegate<T>(T value, bool notNullableOverride)
    {
        static object ValueFactory(Type baseType, Type actualType, bool notNullableOverride, SerializationManager serializationManager)
        {
            var instanceParam = Expression.Constant(serializationManager);
            var objParam = Expression.Parameter(baseType, "value");
            var alwaysWriteParam = Expression.Parameter(typeof(bool), "alwaysWrite");
            var contextParam = Expression.Parameter(typeof(ISerializationContext), "context");

            actualType = actualType.EnsureNotNullableType();
            var sameType = baseType.EnsureNotNullableType() == actualType;

            Expression objAccess = baseType.IsNullable()
                ? Expression.Convert(objParam, sameType ? baseType.EnsureNotNullableType() : actualType)
                : sameType ? objParam : Expression.Convert(objParam, actualType);

            if (baseType.IsGenericType)
            {
                // Frozen dictionaries/sets are abstract and have a bunch of implementations, but we always serialize them as their abstract type.
                var t = baseType.GetGenericTypeDefinition();
                if (t == typeof(FrozenDictionary<,>) || t == typeof(FrozenSet<>))
                    actualType = baseType;
            }

            Expression call;
            if (serializationManager._regularSerializerProvider.TryGetTypeSerializer(typeof(ITypeWriter<>), actualType, out var serializer))
            {
                var serializerConst = Expression.Constant(serializer);
                call = Expression.Call(
                    instanceParam,
                    nameof(WriteValue),
                    new []{actualType},
                    serializerConst,
                    objAccess,
                    alwaysWriteParam,
                    contextParam,
                    Expression.Constant(notNullableOverride)
                );
            }
            else if (actualType.IsEnum)
            {
                // When writing generic enums, we want to use the enum serializer.
                // Otherwise, we fall back to the default IConvertible behaviour.

                if (baseType != typeof(Enum) ||
                    !serializationManager._regularSerializerProvider.TryGetTypeSerializer(typeof(ITypeWriter<>),
                        typeof(Enum), out serializer))
                {
                    call = Expression.Call(
                        instanceParam,
                        nameof(WriteConvertible),
                        Type.EmptyTypes,
                        Expression.Convert(objParam, typeof(IConvertible)));
                }
                else
                {
                    var serializerConst = Expression.Constant(serializer);
                    call = Expression.Call(
                        instanceParam,
                        nameof(WriteValue),
                        new []{typeof(Enum)},
                        serializerConst,
                        Expression.Convert(objParam, typeof(Enum)),
                        alwaysWriteParam,
                        contextParam,
                        Expression.Constant(notNullableOverride)
                    );
                }
            }
            else if (actualType.IsArray)
            {
                call = Expression.Call(
                    instanceParam,
                    nameof(WriteArray),
                    new []{ actualType.GetElementType()! },
                    Expression.Convert(objParam, actualType),
                    alwaysWriteParam,
                    contextParam);
            }
            else if (typeof(ISelfSerialize).IsAssignableFrom(actualType))
            {
                call = Expression.Call(
                    instanceParam,
                    nameof(WriteSelfSerializable),
                    Type.EmptyTypes,
                    Expression.Convert(objParam, typeof(ISelfSerialize)));
            }
            else
            {
                call = Expression.Call(
                    instanceParam,
                    nameof(WriteValueInternal),
                    new[] { actualType },
                    objAccess,
                    Expression.Constant(serializationManager.GetDefinition(actualType), typeof(DataDefinition<>).MakeGenericType(actualType)),
                    alwaysWriteParam,
                    contextParam);

                if (!sameType)
                {
                    var nodeVar = Expression.Variable(typeof(DataNode));
                    call = Expression.Block(
                        new[] { nodeVar },
                        Expression.Assign(nodeVar, call),
                        Expression.Assign(Expression.Field(nodeVar, "Tag"), Expression.Constant($"!type:{actualType.Name}")),
                        nodeVar);
                }
            }

            // check for customtypeserializer before anything
            var serializerType = typeof(ITypeWriter<>).MakeGenericType(actualType);
            var serializerVar = Expression.Variable(serializerType);
            call = Expression.Block(
                new[] { serializerVar },
                Expression.Condition(
                    Expression.AndAlso(
                        Expression.ReferenceNotEqual(contextParam,
                            Expression.Constant(null, typeof(ISerializationContext))),
                        Expression.Call(
                            Expression.Property(contextParam, "SerializerProvider"),
                            "TryGetTypeSerializer",
                            new[] { serializerType, actualType },
                            serializerVar)),
                    Expression.Call(
                        instanceParam,
                        nameof(WriteValue),
                        new []{actualType},
                        serializerVar,
                        objAccess,
                        alwaysWriteParam,
                        contextParam,
                        Expression.Constant(notNullableOverride)),
                    call));

            return Expression.Lambda<WriteGenericDelegate<T>>(
                call,
                objParam,
                alwaysWriteParam,
                contextParam).Compile();
        }

        var type = typeof(T);
        if (!type.IsSealed) // abstract classes, virtual classes, and interfaces.
        {
            return (WriteGenericDelegate<T>)_writeGenericBaseDelegates.GetOrAdd((type, value!.GetType(), notNullableOverride),
                static (tuple, manager) => ValueFactory(tuple.baseType, tuple.actualType, tuple.Item3, manager), this);
        }

        return (WriteGenericDelegate<T>) _writeGenericDelegates
            .GetOrAdd((type, notNullableOverride), static (tuple, manager) => ValueFactory(tuple.Item1, tuple.Item1, tuple.Item2, manager), this);
    }

    private DataNode WriteConvertible(IConvertible obj)
    {
        return new ValueDataNode(obj.ToString(CultureInfo.InvariantCulture));
    }

    private DataNode WriteSelfSerializable(ISelfSerialize obj)
    {
        return new ValueDataNode(obj.Serialize());
    }

    private DataNode WriteArray<TElement>(TElement[] obj, bool alwaysWrite, ISerializationContext? context)
    {
        var sequenceNode = new SequenceDataNode();

        foreach (var val in obj)
        {
            var serializedVal = WriteValue(val, alwaysWrite, context);
            sequenceNode.Add(serializedVal);
        }

        return sequenceNode;
    }

    private DataNode WriteValueInternal<T>(
        T value,
        DataDefinition<T>? definition,
        bool alwaysWrite,
        ISerializationContext? context)
        where T : notnull
    {
        //this check is in here on purpose. we cannot check this during expression tree generation due to the value maybe being handled by a custom typeserializer
        if(definition == null)
            throw new InvalidOperationException($"No data definition found for type {typeof(T)} when writing");

        var mapping = definition.Serialize(value, context, alwaysWrite);

        return mapping;
    }

    #endregion

    public DataNode WriteValue<T>(T value, bool alwaysWrite = false, ISerializationContext? context = null, bool notNullableOverride = false)
    {
        if(value == null)
        {
            CanWriteNullCheck(typeof(T), notNullableOverride);
            return ValueDataNode.Null();
        }

        var node = GetOrCreateWriteGenericDelegate(value, notNullableOverride)(value, alwaysWrite, context);

        if (typeof(T) == typeof(object))
            node.Tag = "!type:" + value.GetType().Name;

        return node;
    }

    public DataNode WriteValue<T>(ITypeWriter<T> writer, T value, bool alwaysWrite = false,
        ISerializationContext? context = null, bool notNullableOverride = false)
    {
        if (value == null)
        {
            CanWriteNullCheck(typeof(T), notNullableOverride);
            return NullNode();
        }

        return writer.Write(this, value, DependencyCollection, alwaysWrite, context);
    }

    public DataNode WriteValue<T, TWriter>(T value, bool alwaysWrite = false, ISerializationContext? context = null, bool notNullableOverride = false)
        where TWriter : ITypeWriter<T>
    {
        return WriteValue(GetOrCreateCustomTypeSerializer<TWriter>(), value, alwaysWrite, context, notNullableOverride);
    }

    public DataNode WriteValue(Type type, object? value, bool alwaysWrite = false, ISerializationContext? context = null, bool notNullableOverride = false)
    {
        if (value == null)
        {
            CanWriteNullCheck(type, notNullableOverride);

            return NullNode();
        }

        return GetOrCreateWriteBoxingDelegate(type, notNullableOverride)(value, alwaysWrite, context);
    }

    private void CanWriteNullCheck(Type type, bool notNullableOverride)
    {
        if (!type.IsNullable() || notNullableOverride)
        {
            throw new NullNotAllowedException();
        }
    }
}
