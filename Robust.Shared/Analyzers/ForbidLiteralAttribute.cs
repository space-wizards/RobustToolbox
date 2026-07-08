using System;

namespace Robust.Shared.Analyzers;

/// <summary>
///     Marks that values used for this parameter should not be literal values.
///     This helps prevent magic numbers/strings/etc, by indicating that values
///     should either be wrapped (for validation) or defined as constants or readonly statics.
/// </summary>
/// <example>
/// <code>
///     public sealed class MyClass
///     {
///         public static bool IsPastry([ForbidLiteral] string id);
///         public static string GrabFromCupboard();
///     }
///     <br/>
///     <br/>
///     // Error RA0033: The id parameter of IsPastry forbids literal values.
///     DebugTools.Assert(MyClass.IsPastry("cupcake"));
///     <br/>
///     var maybePastry = obj.GrabFromCupboard();
///     // Allowed.
///     DebugTools.Assert(MyClass.IsPastry(maybePastry));
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class ForbidLiteralAttribute : Attribute;
