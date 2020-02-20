using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using Robust.Client.Interfaces.Graphics;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        private readonly Dictionary<ClydeHandle, RenderTarget> _renderTargets =
            new Dictionary<ClydeHandle, RenderTarget>();

        IRenderTarget IClyde.CreateRenderTarget(Vector2i size, RenderTargetFormatParameters format,
            TextureSampleParameters? sampleParameters, string name)
        {
            return CreateRenderTarget(size, format, sampleParameters, name);
        }

        private RenderTarget CreateRenderTarget(Vector2i size, RenderTargetFormatParameters format,
            TextureSampleParameters? sampleParameters = null, string name = null)
        {
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
            var renderTarget = new RenderTarget(size, textureObject, fbo, this, handle, depthStencilBuffer);
            _renderTargets.Add(handle, renderTarget);
            return renderTarget;
        }

        private void DeleteRenderTarget(RenderTarget renderTarget)
        {
            GL.DeleteFramebuffer(renderTarget.ObjectHandle.Handle);
            _renderTargets.Remove(renderTarget.Handle);
            DeleteTexture(renderTarget.Texture);

            if (renderTarget.DepthStencilBuffer != default)
            {
                GL.DeleteRenderbuffer(renderTarget.DepthStencilBuffer.Handle);
            }
        }

        private sealed class RenderTarget : IRenderTarget
        {
            private readonly Clyde _clyde;

            public RenderTarget(Vector2i size, ClydeTexture texture, GLHandle objectHandle, Clyde clyde,
                ClydeHandle handle, GLHandle depthStencilBuffer)
            {
                Size = size;
                Texture = texture;
                ObjectHandle = objectHandle;
                _clyde = clyde;
                Handle = handle;
                DepthStencilBuffer = depthStencilBuffer;
            }

            public Vector2i Size { get; }
            public ClydeTexture Texture { get; }
            public ClydeHandle Handle { get; }
            public GLHandle DepthStencilBuffer { get; }

            Texture IRenderTarget.Texture => Texture;

            public GLHandle ObjectHandle { get; }

            public void Delete()
            {
                _clyde.DeleteRenderTarget(this);
            }

            public void Bind()
            {
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, ObjectHandle.Handle);
            }
        }
    }
}
