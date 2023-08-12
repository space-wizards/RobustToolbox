using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Microsoft.Extensions.ObjectPool;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

/// <summary>
/// Stores generic ObjectPools for re-use to avoid duplicating them across systems.
/// </summary>
public sealed class ObjectPoolManager
{
    private const int DefaultPoolSize = 1024;

    private readonly ObjectPool<HashSet<EntityUid>> _entitySetPool =
        new DefaultObjectPool<HashSet<EntityUid>>(new SetPolicy<EntityUid>(), DefaultPoolSize);

    private readonly ObjectPool<List<EntityUid>> _entityListPool =
        new DefaultObjectPool<List<EntityUid>>(new ListPolicy<EntityUid>(), DefaultPoolSize);

    private readonly ObjectPool<HashSet<NetEntity>> _netEntitySetPool =
        new DefaultObjectPool<HashSet<NetEntity>>(new SetPolicy<NetEntity>(), DefaultPoolSize);

    private readonly ObjectPool<List<NetEntity>> _netEntityListPool =
        new DefaultObjectPool<List<NetEntity>>(new ListPolicy<NetEntity>(), DefaultPoolSize);

    #region Entity Set

    [Pure]
    [PublicAPI]
    public HashSet<EntityUid> GetEntitySet() => _entitySetPool.Get();

    [Pure]
    [PublicAPI]
    public HashSet<EntityUid> GetEntitySet(int capacity)
    {
        var set = _entitySetPool.Get();
        set.EnsureCapacity(capacity);
        return set;
    }

    [PublicAPI]
    public void Return(HashSet<EntityUid> set) => _entitySetPool.Return(set);

    [Pure]
    [PublicAPI]
    public List<EntityUid> GetEntityList() => _entityListPool.Get();

    [Pure]
    [PublicAPI]
    public List<EntityUid> GetEntityList(int capacity)
    {
        var set = _entityListPool.Get();
        set.EnsureCapacity(capacity);
        return set;
    }

    [PublicAPI]
    public void Return(List<EntityUid>? list)
    {
        if (list == null)
            return;

        _entityListPool.Return(list);
    }

    #endregion

    #region NetEntity Set

    [Pure]
    [PublicAPI]
    public HashSet<NetEntity> GetNetEntitySet() => _netEntitySetPool.Get();

    [Pure]
    [PublicAPI]
    public HashSet<NetEntity> GetNetEntitySet(int capacity)
    {
        var set = _netEntitySetPool.Get();
        set.EnsureCapacity(capacity);
        return set;
    }

    [PublicAPI]
    public void Return(HashSet<NetEntity> set) => _netEntitySetPool.Return(set);

    [Pure]
    [PublicAPI]
    public List<NetEntity> GetNetEntityList() => _netEntityListPool.Get();

    [Pure]
    [PublicAPI]
    public List<NetEntity> GetNetEntityList(int capacity)
    {
        var set = _netEntityListPool.Get();
        set.EnsureCapacity(capacity);
        return set;
    }

    [PublicAPI]
    public void Return(List<NetEntity>? list)
    {
        if (list == null)
            return;

        _netEntityListPool.Return(list);
    }

    #endregion
}
