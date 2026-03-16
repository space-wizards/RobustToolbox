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
    ///     Applies an entity builder to the simulation, constructing the entity within in full.
    /// </summary>
    /// <param name="builder">The builder to apply.</param>
    /// <returns>The constructed entity.</returns>
    public EntityUid ApplyEntityBuilder(EntityBuilder builder);

    /// <summary>
    ///     Runs through entity builders in phases, ala loading a map.
    /// </summary>
    /// <remarks>
    ///     If you want to create your own map loading logic, this is pretty much how.
    /// </remarks>
    /// <param name="builders">The entity builders to allocate for.</param>
    public void BulkApplyEntityBuilders(ReadOnlySpan<EntityBuilder> builders);
}
