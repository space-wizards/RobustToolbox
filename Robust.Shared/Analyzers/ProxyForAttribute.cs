using System;

namespace Robust.Shared.Analyzers;

/// <summary>
/// Indicates that a method is a proxy method that can and should be used as a shortcut
/// for calling a method in another class. This will cause a compiler warning on any code
/// within the descendants of this class that attempts to call the target method directly
/// instead of using the proxy method.
/// The proxy method must have the same parameters as the target method.
/// </summary>
/// <param name="type"><see cref="System.Type"/> containing the target method.</param>
/// <param name="method">Name of the target method. If null, the name of the proxy method will be used.</param>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ProxyForAttribute(Type type, string? method = null) : Attribute
{
    /// <summary>
    /// <see cref="System.Type"/> containing the target method.
    /// </summary>
    public Type Type = type;

    /// <summary>
    /// Name of the target method. If null, the name of the proxy method will be used.
    /// </summary>
    public string? Method = method;
}
