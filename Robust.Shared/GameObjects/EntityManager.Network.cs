using System.Collections.Generic;

namespace Robust.Shared.GameObjects;

public partial class EntityManager
{
    private readonly Dictionary<NetEntity, EntityUid> _nentityLookup = new();

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
}
