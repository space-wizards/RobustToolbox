using System;

namespace Robust.Shared.Serialization.Manager.Attributes;

/// <summary>
///     Used to denote that a type is not serializable to yaml, and should not be used as a data-field.
///     Types marked with this will cause an error early in serialization init if used as a field.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
internal sealed class NotYamlSerializableAttribute : Attribute;
