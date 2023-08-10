using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

public partial class EntityManager
{
    private const int PoolSize = 256;

    /// <summary>
    /// Inverse lookup for net entities.
    /// Regular lookup uses MetadataComponent.
    /// </summary>
    protected readonly Dictionary<NetEntity, EntityUid> NetEntityLookup = new();

    private readonly ObjectPool<HashSet<EntityUid>> _entitySetPool =
        new DefaultObjectPool<HashSet<EntityUid>>(new SetPolicy<EntityUid>(), PoolSize);

    private readonly ObjectPool<HashSet<NetEntity>> _netEntitySetPool =
        new DefaultObjectPool<HashSet<NetEntity>>(new SetPolicy<NetEntity>(), PoolSize);


    public EntityUid ToEntity(NetEntity nEntity)
    {
        return NetEntityLookup.TryGetValue(nEntity, out var entity) ? entity : EntityUid.Invalid;
    }

    public NetEntity ToNetEntity(EntityUid uid, MetaDataComponent? metadata = null)
    {
        if (!_metaQuery.Resolve(uid, ref metadata))
        {
            return NetEntity.Invalid;
        }

        return metadata.NetEntity;
    }

    #region Helpers

    public HashSet<EntityUid> ToEntityUids(HashSet<NetEntity> netEntities)
    {
        var entities = _entitySetPool.Get();
        entities.EnsureCapacity(netEntities.Count);

        foreach (var netEntity in netEntities)
        {
            entities.Add(ToEntity(netEntity));
        }

        return entities;
    }

    /// <summary>
    /// Returns the <see cref="NetEntity"/> to a HashSet of <see cref="EntityUid"/>
    /// </summary>
    public HashSet<NetEntity> ToNetEntities(HashSet<EntityUid> entities)
    {
        var newSet = _netEntitySetPool.Get();
        newSet.EnsureCapacity(entities.Count);

        foreach (var ent in entities)
        {
            _metaQuery.TryGetComponent(ent, out var metadata);
            newSet.Add(ToNetEntity(ent, metadata));
        }

        return newSet;
    }

    #endregion
}
