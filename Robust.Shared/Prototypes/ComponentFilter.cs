using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Robust.Shared.Prototypes;

/// <summary>
///     A "filter" for entities, allowing you to describe a set of components they match and test for matches.
/// </summary>
/// <remarks>
///     Cache your filters! Filter baking TBD.
/// </remarks>
public sealed class ComponentFilter : ISet<Type>
{
    /// <summary>
    ///     Internals of a filter.
    ///     Do not use outside of serialization, i beg.
    /// </summary>
    internal HashSet<Type> Components;

    /// <summary>
    ///     Constructs a new, blank component filter.
    /// </summary>
    public ComponentFilter()
    {
        Components = new();
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
        Components = components.ToHashSet();
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
        Components = components.ToHashSet();
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
        Components = components.Select(x => factory.GetRegistration(x).Type).ToHashSet();
        // No need to validate.
    }

    public bool Add(Type component)
    {
#if DEBUG
        var factory = IoCManager.Resolve<IComponentFactory>();
        DebugTools.Assert(factory.TryGetRegistration(component, out _),
            "Cannot add non-components to a filter.");
#endif

        return Components.Add(component);
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
        return Components.Add(component);
    }

    public void ExceptWith(IEnumerable<Type> other)
    {
        Components.ExceptWith(other);
        ValidateContents();
    }

    public void IntersectWith(IEnumerable<Type> other)
    {
        Components.IntersectWith(other);
        ValidateContents();
    }

    public bool IsProperSubsetOf(IEnumerable<Type> other)
    {
        return Components.IsProperSubsetOf(other);
    }

    public bool IsProperSupersetOf(IEnumerable<Type> other)
    {
        return Components.IsProperSupersetOf(other);
    }

    public bool IsSubsetOf(IEnumerable<Type> other)
    {
        return Components.IsSubsetOf(other);
    }

    public bool IsSupersetOf(IEnumerable<Type> other)
    {
        return Components.IsSupersetOf(other);
    }

    public bool Overlaps(IEnumerable<Type> other)
    {
        return Components.Overlaps(other);
    }

    public bool SetEquals(IEnumerable<Type> other)
    {
        return Components.SetEquals(other);
    }

    public void SymmetricExceptWith(IEnumerable<Type> other)
    {
        Components.SymmetricExceptWith(other);
        ValidateContents();
    }

    public void UnionWith(IEnumerable<Type> other)
    {
        Components.UnionWith(other);
        ValidateContents();
    }

    public void Clear()
    {
        Components.Clear();
    }

    public bool Contains(Type item)
    {
        return Components.Contains(item);
    }

    public void CopyTo(Type[] array, int arrayIndex)
    {
        Components.CopyTo(array, arrayIndex);
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

        return Components.Remove(component);
    }

    public int Count => Components.Count;
    public bool IsReadOnly => false;

    public IEnumerator<Type> GetEnumerator()
    {
        return Components.GetEnumerator();
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
        DebugTools.Assert(Components.All(x => factory.TryGetRegistration(x, out _)),
            "All types in a filter list must be valid, registered components.");
    }
}
