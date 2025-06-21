using System;

namespace Robust.Shared.Analyzers;

/// <summary>
/// Requires that the types of any values passed in as the target
/// method parameter or type argument have the specified Attribute.
/// Note that only sealed types are checked; others are assumed to be proxy method arguments.
/// </summary>
/// <param name="attribute">Required <see cref="System.Attribute"/></param>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.GenericParameter, AllowMultiple = true)]
public sealed class RequiresAttributeAttribute(Type attribute) : Attribute
{
    /// <summary>
    /// The <see cref="System.Attribute"/> that is required.
    /// </summary>
    public Type Attribute { get; private set; } = attribute;
}