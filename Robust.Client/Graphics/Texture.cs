using System;
using JetBrains.Annotations;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Robust.Client.Graphics
{
    /// <summary>
    ///     Flags for loading of textures.
    /// </summary>
    [PublicAPI]
    public struct TextureLoadParameters
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
    }

    /// <summary>
    ///     Sample flags for textures.
    ///     These are separate from <see cref="TextureLoadParameters"/>,
    ///     because it is possible to create "proxies" to existing textures
    ///     with different sampling parameters than the base texture.
    /// </summary>
    [PublicAPI]
    public struct TextureSampleParameters
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
    }

    /// <summary>
    ///     Controls behavior when reading texture coordinates outside 0-1, which usually wraps the texture somehow.
    /// </summary>
    [PublicAPI]
    public enum TextureWrapMode : byte
    {
        /// <summary>
        ///     Do not wrap, instead clamp to edge.
        /// </summary>
        None = 0,

        /// <summary>
        ///     Repeat the texture.
        /// </summary>
        Repeat,

        /// <summary>
        ///     Repeat the texture mirrored.
        /// </summary>
        MirroredRepeat,
    }
}
