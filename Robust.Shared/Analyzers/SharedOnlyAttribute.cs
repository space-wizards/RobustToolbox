using System;

namespace Robust.Shared.Analyzers;

/// <summary>
/// <para>
/// Indicates that a member should only be used in Shared assemblies.
/// Attempting to use it in a Client-only or Server-only assembly will raise a warning.
/// </para>
/// <para>
/// This should be used when such usage would be nonsensical or pointless,
/// such as calling <c>INetManager.IsClient</c> in client-only code.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field)]
public sealed class SharedOnlyAttribute : Attribute;
