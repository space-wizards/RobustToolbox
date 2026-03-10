using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Robust.Shared.Prototypes;

/// <summary>
///     A registry of instantiated, deserialized components.
/// </summary>
/// <remarks>
///     This is distinctly <b>not</b> a filter list, and contains fully constructed components.
///     To filter for components, use a ComponentFilterList instead.
///
///     Currently, working with names in a registry is most efficient. However when all obsolete API usage has been
///     cleaned up, registry internals will be changed and functionality used in prototypes/yaml will be split off.
/// </remarks>
public sealed class ComponentRegistry : IEntityLoadContext, IEnumerable<KeyValuePair<string, EntityPrototype.ComponentRegistryEntry>>
{
    /// <summary>
    ///     The underlying dictionary of this registry. This stores component names mapped to entries.
    /// </summary>
    private readonly Dictionary<string, EntityPrototype.ComponentRegistryEntry> _inner;

    /// <summary>
    ///     The number of components contained within the registry.
    /// </summary>
    public int Count => _inner.Count;

    [Obsolete("Use Components, ComponentsAndNames, or ComponentTypes instead.")]
    public IReadOnlyCollection<EntityPrototype.ComponentRegistryEntry> Values => _inner.Values;

    [Obsolete("Use Components, ComponentsAndNames, or ComponentTypes instead.")]
    public IReadOnlyCollection<string> Keys => _inner.Keys;


    public ComponentRegistry()
    {
        _inner = new();
    }

    internal ComponentRegistry(Dictionary<string, EntityPrototype.ComponentRegistryEntry> components)
    {
        _inner = components;
    }

    /// <summary>
    ///     Constructs a new component registry from a list of components and the global component factory.
    /// </summary>
    /// <param name="factory">The global component factory.</param>
    /// <param name="component">The components to build from.</param>
    public ComponentRegistry(IComponentFactory factory, params IEnumerable<IComponent> component)
    {
        _inner = component.ToDictionary(
            x => factory.GetRegistration(x.GetType()).Name,
            x => new EntityPrototype.ComponentRegistryEntry(x)
        );
    }

    /// <summary>
    ///     Adds a component to the registry, constructing it.
    /// </summary>
    /// <param name="factory">The global component factory.</param>
    /// <param name="component">The type of component to construct.</param>
    public IComponent AddComponent(IComponentFactory factory, Type component)
    {
        var name = factory.GetComponentName(component);
        var c = factory.GetComponent(component);

        AddComponentManual(name, c);

        return c;
    }

    /// <summary>
    ///     Adds a component to the registry, constructing it.
    /// </summary>
    /// <param name="factory">The global component factory.</param>
    /// <typeparam name="T">The type of component to construct.</typeparam>
    public T AddComponent<T>(IComponentFactory factory)
        where T: IComponent
    {
        return (T)AddComponent(factory, typeof(T));
    }

    /// <summary>
    ///     Adds a pre-constructed component to this registry.
    /// </summary>
    public void AddComponent(IComponentFactory factory, IComponent component)
    {
        var name = factory.GetComponentName(component.GetType());

        AddComponentManual(name, component);
    }

    /// <summary>
    ///     Manually inserts a pre-constructed component into the registry by name.
    /// </summary>
    public void AddComponentManual(IComponentFactory factory, string componentName, IComponent component)
    {
        AddComponentManual(componentName, component);
    }

    /// <summary>
    ///     Manually inserts a pre-constructed component into the registry by name.
    /// </summary>
    /// <remarks>
    ///     You almost never want this, this is for situations where you have no choice (i.e. cannot use IoC).
    /// </remarks>
    internal void AddComponentManual(string componentName, IComponent component)
    {
        _inner[componentName] = new EntityPrototype.ComponentRegistryEntry(component);
    }

    /// <summary>
    ///     Gets a component out of the registry by type.
    /// </summary>
    /// <param name="factory">The global component factory.</param>
    /// <typeparam name="T">The component type to retrieve.</typeparam>
    /// <returns>The component.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the given component is not in the registry.</exception>
    public T GetComponent<T>(IComponentFactory factory)
        where T: class, IComponent, new()
    {
        if (TryGetComponent<T>(factory, out var c))
            return c;

        throw new KeyNotFoundException($"Couldn't find {typeof(T)} in the registry.");
    }

    /// <summary>
    ///     Gets a component out of the registry by type.
    /// </summary>
    /// <param name="factory">The global component factory.</param>
    /// <param name="component">The component type to retrieve.</param>
    /// <returns>The component.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the given component is not in the registry.</exception>
    public IComponent GetComponent(IComponentFactory factory, Type component)
    {
        if (TryGetComponent(factory, component, out var c))
            return c;

        throw new KeyNotFoundException($"Couldn't find {component} in the registry.");
    }

    /// <summary>
    ///     Gets a component out of the registry by name.
    /// </summary>
    /// <param name="factory">The global component factory</param>
    /// <param name="name">The component to retrieve.</param>
    /// <returns>The component.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the given component is not in the registry.</exception>
    public IComponent GetComponentByName(IComponentFactory factory, string name)
    {
        return _inner[name].Component;
    }

    /// <inheritdoc />
    [Obsolete("The IComponentFactory receiving method must be used.")]
    public bool TryGetComponent(string componentName, [NotNullWhen(true)] out IComponent? component)
    {
        var success = _inner.TryGetValue(componentName, out var comp);
        component = comp?.Component;

        return success;
    }

    /// <inheritdoc />
    public bool TryGetComponent(IComponentFactory factory, string componentName, [NotNullWhen(true)] out IComponent? component)
    {
        var success = _inner.TryGetValue(componentName, out var comp);
        component = comp?.Component;

        return success;
    }

    /// <inheritdoc />
    public bool TryGetComponent<TComponent>(
        IComponentFactory factory,
        [NotNullWhen(true)] out TComponent? component
    ) where TComponent : class, IComponent, new()
    {
        component = null;
        var componentName = factory.GetComponentName<TComponent>();
        if (TryGetComponent(factory, componentName, out var foundComponent))
        {
            component = (TComponent)foundComponent;
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public bool TryGetComponent(IComponentFactory factory, Type componentType, [NotNullWhen(true)] out IComponent? component)
    {
        component = null;
        var componentName = factory.GetComponentName(componentType);
        if (TryGetComponent(factory, componentName, out component))
        {
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public IEnumerable<string> GetExtraComponentTypes()
    {
        return _inner.Keys;
    }

    /// <inheritdoc />
    public bool ShouldSkipComponent(string compName)
    {
        return false; //Registries cannot represent the "remove this component" state.
    }

    /// <summary>
    ///     Enumerate components in the registry by type.
    /// </summary>
    public IEnumerable<Type> ComponentTypes()
    {
        return _inner.Values.Select(x => x.Component.GetType());
    }

    /// <summary>
    ///     Enumerate components in the registry by value.
    /// </summary>
    public IEnumerable<IComponent> Components()
    {
        return _inner.Values.Select(x => x.Component);
    }

    /// <summary>
    ///     Enumerate components in the registry by value, with their names.
    /// </summary>
    public IEnumerable<(string, IComponent)> ComponentsAndNames(IComponentFactory factory)
    {
        return _inner.Select(x => (x.Key, x.Value.Component));
    }

    /// <summary>
    ///     Enumerates the names of all components in the registry.
    /// </summary>
    public IEnumerable<string> Names(IComponentFactory factory)
    {
        return _inner.Keys;
    }

    /// <summary>
    ///     Enumerate components in the registry by value that are assignable to T.
    /// </summary>
    /// <typeparam name="T">The type to enumerate for.</typeparam>
    /// <returns>All components assignable to T.</returns>
    /// <example>
    /// <code>
    ///     // Get all components that implement the IFunny interface.
    ///     var funnyComponents = registry.ComponentsAssignableTo&lt;IFunny&gt;();
    /// </code>
    /// </example>
    public IEnumerable<T> ComponentsAssignableTo<T>()
    {
        return _inner.Values
            .Where(x => x.Component is T)
            .Select(x => (T)x.Component);
    }

    /// <summary>
    ///     Tests if the registry contains the given component.
    /// </summary>
    /// <param name="factory">The global component factory.</param>
    /// <param name="component">The type of component to check for.</param>
    /// <returns>Whether the registry contains the given component.</returns>
    public bool ContainsComponent(IComponentFactory factory, Type component)
    {
        var name = factory.GetComponentName(component);

        return _inner.ContainsKey(name);
    }

    /// <summary>
    ///     Tests if the registry contains the given component by name. You should prefer to use types!
    /// </summary>
    /// <param name="name">The name of the component to check for.</param>
    /// <returns>Whether the registry contains the given component.</returns>
    public bool ContainsComponentByName(string name)
    {
        return _inner.ContainsKey(name);
    }

    [Obsolete("ComponentRegistry is no longer an exposed dictionary, use the new methods like Components.")]
    public IEnumerator<KeyValuePair<string, EntityPrototype.ComponentRegistryEntry>> GetEnumerator()
    {
        return _inner.GetEnumerator();
    }

    [Obsolete("ComponentRegistry is no longer an exposed dictionary, use the new methods like Components.")]
    IEnumerator IEnumerable.GetEnumerator()
    {
        return _inner.GetEnumerator();
    }

    [Obsolete("Legacy dictionary API compatibility. Use ContainsComponent.")]
    public bool ContainsKey(string name)
    {
        return _inner.ContainsKey(name);
    }

    [Obsolete("Legacy dictionary API compatibility. Use TryGetComponent.")]
    public bool TryGetValue(string name, [NotNullWhen(true)] out EntityPrototype.ComponentRegistryEntry? o)
    {
        return _inner.TryGetValue(name, out o);
    }

    /// <summary>
    ///     Retrieves an underlying component entry, if possible.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="o"></param>
    /// <returns></returns>
    public bool TryGetEntry(string name, [NotNullWhen(true)] out EntityPrototype.ComponentRegistryEntry? o)
    {
        return _inner.TryGetValue(name, out o);
    }

    [Obsolete("Legacy dictionary API compatibility. Use AddComponent and TryGetComponent.")]
    public EntityPrototype.ComponentRegistryEntry this[string compType]
    {
        get => _inner[compType];
        set => _inner[compType] = value;
    }

    /// <summary>
    ///     Empties the component registry entirely.
    /// </summary>
    public void Clear()
    {
        _inner.Clear();
    }

    /// <summary>
    ///     Ensures the component registry has capacity for N components.
    /// </summary>
    /// <param name="count"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void EnsureCapacity(int count)
    {
        _inner.EnsureCapacity(count);
    }

    /// <summary>
    ///     Method for use by <see cref="ComponentRegistrySerializer"/> when deserializing.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="entry"></param>
    internal void AddEntry(string name, EntityPrototype.ComponentRegistryEntry entry)
    {
        _inner[name] = entry;
    }
}
