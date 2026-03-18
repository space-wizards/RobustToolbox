using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Robust.Shared.GameObjects.EntityBuilders;

namespace Robust.Shared.GameObjects;

public abstract partial class EntityManager
{
    public EntityUid Spawn(EntityBuilder builder, bool? mapInit = null)
    {
        var ent = builder.ReservedEntity;
        // Doesn't allocate. Not that it matters, we're about to allocate a lot.
        SpawnBulk([builder]);
        return ent;
    }

    public void SpawnBulk(ReadOnlySpan<EntityBuilder> builders, bool? mapInit = null)
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

        foreach (var builder in builders)
        {
            var xform = TransformQuery.GetComponent(builder.ReservedEntity);
            var doMapInit = mapInit;
            doMapInit ??= _mapSystem.IsInitialized(xform.MapID);

            if (doMapInit.Value)
                RunMapInit(builder.ReservedEntity, builder.MetaData);

            // Pause inheritance REALLY should not be here, but the old system handled it explicitly too to my understanding.
            // Transform recursively inherits paused status...
            // TODO: This should really be handled by TransformSystem itself!
            if (xform.ParentUid.IsValid() && MetaQuery.Comp(xform.ParentUid).EntityPaused)
            {
                EntitySysManager.GetEntitySystem<MetaDataSystem>().SetEntityPaused(builder.ReservedEntity, true, builder.MetaData);
            }
        }


#if DEBUG
        // Prevent people from relying on entity builder command buffer order.
        // If you need this reliably, make a single CommandBuffer to run after calling SpawnBulk, or even call SpawnBulk
        // with that CommandBuffer to begin with.
        var buildersShuffled = builders.ToArray();
        _random.Shuffle(buildersShuffled);
#else
        var buildersShuffled = builders;
#endif

        foreach (var builder in buildersShuffled)
        {
            if (builder.PostInitCommands is {} commands)
                ApplyCommandBuffer(commands);
        }
    }

    public void SpawnBulkUnordered(Span<EntityBuilder> builders, bool? mapInit = null)
    {
        // Create an index of entity uids to builders,
        var index = new Dictionary<EntityUid, EntityBuilder>();

        foreach (var builder in builders)
        {
            index.Add(builder.ReservedEntity, builder);
        }

        var keys = new int[builders.Length];

        // Go through and figure out how "deep" any given entity is in the hierarchy,
        // i.e. how many parents it has.
        //
        // We can then order the builders by depth ascending to get creation order.
        for (var i = 0; i < builders.Length; i++)
        {
            var builder = builders[i];
            var depth = 0;
            var curr = builder;

            while (curr.Transform._parent != EntityUid.Invalid)
            {
                depth += 1;
                // If we're also spawning their parent, keep going.
                if (index.TryGetValue(curr.Transform._parent, out var parent))
                    curr = parent;
                else // Otherwise, the entity already exists or is otherwise outside our purview, so move on.
                    break;
            }

            keys[i] = depth;
        }

        // Sort ascending.
        keys.Sort(builders);

        SpawnBulk(builders);
    }
}
