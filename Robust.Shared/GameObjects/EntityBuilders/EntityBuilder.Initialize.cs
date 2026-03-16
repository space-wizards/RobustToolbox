using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Robust.Shared.GameObjects.EntityBuilders;

public sealed partial class EntityBuilder
{
    /// <summary>
    ///     Creates the bare minimal spawnable entity with metadata and a transform.
    /// </summary>
    /// <param name="context">The load context to use, if any.</param>
    private void InitializeMinimalEntity(ISerializationContext? context = null)
    {
        MetaData = new MetaDataComponent();
        AddComp(MetaData);

        Transform = new TransformComponent();
        AddComp(Transform);
    }

    /// <summary>
    ///     Initializes a command buffer from a prototype, cloning all the components onto the new entity.
    /// </summary>
    /// <param name="entityProtoId">The prototype to construct from.</param>
    /// <param name="context">The load context to use.</param>
    private void InitializeFromPrototype(EntProtoId entityProtoId, ISerializationContext? context = null)
    {
        var entityProto = _protoMan.Index(entityProtoId);

        InitializeMinimalEntity(context);

        foreach (var component in entityProto.Components.Components())
        {
            EnsureCopyComp(component, context);
        }
    }
}
