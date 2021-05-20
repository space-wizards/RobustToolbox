using System;
using System.Collections.Generic;
using OpenToolkit.Graphics.OpenGL4;
using Robust.Client.Utility;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using PF = OpenToolkit.Graphics.OpenGL4.PixelFormat;
using PT = OpenToolkit.Graphics.OpenGL4.PixelType;

namespace Robust.Client.Graphics.Clyde
{
    // Contains primary screenshot and pixel-copying logic.

    internal sealed partial class Clyde
    {
        // Full-framebuffer screenshots undergo the following sequence of events:
        // 1. Screenshots are queued by content or whatever.
        // 2. When the rendering code reaches the screenshot type,
        //    we instruct the GPU driver to copy the framebuffer and asynchronously transfer it to host memory.
        // 3. Transfer finished asynchronously, we invoke the callback.
        //
        // On RAW GLES2, we cannot do this asynchronously due to lacking GL features,
        // and the game will stutter as a result. This is sadly unavoidable.
        //
        // For CopyPixels on render targets, the copy and transfer is started immediately when the function is called.

        private readonly List<QueuedScreenshot> _queuedScreenshots = new();
        private readonly List<TransferringPixelCopy> _transferringPixelCopies = new();

        public void Screenshot(ScreenshotType type, CopyPixelsDelegate<Rgb24> callback, UIBox2i? subRegion = null)
        {
            _queuedScreenshots.Add(new QueuedScreenshot(type, callback, subRegion));
        }

        private void TakeScreenshot(ScreenshotType type)
        {
            if (_queuedScreenshots.Count == 0)
            {
                return;
            }

            GL.PixelStore(PixelStoreParameter.PackAlignment, 1);
            CheckGlError();

            for (var i = 0; i < _queuedScreenshots.Count; i++)
            {
                var (qType, callback, subRegion) = _queuedScreenshots[i];
                if (qType != type)
                    continue;

                DoCopyPixels(ScreenSize, subRegion, callback);
                _queuedScreenshots.RemoveSwap(i--);
            }
        }

        private void CopyRenderTargetPixels<T>(
            ClydeHandle renderTarget,
            UIBox2i? subRegion,
            CopyPixelsDelegate<T> callback)
            where T : unmanaged, IPixel<T>
        {
            var loaded = _renderTargets[renderTarget];

            var original = GL.GetInteger(GetPName.ReadFramebufferBinding);

            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, loaded.FramebufferHandle.Handle);

            DoCopyPixels(loaded.Size, subRegion, callback);

            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, original);
        }

        private unsafe void DoCopyPixels<T>(
            Vector2i fbSize,
            UIBox2i? subRegion,
            CopyPixelsDelegate<T> callback)
            where T : unmanaged, IPixel<T>
        {
            var (pf, pt) = default(T) switch
            {
                Rgba32 => (PF.Rgba, PT.UnsignedByte),
                Rgb24 => (PF.Rgb, PT.UnsignedByte),
                _ => throw new ArgumentException("Unsupported pixel type.")
            };

            var size = ClydeBase.ClampSubRegion(fbSize, subRegion);

            var bufferLength = size.X * size.Y;
            if (!(_hasGLFenceSync && HasGLAnyMapBuffer && _hasGLPixelBufferObjects))
            {
                _sawmillOgl.Debug("clyde.ogl",
                    "Necessary features for async screenshots not available, falling back to blocking path.");

                // We need these 3 features to be able to do asynchronous screenshots, if we don't have them,
                // we'll have to fall back to a crappy synchronous stalling method of glReadnPixels().

                var buffer = new T[bufferLength];
                fixed (T* ptr = buffer)
                {
                    var bufSize = sizeof(T) * bufferLength;
                    GL.ReadnPixels(
                        0, 0,
                        size.X, size.Y,
                        pf, pt,
                        bufSize,
                        (nint) ptr);

                    CheckGlError();
                }

                var image = new Image<T>(size.X, size.Y);
                var imageSpan = image.GetPixelSpan();

                FlipCopy(buffer, imageSpan, size.X, size.Y);

                callback(image);
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

        private sealed record QueuedScreenshot(
            ScreenshotType Type,
            CopyPixelsDelegate<Rgb24> Callback,
            UIBox2i? SubRegion);

        private sealed record TransferringPixelCopy(
            uint Pbo,
            nint Sync,
            Vector2i Size,
            // Funny callback dance to handle the generics.
            Action<TransferringPixelCopy> TransferContinue,
            Delegate Callback);
    }
}
