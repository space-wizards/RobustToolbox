using System;
#if !ROBUST_ANALYZERS_TEST
using JetBrains.Annotations;
using Robust.Shared.Serialization.Manager.Attributes;
#endif

namespace Robust.Shared.Prototypes;

/// <summary>
///     Defines the unique type id ("kind") and load priority for a prototype, and registers it so the game knows about
///     the type in question during init and can deserialize it.
///     <br/>
/// </summary>
/// <include file='../Serialization/Manager/Attributes/Docs.xml' path='entries/entry[@name="ImpliesDataDefinition"]/*'/>
/// <seealso cref="PrototypeRecordAttribute"/>
/// <seealso cref="IdDataFieldAttribute"/>
/// <seealso cref=""/>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
#if !ROBUST_ANALYZERS_TEST
[BaseTypeRequired(typeof(IPrototype))]
[MeansImplicitUse]
[MeansDataDefinition]
[Virtual]
#endif
public class PrototypeAttribute : Attribute
{
    /// <summary>
    ///     Override for the name of this kind of prototype.
    ///     If not specified, this is automatically inferred via <see cref="PrototypeManager.CalculatePrototypeName"/>
    /// </summary>
    public string? Type { get; internal set; }

    /// <summary>
    ///     Defines the load order for prototype kinds. Higher priorities are loaded earlier.
    /// </summary>
    public readonly int LoadPriority;

    /// <param name="type">See <see cref="Type"/>.</param>
    /// <param name="loadPriority">See <see cref="LoadPriority"/>.</param>
    public PrototypeAttribute(string? type = null, int loadPriority = 1)
    {
        Type = type;
        LoadPriority = loadPriority;
    }

    public PrototypeAttribute(int loadPriority)
    {
        Type = null;
        LoadPriority = loadPriority;
    }
}

/// <summary>
///     Defines the unique type id ("kind") and load priority for a prototype, and registers it so the game knows about
///     the type in question during init and can deserialize it.
///     <br/>
/// </summary>
/// <include file='../Serialization/Manager/Attributes/Docs.xml' path='entries/entry[@name="ImpliesDataRecord"]/*'/>
/// <seealso cref="PrototypeAttribute"/>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
#if !ROBUST_ANALYZERS_TEST
[BaseTypeRequired(typeof(IPrototype))]
[MeansImplicitUse]
[MeansDataDefinition]
[MeansDataRecord]
#endif
public sealed class PrototypeRecordAttribute : PrototypeAttribute
{
    /// <param name="type">See <see cref="PrototypeAttribute.Type"/>.</param>
    /// <param name="loadPriority">See <see cref="PrototypeAttribute.LoadPriority"/>.</param>
    public PrototypeRecordAttribute(string type, int loadPriority = 1) : base(type, loadPriority)
    {
    }
}

