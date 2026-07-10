using System;
using JetBrains.Annotations;

namespace Robust.Shared.GameObjects;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
[BaseTypeRequired(typeof(IComponent))]
public sealed class DenseComponentAttribute : Attribute;
