﻿using System;
#if !ROBUST_ANALYZERS_TEST
using JetBrains.Annotations;
using Robust.Shared.Serialization.Manager.Attributes;
#endif

namespace Robust.Shared.Prototypes;

/// <summary>
/// Quick attribute to give the prototype its type string.
/// To prevent needing to instantiate it because interfaces can't declare statics.
/// </summary>
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
    /// Override for the name of this kind of prototype. If not specified, this is automatically inferred via <see cref="PrototypeManager.CalculatePrototypeName"/>
    /// </summary>
    public string? Type { get; internal set; }
    public readonly int LoadPriority = 1;

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

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
#if !ROBUST_ANALYZERS_TEST
[BaseTypeRequired(typeof(IPrototype))]
[MeansImplicitUse]
[MeansDataDefinition]
[MeansDataRecord]
#endif
public sealed class PrototypeRecordAttribute : PrototypeAttribute
{
    public PrototypeRecordAttribute(string type, int loadPriority = 1) : base(type, loadPriority)
    {
    }
}

