using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Reflection;
using Robust.Shared.Collections;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Robust.Shared.GameObjects;

internal delegate void ComponentPrototypeCopyDelegate(
    IComponent source,
    ref IComponent target,
    ISerializationManager serialization,
    ISerializationContext? context);

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
    /// If this is true, the component will not be saved when saving a map/grid.
    /// </summary>
    /// <seealso cref="UnsavedComponentAttribute"/>
    public bool Unsaved { get; }

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

    /// <summary>
    /// Fields that are networked for this component. Used for delta states.
    /// </summary>
    public string[] NetworkedFields = [];

    public FrozenDictionary<string, int> NetworkedFieldLookup = FrozenDictionary<string, int>.Empty;

    internal ComponentPrototypeCopyDelegate PrototypeCopier { get; }

    // Internal for sandboxing.
    // Avoid content passing an instance of this to ComponentFactory to get any type they want instantiated.
    internal ComponentRegistration(string name, Type type, CompIdx idx, bool unsaved = false)
    {
        Name = name;
        Type = type;
        Idx = idx;
        Unsaved = unsaved;
        PrototypeCopier = CreatePrototypeCopier(type);
    }

    internal void CopyComponentFromPrototype(
        IComponent source,
        ref IComponent target,
        ISerializationManager serialization,
        ISerializationContext? context = null)
    {
        PrototypeCopier(source, ref target, serialization, context);
    }

    private static ComponentPrototypeCopyDelegate CreatePrototypeCopier(Type type)
    {
        var method = typeof(ComponentRegistration)
            .GetMethod(nameof(CopyComponentFromPrototypeTyped), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(type);

        return method.CreateDelegate<ComponentPrototypeCopyDelegate>();
    }

    private static void CopyComponentFromPrototypeTyped<T>(
        IComponent source,
        ref IComponent target,
        ISerializationManager serialization,
        ISerializationContext? context)
        where T : IComponent
    {
        var typedSource = (T) source;
        var typedTarget = (T) target;
        EntityPrototype.CopyComponentFromPrototype(typedSource, ref typedTarget, serialization, context);
        target = typedTarget;
    }

    public override string ToString()
    {
        return $"ComponentRegistration({Name}: {Type})";
    }
}
