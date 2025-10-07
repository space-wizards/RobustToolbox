using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Robust.Client.Graphics.Rhi;
using Robust.Client.Utility;
using Robust.Shared.Graphics;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = Robust.Shared.Maths.Color;
using TextureWrapMode = Robust.Shared.Graphics.TextureWrapMode;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        private ClydeTexture _stockTextureWhite = default!;
        private ClydeTexture _stockTextureBlack = default!;
        private ClydeTexture _stockTextureTransparent = default!;

        private readonly Dictionary<ClydeHandle, LoadedTexture> _loadedTextures = new();

        private readonly ConcurrentQueue<ClydeHandle> _textureDisposeQueue = new();

        public OwnedTexture LoadTextureFromPNGStream(
            Stream stream,
            string? name = null,
            TextureLoadParameters? loadParams = null)
        {
            DebugTools.Assert(_gameThread == Thread.CurrentThread);

            // Load using Rgba32.
            using var image = Image.Load<Rgba32>(stream);

            return LoadTextureFromImage(image, name, loadParams);
        }

        public OwnedTexture LoadTextureFromImage<T>(
            Image<T> image,
            string? name = null,
            TextureLoadParameters? loadParams = null)
            where T : unmanaged, IPixel<T>
        {
            DebugTools.Assert(_gameThread == Thread.CurrentThread);

            var (instance, loaded) = CreateBlankTextureCore<T>((image.Width, image.Height), name, loadParams);

            // Flip image because OpenGL reads images upside down.
            using var copy = FlipClone(image);

            unsafe
            {
                var span = copy.GetPixelSpan();
                Rhi.Queue.WriteTexture(
                    new RhiImageCopyTexture
                    {
                        Texture = loaded.RhiTexture,
                        Aspect = RhiTextureAspect.All,
                        Origin = new RhiOrigin3D(),
                        MipLevel = 0
                    },
                    MemoryMarshal.Cast<T, byte>(span),
                    new RhiImageDataLayout(0, (uint) (sizeof(T) * image.Width), (uint) image.Height),
                    new RhiExtent3D(image.Width, image.Height)
                );
            }

            return instance;
        }

        public OwnedTexture CreateBlankTexture<T>(
            Vector2i size,
            string? name = null,
            in TextureLoadParameters? loadParams = null)
            where T : unmanaged, IPixel<T>
        {
            var (instance, _) = CreateBlankTextureCore<T>(size, name, loadParams);
            return instance;
        }

        private (ClydeTexture, LoadedTexture) CreateBlankTextureCore<T>(
            Vector2i size,
            string? name = null,
            in TextureLoadParameters? loadParams = null)
            where T : unmanaged, IPixel<T>
        {
            DebugTools.Assert(_gameThread == Thread.CurrentThread);

            var actualParams = loadParams ?? TextureLoadParameters.Default;
            var srgb = actualParams.Srgb;

            var format = GetPixelTextureFormat<T>(srgb);

            return CreateBlankTextureCore(size, name, format, actualParams.SampleParameters, srgb);
        }

        private (ClydeTexture, LoadedTexture) CreateBlankTextureCore(
            Vector2i size,
            string? name,
            RhiTextureFormat format,
            TextureSampleParameters sampleParams,
            bool srgb)
        {
            var rhiTexture = Rhi.CreateTexture(new RhiTextureDescriptor(
                new RhiExtent3D(size.X, size.Y),
                format,
                RhiTextureUsage.TextureBinding | RhiTextureUsage.CopySrc | RhiTextureUsage.CopyDst,
                Label: name
            ));

            var rhiTextureView = rhiTexture.CreateView(new RhiTextureViewDescriptor
            {
                Aspect = RhiTextureAspect.All,
                Dimension = RhiTextureViewDimension.Dim2D,
                Format = format,
                Label = name,
                MipLevelCount = 1,
                ArrayLayerCount = 1,
                BaseArrayLayer = 0,
                BaseMipLevel = 0
            });

            var addressMode = sampleParams.WrapMode switch
            {
                TextureWrapMode.None => RhiAddressMode.ClampToEdge,
                TextureWrapMode.Repeat => RhiAddressMode.Repeat,
                TextureWrapMode.MirroredRepeat => RhiAddressMode.MirrorRepeat,
                _ => throw new ArgumentOutOfRangeException()
            };

            var filter = sampleParams.Filter ? RhiFilterMode.Linear : RhiFilterMode.Nearest;

            // TODO: Cache samplers somewhere, we can't actually make infinite of these and they're simple enough.
            var rhiSampler = Rhi.CreateSampler(new RhiSamplerDescriptor(
                AddressModeU: addressMode,
                AddressModeV: addressMode,
                MagFilter: filter,
                MinFilter: filter,
                Label: name
            ));

            var (width, height) = size;

            var id = AllocRid();
            var instance = new ClydeTexture(id, size, srgb, this);
            var loaded = new LoadedTexture
            {
                RhiTexture = rhiTexture,
                DefaultRhiView = rhiTextureView,
                RhiSampler = rhiSampler,
                Width = width,
                Height = height,
                Format = format,
                Name = name,
                TextureInstance = new WeakReference<ClydeTexture>(instance),
            };

            _loadedTextures.Add(id, loaded);

            return (instance, loaded);
        }

        private static RhiTextureFormat GetPixelTextureFormat<T>(bool srgb) where T : unmanaged, IPixel<T>
        {
            return default(T) switch
            {
                Rgba32 => srgb ? RhiTextureFormat.RGBA8UnormSrgb : RhiTextureFormat.RGBA8Unorm,
                _ => throw new ArgumentException("Unsupported pixel format")
            };
        }

        private void TextureSetSubImage<T>(
            ClydeTexture clydeTexture,
            Vector2i topLeft,
            Image<T> sourceImage,
            in UIBox2i sourceRegion)
            where T : unmanaged, IPixel<T>
        {
            if (sourceRegion.Left < 0 ||
                sourceRegion.Top < 0 ||
                sourceRegion.Right > sourceRegion.Width ||
                sourceRegion.Bottom > sourceRegion.Height)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceRegion), "Source rectangle out of bounds.");
            }

            var size = sourceRegion.Width * sourceRegion.Height;

            T[]? pooled = null;
            // C# won't let me use an if due to the stackalloc.
            var copyBuffer = size < 16 * 16
                ? stackalloc T[size]
                : (pooled = ArrayPool<T>.Shared.Rent(size)).AsSpan(0, size);

            var srcSpan = sourceImage.GetPixelSpan();
            var w = sourceImage.Width;
            FlipCopySubRegion(sourceRegion, w, srcSpan, copyBuffer);

            SetSubImageImpl<T>(clydeTexture, topLeft, (sourceRegion.Width, sourceRegion.Height), copyBuffer);

            if (pooled != null)
                ArrayPool<T>.Shared.Return(pooled);
        }

        private void TextureSetSubImage<T>(
            ClydeTexture clydeTexture,
            Vector2i topLeft,
            Vector2i size,
            ReadOnlySpan<T> buffer)
            where T : unmanaged, IPixel<T>
        {
            T[]? pooled = null;
            // C# won't let me use an if due to the stackalloc.
            var copyBuffer = buffer.Length < 16 * 16
                ? stackalloc T[buffer.Length]
                : (pooled = ArrayPool<T>.Shared.Rent(buffer.Length)).AsSpan(0, buffer.Length);

            FlipCopy(buffer, copyBuffer, size.X, size.Y);

            SetSubImageImpl<T>(clydeTexture, topLeft, size, copyBuffer);

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
            var loaded = _loadedTextures[texture.TextureId];
            var format = GetPixelTextureFormat<T>(loaded.IsSrgb);

            if (format != loaded.Format)
            {
                // TODO:
                //if (loaded.TexturePixelType == TexturePixelType.RenderTarget)
                //    throw new InvalidOperationException("Cannot modify texture for render target directly.");

                throw new InvalidOperationException("Mismatching pixel type for texture.");
            }

            if (loaded.Width < dstTl.X + size.X || loaded.Height < dstTl.Y + size.Y)
                throw new ArgumentOutOfRangeException(nameof(size), "Destination rectangle out of bounds.");

            var dstY = loaded.Height - dstTl.Y - size.Y;

            Rhi.Queue.WriteTexture(
                new RhiImageCopyTexture(
                    loaded.RhiTexture,
                    0,
                    new RhiOrigin3D(dstTl.X, dstY)
                ),
                buf,
                new RhiImageDataLayout(0, (uint) (size.X * sizeof(T)), (uint) size.Y),
                new RhiExtent3D(size.X, size.Y)
            );

            // GL.TexSubImage2D(
            //     TextureTarget.Texture2D,
            //     0,
            //     dstTl.X, dstY,
            //     size.X, size.Y,
            //     pf, pt,
            //     (IntPtr) aPtr);
            // CheckGlError();
        }

        /*
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
        */

        /*
        private (PIF pif, PF pf, PT pt) PixelEnums<T>(bool srgb)
            where T : unmanaged, IPixel<T>
        {
            if (_isGLES2)
            {
                return default(T) switch
                {
                    Rgba32 => (PIF.Rgba, PF.Rgba, PT.UnsignedByte),
                    L8 => (PIF.Luminance, PF.Red, PT.UnsignedByte),
                    _ => throw new NotSupportedException("Unsupported pixel type."),
                };
            }

            return default(T) switch
            {
                // Note that if _hasGLSrgb is off, we import an sRGB texture as non-sRGB.
                // Shaders are expected to compensate for this
                Rgba32 => (srgb && _hasGLSrgb ? PIF.Srgb8Alpha8 : PIF.Rgba8, PF.Rgba, PT.UnsignedByte),
                A8 or L8 => (PIF.R8, PF.Red, PT.UnsignedByte),
                _ => throw new NotSupportedException("Unsupported pixel type."),
            };
        }
        */

        /*
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
                TexturePixelType = pixType,
                TextureInstance = new WeakReference<ClydeTexture>(instance)
            };

            _loadedTextures.Add(id, loaded);
            //GC.AddMemoryPressure(memoryPressure);

            return instance;
        }
        */

        private void DeleteTexture(ClydeHandle textureHandle)
        {
            /*
            if (!_loadedTextures.TryGetValue(textureHandle, out var loadedTexture))
            {
                // Already deleted I guess.
                return;
            }

            GL.DeleteTexture(loadedTexture.OpenGLObject.Handle);
            CheckGlError();
            _loadedTextures.Remove(textureHandle);
            //GC.RemoveMemoryPressure(loadedTexture.MemoryPressure);
        */
        }

        private void LoadStockTextures()
        {
            var white = new Image<Rgba32>(1, 1);
            white[0, 0] = new Rgba32(255, 255, 255, 255);
            _stockTextureWhite = (ClydeTexture)LoadTextureFromImage(white, name: "Stock Texture: white");

            var black = new Image<Rgba32>(1, 1);
            black[0, 0] = new Rgba32(0, 0, 0, 255);
            _stockTextureBlack = (ClydeTexture)LoadTextureFromImage(black, name: "Stock Texture: black");

            var blank = new Image<Rgba32>(1, 1);
            blank[0, 0] = new Rgba32(0, 0, 0, 0);
            _stockTextureTransparent = (ClydeTexture)LoadTextureFromImage(blank, name: "Stock Texture: transparent");
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

        internal sealed class LoadedTexture
        {
            public RhiTexture RhiTexture = default!;
            public RhiTextureView DefaultRhiView = default!;
            public RhiSampler RhiSampler = default!;

            public int Width;
            public int Height;
            public long MemoryPressure;
            public RhiTextureFormat Format;
            public string? Name;

            public Vector2i Size => (Width, Height);
            public required WeakReference<ClydeTexture> TextureInstance;

            public bool IsSrgb => Format is RhiTextureFormat.BGRA8UnormSrgb or RhiTextureFormat.RGBA8UnormSrgb;
        }

        private void FlushTextureDispose()
        {
            /*while (_textureDisposeQueue.TryDequeue(out var handle))
            {
                DeleteTexture(handle);
            }*/
        }

        internal sealed class ClydeTexture : OwnedTexture
        {
            private readonly Clyde _clyde;
            public readonly bool IsSrgb;

            internal ClydeHandle TextureId { get; }

            public override void SetSubImage<T>(Vector2i topLeft, Image<T> sourceImage, in UIBox2i sourceRegion)
            {
                _clyde.TextureSetSubImage(this, topLeft, sourceImage, sourceRegion);
            }

            public override void SetSubImage<T>(Vector2i topLeft, Vector2i size, ReadOnlySpan<T> buffer)
            {
                _clyde.TextureSetSubImage(this, topLeft, size, buffer);
            }

            protected override void Dispose(bool disposing)
            {
                /*if (_clyde.IsMainThread())
                {
                    // Main thread, do direct GL deletion.
                    _clyde.DeleteTexture(TextureId);
                }
                else
                {
                    // Finalizer thread
                    _clyde._textureDisposeQueue.Enqueue(TextureId);
                }*/
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

        public IEnumerable<(ClydeTexture, LoadedTexture)> GetLoadedTextures()
        {
            foreach (var loaded in _loadedTextures.Values)
            {
                if (!loaded.TextureInstance.TryGetTarget(out var textureInstance))
                    continue;

                yield return (textureInstance, loaded);
            }
        }

        private sealed class DummyTexture : OwnedTexture
        {
            public DummyTexture(Vector2i size) : base(size)
            {
            }

            public override void SetSubImage<T>(Vector2i topLeft, Image<T> sourceImage, in UIBox2i sourceRegion)
            {
            }

            public override void SetSubImage<T>(Vector2i topLeft, Vector2i size, ReadOnlySpan<T> buffer)
            {
            }
        }
    }
}
