namespace Robust.Client.Graphics.Rhi.WebGpu;

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

        var nativeDestination = new WGPUTexelCopyTextureInfo
        {
            aspect = (WGPUTextureAspect)ValidateTextureAspect(destination.Aspect),
            texture = nativeTexture,
            origin = WgpuOrigin3D(destination.Origin),
            mipLevel = destination.MipLevel
        };

        var nativeDataLayout = new WGPUTexelCopyBufferLayout
        {
            // TODO: Validation
            offset = dataLayout.Offset,
            bytesPerRow = dataLayout.BytesPerRow,
            rowsPerImage = dataLayout.RowsPerImage
        };

        var nativeSize = WgpuExtent3D(size);

        fixed (byte* pData = data)
        {
            wgpuQueueWriteTexture(
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
            wgpuQueueWriteBuffer(
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

        var pBuffers = stackalloc WGPUCommandBuffer[commandBuffers.Length];
        for (var i = 0; i < commandBuffers.Length; i++)
        {
            pBuffers[i] = _commandBufferRegistry[commandBuffers[i].Handle].Native;
        }

        wgpuQueueSubmit(
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
