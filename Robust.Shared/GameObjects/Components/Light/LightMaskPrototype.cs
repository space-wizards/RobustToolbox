using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects;

[Prototype("lightMask")]
public sealed partial class LightMaskPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    [ViewVariables(VVAccess.ReadWrite), DataField("maskPath", required: true)]
    public ResPath MaskPath;
    
    // [ViewVariables(VVAccess.ReadWrite), DataField("coneAngle", required: true)]
    // public float ConeAngle = 0.0f;

    // [ViewVariables(VVAccess.ReadWrite), DataField("cones")]
    // public Dictionary<float, float> cones = new();

    [ViewVariables(VVAccess.ReadWrite), DataField("cones")]
    public List<LightConeData> Cones = new();

}

[DataDefinition]
public sealed partial class LightConeData
{
    [DataField("direction", required: true)] 
    public float Direction;

    /// <summary>
    ///     
    /// </summary>
    [DataField("innerWidth", required: true)] 
    public float InnerWidth;

    [DataField("outerWidth", required: true)] 
    public float OuterWidth;

}
