using Microsoft.Extensions.ObjectPool;
using Robust.Shared.GameObjects.CommandBuffers;

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
}
