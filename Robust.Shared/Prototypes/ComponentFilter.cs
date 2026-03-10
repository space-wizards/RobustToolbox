using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Robust.Shared.Prototypes;

/// <summary>
///     A "filter" for entities, allowing you to describe a set of components they match and test for matches.
/// </summary>
/// <seealso cref="ComponentFilterQuery"/>
[PublicAPI]
public sealed class ComponentFilter : ISet<Type>
{
    private HashSet<Type> _components;

    /// <summary>
    ///     Constructs a new, blank component filter.
    /// </summary>
    public ComponentFilter()
    {
        _components = new();
    }

    /// <summary>
    ///     Constructs a new component filter from the given set of component types.
    /// </summary>
    /// <param name="components">The set of components to turn into a filter.</param>
    /// <remarks>
    ///     Duplicates will be removed.
    /// </remarks>
    public ComponentFilter(params Type[] components)
    {
        _components = components.ToHashSet();
        ValidateContents();
    }

    /// <summary>
    ///     Constructs a new component filter from the given set of component types.
    /// </summary>
    /// <param name="components">The set of components to turn into a filter.</param>
    /// <remarks>
    ///     Duplicates will be removed.
    /// </remarks>
    public ComponentFilter(IReadOnlyCollection<Type> components)
    {
        _components = components.ToHashSet();
        ValidateContents();
    }

    /// <summary>
    ///     Constructs a new component filter from the given set of component names.
    /// </summary>
    /// <param name="factory">The global component factory.</param>
    /// <param name="components">The set of components to turn into a filter.</param>
    /// <remarks>
    ///     Duplicates will be removed.
    /// </remarks>
    public ComponentFilter(IComponentFactory factory, params string[] components)
    {
        _components = components.Select(x => factory.GetRegistration(x).Type).ToHashSet();
        // No need to validate.
    }

    /// <summary>
    ///     Constructs a filter out of the contents of a registry.
    /// </summary>
    /// <param name="factory">The global component factory.</param>
    /// <param name="registry">The registry to obtain component types from.</param>
    /// <remarks>
    /// <para>
    ///     Filters do not retain any of the component's data, they only contain component types.
    ///     There is no engine API to filter for component data.
    /// </para>
    /// <para>
    ///     This is a new allocation, so if you need to do this regularly it's preferable to cache the filter.
    /// </para>
    /// </remarks>
    public ComponentFilter(IComponentFactory factory, ComponentRegistry registry)
    {
        _components = registry.Components().Select(x => x.GetType()).ToHashSet();
        ValidateContents();
    }

    public bool Add(Type component)
    {
#if DEBUG
        var factory = IoCManager.Resolve<IComponentFactory>();
        DebugTools.Assert(factory.TryGetRegistration(component, out _),
            "Cannot add non-components to a filter.");
#endif

        return _components.Add(component);
    }

    /// <summary>
    ///     Adds a given component to the filter.
    /// </summary>
    /// <param name="factory">The global component factory.</param>
    /// <param name="componentName">The component to add by name.</param>
    /// <returns>True if the component was added, false if it was already present.</returns>
    public bool Add(IComponentFactory factory, string componentName)
    {
        var component = factory.GetRegistration(componentName).Type;
        return _components.Add(component);
    }

    public void ExceptWith(IEnumerable<Type> other)
    {
        _components.ExceptWith(other);
        ValidateContents();
    }

    public void IntersectWith(IEnumerable<Type> other)
    {
        _components.IntersectWith(other);
        ValidateContents();
    }

    public bool IsProperSubsetOf(IEnumerable<Type> other)
    {
        return _components.IsProperSubsetOf(other);
    }

    public bool IsProperSupersetOf(IEnumerable<Type> other)
    {
        return _components.IsProperSupersetOf(other);
    }

    public bool IsSubsetOf(IEnumerable<Type> other)
    {
        return _components.IsSubsetOf(other);
    }

    public bool IsSupersetOf(IEnumerable<Type> other)
    {
        return _components.IsSupersetOf(other);
    }

    public bool Overlaps(IEnumerable<Type> other)
    {
        return _components.Overlaps(other);
    }

    public bool SetEquals(IEnumerable<Type> other)
    {
        return _components.SetEquals(other);
    }

    public void SymmetricExceptWith(IEnumerable<Type> other)
    {
        _components.SymmetricExceptWith(other);
        ValidateContents();
    }

    public void UnionWith(IEnumerable<Type> other)
    {
        _components.UnionWith(other);
        ValidateContents();
    }

    public void Clear()
    {
        _components.Clear();
    }

    public bool Contains(Type item)
    {
        return _components.Contains(item);
    }

    public void CopyTo(Type[] array, int arrayIndex)
    {
        _components.CopyTo(array, arrayIndex);
    }

    void ICollection<Type>.Add(Type item)
    {
        Add(item);
    }

    public bool Remove(Type component)
    {
#if DEBUG
        var factory = IoCManager.Resolve<IComponentFactory>();
        DebugTools.Assert(factory.TryGetRegistration(component, out _),
            "Cannot remove non-components from a filter.");
#endif

        return _components.Remove(component);
    }

    public int Count => _components.Count;
    public bool IsReadOnly => false;

    public IEnumerator<Type> GetEnumerator()
    {
        return _components.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    ///     Validates the component filter only contains components.
    /// </summary>
    [Conditional("DEBUG")]
    private void ValidateContents()
    {
        var factory = IoCManager.Resolve<IComponentFactory>();
        DebugTools.Assert(_components.All(x => factory.TryGetRegistration(x, out _)),
            "All types in a filter list must be valid, registered components.");
    }
}
