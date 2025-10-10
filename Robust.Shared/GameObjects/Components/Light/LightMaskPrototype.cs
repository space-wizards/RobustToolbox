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
[Prototype]
public sealed partial class LightMaskPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// File path to the mask image that will be applied to the light for rendering.
    /// </summary>
    [DataField(required: true)]
    public ResPath MaskPath;

    /// <summary>
    /// Light of light cones that correspond to the light mask.
    /// </summary>
    [DataField(required: true)]
    public List<LightConeData> LightCones = new();

}

/// <summary>
/// Container class that holds data for a light cone.
/// This Data is used by <see cref="LightSensitiveSystem"/>
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public partial record struct LightConeData
{
    /// <summary>
    ///     The angle offset of the cone in degrees, relative to the light it belongs to.
    ///     0 is forward, 90 is to the right, etc.
    /// </summary>
    [DataField]
    public float Direction;

    /// <summary>
    ///     Angle limit of the cone to be within the full brightness of the light.
    /// </summary>
    [DataField]
    public float InnerWidth;

    /// <summary>
    ///     Angle limit of the cone to be within the reduced light. Beyond this angle, you are considered out of the light.
    /// </summary>
    [DataField]
    public float OuterWidth;

    public LightConeData(float direction, float innerWidth, float outerWidth)
    {
        Direction = direction;
        InnerWidth = innerWidth;
        OuterWidth = outerWidth;
    }

}
