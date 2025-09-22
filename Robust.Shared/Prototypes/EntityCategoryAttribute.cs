using System;

namespace Robust.Shared.Prototypes;

/// <summary>
/// Attribute that can be applied to components to force any entity prototypes with that component to automatically
/// get added to an <see cref="EntityCategoryPrototype"/> instance.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class EntityCategoryAttribute(params string[] categories) : Attribute
{
    public readonly string[] Categories = categories;
}
