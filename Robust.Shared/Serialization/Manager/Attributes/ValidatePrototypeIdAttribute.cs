using System;
using Robust.Shared.Prototypes;

namespace Robust.Shared.Serialization.Manager.Attributes;

/// <summary>
/// This attribute should be used on static string or string collection fields to validate that they correspond to
/// valid YAML prototype ids. This attribute is not required for static <see cref="ProtoId{T}"/> and
/// <see cref="EntProtoId"/> fields, as they automatically get validated.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class ValidatePrototypeIdAttribute<T> : Attribute where T : IPrototype
{
}
