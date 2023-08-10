using Robust.Shared.GameObjects;

namespace Robust.Server.GameObjects;

public sealed partial class ServerEntityManager
{
    protected override void SpawnEntityArch(EntityUid uid, MetaDataComponent metadata)
    {
        var entity = World.Create(DefaultArchetype);
        World.Set(entity, metadata);
    }
}
