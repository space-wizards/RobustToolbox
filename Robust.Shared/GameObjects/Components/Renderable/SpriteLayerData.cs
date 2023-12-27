using System;
using System.Collections.Generic;
using System.Numerics;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.GameObjects;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class PrototypeLayerData
{
    /// <summary>
    /// The shader prototype to use for this layer.
    /// </summary>
    /// <remarks>
    /// Null implies no shader is specified. An empty string will clear the current shader.
    /// </remarks>
    [DataField("shader")] public string? Shader;

    [DataField("texture")] public string? TexturePath;
    [DataField("sprite")] public string? RsiPath;
    [DataField("state")] public string? State;
    [DataField("scale")] public Vector2? Scale;
    [DataField("rotation")] public Angle? Rotation;
    [DataField("offset")] public Vector2? Offset;
    [DataField("visible")] public bool? Visible;
    [DataField("color")] public Color? Color;
    [DataField("map")] public HashSet<string>? MapKeys;
    [DataField("renderingStrategy")] public LayerRenderingStrategy? RenderingStrategy;

    [DataField] public bool Cycle;
}

[Serializable, NetSerializable]
public enum LayerRenderingStrategy
{
    Default,
    SnapToCardinals,
    NoRotation,
    UseSpriteStrategy
}
