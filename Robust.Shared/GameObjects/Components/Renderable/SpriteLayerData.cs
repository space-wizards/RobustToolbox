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

    /// <summary>
    /// If set, indicates that this sprite layer should instead be used to copy into shader parameters on another layer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If set, this sprite layer is not rendered. Instead, the "result" of rendering it (exact sprite layer and such)
    /// are copied into the shader parameters of another object,
    /// specified by the <see cref="PrototypeCopyToShaderParameters"/>.
    /// </para>
    /// <para>
    /// The specified layer must have a shader set. When it does, the shader's
    /// </para>
    /// <para>
    /// Note that sprite layers are processed in-order, so to avoid 1-frame delays,
    /// the layer doing the copying should occur BEFORE the layer being copied into.
    /// </para>
    /// </remarks>
    [DataField] public PrototypeCopyToShaderParameters? CopyToShaderParameters;

    [DataField] public bool Cycle;
}

/// <summary>
/// Stores parameters for <see cref="PrototypeLayerData.CopyToShaderParameters"/>.
/// </summary>
[Serializable, NetSerializable, DataDefinition]
public sealed partial class PrototypeCopyToShaderParameters
{
    /// <summary>
    /// The map key of the layer that will have its shader modified.
    /// </summary>
    [DataField(required: true)] public string LayerKey;

    /// <summary>
    /// The name of the shader parameter that will receive the actual selected texture.
    /// </summary>
    [DataField] public string? ParameterTexture;

    /// <summary>
    /// The name of the shader parameter that will receive UVs to select the sprite in <see cref="ParameterTexture"/>.
    /// </summary>
    [DataField] public string? ParameterUV;
}

[Serializable, NetSerializable]
public enum LayerRenderingStrategy
{
    Default,
    SnapToCardinals,
    NoRotation,
    UseSpriteStrategy
}
