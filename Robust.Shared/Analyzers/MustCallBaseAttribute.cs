using System;

namespace Robust.Shared.Analyzers;

/// <summary>
/// Indicates that overriders of this method must always call the base function.
/// </summary>
/// <param name="onlyOverrides">
/// If true, only base calls to *overrides* are necessary.
/// This is intended for base classes where the base function is always empty,
/// so a base call from the first override may be ommitted.
/// </param>
[AttributeUsage(AttributeTargets.Method)]
public sealed class MustCallBaseAttribute(bool onlyOverrides = false) : Attribute
{
    public bool OnlyOverrides { get; } = onlyOverrides;
}
