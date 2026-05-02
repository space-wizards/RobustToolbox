using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects.EntityBuilders;

namespace Robust.Shared.GameObjects;

public abstract partial class EntityManager
{
    public EntityUid Spawn(EntityBuilder builder, bool? mapInit = null)
    {
        var ent = builder.ReservedEntity;
        // Doesn't allocate. Not that it matters, we're about to allocate a lot.
        // Also, the remaining arguments to SpawnBulk don't matter here.
        // Code comprehension exercise for the reader to determine why.
        SpawnBulk([builder], mapInit);
        return ent;
    }

    public void SpawnBulk(ReadOnlySpan<EntityBuilder> builders, bool? mapInit = null, bool abortOnAnyFailure = true, bool deleteNonFailingEntities = true)
    {
        var anyFails = false;

        // Create entities + ComponentAdd
        foreach (var builder in builders)
        {
            try
            {
                AllocBuilderEntity(builder);

                // Pause inheritance REALLY should not be here, but the old system handled it explicitly too to my understanding.
                // Transform recursively inherits paused status...
                // TODO: This should really be handled by TransformSystem itself!
                if (builder.Transform.ParentUid.IsValid() && MetaQuery.Comp(builder.Transform.ParentUid).EntityPaused)
                {
                    EntitySysManager.GetEntitySystem<MetaDataSystem>()
                        .SetEntityPaused(builder.ReservedEntity, true, builder.MetaData);
                }
            }
            catch (Exception e)
            {
                DeleteEntity(builder.ReservedEntity, builder.MetaData, builder.Transform);
                builder.CreationFailure = new(e, EntityCreationException2.CreationStep.AllocAdd, builder);
                anyFails = true;
                if (abortOnAnyFailure)
                    goto handleFailure;
            }
        }

        // ComponentInit
        foreach (var builder in builders)
        {
            if (builder.CreationFailure is not null)
                continue; // Don't go any further.

            // Stuff happens, don't keep going if we don't exist.
            if (Deleted(builder.ReservedEntity))
                continue;

            try
            {
                InitializeEntity(builder.ReservedEntity, builder.MetaData);
            }
            catch (Exception e)
            {
                DeleteEntity(builder.ReservedEntity, builder.MetaData, builder.Transform);
                builder.CreationFailure = new(e, EntityCreationException2.CreationStep.Initialize, builder);;
                anyFails = true;
                if (abortOnAnyFailure)
                    goto handleFailure;
            }
        }

        // ComponentStartup
        foreach (var builder in builders)
        {
            if (builder.CreationFailure is not null)
                continue; // Don't go any further.

            // Stuff happens, don't keep going if we don't exist.
            if (Deleted(builder.ReservedEntity))
                continue;

            try
            {
                StartEntity(builder.ReservedEntity);
            }
            catch (Exception e)
            {
                DeleteEntity(builder.ReservedEntity, builder.MetaData, builder.Transform);
                builder.CreationFailure = new(e, EntityCreationException2.CreationStep.Startup, builder);;
                anyFails = true;
                if (abortOnAnyFailure)
                    goto handleFailure;
            }
        }

        foreach (var builder in builders)
        {
            if (builder.CreationFailure is not null)
                continue; // Don't go any further.

            // Stuff happens, don't keep going if we don't exist.
            if (Deleted(builder.ReservedEntity))
                continue;

            try
            {
                // MapInit is inherited, if we're on an initialized map we should also map init unless otherwise told.
                var doMapInit = mapInit;
                // Replicate whatever map we're on if mapInit is null.
                doMapInit ??= _mapSystem.IsInitialized(builder.Transform.MapID);

                if (doMapInit.Value)
                    RunMapInit(builder.ReservedEntity, builder.MetaData);
            }
            catch (Exception e)
            {
                DeleteEntity(builder.ReservedEntity, builder.MetaData, builder.Transform);
                builder.CreationFailure = new(e, EntityCreationException2.CreationStep.MapInit, builder);;
                anyFails = true;
                if (abortOnAnyFailure)
                    goto handleFailure;
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
            if (builder.CreationFailure is not null)
                continue; // Don't go any further.

            // Stuff happens, don't keep going if we don't exist.
            if (Deleted(builder.ReservedEntity))
                continue;

            try
            {
                if (builder.PostInitCommands is { } commands)
                    ApplyCommandBuffer(commands);
            }
            catch (Exception e)
            {
                DeleteEntity(builder.ReservedEntity, builder.MetaData, builder.Transform);
                builder.CreationFailure = new(e, EntityCreationException2.CreationStep.PostInitCommandBuffer, builder);;
                anyFails = true;
                if (abortOnAnyFailure)
                    goto handleFailure;
            }
        }

        if (!anyFails)
            return; // We done!

        handleFailure:

        var fails = new List<EntityCreationException2>();

        // We're not done. Gotta aggregate all our failures.
        foreach (var builder in builders)
        {
            if (builder.CreationFailure is not { } exception)
                continue;

            fails.Add(exception);
        }

        if (deleteNonFailingEntities)
        {
            foreach (var builder in builders)
            {
                try
                {
                    if (builder.CreationFailure is not null)
                        continue; // already dead.

                    if (Deleted(builder.ReservedEntity))
                        continue; // We killed it already.

                    DeleteEntity(builder.ReservedEntity, builder.MetaData, builder.Transform);
                }
                catch (Exception e)
                {
                    // More?? Oh no.
                    fails.Add(new (e, EntityCreationException2.CreationStep.FailureCleanup, builder));
                }
            }
        }

        throw new AggregateException("One or more entities failed to spawn.", fails);
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
