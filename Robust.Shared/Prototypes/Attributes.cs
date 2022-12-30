using System;
using JetBrains.Annotations;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.Prototypes;

/// <summary>
/// Quick attribute to give the prototype its type string.
/// To prevent needing to instantiate it because interfaces can't declare statics.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
[BaseTypeRequired(typeof(IPrototype))]
[MeansImplicitUse]
[MeansDataDefinition]
[Virtual]
public class PrototypeAttribute : Attribute
{
    private readonly string type;
    public string Type => type;
    public readonly int LoadPriority = 1;

    public PrototypeAttribute(string type, int loadPriority = 1)
    {
        this.type = type;
        LoadPriority = loadPriority;
    }
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
[BaseTypeRequired(typeof(IPrototype))]
[MeansImplicitUse]
[MeansDataDefinition]
[MeansDataRecord]
public sealed class PrototypeRecordAttribute : PrototypeAttribute
{
    public PrototypeRecordAttribute(string type, int loadPriority = 1) : base(type, loadPriority)
    {
    }
}

