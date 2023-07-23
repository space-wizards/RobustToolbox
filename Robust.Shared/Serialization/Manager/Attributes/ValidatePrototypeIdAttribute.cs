using System;
using Robust.Shared.Prototypes;

namespace Robust.Shared.Serialization.Manager.Attributes;

/// <summary>
/// This attribute should be used on string fields to validate that they correspond to a valid YAML prototype id.
/// If the field needs to be have a default value.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class ValidatePrototypeIdAttribute<T> : Attribute where T : IPrototype
{
}
