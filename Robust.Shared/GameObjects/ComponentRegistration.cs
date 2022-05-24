using System;
using System.Collections.Generic;
using Robust.Shared.GameStates;
using Robust.Shared.Utility.Collections;

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

    public ValueList<Type> References;

    public ComponentRegistration(string name, Type type)
    {
        Name = name;
        Type = type;
        References.Add(type);
    }

    public override string ToString()
    {
        return $"ComponentRegistration({Name}: {Type})";
    }
}
