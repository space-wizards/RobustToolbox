using Robust.Shared.GameObjects.CommandBuffers;
using Robust.Shared.GameObjects.EntityBuilders;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

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
    ///     Creates a new entity builder, optionally for an entity with the given prototype.
    /// </summary>
    /// <param name="protoId">The entity prototype to use, if any.</param>
    /// <param name="context">A serialization context to use if constructing an entity from a prototype.</param>
    /// <returns>An entity builder with the expected set of components (MetaData, Transform, and any prototype-provided components.)</returns>
    public EntityBuilder EntityBuilder(EntProtoId? protoId = null, ISerializationContext? context = null);

    /// <summary>
    ///     Retrieves an unused entity slot, which command buffer application can fill in when spawning entities.
    /// </summary>
    /// <returns>A completely unallocated, now reserved entity id.</returns>
    internal EntityUid GetUnusedEntityUid();


}
