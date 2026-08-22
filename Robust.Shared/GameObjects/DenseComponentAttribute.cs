using System;
using JetBrains.Annotations;

namespace Robust.Shared.GameObjects;

/// <summary>
///     Makes component operations using this component type use an array lookup instead of a dictionary lookup
///     where possible, using more memory but improving performance for components that are used very often, such as
///     <see cref="TransformComponent"/> and <see cref="MetaDataComponent"/>
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
[BaseTypeRequired(typeof(IComponent))]
public sealed class DenseComponentAttribute : Attribute;
