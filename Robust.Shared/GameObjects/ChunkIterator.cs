using Arch.Core;

namespace Robust.Shared.GameObjects;

internal struct ArchChunkIterator
{
    private readonly ArchetypeEnumerator _archetypes;

    internal ArchChunkIterator(in ArchetypeEnumerator archetypes)
    {
        _archetypes = archetypes;
    }

    public ArchChunkEnumerator GetEnumerator()
    {
        return new ArchChunkEnumerator(_archetypes);
    }
}

internal struct ArchChunkEnumerator
{
    private ArchetypeEnumerator _archetypes;
    private int _chunkIndex;
    public Chunk Current => _archetypes.Current.GetChunk(_chunkIndex);

    internal ArchChunkEnumerator(in ArchetypeEnumerator archetypes)
    {
        _archetypes = archetypes;

        if (_archetypes.MoveNext())
        {
            _chunkIndex = _archetypes.Current.ChunkCount;
        }
    }

    public bool MoveNext()
    {
        if (--_chunkIndex >= 0 && Current.Count > 0)
        {
            return true;
        }

        if (!_archetypes.MoveNext())
        {
            return false;
        }

        _chunkIndex = _archetypes.Current.ChunkCount - 1;
        return true;
    }
}

internal static partial class QueryExtensions
{
    internal static ArchChunkIterator ChunkIterator(this Query query, World world)
    {
        query.Match();
        var enumerator = new ArchetypeEnumerator(query.GetMatches());
        return new ArchChunkIterator(in enumerator);
    }
}
