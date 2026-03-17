using System;
using System.Collections.Generic;
using System.Numerics;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Utility;

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
    private readonly ILocalizationManager _locMan;

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

    internal MetaDataComponent MetaData = default!;
    internal TransformComponent Transform = default!;

    internal EntityBuilder(IDependencyCollection collection, EntityUid reservedEntity)
    {
        _factory = collection.Resolve<IComponentFactory>();
        _protoMan = collection.Resolve<IPrototypeManager>();
        _serMan = collection.Resolve<ISerializationManager>();
        _locMan = collection.Resolve<ILocalizationManager>();
        ReservedEntity = reservedEntity;
        EntityComponents = new();
    }

    internal static EntityBuilder BlankEntity(
        IDependencyCollection collection,
        EntityUid reservedId,
        ISerializationContext? context)
    {
        var self = new EntityBuilder(collection, reservedId);

        self.InitializeMinimalEntity(context);

        return self;
    }

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
    ///     Retrieves the given concretely typed component from the builder.
    /// </summary>
    /// <param name="comp">The retrieved component.</param>
    /// <typeparam name="TComp">The type of component to retrieve.</typeparam>
    /// <returns>The builder, for chaining.</returns>
    public EntityBuilder GetComp<TComp>(out TComp comp)
    {
        comp = (TComp) EntityComponents[typeof(TComp)];
        return this;
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
}
