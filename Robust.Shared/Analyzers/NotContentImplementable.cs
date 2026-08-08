using System;

namespace Robust.Shared.Analyzers;

/// <summary>
///     Marker attribute specifying that content <b>must not</b> implement this interface itself.
/// </summary>
/// <remarks>
/// <para>
///     Interfaces with this attribute may have members added by the engine at any time,
///     so implementing them yourself would not be API-stable.
/// </para>
/// <para>
///     Currently, nothing enforces this, but your codebase may spontaneously cease building in any minor version if
///     you inherit from or implement anything marked with this attribute.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Interface)]
public sealed class NotContentImplementableAttribute : Attribute;
