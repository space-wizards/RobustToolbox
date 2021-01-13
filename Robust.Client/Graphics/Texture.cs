using System;
using System.IO;
using JetBrains.Annotations;
using Robust.Client.Interfaces.Graphics;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using YamlDotNet.RepresentationModel;

namespace Robust.Client.Graphics
{
    /// <summary>
    ///     Contains a texture used for drawing things.
    /// </summary>
    [PublicAPI]
    public abstract class Texture : IDirectionalTextureProvider
    {
        /// <summary>
        ///     The width of the texture, in pixels.
        /// </summary>
        public int Width => Size.X;

        /// <summary>
        ///     The height of the texture, in pixels.
        /// </summary>
        public int Height => Size.Y;

        /// <summary>
        ///     The size of the texture, in pixels.
        /// </summary>
        public Vector2i Size { get; /*protected set;*/ }

        protected Texture(Vector2i size)
        {
            Size = size;
        }

        public static Texture Transparent =>
            IoCManager.Resolve<IClydeInternal>().GetStockTexture(ClydeStockTexture.Transparent);

        public static Texture White =>
            IoCManager.Resolve<IClydeInternal>().GetStockTexture(ClydeStockTexture.White);

        public static Texture Black =>
            IoCManager.Resolve<IClydeInternal>().GetStockTexture(ClydeStockTexture.Black);

        /// <summary>
        ///     Loads a new texture an existing image.
        /// </summary>
        /// <param name="image">The image to load.</param>
        /// <param name="name">The "name" of this texture. This can be referred to later to aid debugging.</param>
        /// <param name="loadParameters">
        ///     Parameters that influence the loading of textures.
        ///     Defaults to <see cref="TextureLoadParameters.Default"/> if <c>null</c>.
        /// </param>
        /// <typeparam name="T">The type of pixels of the image. At the moment, images must be <see cref="Rgba32"/>.</typeparam>
        public static Texture LoadFromImage<T>(Image<T> image, string? name = null,
            TextureLoadParameters? loadParameters = null) where T : unmanaged, IPixel<T>
        {
            var manager = IoCManager.Resolve<IClyde>();
            return manager.LoadTextureFromImage(image, name, loadParameters);
        }

        /// <summary>
        ///     Loads an image from a stream containing PNG data.
        /// </summary>
        /// <param name="stream">The stream to load the image from.</param>
        /// <param name="name">The "name" of this texture. This can be referred to later to aid debugging.</param>
        /// <param name="loadParameters">
        ///     Parameters that influence the loading of textures.
        ///     Defaults to <see cref="TextureLoadParameters.Default"/> if <c>null</c>.
        /// </param>
        public static Texture LoadFromPNGStream(Stream stream, string? name = null,
            TextureLoadParameters? loadParameters = null)
        {
            var manager = IoCManager.Resolve<IClyde>();
            return manager.LoadTextureFromPNGStream(stream, name, loadParameters);
        }

        Texture IDirectionalTextureProvider.Default => this;

        Texture IDirectionalTextureProvider.TextureFor(Direction dir)
        {
            return this;
        }
    }

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
            var loadParams = TextureLoadParameters.Default;
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
