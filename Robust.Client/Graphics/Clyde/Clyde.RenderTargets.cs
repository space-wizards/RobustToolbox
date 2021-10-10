using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using OpenToolkit.Graphics.OpenGL4;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using SixLabors.ImageSharp.PixelFormats;

// ReSharper disable once IdentifierTypo
using RTCF = Robust.Client.Graphics.RenderTargetColorFormat;
using PIF = OpenToolkit.Graphics.OpenGL4.PixelInternalFormat;
using PF = OpenToolkit.Graphics.OpenGL4.PixelFormat;
using PT = OpenToolkit.Graphics.OpenGL4.PixelType;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        private readonly Dictionary<ClydeHandle, LoadedRenderTarget> _renderTargets =
            new();

        private readonly ConcurrentQueue<ClydeHandle> _renderTargetDisposeQueue
            = new();

        // This is always kept up-to-date, except in CreateRenderTarget (because it restores the old value)
        // It is used for SRGB emulation.
        // It, like _mainWindowRenderTarget, is initialized in Clyde's constructor
        private LoadedRenderTarget _currentBoundRenderTarget;

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

            // Cache currently bound framebuffers
            // so if somebody creates a framebuffer while drawing it won't ruin everything.
            // Note that this means _currentBoundRenderTarget goes temporarily out of sync here
            var boundDrawBuffer = GL.GetInteger(GetPName.DrawFramebufferBinding);
            var boundReadBuffer = 0;
            if (_hasGLReadFramebuffer)
            {
                boundReadBuffer = GL.GetInteger(GetPName.ReadFramebufferBinding);
            }

            // Generate FBO.
            var fbo = new GLHandle(GL.GenFramebuffer());

            // Bind color attachment to FBO.
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo.Handle);
            CheckGlError();

            ObjectLabelMaybe(ObjectLabelIdentifier.Framebuffer, fbo, name);

            var (width, height) = size;

            ClydeTexture textureObject;
            GLHandle depthStencilBuffer = default;

            var estPixSize = 0L;

            // Color attachment.
            {
                var texture = new GLHandle(GL.GenTexture());
                CheckGlError();

                GL.BindTexture(TextureTarget.Texture2D, texture.Handle);
                CheckGlError();

                ApplySampleParameters(sampleParameters);

                var colorFormat = format.ColorFormat;
                if ((!_hasGLSrgb) && (colorFormat == RTCF.Rgba8Srgb))
                {
                    // If SRGB is not supported, switch formats.
                    // The shaders will have to compensate.
                    // Note that a check is performed on the *original* format.
                    colorFormat = RTCF.Rgba8;
                }
                // This isn't good
                if (!_hasGLFloatFramebuffers)
                {
                    switch (colorFormat)
                    {
                        case RTCF.R32F:
                        case RTCF.RG32F:
                        case RTCF.R11FG11FB10F:
                        case RTCF.Rgba16F:
                            _sawmillOgl.Warning("The framebuffer {0} [{1}] is trying to be floating-point when that's not supported. Forcing Rgba8.", name == null ? "[unnamed]" : name, size);
                            colorFormat = RTCF.Rgba8;
                            break;
                    }
                }

                // Make sure to specify the correct pixel type and formats even if we're not uploading any data.
                // Not doing this (just sending red/byte) is fine on desktop GL but illegal on ES.
                var (internalFormat, pixFormat, pixType) = colorFormat switch
                {
                    // using block comments to force formatters to not fuck this up.
                    RTCF.Rgba8 => /*       */(PIF.Rgba8, /*       */PF.Rgba, /**/PT.UnsignedByte),
                    RTCF.Rgba16F => /*     */(PIF.Rgba16f, /*     */PF.Rgba, /**/PT.Float),
                    RTCF.Rgba8Srgb => /*   */(PIF.Srgb8Alpha8, /* */PF.Rgba, /**/PT.UnsignedByte),
                    RTCF.R11FG11FB10F => /**/(PIF.R11fG11fB10f, /**/PF.Rgb, /* */PT.Float),
                    RTCF.R32F => /*        */(PIF.R32f, /*        */PF.Red, /* */PT.Float),
                    RTCF.RG32F => /*       */(PIF.Rg32f, /*       */PF.Rg, /*  */PT.Float),
                    RTCF.R8 => /*          */(PIF.R8, /*          */PF.Red, /* */PT.UnsignedByte),
                    _ => throw new ArgumentOutOfRangeException(nameof(format.ColorFormat), format.ColorFormat, null)
                };

                estPixSize += EstPixelSize(internalFormat);

                GL.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, width, height, 0, pixFormat,
                    pixType, IntPtr.Zero);
                CheckGlError();

                if (!_isGLES)
                {
                    GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                        texture.Handle,
                        0);
                }
                else
                {
                    // OpenGL ES uses a different name, and has an odd added target argument
                    GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                        TextureTarget.Texture2D, texture.Handle, 0);
                }
                CheckGlError();

                // Check on original format is NOT a bug, this is so srgb emulation works
                textureObject = GenTexture(texture, size, format.ColorFormat == RTCF.Rgba8Srgb, name == null ? null : $"{name}-color", TexturePixelType.RenderTarget);
            }

            // Depth/stencil buffers.
            if (format.HasDepthStencil)
            {
                depthStencilBuffer = new GLHandle(GL.GenRenderbuffer());
                GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthStencilBuffer.Handle);
                CheckGlError();

                ObjectLabelMaybe(ObjectLabelIdentifier.Renderbuffer, depthStencilBuffer,
                    name == null ? null : $"{name}-depth-stencil");

                GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Depth24Stencil8, width,
                    height);
                CheckGlError();

                estPixSize += 4;

                GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
                    RenderbufferTarget.Renderbuffer, depthStencilBuffer.Handle);
                GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.StencilAttachment,
                    RenderbufferTarget.Renderbuffer, depthStencilBuffer.Handle);
                CheckGlError();
            }

            // This should always pass but OpenGL makes it easy to check for once so let's.
            var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            CheckGlError();
            DebugTools.Assert(status == FramebufferErrorCode.FramebufferComplete,
                $"new framebuffer has bad status {status}");

            // Re-bind previous framebuffers (thus _currentBoundRenderTarget is back in sync)
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, boundDrawBuffer);
            CheckGlError();
            if (_hasGLReadFramebuffer)
            {
                GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, boundReadBuffer);
                CheckGlError();
            }

            var pressure = estPixSize * size.X * size.Y;

            var handle = AllocRid();
            var data = new LoadedRenderTarget
            {
                IsWindow = false,
                IsSrgb = textureObject.IsSrgb,
                DepthStencilHandle = depthStencilBuffer,
                FramebufferHandle = fbo,
                Size = size,
                TextureHandle = textureObject.TextureId,
                MemoryPressure = pressure,
                ColorFormat = format.ColorFormat
            };

            //GC.AddMemoryPressure(pressure);
            var renderTarget = new RenderTexture(size, textureObject, this, handle);
            _renderTargets.Add(handle, data);
            return renderTarget;
        }

        private void DeleteRenderTexture(ClydeHandle handle)
        {
            if (!_renderTargets.TryGetValue(handle, out var renderTarget))
            {
                return;
            }

            DebugTools.Assert(renderTarget.FramebufferHandle != default);
            DebugTools.Assert(!renderTarget.IsWindow, "Cannot delete window-backed render targets directly.");

            GL.DeleteFramebuffer(renderTarget.FramebufferHandle.Handle);
            renderTarget.FramebufferHandle = default;
            CheckGlError();
            _renderTargets.Remove(handle);
            DeleteTexture(renderTarget.TextureHandle);

            if (renderTarget.DepthStencilHandle != default)
            {
                GL.DeleteRenderbuffer(renderTarget.DepthStencilHandle.Handle);
                CheckGlError();
            }

            //GC.RemoveMemoryPressure(renderTarget.MemoryPressure);
        }

        private void BindRenderTargetFull(LoadedRenderTarget rt)
        {
            BindRenderTargetImmediate(rt);
            _currentRenderTarget = rt;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BindRenderTargetFull(RenderTargetBase rt)
        {
            BindRenderTargetFull(RtToLoaded(rt));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private LoadedRenderTarget RtToLoaded(RenderTargetBase rt)
        {
            return _renderTargets[rt.Handle];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BindRenderTargetImmediate(LoadedRenderTarget rt)
        {
            // NOTE: It's critically important that this be the "focal point" of all framebuffer bindings.
            if (rt.IsWindow)
            {
                _glContext!.BindWindowRenderTarget(rt.WindowId);
            }
            else
            {
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, rt.FramebufferHandle.Handle);
                CheckGlError();
            }
            _currentBoundRenderTarget = rt;
        }

        private void FlushRenderTargetDispose()
        {
            while (_renderTargetDisposeQueue.TryDequeue(out var handle))
            {
                DeleteRenderTexture(handle);
            }
        }

        private sealed class LoadedRenderTarget
        {
            public bool IsWindow;
            public WindowId WindowId;

            public Vector2i Size;
            public bool IsSrgb;

#pragma warning disable 649
            // Gets assigned by (currently commented out) GLContextAngle.
            // It's fine don't worry about it.
            public bool FlipY;
#pragma warning restore 649

            public RTCF ColorFormat;

            // Remaining properties only apply if the render target is NOT a window.
            // Handle to the framebuffer object.
            public GLHandle FramebufferHandle;

            // Handle to the loaded clyde texture managing the color attachment.
            public ClydeHandle TextureHandle;

            // Renderbuffer handle
            public GLHandle DepthStencilHandle;
            public long MemoryPressure;
        }

        private abstract class RenderTargetBase : IRenderTarget
        {
            protected readonly Clyde Clyde;
            private bool _disposed;

            public bool MakeGLFence;
            public nint LastGLSync;

            protected RenderTargetBase(Clyde clyde, ClydeHandle handle)
            {
                Clyde = clyde;
                Handle = handle;
            }

            public abstract Vector2i Size { get; }

            public void CopyPixelsToMemory<T>(CopyPixelsDelegate<T> callback, UIBox2i? subRegion = null) where T : unmanaged, IPixel<T>
            {
                Clyde.CopyRenderTargetPixels(Handle, subRegion, callback);
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

        private sealed class RenderTexture : RenderTargetBase, IRenderTexture
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
                Clyde._renderTargetDisposeQueue.Enqueue(Handle);
            }
        }

        private sealed class RenderWindow : RenderTargetBase
        {
            public override Vector2i Size => Clyde._renderTargets[Handle].Size;

            public RenderWindow(Clyde clyde, ClydeHandle handle) : base(clyde, handle)
            {
            }
        }
    }
}
