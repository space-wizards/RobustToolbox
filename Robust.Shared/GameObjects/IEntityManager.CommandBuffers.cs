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
    /// <param name="builder">The builder to apply.</param>
    /// <param name="mapInit">Whether map init should be run for the built entities.</param>
    /// <returns>The constructed entity.</returns>
    public EntityUid Spawn(EntityBuilder builder, bool mapInit = true);

    /// <summary>
    ///     Spawns the provided set of entity builders, in a manner much like loading a map does (with initialization
    ///     occuring in stages.)
    /// </summary>
    /// <remarks>
    ///     If you want to create your own map loading logic, this is pretty much how.
    ///     Entities given to this should be ordered such that transform parents initialize before their children do,
    ///     this limitation may be lifted in the future.
    /// </remarks>
    /// <param name="builders">The entity builders to allocate for.</param>
    /// <param name="mapInit">Whether map init should be run for the built entities.</param>
    public void SpawnBulk(ReadOnlySpan<EntityBuilder> builders, bool mapInit = true);
}
