using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;

namespace Robust.Shared.ComponentTrees;

public interface IComponentTreeComponent<TComp> where TComp : Component, IComponentTreeEntry<TComp>
{
    public DynamicTree<ComponentTreeEntry<TComp>> Tree { get; set; }
}

/// <summary>
/// Wrapper for the <see cref="IComponentTreeComponent"> that allows "Layers" to be defined as well
/// </summary>
/// <typeparam name="TComp"></typeparam>
public interface ILayeredComponentTreeComponent<TComp> : IComponentTreeComponent<TComp> where TComp : Component, IComponentTreeEntry<TComp>
{
    public Dictionary<int, DynamicTree<ComponentTreeEntry<TComp>>> Trees { get; set; }
}

/// <summary>
///     Interface that must be implemented by components that can be stored on component trees.
/// </summary>
public interface IComponentTreeEntry<TComp> where TComp : Component
{
    /// <summary>
    ///     The tree that the component is currently stored on.
    /// </summary>
    public EntityUid? TreeUid { get; set; }

    /// <summary>
    ///     The tree that the component is currently stored on.
    /// </summary>
    public DynamicTree<ComponentTreeEntry<TComp>>? Tree { get; set; }

    /// <summary>
    ///     Whether or not the component should currently be added to a tree.
    /// </summary>
    public bool AddToTree { get; }

    public bool TreeUpdateQueued { get; set; }
}

public interface ILayeredComponentTreeEntry<TComp> : IComponentTreeEntry<TComp> where TComp : Component
{
    public HashSet<int> LayersUsed {get; set;}

    /// <summary>
    /// Dynamic Trees dictionary that this component is a part of.
    /// We aren't necessarily in ALL of them, but this ref is used to remove us from all of them on deletion.
    /// </summary>
    public Dictionary<int, DynamicTree<ComponentTreeEntry<TComp>>>? Trees { get; set; }
}
