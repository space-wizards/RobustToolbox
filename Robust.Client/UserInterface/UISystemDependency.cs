using System;

namespace Robust.Client.UserInterface;

/// <summary>
///     Attribute applied to EntitySystem-typed fields inside UIControllers that should be
///     injected when the system becomes available.
/// </summary>
public sealed class UISystemDependency : Attribute
{
}
