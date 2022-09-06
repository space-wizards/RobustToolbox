using System;
using JetBrains.Annotations;

namespace Robust.Shared.Serialization.Manager.Attributes;

/// <summary>
///     <seealso cref="DataRecordAttribute"/>
/// </summary>
[BaseTypeRequired(typeof(Attribute))]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class MeansDataRecordAttribute : Attribute
{
}
