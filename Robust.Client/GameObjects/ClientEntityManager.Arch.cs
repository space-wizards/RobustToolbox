using Arch.Core;
using Robust.Shared.GameObjects;

namespace Robust.Client.GameObjects;

public sealed partial class ClientEntityManager
{
    private World _clientWorld = default!;

    protected override void InitializeArch()
    {
        base.InitializeArch();
        _clientWorld = World.Create();
    }

    protected override void ShutdownArch()
    {
        base.ShutdownArch();
        World.Destroy(_clientWorld);
    }

    protected override void SpawnEntityArch(EntityUid uid, MetaDataComponent metadata)
    {
        Entity entity;

        if (uid.IsClientSide())
        {
            entity = _clientWorld.Create(DefaultArchetype);
        }
        else
        {
            entity = World.Create(uid.ToArch().Id, DefaultArchetype);
        }

        World.Set(entity, metadata);
    }

    protected override void DestroyArch(EntityUid uid)
    {
        var entity = uid.ToArch();

        if (uid.IsClientSide())
        {
            _clientWorld.Destroy(entity);
            return;
        }

        // This is mainly here for client reasons
        if (World.IsAlive(entity))
        {
            World.Destroy(entity);
        }
    }
}
