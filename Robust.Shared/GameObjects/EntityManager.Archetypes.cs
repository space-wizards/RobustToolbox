using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Arch.LowLevel.Jagged;

namespace Robust.Shared.GameObjects;

public abstract partial class EntityManager
{
    /*
     * Arch when
     */

    private const int ArchetypeBucketSize = 256;

    internal const int NoArchetype = -1;

    /// <summary>
    /// Archetype index of each entity, by <see cref="EntityUid.Id"/>.
    /// </summary>
    private readonly JaggedArray<int> _entArchetypes = new(ArchetypeBucketSize, NoArchetype, ArchetypeBucketSize);

    /// <summary>
    /// Archetypes by index. Index 0 is the empty archetype.
    /// </summary>
    private readonly JaggedArray<Archetype> _archetypes = new(ArchetypeBucketSize, ArchetypeBucketSize);

    internal int ArchetypeCount { get; private set; }

    private readonly Dictionary<ulong[], int> _archetypeLookup = new(ArchetypeMaskComparer.Instance);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Archetype GetArchetype(int index) => _archetypes[index];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int GetArchetypeIndex(EntityUid uid)
    {
        return uid.Id < _entArchetypes.Capacity ? _entArchetypes[uid.Id] : NoArchetype;
    }

    /// <summary>
    /// Component lookup for event dispatch.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryGetComponentForEvent(EntityUid uid, CompIdx idx, int denseIndex, [NotNullWhen(true)] out IComponent? component)
    {
        if (denseIndex >= 0)
        {
            _entTraitJaggedArray[denseIndex].TryGetValue(uid.Id, out IComponent? dense);
            component = dense is { Deleted: false } ? dense : null;
            return component != null;
        }

        return _entTraitArray[idx.Value].TryGetValue(uid, out component) && !component.Deleted;
    }

    /// <summary>
    /// The entity's components in <see cref="CompIdx"/> order.
    /// </summary>
    internal IComponent[] GetComponentsSnapshot(EntityUid uid)
    {
        var archetype = GetArchetypeIndex(uid);
        if (archetype == NoArchetype)
            return [];

        var types = _archetypes[archetype].Components;
        var result = new IComponent[types.Length];
        var count = 0;
        foreach (var idx in types)
        {
            if (_entTraitArray[idx.Value].TryGetValue(uid, out var comp))
                result[count++] = comp;
        }

        return count == result.Length ? result : result[..count];
    }

    internal void ArchetypeAddEntity(EntityUid uid)
    {
        if (ArchetypeCount == 0)
            InternArchetype([]);

        _entArchetypes.EnsureCapacity(uid.Id + 1);
        _entArchetypes[uid.Id] = 0;
    }

    internal void ArchetypeRemoveEntity(EntityUid uid)
    {
        if (uid.Id < _entArchetypes.Capacity)
            _entArchetypes[uid.Id] = NoArchetype;
    }

    internal void ArchetypeAddComponent(EntityUid uid, CompIdx idx)
    {
        var current = GetArchetypeIndex(uid);
        if (current == NoArchetype)
            return;

        var archetype = _archetypes[current];
        if (!archetype.AddEdges.TryGetValue(idx.Value, out var next))
        {
            next = archetype.Has(idx) ? current : InternArchetype(Archetype.WithBit(archetype.Mask, idx, true));
            archetype.AddEdges[idx.Value] = next;
        }

        _entArchetypes[uid.Id] = next;
    }

    internal void ArchetypeRemoveComponent(EntityUid uid, CompIdx idx)
    {
        var current = GetArchetypeIndex(uid);
        if (current == NoArchetype)
            return;

        var archetype = _archetypes[current];
        if (!archetype.RemoveEdges.TryGetValue(idx.Value, out var next))
        {
            next = archetype.Has(idx) ? InternArchetype(Archetype.WithBit(archetype.Mask, idx, false)) : current;
            archetype.RemoveEdges[idx.Value] = next;
        }

        _entArchetypes[uid.Id] = next;
    }

    private int InternArchetype(ulong[] mask)
    {
        if (_archetypeLookup.TryGetValue(mask, out var existing))
            return existing;

        var index = ArchetypeCount++;
        _archetypes.EnsureCapacity(index + 1);
        _archetypes[index] = new Archetype(mask);
        _archetypeLookup.Add(mask, index);
        return index;
    }
}
