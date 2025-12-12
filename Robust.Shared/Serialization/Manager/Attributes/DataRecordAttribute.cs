using System;
using JetBrains.Annotations;

namespace Robust.Shared.Serialization.Manager.Attributes;

/// <include file='Docs.xml' path='entries/entry[@name="MeansDataDefinitionHaver"]/*'/>
/// <summary>
///     Additionally marks all properties on the class with <see cref="DataFieldAttribute"/> automatically, for use with
///     records.
/// </summary>
/// <example>
///     <code>
///         [DataRecord]
///         public sealed record MyRecord(int Foo, bool Bar);
///     </code>
///     which has the serialized yaml equivalent of:
///     <code>
///         foo: 0
///         bar: false
///     </code>
/// </example>
/// <seealso cref="DataDefinitionAttribute"/>
/// <seealso cref="DataFieldAttribute"/>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
[MeansDataDefinition]
[MeansDataRecord]
[MeansImplicitUse]
public sealed class DataRecordAttribute : Attribute
{
}
