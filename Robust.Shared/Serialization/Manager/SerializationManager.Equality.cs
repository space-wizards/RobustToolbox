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

    public bool DataFieldEquals<T>(T left, T right, ISerializationContext? context = null)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left is null || right is null)
            return false;

        var leftType = left.GetType();
        if (leftType != right.GetType())
            return false;

        // Primitive, enum, string, Type and CopyByRef values have no structural
        // equality to fall back to.
        if (leftType == typeof(T) && SerializedType<T>.Information.ReturnSource)
            return EqualityComparer<T>.Default.Equals(left, right);

        return GetOrCreateEqualityBoxingDelegate(leftType)(left, right, context);
    }

    private bool HashSetEqualsGeneric<T>(object left, object right, ISerializationContext? _)
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
        return _equalityBoxingDelegates.GetOrAdd(type, static (type, manager) =>
        {
            if (type.IsAssignableTo(typeof(ISerializationGenerated<>).MakeGenericType(type)))
                return manager.CreateGenericEqualityDelegate(type, nameof(DataDefinitionEqualsGeneric));

            if (type.IsArray)
                return manager.ArrayEqualsBoxed;

            if (type.IsAssignableTo(typeof(IDictionary)))
                return manager.DictionaryEqualsBoxed;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(HashSet<>))
                return manager.CreateGenericEqualityDelegate(type.GetGenericArguments()[0], nameof(HashSetEqualsGeneric));

            if (type != typeof(string) && type.IsAssignableTo(typeof(IEnumerable)))
                return manager.EnumerableEqualsBoxed;

            return static (left, right, _) => Equals(left, right);
        }, this);
    }

    private EqualityBoxingDelegate CreateGenericEqualityDelegate(Type type, string methodName)
    {
        var method = typeof(SerializationManager)
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
            .MakeGenericMethod(type);

        return method.CreateDelegate<EqualityBoxingDelegate>(this);
    }

    private bool ArrayEqualsBoxed(object left, object right, ISerializationContext? context)
    {
        return ArrayEquals((Array) left, (Array) right, context);
    }

    private bool DictionaryEqualsBoxed(object left, object right, ISerializationContext? context)
    {
        return DictionaryEquals((IDictionary) left, (IDictionary) right, context);
    }

    private bool EnumerableEqualsBoxed(object left, object right, ISerializationContext? context)
    {
        return EnumerableEquals((IEnumerable) left, (IEnumerable) right, context);
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
        using var leftDisposable = leftEnumerator as IDisposable;
        using var rightDisposable = rightEnumerator as IDisposable;
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
        using var leftDisposable = leftEnumerator as IDisposable;
        using var rightDisposable = rightEnumerator as IDisposable;

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
        if (ReferenceEquals(left, right))
            return true;

        if (left is null || right is null)
            return false;

        var type = left.GetType();
        if (type != right.GetType())
            return false;

        return GetOrCreateEqualityBoxingDelegate(type)(left, right, context);
    }
}
