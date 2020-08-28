using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using OpenToolkit.Graphics.OpenGL4;
using Robust.Client.Interfaces.Graphics;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Robust.Shared.Log;

// ReSharper disable once IdentifierTypo
using RTCF = Robust.Client.Interfaces.Graphics.RenderTargetColorFormat;
using PIF = OpenToolkit.Graphics.OpenGL4.PixelInternalFormat;
using PF = OpenToolkit.Graphics.OpenGL4.PixelFormat;
using PT = OpenToolkit.Graphics.OpenGL4.PixelType;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        private readonly Dictionary<ClydeHandle, LoadedRenderTarget> _renderTargets =
            new Dictionary<ClydeHandle, LoadedRenderTarget>();

        private readonly ConcurrentQueue<ClydeHandle> _renderTargetDisposeQueue
            = new ConcurrentQueue<ClydeHandle>();

        IRenderWindow IClyde.MainWindowRenderTarget => _mainWindowRenderTarget;
        private readonly RenderWindow _mainWindowRenderTarget;

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

            ObjectLabelMaybe(ObjectLabelIdentifier.Framebuffer, fbo, name);

            var (width, height) = size;

            ClydeTexture textureObject;
            GLHandle depthStencilBuffer = default;

            var estPixSize = 0L;

            // Color attachment.
            {
                var texture = new GLHandle(GL.GenTexture());

                GL.BindTexture(TextureTarget.Texture2D, texture.Handle);

                ApplySampleParameters(sampleParameters);

                var colorFormat = format.ColorFormat;
                if ((!_hasGLSrgb) && (colorFormat == RTCF.Rgba8Srgb))
                {
                    // If SRGB is not supported, switch formats.
                    // The shaders will have to compensate.
                    colorFormat = RTCF.Rgba8;
                }
                // This isn't good
                if (!_hasGLFancyFloatFormats)
                {
                    switch (colorFormat)
                    {
                        case RTCF.R32F:
                        case RTCF.RG32F:
                        case RTCF.R11FG11FB10F:
                        case RTCF.Rgba16F:
                            Logger.WarningS("clyde.ogl", "The framebuffer {0} [{1}] is trying to be floating-point when that's not supported. Forcing Rgba8.", name == null ? "[unnamed]" : name, size);
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

                if (!_hasGLES)
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

                textureObject = GenTexture(texture, size, name == null ? null : $"{name}-color");
            }

            // Depth/stencil buffers.
            if (format.HasDepthStencil)
            {
                depthStencilBuffer = new GLHandle(GL.GenRenderbuffer());
                GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthStencilBuffer.Handle);

                ObjectLabelMaybe(ObjectLabelIdentifier.Renderbuffer, depthStencilBuffer,
                    name == null ? null : $"{name}-depth-stencil");

                GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Depth24Stencil8, width,
                    height);

                estPixSize += 4;

                GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
                    RenderbufferTarget.Renderbuffer, depthStencilBuffer.Handle);
                GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.StencilAttachment,
                    RenderbufferTarget.Renderbuffer, depthStencilBuffer.Handle);
            }

            // This should always pass but OpenGL makes it easy to check for once so let's.
            var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            DebugTools.Assert(status == FramebufferErrorCode.FramebufferComplete,
                $"new framebuffer has bad status {status}");

            // Re-bind previous framebuffers.
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, boundDrawBuffer);
            if (_hasGLReadFramebuffer)
            {
                GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, boundReadBuffer);
            }

            var pressure = estPixSize * size.X * size.Y;

            var handle = AllocRid();
            var data = new LoadedRenderTarget
            {
                IsWindow = false,
                DepthStencilHandle = depthStencilBuffer,
                FramebufferHandle = fbo,
                Size = size,
                TextureHandle = textureObject.TextureId,
                MemoryPressure = pressure
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

            DebugTools.Assert(!renderTarget.IsWindow, "Cannot delete window-backed render targets directly.");

            GL.DeleteFramebuffer(renderTarget.FramebufferHandle.Handle);
            _renderTargets.Remove(handle);
            DeleteTexture(renderTarget.TextureHandle);

            if (renderTarget.DepthStencilHandle != default)
            {
                GL.DeleteRenderbuffer(renderTarget.DepthStencilHandle.Handle);
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

        private static void BindRenderTargetImmediate(LoadedRenderTarget rt)
        {
            if (rt.IsWindow)
            {
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            }
            else
            {
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, rt.FramebufferHandle.Handle);
            }
        }

        private void FlushRenderTargetDispose()
        {
            while (_renderTargetDisposeQueue.TryDequeue(out var handle))
            {
                DeleteRenderTexture(handle);
            }
        }

        private void UpdateWindowLoadedRtSize()
        {
            var loadedRt = RtToLoaded(_mainWindowRenderTarget);
            loadedRt.Size = _framebufferSize;
        }

        private sealed class LoadedRenderTarget
        {
            public bool IsWindow;
            public Vector2i Size;

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

            protected RenderTargetBase(Clyde clyde, ClydeHandle handle)
            {
                Clyde = clyde;
                Handle = handle;
            }

            public abstract Vector2i Size { get; }
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
                if (disposing)
                {
                    Clyde.DeleteRenderTexture(Handle);
                }
                else
                {
                    Clyde._renderTargetDisposeQueue.Enqueue(Handle);
                }
            }
        }

        private sealed class RenderWindow : RenderTargetBase, IRenderWindow
        {
            public override Vector2i Size => Clyde._framebufferSize;

            public RenderWindow(Clyde clyde, ClydeHandle handle) : base(clyde, handle)
            {
            }
        }
    }
}
