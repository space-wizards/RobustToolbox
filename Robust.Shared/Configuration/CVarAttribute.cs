using System;
using JetBrains.Annotations;

namespace Robust.Shared.Configuration;

/// <summary>
/// Marks a field or property to be automatically assigned when the associated CVar value changes.
/// </summary>
/// <typeparam name="T">The type containing the CVar definition.</typeparam>
/// <param name="cVarPath">The name of the field of the CVar definition on <see cref="T"/></param>
/// <seealso cref="IConfigurationManagerInternal.RegisterCVarAttributesInternal"/>
/// <example>
/// How to use this to read the initial value of, and any changes to, <see cref="CVars.NetMaxConnections"/>
/// <code>
///     [CVar&lt;CVars&gt;(nameof(CVars.NetMaxConnections))]
///     public int Value;
///
///     [CVar&lt;CVars&gt;(nameof(CVars.NetMaxConnections))]
///     public void Test(int v)
///     {
///         Assert.That(v, Is.EqualTo(Value));
///     }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
[MeansImplicitAssignment]
[MeansImplicitUse(ImplicitUseKindFlags.Assign)]
public sealed class CVarAttribute<T>(string cVarPath) : CVarAttribute(typeof(T), cVarPath);

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
public abstract class CVarAttribute(Type type, string cVarPath) : Attribute
{
    internal readonly Type Type = type;
    internal readonly string CVarPath = cVarPath;
}
