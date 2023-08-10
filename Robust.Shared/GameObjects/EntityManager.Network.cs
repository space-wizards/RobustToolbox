using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

public partial class EntityManager
{
    private const int PoolSize = 256;

    private readonly Dictionary<NetEntity, EntityUid> _nentityLookup = new();

    private readonly ObjectPool<HashSet<NetEntity>> _netEntitySetPool =
        new DefaultObjectPool<HashSet<NetEntity>>(new SetPolicy<NetEntity>(), PoolSize);


    public EntityUid ToEntity(NetEntity nEntity)
    {
        if (_nentityLookup.TryGetValue(nEntity, out var entity))
            return entity;

        return EntityUid.Invalid;
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
            newSet.Add(ToNetEntity(ent));
        }

        return newSet;
    }

    #endregion
}
