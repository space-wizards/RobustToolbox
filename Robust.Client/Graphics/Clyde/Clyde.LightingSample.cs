using System;
using System.Collections.Generic;
using System.Diagnostics;
using OpenToolkit.Graphics.OpenGL4;
using Robust.Client.Utility;
using Robust.Shared.Graphics;
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

        private readonly List<SampleBrightness> _transferringLightCopies = new();

        private void SampleLighting(Viewport viewport, RenderTexture renderTarget, UIBox2i subRegion)
        {
            if (viewport.Eye == null || !viewport.Eye.MeasureBrightness)
            {
                return;
            }

            if (!_hasGLFenceSync || !HasGLAnyMapBuffer || !_hasGLPixelBufferObjects)
            {
                // We need these 3 features to be able to do asynchronous sampling.
                // Sample exposure in foreground instead.

                // Midpoint of the screen and a box around the player.
                // It's expensive to get textures back from the GPU but the results are worth it.
                var centreSqColor = viewport.LightRenderTarget.Texture.MeasureBrightness(subRegion.Left,
                    subRegion.Top, subRegion.Size.X, subRegion.Size.Y);

                // When calculating intensity, count the red less because red doesn't bother human night vision, so why
                //   not extend that into the game.
                var intensity = centreSqColor.R * 0.2f + centreSqColor.G * 0.4f + centreSqColor.B * 0.4f;
                if (!_hasGLFloatFramebuffers)
                {
                    // Measured intensity is going to cap out at 1.0 because without floats we have no overbrighten.
                    //   So aim for a slightly darker fullbright.
                    intensity *= 1.5f;
                }

                // User code can now use this to adjust exposure. See EyeExposureSystem.UpdateViewportExposure in
                //   SS14 client code.
                viewport.Eye.LastBrightness = intensity;
                return;
            }

            GL.PixelStore(PixelStoreParameter.PackAlignment, 1);
            CheckGlError();

            var loaded = _renderTargets[renderTarget.Handle];

            var original = GL.GetInteger(GetPName.ReadFramebufferBinding);

            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, loaded.FramebufferHandle.Handle);

            var pf = renderTarget.Texture.Format.pixFormat;
            var pt = renderTarget.Texture.Format.pixType;

            DoSamplePixels(loaded.Size, subRegion, pf, pt, viewport.Eye);

            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, original);
        }

        private unsafe void DoSamplePixels(
            Vector2i fbSize,
            UIBox2i subRegion,
            PF pf, PT pt, IEye eye)
        {
            var intersect = UIBox2i.FromDimensions(Vector2i.Zero, fbSize).Intersection(subRegion);
            if (intersect == null)
                return;

            var region = intersect.Value;
            var size = region.Size;

            var bufferLength = size.X * size.Y;
            var stride = pf == PF.Rgb ? 3 : 4;
            var bufSize = (pt == PT.Float ? sizeof(float) : sizeof(byte)) * bufferLength * stride;

            if (!_hasGLFenceSync || !HasGLAnyMapBuffer || !_hasGLPixelBufferObjects)
            {
                // We need these 3 features to be able to do asynchronous sampling.

                return;
            }

            GL.GenBuffers(1, out uint pbo);
            CheckGlError();

            GL.BindBuffer(BufferTarget.PixelPackBuffer, pbo);
            CheckGlError();

            GL.BufferData(
                BufferTarget.PixelPackBuffer,
                bufSize, IntPtr.Zero,
                BufferUsageHint.StreamRead);
            CheckGlError();

            GL.ReadPixels(region.Left, region.Top, size.X, size.Y, pf, pt, IntPtr.Zero);
            CheckGlError();

            var fence = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, WaitSyncFlags.None);
            CheckGlError();

            GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);
            CheckGlError();

            var transferring = new SampleBrightness(pbo, fence, size, pf, pt, eye);
            _transferringLightCopies.Add(transferring);
        }

        private unsafe void CheckTransferringLights()
        {
            if (_transferringLightCopies.Count == 0)
            {
                return;
            }

            for (var i = 0; i < _transferringLightCopies.Count; i++)
            {
                var transferring = _transferringLightCopies[i];

                // Check if transfer done (sync signalled)
                int status;
                GL.GetSync(transferring.Sync, SyncParameterName.SyncStatus, sizeof(int), null, &status);
                CheckGlError();

                if (status != (int) All.Signaled)
                    continue;

                FinishSamplingTransfer(transferring);
                _transferringLightCopies.RemoveSwap(i--);
            }
        }

        private unsafe void FinishSamplingTransfer(SampleBrightness transferring)
        {
            var (pbo, fence, (width, height), pf, pt, eye) = transferring;

            var stride = pf == PF.Rgb ? 3 : 4;
            var numPixels = width * height;
            var bufSize = (pt == PT.Float ? sizeof(float) : sizeof(byte)) * numPixels * stride;

            GL.BindBuffer(BufferTarget.PixelPackBuffer, pbo);
            CheckGlError();

            var ptr = MapFullBuffer(
                BufferTarget.PixelPackBuffer,
                bufSize,
                BufferAccess.ReadOnly,
                BufferAccessMask.MapReadBit);

            // This is the output of all this sampling - a single intensity value for the foreground to use.
            eye.LastBrightness = CalcBufferIntensity(pt, ptr, numPixels, stride);

            UnmapBuffer(BufferTarget.PixelPackBuffer);
            GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);
            CheckGlError();

            GL.DeleteBuffer(pbo);
            CheckGlError();

            GL.DeleteSync(fence);
            CheckGlError();
        }

        private static unsafe float CalcBufferIntensity(PixelType pt, void* ptr, int numPixels, int stride)
        {
            Color accum = new Color();
            int bufLen = numPixels * stride;
            var divisor = (1.0f / (float)(numPixels));

            if (pt == PT.Float)
            {
                var packSpan = new ReadOnlySpan<float>(ptr, bufLen);
                for (int i = 0; i < bufLen; i += stride)
                {
                    accum.R += packSpan[i + 0];
                    accum.G += packSpan[i + 1];
                    accum.B += packSpan[i + 2];
                }
            }
            else
            {
                var packSpan = new ReadOnlySpan<byte>(ptr, bufLen);
                for (int i = 0; i < bufLen; i += stride)
                {
                    accum.R += packSpan[i + 0];
                    accum.G += packSpan[i + 1];
                    accum.B += packSpan[i + 2];
                }

                // Measured intensity is going to cap out at 1.0 because without floats we have no overbrighten.
                //   So aim for a slightly darker fullbright.
                divisor *= 1.2f;
            }

            var centreSqColor = accum * new Color(divisor, divisor, divisor, divisor);
            var intensity = centreSqColor.R * 0.2f + centreSqColor.G * 0.4f + centreSqColor.B * 0.4f;
            return intensity;
        }

        private sealed record SampleBrightness(
            uint Pbo,
            nint Sync,
            Vector2i Size,
            PF pf,
            PT pt,
            IEye Eye
        );
    }
}
