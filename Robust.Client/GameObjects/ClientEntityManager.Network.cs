using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

namespace Robust.Client.GameObjects;

public sealed partial class ClientEntityManager
{
    // TODO: Generate clientside
    protected override NetEntity GenerateNetEntity() => NetEntity.Invalid;

    /// <summary>
    /// If the client fails to resolve a NetEntity then during component state handling or the likes we
    /// flag that comp state as requiring re-running if that NetEntity comes in.
    /// </summary>
    /// <returns></returns>
    internal readonly Dictionary<NetEntity, List<(Type type, EntityUid Owner)>> PendingNetEntityStates = new();

    /// <summary>
    /// Set the inverse lookup for a particular entityuid.
    /// </summary>
    public void SetNetEntity(EntityUid uid, NetEntity netEntity)
    {
        NetEntityLookup[netEntity] = uid;
    }

    public override bool IsClientSide(EntityUid uid, MetaDataComponent? metadata = null)
    {
        // Can't log false because some content code relies on invalid UIDs.
        if (!MetaQuery.Resolve(uid, ref metadata, false))
            return false;

        return !metadata.NetEntity.IsValid();
    }

    public override EntityUid EnsureEntity(NetEntity nEntity, Type type, EntityUid callerEntity)
    {
        if (!nEntity.Valid)
        {
            return EntityUid.Invalid;
        }

        if (NetEntityLookup.TryGetValue(nEntity, out var entity))
        {
            return entity;
        }

        // Spawn an entity and reserve it at this point.
        entity = Spawn();
        MetaQuery.GetComponent(entity).NetEntity = nEntity;
        NetEntityLookup[nEntity] = entity;

        var pending = PendingNetEntityStates.GetOrNew(nEntity);
        pending.Add((type, callerEntity));

        return entity;
    }
}
