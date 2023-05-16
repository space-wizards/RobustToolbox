using System;
using Silk.NET.WebGPU;

namespace Robust.Client.Graphics.Clyde.Rhi;

internal sealed unsafe partial class RhiWebGpu
{
    public override RhiQueue Queue { get; }

    // queue is ignored as parameter, since WebGPU only supports one queue for now.

    internal override void QueueWriteTexture(
        RhiQueue queue,
        in RhiImageCopyTexture destination,
        ReadOnlySpan<byte> data,
        in RhiImageDataLayout dataLayout,
        RhiExtent3D size)
    {
        // TODO: Thread safety
        var nativeTexture = _textureRegistry[destination.Texture.Handle].Native;

        var nativeDestination = new ImageCopyTexture
        {
            Aspect = (TextureAspect)ValidateTextureAspect(destination.Aspect),
            Texture = nativeTexture,
            Origin = WgpuOrigin3D(destination.Origin),
            MipLevel = destination.MipLevel
        };

        var nativeDataLayout = new TextureDataLayout
        {
            // TODO: Validation
            Offset = dataLayout.Offset,
            BytesPerRow = dataLayout.BytesPerRow,
            RowsPerImage = dataLayout.RowsPerImage
        };

        var nativeSize = WgpuExtent3D(size);

        fixed (byte* pData = data)
        {
            _webGpu.QueueWriteTexture(
                _wgpuQueue,
                &nativeDestination,
                pData, (nuint) data.Length,
                &nativeDataLayout,
                &nativeSize
            );
        }
    }

    public override void QueueWriteBuffer(RhiBuffer buffer, ulong bufferOffset, ReadOnlySpan<byte> data)
    {
        var nativeBuffer = _bufferRegistry[buffer.Handle].Native;

        fixed (byte* pData = data)
        {
            _webGpu.QueueWriteBuffer(
                _wgpuQueue,
                nativeBuffer,
                bufferOffset,
                pData,
                (nuint) data.Length);
        }
    }

    internal override void QueueSubmit(RhiQueue queue, RhiCommandBuffer[] commandBuffers)
    {
        // TODO: Safety

        var pBuffers = stackalloc CommandBuffer*[commandBuffers.Length];
        for (var i = 0; i < commandBuffers.Length; i++)
        {
            pBuffers[i] = _commandBufferRegistry[commandBuffers[i].Handle].Native;
        }

        _webGpu.QueueSubmit(
            _wgpuQueue,
            (uint) commandBuffers.Length,
            pBuffers
        );

        foreach (var commandBuffer in commandBuffers)
        {
            CommandBufferDropped(commandBuffer);
        }
    }
}
