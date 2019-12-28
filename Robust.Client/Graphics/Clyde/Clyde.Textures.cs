using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using OpenTK.Graphics.OpenGL;
using Robust.Client.Interfaces.Graphics;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using OGLTextureWrapMode = OpenTK.Graphics.OpenGL.TextureWrapMode;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        private ClydeTexture _stockTextureWhite;
        private ClydeTexture _stockTextureTransparent;

        private readonly Dictionary<ClydeHandle, LoadedTexture> _loadedTextures = new Dictionary<ClydeHandle, LoadedTexture>();

        public Texture LoadTextureFromPNGStream(Stream stream, string name = null,
            TextureLoadParameters? loadParams = null)
        {
            DebugTools.Assert(_mainThread == Thread.CurrentThread);

            // Load using Rgba32.
            using (var image = Image.Load(stream))
            {
                return LoadTextureFromImage(image, name, loadParams);
            }
        }

        public Texture LoadTextureFromImage<T>(Image<T> image, string name = null,
            TextureLoadParameters? loadParams = null) where T : unmanaged, IPixel<T>
        {
            DebugTools.Assert(_mainThread == Thread.CurrentThread);

            var actualParams = loadParams ?? TextureLoadParameters.Default;
            var pixelType = typeof(T);

            var texture = new OGLHandle((uint) GL.GenTexture());
            GL.BindTexture(TextureTarget.Texture2D, texture.Handle);
            _applySampleParameters(actualParams.SampleParameters);

            PixelInternalFormat internalFormat;
            PixelFormat pixelDataFormat;
            PixelType pixelDataType;

            if (pixelType == typeof(Rgba32))
            {
                internalFormat = actualParams.Srgb ? PixelInternalFormat.Srgb8Alpha8 : PixelInternalFormat.Rgba8;
                pixelDataFormat = PixelFormat.Rgba;
                pixelDataType = PixelType.UnsignedByte;
            }
            else if (pixelType == typeof(Alpha8))
            {
                if (image.Width % 4 != 0 || image.Height % 4 != 0)
                {
                    throw new ArgumentException("Alpha8 images must have multiple of 4 sizes.");
                }
                internalFormat = PixelInternalFormat.R8;
                pixelDataFormat = PixelFormat.Red;
                pixelDataType = PixelType.UnsignedByte;

                // TODO: Does it make sense to default to 1 for RGB parameters?
                // It might make more sense to pass some options to change swizzling.
                var swizzle = new[] {(int) All.One, (int) All.One, (int) All.One, (int) All.Red};
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleRgba, swizzle);
            }
            else if (pixelType == typeof(Gray8) && !actualParams.Srgb)
            {
                // Can only use R8 for Gray8 if sRGB is OFF.
                // Because OpenGL doesn't provide non-sRGB single/dual channel image formats.
                // Vulkan when?
                if (image.Width % 4 != 0 || image.Height % 4 != 0)
                {
                    throw new ArgumentException("Gray8 non-sRGB images must have multiple of 4 sizes.");
                }

                internalFormat = PixelInternalFormat.R8;
                pixelDataFormat = PixelFormat.Red;
                pixelDataType = PixelType.UnsignedByte;

                var swizzle = new[] {(int) All.Red, (int) All.Red, (int) All.Red, (int) All.One};
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleRgba, swizzle);
            }
            else
            {
                throw new NotImplementedException($"Unable to handle pixel type '{pixelType.Name}'");
            }

            unsafe
            {
                var span = image.GetPixelSpan();
                fixed (T* ptr = span)
                {
                    GL.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, image.Width, image.Height, 0,
                        pixelDataFormat, pixelDataType, (IntPtr) ptr);
                }
            }

            return _genTexture(texture, (image.Width, image.Height), name);
        }

        private static void _applySampleParameters(TextureSampleParameters? sampleParameters)
        {
            var actualParams = sampleParameters ?? TextureSampleParameters.Default;
            if (actualParams.Filter)
            {
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                    (int) TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                    (int) TextureMagFilter.Linear);
            }
            else
            {
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                    (int) TextureMinFilter.Nearest);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                    (int) TextureMagFilter.Nearest);
            }

            switch (actualParams.WrapMode)
            {
                case TextureWrapMode.None:
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS,
                        (int) OGLTextureWrapMode.ClampToEdge);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT,
                        (int) OGLTextureWrapMode.ClampToEdge);
                    break;
                case TextureWrapMode.Repeat:
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS,
                        (int) OGLTextureWrapMode.Repeat);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT,
                        (int) OGLTextureWrapMode.Repeat);
                    break;
                case TextureWrapMode.MirroredRepeat:
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS,
                        (int) OGLTextureWrapMode.MirroredRepeat);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT,
                        (int) OGLTextureWrapMode.MirroredRepeat);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private ClydeTexture _genTexture(OGLHandle oglHandle, Vector2i size, string name)
        {
            if (name != null)
            {
                _objectLabelMaybe(ObjectLabelIdentifier.Texture, oglHandle, name);
            }

            var (width, height) = size;

            var loaded = new LoadedTexture
            {
                OpenGLObject = oglHandle,
                Width = width,
                Height = height,
                Name = name
            };

            var id = AllocRid();
            _loadedTextures.Add(id, loaded);

            return new ClydeTexture(id, size, this);
        }

        private void _deleteTexture(ClydeTexture texture)
        {
            if (!_loadedTextures.TryGetValue(texture.TextureId, out var loadedTexture))
            {
                // Already deleted.
                return;
            }

            GL.DeleteTexture(loadedTexture.OpenGLObject.Handle);
            _loadedTextures.Remove(texture.TextureId);
        }

        private void _loadStockTextures()
        {
            var white = new Image<Rgba32>(1, 1);
            white[0, 0] = Rgba32.White;
            _stockTextureWhite = (ClydeTexture)Texture.LoadFromImage(white);

            var blank = new Image<Rgba32>(1, 1);
            blank[0, 0] = Rgba32.Transparent;
            _stockTextureTransparent = (ClydeTexture)Texture.LoadFromImage(blank);
        }

        private sealed class LoadedTexture
        {
            public OGLHandle OpenGLObject;
            public int Width;
            public int Height;
            public string Name;
            public Vector2i Size => (Width, Height);
        }

        private sealed class ClydeTexture : OwnedTexture
        {
            private readonly Clyde _clyde;

            internal ClydeHandle TextureId { get; }

            public override void Delete()
            {
                _clyde._deleteTexture(this);
            }

            internal ClydeTexture(ClydeHandle id, Vector2i size, Clyde clyde) : base(size)
            {
                TextureId = id;
                _clyde = clyde;
            }
        }

        public Texture GetStockTexture(ClydeStockTexture stockTexture)
        {
            return stockTexture switch
            {
                ClydeStockTexture.White => _stockTextureWhite,
                ClydeStockTexture.Transparent => _stockTextureTransparent,
                _ => throw new ArgumentException(nameof(stockTexture))
            };
        }
    }
}

