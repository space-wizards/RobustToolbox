using System;

namespace Robust.Shared.Analyzers;

/// <summary>
/// Specify that this class is allowed to be inherited.
/// </summary>
/// <remarks>
/// Robust uses analyzers to prevent accidental usage of non-sealed classes:
/// a class must be either marked [Virtual], abstract, or sealed.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class VirtualAttribute : Attribute
{
}
