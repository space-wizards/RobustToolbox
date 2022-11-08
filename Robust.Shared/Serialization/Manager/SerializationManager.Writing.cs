using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq.Expressions;
using Robust.Shared.Serialization.Manager.Definition;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.Manager;

public sealed partial class SerializationManager
{
    #region DelegateElements

    private delegate DataNode WriteDelegate(
        object obj,
        bool alwaysWrite,
        ISerializationContext? context);

    private readonly ConcurrentDictionary<Type, WriteDelegate> _writerDelegates = new();

    private WriteDelegate GetOrCreateWriteDelegate(Type type)
    {
        type = type.EnsureNotNullableType();

        return _writerDelegates
            .GetOrAdd(type, static (t, manager) =>
            {
                var instanceParam = Expression.Constant(manager);
                var objParam = Expression.Parameter(typeof(object), "obj");
                var alwaysWriteParam = Expression.Parameter(typeof(bool), "alwaysWrite");
                var contextParam = Expression.Parameter(typeof(ISerializationContext), "context");

                Expression call;
                if (manager._regularSerializerProvider.TryGetTypeSerializer(typeof(ITypeWriter<>), t, out var serializer))
                {
                    var serializerConst = Expression.Constant(serializer);
                    var depCollection = Expression.Constant(manager.DependencyCollection);
                    call = Expression.Call(
                        serializerConst,
                        typeof(ITypeWriter<>).MakeGenericType(t).GetMethod("Write")!,
                        instanceParam,
                        Expression.Convert(objParam, t),
                        depCollection,
                        alwaysWriteParam,
                        contextParam
                    );

                }
                else if (t.IsEnum)
                {
                    // Enums implement IConvertible.
                    // Need it for the culture overload.
                    call = Expression.Call(
                        instanceParam,
                        nameof(WriteConvertible),
                        Type.EmptyTypes,
                        Expression.Convert(objParam, typeof(IConvertible)));
                }
                else if (t.IsArray)
                {
                    call = Expression.Call(
                        instanceParam,
                        nameof(WriteArray),
                        Type.EmptyTypes,
                        Expression.Convert(objParam, typeof(Array)),
                        alwaysWriteParam,
                        contextParam);
                }
                else if (typeof(ISelfSerialize).IsAssignableFrom(t))
                {
                    call = Expression.Call(
                        instanceParam,
                        nameof(WriteSelfSerializable),
                        Type.EmptyTypes,
                        Expression.Convert(objParam, typeof(ISelfSerialize)));
                }
                else
                {

                    if (t.IsAbstract || t.IsInterface)
                    {
                        call = Expression.Call(
                            instanceParam,
                            nameof(WriteValue),
                            Type.EmptyTypes,
                            Expression.Convert(objParam, typeof(object)),
                            alwaysWriteParam,
                            contextParam);
                    }
                    else
                    {
                        call = Expression.Call(
                            instanceParam,
                            nameof(WriteValueInternal),
                            new[] { t },
                            Expression.Convert(objParam, t),
                            Expression.Constant(manager.GetDefinition(t), typeof(DataDefinition<>).MakeGenericType(t)),
                            alwaysWriteParam,
                            contextParam);
                    }

                }

                return Expression.Lambda<WriteDelegate>(
                    call,
                    objParam,
                    alwaysWriteParam,
                    contextParam).Compile();
            }, this);
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

    private DataNode WriteValueInternal<T>(
        T value,
        DataDefinition<T>? definition,
        bool alwaysWrite,
        ISerializationContext? context)
        where T : notnull
    {
        if(context != null && context.SerializerProvider.TryGetTypeSerializer<ITypeWriter<T>, T>(out var writer))
        {
            return writer.Write(this, (T)value, DependencyCollection, alwaysWrite, context);
        }

        if (definition == null)
        {
            throw new InvalidOperationException($"No data definition found for type {typeof(T)} when writing");
        }

        var mapping = definition.Serialize(value, context, alwaysWrite);
        if (typeof(T).IsAbstract || typeof(T).IsInterface)
        {
            mapping.Tag = $"!type:{value.GetType().Name}";
        }

        return mapping;
    }

    #endregion

    public static ValueDataNode NullNode() => new ValueDataNode("null");

    public DataNode WriteValue<T>(T value, bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        return WriteValue(typeof(T), value, alwaysWrite, context);
    }

    public DataNode WriteValue(Type type, object? value, bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        if (value == null) return NullNode();

        return GetOrCreateWriteDelegate(underlyingType)(value, alwaysWrite, context);
    }

    public DataNode WriteValue(object? value, bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        if (value == null) return NullNode();

        return WriteValue(value.GetType(), alwaysWrite, context);
    }
}
