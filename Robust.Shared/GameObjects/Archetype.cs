using System;
using System.Collections.Generic;

namespace Robust.Shared.GameObjects;

/// <summary>
/// Every entity with exactly these components shares an archetype.
/// </summary>
internal sealed class Archetype
{
    // Will use Arch at some point; this is just a lightweight one so we can nuke EntityEventTable without a larger query migration.

    /// <summary>
    /// Bit per <see cref="CompIdx.Value"/>.
    /// </summary>
    public readonly ulong[] Mask;

    /// <summary>
    /// The component types in order.
    /// </summary>
    public readonly CompIdx[] Components;

    /// <summary>
    ///<see cref="CompIdx.Value"/> -> archetype index.
    /// </summary>
    public readonly Dictionary<int, int> AddEdges = new();
    public readonly Dictionary<int, int> RemoveEdges = new();

    public Archetype(ulong[] mask)
    {
        Mask = mask;

        var components = new List<CompIdx>();
        for (var word = 0; word < mask.Length; word++)
        {
            var bits = mask[word];
            while (bits != 0)
            {
                components.Add(new CompIdx(word * 64 + System.Numerics.BitOperations.TrailingZeroCount(bits)));
                bits &= bits - 1;
            }
        }

        Components = components.ToArray();
    }

    public bool Has(CompIdx idx)
    {
        var word = idx.Value >> 6;
        return word < Mask.Length && (Mask[word] & (1UL << (idx.Value & 63))) != 0;
    }

    /// <summary>
    /// Returns a trimmed copy of <paramref name="mask"/> with <paramref name="idx"/> set or cleared.
    /// </summary>
    public static ulong[] WithBit(ulong[] mask, CompIdx idx, bool set)
    {
        var word = idx.Value >> 6;
        var result = new ulong[Math.Max(mask.Length, set ? word + 1 : 0)];
        mask.AsSpan().CopyTo(result);

        if (set)
            result[word] |= 1UL << (idx.Value & 63);
        else if (word < result.Length)
            result[word] &= ~(1UL << (idx.Value & 63));

        var length = result.Length;
        while (length > 0 && result[length - 1] == 0)
        {
            length--;
        }

        return length == result.Length ? result : result[..length];
    }
}

internal sealed class ArchetypeMaskComparer : IEqualityComparer<ulong[]>
{
    public static readonly ArchetypeMaskComparer Instance = new();

    public bool Equals(ulong[]? x, ulong[]? y) => x.AsSpan().SequenceEqual(y);

    public int GetHashCode(ulong[] obj)
    {
        var hash = new HashCode();
        foreach (var word in obj)
        {
            hash.Add(word);
        }

        return hash.ToHashCode();
    }
}
