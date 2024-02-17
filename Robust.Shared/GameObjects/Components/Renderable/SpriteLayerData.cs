using System;
using System.Collections.Generic;
using System.Numerics;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

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

    /// <summary>
    /// A drawdepth for this layer specifically. If null, then the one on SpriteComponent will be used.
    /// </summary>
    [DataField(customTypeSerializer: typeof(ConstantSerializer<DrawDepth>))]
    public int? DrawDepth;

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
