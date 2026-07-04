using Robust.Client.Graphics;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Client.GameObjects;

[DataDefinition]
public sealed partial class SpriteShaderData(
    ProtoId<ShaderPrototype> protoId,
    bool raiseShaderEvent = false,
    bool getScreenTexture = false,
    bool mutable = true,
    Color? color = null,
    ShaderInstance? instance = null)
{
    public SpriteShaderData() : this(default)
    {
    }

    /// <summary>
    ///     Proto ID of the shader.
    /// </summary>
    [DataField]
    public ProtoId<ShaderPrototype> ProtoId = protoId;

    /// <summary>
    ///     If true, this raise a entity system event before rendering this sprite, allowing systems to modify the
    ///     shader parameters. Usually this can just be done via a frame-update, but some shaders require
    ///     information about the viewport / eye.
    /// </summary>
    [DataField]
    public bool RaiseShaderEvent = raiseShaderEvent;

    /// <summary>
    ///     Whether to pass the screen texture to the <see cref="Instance"/>.
    /// </summary>
    /// <remarks>
    ///     Should be false unless you really need it.
    /// </remarks>
    [DataField]
    public bool GetScreenTexture = getScreenTexture;

    /// <summary>
    ///     Is the shader mutable
    /// </summary>
    [DataField]
    public bool Mutable = mutable;

    /// <summary>
    ///     Color to apply when rendering shader
    /// </summary>
    [DataField]
    public Color? Color = color;

    public ShaderInstance? Instance = instance;

    public SpriteShaderData Copy()
    {
        var shader = Instance is not { Disposed: true } ? Instance?.Mutable is true ? Instance.Duplicate() : Instance : null;
        return new SpriteShaderData(ProtoId, RaiseShaderEvent, GetScreenTexture, Mutable, Color, shader);
    }
}
