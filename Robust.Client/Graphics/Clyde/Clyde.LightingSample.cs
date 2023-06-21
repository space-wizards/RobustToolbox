using System;
using System.Collections.Generic;
using OpenToolkit.Graphics.OpenGL4;
using Robust.Client.Utility;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = Robust.Shared.Maths.Color;
using PF = OpenToolkit.Graphics.OpenGL4.PixelFormat;
using PixelFormat = OpenToolkit.Graphics.OpenGL.PixelFormat;
using PT = OpenToolkit.Graphics.OpenGL4.PixelType;

namespace Robust.Client.Graphics.Clyde
{
    // Contains primary screenshot and pixel-copying logic.

    internal sealed partial class Clyde
    {
        // For sampling lighting buffer to see how bright the scene is
        // 1. User specifies IEye.MeasureBrightness to true
        // 2. When the rendering code has processed the lighting buffer
        //    we instruct the GPU driver to copy the lighting buffer, reducing its resolution and asynchronously transfer it to host memory.
        // 3. Transfer finished asynchronously, we copy the middle pixel to IEye.LastBrightness
        //
        // On RAW GLES2, we cannot do this asynchronously due to lacking GL features,
        // and the game will stutter as a result. This is sadly unavoidable.

        private void SampleLighting(Viewport viewport, RenderTexture renderTarget, UIBox2i subRegion)
        {
            if (!viewport.Eye?.MeasureBrightness ?? false)
            {
                return;
            }

            GL.PixelStore(PixelStoreParameter.PackAlignment, 1);
            CheckGlError();

            var loaded = _renderTargets[renderTarget.Handle];

            var original = GL.GetInteger(GetPName.ReadFramebufferBinding);

            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, loaded.FramebufferHandle.Handle);

            var pf = renderTarget.Texture.Format.pixFormat;
            var pt = renderTarget.Texture.Format.pixType;
            DoSamplePixels(loaded.Size, subRegion, pf, pt);

            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, original);
        }

        private unsafe void DoSamplePixels(
            Vector2i fbSize,
            UIBox2i subRegion,
            PF pf, PT pt)
        {
            var intersect = UIBox2i.FromDimensions(Vector2i.Zero, fbSize).Intersection(subRegion);
            if (intersect == null)
                return;

            var region = intersect.Value;
            var size = region.Size;

            var bufferLength = size.X * size.Y;
            if (!(_hasGLFenceSync && HasGLAnyMapBuffer && _hasGLPixelBufferObjects))
            {
                _sawmillOgl.Debug("clyde.ogl",
                    "Necessary features for async screenshots not available, falling back to blocking path.");

                // We need these 3 features to be able to do asynchronous screenshots, if we don't have them,
                // we'll have to fall back to a crappy synchronous stalling method of glReadnPixels().

                var stride = pf == PF.Rgb ? 3 : 4;
                Span<float> rgba = stackalloc float[stride * size.X * size.Y];
                unsafe
                {
                    fixed (float* p = rgba)
                    {
                        GL.ReadnPixels(
                            region.Left, region.Top,
                            size.X, size.Y,
                            pf, pt,
                            rgba.Length * sizeof(float),
                            (nint)p);

                        CheckGlError();
                    }
                }

                var accum = new Color();
                for (int i = 0; i < rgba.Length; i+=stride)
                {
                    accum.R += rgba[i + 0];
                    accum.G += rgba[i + 1];
                    accum.B += rgba[i + 2];
                }

                var divisor = (1.0f / (float)(size.X * size.Y));
                var result = accum * new Color(divisor, divisor, divisor, divisor);
                
                return;
            }

            GL.GenBuffers(1, out uint pbo);
            CheckGlError();

            GL.BindBuffer(BufferTarget.PixelPackBuffer, pbo);
            CheckGlError();

            GL.BufferData(
                BufferTarget.PixelPackBuffer,
                bufferLength * sizeof(Rgba32), IntPtr.Zero,
                BufferUsageHint.StreamRead);
            CheckGlError();

            GL.ReadPixels(0, 0, size.X, size.Y, pf, pt, IntPtr.Zero);
            CheckGlError();

            var fence = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, WaitSyncFlags.None);
            CheckGlError();

            GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);
            CheckGlError();

            var transferring = new TransferringPixelCopy(pbo, fence, size, FinishPixelTransfer<T>, callback);
            _transferringPixelCopies.Add(transferring);
        }

        private unsafe void CheckTransferringScreenshots()
        {
            if (_transferringPixelCopies.Count == 0)
            {
                return;
            }

            for (var i = 0; i < _transferringPixelCopies.Count; i++)
            {
                var transferring = _transferringPixelCopies[i];

                // Check if transfer done (sync signalled)
                int status;
                GL.GetSync(transferring.Sync, SyncParameterName.SyncStatus, sizeof(int), null, &status);
                CheckGlError();

                if (status != (int) All.Signaled)
                    continue;

                transferring.TransferContinue(transferring);
                _transferringPixelCopies.RemoveSwap(i--);
            }
        }

        private unsafe void FinishPixelTransfer<T>(TransferringPixelCopy transferring) where T : unmanaged, IPixel<T>
        {
            var (pbo, fence, (width, height), _, callback) = transferring;

            var bufLen = width * height;
            var bufSize = sizeof(T) * bufLen;

            GL.BindBuffer(BufferTarget.PixelPackBuffer, pbo);
            CheckGlError();

            var ptr = MapFullBuffer(
                BufferTarget.PixelPackBuffer,
                bufSize,
                BufferAccess.ReadOnly,
                BufferAccessMask.MapReadBit);

            var packSpan = new ReadOnlySpan<T>(ptr, width * height);

            var image = new Image<T>(width, height);
            var imageSpan = image.GetPixelSpan();

            FlipCopy(packSpan, imageSpan, width, height);

            UnmapBuffer(BufferTarget.PixelPackBuffer);

            GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);
            CheckGlError();

            GL.DeleteBuffer(pbo);
            CheckGlError();

            GL.DeleteSync(fence);
            CheckGlError();

            var castCallback = (CopyPixelsDelegate<T>) callback;
            castCallback(image);
        }

        private sealed record SampleBrightness(
            uint Pbo,
            nint Sync,
            Vector2i Size,
            IEye Eye
        );
    }
}
