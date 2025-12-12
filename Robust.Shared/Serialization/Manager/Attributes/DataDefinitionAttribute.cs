using System;
using JetBrains.Annotations;

namespace Robust.Shared.Serialization.Manager.Attributes;

/// <include file='Docs.xml' path='entries/entry[@name="MeansDataDefinitionHaver"]/*'/>
/// <include file='Docs.xml' path='entries/entry[@name="DataDefinitionExample"]/*'/>
/// <remarks>
///     Has no relation to <see cref="T:System.SerializableAttribute"/>, which is only used with
///     <see cref="T:Robust.Shared.Serialization.NetSerializableAttribute"/> in RobustToolbox games.
/// </remarks>
/// <seealso cref="DataFieldAttribute"/>
/// <seealso cref="DataRecordAttribute"/>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
[MeansDataDefinition]
[MeansImplicitUse]
public sealed class DataDefinitionAttribute : Attribute
{
}
