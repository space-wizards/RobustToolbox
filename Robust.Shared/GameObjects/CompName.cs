using System;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Toolshed.TypeParsers;

namespace Robust.Shared.GameObjects;

/// <summary>
/// Wrapper type for the name of a component.
/// All methods of creating it are checked to be valid, so the string value is also guaranteed to be valid.
/// </summary>
/// <remarks>
/// This will be automatically validated by <see cref="CompNameSerializer"/> if used in data fields.
/// Doing so however will NOT skip ignored components, move your shitcode to shared!
/// </remarks>
public readonly record struct CompName :
    IEquatable<string>,
    IComparable<CompName>,
    IAsType<string>
{
    /// <summary>
    /// Wrap a component name string, throwing an exception if it is not registered.
    /// </summary>
    public CompName(string name, IComponentFactory factory)
    {
        if (!factory.HasRegistration(name))
            throw new UnknownComponentException($"Tried to create CompName with unregistered component name '{name}'");
        Name = name;
    }

    private CompName([ForbidLiteral] string name)
    {
        // no checking, only used by Get methods below which are guaranteed to be valid
        Name = name;
    }

    public static implicit operator string(CompName name)
        => name.Name;

    /// <summary>
    /// Get the name of a given component using a <see cref="IComponentFactory"/> and <typeparamref name="T"/>.
    /// </summary>
    public static CompName Get<T>(IComponentFactory factory) where T : IComponent, new()
        => new CompName(factory.GetComponentName<T>());

    /// <summary>
    /// Get the name of a given component using a <see cref="IComponentFactory"/> and a component's <see cref="Type"/>.
    /// Will throw for non-component types.
    /// </summary>
    public static CompName Get(Type type, IComponentFactory factory)
        => new CompName(factory.GetComponentName(type));

    /// <summary>
    /// The underlying string for this component name.
    /// E.g. for <c>TransformComponent</c> this is "Transform".
    /// </summary>
    public string Name { get; private init; }

    public bool Equals(string? other)
        => Name == other;

    public int CompareTo(CompName other)
        => string.Compare(Name, other.Name, StringComparison.Ordinal);

    public string AsType() => Name;

    public override string ToString() => Name;
}
