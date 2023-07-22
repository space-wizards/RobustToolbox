using System;
using Robust.Shared.Prototypes;

namespace Robust.Shared.Serialization.Manager.Attributes;

/// <summary>
/// This attribute should be used on string constants to validate that they correspond to a valid YAML prototype id.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class PrototypeIdAttribute<T> : Attribute where T : IPrototype
{
}
