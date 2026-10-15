using System;

namespace Robust.Shared.Analyzers;

/// <summary>
///     Indicates that the ability to <i>inherit</i> this type is obsolete, and attempting to do so should give a warning.
/// </summary>
/// <remarks>
///     This is useful to gracefully deal with types that should never have had <see cref="VirtualAttribute"/>.
/// </remarks>
/// <example>
/// <code>
///     [ObsoleteInheritance]
///     public class MyClass;
///     <br/>
///     // Warning RA0034: Type 'MyDescendant' inherits from 'MyClass', which has obsoleted inheriting from itself.
///     public sealed class MyDescendant : MyClass;
/// </code>
/// </example>
/// <seealso cref="VirtualAttribute"/>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ObsoleteInheritanceAttribute : Attribute
{
    /// <summary>
    /// An optional message provided alongside this obsoletion.
    /// </summary>
    public string? Message { get; }

    public ObsoleteInheritanceAttribute()
    {
    }

    public ObsoleteInheritanceAttribute(string message)
    {
        Message = message;
    }
}
