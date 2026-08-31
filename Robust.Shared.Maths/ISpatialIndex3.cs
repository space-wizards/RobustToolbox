using System.Collections.Generic;

namespace Robust.Shared.Maths;

/// <summary>
/// Broadphase contract for objects bounded in three-dimensional space.
/// </summary>
public interface ISpatialIndex3<T> where T : notnull
{
    int Count { get; }

    void Add(T item, Box3 bounds);

    void Update(T item, Box3 bounds);

    bool Remove(T item);

    bool TryGetBounds(T item, out Box3 bounds);

    void Query(Box3 bounds, ICollection<T> results);
}
