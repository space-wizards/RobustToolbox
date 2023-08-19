using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Utils;

namespace Robust.Shared.GameObjects;

public partial class EntityManager
{
    private World _world = default!;
    protected ComponentType[] DefaultArchetype = new ComponentType[] { typeof(MetaDataComponent) };

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
        _world.Destroy(uid.ToArch());
    }

    private EntityUid SpawnEntityArch()
    {
        var entity = _world.Create(DefaultArchetype);
        return EntityUid.FromArch(entity);
    }

    private void CleanupArch()
    {
        _world.TrimExcess();
    }
}
