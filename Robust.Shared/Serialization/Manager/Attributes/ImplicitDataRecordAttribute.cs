using System;

namespace Robust.Shared.Serialization.Manager.Attributes;

/// <summary>
///     Makes any inheritors data records.
///     <seealso cref="DataRecordAttribute"/>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public sealed class ImplicitDataRecordAttribute : Attribute
{
}
