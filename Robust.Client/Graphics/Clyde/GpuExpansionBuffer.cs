using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Robust.Client.Graphics.Rhi;
using Robust.Shared.Collections;
using Robust.Shared.Exceptions;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using RhiBase = Robust.Client.Graphics.Rhi.RhiBase;

namespace Robust.Client.Graphics.Clyde;

internal sealed class GpuExpansionBuffer : IDisposable
{
    private readonly RhiBase _rhi;
    private readonly RhiBufferUsageFlags _usage;
    private readonly string? _label;

    private readonly byte[] _tempBuffer;

    private int IndividualBufferSize => _tempBuffer.Length;

    private ValueList<RhiBuffer> _gpuBuffers;
    private int _curBufferPos;
    private int _curBufferIdx;

    public GpuExpansionBuffer(
        RhiBase rhi,
        int individualBufferSize,
        RhiBufferUsageFlags usage,
        string? label = null)
    {
        if ((usage & RhiBufferUsageFlags.CopyDst) == 0)
            throw new ArgumentException($"Buffer usages must include {nameof(RhiBufferUsageFlags.CopyDst)}");

        _rhi = rhi;
        _usage = usage;
        _label = label;
        _tempBuffer = new byte[individualBufferSize];

        // Make sure we have at least one buffer to start with.
        AllocateBuffer();
        DebugTools.Assert(_curBufferIdx < _gpuBuffers.Count, "Must have one buffer after creation");
    }

    public void Flush()
    {
        if (_curBufferPos == 0)
            return;

        FlushCurrentBuffer();
        _curBufferIdx = 0;

        // TODO: automatically cull GPU buffers if they remain unused for many frames.
    }

    // TODO: Verify this is safe to expose to content.
    public Span<T> Allocate<T>(int count, out Position position)
        where T : unmanaged
    {
        return AllocateAligned<T>(count, 1, out position);
    }

    // TODO: Verify this is safe to expose to content.
    public unsafe Span<T> AllocateAligned<T>(int count, int alignment, out Position position)
        where T : unmanaged
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            throw new TypeArgumentException();

        var length = checked(count * sizeof(T));
        if (length > IndividualBufferSize)
            throw new ArgumentException("Requested allocation is larger than what could fit inside a single buffer.");

        var posRounded = MathHelper.CeilingPowerOfTwo(_curBufferPos, alignment);
        if (posRounded + length > IndividualBufferSize)
        {
            FlushCurrentBuffer();

            DebugTools.Assert(_curBufferPos == 0, "We must have a fresh buffer now");

            posRounded = 0;
        }

        position = new Position(_gpuBuffers[_curBufferIdx], posRounded);

        var byteSpan = _tempBuffer.AsSpan(posRounded, length);
        _curBufferPos = posRounded + length;
        var itemSpan = MemoryMarshal.Cast<byte, T>(byteSpan);

        DebugTools.Assert(itemSpan.Length == count);
        DebugTools.Assert(_curBufferPos <= _tempBuffer.Length);

        return itemSpan;
    }

    private void FlushCurrentBuffer()
    {
        DebugTools.Assert(_curBufferPos != 0, "We have nothing written, flushing the buffer makes no sense!");

        // Temp buffer full. Upload buffer to GPU
        var curBuffer = _gpuBuffers[_curBufferIdx];
        _rhi.Queue.WriteBuffer(curBuffer, 0, _tempBuffer.AsSpan(0, _curBufferPos));

        _curBufferIdx += 1;
        if (_curBufferIdx == _gpuBuffers.Count)
        {
            // Out of spare buffers, make a new one.
            AllocateBuffer();
            DebugTools.Assert(_curBufferIdx < _gpuBuffers.Count, "Must have one buffer after creation");
        }

        _curBufferPos = 0;
    }

    private RhiBuffer AllocateBuffer()
    {
        var idx = _gpuBuffers.Count;
        var buffer = _rhi.CreateBuffer(new RhiBufferDescriptor(
            (ulong) IndividualBufferSize,
            _usage,
            false,
            _label == null ? null : $"{_label}-{idx}")
        );

        _gpuBuffers.Add(buffer);
        return buffer;
    }

    public void Dispose()
    {
        foreach (var gpuBuffer in _gpuBuffers)
        {
            gpuBuffer.Dispose();
        }

        _gpuBuffers.Clear();
    }

    public record struct Position(RhiBuffer Buffer, int ByteOffset);
}
