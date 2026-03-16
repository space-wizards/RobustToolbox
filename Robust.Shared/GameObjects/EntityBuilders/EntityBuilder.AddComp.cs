using System;
using System.Collections.Generic;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects.EntityBuilders;

public sealed partial class EntityBuilder
{
    /// <summary>
    ///     Adds a component to the entity being built.
    /// </summary>
    /// <typeparam name="T">The type of component to add.</typeparam>
    /// <exception cref="ArgumentException">Thrown if the component already exists on the in progress entity.</exception>
    /// <returns>The builder, for chaining.</returns>
    public EntityBuilder AddComp<T>()
        where T : IComponent, new()
    {
        if (!_entityComponents.TryAdd(typeof(T), _factory.GetComponent(CompIdx.Index<T>())))
        {
            throw new ArgumentException(
                $"The component {_factory.GetComponentName<T>()} already existed in the builder.");
        }

        return this;
    }

    /// <summary>
    ///     Adds a component to the entity being built.
    /// </summary>
    /// <typeparam name="T">The type of component to add.</typeparam>
    /// <exception cref="ArgumentException">Thrown if the component already exists on the in progress entity.</exception>
    /// <returns>The builder, for chaining.</returns>
    public EntityBuilder AddComp(Type t)
    {
        if (!_entityComponents.TryAdd(t, _factory.GetComponent(t)))
        {
            throw new ArgumentException(
                $"The component {_factory.GetComponentName(t)} already existed in the builder.");
        }

        return this;
    }

    /// <summary>
    ///     Adds a component to the entity being built.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     This directly adds the provided component, <b>without making a copy</b>.
    ///     Do not use this with components you got from a registry without copying them!
    /// </para>
    /// <para>
    ///     Works with IComponent, but the concrete type of <paramref name="component"/> must be a constructable,
    ///     registered component.
    /// </para>
    /// </remarks>
    /// <param name="component">The component to add.</param>
    /// <typeparam name="T">The type of component to add.</typeparam>
    /// <exception cref="ArgumentException">Thrown if the component already exists on the in progress entity.</exception>
    /// <returns>The builder, for chaining.</returns>
    public EntityBuilder AddComp<T>(T component)
        where T : IComponent
    {
        DebugTools.AssertEqual(component.LifeStage, ComponentLifeStage.PreAdd);
        DebugTools.Assert(_factory.TryGetRegistration(component.GetType(), out _));

        if (!_entityComponents.TryAdd(component.GetType(), component))
        {
            throw new ArgumentException(
                $"The component {_factory.GetComponentName(component.GetType())} already existed in the builder.");
        }

        return this;
    }

    /// <summary>
    ///     Copies a component to the entity being built.
    /// </summary>
    /// <remarks>
    ///     Works with IComponent, but the concrete type of <paramref name="component"/> must be a constructable,
    ///     registered component.
    /// </remarks>
    /// <param name="component">The component to copy.</param>
    /// <param name="context">The optional serialization context to use when copying.</param>
    /// <typeparam name="T">The type of component to add.</typeparam>
    /// <exception cref="ArgumentException">Thrown if the component already exists on the in progress entity.</exception>
    /// <returns>The builder, for chaining.</returns>
    public EntityBuilder CopyComp<T>(T component, ISerializationContext? context = null)
        where T : IComponent
    {
        var newComp = _factory.GetComponent(component.GetType());

        _serMan.CopyTo(component, ref newComp, context, notNullableOverride: true);

        if (!_entityComponents.TryAdd(typeof(T), newComp))
        {
            throw new ArgumentException(
                $"The component {_factory.GetComponentName(component.GetType())} already existed in the builder.");
        }

        return this;
    }

    /// <summary>
    ///     Directly adds the given set of components to the entity being built.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     This directly adds the provided components, <b>without making a copy</b>.
    ///     Do not use this with components you got from a registry without copying them!
    /// </para>
    /// <para>
    ///     Works with IComponent, but the concrete types of <paramref name="components"/> must be constructable,
    ///     registered components.
    /// </para>
    /// </remarks>
    /// <param name="components">The set of components to add.</param>
    /// <exception cref="ArgumentException">Thrown if any component already exists on the in progress entity.</exception>
    /// <returns>The builder, for chaining.</returns>
    public EntityBuilder AddComps(IEnumerable<IComponent> components)
    {
        foreach (var component in components)
        {
            AddComp(component);
        }

        return this;
    }

    /// <inheritdoc cref="M:Robust.Shared.GameObjects.EntityBuilders.EntityBuilder.AddComps(System.Collections.Generic.IEnumerable{Robust.Shared.GameObjects.IComponent})"/>
    public EntityBuilder AddComps(Span<IComponent> components)
    {
        foreach (var component in components)
        {
            AddComp(component);
        }

        return this;
    }

    /// <summary>
    ///     Directly adds the given set of components to the entity being built.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     Works with IComponent, but the concrete types of <paramref name="components"/> must be constructable,
    ///     registered components.
    /// </para>
    /// </remarks>
    /// <param name="components">The set of components to add.</param>
    /// <exception cref="ArgumentException">Thrown if any component already exists on the in progress entity.</exception>
    /// <returns>The builder, for chaining.</returns>
    public EntityBuilder AddComps<T>(T components)
        where T : IEnumerable<Type>
    {
        foreach (var component in components)
        {
            AddComp(component);
        }

        return this;
    }

    /// <inheritdoc cref="M:Robust.Shared.GameObjects.EntityBuilders.EntityBuilder.AddComps(System.Collections.Generic.IEnumerable{System.Type})"/>
    public EntityBuilder AddComps(Span<Type> components)
    {
        foreach (var component in components)
        {
            AddComp(component);
        }

        return this;
    }

    /// <summary>
    ///     Copies a component to the entity being built.
    /// </summary>
    /// <remarks>
    ///     Works with IComponent, but the concrete types of <paramref name="components"/> must be constructable,
    ///     registered components.
    /// </remarks>
    /// <param name="components">The components to copy.</param>
    /// <param name="context">The optional serialization context to use when copying.</param>
    /// <exception cref="ArgumentException">Thrown if any component already exists on the in progress entity.</exception>
    /// <returns>The builder, for chaining.</returns>
    public EntityBuilder CopyComps<T>(T components, ISerializationContext? context = null)
        where T : IEnumerable<IComponent>
    {
        foreach (var component in components)
        {
            CopyComp(component, context);
        }

        return this;
    }

    /// <inheritdoc cref="M:Robust.Shared.GameObjects.EntityBuilders.EntityBuilder.CopyComps``1(``0,Robust.Shared.Serialization.Manager.ISerializationContext)"/>
    /// <returns>The builder, for chaining.</returns>
    public EntityBuilder CopyComps(Span<IComponent> components, ISerializationContext? context = null)
    {
        foreach (var component in components)
        {
            CopyComp(component, context);
        }

        return this;
    }
}
