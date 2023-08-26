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
            _chunkIndex = _archetypes.Current.Size;
        }
    }

    public bool MoveNext()
    {
        if (--_chunkIndex >= 0 && Current.Size > 0)
        {
            return true;
        }

        if (!_archetypes.MoveNext())
        {
            return false;
        }

        _chunkIndex = _archetypes.Current.Size - 1;
        return true;
    }
}

internal static partial class QueryExtensions
{
    internal static ArchChunkIterator ChunkIterator(this in Query query, World world)
    {
        var archetypeEnumerator = new ArchetypeEnumerator(in query, world.Archetypes);
        return new ArchChunkIterator(in archetypeEnumerator);
    }
}
