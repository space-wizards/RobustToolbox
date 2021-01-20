using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using OpenToolkit.Graphics.OpenGL4;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Utility;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using OGLTextureWrapMode = OpenToolkit.Graphics.OpenGL.TextureWrapMode;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        private ClydeTexture _stockTextureWhite = default!;
        private ClydeTexture _stockTextureBlack = default!;
        private ClydeTexture _stockTextureTransparent = default!;

        private readonly Dictionary<ClydeHandle, LoadedTexture> _loadedTextures =
            new();

        private readonly ConcurrentQueue<ClydeHandle> _textureDisposeQueue = new();

        public Texture LoadTextureFromPNGStream(Stream stream, string? name = null,
            TextureLoadParameters? loadParams = null)
        {
            DebugTools.Assert(_mainThread == Thread.CurrentThread);

            // Load using Rgba32.
            using var image = Image.Load<Rgba32>(stream);

            return LoadTextureFromImage(image, name, loadParams);
        }

        public Texture LoadTextureFromImage<T>(Image<T> image, string? name = null,
            TextureLoadParameters? loadParams = null) where T : unmanaged, IPixel<T>
        {
            DebugTools.Assert(_mainThread == Thread.CurrentThread);

            var actualParams = loadParams ?? TextureLoadParameters.Default;
            var pixelType = typeof(T);

            if (!_hasGLTextureSwizzle)
            {
                // If texture swizzle isn't available we have to pre-process the images to apply it ourselves
                // and then upload as RGBA8.
                // Yes this is inefficient but the alternative is modifying the shaders,
                // which I CBA to do.
                // Even 8 year old iGPUs support texture swizzle.
                if (pixelType == typeof(A8))
                {
                    // Disable sRGB so stuff doesn't get interpreter wrong.
                    actualParams.Srgb = false;
                    var img = ApplyA8Swizzle((Image<A8>) (object) image);
                    return LoadTextureFromImage(img, name, loadParams);
                }

                if (pixelType == typeof(L8) && !actualParams.Srgb)
                {
                    var img = ApplyL8Swizzle((Image<L8>) (object) image);
                    return LoadTextureFromImage(img, name, loadParams);
                }
            }

            // Flip image because OpenGL reads images upside down.
            var copy = FlipClone(image);

            var texture = new GLHandle((uint) GL.GenTexture());
            CheckGlError();
            GL.BindTexture(TextureTarget.Texture2D, texture.Handle);
            CheckGlError();
            ApplySampleParameters(actualParams.SampleParameters);

            PixelInternalFormat internalFormat;
            PixelFormat pixelDataFormat;
            PixelType pixelDataType;
            bool isActuallySrgb = false;

            if (pixelType == typeof(Rgba32))
            {
                // Note that if _hasGLSrgb is off, we import an sRGB texture as non-sRGB.
                // Shaders are expected to compensate for this
                internalFormat = (actualParams.Srgb && _hasGLSrgb) ? PixelInternalFormat.Srgb8Alpha8 : PixelInternalFormat.Rgba8;
                isActuallySrgb = actualParams.Srgb;
                pixelDataFormat = PixelFormat.Rgba;
                pixelDataType = PixelType.UnsignedByte;
            }
            else if (pixelType == typeof(A8))
            {
                if (image.Width % 4 != 0 || image.Height % 4 != 0)
                {
                    throw new ArgumentException("Alpha8 images must have multiple of 4 sizes.");
                }

                internalFormat = PixelInternalFormat.R8;
                pixelDataFormat = PixelFormat.Red;
                pixelDataType = PixelType.UnsignedByte;

                unsafe
                {
                    // TODO: Does it make sense to default to 1 for RGB parameters?
                    // It might make more sense to pass some options to change swizzling.
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleR, (int) All.One);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleG, (int) All.One);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleB, (int) All.One);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleA, (int) All.Red);
                }
            }
            else if (pixelType == typeof(L8) && !actualParams.Srgb)
            {
                // Can only use R8 for L8 if sRGB is OFF.
                // Because OpenGL doesn't provide sRGB single/dual channel image formats.
                // Vulkan when?
                if (copy.Width % 4 != 0 || copy.Height % 4 != 0)
                {
                    throw new ArgumentException("L8 non-sRGB images must have multiple of 4 sizes.");
                }

                internalFormat = PixelInternalFormat.R8;
                pixelDataFormat = PixelFormat.Red;
                pixelDataType = PixelType.UnsignedByte;

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleR, (int) All.Red);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleG, (int) All.Red);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleB, (int) All.Red);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleA, (int) All.One);
            }
            else
            {
                throw new NotImplementedException($"Unable to handle pixel type '{pixelType.Name}'");
            }

            unsafe
            {
                var span = copy.GetPixelSpan();
                fixed (T* ptr = span)
                {
                    GL.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, copy.Width, copy.Height, 0,
                        pixelDataFormat, pixelDataType, (IntPtr) ptr);
                    CheckGlError();
                }
            }

            var pressureEst = EstPixelSize(internalFormat) * copy.Width * copy.Height;

            return GenTexture(texture, (copy.Width, copy.Height), isActuallySrgb, name, pressureEst);
        }

        private void ApplySampleParameters(TextureSampleParameters? sampleParameters)
        {
            var actualParams = sampleParameters ?? TextureSampleParameters.Default;
            if (actualParams.Filter)
            {
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                    (int) TextureMinFilter.Linear);
                CheckGlError();
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                    (int) TextureMagFilter.Linear);
                CheckGlError();
            }
            else
            {
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                    (int) TextureMinFilter.Nearest);
                CheckGlError();
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                    (int) TextureMagFilter.Nearest);
                CheckGlError();
            }

            switch (actualParams.WrapMode)
            {
                case TextureWrapMode.None:
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS,
                        (int) OGLTextureWrapMode.ClampToEdge);
                    CheckGlError();
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT,
                        (int) OGLTextureWrapMode.ClampToEdge);
                    CheckGlError();
                    break;
                case TextureWrapMode.Repeat:
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS,
                        (int) OGLTextureWrapMode.Repeat);
                    CheckGlError();
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT,
                        (int) OGLTextureWrapMode.Repeat);
                    CheckGlError();
                    break;
                case TextureWrapMode.MirroredRepeat:
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS,
                        (int) OGLTextureWrapMode.MirroredRepeat);
                    CheckGlError();
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT,
                        (int) OGLTextureWrapMode.MirroredRepeat);
                    CheckGlError();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            CheckGlError();
        }

        private ClydeTexture GenTexture(GLHandle glHandle, Vector2i size, bool srgb, string? name, long memoryPressure=0)
        {
            if (name != null)
            {
                ObjectLabelMaybe(ObjectLabelIdentifier.Texture, glHandle, name);
            }

            var (width, height) = size;

            var id = AllocRid();
            var instance = new ClydeTexture(id, size, srgb, this);
            var loaded = new LoadedTexture
            {
                OpenGLObject = glHandle,
                Width = width,
                Height = height,
                IsSrgb = srgb,
                Name = name,
                MemoryPressure = memoryPressure
                // TextureInstance = new WeakReference<ClydeTexture>(instance)
            };

            _loadedTextures.Add(id, loaded);
            //GC.AddMemoryPressure(memoryPressure);

            return instance;
        }

        private void DeleteTexture(ClydeHandle textureHandle)
        {
            if (!_loadedTextures.TryGetValue(textureHandle, out var loadedTexture))
            {
                // Already deleted I guess.
                return;
            }

            GL.DeleteTexture(loadedTexture.OpenGLObject.Handle);
            CheckGlError();
            _loadedTextures.Remove(textureHandle);
            //GC.RemoveMemoryPressure(loadedTexture.MemoryPressure);
        }

        private void LoadStockTextures()
        {
            var white = new Image<Rgba32>(1, 1);
            white[0, 0] = new Rgba32(255, 255, 255, 255);
            _stockTextureWhite = (ClydeTexture) Texture.LoadFromImage(white);

            var black = new Image<Rgba32>(1, 1);
            black[0, 0] = new Rgba32(0, 0, 0, 255);
            _stockTextureBlack = (ClydeTexture) Texture.LoadFromImage(black);

            var blank = new Image<Rgba32>(1, 1);
            blank[0, 0] = new Rgba32(0, 0, 0, 0);
            _stockTextureTransparent = (ClydeTexture) Texture.LoadFromImage(blank);
        }

        /// <summary>
        ///     Makes a clone of the image that is also flipped.
        /// </summary>
        private static Image<T> FlipClone<T>(Image<T> source) where T : unmanaged, IPixel<T>
        {
            var w = source.Width;
            var h = source.Height;

            var copy = new Image<T>(w, h);

            var srcSpan = source.GetPixelSpan();
            var dstSpan = copy.GetPixelSpan();

            FlipCopy(srcSpan, dstSpan, w, h);

            return copy;
        }

        private static void FlipCopy<T>(ReadOnlySpan<T> srcSpan, Span<T> dstSpan, int w, int h)
        {
            var dr = h - 1;
            for (var r = 0; r < h; r++, dr--)
            {
                var si = r * w;
                var di = dr * w;
                var srcRow = srcSpan[si..(si + w)];
                var dstRow = dstSpan[di..(di + w)];

                srcRow.CopyTo(dstRow);
            }
        }

        private static void FlipCopyScreenshot(ReadOnlySpan<Rgba32> srcSpan, Span<Rgb24> dstSpan, int w, int h)
        {
            var dr = h - 1;
            for (var r = 0; r < h; r++, dr--)
            {
                var si = r * w;
                var di = dr * w;
                var srcRow = srcSpan[si..(si + w)];
                var dstRow = dstSpan[di..(di + w)];

                for (var x = 0; x < w; x++)
                {
                    var src = srcRow[x];
                    dstRow[x] = new Rgb24(src.R, src.G, src.B);
                }
            }
        }

        private static Image<Rgba32> ApplyA8Swizzle(Image<A8> source)
        {
            var newImage = new Image<Rgba32>(source.Width, source.Height);
            var sourceSpan = source.GetPixelSpan();
            var destSpan = newImage.GetPixelSpan();

            for (var i = 0; i < sourceSpan.Length; i++)
            {
                var px = sourceSpan[i].PackedValue;
                destSpan[i] = new Rgba32(255, 255, 255, px);
            }

            return newImage;
        }

        private static Image<Rgba32> ApplyL8Swizzle(Image<L8> source)
        {
            var newImage = new Image<Rgba32>(source.Width, source.Height);
            var sourceSpan = source.GetPixelSpan();
            var destSpan = newImage.GetPixelSpan();

            for (var i = 0; i < sourceSpan.Length; i++)
            {
                var px = sourceSpan[i].PackedValue;
                destSpan[i] = new Rgba32(px, px, px, 255);
            }

            return newImage;
        }


        private sealed class LoadedTexture
        {
            public GLHandle OpenGLObject;
            public int Width;
            public int Height;
            public bool IsSrgb;
            public string? Name;
            public long MemoryPressure;
            public Vector2i Size => (Width, Height);
            // public WeakReference<ClydeTexture> TextureInstance;
        }

        private void FlushTextureDispose()
        {
            while (_textureDisposeQueue.TryDequeue(out var handle))
            {
                DeleteTexture(handle);
            }
        }

        private sealed class ClydeTexture : OwnedTexture, IDeepClone
        {
            private readonly Clyde _clyde;
            public readonly bool IsSrgb;

            internal ClydeHandle TextureId { get; }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    // Main thread, do direct GL deletion.
                    _clyde.DeleteTexture(TextureId);
                }
                else
                {
                    // Finalizer thread
                    _clyde._textureDisposeQueue.Enqueue(TextureId);
                }
            }

            internal ClydeTexture(ClydeHandle id, Vector2i size, bool srgb, Clyde clyde) : base(size)
            {
                TextureId = id;
                IsSrgb = srgb;
                _clyde = clyde;
            }

            public override string ToString()
            {
                if (_clyde._loadedTextures.TryGetValue(TextureId, out var loaded) && loaded.Name != null)
                {
                    return $"ClydeTexture: {loaded.Name} ({TextureId})";
                }

                return $"ClydeTexture: ({TextureId})";
            }

            public IDeepClone DeepClone()
            {
                return new ClydeTexture(TextureId, Size, IsSrgb, _clyde);
            }
        }

        public Texture GetStockTexture(ClydeStockTexture stockTexture)
        {
            return stockTexture switch
            {
                ClydeStockTexture.White => _stockTextureWhite,
                ClydeStockTexture.Transparent => _stockTextureTransparent,
                ClydeStockTexture.Black => _stockTextureBlack,
                _ => throw new ArgumentException(nameof(stockTexture))
            };
        }
    }
}
