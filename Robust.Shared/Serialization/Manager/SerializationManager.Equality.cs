using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Robust.Shared.Serialization.Manager.Definition;

namespace Robust.Shared.Serialization.Manager;

public sealed partial class SerializationManager
{
    private delegate bool EqualityBoxingDelegate(
        object left,
        object right,
        ISerializationContext? context);

    private readonly ConcurrentDictionary<Type, EqualityBoxingDelegate> _equalityBoxingDelegates = new();
    private readonly ConcurrentDictionary<Type, EqualityBoxingDelegate> _hashSetEqualityBoxingDelegates = new();

    public bool DataFieldEquals<T>(T left, T right, ISerializationContext? context = null)
    {
        if (EqualityComparer<T>.Default.Equals(left, right))
            return true;

        if (left is null || right is null)
            return false;

        var type = typeof(T);
        var leftType = left.GetType();
        if (leftType != right.GetType())
            return false;

        if (type.IsAssignableTo(typeof(ISerializationGenerated<>).MakeGenericType(type)))
            return GetOrCreateEqualityBoxingDelegate(type)(left, right, context);

        if (leftType.IsAssignableTo(typeof(ISerializationGenerated<>).MakeGenericType(leftType)))
            return GetOrCreateEqualityBoxingDelegate(leftType)(left, right, context);

        if (left is Array leftArray && right is Array rightArray)
            return ArrayEquals(leftArray, rightArray, context);

        if (left is IDictionary leftDictionary && right is IDictionary rightDictionary)
            return DictionaryEquals(leftDictionary, rightDictionary, context);

        if (leftType.IsGenericType &&
            leftType.GetGenericTypeDefinition() == typeof(HashSet<>))
        {
            return GetOrCreateHashSetEqualityBoxingDelegate(leftType)(left, right, context);
        }

        if (left is IEnumerable leftEnumerable &&
            right is IEnumerable rightEnumerable &&
            left is not string)
        {
            return EnumerableEquals(leftEnumerable, rightEnumerable, context);
        }

        return false;
    }

    private EqualityBoxingDelegate GetOrCreateHashSetEqualityBoxingDelegate(Type type)
    {
        return _hashSetEqualityBoxingDelegates.GetOrAdd(type, type =>
        {
            var method = typeof(SerializationManager)
                .GetMethod(nameof(HashSetEqualsGeneric), BindingFlags.Instance | BindingFlags.NonPublic)!
                .MakeGenericMethod(type.GetGenericArguments()[0]);

            return method.CreateDelegate<EqualityBoxingDelegate>(this);
        });
    }

    private bool HashSetEqualsGeneric<T>(object left, object right, ISerializationContext? context)
    {
        return ((HashSet<T>) left).SetEquals((HashSet<T>) right);
    }

    public bool DataFieldEquals(Type type, object? left, object? right, ISerializationContext? context = null)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left is null || right is null)
            return false;

        if (left.GetType() != type || right.GetType() != type)
            return false;

        return GetOrCreateEqualityBoxingDelegate(type)(left, right, context);
    }

    private EqualityBoxingDelegate GetOrCreateEqualityBoxingDelegate(Type type)
    {
        return _equalityBoxingDelegates.GetOrAdd(type, type =>
        {
            var methodName = type.IsAssignableTo(typeof(ISerializationGenerated<>).MakeGenericType(type))
                ? nameof(DataDefinitionEqualsGeneric)
                : nameof(DataFieldEqualsGeneric);

            var method = typeof(SerializationManager)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
                .MakeGenericMethod(type);

            return method.CreateDelegate<EqualityBoxingDelegate>(this);
        });
    }

    private bool DataFieldEqualsGeneric<T>(object left, object right, ISerializationContext? context)
    {
        return DataFieldEquals((T) left, (T) right, context);
    }

    private bool DataDefinitionEqualsGeneric<T>(object left, object right, ISerializationContext? context)
        where T : ISerializationGenerated<T>
    {
        return GetDefinition<T>()!.AreEqual((T) left, (T) right, this, context);
    }

    private bool ArrayEquals(Array left, Array right, ISerializationContext? context)
    {
        if (left.Rank != right.Rank || left.Length != right.Length)
            return false;

        for (var i = 0; i < left.Rank; i++)
        {
            if (left.GetLength(i) != right.GetLength(i))
                return false;
        }

        var leftEnumerator = left.GetEnumerator();
        var rightEnumerator = right.GetEnumerator();
        while (leftEnumerator.MoveNext())
        {
            rightEnumerator.MoveNext();
            if (!ObjectFieldEquals(leftEnumerator.Current, rightEnumerator.Current, context))
                return false;
        }

        return true;
    }

    private bool DictionaryEquals(IDictionary left, IDictionary right, ISerializationContext? context)
    {
        if (left.Count != right.Count)
            return false;

        foreach (DictionaryEntry entry in left)
        {
            if (!right.Contains(entry.Key))
                return false;

            if (!ObjectFieldEquals(entry.Value, right[entry.Key], context))
                return false;
        }

        return true;
    }

    private bool EnumerableEquals(IEnumerable left, IEnumerable right, ISerializationContext? context)
    {
        var leftEnumerator = left.GetEnumerator();
        var rightEnumerator = right.GetEnumerator();

        while (true)
        {
            var leftHasValue = leftEnumerator.MoveNext();
            var rightHasValue = rightEnumerator.MoveNext();

            if (leftHasValue != rightHasValue)
                return false;

            if (!leftHasValue)
                return true;

            if (!ObjectFieldEquals(leftEnumerator.Current, rightEnumerator.Current, context))
                return false;
        }
    }

    private bool ObjectFieldEquals(object? left, object? right, ISerializationContext? context)
    {
        if (Equals(left, right))
            return true;

        if (left is null || right is null)
            return false;

        var type = left.GetType();
        if (type != right.GetType())
            return false;

        return GetOrCreateEqualityBoxingDelegate(type)(left, right, context);
    }
}
