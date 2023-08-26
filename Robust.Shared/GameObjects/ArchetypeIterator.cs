using Arch.Core;
using Collections.Pooled;

namespace Robust.Shared.GameObjects;

internal struct ArchetypeIterator
{
    private readonly Query _query;
    private readonly PooledList<Archetype> _archetypes;

    internal ArchetypeIterator(in Query query, PooledList<Archetype> archetypes)
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
    private readonly PooledList<Archetype> _archetypes;
    private int _index;

    public ArchetypeEnumerator(in Query query, PooledList<Archetype> archetypes)
    {
        _query = query;
        _archetypes = archetypes;
    }

    public bool MoveNext()
    {
        while (++_index < _archetypes.Count)
        {
            var archetype = Current;
            if (archetype.Entities > 0 && _query.Valid(archetype.BitSet))
            {
                return true;
            }
        }

        return false;
    }

    public Archetype Current => _archetypes[_index];
}
