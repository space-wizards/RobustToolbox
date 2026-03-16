using System;
using System.Collections.Generic;
using Robust.Shared.IoC;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Robust.Shared.GameObjects.EntityBuilders;

/// <summary>
/// <para>
///     A builder for an entity, allowing you to construct and mutate an incomplete entity before spawning it.
///     Supports construction from a prototype, and from a fresh list of components.
/// </para>
/// </summary>
public sealed partial class EntityBuilder
{
    private readonly IComponentFactory _factory;
    private readonly IPrototypeManager _protoMan;
    private readonly ISerializationManager _serMan;

    private void Foo(IRemoteEntityManager entMan)
    {
        var builder = entMan.BlankEntityBuilder()
            .AddComp<MapComponent>()
            .AddComp(new MapGridComponent
            {
                CanSplit = false
            });
    }

    /// <summary>
    ///     The EntityUid we have reserved.
    /// </summary>
    private readonly EntityUid _reservedId;

    /// <summary>
    ///     The components we'll be adding to the entity we construct.
    /// </summary>
    private readonly Dictionary<Type, IComponent> _entityComponents;

    internal EntityBuilder(IDependencyCollection collection, EntityUid reservedId)
    {
        _factory = collection.Resolve<IComponentFactory>();
        _protoMan = collection.Resolve<IPrototypeManager>();
        _serMan = collection.Resolve<ISerializationManager>();
        _reservedId = reservedId;
        _entityComponents = new();
    }

    internal static EntityBuilder BlankEntity(
        IDependencyCollection collection,
        EntityUid reservedId,
        IEntityLoadContext? loadContext)
    {
        var self = new EntityBuilder(collection, reservedId);

        self.InitializeMinimalEntity(loadContext);

        return self;
    }

    /// <summary>
    ///     Creates the bare minimal spawnable entity with metadata and a transform.
    /// </summary>
    /// <param name="context">The load context to use, if any.</param>
    private void InitializeMinimalEntity(IEntityLoadContext? context = null)
    {
        if (context?.TryGetComponent(_factory, out MetaDataComponent? meta) ?? false)
        {
            CopyComp(meta, context as ISerializationContext);
        }
        else
        {
            AddComp<MetaDataComponent>();
        }

        if (context?.TryGetComponent(_factory, out TransformComponent? xform) ?? false)
        {
            CopyComp(xform, context as ISerializationContext);
        }
        else
        {
            AddComp<TransformComponent>();
        }
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

        var meta = new MetaDataComponent();

        if (context?.TryGetComponent(_factory, out MetaDataComponent? overrideMeta) ?? false)
        {
            _serMan.CopyTo<MetaDataComponent>(overrideMeta,
                ref meta,
                context as ISerializationContext,
                notNullableOverride: true);
        }

        // Ensure we set up our metadata correctly, i.e. set the prototype so no explosions.
        meta._entityPrototype = entityProto;
        AddComp(meta);

        if (context?.TryGetComponent(_factory, out TransformComponent? xform) ?? false)
        {
            CopyComp(xform, context as ISerializationContext);
        }
        else
        {
            AddComp<TransformComponent>();
        }

        foreach (var component in entityProto.Components.Components())
        {
        }
    }

    /// <summary>
    ///     Mutates a given component in place using the provided, ideally static, action.
    /// </summary>
    /// <param name="action">The mutator to run</param>
    /// <param name="context">The context object for the action.</param>
    /// <typeparam name="TContext"></typeparam>
    /// <typeparam name="TComp">The concrete type of the component.</typeparam>
    public void MutateComp<TContext, TComp>(Action<TContext, TComp> action, TContext context)
        where TComp: IComponent, new()
    {
        var comp = _entityComponents[typeof(TComp)];

        action(context, (TComp)comp);
    }
}
