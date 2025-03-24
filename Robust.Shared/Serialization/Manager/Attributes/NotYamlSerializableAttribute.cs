using System;

namespace Robust.Shared.Serialization.Manager.Attributes;

/// <summary>
/// Used to denote that a type is not serializable to yaml, and should not be used as a data-field.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
internal sealed class NotYamlSerializableAttribute : Attribute;
