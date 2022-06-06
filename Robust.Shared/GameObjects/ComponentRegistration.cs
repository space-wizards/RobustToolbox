using System;
using System.Collections.Generic;
using Robust.Shared.Collections;
using Robust.Shared.GameStates;

namespace Robust.Shared.GameObjects;

/// <summary>
/// Represents a component registered into a <see cref="IComponentFactory" />.
/// </summary>
/// <seealso cref="IComponentFactory" />
/// <seealso cref="IComponent" />
public sealed class ComponentRegistration
{
    /// <summary>
    /// The name of the component.
    /// This is used as the <c>type</c> field in the component declarations if entity prototypes.
    /// </summary>
    /// <seealso cref="IComponent.Name" />
    public string Name { get; }

    public CompIdx Idx { get; }

    /// <summary>
    /// ID used to reference the component type across the network.
    /// If null, no network synchronization will be available for this component.
    /// </summary>
    /// <seealso cref="NetworkedComponentAttribute" />
    public ushort? NetID { get; internal set; }

    /// <summary>
    /// The type that will be instantiated if this component is created.
    /// </summary>
    public Type Type { get; }

    public ValueList<CompIdx> References;

    // Internal for sandboxing.
    // Avoid content passing an instance of this to ComponentFactory to get any type they want instantiated.
    internal ComponentRegistration(string name, Type type, CompIdx idx)
    {
        Name = name;
        Type = type;
        Idx = idx;
        References.Add(idx);
    }

    public override string ToString()
    {
        return $"ComponentRegistration({Name}: {Type})";
    }
}
