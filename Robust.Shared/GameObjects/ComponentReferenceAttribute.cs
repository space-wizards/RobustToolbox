using System;

namespace Robust.Shared.GameObjects;

/*
 * Obsoleted because this has a massive performance cost and it is not worth it.
 */

/// <summary>
///     Marks a component as having a specific reference type,
///     for use with <see cref="RegisterComponentAttribute"/>.
/// </summary>
[Obsolete("Refactor your code to not use component references.")]
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class ComponentReferenceAttribute : Attribute
{
    /// <summary>
    ///     The type this component is a reference to.
    /// </summary>
    public Type ReferenceType { get; }

    /// <summary>
    ///     Default constructor.
    /// </summary>
    /// <param name="referenceType">The type this component is a reference to.</param>
    public ComponentReferenceAttribute(Type referenceType)
    {
        ReferenceType = referenceType;
    }
}
