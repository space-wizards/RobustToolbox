using System;

#if ROBUST_ANALYZERS_IMPL
namespace Robust.Shared.Analyzers.Implementation;
#else
namespace Robust.Shared.Analyzers;
#endif

/// <summary>
///     Marks that use of a generic Type should be replaced with a specific other Type
///     when the type argument T is a certain Type.
/// </summary>
/// <param name="genericType">The type that, when used as the sole generic argument, should trigger the warning.</param>
/// <param name="replacementType">The type that you should replace the usage with.</param>
/// <example>
/// <code>
///     [PreferOtherTypeAttribute(typeof(int), typeof(MySpecializedType))]
///     public sealed record MyGeneralType&lt;T&gt;(T Field);
///     <br/>
///     public sealed record MySpecializedType(int Field);
///     <br/>
///     // Warning RA0031: Use the specific type MySpecializedType instead of MyGeneralType when the type argument is int.
///     var obj = new MyGeneralType&lt;int&gt;(42);
///     <br/>
///     // No warning.
///     var obj = new MySpecializedType(42);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public sealed class PreferOtherTypeAttribute(Type genericType, Type replacementType) : Attribute
{
    public readonly Type GenericArgument = genericType;
    public readonly Type ReplacementType = replacementType;
}
