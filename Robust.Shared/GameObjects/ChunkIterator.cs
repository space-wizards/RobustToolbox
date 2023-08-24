using Arch.Core;
using Collections.Pooled;

namespace Robust.Shared.GameObjects;

internal struct ArchChunkIterator
{
    private readonly Query _query;
    private readonly PooledList<Archetype> _archetypes;

    internal ArchChunkIterator(in Query query, PooledList<Archetype> archetypes)
    {
        _query = query;
        _archetypes = archetypes;
    }

    public ArchChunkEnumerator GetEnumerator()
    {
        return new ArchChunkEnumerator(_query, _archetypes);
    }
}

internal struct ArchChunkEnumerator
{
    private readonly Query _query;
    private PooledList<Archetype>.Enumerator _archetypes;
    private int _chunkIndex;
    public Chunk Current { get; private set; }

    internal ArchChunkEnumerator(in Query query, PooledList<Archetype> archetypes)
    {
        _query = query;
        _archetypes = archetypes.GetEnumerator();
    }

    private bool NextArchetype()
    {
        while (_archetypes.MoveNext())
        {
            var archetype = _archetypes.Current;
            if (archetype.Size > 0 && _query.Valid(archetype.BitSet))
                return true;
        }

        return false;
    }

    public bool MoveNext()
    {
        if (_archetypes.Current == null! || _chunkIndex >= _archetypes.Current.Size)
        {
            while (_archetypes.Current == null || _chunkIndex >= _archetypes.Current.Size)
            {
                if (!NextArchetype())
                    return false;
            }

            _chunkIndex = 0;
        }

        Current = _archetypes.Current.Chunks[_chunkIndex];
        _chunkIndex++;
        return true;
    }
}

internal static partial class QueryExtensions
{
    internal static ArchChunkIterator ChunkIterator(this in Query query, World world)
    {
        return new ArchChunkIterator(query, world.Archetypes!);
    }
}
