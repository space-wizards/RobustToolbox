using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq.Expressions;
using Robust.Shared.Serialization.Manager.Definition;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

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

    private readonly ConcurrentDictionary<Type, WriteBoxingDelegate> _writeBoxingDelegates = new();
    private readonly ConcurrentDictionary<(Type baseType, Type actualType), object> _writeGenericBaseDelegates = new();
    private readonly ConcurrentDictionary<Type, object> _writeGenericDelegates = new();

    private WriteBoxingDelegate GetOrCreateWriteBoxingDelegate(Type type)
    {
        return _writeBoxingDelegates.GetOrAdd(type, static (type, manager) =>
        {
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
                contextParam);

            return Expression.Lambda<WriteBoxingDelegate>(
                call,
                valueParam,
                alwaysWrite,
                contextParam).Compile();
        }, this);

    }

    private WriteGenericDelegate<T> GetOrCreateWriteGenericDelegate<T>(T value)
    {
        static object ValueFactory(Type baseType, Type actualType, SerializationManager serializationManager)
        {
            var instanceParam = Expression.Constant(serializationManager);
            var objParam = Expression.Parameter(baseType, "value");
            var alwaysWriteParam = Expression.Parameter(typeof(bool), "alwaysWrite");
            var contextParam = Expression.Parameter(typeof(ISerializationContext), "context");

            var sameType = baseType == actualType;
            Expression call;
            if (serializationManager._regularSerializerProvider.TryGetTypeSerializer(typeof(ITypeWriter<>), actualType, out var serializer))
            {
                var serializerConst = Expression.Constant(serializer);
                call = Expression.Call(
                    instanceParam,
                    nameof(WriteValue),
                    new []{actualType},
                    serializerConst,
                    sameType ? objParam : Expression.Convert(objParam, actualType),
                    alwaysWriteParam,
                    contextParam
                );
            }
            else if (actualType.IsEnum)
            {
                // Enums implement IConvertible.
                // Need it for the culture overload.
                call = Expression.Call(
                    instanceParam,
                    nameof(WriteConvertible),
                    Type.EmptyTypes,
                    Expression.Convert(objParam, typeof(IConvertible)));
            }
            else if (actualType.IsArray)
            {
                call = Expression.Call(
                    instanceParam,
                    nameof(WriteArray),
                    Type.EmptyTypes,
                    Expression.Convert(objParam, typeof(Array)),
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
                    sameType ? objParam : Expression.Convert(objParam, actualType),
                    Expression.Constant(serializationManager.GetDefinition(actualType), typeof(DataDefinition<>).MakeGenericType(actualType)),
                    alwaysWriteParam,
                    contextParam);

                if (!sameType)
                {
                    var mappingVar = Expression.Variable(typeof(MappingDataNode));
                    call = Expression.Block(
                        new[] { mappingVar },
                        Expression.Assign(mappingVar, call),
                        Expression.Assign(Expression.Field(mappingVar, "Tag"), Expression.Constant($"!type:{actualType.Name}")),
                        mappingVar);
                }

                call = Expression.Convert(call, typeof(DataNode));
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
                        sameType ? objParam : Expression.Convert(objParam, actualType),
                        alwaysWriteParam,
                        contextParam),
                    call));

            return Expression.Lambda<WriteGenericDelegate<T>>(
                call,
                objParam,
                alwaysWriteParam,
                contextParam).Compile();
        }

        var type = typeof(T);
        if (type.IsAbstract || type.IsInterface)
        {
            return (WriteGenericDelegate<T>)_writeGenericBaseDelegates.GetOrAdd((type, value!.GetType()),
                static (tuple, manager) => ValueFactory(tuple.baseType, tuple.actualType, manager), this);
        }

        return (WriteGenericDelegate<T>) _writeGenericDelegates
            .GetOrAdd(type, static (type, manager) => ValueFactory(type, type, manager), this);
    }

    private DataNode WriteConvertible(IConvertible obj)
    {
        return new ValueDataNode(obj.ToString(CultureInfo.InvariantCulture));
    }

    private DataNode WriteSelfSerializable(ISelfSerialize obj)
    {
        return new ValueDataNode(obj.Serialize());
    }

    private DataNode WriteArray(Array obj, bool alwaysWrite, ISerializationContext? context)
    {
        var sequenceNode = new SequenceDataNode();

        foreach (var val in obj)
        {
            var serializedVal = WriteValue(val.GetType(), val, alwaysWrite, context);
            sequenceNode.Add(serializedVal);
        }

        return sequenceNode;
    }

    private MappingDataNode WriteValueInternal<T>(
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

    public DataNode WriteValue<T>(T value, bool alwaysWrite = false, ISerializationContext? context = null)
    {
        return GetOrCreateWriteGenericDelegate(value)(value, alwaysWrite, context);
    }

    public DataNode WriteValue<T>(ITypeWriter<T> writer, T value, bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        if (value == null) return NullNode();

        return writer.Write(this, value, DependencyCollection, alwaysWrite, context);
    }

    public DataNode WriteValue<T, TWriter>(T value, bool alwaysWrite = false, ISerializationContext? context = null)
        where TWriter : ITypeWriter<T>
    {
        return WriteValue(GetOrCreateCustomTypeSerializer<TWriter>(), value, alwaysWrite, context);
    }

    public DataNode WriteValue(object? value, bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        if (value == null) return NullNode();

        return WriteValue(value.GetType(), value, alwaysWrite, context);
    }

    public DataNode WriteValue(Type type, object? value, bool alwaysWrite = false, ISerializationContext? context = null)
    {
        if (value == null) return NullNode();

        return GetOrCreateWriteBoxingDelegate(type)(value, alwaysWrite, context);
    }
}
