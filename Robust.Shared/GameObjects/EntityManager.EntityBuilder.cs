using System;
using System.Runtime.CompilerServices;
using Robust.Shared.GameObjects.EntityBuilders;

namespace Robust.Shared.GameObjects;

public abstract partial class EntityManager
{
    public EntityUid Spawn(EntityBuilder builder, bool mapInit = true)
    {
        var ent = builder.ReservedEntity;
        // Doesn't allocate. Not that it matters, we're about to allocate a lot.
        SpawnBulk([builder]);
        return ent;
    }

    public void SpawnBulk(ReadOnlySpan<EntityBuilder> builders, bool mapInit = true)
    {
        // Create entities + ComponentAdd
        foreach (var builder in builders)
        {
            AllocBuilderEntity(builder);
        }

        // ComponentInit
        foreach (var builder in builders)
        {
            InitializeEntity(builder.ReservedEntity, builder.MetaData);
        }

        // ComponentStartup
        foreach (var builder in builders)
        {
            StartEntity(builder.ReservedEntity);
        }

        // MapInit
        if (mapInit)
        {
            foreach (var builder in builders)
            {
                RunMapInit(builder.ReservedEntity, builder.MetaData);
            }
        }
    }
}
