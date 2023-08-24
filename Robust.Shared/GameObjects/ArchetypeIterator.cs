using Arch.Core;
using Collections.Pooled;

namespace Robust.Shared.GameObjects;

internal struct ArchetypeIterator
{
    private readonly Query _query;
    private readonly PooledList<Archetype>.Enumerator _archetypes;

    internal ArchetypeIterator(in Query query, PooledList<Archetype>.Enumerator archetypes)
    {
        _query = query;
        _archetypes = archetypes;
    }

    public ArchetypeEnumerator GetEnumerator()
    {
        return new ArchetypeEnumerator(_query, _archetypes);
    }
}

internal struct ArchetypeEnumerator
{
    private readonly Query _query;
    private PooledList<Archetype>.Enumerator _archetypes;
    public Archetype Current { get; private set; } = default!;

    public ArchetypeEnumerator(in Query query, PooledList<Archetype>.Enumerator archetypes)
    {
        _query = query;
        _archetypes = archetypes;
    }

    public bool MoveNext()
    {
        while (_archetypes.MoveNext())
        {
            var archetype = _archetypes.Current;
            if (archetype.Size > 0 && _query.Valid(archetype.BitSet))
                return true;
        }

        return false;
    }
}
