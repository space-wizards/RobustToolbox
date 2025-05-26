using System;

namespace Robust.Shared.Analyzers;

/// <summary>
/// Marks that values used for this parameter should not be literal values.
/// This helps prevent magic numbers/strings/etc, by indicating that values
/// should either be wrapped (for validation) or defined as constants or readonly statics.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class ForbidLiteralAttribute : Attribute;
