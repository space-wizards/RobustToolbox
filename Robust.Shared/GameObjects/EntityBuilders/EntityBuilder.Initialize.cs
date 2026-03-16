using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Robust.Shared.GameObjects.EntityBuilders;

public sealed partial class EntityBuilder
{
    /// <summary>
    ///     Creates the bare minimal spawnable entity with metadata and a transform.
    /// </summary>
    /// <param name="context">The load context to use, if any.</param>
    private void InitializeMinimalEntity(IEntityLoadContext? context = null)
    {
        MetaData = new MetaDataComponent();

        if (context?.TryGetComponent(_factory, out MetaDataComponent? overrideMeta) ?? false)
        {
            _serMan.CopyTo(overrideMeta,
                ref MetaData,
                context as ISerializationContext,
                notNullableOverride: true);
        }

        AddComp(MetaData);

        Transform = new TransformComponent();

        if (context?.TryGetComponent(_factory, out TransformComponent? overrideXform) ?? false)
        {
            _serMan.CopyTo(overrideXform,
                ref Transform,
                context as ISerializationContext,
                notNullableOverride: true);
        }

        AddComp(Transform);
    }

    /// <summary>
    ///     Initializes a command buffer from a prototype, doing most of entity setup aside from actually
    ///     constructing an entity to apply the components.
    /// </summary>
    /// <param name="entityProtoId">The prototype to construct from.</param>
    /// <param name="context">The load context to use.</param>
    private void InitializeFromPrototype(EntProtoId entityProtoId, IEntityLoadContext? context)
    {
        var entityProto = _protoMan.Index(entityProtoId);

        InitializeMinimalEntity(context);

        foreach (var component in entityProto.Components.Components())
        {
        }
    }
}
