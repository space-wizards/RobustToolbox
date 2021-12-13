using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using OpenToolkit.Graphics.OpenGL4;
using Robust.Client.Utility;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = Robust.Shared.Maths.Color;
using OGLTextureWrapMode = OpenToolkit.Graphics.OpenGL.TextureWrapMode;
using PIF = OpenToolkit.Graphics.OpenGL4.PixelInternalFormat;
using PF = OpenToolkit.Graphics.OpenGL4.PixelFormat;
using PT = OpenToolkit.Graphics.OpenGL4.PixelType;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        private ClydeTexture _stockTextureWhite = default!;
        private ClydeTexture _stockTextureBlack = default!;
        private ClydeTexture _stockTextureTransparent = default!;

        private readonly Dictionary<ClydeHandle, LoadedTexture> _loadedTextures = new();

        private readonly ConcurrentQueue<ClydeHandle> _textureDisposeQueue = new();

        public OwnedTexture LoadTextureFromPNGStream(Stream stream, string? name = null,
            TextureLoadParameters? loadParams = null)
        {
            DebugTools.Assert(_gameThread == Thread.CurrentThread);

            // Load using Rgba32.
            using var image = Image.Load<Rgba32>(stream);

            return LoadTextureFromImage(image, name, loadParams);
        }

        public OwnedTexture LoadTextureFromImage<T>(Image<T> image, string? name = null,
            TextureLoadParameters? loadParams = null) where T : unmanaged, IPixel<T>
        {
            DebugTools.Assert(_gameThread == Thread.CurrentThread);

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
                    using var img = ApplyA8Swizzle((Image<A8>) (object) image);
                    return LoadTextureFromImage(img, name, loadParams);
                }

                if (pixelType == typeof(L8) && !actualParams.Srgb)
                {
                    using var img = ApplyL8Swizzle((Image<L8>) (object) image);
                    return LoadTextureFromImage(img, name, loadParams);
                }
            }

            // Flip image because OpenGL reads images upside down.
            using var copy = FlipClone(image);

            var texture = CreateBaseTextureInternal<T>(image.Width, image.Height, actualParams, name);

            unsafe
            {
                var span = copy.GetPixelSpan();
                fixed (T* ptr = span)
                {
                    // Still bound.
                    DoTexUpload(copy.Width, copy.Height, actualParams.Srgb, ptr);
                }
            }

            return texture;
        }

        public unsafe OwnedTexture CreateBlankTexture<T>(
            Vector2i size,
            string? name = null,
            in TextureLoadParameters? loadParams = null)
            where T : unmanaged, IPixel<T>
        {
            var actualParams = loadParams ?? TextureLoadParameters.Default;
            if (!_hasGLTextureSwizzle)
            {
                // Actually create RGBA32 texture if missing texture swizzle.
                // This is fine (TexturePixelType that's stored) because all other APIs do the same.
                if (typeof(T) == typeof(A8) || typeof(T) == typeof(L8))
                {
                    return CreateBlankTexture<Rgba32>(size, name, loadParams);
                }
            }

            var texture = CreateBaseTextureInternal<T>(
                size.X, size.Y,
                actualParams,
                name);

            // Texture still bound, run glTexImage2D with null data param to specify bounds.
            DoTexUpload<T>(size.X, size.Y, actualParams.Srgb, null);

            return texture;
        }

        private unsafe void DoTexUpload<T>(int width, int height, bool srgb, T* ptr) where T : unmanaged, IPixel<T>
        {
            if (sizeof(T) < 4)
            {
                GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
                CheckGlError();
            }

            var (pif, pf, pt) = PixelEnums<T>(srgb);
            GL.TexImage2D(TextureTarget.Texture2D, 0, pif, width, height, 0, pf, pt, (IntPtr) ptr);
            CheckGlError();

            if (sizeof(T) < 4)
            {
                GL.PixelStore(PixelStoreParameter.UnpackAlignment, 4);
                CheckGlError();
            }
        }

        private ClydeTexture CreateBaseTextureInternal<T>(
            int width, int height,
            in TextureLoadParameters loadParams,
            string? name = null)
            where T : unmanaged, IPixel<T>
        {
            var texture = new GLHandle((uint) GL.GenTexture());
            CheckGlError();
            GL.BindTexture(TextureTarget.Texture2D, texture.Handle);
            CheckGlError();
            ApplySampleParameters(loadParams.SampleParameters);

            var (pif, pf, pt) = PixelEnums<T>(loadParams.Srgb);
            var pixelType = typeof(T);
            var texPixType = GetTexturePixelType<T>();
            var isActuallySrgb = false;

            if (pixelType == typeof(Rgba32))
            {
                isActuallySrgb = loadParams.Srgb;
            }
            else if (pixelType == typeof(A8))
            {
                DebugTools.Assert(_hasGLTextureSwizzle);

                // TODO: Does it make sense to default to 1 for RGB parameters?
                // It might make more sense to pass some options to change swizzling.
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleR, (int) All.One);
                CheckGlError();
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleG, (int) All.One);
                CheckGlError();
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleB, (int) All.One);
                CheckGlError();
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleA, (int) All.Red);
                CheckGlError();
            }
            else if (pixelType == typeof(L8) && !loadParams.Srgb)
            {
                DebugTools.Assert(_hasGLTextureSwizzle);

                // Can only use R8 for L8 if sRGB is OFF.
                // Because OpenGL doesn't provide sRGB single/dual channel image formats.
                // Vulkan when?

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleR, (int) All.Red);
                CheckGlError();
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleG, (int) All.Red);
                CheckGlError();
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleB, (int) All.Red);
                CheckGlError();
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleA, (int) All.One);
                CheckGlError();
            }
            else
            {
                throw new NotSupportedException($"Unable to handle pixel type '{pixelType.Name}'");
            }

            var pressureEst = EstPixelSize(pif) * width * height;

            return GenTexture(texture, (width, height), isActuallySrgb, name, texPixType, pressureEst);
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

        private (PIF pif, PF pf, PT pt) PixelEnums<T>(bool srgb)
            where T : unmanaged, IPixel<T>
        {
            return default(T) switch
            {
                // Note that if _hasGLSrgb is off, we import an sRGB texture as non-sRGB.
                // Shaders are expected to compensate for this
                Rgba32 => (srgb && _hasGLSrgb ? PIF.Srgb8Alpha8 : PIF.Rgba8, PF.Rgba, PT.UnsignedByte),
                A8 or L8 => (PIF.R8, PF.Red, PT.UnsignedByte),
                _ => throw new NotSupportedException("Unsupported pixel type."),
            };
        }

        private ClydeTexture GenTexture(
            GLHandle glHandle,
            Vector2i size,
            bool srgb,
            string? name,
            TexturePixelType pixType,
            long memoryPressure = 0)
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
                MemoryPressure = memoryPressure,
                TexturePixelType = pixType
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

        private unsafe void SetSubImage<T>(
            ClydeTexture texture,
            Vector2i dstTl,
            Image<T> img,
            in UIBox2i srcBox)
            where T : unmanaged, IPixel<T>
        {
            if (srcBox.Left < 0 ||
                srcBox.Top < 0 ||
                srcBox.Right > srcBox.Width ||
                srcBox.Bottom > srcBox.Height)
            {
                throw new ArgumentOutOfRangeException(nameof(srcBox), "Source rectangle out of bounds.");
            }

            var size = srcBox.Width * srcBox.Height;

            T[]? pooled = null;
            // C# won't let me use an if due to the stackalloc.
            var copyBuffer = size < 16 * 16
                ? stackalloc T[size]
                : (pooled = ArrayPool<T>.Shared.Rent(size)).AsSpan(0, size);

            var srcSpan = img.GetPixelSpan();
            var w = img.Width;
            FlipCopySubRegion(srcBox, w, srcSpan, copyBuffer);

            SetSubImageImpl<T>(texture, dstTl, (srcBox.Width, srcBox.Height), copyBuffer);

            if (pooled != null)
                ArrayPool<T>.Shared.Return(pooled);
        }

        private unsafe void SetSubImage<T>(
            ClydeTexture texture,
            Vector2i dstTl,
            Vector2i size,
            ReadOnlySpan<T> buf)
            where T : unmanaged, IPixel<T>
        {
            T[]? pooled = null;
            // C# won't let me use an if due to the stackalloc.
            var copyBuffer = buf.Length < 16 * 16
                ? stackalloc T[buf.Length]
                : (pooled = ArrayPool<T>.Shared.Rent(buf.Length)).AsSpan(0, buf.Length);

            FlipCopy(buf, copyBuffer, size.X, size.Y);

            SetSubImageImpl<T>(texture, dstTl, size, copyBuffer);

            if (pooled != null)
                ArrayPool<T>.Shared.Return(pooled);
        }

        private unsafe void SetSubImageImpl<T>(
            ClydeTexture texture,
            Vector2i dstTl,
            Vector2i size,
            ReadOnlySpan<T> buf)
            where T : unmanaged, IPixel<T>
        {
            if (!_hasGLTextureSwizzle && (typeof(T) == typeof(A8) || typeof(T) == typeof(L8)))
            {
                var swizzleBuf = ArrayPool<Rgba32>.Shared.Rent(buf.Length);

                var destSpan = swizzleBuf.AsSpan(0, buf.Length);
                if (typeof(T) == typeof(A8))
                    ApplyA8Swizzle(MemoryMarshal.Cast<T, A8>(buf), destSpan);
                else if (typeof(T) == typeof(L8))
                    ApplyL8Swizzle(MemoryMarshal.Cast<T, L8>(buf), destSpan);

                SetSubImageImpl<Rgba32>(texture, dstTl, size, destSpan);
                ArrayPool<Rgba32>.Shared.Return(swizzleBuf);
                return;
            }

            var loaded = _loadedTextures[texture.TextureId];
            var pixType = GetTexturePixelType<T>();

            if (pixType != loaded.TexturePixelType)
            {
                if (loaded.TexturePixelType == TexturePixelType.RenderTarget)
                    throw new InvalidOperationException("Cannot modify texture for render target directly.");

                throw new InvalidOperationException("Mismatching pixel type for texture.");
            }

            if (loaded.Width < dstTl.X + size.X || loaded.Height < dstTl.Y + size.Y)
                throw new ArgumentOutOfRangeException(nameof(size), "Destination rectangle out of bounds.");

            if (sizeof(T) != 4)
            {
                GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
                CheckGlError();
            }

            // sRGB doesn't matter since that only changes the internalFormat, which we don't need here.
            var (_, pf, pt) = PixelEnums<T>(srgb: false);

            GL.BindTexture(TextureTarget.Texture2D, loaded.OpenGLObject.Handle);
            CheckGlError();

            fixed (T* aPtr = buf)
            {
                var dstY = loaded.Height - dstTl.Y - size.Y;
                GL.TexSubImage2D(
                    TextureTarget.Texture2D,
                    0,
                    dstTl.X, dstY,
                    size.X, size.Y,
                    pf, pt,
                    (IntPtr) aPtr);
                CheckGlError();
            }

            if (sizeof(T) != 4)
            {
                GL.PixelStore(PixelStoreParameter.UnpackAlignment, 4);
                CheckGlError();
            }
        }

        private static TexturePixelType GetTexturePixelType<T>() where T : unmanaged, IPixel<T>
        {
            return default(T) switch
            {
                Rgba32 => TexturePixelType.Rgba32,
                L8 => TexturePixelType.L8,
                A8 => TexturePixelType.A8,
                _ => throw new NotSupportedException("Unsupported pixel type."),
            };
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

        private static void FlipCopySubRegion<T>(
            UIBox2i srcBox,
            int w,
            ReadOnlySpan<T> srcSpan,
            Span<T> copyBuffer)
            where T : unmanaged, IPixel<T>
        {
            var subH = srcBox.Height;
            var subW = srcBox.Width;

            var dr = subH - 1;
            for (var r = 0; r < subH; r++, dr--)
            {
                var si = r * w + srcBox.Left;
                var di = dr * subW;
                var srcRow = srcSpan[si..(si + subW)];
                var dstRow = copyBuffer[di..(di + subW)];

                srcRow.CopyTo(dstRow);
            }
        }

        private static Image<Rgba32> ApplyA8Swizzle(Image<A8> source)
        {
            var newImage = new Image<Rgba32>(source.Width, source.Height);
            var sourceSpan = source.GetPixelSpan();
            var destSpan = newImage.GetPixelSpan();

            ApplyA8Swizzle(sourceSpan, destSpan);

            return newImage;
        }

        private static Image<Rgba32> ApplyL8Swizzle(Image<L8> source)
        {
            var newImage = new Image<Rgba32>(source.Width, source.Height);
            var sourceSpan = source.GetPixelSpan();
            var destSpan = newImage.GetPixelSpan();

            ApplyL8Swizzle(sourceSpan, destSpan);

            return newImage;
        }

        private static void ApplyL8Swizzle(ReadOnlySpan<L8> src, Span<Rgba32> dst)
        {
            for (var i = 0; i < src.Length; i++)
            {
                var px = src[i].PackedValue;
                dst[i] = new Rgba32(px, px, px, 255);
            }
        }

        private static void ApplyA8Swizzle(ReadOnlySpan<A8> src, Span<Rgba32> dst)
        {
            for (var i = 0; i < src.Length; i++)
            {
                var px = src[i].PackedValue;
                dst[i] = new Rgba32(255, 255, 255, px);
            }
        }

        private sealed class LoadedTexture
        {
            public GLHandle OpenGLObject;
            public int Width;
            public int Height;
            public bool IsSrgb;
            public string? Name;
            public long MemoryPressure;
            public TexturePixelType TexturePixelType;

            public Vector2i Size => (Width, Height);
            // public WeakReference<ClydeTexture> TextureInstance;
        }

        private enum TexturePixelType : byte
        {
            RenderTarget = 0,
            Rgba32,
            A8,
            L8,
        }

        private void FlushTextureDispose()
        {
            while (_textureDisposeQueue.TryDequeue(out var handle))
            {
                DeleteTexture(handle);
            }
        }

        private sealed class ClydeTexture : OwnedTexture
        {
            private readonly Clyde _clyde;
            public readonly bool IsSrgb;

            internal ClydeHandle TextureId { get; }

            public override void SetSubImage<T>(Vector2i topLeft, Image<T> sourceImage, in UIBox2i sourceRegion)
            {
                _clyde.SetSubImage(this, topLeft, sourceImage, sourceRegion);
            }

            public override void SetSubImage<T>(Vector2i topLeft, Vector2i size, ReadOnlySpan<T> buffer)
            {
                _clyde.SetSubImage(this, topLeft, size, buffer);
            }

            protected override void Dispose(bool disposing)
            {
                if (_clyde.IsMainThread())
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

            public override Color GetPixel(int x, int y)
            {
                if (!_clyde._loadedTextures.TryGetValue(TextureId, out var loaded))
                {
                    throw new DataException("Texture not found");
                }

                Span<byte> rgba = stackalloc byte[4];
                unsafe
                {
                    fixed (byte* p = rgba)
                    {
                        GL.GetTextureImage(loaded.OpenGLObject.Handle, 0, PF.Rgba, PT.UnsignedByte, 4, (IntPtr) p);
                    }
                }

                return new Color(rgba[0], rgba[1], rgba[2], rgba[3]);
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
