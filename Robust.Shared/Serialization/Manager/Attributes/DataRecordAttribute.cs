using System;
using JetBrains.Annotations;

namespace Robust.Shared.Serialization.Manager.Attributes;

/// <summary>
///     Makes all properties in a record data fields with camel case naming.
///     <seealso cref="DataFieldAttribute"/>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
[MeansDataDefinition]
[MeansDataRecord]
[MeansImplicitUse]
public sealed class DataRecordAttribute : Attribute
{
}
