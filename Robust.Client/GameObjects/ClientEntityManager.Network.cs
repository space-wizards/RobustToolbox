using Robust.Shared.GameObjects;

namespace Robust.Client.GameObjects;

public sealed partial class ClientEntityManager
{
    /// <summary>
    /// Clientside ents never get valid NetEntities.
    /// </summary>
    protected override NetEntity GenerateNetEntity() => NetEntity.Invalid;

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
}
