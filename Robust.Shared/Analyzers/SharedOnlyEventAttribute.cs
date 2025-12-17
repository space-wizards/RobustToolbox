using System;

namespace Robust.Shared.Analyzers;

/// <summary>
/// Indicates that this event should only be subscribed to in Shared code.
/// Attempting to subscribe to it in Server or Client code will raise a warning.
/// </summary>
/// <param name="allowClientOnly">If true, subscriptions to this event are also allowed in Client-only code.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class SharedOnlyEventAttribute(bool allowClientOnly = true) : Attribute
{
    /// <summary>
    /// If true, subscriptions to this event are also allowed in Client-only code.
    /// </summary>
    public readonly bool AllowClientOnly = allowClientOnly;
}
