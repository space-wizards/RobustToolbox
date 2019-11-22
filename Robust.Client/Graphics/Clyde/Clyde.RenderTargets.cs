using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using Robust.Client.Interfaces.Graphics;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        private readonly Dictionary<ClydeHandle, RenderTarget> _renderTargets =
            new Dictionary<ClydeHandle, RenderTarget>();

        IRenderTarget IClyde.CreateRenderTarget(Vector2i size, RenderTargetColorFormat colorFormat,
            TextureSampleParameters? sampleParameters, string name)
        {
            return CreateRenderTarget(size, colorFormat, sampleParameters, name);
        }

        private RenderTarget CreateRenderTarget(Vector2i size, RenderTargetColorFormat colorFormat,
            TextureSampleParameters? sampleParameters = null, string name = null)
        {
            // Generate color attachment texture.
            var texture = new OGLHandle(GL.GenTexture());

            GL.BindTexture(TextureTarget.Texture2D, texture.Handle);

            _applySampleParameters(sampleParameters);

            PixelInternalFormat internalFormat;
            switch (colorFormat)
            {
                case RenderTargetColorFormat.Rgba8:
                    internalFormat = PixelInternalFormat.Rgba8;
                    break;
                case RenderTargetColorFormat.Rgba16F:
                    internalFormat = PixelInternalFormat.Rgba16f;
                    break;
                case RenderTargetColorFormat.Rgba8Srgb:
                    internalFormat = PixelInternalFormat.Srgb8Alpha8;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(colorFormat), colorFormat, null);
            }

            var (width, height) = size;
            GL.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, width, height, 0, PixelFormat.Red,
                PixelType.Byte, IntPtr.Zero);

            // Generate FBO.
            var fbo = new OGLHandle(GL.GenFramebuffer());

            // Cache currently bound framebuffers
            // so if somebody creates a framebuffer while drawing it won't ruin everything.
            var boundDrawBuffer = GL.GetInteger(GetPName.DrawFramebufferBinding);
            var boundReadBuffer = GL.GetInteger(GetPName.ReadFramebufferBinding);

            // Bind color attachment to FBO.
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo.Handle);

            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, texture.Handle,
                0);

            // This should always pass but OpenGL makes it easy to check for once so let's.
            var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            DebugTools.Assert(status == FramebufferErrorCode.FramebufferComplete,
                $"new framebuffer has bad status {status}");

            // Re-bind previous framebuffers.
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, boundDrawBuffer);
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, boundReadBuffer);

            var textureObject = _genTexture(texture, size, name);
            var handle = AllocRid();
            var renderTarget = new RenderTarget(size, textureObject, fbo, this, handle);
            _renderTargets.Add(handle, renderTarget);
            return renderTarget;
        }

        private void _deleteRenderTarget(RenderTarget renderTarget)
        {
            GL.DeleteFramebuffer(renderTarget.ObjectHandle.Handle);
            _renderTargets.Remove(renderTarget.Handle);
            _deleteTexture(renderTarget.Texture);
        }

        private sealed class RenderTarget : IRenderTarget
        {
            private readonly Clyde _clyde;

            public RenderTarget(Vector2i size, ClydeTexture texture, OGLHandle objectHandle, Clyde clyde, ClydeHandle handle)
            {
                Size = size;
                Texture = texture;
                ObjectHandle = objectHandle;
                _clyde = clyde;
                Handle = handle;
            }

            public Vector2i Size { get; }
            public ClydeTexture Texture { get; }
            public ClydeHandle Handle { get; }

            Texture IRenderTarget.Texture => Texture;

            public OGLHandle ObjectHandle { get; }

            public void Delete()
            {
                _clyde._deleteRenderTarget(this);
            }
        }
    }
}
