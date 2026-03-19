using System;
using Microsoft.Extensions.ObjectPool;
using Robust.Shared.GameObjects.CommandBuffers;
using Robust.Shared.GameObjects.EntityBuilders;

namespace Robust.Shared.GameObjects;

public partial interface IEntityManager
{
    /// <summary>
    ///     The object pool containing unused command buffers.
    /// </summary>
    internal DefaultObjectPool<CommandBuffer> CommandBufferPool { get; }

    /// <summary>
    ///     Applies a command buffer to the simulation, running all queued commands.
    /// </summary>
    /// <param name="buffer">The command buffer to apply to sim.</param>
    public void ApplyCommandBuffer(CommandBuffer buffer);

    /// <summary>
    ///     Applies an entity builder to the simulation, spawning the entity it describes.
    /// </summary>
    /// <param name="builder">The entity builder to spawn into the world.</param>
    /// <param name="mapInit">Whether map init should be run for the built entities, or automatically inferred if null.</param>
    /// <returns>The constructed entity.</returns>
    public EntityUid Spawn(EntityBuilder builder, bool? mapInit = null);

    /// <summary>
    ///     Spawns the provided set of entity builders, in a manner much like loading a map does (with initialization
    ///     occuring in stages.)
    /// </summary>
    /// <remarks>
    ///     If you want to create your own map loading logic, this is pretty much how.
    ///     Entities given to this should be ordered such that transform parents initialize before their children do,
    ///     this limitation may be lifted in the future.
    /// </remarks>
    /// <param name="builders">The entity builders to spawn into the world.</param>
    /// <param name="mapInit">Whether map init should be run for the built entities, or automatically inferred if null.</param>
    /// <seealso cref="SpawnBulkUnordered"/>
    public void SpawnBulk(ReadOnlySpan<EntityBuilder> builders, bool? mapInit = null);

    /// <summary>
    ///     Spawns the provided set of entity builders, in a manner much like loading a map does (with initialization
    ///     occuring in stages.)
    /// </summary>
    /// <remarks>
    /// <para>
    ///     This accepts an unordered set of entities, and will rewrite the given span in place to be ordered by depth
    ///     in the hierarchy.
    /// </para>
    /// <para>
    ///     This only accounts for engine-imposed entity order constraints, if you need more complex behavior then
    ///     using <seealso cref="SpawnBulk"/> and your own sorting is better.
    /// </para>
    /// <para>
    ///     This has to sort the entire input list for you, if possible (i.e. your input is already hierarchically ordered)
    ///     use <see cref="SpawnBulk"/> instead.
    /// </para>
    /// <para>
    ///     This will hang if you give it a looping hierarchy! Do not create looping entity hierarchies.
    /// </para>
    /// </remarks>
    /// <param name="builders">The entity builders to spawn into the world.</param>
    /// <param name="mapInit">Whether map init should be run for the built entities.</param>
    /// <seealso cref="SpawnBulk"/>
    public void SpawnBulkUnordered(Span<EntityBuilder> builders, bool? mapInit = null);
}
