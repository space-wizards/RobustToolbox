using System;
using JetBrains.Annotations;

namespace Robust.Shared.GameObjects;

/// <summary>
///     Marks a component as being automatically registered by <see cref="IComponentFactory.DoAutoRegistrations" />
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
[BaseTypeRequired(typeof(IComponent))]
[MeansImplicitUse]
public sealed class RegisterComponentAttribute : Attribute;

/// <summary>
/// Defines Name that this component is represented with in prototypes.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ComponentProtoNameAttribute(string prototypeName) : Attribute
{
    public string PrototypeName { get; } = prototypeName;
}

/// <summary>
/// Marks a component as not being saved when saving maps/grids.
/// </summary>
/// <seealso cref="ComponentRegistration.Unsaved"/>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class UnsavedComponentAttribute : Attribute;
