using System;

#if ROBUST_ANALYZERS_IMPL
namespace Robust.Shared.Analyzers.Implementation;
#else
namespace Robust.Shared.Analyzers;
#endif

/// <summary>
///     Indicates that the marked method has an alternative version that takes the Type input as a generic,
///     and warns the user to use the generic version instead if they use <see langword="typeof"/>.
/// </summary>
/// <example>
/// <code>
///     public sealed MyClass
///     {
///         [PreferGenericVariant]
///         public static bool IsPastry(Type t);
///         public static bool IsPastry&lt;T&gt;();
///     }
///     <br/>
///     // Warning RA0005: Consider using the generic variant of this method to avoid potential allocations.
///     MyClass.IsPastry(typeof(Cupcake));
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method)]
public sealed class PreferGenericVariantAttribute : Attribute
{
    public readonly string GenericVariant;

    public PreferGenericVariantAttribute(string genericVariant = null!)
    {
        GenericVariant = genericVariant;
    }
}
