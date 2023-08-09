using System.Collections.Generic;
using Arch.Core;
using Robust.Shared.GameObjects;

namespace Robust.Client.GameObjects;

public sealed partial class ClientEntityManager
{
    private World _clientWorld = default!;
    // End me
    private Dictionary<int, EntityReference> _archMap = new();

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
            var entity = _clientWorld.Create();
            var refo = _clientWorld.Reference(entity);
            _archMap[uid.GetHashCode()] = refo;
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
            _clientWorld.Destroy(_archMap[uid.GetHashCode()].Entity);
            _archMap.Remove(uid.GetHashCode());
            return;
        }

        base.DestroyArch(uid);
    }
}
