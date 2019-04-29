using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Utility;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Robust.Client.Graphics
{
    /// <summary>
    ///     Contains a texture used for drawing things.
    /// </summary>
    [PublicAPI]
    public abstract class Texture : IDirectionalTextureProvider
    {
        internal abstract Godot.Texture GodotTexture { get; }

        /// <summary>
        ///     The width of the texture, in pixels.
        /// </summary>
        public abstract int Width { get; }

        /// <summary>
        ///     The height of the texture, in pixels.
        /// </summary>
        public abstract int Height { get; }

        public Vector2i Size => new Vector2i(Width, Height);

        public static implicit operator Godot.Texture(Texture src)
        {
            return src?.GodotTexture;
        }

        public static Texture Transparent { get; internal set; }
        public static Texture White { get; internal set; }

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
        public static Texture LoadFromImage<T>(Image<T> image, string name = null,
            TextureLoadParameters? loadParameters = null) where T : unmanaged, IPixel<T>
        {
            switch (GameController.Mode)
            {
                case GameController.DisplayMode.Headless:
                    return new DummyTexture(image.Width, image.Height);
                case GameController.DisplayMode.Godot:
                {
                    var stream = new MemoryStream();

                    try
                    {
                        image.SaveAsPng(stream, new PngEncoder {CompressionLevel = 1});

                        var gdImage = new Godot.Image();
                        var ret = gdImage.LoadPngFromBuffer(stream.ToArray());
                        if (ret != Godot.Error.Ok)
                        {
                            throw new InvalidDataException(ret.ToString());
                        }

                        // Godot does not provide a way to load from memory directly so we turn it into a PNG I guess.
                        var texture = new Godot.ImageTexture();
                        texture.CreateFromImage(gdImage);
                        (loadParameters ?? TextureLoadParameters.Default).SampleParameters.ApplyToGodotTexture(texture);
                        return new GodotTextureSource(texture);
                    }
                    finally
                    {
                        stream.Dispose();
                    }
                }
                case GameController.DisplayMode.Clyde:
                {
                    var manager = IoCManager.Resolve<IClyde>();
                    return manager.LoadTextureFromImage(image, name);
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
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
        public static Texture LoadFromPNGStream(Stream stream, string name = null,
            TextureLoadParameters? loadParameters = null)
        {
            switch (GameController.Mode)
            {
                case GameController.DisplayMode.Headless:
                    return new DummyTexture();
                case GameController.DisplayMode.Godot:
                    using (var memoryStream = new MemoryStream())
                    {
                        stream.CopyTo(memoryStream);

                        var data = memoryStream.ToArray();
                        var gdImage = new Godot.Image();
                        var ret = gdImage.LoadPngFromBuffer(data);
                        if (ret != Godot.Error.Ok)
                        {
                            throw new InvalidDataException(ret.ToString());
                        }

                        var texture = new Godot.ImageTexture();
                        texture.CreateFromImage(gdImage);
                        (loadParameters ?? TextureLoadParameters.Default).SampleParameters.ApplyToGodotTexture(texture);
                        return new GodotTextureSource(texture);
                    }
                case GameController.DisplayMode.Clyde:
                {
                    var manager = IoCManager.Resolve<IClyde>();
                    return manager.LoadTextureFromPNGStream(stream, name, loadParameters);
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static TextureArray LoadArrayFromImages<T>(ICollection<Image<T>> images, string name = null,
            TextureLoadParameters? loadParameters = null)
            where T : unmanaged, IPixel<T>
        {
            if (GameController.Mode != GameController.DisplayMode.Clyde)
            {
                throw new NotImplementedException();
            }

            var manager = IoCManager.Resolve<IClyde>();
            return manager.LoadArrayFromImages(images, name, loadParameters);
        }

        Texture IDirectionalTextureProvider.Default => this;

        Texture IDirectionalTextureProvider.TextureFor(Direction dir)
        {
            return this;
        }
    }

    public sealed class TextureArray : IReadOnlyList<Texture>
    {
        public Texture this[int index] => _subTextures[index];
        private readonly OpenGLTexture[] _subTextures;

        public int Count => _subTextures.Length;

        internal TextureArray(OpenGLTexture[] subTextures)
        {
            _subTextures = subTextures;
        }

        public IEnumerator<Texture> GetEnumerator()
        {
            return ((IReadOnlyCollection<Texture>) _subTextures).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    internal sealed class DummyTexture : Texture
    {
        internal override Godot.Texture GodotTexture => null;

        public override int Width { get; }
        public override int Height { get; }

        public DummyTexture(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public DummyTexture()
        {
            DebugTools.Assert(GameController.Mode == GameController.DisplayMode.Headless);
        }
    }

    /// <summary>
    ///     Represents a sub region of another texture.
    ///     This can be a useful optimization in many cases.
    /// </summary>
    [PublicAPI]
    public sealed class AtlasTexture : Texture
    {
        public AtlasTexture(Texture texture, UIBox2 subRegion)
        {
            DebugTools.Assert(SubRegion.Right < texture.Width);
            DebugTools.Assert(SubRegion.Bottom < texture.Height);
            DebugTools.Assert(SubRegion.Left >= 0);
            DebugTools.Assert(SubRegion.Top >= 0);

            if (GameController.OnGodot)
            {
                GodotTexture = new Godot.AtlasTexture
                {
                    Atlas = texture,
                    Region = subRegion.Convert()
                };
            }

            SubRegion = subRegion;
            SourceTexture = texture;
        }

        internal override Godot.Texture GodotTexture { get; }

        /// <summary>
        ///     The texture this texture is a sub region of.
        /// </summary>
        public Texture SourceTexture { get; }

        /// <summary>
        ///     Our sub region within our source, in pixel coordinates.
        /// </summary>
        public UIBox2 SubRegion { get; }

        public override int Width => (int) SubRegion.Width;
        public override int Height => (int) SubRegion.Height;
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

        public static TextureLoadParameters FromYaml(YamlMappingNode yaml)
        {
            if (yaml.TryGetNode("sample", out YamlMappingNode sampleNode))
            {
                return new TextureLoadParameters {SampleParameters = TextureSampleParameters.FromYaml(sampleNode)};
            }

            return Default;
        }

        public static readonly TextureLoadParameters Default = new TextureLoadParameters
        {
            SampleParameters = TextureSampleParameters.Default
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

        internal void ApplyToGodotTexture(Godot.Texture texture)
        {
            var flags = texture.Flags;
            if (Filter)
            {
                flags |= (int) Godot.Texture.FlagsEnum.Filter;
            }
            else
            {
                flags &= ~(int) Godot.Texture.FlagsEnum.Filter;
            }

            switch (WrapMode)
            {
                case TextureWrapMode.None:
                    flags &= ~(int) Godot.Texture.FlagsEnum.Repeat;
                    flags &= ~(int) Godot.Texture.FlagsEnum.MirroredRepeat;
                    break;
                case TextureWrapMode.Repeat:
                    flags |= (int) Godot.Texture.FlagsEnum.Repeat;
                    flags &= ~(int) Godot.Texture.FlagsEnum.MirroredRepeat;
                    break;
                case TextureWrapMode.MirroredRepeat:
                    flags &= ~(int) Godot.Texture.FlagsEnum.Repeat;
                    flags |= (int) Godot.Texture.FlagsEnum.MirroredRepeat;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            texture.Flags = flags;
        }

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

        public static readonly TextureSampleParameters Default = new TextureSampleParameters
        {
            Filter = false,
            WrapMode = TextureWrapMode.None
        };
    }

    /// <summary>
    ///     Controls behavior when reading texture coordinates outside 0-1, which usually wraps the texture somehow.
    /// </summary>
    [PublicAPI]
    public enum TextureWrapMode
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

    internal class OpenGLTexture : Texture
    {
        internal override Godot.Texture GodotTexture => null;
        internal int OpenGLTextureId { get; }
        internal int ArrayIndex { get; }

        public override int Width { get; }
        public override int Height { get; }

        internal OpenGLTexture(int id, int width, int height)
        {
            OpenGLTextureId = id;
            Width = width;
            Height = height;
        }

        internal OpenGLTexture(int id, int width, int height, int arrayIndex)
        {
            OpenGLTextureId = id;
            Width = width;
            Height = height;
            ArrayIndex = arrayIndex;
        }
    }

    /// <summary>
    ///     Wraps a texture returned by Godot itself,
    ///     for example when the texture was set in a GUI scene.
    /// </summary>
    internal class GodotTextureSource : Texture
    {
        internal override Godot.Texture GodotTexture { get; }
        public override int Width => GodotTexture.GetWidth();
        public override int Height => GodotTexture.GetHeight();

        public GodotTextureSource(Godot.Texture texture)
        {
            DebugTools.Assert(GameController.OnGodot);
            GodotTexture = texture;
        }
    }
}
