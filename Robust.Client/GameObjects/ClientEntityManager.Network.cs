using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Robust.Client.GameObjects;

public sealed partial class ClientEntityManager
{
    protected override NetEntity GenerateNetEntity() => new(NextNetworkId++ | NetEntity.ClientEntity);

    /// <summary>
    /// If the client fails to resolve a NetEntity then during component state handling or the likes we
    /// flag that comp state as requiring re-running if that NetEntity comes in.
    /// </summary>
    /// <returns></returns>
    internal readonly Dictionary<NetEntity, List<(Type type, EntityUid Owner)>> PendingNetEntityStates = new();

    public override bool IsClientSide(EntityUid uid, MetaDataComponent? metadata = null)
    {
        // Can't log false because some content code relies on invalid UIDs.
        if (!MetaQuery.Resolve(uid, ref metadata, false))
            return false;

        return metadata.NetEntity.IsClientSide();
    }

    public override EntityUid EnsureEntity<T>(NetEntity nEntity, EntityUid callerEntity)
    {
        if (!nEntity.Valid)
        {
            return EntityUid.Invalid;
        }

        if (NetEntityLookup.TryGetValue(nEntity, out var entity))
        {
            return entity.Item1;
        }

        // Flag the callerEntity to have their state potentially re-run later.
        var pending = PendingNetEntityStates.GetOrNew(nEntity);
        pending.Add((typeof(T), callerEntity));



        return entity.Item1;
    }

    public override EntityCoordinates EnsureCoordinates<T>(NetCoordinates netCoordinates, EntityUid callerEntity)
    {
        var entity = EnsureEntity<T>(netCoordinates.NetEntity, callerEntity);
        return new EntityCoordinates(entity, netCoordinates.Position);
    }
}
