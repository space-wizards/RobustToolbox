using System;
using System.Collections.Generic;
using Arch.Core;
using Robust.Shared.GameObjects;
using Robust.Shared.Toolshed.TypeParsers;
using ComponentType = Arch.Core.Utils.ComponentType;

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

    protected override void SpawnEntityArch(EntityUid uid)
    {
        if (uid.IsClientSide())
        {
            _clientWorld.Create();
        }
        else
        {
            World.Create(uid.ToArch().Id);
        }
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
