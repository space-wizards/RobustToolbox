using System;

namespace Robust.Shared.Serialization.Manager.Attributes;

/// <summary>
///     Marks all classes or interfaces that inherit from the one with this attribute with
///     <see cref="DataRecordAttribute"/>, without requiring this be done manually.
///     Cannot be reversed by inheritors!
/// </summary>
/// <seealso cref="ImplicitDataDefinitionForInheritorsAttribute"/>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public sealed class ImplicitDataRecordAttribute : Attribute
{
}
