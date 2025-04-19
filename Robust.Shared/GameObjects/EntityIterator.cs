using Arch.Core;

namespace Robust.Shared.GameObjects;

internal struct EntityIterator
{
    private readonly Chunk _chunk;

    internal EntityIterator(in Chunk chunk)
    {
        _chunk = chunk;
    }

    public EntityEnumerator GetEnumerator()
    {
        return new EntityEnumerator(_chunk);
    }
}

internal struct EntityEnumerator
{
    private readonly Chunk _chunk;
    private int _entityIndex;
    public Entity Current { get; private set; }

    public EntityEnumerator(in Chunk chunk)
    {
        _chunk = chunk;
    }

    public bool MoveNext()
    {
        if (_entityIndex >= _chunk.Count)
            return false;

        Current = _chunk.Entity(_entityIndex);
        _entityIndex++;
        return true;
    }
}

internal static partial class QueryExtensions
{
    internal static EntityIterator ChunkIterator(this in Chunk chunk)
    {
        return new EntityIterator(chunk);
    }
}
