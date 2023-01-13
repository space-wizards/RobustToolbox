using System;
using JetBrains.Annotations;

namespace Robust.Shared.GameObjects;

/// <summary>
///     When a component is marked with this attribute, any members it has marked with <see cref="AutoNetworkedFieldAttribute"/>
///     will automatically be replicated using component states to clients. Systems which need to have more intelligent
///     component state replication beyond just directly setting variables should not use this attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
[BaseTypeRequired(typeof(Component))]
public sealed class AutoGenerateComponentStateAttribute : Attribute
{
}

/// <summary>
///     Used to mark component members which should be automatically replicated, assuming the component is marked with
///     <see cref="AutoGenerateComponentStateAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class AutoNetworkedFieldAttribute : Attribute
{
}
