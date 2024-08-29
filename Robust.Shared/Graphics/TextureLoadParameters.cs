using System;
using JetBrains.Annotations;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Graphics;

/// <summary>
///     Flags for loading of textures.
/// </summary>
[PublicAPI]
public struct TextureLoadParameters : IEquatable<TextureLoadParameters>
{
    /// <summary>
    ///     The default sampling parameters for the texture.
    /// </summary>
    public TextureSampleParameters SampleParameters { get; set; }

    /// <summary>
    ///     If true, the image data will be treated as sRGB.
    /// </summary>
    public bool Srgb { get; set; }

    public static TextureLoadParameters FromYaml(YamlMappingNode yaml)
    {
        var loadParams = Default;
        if (yaml.TryGetNode("sample", out YamlMappingNode? sampleNode))
        {
            loadParams.SampleParameters = TextureSampleParameters.FromYaml(sampleNode);
        }

        if (yaml.TryGetNode("srgb", out var srgb))
        {
            loadParams.Srgb = srgb.AsBool();
        }

        return loadParams;
    }

    public static readonly TextureLoadParameters Default = new()
    {
        SampleParameters = TextureSampleParameters.Default,
        Srgb = true
    };

    public bool Equals(TextureLoadParameters other)
    {
        return SampleParameters.Equals(other.SampleParameters) && Srgb == other.Srgb;
    }

    public override bool Equals(object? obj)
    {
        return obj is TextureLoadParameters other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(SampleParameters, Srgb);
    }

    public static bool operator ==(TextureLoadParameters left, TextureLoadParameters right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TextureLoadParameters left, TextureLoadParameters right)
    {
        return !left.Equals(right);
    }
}
