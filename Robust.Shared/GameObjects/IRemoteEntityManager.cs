using Robust.Shared.GameObjects.CommandBuffers;

namespace Robust.Shared.GameObjects;

/// <summary>
///     A thread-safe subset of <see cref="IEntityManager"/>'s interface.
///     These can be used in parallel with a running game thread mutating the ECS.
/// </summary>
public interface IRemoteEntityManager
{
    /// <summary>
    ///     Creates a new, empty command buffer.
    /// </summary>
    public CommandBuffer GetCommandBuffer();

    /// <summary>
    ///     Retrieves an unused entity slot, which command buffer application can fill in when spawning entities.
    /// </summary>
    /// <returns>A completely unallocated, now reserved entity id.</returns>
    internal EntityUid GetUnusedEntityUid();
}
