using System;

#if ROBUST_ANALYZERS_IMPL
namespace Robust.Shared.Analyzers.Implementation;
#else
namespace Robust.Shared.Analyzers;
#endif

/// <summary>
///     Indicates that the user should prefer to use non-generic, special methods for the given generic type arguments.
/// </summary>
/// <example>
/// <code>
///     public sealed MyClass
///     {
///         [PreferNonGenericVariantFor(typeof(Cupcake))]
///         public static string DescribeFood&lt;T&gt;(T food);
///         public static string DescribeCupcake(Cupcake food);
///     }
///     <br/>
///     // Warning RA0030: Use the non-generic variant of this method for type Cupcake.
///     MyClass.DescribeFood&lt;Cupcake&gt;(new Cupcake());
///     <br/>
///     // No warning
///     MyClass.DescribeCupcake(new Cupcake());
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method)]
public sealed class PreferNonGenericVariantForAttribute : Attribute
{
    /// <summary>
    ///     The types to recommend using non-generic methods for.
    /// </summary>
    public readonly Type[] ForTypes;

    public PreferNonGenericVariantForAttribute(params Type[] forTypes)
    {
        ForTypes = forTypes;
    }
}
