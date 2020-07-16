using System;
using System.Collections.Generic;
using OpenToolkit.Graphics.OpenGL4;
using Robust.Client.Interfaces.Graphics;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        private readonly Dictionary<ClydeHandle, RenderTargetBase> _renderTargets =
            new Dictionary<ClydeHandle, RenderTargetBase>();

        public IRenderWindow MainWindowRenderTarget { get; }

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
            var boundReadBuffer = GL.GetInteger(GetPName.ReadFramebufferBinding);

            // Generate FBO.
            var fbo = new GLHandle(GL.GenFramebuffer());

            // Bind color attachment to FBO.
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo.Handle);

            ObjectLabelMaybe(ObjectLabelIdentifier.Framebuffer, fbo, name);

            var (width, height) = size;

            ClydeTexture textureObject;
            GLHandle depthStencilBuffer = default;

            // Color attachment.
            {
                var texture = new GLHandle(GL.GenTexture());

                GL.BindTexture(TextureTarget.Texture2D, texture.Handle);

                ApplySampleParameters(sampleParameters);

                var internalFormat = format.ColorFormat switch
                {
                    RenderTargetColorFormat.Rgba8 => PixelInternalFormat.Rgba8,
                    RenderTargetColorFormat.Rgba16F => PixelInternalFormat.Rgba16f,
                    RenderTargetColorFormat.Rgba8Srgb => PixelInternalFormat.Srgb8Alpha8,
                    RenderTargetColorFormat.R11FG11FB10F => PixelInternalFormat.R11fG11fB10f,
                    RenderTargetColorFormat.R32F => PixelInternalFormat.R32f,
                    RenderTargetColorFormat.RG32F => PixelInternalFormat.Rg32f,
                    RenderTargetColorFormat.R8 => PixelInternalFormat.R8,
                    _ => throw new ArgumentOutOfRangeException(nameof(format.ColorFormat), format.ColorFormat, null)
                };

                GL.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, width, height, 0, PixelFormat.Red,
                    PixelType.Byte, IntPtr.Zero);

                GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                    texture.Handle,
                    0);

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

                GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment,
                    RenderbufferTarget.Renderbuffer, depthStencilBuffer.Handle);
            }

            // This should always pass but OpenGL makes it easy to check for once so let's.
            var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            DebugTools.Assert(status == FramebufferErrorCode.FramebufferComplete,
                $"new framebuffer has bad status {status}");

            // Re-bind previous framebuffers.
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, boundDrawBuffer);
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, boundReadBuffer);

            var handle = AllocRid();
            var renderTarget = new RenderTexture(size, textureObject, fbo, this, handle, depthStencilBuffer);
            _renderTargets.Add(handle, renderTarget);
            return renderTarget;
        }

        private void DeleteRenderTexture(RenderTexture renderTarget)
        {
            GL.DeleteFramebuffer(renderTarget.ObjectHandle.Handle);
            _renderTargets.Remove(renderTarget.Handle);
            DeleteTexture(renderTarget.Texture);

            if (renderTarget.DepthStencilBuffer != default)
            {
                GL.DeleteRenderbuffer(renderTarget.DepthStencilBuffer.Handle);
            }
        }

        private void BindRenderTargetFull(RenderTargetBase rt)
        {
            BindRenderTargetImmediate(rt);
            _currentRenderTarget = rt;
        }

        private void BindRenderTargetImmediate(RenderTargetBase rt)
        {
            if (rt is RenderTexture renderTexture)
            {
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, renderTexture.ObjectHandle.Handle);
            }
            else
            {
                DebugTools.Assert(rt is RenderWindow);
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            }
        }

        /*
        This was a dumb idea:

        private void ResizeRenderTarget(RenderTarget rt, Vector2i newSize)
        {
            var loadedTexture = _loadedTextures[rt.Texture.TextureId];
            var textureInstance = rt.Texture;

            // Set new sizes.
            textureInstance.SetSize(newSize);
            rt.Size = newSize;

            // Delete old textures.
            GL.DeleteTexture(loadedTexture.OpenGLObject.Handle);
            if (rt.DepthStencilBuffer != default)
            {
                GL.DeleteRenderbuffer(rt.DepthStencilBuffer.Handle);
            }

            // Delete the entire old framebuffer because bad OpenGL drivers will just explode otherwise.
            GL.DeleteFramebuffer(rt.ObjectHandle.Handle);
        }
        */

        private abstract class RenderTargetBase : IRenderTarget
        {
            protected RenderTargetBase(ClydeHandle handle)
            {
                Handle = handle;
            }

            public abstract Vector2i Size { get; }
            public ClydeHandle Handle { get; }
        }

        private sealed class RenderTexture : RenderTargetBase, IRenderTexture
        {
            private readonly Clyde _clyde;

            public RenderTexture(Vector2i size, ClydeTexture texture, GLHandle objectHandle, Clyde clyde,
                ClydeHandle handle, GLHandle depthStencilBuffer) : base(handle)
            {
                Size = size;
                Texture = texture;
                ObjectHandle = objectHandle;
                _clyde = clyde;
                DepthStencilBuffer = depthStencilBuffer;
            }

            public override Vector2i Size { get; }
            public ClydeTexture Texture { get; }
            public GLHandle DepthStencilBuffer { get; }

            Texture IRenderTexture.Texture => Texture;

            public GLHandle ObjectHandle { get; }

            /*
            // Used to recreate the render target on resize.
            public RenderTargetFormatParameters FormatParameters;
            public TextureSampleParameters? SampleParameters;
            */

            public void Delete()
            {
                _clyde.DeleteRenderTexture(this);
            }
        }

        private sealed class RenderWindow : RenderTargetBase, IRenderWindow
        {
            private readonly Clyde _clyde;
            public override Vector2i Size => _clyde._framebufferSize;

            public RenderWindow(Clyde clyde, ClydeHandle handle) : base(handle)
            {
                _clyde = clyde;
            }
        }
    }
}
