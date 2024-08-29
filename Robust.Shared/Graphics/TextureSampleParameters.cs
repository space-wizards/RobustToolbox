using System;
using JetBrains.Annotations;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Graphics;

/// <summary>
///     Sample flags for textures.
///     These are separate from <see cref="TextureLoadParameters"/>,
///     because it is possible to create "proxies" to existing textures
///     with different sampling parameters than the base texture.
/// </summary>
[PublicAPI]
public struct TextureSampleParameters : IEquatable<TextureSampleParameters>
{
    // NOTE: If somebody is gonna add support for 3D/1D textures, change this doc comment.
    // See the note on this page for why: https://www.khronos.org/opengl/wiki/Sampler_Object#Filtering
    /// <summary>
    ///     If true, use bi-linear texture filtering if the texture cannot be rendered 1:1
    /// </summary>
    public bool Filter { get; set; }

    /// <summary>
    ///     Controls how to wrap the texture if texture coordinates outside 0-1 are accessed.
    /// </summary>
    public TextureWrapMode WrapMode { get; set; }

    public static TextureSampleParameters FromYaml(YamlMappingNode node)
    {
        var wrap = TextureWrapMode.None;
        var filter = false;

        if (node.TryGetNode("filter", out var filterNode))
        {
            filter = filterNode.AsBool();
        }

        if (node.TryGetNode("wrap", out var wrapNode))
        {
            switch (wrapNode.AsString())
            {
                case "none":
                    wrap = TextureWrapMode.None;
                    break;
                case "repeat":
                    wrap = TextureWrapMode.Repeat;
                    break;
                case "mirrored_repeat":
                    wrap = TextureWrapMode.MirroredRepeat;
                    break;
                default:
                    throw new ArgumentException("Not a valid wrap mode.");
            }
        }

        return new TextureSampleParameters {Filter = filter, WrapMode = wrap};
    }

    public static readonly TextureSampleParameters Default = new()
    {
        Filter = false,
        WrapMode = TextureWrapMode.None
    };

    public bool Equals(TextureSampleParameters other)
    {
        return Filter == other.Filter && WrapMode == other.WrapMode;
    }

    public override bool Equals(object? obj)
    {
        return obj is TextureSampleParameters other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Filter, (int) WrapMode);
    }

    public static bool operator ==(TextureSampleParameters left, TextureSampleParameters right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TextureSampleParameters left, TextureSampleParameters right)
    {
        return !left.Equals(right);
    }
}
