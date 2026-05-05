using System;

namespace Robust.Shared.Serialization.Manager.Attributes;

/// <summary>
/// Use this attribute to enable short form versions for derived types in YAML
/// i.e.: !type:MyBaseTypeConcrete into !Concrete.
/// Requires the name of the type to end in 'Base'.
/// </summary>
/// <remarks>Use the <see cref="CustomChildTagAttribute{T}"/> attribute to add
/// support for derived types which do not use the correct naming scheme.</remarks>
[System.AttributeUsage(System.AttributeTargets.Class)]
public sealed class YamlTagShortenerAttribute : Attribute;
