using System.Collections.Generic;

namespace Robust.Shared.Collections;

/// <summary>
/// Ref struct version of <see cref="KeyValuePair{K, T}"/> which stores a ref to a stored value.
/// </summary>
public readonly ref struct KeyValueRef<TKey, TValue>(TKey key, ref TValue value)
{
    /// <summary>
    /// The key indexing the <see cref="Value"/>.
    /// </summary>
    public readonly TKey Key = key;

    /// <summary>
    /// The value indexed by the <see cref="Key"/>.
    /// </summary>
    public readonly ref TValue Value = ref value;

    public void Deconstruct(out TKey key, out TValue value)
    {
        key = Key;
        value = Value;
    }

    public override string ToString()
    {
        return $"Key: {Key}, Value: {Value}";
    }

    public static implicit operator KeyValuePair<TKey, TValue>(KeyValueRef<TKey, TValue> pair) => new(pair.Key, pair.Value);
}
