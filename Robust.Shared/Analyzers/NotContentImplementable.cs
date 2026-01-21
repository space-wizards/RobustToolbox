using System;

namespace Robust.Shared.Analyzers;

/// <summary>
/// Marker attribute specifying that content <b>must not</b> implement this interface itself.
/// </summary>
/// <remarks>
/// Interfaces with this attribute may have members added by the engine at any time,
/// so implementing them yourself would not be API-stable.
/// </remarks>
[AttributeUsage(AttributeTargets.Interface)]
public sealed class NotContentImplementableAttribute : Attribute;
