using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.CommandBuffers;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Robust.Shared.GameObjects.EntityBuilders;

/// <summary>
/// <para>
///     A builder for an entity, allowing you to construct and mutate an incomplete entity before spawning it.
///     Supports construction from a prototype, and from a fresh list of components.
/// </para>
/// </summary>
[PublicAPI]
public sealed partial class EntityBuilder
{
    private readonly IComponentFactory _factory;
    private readonly IPrototypeManager _protoMan;
    private readonly ISerializationManager _serMan;
    private readonly ILocalizationManager _locMan;
    private readonly IRemoteEntityManager _remoteEntMan;

    /// <summary>
    ///     The EntityUid we have reserved. No entity exists here yet unless the builder has been
    ///     applied and consumed.
    /// </summary>
    public readonly EntityUid ReservedEntity;

    /// <summary>
    ///     The components we'll be adding to the entity we construct.
    /// </summary>
    internal readonly Dictionary<Type, IComponent> EntityComponents;

    /// <summary>
    ///     The coordinates to spawn the entity at, if any.
    ///     Due to how map coordinates work, these have to be resolved at spawn time just before initializing
    ///     the entities.
    /// </summary>
    private MapCoordinates? _mapCoordinates;

    public MetaDataComponent MetaData { get; private set; } = default!;
    public TransformComponent Transform { get; private set; } = default!;

    internal CommandBuffer? PostInitCommands { get; private set; }

    internal EntityBuilder(IDependencyCollection collection, EntityUid reservedEntity)
    {
        _factory = collection.Resolve<IComponentFactory>();
        _protoMan = collection.Resolve<IPrototypeManager>();
        _serMan = collection.Resolve<ISerializationManager>();
        _locMan = collection.Resolve<ILocalizationManager>();
        _remoteEntMan = collection.Resolve<IRemoteEntityManager>();
        ReservedEntity = reservedEntity;
        EntityComponents = new();
    }

    /// <summary>
    ///     Constructs a blank (MetaData & Transform only) entity builder for the given reserved ID.
    /// </summary>
    internal static EntityBuilder BlankEntity(
        IDependencyCollection collection,
        EntityUid reservedId,
        ISerializationContext? context)
    {
        var self = new EntityBuilder(collection, reservedId);

        self.InitializeMinimalEntity(context);

        return self;
    }

    /// <summary>
    ///     Constructs an entity builder for the given prototype and reserved entity ID.
    /// </summary>
    internal static EntityBuilder PrototypedEntity(
        IDependencyCollection collection,
        EntityUid reservedId,
        EntProtoId proto,
        ISerializationContext? context)
    {
        var self = new EntityBuilder(collection, reservedId);

        self.InitializeFromPrototype(proto, context);

        return self;
    }

    // wishing we had rust alloc-free lambdas rn.
    /// <summary>
    ///     Mutates a given component in place using the provided, ideally static, action.
    /// </summary>
    /// <param name="action">The mutator to run</param>
    /// <param name="context">The context object for the action.</param>
    /// <typeparam name="TContext"></typeparam>
    /// <typeparam name="TComp">The concrete type of the component.</typeparam>
    /// <returns>The builder, for chaining.</returns>
    /// <remarks>
    ///     If you need this often you may be better off making an extension method.
    /// </remarks>
    /// <example>
    /// <code>
    ///     // Allocation-free usage of MutateComp.
    ///     public sealed class MySystem : EntitySystem
    ///     {
    ///         public EntityUid TestMethod()
    ///         {
    ///             var builder = EntityManager.BlankEntityBuilder()
    ///                 .AddComp&lt;MapGridComponent&gt;()
    ///                 .MutateComp&lt;MySystem, MetaDataComponent&gt;(
    ///                     static (ctx, meta) => ctx.DoThing(meta),
    ///                     this);
    ///
    ///         }
    ///
    ///         public void DoThing(MetaDataComponent meta)
    ///         {
    ///             // ...
    ///         }
    ///     }
    /// </code>
    /// </example>
    public EntityBuilder MutateComp<TContext, TComp>(Action<TContext, TComp> action, TContext context)
        where TComp: IComponent, new()
    {
        var comp = EntityComponents[typeof(TComp)];

        action(context, (TComp)comp);

        return this;
    }

    /// <summary>
    ///     Attempts to mutate a given component in place using the provided, ideally static, action.
    ///     If the component doesn't exist, nothing happens.
    /// </summary>
    /// <param name="action">The mutator to run</param>
    /// <param name="context">The context object for the action.</param>
    /// <typeparam name="TContext"></typeparam>
    /// <typeparam name="TComp">The concrete type of the component.</typeparam>
    /// <returns>The builder, for chaining.</returns>
    /// <remarks>
    ///     If you need this often you may be better off making an extension method.
    /// </remarks>
    /// <example>
    /// <code>
    ///     // Allocation-free usage of MutateComp.
    ///     public sealed class MySystem : EntitySystem
    ///     {
    ///         public EntityUid TestMethod()
    ///         {
    ///             var builder = EntityManager.BlankEntityBuilder()
    ///                 .AddComp&lt;MapGridComponent&gt;()
    ///                 .MutateComp&lt;MySystem, MetaDataComponent&gt;(
    ///                     static (ctx, meta) => ctx.DoThing(meta),
    ///                     this);
    ///
    ///         }
    ///
    ///         public void DoThing(MetaDataComponent meta)
    ///         {
    ///             // ...
    ///         }
    ///     }
    /// </code>
    /// </example>
    public EntityBuilder TryMutateComp<TContext, TComp>(Action<TContext, TComp> action, TContext context)
        where TComp: IComponent, new()
    {
        if (EntityComponents.TryGetValue(typeof(TComp), out var comp))
        {
            action(context, (TComp)comp);
        }

        return this;
    }

    /// <summary>
    ///     Retrieves the given concretely typed component from the builder.
    /// </summary>
    /// <param name="comp">The retrieved component.</param>
    /// <typeparam name="TComp">The type of component to retrieve.</typeparam>
    /// <returns>The builder, for chaining.</returns>
    [PreferNonGenericVariantFor(typeof(MetaDataComponent), typeof(TransformComponent))]
    public EntityBuilder GetComp<TComp>(out TComp comp)
        where TComp : IComponent, new()
    {
        comp = (TComp) EntityComponents[typeof(TComp)];
        return this;
    }

    /// <summary>
    ///     Retrieves the given component from the builder.
    /// </summary>
    /// <param name="t">The type of component to retrieve.</param>
    /// <param name="comp">The retrieved component.</param>
    /// <returns>The builder, for chaining.</returns>
    [PreferGenericVariant]
    public EntityBuilder GetComp(Type t, out IComponent comp)
    {
        comp = EntityComponents[t];
        return this;
    }

    /// <summary>
    ///     Attempts to retrieve the given component from the builder.
    /// </summary>
    /// <param name="comp">The component, if any.</param>
    /// <typeparam name="TComp">The type of component to retrieve.</typeparam>
    /// <remarks>Not chainable.</remarks>
    /// <returns>Whether the operation succeeded.</returns>
    [PreferNonGenericVariantFor(typeof(MetaDataComponent), typeof(TransformComponent))]
    public bool TryComp<TComp>([NotNullWhen(true)] out TComp? comp)
        where TComp : IComponent, new()
    {
        if (EntityComponents.TryGetValue(typeof(TComp), out var c))
        {
            comp = (TComp)c;
            return true;
        }

        comp = default;
        return false;
    }

    /// <summary>
    ///     Attempts to retrieve the given component from the builder.
    /// </summary>
    /// <param name="t">The type of component to retrieve.</param>
    /// <param name="comp">The component, if any.</param>
    /// <remarks>Not chainable.</remarks>
    /// <returns>Whether the operation succeeded.</returns>
    [PreferGenericVariant]
    public bool TryComp(Type t, [NotNullWhen(true)] out IComponent? comp)
    {
        return EntityComponents.TryGetValue(t, out comp);
    }

    /// <summary>
    ///     Ensures the entity is spawned as a transform child of the given parent.
    /// </summary>
    /// <param name="parent">The parent to attach to.</param>
    /// <param name="relativePos">The parent-relative position to use for coordinates.</param>
    /// <param name="rotation">The parent-relative angle to use.</param>
    /// <include file='Docs.xml' path='entries/entry[@name="ParentingRaceConditionRemark"]/*'/>
    /// <returns>The builder, for chaining.</returns>
    public EntityBuilder ChildOf(EntityUid parent, Vector2 relativePos = default, Angle? rotation = null)
    {
        if (_mapCoordinates is not null)
            _mapCoordinates = null; // One or the other.

        Transform._parent = parent;
        Transform._localPosition = relativePos;

        if (rotation is not null)
            Transform._localRotation = rotation.Value;

        return this;
    }

    /// <summary>
    ///     Ensures the entity is spawned as a transform child of the given parent.
    /// </summary>
    /// <param name="coordinates">The relative coordinates to give this entity.</param>
    /// <param name="rotation">The parent-relative angle to use.</param>
    /// <include file='Docs.xml' path='entries/entry[@name="ParentingRaceConditionRemark"]/*'/>
    /// <returns>The builder, for chaining.</returns>
    public EntityBuilder ChildOf(EntityCoordinates coordinates, Angle? rotation = null)
    {
        if (_mapCoordinates is not null)
            _mapCoordinates = null; // One or the other.

        Transform._parent = coordinates.EntityId;
        Transform._localPosition = coordinates.Position;

        if (rotation is not null)
            Transform._localRotation = rotation.Value;

        return this;
    }

    /// <summary>
    ///     Ensures the entity is spawned at the given map coordinate, automatically finding a parent.
    /// </summary>
    /// <param name="mapCoordinates">The coordinates to spawn at</param>
    /// <include file='Docs.xml' path='entries/entry[@name="ParentingRaceConditionRemark"]/*'/>
    /// <returns>The builder, for chaining.</returns>
    public EntityBuilder LocatedAt(MapCoordinates mapCoordinates)
    {
        _mapCoordinates = mapCoordinates;

        if (Transform._parent != EntityUid.Invalid)
        {
            Transform._parent = EntityUid.Invalid;
            Transform._localPosition = default;
        }

        return this;
    }

    /// <summary>
    ///     Returns a buffer that will be run when the entity has been fully constructed.
    /// </summary>
    /// <param name="buffer">The command buffer that post init commands should be added to.</param>
    /// <returns>The builder, for chaining.</returns>
    /// <remarks>
    /// <para>
    ///     It is <b>strongly discouraged</b> to use the post init command buffer to construct additional entities, or
    ///     add additional components.
    ///     Content systems should support working with entity builders directly when possible as they perform better than
    ///     adding additional entities and components after-the-fact.
    /// </para>
    /// <para>
    ///     The buffer is guaranteed to run only after all entities in a <see cref="IEntityManager.SpawnBulk"/> invocation
    ///     have been started up (and optionally map initialized), and no sooner.
    ///     The order in which post init buffers run is undefined.
    /// </para>
    /// <para>
    ///     This only ever creates one buffer, and upon being called again, will return the same buffer. As adding to
    ///     command buffers is not threadsafe and should only be done from one thread at a time, if you need multiple
    ///     please use <see cref="CommandBuffer.CreateSubBuffer"/>.
    /// </para>
    /// </remarks>
    public EntityBuilder WithPostInitCommands(out CommandBuffer buffer)
    {
        PostInitCommands ??= _remoteEntMan.GetCommandBuffer();

        buffer = PostInitCommands;
        return this;
    }
}
