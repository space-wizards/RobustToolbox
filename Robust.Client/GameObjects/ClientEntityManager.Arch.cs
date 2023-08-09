using System.Collections.Generic;
using Arch.Core;
using Robust.Shared.GameObjects;

namespace Robust.Client.GameObjects;

public sealed partial class ClientEntityManager
{
    private World _clientWorld = default!;
    // End me
    private Dictionary<int, int> _archMap = new();

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

    protected override void SpawnEntityArch(EntityUid uid)
    {
        if (uid.IsClientSide())
        {
            var normalised = uid.GetArchId() & ~EntityUid.ClientUid;
            var entity = _clientWorld.Create();
            _archMap[normalised] = entity.Id;
        }
        else
        {
            World.Create(uid.GetHashCode());
        }
    }

    protected override void DestroyArch(EntityUid uid)
    {
        if (uid.IsClientSide())
        {
            var entity = new Entity(_archMap[uid.GetArchId() & ~EntityUid.ClientUid]);
            _clientWorld.Destroy(entity);
            return;
        }

        base.DestroyArch(uid);
    }
}
