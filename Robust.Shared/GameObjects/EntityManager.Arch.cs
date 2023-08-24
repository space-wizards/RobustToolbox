using System;
using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Utils;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

public partial class EntityManager
{
    private World _world = default!;
    protected ComponentType[] DefaultArchetype = { typeof(MetaDataComponent) };

    protected void InitializeArch()
    {
        _world = World.Create();
    }

    protected void ShutdownArch()
    {
        World.Destroy(_world);
    }

    protected void DestroyArch(EntityUid uid)
    {
        var archEnt = new Entity(uid.GetArchId());
        var reference = _world.Reference(in archEnt);

        if (reference.Version != (uid.Version - EntityUid.ArchVersionOffset))
        {
            throw new InvalidOperationException($"Tried to delete a different matching entity for Arch.");
        }

        _world.Destroy(archEnt);
    }

    private void SpawnEntityArch(out EntityUid entity, out MetaDataComponent metadata)
    {
        metadata = new MetaDataComponent();
        var archEnt = _world.Create(metadata);
        var reference = _world.Reference(archEnt);
        entity = new EntityUid(reference);
    }

    private void CleanupArch()
    {
        _world.TrimExcess();
    }
}
