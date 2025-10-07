using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Robust.Shared.Graphics;
using Robust.Shared.Map;
using Robust.Client.Graphics.Rhi;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using SixLabors.ImageSharp.PixelFormats;

// ReSharper disable once IdentifierTypo
using RTCF = Robust.Client.Graphics.RenderTargetColorFormat;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        private readonly Dictionary<ClydeHandle, LoadedRenderTarget> _renderTargets = new();

        // private readonly ConcurrentQueue<ClydeHandle> _renderTargetDisposeQueue = new();


        public IRenderTexture CreateLightRenderTarget(Vector2i size, string? name = null, bool depthStencil = true)
        {
            var lightMapColorFormat = true // _hasGLFloatFramebuffers
                ? RTCF.R11FG11FB10F
                : RTCF.Rgba8;
            var lightMapSampleParameters = new TextureSampleParameters { Filter = true };

            return CreateRenderTarget(size,
                new RenderTargetFormatParameters(lightMapColorFormat, hasDepthStencil: depthStencil),
                lightMapSampleParameters,
                name: name);
        }

        IRenderTexture IClyde.CreateRenderTarget(Vector2i size, RenderTargetFormatParameters format,
            TextureSampleParameters? sampleParameters, string? name)
        {
            return CreateRenderTarget(size, format, sampleParameters, name);
        }

        private RenderTexture CreateRenderTarget(Vector2i size, RenderTargetFormatParameters format,
            TextureSampleParameters? sampleParameters = null, string? name = null)
        {
            DebugTools.Assert(size.X != 0);
            DebugTools.Assert(size.Y != 0);

            ClydeTexture textureObject;

            RhiTexture? depthStencilTexture = null;
            RhiTextureView? depthStencilTextureView = null;

            // Color attachment.
            {
                var textureFormat = format.ColorFormat switch
                {
                    RenderTargetColorFormat.Rgba8 => RhiTextureFormat.BGRA8Unorm,
                    RenderTargetColorFormat.Rgba8Srgb => RhiTextureFormat.BGRA8UnormSrgb,
                    RenderTargetColorFormat.R32F => RhiTextureFormat.R32Float,
                    RenderTargetColorFormat.RG32F => RhiTextureFormat.RG32Float,
                    RenderTargetColorFormat.Rgba16F => RhiTextureFormat.RGBA16Float,
                    RenderTargetColorFormat.R11FG11FB10F => RhiTextureFormat.RG11B10Ufloat,
                    RenderTargetColorFormat.R8 => RhiTextureFormat.R8Unorm,
                    _ => throw new ArgumentOutOfRangeException()
                };

                (textureObject, _) = CreateBlankTextureCore(
                    size,
                    name != null ? $"RT_{name}_color" : null,
                    textureFormat,
                    sampleParameters ?? TextureSampleParameters.Default,
                    format.ColorFormat == RenderTargetColorFormat.Rgba8Srgb
                );
            }

            // Depth/stencil buffers.
            if (format.HasDepthStencil)
            {
                var depthLabel = name != null ? $"RT_{name}_depthStencil" : null;

                depthStencilTexture = Rhi.CreateTexture(new RhiTextureDescriptor(
                    new RhiExtent3D(size.X, size.Y),
                    RhiTextureFormat.Depth24PlusStencil8,
                    RhiTextureUsage.RenderAttachment,
                    Label: depthLabel
                ));

                depthStencilTextureView = depthStencilTexture.CreateView(new RhiTextureViewDescriptor
                {
                    Aspect = RhiTextureAspect.All,
                    Dimension = RhiTextureViewDimension.Dim2D,
                    Format = RhiTextureFormat.Depth24PlusStencil8,
                    Label = depthLabel,
                    MipLevelCount = 1,
                    ArrayLayerCount = 1,
                    BaseArrayLayer = 0,
                    BaseMipLevel = 0
                });
            }

            var handle = AllocRid();
            var renderTarget = new RenderTexture(size, textureObject, this, handle);
            var data = new LoadedRenderTarget
            {
                IsWindow = false,
                IsSrgb = textureObject.IsSrgb,
                Size = size,
                TextureHandle = textureObject.TextureId,
                ColorFormat = format.ColorFormat,
                DepthStencilTexture = depthStencilTexture,
                DepthSencilTextureView = depthStencilTextureView,
                MemoryPressure = 0, // pressure,
                SampleParameters = sampleParameters,
                Instance = new WeakReference<RenderTargetBase>(renderTarget),
                Name = name,
            };

            _renderTargets.Add(handle, data);
            return renderTarget;
        }

        private void DeleteRenderTexture(ClydeHandle handle)
        {
            if (!_renderTargets.TryGetValue(handle, out var renderTarget))
                return;

            DebugTools.Assert(!renderTarget.IsWindow, "Cannot delete window-backed render targets directly.");

            _renderTargets.Remove(handle);
            DeleteTexture(renderTarget.TextureHandle);

            renderTarget.DepthStencilTexture?.Dispose();
            renderTarget.DepthSencilTextureView?.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private LoadedRenderTarget RtToLoaded(IRenderTarget rt)
        {
            switch (rt)
            {
                case RenderTargetBase based:
                    return _renderTargets[based.Handle];
                default:
                    throw new NotImplementedException();
            }
        }

        /*
        private void FlushRenderTargetDispose()
        {
            while (_renderTargetDisposeQueue.TryDequeue(out var handle))
            {
                DeleteRenderTexture(handle);
            }
        }
        */

        public IEnumerable<(RenderTargetBase, LoadedRenderTarget)> GetLoadedRenderTextures()
        {
            foreach (var loaded in _renderTargets.Values)
            {
                if (!loaded.Instance.TryGetTarget(out var instance))
                    continue;

                yield return (instance, loaded);
            }
        }

        internal sealed class LoadedRenderTarget
        {
            public bool IsWindow;
            public WindowReg? Window;
            public string? Name;

            public Vector2i Size;
            public bool IsSrgb;

            public bool FlipY;

            public RTCF ColorFormat;

            // Remaining properties only apply if the render target is NOT a window.
            // Handle to the loaded clyde texture managing the color attachment.
            public ClydeHandle TextureHandle;

            // Depth/stencil attachment.
            public RhiTexture? DepthStencilTexture;
            public RhiTextureView? DepthSencilTextureView;

            public TextureSampleParameters? SampleParameters;
            public required WeakReference<RenderTargetBase> Instance;

            public long MemoryPressure;
        }

        internal abstract class RenderTargetBase : IRenderTarget
        {
            protected readonly Clyde Clyde;
            private bool _disposed;

            protected RenderTargetBase(Clyde clyde, ClydeHandle handle)
            {
                Clyde = clyde;
                Handle = handle;
            }

            public abstract Vector2i Size { get; }

            public void CopyPixelsToMemory<T>(CopyPixelsDelegate<T> callback, UIBox2i? subRegion = null)
                where T : unmanaged, IPixel<T>
            {
                throw new NotImplementedException();
                //Clyde.CopyRenderTargetPixels(Handle, subRegion, callback);
            }

            public ClydeHandle Handle { get; }

            protected virtual void Dispose(bool disposing)
            {
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            public void DisposeDeferred()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                DisposeDeferredImpl();
                GC.SuppressFinalize(this);
            }

            protected virtual void DisposeDeferredImpl()
            {
            }

            ~RenderTargetBase()
            {
                Dispose(false);
            }
        }

        internal sealed class RenderTexture : RenderTargetBase, IRenderTexture
        {
            public RenderTexture(Vector2i size, ClydeTexture texture, Clyde clyde, ClydeHandle handle)
                : base(clyde, handle)
            {
                Size = size;
                Texture = texture;
            }

            public override Vector2i Size { get; }
            public ClydeTexture Texture { get; }
            Texture IRenderTexture.Texture => Texture;

            protected override void Dispose(bool disposing)
            {
                if (Clyde.IsMainThread())
                {
                    Clyde.DeleteRenderTexture(Handle);
                }
                else
                {
                    DisposeDeferredImpl();
                }
            }

            protected override void DisposeDeferredImpl()
            {
                throw new NotImplementedException();
                // Clyde._renderTargetDisposeQueue.Enqueue(Handle);
            }
        }

        internal sealed class RenderWindow : RenderTargetBase
        {
            public override Vector2i Size => Clyde._renderTargets[Handle].Size;

            public RenderWindow(Clyde clyde, ClydeHandle handle) : base(clyde, handle)
            {
            }
        }
    }
}
