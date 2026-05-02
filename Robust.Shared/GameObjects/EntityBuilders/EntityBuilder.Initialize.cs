using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Robust.Shared.GameObjects.EntityBuilders;

public sealed partial class EntityBuilder
{
    /// <summary>
    ///     Creates the bare minimal spawnable entity with metadata and a transform.
    /// </summary>
    /// <param name="context">The serialization context to use, if any.</param>
    private void InitializeMinimalEntity(ISerializationContext? context = null)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        MetaData = new MetaDataComponent() { Owner = ReservedEntity };
#pragma warning restore CS0618 // Type or member is obsolete
        AddComp(MetaData);

        // This thing is so legacy it has a [Dependency] in it.
        Transform = _factory.GetComponent<TransformComponent>();
        AddComp(Transform);
    }

    /// <summary>
    ///     Initializes a command buffer from a prototype, cloning all the components onto the new entity.
    /// </summary>
    /// <param name="entityProtoId">The prototype to construct from.</param>
    /// <param name="context">The serialization context to use, if any.</param>
    private void InitializeFromPrototype(EntProtoId entityProtoId, ISerializationContext? context = null)
    {
        var entityProto = _protoMan.Index(entityProtoId);

        InitializeMinimalEntity(context);

        MetaData._entityPrototype = entityProto;

        foreach (var component in entityProto.Components.Components())
        {
            EnsureCopyComp(component, context);
        }
    }

    /// <summary>
    ///     Applies the given component registry to the builder, copying over all components it contains.
    /// </summary>
    /// <param name="registry">The registry to apply.</param>
    /// <param name="context">The serialization context to use, if any.</param>
    /// <returns></returns>
    public EntityBuilder ApplyRegistry(ComponentRegistry registry, ISerializationContext? context = null)
    {
        foreach (var component in registry.Components())
        {
            EnsureCopyComp(component, context);
        }

        return this;
    }
}
