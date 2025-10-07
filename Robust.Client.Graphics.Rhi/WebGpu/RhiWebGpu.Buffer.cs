using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Robust.Client.Graphics.Rhi.WebGpu;

internal sealed partial class RhiWebGpu
{
    private readonly Dictionary<RhiHandle, BufferReg> _bufferRegistry = new();

    public override unsafe RhiBuffer CreateBuffer(in RhiBufferDescriptor descriptor)
    {
        Span<byte> buffer = stackalloc byte[512];
        var pDescriptor = BumpAllocate<WGPUBufferDescriptor>(ref buffer);
        pDescriptor->label = BumpAllocateStringView(ref buffer, descriptor.Label);
        pDescriptor->mappedAtCreation = descriptor.MappedAtCreation ? 1u : 0u;
        pDescriptor->size = descriptor.Size;
        pDescriptor->usage = (ulong) descriptor.Usage;

        var native = wgpuDeviceCreateBuffer(_wgpuDevice, pDescriptor);

        var handle = AllocRhiHandle();
        _bufferRegistry.Add(handle, new BufferReg { Native = native });
        var rhiBuffer= new RhiBuffer(this, handle);

        if (pDescriptor->mappedAtCreation == 1)
        {
            rhiBuffer.Mapping = new RhiBuffer.ActiveMapping(rhiBuffer) { Valid = true };
        }

        return rhiBuffer;
    }

    internal override unsafe RhiBufferMapState BufferGetMapState(RhiBuffer buffer)
    {
        var nativeBuffer = _bufferRegistry[buffer.Handle].Native;
        return (RhiBufferMapState) wgpuBufferGetMapState(nativeBuffer);
    }

    internal override async ValueTask BufferMapAsync(RhiBuffer buffer, RhiMapModeFlags mode, nuint offset, nuint size)
    {
        // TODO: Probably need some more locks here idk.
        // So people can't map the buffer at the same time as or something.

        buffer.MapState = RhiBufferMapState.Pending;

        WgpuMapBufferAsyncResult result;
        using (var promise = new WgpuPromise<WgpuMapBufferAsyncResult>())
        {
            unsafe
            {
                var nativeBuffer = _bufferRegistry[buffer.Handle].Native;

                wgpuBufferMapAsync(
                    nativeBuffer,
                    (ulong) mode,
                    offset,
                    size,
                    new WGPUBufferMapCallbackInfo
                    {
                        callback = &WgpuMapBufferAsyncCallback,
                        userdata1 = promise.UserData,
                    }
                );
            }

            // TODO: are we handling the error correctly, here?
            result = await promise.Task;

            buffer.Mapping = new RhiBuffer.ActiveMapping(buffer) { Valid = true };
        }

        if (result.Status != WGPUMapAsyncStatus.WGPUMapAsyncStatus_Success)
            throw new RhiException(result.Status.ToString());

        buffer.MapState = RhiBufferMapState.Mapped;
    }

    internal override unsafe RhiMappedBufferRange BufferGetMappedRange(RhiBuffer buffer, nuint offset, nuint size)
    {
        if (size > int.MaxValue)
            throw new ArgumentException("Mapped area too big!");

        if (buffer.Mapping == null)
            throw new InvalidOperationException("Buffer is not mapped");

        lock (buffer.Mapping)
        {
            if (!buffer.Mapping.Valid)
            {
                // Not sure if this is possible, but can't hurt.
                throw new InvalidOperationException();
            }

            var nativeBuffer = _bufferRegistry[buffer.Handle].Native;
            var mapped = wgpuBufferGetMappedRange(nativeBuffer, offset, size);

            return new RhiMappedBufferRange(buffer.Mapping, mapped, (int) size);
        }
    }

    internal override unsafe void BufferUnmap(RhiBuffer buffer)
    {
        if (buffer.Mapping == null)
            throw new InvalidOperationException("Buffer is not mapped!");

        lock (buffer.Mapping)
        {
            if (!buffer.Mapping.Valid)
            {
                // Not sure if this is possible, but can't hurt.
                throw new InvalidOperationException();
            }

            if (buffer.Mapping.ActiveSpans > 0)
                throw new InvalidOperationException("Current thread has buffer accessible as span, cannot unmap!");

            var nativeBuffer = _bufferRegistry[buffer.Handle].Native;
            wgpuBufferUnmap(nativeBuffer);

            buffer.Mapping.Valid = false;
            buffer.Mapping = null;
            buffer.MapState = RhiBufferMapState.Unmapped;
        }
    }

    internal override unsafe void BufferDrop(RhiBuffer buffer)
    {
        wgpuBufferRelease(_bufferRegistry[buffer.Handle].Native);
        _bufferRegistry.Remove(buffer.Handle);
    }

    private sealed unsafe class BufferReg
    {
        public WGPUBuffer Native;
    }

    private record struct WgpuMapBufferAsyncResult(WGPUMapAsyncStatus Status);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static unsafe void WgpuMapBufferAsyncCallback(
        WGPUMapAsyncStatus status,
        WGPUStringView stringView,
        void* userdata1,
        void* userdata2)
    {
        WgpuPromise<WgpuMapBufferAsyncResult>.SetResult(userdata1, new WgpuMapBufferAsyncResult(status));
    }
}
